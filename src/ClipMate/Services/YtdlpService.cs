using ClipMate.Models;
using System.Text.RegularExpressions;
using YtdlpDotNet;

namespace ClipMate.Services;

public class YtdlpService(AppLogger logger)
{
    private readonly string _ytdlpPath = Path.Combine(AppContext.BaseDirectory, "Tools", "yt-dlp.exe");
    private readonly AppLogger _logger = logger;

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
                    job.Thumbnail = "videoimage.png"; // Fallback
                }
            }
        };

        ytdlp.OnProgressDownload += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (e.Message.Contains("has already been downloaded", StringComparison.InvariantCultureIgnoreCase))
                {
                    job.Status = DownloadStatus.Completed;
                    job.IsDownloading = false;
                    job.IsCompleted = true;
                    job.ErrorMessage = $"✅ {job.Url} has already been downloaded.";

                    _logger.Log(LogType.Info, e.Message);
                    return;
                }

                job.Progress = e.Percent / 100.0;
                job.Eta = e.ETA;
                job.Speed = e.Speed;
                job.Status = DownloadStatus.Downloading;
                job.IsDownloading = true;
                job.ErrorMessage = string.Empty;

                if (File.Exists(originalPath))
                {
                    job.ThumbnailBase64 = ConvertImageToBase64(originalPath);
                    job.Thumbnail = null;

                    try
                    {
                        File.Delete(originalPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogType.Info, ex.Message);
                    }
                }
            });
        };

        ytdlp.OnProgressMessage += (s, msg) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (msg.Contains("has already been downloaded", StringComparison.InvariantCultureIgnoreCase))
                {
                    job.Status = DownloadStatus.Completed;
                    job.IsDownloading = false;
                    job.IsCompleted = true;
                    job.ErrorMessage = $"✅ {job.Url} has already been downloaded.";

                    _logger.Log(LogType.Info, msg);
                    return;
                }

                job.Status = DownloadStatus.Downloading;
                job.IsDownloading = true;
                job.ErrorMessage = string.Empty;

                if (msg.Contains("Merging formats"))
                    job.Status = DownloadStatus.Merging;
            });
        };

        ytdlp.OnCompleteDownload += (s, msg) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                job.Status = DownloadStatus.Completed;
                job.IsDownloading = false;
                job.IsCompleted = true;
                job.Progress = 100;
                job.ErrorMessage = string.Empty;
            });
        };

        ytdlp.OnErrorMessage += (s, msg) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (msg.Contains("warning", StringComparison.InvariantCultureIgnoreCase))
                {
                    job.Status = DownloadStatus.Warning;
                    job.ErrorMessage = msg;
                }
                else
                {
                    job.ErrorMessage = msg;
                    job.IsDownloading = false;
                    job.Status = DownloadStatus.Failed;
                }
            });
        };

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
                job.IsDownloading = false;
                job.Status = DownloadStatus.Failed;
            }
        }
    }

    private string ConvertImageToBase64(string imagePath)
    {
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        return $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
    }
}
