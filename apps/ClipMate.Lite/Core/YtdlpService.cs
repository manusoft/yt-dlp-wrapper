using ManuHub.Ytdlp;
using ManuHub.Ytdlp.Models;

namespace VideoDownloader.Core;

public sealed class YtdlpService
{
    private readonly YtdlpRootBuilder _root;
    private readonly ILogger _logger;
    private readonly string _path;

    public event Action<DownloadProgressEventArgs>? Progress;
    public event Action<string>? ProgressMessage;
    public event Action<string>? ErrorMessage;
    public event Action? DownloadCompleted;
    public event Action? PostProcessStarted;
    public event Action? PostProcessCompleted;
    public event Action? ProcessCompleted;

    public YtdlpService(string path, ILogger logger)
    {
        _path = path;
        _logger = logger;
        _root = Ytdlp.Create(path, logger);
    }

    public async Task<string> GetVersionAsync()
        => await _root.General().VersionAsync() ?? "unknown";

    public async Task<List<Format>> GetFormatsAsync(string url)
        => await _root.Probe(url).GetFormatsDetailedAsync() ?? [];

    public async Task DownloadAsync(
        string url,
        string format,
        string outputFolder,
        string ffmpeg,
        string outputTemplate)
    {
        try
        {
            var command = _root.Download()
               .WithFormat(format)
               .WithConcurrentFragments(8)
               .WithOutputFolder(outputFolder)
               .WithFFmpegLocation(ffmpeg)
               .WithOutputTemplate(outputTemplate)
               .WindowsFileNames()
               .Build();

            Subscribe(command);

            await command.ExecuteAsync(url);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void Subscribe(YtdlpCommand command)
    {
        command.OnProgressMessage += (s, e) => ProgressMessage?.Invoke(e);

        command.OnErrorMessage += (s, e) => ErrorMessage?.Invoke(e);

        command.OnProgressDownload += (s, e) => Progress?.Invoke(e);

        command.OnPostProcessingStarted += (s, e) => PostProcessStarted?.Invoke();

        command.OnPostProcessingCompleted += (s, e) =>  PostProcessCompleted?.Invoke();

        command.OnCompleteDownload += (s, e) => DownloadCompleted?.Invoke();

        command.OnProcessCompleted += (s, e) =>  ProcessCompleted?.Invoke();
    }
}