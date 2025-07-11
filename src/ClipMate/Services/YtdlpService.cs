using ClipMate.Models;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Text.RegularExpressions;
using YtdlpDotNet;

namespace ClipMate.Services;

public class YtdlpService
{
    private readonly string _ytdlpPath = Path.Combine(AppContext.BaseDirectory, "Tools", "yt-dlp.exe");
    private readonly ILogger _logger;

    public YtdlpService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<List<VideoFormat>> GetFormatsAsync(string url)
    {
        var ytdlp = new Ytdlp(_ytdlpPath, _logger);
        var formats = await ytdlp.GetAvailableFormatsAsync(url);
        return formats?.Select(f => new VideoFormat
        {
            ID = f.ID,
            Resolution = f.Resolution,
            Extension = f.Extension,
            FileSize = f.FileSize
        }).ToList() ?? new();
    }

    public async Task ExecuteDownloadAsync(DownloadJob job)
    {
        var ytdlp = new Ytdlp(_ytdlpPath, _logger);
        string originalPath = "";

        ytdlp.OnOutputMessage += (s, msg) =>
        {
            if (msg.StartsWith("[info] Writing video thumbnail"))
            {
                // Extract path from the message
                var match = Regex.Match(msg, @"to:\s(.+)$");
                if (match.Success)
                {
                    originalPath = match.Groups[1].Value.Trim();
                    _logger.Log(LogType.Info, $"Thumbnail saved at: {originalPath}");
                }
                else
                {
                    job.Thumbnail = "dotnet_bot.png"; // Fallback
                }
            }
        };

        ytdlp.OnProgressDownload += (s, e) =>
        {
            if (double.TryParse(e.Percent.ToString(), out var percent))
                job.Progress = percent / 100.0;

            job.ETA = e.ETA;
            job.Speed = e.Speed;           
            job.Status = DownloadStatus.Downloading;

            if (File.Exists(originalPath))
            {
                job.Thumbnail = originalPath;
                job.ErrorMessage = "exist!";
            }
            else
            {
                job.Thumbnail = "dotnet_bot.png"; // Fallback
            }
        };

        ytdlp.OnProgressMessage += (s, msg) =>
        {
            if (msg.Contains("Merging formats"))
                job.Status = DownloadStatus.Merging;
        };

        ytdlp.OnCompleteDownload += async (s, msg) =>
        {
            job.Status = DownloadStatus.Completed;
            job.IsCompleted = true;
            job.Progress = 100;
            await ShowToastAsync($"✅ Download finished successfully for:{job.Url}");
        };

        ytdlp.OnErrorMessage += (s, msg) =>
        {
            if (msg.Contains("warning", StringComparison.InvariantCultureIgnoreCase))
            {
                job.Status = DownloadStatus.Warning;
                job.ErrorMessage = msg;
            }
            else
            {
                job.ErrorMessage = msg;
                job.Status = DownloadStatus.Failed;
            }
        };

        job.Status = DownloadStatus.Downloading;

        try
        {
            await ytdlp
                .SetOutputFolder(job.OutputPath)
                .SetFormat(job.Format?.ID ?? "b")
                .AddCustomCommand("--restrict-filenames")
                .SetOutputTemplate("%(upload_date)s_%(title)s_.%(ext)s")
                .DownloadThumbnails()
                .ExecuteAsync(job.Url);
        }
        catch (YtdlpException ex)
        {
            _logger.Log(LogType.Error, ex.Message);
            if (ex.Message.Contains("warning", StringComparison.InvariantCultureIgnoreCase))
            {
                job.Status = DownloadStatus.Warning;
                job.ErrorMessage = ex.Message;
            }
            else
            {
                job.ErrorMessage = ex.Message;
                job.Status = DownloadStatus.Failed;
            }
        }
    }

    // Toast settings
    public async Task ShowToastAsync(string message)
    {
        var toast = Toast.Make(message, ToastDuration.Long, 14);
        await toast.Show(new CancellationTokenSource().Token);
    }
}
