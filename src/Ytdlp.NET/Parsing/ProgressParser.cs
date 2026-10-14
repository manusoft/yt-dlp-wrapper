using System.Text.RegularExpressions;

namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Parses yt-dlp output and raises strongly-typed progress,
/// download, and post-processing events.
/// </summary>
public sealed class ProgressParser
{
    private readonly Dictionary<Regex, Action<Match>> _regexHandlers;
    private readonly ILogger _logger;

    private readonly RegexOptions compiledIgnoreCase = RegexOptions.Compiled | RegexOptions.IgnoreCase;

    // State tracking
    private bool _isDownloadCompleted;
    private bool _postProcessingStarted;
    private int _postProcessStepCount;
    private int _deleteCount;

    // ───────────── Events (unchanged) ─────────────
    #region Events   
    internal event EventHandler<DownloadProgressEventArgs>? ProgressDownload;
    internal event EventHandler<string>? ProgressMessage;
    internal event EventHandler<string>? DownloadCompleted;
    internal event EventHandler<string>? PostProcessingStarted;
    internal event EventHandler<string>? PostProcessingCompleted;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressParser"/> class.
    /// </summary>
    /// <param name="logger">
    /// Optional logger used for diagnostic and parsing messages.
    /// If <see langword="null"/>, a default logger implementation is used.
    /// </param>
    public ProgressParser(ILogger? logger = null)
    {
        _logger = logger ?? new DefaultLogger();

        _regexHandlers = new Dictionary<Regex, Action<Match>>
        {
            // Begining
            { new Regex(RegexPatterns.DownloadDestination, compiledIgnoreCase), HandleDownloadDestination },
            { new Regex(RegexPatterns.ResumeDownload, compiledIgnoreCase), HandleResumeDownload },
            { new Regex(RegexPatterns.DownloadAlreadyDownloaded, compiledIgnoreCase), HandleDownloadAlreadyCompleted },


            // Live / no-total-size progress (livestreams)
            // Matches both:
            //   [download]    8.36MiB at  169.71KiB/s (00:01:07) (frag 244)
            //   2: [download]    8.36MiB at  169.71KiB/s (00:01:07) (frag 244)
            { new Regex(RegexPatterns.DownloadProgressLive, compiledIgnoreCase), HandleDownloadProgressLive },

            // Progress
            { new Regex(RegexPatterns.DownloadProgressComplete, compiledIgnoreCase), HandleDownloadProgressComplete },
            { new Regex(RegexPatterns.DownloadProgressWithFrag, compiledIgnoreCase), HandleDownloadProgressWithFrag },
            { new Regex(RegexPatterns.DownloadProgress, compiledIgnoreCase), HandleDownloadProgress },

            // Post-processing            
            { new Regex(RegexPatterns.FixupM3u8, compiledIgnoreCase), HandlePostProcessingStep },
            { new Regex(RegexPatterns.VideoRemuxer, compiledIgnoreCase), HandlePostProcessingStep },
            { new Regex(RegexPatterns.Metadata, compiledIgnoreCase), HandlePostProcessingStep },
            { new Regex(RegexPatterns.ThumbnailsConvertor, compiledIgnoreCase), HandlePostProcessingStep },
            { new Regex(RegexPatterns.EmbedThumbnail, compiledIgnoreCase), HandlePostProcessingStep },
            { new Regex(RegexPatterns.MoveFiles, compiledIgnoreCase), HandlePostProcessingStep },
            { new Regex(RegexPatterns.PostProcessorGeneric, compiledIgnoreCase), HandlePostProcessingStep },

            { new Regex(RegexPatterns.DeleteingOriginalFile, compiledIgnoreCase), HandlePostProcessingStep },

            { new Regex(RegexPatterns.UnknownError, compiledIgnoreCase), HandleUnknownError },
            { new Regex(RegexPatterns.SpecificError, compiledIgnoreCase), HandleSpecificError },

            // Optional: add playlist awareness
            { new Regex(RegexPatterns.PlaylistItem, compiledIgnoreCase), m =>
                LogAndNotify(LogType.Information, $"Playlist progress: item {m.Groups["item"].Value}/{m.Groups["total"].Value} - {m.Groups["playlist"].Value}") },
           
        };
    }

    /// <summary>
    /// Parses a single line of yt-dlp output and dispatches it to the
    /// appropriate registered handler if a known pattern is matched.
    /// </summary>
    /// <param name="output">Raw output line from the yt-dlp process.</param>
    public void ParseProgress(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return;

        foreach (var (regex, handler) in _regexHandlers)
        {
            var match = regex.Match(output);
            if (match.Success)
            {
                handler(match);
                return;
            }
        }

        HandleUnknownOutput(output);
    }

    /// <summary>
    /// Resets the internal state of the progress parser.
    /// </summary>
    /// <remarks>
    /// Clears all tracking flags related to download and post-processing stages.
    /// This does not clear registered regex handlers.
    /// </remarks>
    public void Reset()
    {
        _isDownloadCompleted = false;
        _postProcessingStarted = false;
        _postProcessStepCount = 0;
        _deleteCount = 0;
        _logger.Log(LogType.Information, "Progress parser state reset.");
    }

    // ───────────── Event Handlers (existing + improved) ─────────────
    #region Event Handlers

    private void HandleDownloadDestination(Match match)
    {
        string path = match.Groups["path"].Value;
        LogAndNotify(LogType.Information, $"Download destination: {path}");
    }

    private void HandleResumeDownload(Match match)
    {
        string bytePosition = match.Groups["byte"].Value;
        var message = $"Resuming download at byte {bytePosition}";
        LogAndNotify(LogType.Information, message);
        ProgressDownload?.Invoke(this, new DownloadProgressEventArgs { Message = message });
    }

    private void HandleDownloadProgress(Match match)
    {
        string percentString = match.Groups["percent"].Value;
        string sizeString = match.Groups["total"].Value;
        string speedString = match.Groups["speed"].Value;
        string etaString = match.Groups["eta"].Value;

        var isParsed = double.TryParse(percentString.Replace("%", ""), out double percent);

        if (!isParsed) percent = 0;
        if (!isParsed || percent >= 100) return;

        var args = new DownloadProgressEventArgs
        {
            Percent = percent,
            Size = sizeString,
            Speed = speedString,
            ETA = etaString,
            Message = $"Progress: {percent:F2}% | {sizeString} | {speedString} | ETA {etaString}"
        };

        LogAndNotify(LogType.Information, args.Message);
        ProgressDownload?.Invoke(this, args);

        if (percent >= 99.0 && !_isDownloadCompleted)
        {
            HandleDownloadProgressComplete(match);
        }
    }

    private void HandleDownloadProgressWithFrag(Match match)
    {
        string percentString = match.Groups["percent"].Value;
        string sizeString = match.Groups["size"].Value;
        string speedString = match.Groups["speed"].Value;
        string etaString = match.Groups["eta"].Value;
        string fragString = match.Groups["frag"].Value;

        var isParsed = double.TryParse(percentString.Replace("%", ""), out double percent);

        if (!isParsed) percent = 0;
        if (!isParsed || percent >= 100) return;

        var args = new DownloadProgressEventArgs
        {
            Percent = percent,
            Size = sizeString,
            Speed = speedString,
            ETA = etaString,
            Fragments = fragString,
            Message = $"Progress: {percent:F2}% | {sizeString} | {speedString} | ETA {etaString} | {fragString}"
        };

        LogAndNotify(LogType.Information, args.Message);
        ProgressDownload?.Invoke(this, args);

        // Only trigger complete if really done (avoid false 100% on fragment level)
        if (percent >= 99.0 && IsFinalFragment(fragString) && !_isDownloadCompleted)
        {
            HandleDownloadProgressComplete(match);
        }
    }

    private void HandleDownloadProgressLive(Match match)
    {
        string sizeString = match.Groups["size"].Value;
        string speedString = match.Groups["speed"].Value;
        string elapsedString = match.Groups["elapsed"].Value;
        string fragString = match.Groups["frag"].Success ? match.Groups["frag"].Value : string.Empty;

        var args = new DownloadProgressEventArgs
        {
            // No reliable percent for live streams
            Percent = 0,
            Size = sizeString,          // bytes downloaded so far
            Speed = speedString,
            ETA = null,                // or leave empty – we only have elapsed
            Elapsed = elapsedString,
            Fragments = fragString,
            Message = string.IsNullOrEmpty(fragString)
                ? $"Live: {sizeString} | {speedString} | elapsed {elapsedString}"
                : $"Live: {sizeString} | {speedString} | elapsed {elapsedString} | frag {fragString}"
        };

        LogAndNotify(LogType.Information, args.Message);
        ProgressDownload?.Invoke(this, args);

        // Never mark download as completed from a live progress line
    }

    private static bool IsFinalFragment(string frag)
    {
        if (string.IsNullOrEmpty(frag) || !frag.Contains('/')) return true;

        var parts = frag.Split('/');
        if (parts.Length != 2) return false;

        return int.TryParse(parts[0], out int current) &&
               int.TryParse(parts[1], out int total) &&
               current >= total - 1;        // allow last 1-2 fragments due to concurrency
    }

    private void HandleDownloadProgressComplete(Match match)
    {
        if (_isDownloadCompleted) return;

        _isDownloadCompleted = true;

        string percent = match.Groups["percent"]?.Value ?? "100";
        string size = match.Groups["size"]?.Value ?? match.Groups["total"]?.Value ?? "unknown";

        var message = $"Download finished: {percent}% of {size}";

        LogAndNotifyComplete(message);
        _logger.Log(LogType.Information, "Download marked as completed.");
    }

    private void HandleDownloadAlreadyCompleted(Match match)
    {
        string path = match.Groups["path"].Value;
        var message = $"Download completed: {path} has already been downloaded.";
        LogAndNotify(LogType.Information, message);
        ProgressDownload?.Invoke(this, new DownloadProgressEventArgs { Message = message });
    }

    private void HandleUnknownError(Match match)
    {
        LogAndNotify(LogType.Error, $"Unknown error: {match.Value}");
    }

    private void HandleSpecificError(Match match)
    {
        string error = match.Groups["error"].Value;
        LogAndNotify(LogType.Error, $"Error: {error}");
    }

    private void HandlePostProcessingStep(Match match)
    {
        // Ensure download is marked completed
        if (!_isDownloadCompleted)
            _isDownloadCompleted = true;

        // Start post-processing phase **only once** per download
        if (!_postProcessingStarted)
        {
            _postProcessingStarted = true;
            _postProcessStepCount = 0;

            LogAndNotify(LogType.Information, "Post-processing started");
            PostProcessingStarted?.Invoke(this, "Post-processing started");
        }

        _postProcessStepCount++;

        // Extract processor and action safely
        string processor = match.Groups["processor"].Success
            ? match.Groups["processor"].Value.Trim()
            : "PostProcessor";

        string action = match.Groups["action"].Success
            ? match.Groups["action"].Value.Trim()
            : match.Value.Trim();

        var message = $"[{processor}] {action}";
        LogAndNotify(LogType.Information, $"Post-processing [{_postProcessStepCount}]: {message}");

        // Trigger completion when we hit the real last step (MoveFiles is usually the final one)
        bool isFinalStep =
            processor.Equals("MoveFiles", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("Moving file", StringComparison.OrdinalIgnoreCase) ||
            _postProcessStepCount >= 10;  // safety net for unusual cases

        if (isFinalStep)
        {
            var completeMsg = "Post-processing completed successfully";

            LogAndNotify(LogType.Information, completeMsg);
            PostProcessingCompleted?.Invoke(this, completeMsg);

            _logger.Log(LogType.Information, "OnPostProcessingComplete event triggered.");

            // Reset flags so next download starts fresh
            Reset();
        }
    }

    private void HandleUnknownOutput(string output)
    {
        var lower = output.ToLowerInvariant().Trim();

        LogType logType = lower.Contains("error") ? LogType.Error :
                          lower.Contains("warning") ? LogType.Warning :
                          lower.Contains("[debug]") ? LogType.Debug :
                          LogType.Information;

        LogAndNotify(logType, output);
    }

    #endregion

    // ───────────── Helpers (unchanged except minor polish) ─────────────
    #region helpers
    private void LogAndNotify(LogType logType, string message)
    {
        _logger.Log(logType, message);
        ProgressMessage?.Invoke(this, message);
    }

    private void LogAndNotifyComplete(string message)
    {
        _logger.Log(LogType.Information, message);
        DownloadCompleted?.Invoke(this, message);
    }
    #endregion   
}