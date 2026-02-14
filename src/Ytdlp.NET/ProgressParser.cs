using System.Text.RegularExpressions;

namespace Ytdlp.NET;

public sealed class ProgressParser
{
    private readonly Dictionary<Regex, Action<Match>> _regexHandlers;
    private readonly ILogger _logger;
    private bool _isDownloadCompleted;
    private bool _isMerging; // Track merging state

    public ProgressParser(ILogger? logger = null)
    {
        _logger = logger ?? new DefaultLogger();
        _regexHandlers = new Dictionary<Regex, Action<Match>>
    {
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
        { new Regex(RegexPatterns.DeleteingOriginalFile, RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleMergingComplete }, // New handler
        { new Regex(@"has been successfully merged", RegexOptions.Compiled | RegexOptions.IgnoreCase), HandleMergingComplete }
    };
    }

    public void ParseProgress(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return;

        OnOutputMessage?.Invoke(this, output);

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
        _logger.Log(LogType.Info, "Resetting progress parser.");
    }

    #region Event Handlers
    private void HandleExtractingUrl(Match match)
    {
        string url = match.Groups["url"].Value;
        LogAndNotify(LogType.Info, $"Extracting URL: {url}");
    }

    private void HandleDownloadingWebpage(Match match)
    {
        string id = match.Groups["id"].Value;
        string type = match.Groups["type"].Value;
        LogAndNotify(LogType.Info, $"Downloading {type} webpage for video ID: {id}");
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
        string percentString = match.Groups["percent"].Value;
        string sizeString = match.Groups["size"].Value;
        string speedString = match.Groups["speed"].Value;
        string etaString = match.Groups["eta"].Value;

        double percent = double.TryParse(percentString, out var p) ? p : 0;
        var args = new DownloadProgressEventArgs
        {
            Percent = percent,
            Size = sizeString,
            Speed = speedString,
            ETA = etaString,
            Message = $"Downloading: {percent:F2}% of {sizeString}, Speed: {speedString}, ETA: {etaString}"
        };
        LogAndNotify(LogType.Info, args.Message);
        OnProgressDownload?.Invoke(this, args);
    }

    private void HandleDownloadProgressWithFrag(Match match)
    {
        string percentString = match.Groups["percent"].Value;
        string sizeString = match.Groups["size"].Value;
        string speedString = match.Groups["speed"].Value;
        string etaString = match.Groups["eta"].Value;
        string fragString = match.Groups["frag"].Value;

        double percent = double.TryParse(percentString, out var p) ? p : 0;
        if (sizeString != "~" && percent >= 100 && !_isDownloadCompleted)
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
            Message = $"Downloading: {percent:F2}% of {sizeString}, Speed: {speedString}, ETA: {etaString}, Fragments: {fragString}"
        };
        LogAndNotify(LogType.Info, args.Message);
        OnProgressDownload?.Invoke(this, args);
    }

    private void HandleDownloadProgressComplete(Match match)
    {
        string percent = match.Groups["percent"].Value;
        string size = match.Groups["size"].Value;

        if (size != "~" && !_isDownloadCompleted)
        {
            _isDownloadCompleted = true;
            var message = $"Download complete: {percent}% of {size}";
            LogAndNotifyComplete(message);
        }
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

    private void HandleUnknownOutput(string output)
    {
        var logType = output.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ? LogType.Error :
                      output.Contains("WARNING", StringComparison.OrdinalIgnoreCase) ? LogType.Warning :
                      LogType.Info;
        LogAndNotify(logType, $"Unmatched output: {output}");
    }

    private void HandleMergingFormats(Match match)
    {
        string path = match.Groups["path"].Value;
        _isMerging = true;
        LogAndNotify(LogType.Info, $"Merging formats into: {path}");
        _logger.Log(LogType.Info, "Set _isMerging to true");
    }

    private int _deleteCount = 0;
    private void HandleMergingComplete(Match match)
    {
        if (_isMerging)
        {
            _deleteCount++;
            var message = $"Merging complete: {match.Value}";
            LogAndNotify(LogType.Info, message);
            if (_deleteCount >= 2) // Wait for both deletions
            {
                _isMerging = false;
                _deleteCount = 0;
                OnPostProcessingComplete?.Invoke(this, message);
                _logger.Log(LogType.Info, "OnPostProcessingComplete event triggered.");
            }
        }
        else
        {
            _logger.Log(LogType.Warning, $"Merging complete detected without prior merging start: {match.Value}");
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

    private void LogAndNotify(LogType logType, string message)
    {
        _logger.Log(logType, message);
        if (logType != LogType.Error)
            OnProgressMessage?.Invoke(this, message);
        else
            OnErrorMessage?.Invoke(this, message);
    }

    private void LogAndNotifyComplete(string message)
    {
        _logger.Log(LogType.Info, message);
        OnCompleteDownload?.Invoke(this, message);
    }
    #endregion

    #region Events
    public event EventHandler<string>? OnOutputMessage;
    public event EventHandler<string>? OnProgressMessage;
    public event EventHandler<string>? OnErrorMessage;
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    public event EventHandler<string>? OnCompleteDownload;
    public event EventHandler<string>? OnPostProcessingComplete; // New event
    #endregion
}