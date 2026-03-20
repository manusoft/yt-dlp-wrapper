using System.Text.RegularExpressions;

namespace ManuHub.Ytdlp.Core;

public sealed class ProgressParser
{
    private readonly Dictionary<Regex, Action<Match>> _regexHandlers;
    private readonly ILogger _logger;

    private bool _isDownloadCompleted;
    private bool _isMerging;
    private int _postProcessStepCount;

    public ProgressParser(ILogger? logger = null)
    {
        _logger = logger ?? new DefaultLogger();

        _regexHandlers = new Dictionary<Regex, Action<Match>>
        {
            // ───────────── Download Progress ─────────────
            { new Regex(RegexPatterns.DownloadProgress, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadProgress },
            { new Regex(RegexPatterns.DownloadProgressWithFrag, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadProgress },
            { new Regex(RegexPatterns.DownloadProgressComplete, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadComplete },

            // ───────────── Post-Processing ─────────────
            { new Regex(RegexPatterns.MergerStart, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleMergingStart },
            { new Regex(RegexPatterns.MergerSuccess, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleMergingComplete },
            { new Regex(RegexPatterns.FFmpegAction, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleFFmpeg },

            // ───────────── FixupM3u8 + MoveFiles Post-Processing ─────────────
            { new Regex(@"\[(?<source>FixupM3u8|MoveFiles)\]\s*(?<action>.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m =>
              {
                  string source = m.Groups["source"].Value;
                  string action = m.Groups["action"].Value;

                  _logger.Log(LogType.Info, $"Post-processing detected: [{source}] {action}");

                  // Fire started/completed events immediately
                  OnPostProcessingStarted?.Invoke(this, $"{source}: {action}");
                  OnPostProcessingCompleted?.Invoke(this, $"{source}: {action}");
              }
            },

            // ───────────── Errors & Warnings ─────────────
            { new Regex(RegexPatterns.WarningLine, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                m => LogAndNotify(LogType.Warning, m.Groups["message"].Value) },
            { new Regex(RegexPatterns.ErrorLine, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                m => LogAndNotify(LogType.Error, m.Groups["message"].Value) },

            // ───────────── YouTube / Extractor Info ─────────────
            { new Regex(RegexPatterns.YoutubeDownloadingWebpage, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                m => LogAndNotify(LogType.Info, $"[youtube] Downloading webpage for {m.Groups["id"].Value}") },
            { new Regex(RegexPatterns.YoutubeDownloadingPlayer, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                m => LogAndNotify(LogType.Info, $"[youtube] Downloading player {m.Groups["player"].Value} for {m.Groups["id"].Value}") },
            { new Regex(RegexPatterns.InfoFormat, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                m => LogAndNotify(LogType.Info, $"Downloading format(s): {m.Groups["formats"].Value}") },

            // ───────────── Generic fallback ─────────────
            { new Regex(RegexPatterns.GenericLine, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                m => LogAndNotify(LogType.Info, $"[{m.Groups["source"].Value}] {m.Groups["content"].Value}") }
        };
    }

    public void ParseProgress(string? output)
    {
        if (string.IsNullOrWhiteSpace(output)) return;

        foreach (var (regex, handler) in _regexHandlers)
        {
            var match = regex.Match(output);
            if (match.Success)
            {
                handler(match);
                return;
            }
        }

        // Unknown lines fallback
        LogAndNotify(LogType.Debug, output.Trim());
    }

    public void Reset()
    {
        _isDownloadCompleted = false;
        _isMerging = false;
        _postProcessStepCount = 0;
    }

    #region Event Handlers

    private void HandleDownloadProgress(Match match)
    {
        if (!double.TryParse(match.Groups["percent"].Value.Replace("%", ""), out var percent))
            percent = 0;

        var message = $"Progress: {percent:F2}% | {match.Groups["size"].Value} | {match.Groups["speed"].Value} | ETA {match.Groups["eta"].Value} | {match.Groups["frag"].Value}";
        LogAndNotify(LogType.Info, message);
        OnProgressDownload?.Invoke(this, new DownloadProgressEventArgs
        {
            Percent = percent,
            Size = match.Groups["size"].Value,
            Speed = match.Groups["speed"].Value,
            ETA = match.Groups["eta"].Value,
            Fragments = match.Groups["frag"].Value,
            Message = message
        });
    }

    private void HandleDownloadComplete(Match match)
    {
        if (_isDownloadCompleted) return;
        _isDownloadCompleted = true;

        var message = $"Download finished: {match.Groups["size"].Value} in {match.Groups["time"].Value}";
        LogAndNotifyComplete(message);
    }

    private void HandleMergingStart(Match match)
    {
        _isMerging = true;
        _postProcessStepCount = 0;
        LogAndNotify(LogType.Info, $"Merging formats into: {match.Groups["path"].Value}");
    }

    private void HandleMergingComplete(Match match)
    {
        if (_isMerging)
        {
            _postProcessStepCount++;
            if (_postProcessStepCount >= 1)
            {
                _isMerging = false;
                _postProcessStepCount = 0;
                OnPostProcessingCompleted?.Invoke(this, "Merge completed successfully");
            }
        }
    }

    private void HandleFFmpeg(Match match)
    {
        _postProcessStepCount++;
        LogAndNotify(LogType.Info, $"[ffmpeg] {match.Groups["action"].Value}");
    }
    #endregion

    #region Helpers
    private void LogAndNotify(LogType logType, string message)
    {
        _logger.Log(logType, message);
        if (logType == LogType.Error) OnErrorMessage?.Invoke(this, message);
        else OnProgressMessage?.Invoke(this, message);
    }

    private void LogAndNotifyComplete(string message)
    {
        _logger.Log(LogType.Info, message);
        OnCompleteDownload?.Invoke(this, message);
    }
    #endregion

    #region Events
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    public event EventHandler<string>? OnProgressMessage;
    public event EventHandler<string>? OnErrorMessage;
    public event EventHandler<string>? OnPostProcessingStarted;
    public event EventHandler<string>? OnPostProcessingCompleted;
    public event EventHandler<string>? OnCompleteDownload;
    #endregion
}