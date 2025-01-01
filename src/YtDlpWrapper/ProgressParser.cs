using System.Text.RegularExpressions;

namespace YtDlpWrapper;

public class ProgressParser
{
    private readonly Dictionary<string, Action<Match>> _regexHandlers;
    private readonly List<Regex> _compiledRegex;
    private bool isDownloadComplted = false;

    public ProgressParser()
    {
        _regexHandlers = new Dictionary<string, Action<Match>>
        {
            { RegexPatterns.ExtractingUrl, HandleExtractingUrl },
            { RegexPatterns.DownloadingWebpage, HandleDownloadingWebpage },
            { RegexPatterns.DownloadingJson, HandleDownloadingJson },
            { RegexPatterns.DownloadingM3u8, HandleDownloadingM3u8 },
            { RegexPatterns.DownloadingManifest, HandleDownloadingManifest },
            { RegexPatterns.TotalFragments, HandleTotalFragments },
            { RegexPatterns.TestingFormat, HandleTestingFormat },
            { RegexPatterns.DownloadingFormat, HandleDownloadingFormat },
            { RegexPatterns.DownloadDestination, HandleDownloadDestination },
            { RegexPatterns.ResumeDownload, HandleResumeDownload },
            { RegexPatterns.DownloadAlreadyDownloaded,  HandleDownloadAlreadyCompleted },
            { RegexPatterns.DownloadCompleted, HandleDownloadProgressComplete },
            { RegexPatterns.DownloadProgressComplete, HandleDownloadProgressComplete },
            { RegexPatterns.DownloadProgress, HandleDownloadProgress },
            { RegexPatterns.DownloadProgressWithFrag, HandleDownloadProgressWithFrag },
            { RegexPatterns.UnknownError, HandleUnknownError }
        };

        _compiledRegex = _regexHandlers.Keys
            .Select(pattern => new Regex(pattern, options: RegexOptions.Compiled | RegexOptions.IgnoreCase))
            .ToList();
    }

    public void ParseProgress(string output)
    {
        if (string.IsNullOrEmpty(output))
            return;

        foreach (var regex in _compiledRegex)
        {
            var match = regex.Match(output);
            if (match.Success)
            {
                _regexHandlers[regex.ToString()]?.Invoke(match);
                return;
            }
        }

        // Fallback for unknown or unhandled output
        HandleUnknownOutput(output);
    }

    private void HandleExtractingUrl(Match match)
    {
        string url = match.Groups["url"].Value;
        Logger.Log(LogType.Info, $"Extracting URL: {url}");
        OnProgressMessage?.Invoke(this, $"Extracting URL: {url}");
    }

    private void HandleDownloadingWebpage(Match match)
    {
        string id = match.Groups["id"].Value;
        Logger.Log(LogType.Info, $"Downloading webpage for video ID: {id}");
        OnProgressMessage?.Invoke(this, $"Downloading webpage for video ID: {id}");
    }

    private void HandleDownloadingJson(Match match)
    {
        string id = match.Groups["id"].Value;
        Logger.Log(LogType.Info, $"Downloading player API JSON for video ID: {id}");
        OnProgressMessage?.Invoke(this, $"Downloading player API JSON for video ID: {id}");
    }

    private void HandleDownloadingM3u8(Match match)
    {
        string id = match.Groups["id"].Value;
        Logger.Log(LogType.Info, $"Downloading m3u8 information for video ID: {id}");
        OnProgressMessage?.Invoke(this, $"Downloading m3u8 information for video ID: {id}");
    }

    private void HandleTestingFormat(Match match)
    {
        string format = match.Groups["format"].Value;
        Logger.Log(LogType.Info, $"Testing format {format}");
        OnProgressMessage?.Invoke(this, $"Testing format {format}");
    }

    private void HandleDownloadingFormat(Match match)
    {
        string format = match.Groups["format"].Value;
        string id = match.Groups["id"].Value;
        Logger.Log(LogType.Info, $"Downloading format {format} for video ID: {id}");
        OnProgressMessage?.Invoke(this, $"Downloading format {format} for video ID: {id}");
    }

    private void HandleDownloadingManifest(Match match)
    {
        OnProgressMessage?.Invoke(this, $"Downloading manifest");
    }

    private void HandleTotalFragments(Match match)
    {
        var fragments = match.Groups[1].Value;
        OnProgressMessage?.Invoke(this, $"Total fragments: {fragments}");
    }

    private void HandleDownloadDestination(Match match)
    {
        string path = match.Groups["path"].Value;
        Logger.Log(LogType.Info, $"Download destination: {path}");
        OnProgressMessage?.Invoke(this, $"Download destination: {path}");
    }

    private void HandleResumeDownload(Match match)
    {
        string bytePosition = match.Groups["byte"].Value;
        Logger.Log(LogType.Info, $"Resuming download at byte {bytePosition}");
        OnProgressDownload?.Invoke(this, new DownloadProgressEventArgs
        {
            Message = $"Resuming download at byte {bytePosition}"
        });
    }

    private void HandleDownloadProgress(Match match)
    {
        string percentString = match.Groups["percent"].Value;
        string sizeString = match.Groups["size"].Value;
        string speedString = match.Groups["speed"].Value;
        string etaString = match.Groups["eta"].Value;

        // Convert percent to double
        double percent = 0;
        double.TryParse(percentString, out percent);

        OnProgressDownload?.Invoke(this, new DownloadProgressEventArgs
        {
            Percent = percent,
            Size = sizeString,
            Speed = speedString,
            ETA = etaString,
            Message = $"Downloading: {percent}% of {sizeString}, Speed: {speedString}, ETA: {etaString}"
        });
    }

    private void HandleDownloadProgressWithFrag(Match match)
    {
        string percentString = match.Groups["percent"].Value;
        string sizeString = match.Groups["size"].Value;
        string speedString = match.Groups["speed"].Value;
        string etaString = match.Groups["eta"].Value;
        string fragString = match.Groups["frag"].Value;

        // Convert percent to double
        double percent = 0;
        double.TryParse(percentString, out percent);

        if (sizeString != "~" && !isDownloadComplted && percentString == "100.00")
        {
            HandleDownloadProgressComplete(match);
        }

        // Convert frag to int
        int frag = 0;
        int.TryParse(fragString, out frag);
        Console.WriteLine(match);

        OnProgressDownload?.Invoke(this, new DownloadProgressEventArgs
        {
            Percent = percent,
            Size = sizeString,
            Speed = speedString,
            ETA = etaString,
            Fragments = fragString,
            Message = $"Downloading: {percent}% of {sizeString}, Speed: {speedString}, ETA: {etaString}, Fragments: {fragString}"
        });
    }

    private void HandleDownloadProgressComplete(Match match)
    {
        string percent = match.Groups["percent"].Value;
        string size = match.Groups["size"].Value;

        if (size != "~" && !isDownloadComplted)
        {
            isDownloadComplted = true;
            Logger.Log(LogType.Info, $"Download complete: {percent}% of {size}");
            OnCompleteDownload?.Invoke(this, $"Download completed successfully.");
        }
    }

    private void HandleDownloadAlreadyCompleted(Match match)
    {
        string path = match.Groups["path"].Value;
        Logger.Log(LogType.Info, $"Download completed: {path} has already been downloaded.");
        OnProgressDownload?.Invoke(this, new DownloadProgressEventArgs
        {
            Message = $"Download completed: {path} has already been downloaded."
        });
    }

    private void HandleUnknownError(Match match)
    {
        Logger.Log(LogType.Info, match.Value);
    }

    private void HandleUnknownOutput(string output)
    {
        if (output.Contains("ERROR", StringComparison.InvariantCultureIgnoreCase))
        {
            Logger.Log(LogType.Error, output);
        }
        else if (output.Contains("WARNING", StringComparison.InvariantCultureIgnoreCase))
        {
            Logger.Log(LogType.Warning, output);
        }
        else
        {
            Logger.Log(LogType.Info, output);
            OnProgressMessage?.Invoke(this, output);
        }
    }

    public event EventHandler<string> OnProgressMessage;
    public event EventHandler<DownloadProgressEventArgs> OnProgressDownload;
    public event EventHandler<string> OnCompleteDownload;
}
