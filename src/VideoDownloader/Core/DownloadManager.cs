using System.Collections.Concurrent;
using VideoDownloader.Engine;
using VideoDownloader.Models;

namespace VideoDownloader.Core;

public sealed class DownloadManager
{
    private readonly ConcurrentQueue<DownloadTask> _queue = new();

    private readonly List<Task> _workers = new();

    public int ParallelDownloads { get; set; } = 3;

    private readonly YtdlpEngine _engine =
        new(@".\Tools\yt-dlp.exe");

    public event Action? Updated;

    public void Enqueue(DownloadTask task)
    {
        _queue.Enqueue(task);
        Start();
    }

    private void Start()
    {
        if (_workers.Count >= ParallelDownloads)
            return;

        _workers.Add(Task.Run(WorkerLoop));
    }

    private async Task WorkerLoop()
    {
        var worker = new DownloadWorker(_engine);

        while (_queue.TryDequeue(out var task))
        {
            await worker.RunAsync(task);
            Updated?.Invoke();
        }
    }
}