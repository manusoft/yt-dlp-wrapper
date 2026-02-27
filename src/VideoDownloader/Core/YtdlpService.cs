using YtdlpNET;

namespace VideoDownloader.Core;

public sealed class YtdlpService
{
    private readonly Ytdlp _ytdlp;
    private readonly ILogger _logger;

    public event Action<DownloadProgressEventArgs>? Progress;
    public event Action<string>? Log;
    public event Action<string>? Error;
    public event Action? Completed;
    public event Action? MergeCompleted;

    public YtdlpService(string path, ILogger logger)
    {
        _logger = logger;
        _ytdlp = new Ytdlp(path, logger);
        Subscribe();
    }

    private void Subscribe()
    {
        _ytdlp.OnOutputMessage += (s, e) => Log?.Invoke(e);
        _ytdlp.OnErrorMessage += (s, e) => Error?.Invoke(e);

        _ytdlp.OnProgressDownload += (s, e) =>
        {
            Progress?.Invoke(e);
        };

        _ytdlp.OnProgressMessage += (s, e) =>
        {
            if (e.Contains("Merging formats"))
                Log?.Invoke("Merging...");
        };

        _ytdlp.OnCompleteDownload += (s, e) =>
        {
            Completed?.Invoke();
        };

        _ytdlp.OnPostProcessingComplete += (s, e) =>
        {
            MergeCompleted?.Invoke();
        };
    }

    public async Task<string> GetVersionAsync()
        => await _ytdlp.GetVersionAsync() ?? "unknown";

    public async Task<List<Format>> GetFormatsAsync(string url)
        => await _ytdlp.GetFormatsDetailedAsync(url) ?? [];

    public async Task DownloadAsync(
        string url,
        string format,
        string outputFolder,
        string ffmpeg,
        string outputTemplate)
    {
        await _ytdlp
            .SetFormat(format)
            .SetOutputFolder(outputFolder)
            .SetFFmpegLocation(ffmpeg)
            .SetOutputTemplate(outputTemplate)
            .AddCustomCommand("--newline")
            .AddCustomCommand("--windows-filenames")
            .AddCustomCommand("--no-playlist")
            .ExecuteAsync(url);
    }
}