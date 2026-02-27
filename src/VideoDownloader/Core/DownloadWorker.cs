using VideoDownloader.Engine;
using VideoDownloader.Models;

namespace VideoDownloader.Core;

public sealed class DownloadWorker
{
    private readonly YtdlpEngine _engine;

    public DownloadWorker(YtdlpEngine engine)
    {
        _engine = engine;
    }

    public async Task RunAsync(DownloadTask task)
    {
        task.State = DownloadState.Running;
        await _engine.DownloadAsync(task);
    }
}