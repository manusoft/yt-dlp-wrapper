using VideoDownloader.Models;
using YtdlpNET;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using Task = System.Threading.Tasks.Task;

namespace VideoDownloader.Engine;

public sealed class YtdlpEngine
{
    private readonly Ytdlp _ytdlp;

    public event Action<DownloadTask>? Progress;
    public event Action<DownloadTask>? Completed;
    public event Action<DownloadTask, string>? Error;

    public YtdlpEngine(string exePath)
    {
        _ytdlp = new Ytdlp(exePath);

        Subscribe();
    }

    private void Subscribe()
    {
        _ytdlp.OnProgressDownload += (_, e) =>
        {
            _currentTask.Progress = e.Percent;
            _currentTask.Speed = e.Speed;
            _currentTask.ETA = e.ETA;

            Progress?.Invoke(_currentTask);
        };

        _ytdlp.OnCompleteDownload += (_, _) =>
        {
            _currentTask.State = DownloadState.Completed;
            Completed?.Invoke(_currentTask);
        };

        _ytdlp.OnErrorMessage += (_, msg) =>
        {
            _currentTask.State = DownloadState.Error;
            Error?.Invoke(_currentTask, msg);
        };
    }

    private DownloadTask _currentTask = default!;

    public async Task DownloadAsync(DownloadTask task)
    {
        _currentTask = task;

        await _ytdlp
            .SetOutputFolder(task.OutputFolder)
            .SetFormat(task.Format)
            .SetOutputTemplate(task.OutputTemplate)
            .AddCustomCommand("--newline")
            .AddCustomCommand("--progress")
            .AddCustomCommand("--concurrent-fragments 8")
            .AddCustomCommand("--continue")
            .ExecuteAsync(task.Url);
    }

    public async Task<List<Format>> GetFormatsAsync(string url)
    {
        return await _ytdlp.GetFormatsDetailedAsync(url)
            ?? new List<Format>();
    }

    public async Task<string?> GetPlaylistJsonAsync(string url)
    {
        //return await _ytdlp
        //    .AddCustomCommand("--flat-playlist")
        //    .AddCustomCommand("-J")
        //    .ExecuteAsync(url);
        return "";
    }
}