using ClipMate.Models;
using System.Text.RegularExpressions;
using YtdlpDotNet;

namespace ClipMate.Services;

public class YtdlpService(AppLogger logger)
{
    private readonly string _ytdlpPath = Path.Combine(AppContext.BaseDirectory, "Tools", "yt-dlp.exe");
    private readonly AppLogger _logger = logger;

    public async Task<List<VideoFormat>> GetFormatsAsync(string url, CancellationToken cancellationToken = default)
    {
        var ytdlp = new Ytdlp(_ytdlpPath, _logger);
        var formats = await ytdlp.GetAvailableFormatsAsync(url, cancellationToken);
        return formats?.Select(f => new VideoFormat
        {
            ID = f.ID,
            Resolution = f.Resolution,
            Extension = f.Extension,
            FileSize = f.FileSize
        }).ToList() ?? new();
    }

    public async Task ExecuteDownloadAsync(DownloadJob job, CancellationToken cancellationToken)
    {
        var ytdlp = new Ytdlp(_ytdlpPath, _logger);
        string originalPath = "";

        void HandleOutput(object? s, string msg)
        {
            if (cancellationToken.IsCancellationRequested) return;

            if (msg.StartsWith("[info] Writing video thumbnail"))
            {
                var match = Regex.Match(msg, @"to:\s(.+)$");
                if (match.Success)
                {
                    originalPath = match.Groups[1].Value.Trim();
                    _logger.Log(LogType.Info, $"Thumbnail saved at: {originalPath}");
                }
                else
                {
                    job.Thumbnail = "videoimage.png";
                }
            }

            job.Message = msg;
        }

        void HandleProgress(object? s, DownloadProgressEventArgs e)
        {
            if (cancellationToken.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (File.Exists(originalPath))
                {
                    job.ThumbnailBase64 = ConvertImageToBase64(originalPath);
                    job.Thumbnail = null;
                    try { File.Delete(originalPath); } catch (Exception ex) { _logger.Log(LogType.Info, ex.Message); }
                }

                if (e.Message.Contains("has already been downloaded", StringComparison.InvariantCultureIgnoreCase))
                {
                    job.Status = DownloadStatus.Completed;
                    job.IsDownloading = false;
                    job.IsCompleted = true;
                    job.Format!.FileSize = e.Size;
                    job.ErrorMessage = $"✅ {job.Url} has already been downloaded.";
                    _logger.Log(LogType.Info, e.Message);
                    return;
                }

                job.Progress = e.Percent / 100.0;
                job.Eta = e.ETA;
                job.Speed = e.Speed;
                job.Status = DownloadStatus.Downloading;
                job.IsDownloading = true;
                job.IsCompleted = false;
                job.Format!.FileSize = e.Size;
                job.ErrorMessage = string.Empty;
            });
        }

        void HandleMessage(object? s, string msg)
        {
            if (cancellationToken.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (msg.Contains("has already been downloaded", StringComparison.InvariantCultureIgnoreCase))
                {
                    job.Status = DownloadStatus.Completed;
                    job.IsDownloading = false;
                    job.IsCompleted = true;
                    job.ErrorMessage = $"✅ {job.Url} has already been downloaded.";
                    _logger.Log(LogType.Info, msg);

                    if (File.Exists(job.Thumbnail))
                    {
                        try
                        {
                            File.Delete(job.Thumbnail);
                            job.Thumbnail = null;
                        }
                        catch (Exception ex)
                        {
                            _logger.Log(LogType.Error, $"Thumbnail delete error: {ex.Message}");
                        }
                    }

                    return;
                }

                if (msg.Contains("Merging formats", StringComparison.InvariantCultureIgnoreCase))                
                {
                    await Task.Delay(1000);
                    job.Status = DownloadStatus.Completed;
                    job.IsDownloading = false;
                    job.IsCompleted = true;
                    job.Progress = 100;
                    job.ErrorMessage = string.Empty;
                }

                if (msg.Contains("Fixing", StringComparison.InvariantCultureIgnoreCase))
                {
                    await Task.Delay(1000);
                    job.Status = DownloadStatus.Completed;
                    job.IsDownloading = false;
                    job.IsCompleted = true;
                    job.Progress = 100;
                    job.ErrorMessage = string.Empty;
                }

            });
        }

        void HandleComplete(object? s, string msg)
        {
            if (cancellationToken.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                job.Status = DownloadStatus.Completed;
                job.IsDownloading = false;
                job.IsCompleted = true;
                job.Progress = 100;
                job.ErrorMessage = string.Empty;
            });
        }

        async void HandleError(object? s, string msg)
        {
            if (cancellationToken.IsCancellationRequested) return;

            await MainThread.InvokeOnMainThreadAsync(async() =>
            {
                if (msg.Contains("warning", StringComparison.InvariantCultureIgnoreCase))
                {
                    job.ErrorMessage = msg;
                    await Task.Delay(3000);

                    job.IsCompleted = true;
                    job.Progress = 100;
                    job.ErrorMessage = string.Empty;
                    job.IsDownloading = false;
                    job.Status = DownloadStatus.Completed;
                }
                else
                {
                    job.ErrorMessage = msg;
                    job.IsDownloading = false;
                    job.Status = DownloadStatus.Failed;
                }
            });
        }       

        // Subscribe handlers
        ytdlp.OnOutputMessage += HandleOutput;
        ytdlp.OnProgressDownload += HandleProgress;
        ytdlp.OnProgressMessage += HandleMessage;
        ytdlp.OnCompleteDownload += HandleComplete;
        ytdlp.OnErrorMessage += HandleError;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ytdlp
                .SetOutputFolder(job.OutputPath)
                .SetFormat(job.Format?.Id ?? "b")
                .AddCustomCommand("--restrict-filenames")
                .SetOutputTemplate("%(upload_date)s_%(title)s_.%(ext)s")
                .DownloadThumbnails()
                .ExecuteAsync(job.Url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Info, "⛔ Download was canceled by the user.");
            job.Status = DownloadStatus.Cancelled;
            job.IsDownloading = false;
            job.ErrorMessage = "⛔ Download canceled.";
        }
        catch (YtdlpException ex)
        {
            _logger.Log(LogType.Error, ex.Message);
            if (ex.Message.Contains("warning", StringComparison.InvariantCultureIgnoreCase))
            {
                job.ErrorMessage = ex.Message;
                await Task.Delay(3000);
                job.IsCompleted = true;
                job.IsDownloading = false;
                job.Progress = 100;
                job.Status = DownloadStatus.Completed;
            }
            else
            {
                job.ErrorMessage = ex.Message;
                job.IsDownloading = false;
                job.Status = DownloadStatus.Failed;
            }
        }
        finally
        {
            // 🔐 Unsubscribe handlers to avoid memory leaks
            ytdlp.OnOutputMessage -= HandleOutput;
            ytdlp.OnProgressDownload -= HandleProgress;
            ytdlp.OnProgressMessage -= HandleMessage;
            ytdlp.OnCompleteDownload -= HandleComplete;
            ytdlp.OnErrorMessage -= HandleError;
        }
    }

    private string ConvertImageToBase64(string imagePath)
    {
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        return $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
    }
}
