using ClipMate.Models;
using YtdlpDotNet;

namespace ClipMate.Services;

public class YtdlpService(AppLogger logger)
{
    private readonly string _ytdlpPath = Path.Combine(AppContext.BaseDirectory, "Tools", "yt-dlp.exe");
    private readonly AppLogger _logger = logger;

    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var ytdlp = new Ytdlp(_ytdlpPath, _logger);
        try
        {
            var version = await ytdlp.GetVersionAsync();
            return version ?? "Unknown";
        }
        catch (YtdlpException ex)
        {
            _logger.Log(LogType.Error, $"Error getting yt-dlp version: {ex.Message}");
            return "Error";
        }
    }

    public async Task<string> GetUpdateAsync(CancellationToken cancellationToken = default)
    {
        var ytdlp = new Ytdlp(_ytdlpPath, _logger);
        try
        {
            return await ytdlp.UpdateAsync();
        }
        catch (YtdlpException ex)
        {
            _logger.Log(LogType.Error, $"Error getting yt-dlp version: {ex.Message}");
            return "Error";
        }
    }

    public async Task<Metadata> GetMetadataAsync(string url, CancellationToken cancellationToken = default)
    {
        var ytdlp = new Ytdlp(_ytdlpPath, _logger);
        try
        {
            var metadata = await ytdlp.GetVideoMetadataJsonAsync(url, cancellationToken);
            return metadata ?? throw new YtdlpException("Failed to retrieve metadata.");
        }
        catch (YtdlpException ex)
        {
            _logger.Log(LogType.Error, $"Error getting metadata: {ex.Message}");
            throw new YtdlpException("Failed to retrieve metadata.");
        }
    }

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

        void HandleOutput(object? s, string msg)
        {
            if (cancellationToken.IsCancellationRequested) return;

            job.Message = msg.Length > 300 ? msg[..300] + "..." : msg;
        }

        void HandleProgress(object? s, DownloadProgressEventArgs e)
        {
            if (cancellationToken.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (e.Message.Contains("has already been downloaded", StringComparison.InvariantCultureIgnoreCase))
                {
                    job.Status = DownloadStatus.Completed;
                    job.IsDownloading = false;
                    job.IsCompleted = true;
                    job.FileSize = e.Size;
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
                job.FileSize = e.Size;
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

        // Working method ignore all WARNINGS
        //async void HandleError(object? s, string msg)
        //{
        //    if (cancellationToken.IsCancellationRequested)
        //        return;

        //    await MainThread.InvokeOnMainThreadAsync(() =>
        //    {
        //        // Skip all WARNING-prefixed messages
        //        if (msg.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase))
        //            return Task.CompletedTask;

        //        // Real errors
        //        job.ErrorMessage = msg;
        //        job.IsDownloading = false;
        //        job.Status = DownloadStatus.Failed;

        //        return Task.CompletedTask;
        //    });
        //}

        // Ignor selected WARNINGS
        async void HandleError(object? s, string msg)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var ignoredWarnings = new[]
            {
                "No supported JavaScript runtime",
                "web_safari client https formats have been skipped",
                "SABR streaming"
            };

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (msg.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase) &&
                    ignoredWarnings.Any(w => msg.Contains(w, StringComparison.OrdinalIgnoreCase)))
                {
                    // Ignore only these known warnings
                    return Task.CompletedTask;
                }

                // Real errors
                job.ErrorMessage = msg;
                job.IsDownloading = false;
                job.Status = DownloadStatus.Failed;

                return Task.CompletedTask;
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

            var isAudio = job.MediaFormat?.IsAudio ?? false;

            // If audio, just use formatId or "b" for best
            // If video, merge with best audio
            string format = isAudio
                ? (job.FormatId ?? "b")
                : (job.FormatId != null ? $"{job.FormatId}+bestaudio" : "best");

            await ytdlp
                .SetOutputFolder(job.OutputPath)
                .SetFormat(format) // job.FormatId ?? "b"
                .AddCustomCommand("--restrict-filenames")
                .AddCustomCommand("--remote-components ejs:npm")
                .SetOutputTemplate(AppSettings.OutputTemplate)
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
            var userMessage = GetFriendlyErrorMessage(ex.Message);
                
            _logger.Log(LogType.Error, ex.Message);
            if (ex.Message.Contains("warning", StringComparison.InvariantCultureIgnoreCase))
            {
                job.ErrorMessage = userMessage;
                await Task.Delay(3000);
                job.IsCompleted = true;
                job.IsDownloading = false;
                job.Progress = 100;
                job.Eta = "n/a";
                job.Status = DownloadStatus.Completed;
            }
            else
            {
                job.ErrorMessage = userMessage;
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

    private string GetFriendlyErrorMessage(string message)
    {
        message = message.ToLowerInvariant();

        if (message.Contains("403") || message.Contains("access denied"))
            return "🔒 Access denied. This video may be private, age-restricted, or region-locked.";

        if (message.Contains("404") || message.Contains("not found"))
            return "❌ Video not found. The URL may be invalid or the video has been removed.";

        if (message.Contains("yt-dlp executable"))
            return "⚠️ yt-dlp is not installed or the path is incorrect.";

        if (message.Contains("unsupported url"))
            return "🚫 The provided URL is not supported by yt-dlp. Please check the source.";

        if (message.Contains("no video formats found"))
            return "🎞️ No downloadable video formats were found. This may be a livestream or DRM-protected.";

        if (message.Contains("ffmpeg") && message.Contains("not found"))
            return "🛠️ ffmpeg is required but not found. Please install it and ensure it's in your system PATH.";

        if (message.Contains("merge") && message.Contains("failed"))
            return "⚠️ Failed to merge audio and video. The selected formats may be incompatible.";

        if (message.Contains("ssl") && message.Contains("certificate"))
            return "🔐 SSL certificate error. Try updating Python/yt-dlp or checking your internet settings.";

        if (message.Contains("timed out") || message.Contains("timeout"))
            return "⏱️ The download timed out. Check your internet connection and try again.";

        if (message.Contains("proxy") && message.Contains("refused"))
            return "🌐 Proxy connection failed. Check your proxy settings.";

        if (message.Contains("too many requests") || message.Contains("429"))
            return "🚧 Too many requests. Please wait a while and try again later (rate-limited).";

        if (message.Contains("cookies"))
            return "🍪 This video may require authentication. Try adding cookies with `--cookies`.";

        if (message.Contains("extractor error"))
            return "❌ Failed to process this video. The site may have changed or yt-dlp needs an update.";

        if (message.Contains("postprocessing"))
            return "🧩 An error occurred during post-processing. Check ffmpeg and format settings.";

        if (message.Contains("already downloaded"))
            return "📁 This video has already been downloaded (duplicate).";

        if (message.Contains("file not found"))
            return "🗂️ File or path not found. Please check your output folder or filename template.";

        if (message.Contains("error") || message.Contains("failed"))
            return "❌ An unexpected error occurred during download.";

        // Default fallback
        return "⚠️ An error occurred. Please check logs or try again.";
    }


    // Method to convert image to thumbnail base64, but not used in the current implementation

    //private string ConvertImageToThumbnailBase64(string imagePath, int maxWidth = 150, int maxHeight = 150)
    //{
    //    using var inputStream = File.OpenRead(imagePath);
    //    using var original = SKBitmap.Decode(inputStream);

    //    if (original == null)
    //        throw new Exception("Could not load image.");

    //    // Maintain aspect ratio
    //    float widthRatio = (float)maxWidth / original.Width;
    //    float heightRatio = (float)maxHeight / original.Height;
    //    float scale = Math.Min(widthRatio, heightRatio);

    //    int newWidth = (int)(original.Width * scale);
    //    int newHeight = (int)(original.Height * scale);

    //    var resizedBitmap = new SKBitmap(newWidth, newHeight);

    //    // Use SKSamplingOptions instead of obsolete SKFilterQuality
    //    var sampling = new SKSamplingOptions(SKFilterMode.Nearest);

    //    original.ScalePixels(resizedBitmap, sampling);

    //    using var image = SKImage.FromBitmap(resizedBitmap);
    //    using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);

    //    byte[] imageBytes = encoded.ToArray();
    //    return $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
    //}


}
