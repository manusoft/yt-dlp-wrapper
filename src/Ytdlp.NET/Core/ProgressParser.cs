using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ManuHub.Ytdlp.Core;

public sealed class ProgressParser
{
    private readonly Dictionary<Regex, Action<Match>> _regexHandlers;
    private readonly ILogger _logger;
    // State tracking
    private bool _isDownloadCompleted;
    private bool _isMerging;
    private int _postProcessStepCount;
    private int _deleteCount;
    public ProgressParser(ILogger? logger = null)
    {
        _logger = logger ?? new DefaultLogger();

        //_regexHandlers = new Dictionary<Regex, Action<Match>>
        //{
        //    // 1. Extracting URL
        //    { new Regex(RegexPatterns.ExtractingUrl, RegexOptions.Compiled | RegexOptions.IgnoreCase),
        //      m =>
        //      {
        //          LogAndNotify(LogType.Info, $"Extracting URL: {m.Groups["url"].Value}");
        //          OnExtracting?.Invoke(this, m.Groups["url"].Value);  // NEW
        //      }
        //    },

        //    // 2. Downloading started (first real media line)
        //    { new Regex(@"\[(?<source>[^\]]+)\]\s*Downloading", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        //      m =>
        //      {
        //          LogAndNotify(LogType.Info, $"[{m.Groups["source"].Value}] Downloading...");
        //          OnDownloadingStarted?.Invoke(this, EventArgs.Empty);  // NEW
        //      }
        //    },

        //     { new Regex(RegexPatterns.DownloadProgress, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadProgress },
        //     { new Regex(RegexPatterns.DownloadProgressWithFrag, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadProgressWithFrag },

        //    // 3. Post-processing start (Merging, Fixup, MoveFiles, etc.)
        //    { new Regex(@"\[(?<source>Merger|FixupM3u8|MoveFiles|ffmpeg|ConvertSubs|SponsorBlock)\]\s*(Merging|Fixing|Moving|Processing)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        //      m =>
        //      {
        //          string source = m.Groups["source"].Value;
        //          string action = m.Groups[2].Value; // approximate
        //          string path = m.Groups.Count > 3 && m.Groups[3].Success ? m.Groups[3].Value : null;

        //          LogAndNotify(LogType.Info, $"[{source}] {action} started...");
        //          OnPostProcessingStarted?.Invoke(this, path ?? "");  // NEW
        //      }
        //    },

        //    // 4. Post-processing completed (reuse existing OnPostProcessingComplete)
        //    { new Regex(@"\[(?<source>Merger|FixupM3u8|MoveFiles)\]\s*(Successfully merged|Fixing completed|Moving file.*to)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        //      m =>
        //      {
        //          string source = m.Groups["source"].Value;
        //          string action = m.Groups[2].Value;
        //          string path = m.Groups.Count > 3 && m.Groups[3].Success ? m.Groups[3].Value : null;

        //          LogAndNotify(LogType.Info, $"[{source}] {action}");
        //          OnPostProcessingCompleted?.Invoke(this, path ?? "");  // NEW
        //      }
        //    },

        //    // 5. Finished (already in OnCompleteDownload)
        //};

        _regexHandlers = new Dictionary<Regex, Action<Match>>
        {
            // ───────────── Existing Handlers ─────────────
            { new Regex(RegexPatterns.ExtractingUrl, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleExtractingUrl },
            { new Regex(RegexPatterns.DownloadingWebpage, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadingWebpage },
            { new Regex(RegexPatterns.DownloadingJson, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadingJson },
            { new Regex(RegexPatterns.DownloadingTvClientConfig, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadingTvClientConfig },
            { new Regex(RegexPatterns.DownloadingM3u8, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadingM3u8 },
            { new Regex(RegexPatterns.DownloadingManifest, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadingManifest },
            { new Regex(RegexPatterns.TotalFragments, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleTotalFragments },
            { new Regex(RegexPatterns.TestingFormat, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleTestingFormat },
            { new Regex(RegexPatterns.DownloadingFormat, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadingFormat },
            { new Regex(RegexPatterns.DownloadingThumbnail, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadingThumbnail },
            { new Regex(RegexPatterns.WritingThumbnail, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleWritingThumbnail },
            { new Regex(RegexPatterns.DownloadDestination, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadDestination },
            { new Regex(RegexPatterns.ResumeDownload, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleResumeDownload },
            { new Regex(RegexPatterns.DownloadAlreadyDownloaded, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadAlreadyCompleted },
            { new Regex(RegexPatterns.DownloadProgressComplete, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadProgressComplete },
            { new Regex(RegexPatterns.DownloadProgressWithFrag, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadProgressWithFrag },
            { new Regex(RegexPatterns.DownloadProgress, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadProgress },
            { new Regex(RegexPatterns.UnknownError, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleUnknownError },
            { new Regex(RegexPatterns.MergingFormats, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleMergingFormats },
            { new Regex(RegexPatterns.ExtractingMetadata, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleExtractingMetadata },
            { new Regex(RegexPatterns.SpecificError, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleSpecificError },
            { new Regex(RegexPatterns.DownloadingSubtitles, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleDownloadingSubtitles },
            { new Regex(RegexPatterns.DeleteingOriginalFile, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandlePostProcessingStep },
            { new Regex(RegexPatterns.MergerSuccess, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandlePostProcessingStep },
            { new Regex(@"\[Merger\] Merging formats? into ""(?<path>[^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleMergingFormats },
            { new Regex(RegexPatterns.SponsorBlockAction, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleSponsorBlock },
            { new Regex(RegexPatterns.ConcurrentFragmentRange, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleConcurrentFragment },
            { new Regex(RegexPatterns.ConvertSubs, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleSubtitleConversion },
            { new Regex(RegexPatterns.FFmpegAction, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleFFmpegPostProcess },
            { new Regex(RegexPatterns.PostProcessGeneric, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleGenericPostProcess },
            { new Regex(RegexPatterns.PlaylistItem, RegexOptions.Compiled | RegexOptions.IgnoreCase), m =>
                LogAndNotify(LogType.Info, $"Playlist progress: item {m.Groups["item"].Value}/{m.Groups["total"].Value} - {m.Groups["playlist"].Value}") },

            // YouTube / extractor lines
            { new Regex(RegexPatterns.YoutubeDownloadingWebpage, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Info, $"[youtube] Downloading webpage for {m.Groups["id"].Value}") },

            { new Regex(RegexPatterns.YoutubeDownloadingPlayer, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Info, $"[youtube] Downloading player {m.Groups["player"].Value} for {m.Groups["id"].Value}") },

            { new Regex(RegexPatterns.YoutubeJsChallenge, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Info, $"[youtube] Solving JS challenges using {m.Groups["engine"].Value}") },

            { new Regex(RegexPatterns.YoutubeInfoFormat, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Info, $"[info] Downloading format(s): {m.Groups["formats"].Value} for {m.Groups["id"].Value}") },

            // HLS / DASH / manifest
            { new Regex(RegexPatterns.HlsnativeDownloadingManifest, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Info, "[hlsnative] Downloading m3u8 manifest") },

            { new Regex(RegexPatterns.HlsnativeDownloadingSegments, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Info, $"[hlsnative] Downloading {m.Groups["count"].Value} segments") },

            { new Regex(RegexPatterns.DashDownloadingFragments, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Info, $"[dashsegments] Downloading {m.Groups["count"].Value} fragments") },

            // Post-processing
            { new Regex(RegexPatterns.PostprocessorFfmpeg, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Info, $"[ffmpeg] {m.Groups["action"].Value}") },

            { new Regex(RegexPatterns.MergerDeletingOriginal, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Info, $"[Merger] Deleting original file {m.Groups["path"].Value}") },

            { new Regex(RegexPatterns.VideoRemuxing, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Info, $"[VideoRemuxer] Remuxing from {m.Groups["from"].Value} to {m.Groups["to"].Value}") },

            // Warnings & errors
            { new Regex(RegexPatterns.WarningUnavailable, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Warning, "Video is unavailable") },

            { new Regex(RegexPatterns.ErrorSignatureExtraction, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Error, "Signature extraction failed") },

            // Playlist
            { new Regex(RegexPatterns.PlaylistDownload, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Info, $"Downloading playlist: {m.Groups["title"].Value} - {m.Groups["count"].Value} videos") },

            { new Regex(RegexPatterns.PlaylistItemProgress, RegexOptions.Compiled | RegexOptions.IgnoreCase),
              m => LogAndNotify(LogType.Info, $"Downloading playlist item {m.Groups["current"].Value}/{m.Groups["total"].Value}") },

            // Generic catch-all for lines with [source] prefix
            {
                new Regex(RegexPatterns.GenericSourceLine, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                m =>
                {
                    string source = m.Groups["source"].Value;
                    string content = m.Groups["content"].Value.Trim();

                    // Avoid duplicating progress lines (already handled by specific progress handlers)
                    if (content.StartsWith("100%") || content.Contains("% of")) return;

                    LogAndNotify(LogType.Info, $"[{source}] {content}");
                }
            },

            // Specific overrides (more detailed logging)
            {
                new Regex(RegexPatterns.GenericDownloadingWebpage, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                m => LogAndNotify(LogType.Info, $"[{m.Groups["source"].Value}] Downloading webpage for {m.Groups["id"].Value}")
            },

            {
                new Regex(RegexPatterns.GenericDownloadingPlayer, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                m => LogAndNotify(LogType.Info, $"[{m.Groups["source"].Value}] Downloading player for {m.Groups["id"].Value}")
            },

            {
                new Regex(RegexPatterns.GenericJsChallenge, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                m => LogAndNotify(LogType.Info, $"[{m.Groups["source"].Value}] Solving JS challenges using {m.Groups["engine"].Value}")
            },

            {
                new Regex(RegexPatterns.GenericDownloadingManifest, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                m => LogAndNotify(LogType.Info, $"[{m.Groups["source"].Value}] Downloading m3u8 manifest")
            },

            {
                new Regex(RegexPatterns.GenericPostProcessing, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                m => LogAndNotify(LogType.Info, $"[{m.Groups["source"].Value}] {m.Groups["action"].Value}")
            },
        };
    }

    public void ParseProgress(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return;
        OnOutputMessage?.Invoke(this, output.TrimEnd());
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

    public void Reset()
    {
        _isDownloadCompleted = false;
        _isMerging = false;
        _postProcessStepCount = 0;
        _deleteCount = 0;
        _logger.Log(LogType.Info, "Progress parser state reset.");
    }

    // ───────────── Event Handlers (existing + improved) ─────────────
    #region Event Handlers
    private void HandleExtractingUrl(Match match) => LogAndNotify(LogType.Info, $"Extracting URL: {match.Groups["url"].Value}");

    private void HandleDownloadingWebpage(Match match)
    {
        var id = match.Groups["id"].Value;
        var type = match.Groups["type"].Value;
        LogAndNotify(LogType.Info, $"Downloading {type} webpage for {id}");
    }

    private void HandleDownloadingJson(Match match)
    {
        string id = match.Groups["id"].Value;
        string type = match.Groups["type"].Value;
        LogAndNotify(LogType.Info, $"Downloading {type} player API JSON for video ID: {id}");
    }

    private void HandleDownloadingTvClientConfig(Match match)
    {
        string id = match.Groups["id"].Value;
        LogAndNotify(LogType.Info, $"Downloading tv client config for video ID: {id}");
    }

    private void HandleDownloadingM3u8(Match match)
    {
        string id = match.Groups["id"].Value;
        LogAndNotify(LogType.Info, $"Downloading m3u8 information for video ID: {id}");
    }

    private void HandleDownloadingManifest(Match match)
    {
        LogAndNotify(LogType.Info, "Downloading manifest");
    }

    private void HandleTotalFragments(Match match)
    {
        string fragments = match.Groups["fragments"].Value;
        LogAndNotify(LogType.Info, $"Total fragments: {fragments}");
    }
    private void HandleTestingFormat(Match match)
    {
        string format = match.Groups["format"].Value;
        LogAndNotify(LogType.Info, $"Testing format {format}");
    }

    private void HandleDownloadingFormat(Match match)
    {
        string format = match.Groups["format"].Value;
        string id = match.Groups["id"].Value;
        LogAndNotify(LogType.Info, $"Downloading format {format} for video ID: {id}");
    }

    private void HandleDownloadingThumbnail(Match match)
    {
        string number = match.Groups["number"].Value;
        LogAndNotify(LogType.Info, $"Downloading video thumbnail {number}");
    }

    private void HandleWritingThumbnail(Match match)
    {
        string number = match.Groups["number"].Value;
        string path = match.Groups["path"].Value;
        LogAndNotify(LogType.Info, $"Writing video thumbnail {number} to: {path}");
    }

    private void HandleDownloadDestination(Match match)
    {
        string path = match.Groups["path"].Value;
        LogAndNotify(LogType.Info, $"Download destination: {path}");
    }

    private void HandleResumeDownload(Match match)
    {
        string bytePosition = match.Groups["byte"].Value;
        var message = $"Resuming download at byte {bytePosition}";
        LogAndNotify(LogType.Info, message);
        OnProgressDownload?.Invoke(this, new DownloadProgressEventArgs { Message = message });
    }

    private void HandleDownloadProgress(Match match)
    {
        // Existing logic unchanged
        string percentString = match.Groups["percent"].Value;
        string sizeString = match.Groups["size"].Value;
        string speedString = match.Groups["speed"].Value;
        string etaString = match.Groups["eta"].Value;
        if (!double.TryParse(percentString.Replace("%", ""), out double percent)) percent = 0;
        var args = new DownloadProgressEventArgs
        {
            Percent = percent,
            Size = sizeString,
            Speed = speedString,
            ETA = etaString,
            Message = $"Progress: {percent:F2}% | {sizeString} | {speedString} | ETA {etaString}"
        };
        LogAndNotify(LogType.Info, args.Message);
        OnProgressDownload?.Invoke(this, args);
    }

    private void HandleDownloadProgressWithFrag(Match match)
    {
        // Existing + prevent premature complete if fragments remain
        string percentString = match.Groups["percent"].Value;
        string sizeString = match.Groups["size"].Value;
        string speedString = match.Groups["speed"].Value;
        string etaString = match.Groups["eta"].Value;
        string fragString = match.Groups["frag"].Value;
        if (!double.TryParse(percentString.Replace("%", ""), out double percent)) percent = 0;
        // Only trigger complete if really done (avoid false 100% on fragment level)
        if (sizeString != "~" && percent >= 99.9 && !_isDownloadCompleted && !fragString.Contains("/"))
        {
            HandleDownloadProgressComplete(match);
            return;
        }
        var args = new DownloadProgressEventArgs
        {
            Percent = percent,
            Size = sizeString,
            Speed = speedString,
            ETA = etaString,
            Fragments = fragString,
            Message = $"Progress: {percent:F2}% | {sizeString} | {speedString} | ETA {etaString} | {fragString}"
        };
        LogAndNotify(LogType.Info, args.Message);
        OnProgressDownload?.Invoke(this, args);
    }

    private void HandleDownloadProgressComplete(Match match)
    {
        if (_isDownloadCompleted) return;
        _isDownloadCompleted = true;
        var message = $"Download finished: {match.Groups["percent"].Value} of {match.Groups["size"].Value}";
        LogAndNotifyComplete(message);
    }

    private void HandleDownloadAlreadyCompleted(Match match)
    {
        string path = match.Groups["path"].Value;
        var message = $"Download completed: {path} has already been downloaded.";
        LogAndNotify(LogType.Info, message);
        OnProgressDownload?.Invoke(this, new DownloadProgressEventArgs { Message = message });
    }

    private void HandleUnknownError(Match match)
    {
        LogAndNotify(LogType.Error, $"Unknown error: {match.Value}");
    }

    private void HandleMergingFormats(Match match)
    {
        var path = match.Groups["path"].Value;
        _isMerging = true;
        _postProcessStepCount = 0;
        _deleteCount = 0;
        LogAndNotify(LogType.Info, $"Starting merge → {path}");
    }

    private void HandleMergingComplete(Match match)
    {
        if (_isMerging)
        {
            _postProcessStepCount++;
            var message = $"Post-processing step: {match.Value}";
            LogAndNotify(LogType.Info, message);
            // More flexible trigger: complete after 2+ steps OR explicit "merged" phrase
            if (_postProcessStepCount >= 2 || match.Value.Contains("successfully merged", StringComparison.OrdinalIgnoreCase))
            {
                _isMerging = false;
                _postProcessStepCount = 0;
                OnPostProcessingCompleted?.Invoke(this, message);
                _logger.Log(LogType.Info, "OnPostProcessingComplete event reliably triggered.");
            }
        }
        else
        {
            // Fallback: treat as post-processing even if no merge start detected
            LogAndNotify(LogType.Info, $"Post-processing detected: {match.Value}");
            OnPostProcessingCompleted?.Invoke(this, match.Value);
        }
    }

    private void HandleExtractingMetadata(Match match)
    {
        string id = match.Groups["id"].Value;
        LogAndNotify(LogType.Info, $"Extracting metadata for video ID: {id}");
    }

    private void HandleSpecificError(Match match)
    {
        string error = match.Groups["error"].Value;
        LogAndNotify(LogType.Error, $"Error: {error}");
    }

    private void HandleDownloadingSubtitles(Match match)
    {
        string language = match.Groups["language"].Value;
        LogAndNotify(LogType.Info, $"Downloading subtitles for language: {language}");
    }

    // ───────────── v2.0 Enhanced Post-Processing Detection ─────────────
    private void HandlePostProcessingStep(Match match)
    {
        if (!_isMerging && _postProcessStepCount == 0)
        {
            // Single-format or no-merge case — still fire event eventually
            LogAndNotify(LogType.Info, "Post-processing detected (no prior merge)");
        }
        _postProcessStepCount++;
        _deleteCount++; // still count deletions as steps
        var line = match.Value.Trim();
        var message = $"Post-processing [{_postProcessStepCount}]: {line}";
        LogAndNotify(LogType.Info, message);
        // Trigger completion if:
        // - At least 2 steps completed, OR
        // - Explicit success phrase found, OR
        // - Multiple deletions happened
        if (_postProcessStepCount >= 2 ||
            line.Contains("successfully merged", StringComparison.OrdinalIgnoreCase) ||
            _deleteCount >= 2)
        {
            _isMerging = false;
            _postProcessStepCount = 0;
            _deleteCount = 0;
            OnPostProcessingCompleted?.Invoke(this, message);
            _logger.Log(LogType.Info, "Reliably triggered OnPostProcessingComplete");
        }
    }


    private void HandleGenericPostProcess(Match match)
    {
        var action = match.Groups["action"].Value.Trim();
        LogAndNotify(LogType.Info, $"Post-process: {action}");
        _postProcessStepCount++;
    }

    // ───────────── New Handlers for modern features ─────────────
    private void HandleSponsorBlock(Match match)
    {
        var action = match.Groups["action"].Value.Trim();
        var details = match.Groups["details"].Success ? match.Groups["details"].Value.Trim() : "";
        LogAndNotify(LogType.Info, $"SponsorBlock {action}{(string.IsNullOrEmpty(details) ? "" : $": {details}")}");
    }

    private void HandleConcurrentFragment(Match match)
    {
        var frag = match.Groups["frag"].Value;
        LogAndNotify(LogType.Info, $"Concurrent fragment processing: {frag}");
    }

    private void HandleSubtitleConversion(Match match)
    {
        var file = match.Groups["file"].Value.Trim();
        var format = match.Groups["format"].Value.Trim();
        LogAndNotify(LogType.Info, $"Converting subtitle → {file} to {format}");
    }

    private void HandleFFmpegPostProcess(Match match)
    {
        var action = match.Groups["action"].Value.Trim();
        LogAndNotify(LogType.Info, $"FFmpeg: {action}");
    }

    private void HandleUnknownOutput(string output)
    {
        Debug.WriteLine("UNKNOWN DEBUG: [" + output + "]");
        var trimmed = output.Trim();
        var lower = trimmed.ToLowerInvariant();

        LogType logType = lower.Contains("error") ? LogType.Error :
                          lower.Contains("warning") ? LogType.Warning :
                          lower.Contains("[debug]") ? LogType.Debug :
                          LogType.Info;

        // Detect if line already has a [category] prefix
        bool hasYtPrefix = trimmed.StartsWith("[") && trimmed.Contains("]");

        string category = hasYtPrefix
            ? ""  // ← don't add extra prefix if yt-dlp already has one
            : lower.Contains("[ffmpeg]") ? "FFmpeg" :
              lower.Contains("[extractor]") ? "Extractor" :
              lower.Contains("[sponsorblock]") ? "SponsorBlock" :
              lower.Contains("[download]") ? "Download" : "Unknown";

        var message = string.IsNullOrEmpty(category)
            ? trimmed
            : $"[{category}] {trimmed}";

        LogAndNotify(logType, message);
    }

    #endregion

    // ───────────── Helpers (unchanged except minor polish) ─────────────
    #region helpers

    private void LogAndNotify(LogType logType, string message)
    {
        _logger.Log(logType, message);
        if (logType == LogType.Error)
            OnErrorMessage?.Invoke(this, message);
        else
            OnProgressMessage?.Invoke(this, message);
    }

    private void LogAndNotifyComplete(string message)
    {
        _logger.Log(LogType.Info, message);
        OnCompleteDownload?.Invoke(this, message);
    }

    #endregion

    // ───────────── Events (unchanged) ─────────────
    #region Events
    public event EventHandler<string>? OnOutputMessage;
    public event EventHandler<string>? OnProgressMessage;
    public event EventHandler<string>? OnErrorMessage;

    public event EventHandler<string>? OnExtracting;
    public event EventHandler? OnDownloadingStarted;
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    public event EventHandler<string>? OnCompleteDownload;
    public event EventHandler<string>? OnPostProcessingStarted;
    public event EventHandler<string>? OnPostProcessingCompleted;
    #endregion
}