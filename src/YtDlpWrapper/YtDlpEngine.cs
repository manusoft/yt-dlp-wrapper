using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YtDlpWrapper;

public class YtDlpEngine
{
    // Event to notify progress updates
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    // Event to notify message updates
    public event EventHandler<string>? OnProgressMessage;
    // Event to notify message updates
    public event EventHandler<string>? OnErrorMessage;

    private readonly string ytDlpExecutable;
    private readonly string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
    private readonly string logPath = Path.Combine(AppContext.BaseDirectory, "Logs", $"EngineLog_{DateTime.Today.ToString("yyyy_MM_dd")}.log");

    /// <summary>
    /// Constructor to initialize the YtDlpEngine
    /// </summary>
    /// <param name="ytDlpPath">Provide the yt-dlp.exe path</param>
    /// <exception cref="FileNotFoundException"></exception>
    public YtDlpEngine(string ytDlpPath = "yt-dlp.exe")
    {
        Initialize();

        // Log initialization
        LogToFile(LogType.Info, "Initializing YtDlpEngine...");

        // Validate the path
        ytDlpExecutable = ValidateExecutablePath(ytDlpPath);

        LogToFile(LogType.Info, "Engine started successfully.");
    }

    /// <summary>
    /// Validates the yt-dlp executable path and ensures it exists.
    /// </summary>
    /// <param name="ytDlpPath">Relative or absolute path to yt-dlp executable.</param>
    /// <returns>The absolute path to the yt-dlp executable.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the executable is not found.</exception>
    private string ValidateExecutablePath(string ytDlpPath)
    {
        // Get absolute path if relative
        string absolutePath = Path.IsPathRooted(ytDlpPath)
            ? ytDlpPath
            : Path.Combine(AppContext.BaseDirectory, ytDlpPath);

        // Check if the file exists
        if (!File.Exists(absolutePath))
        {
            LogToFile(LogType.Error, $"yt-dlp executable not found at: {absolutePath}");
            throw new FileNotFoundException($"yt-dlp executable not found at: {absolutePath}");
        }

        LogToFile(LogType.Info, $"yt-dlp executable found at: {absolutePath}");
        return absolutePath;
    }

    /// <summary>
    /// Initialize the log directory
    /// </summary>
    private void Initialize()
    {
        if (!Directory.Exists(logDirectory))
        {
            try
            {
                Directory.CreateDirectory(logDirectory);
            }
            catch (Exception) { }
        }
    }

    /// <summary>
    /// Download video from the video URL
    /// </summary>
    /// <param name="videoUrl"></param>
    /// <param name="outputDirectory"></param>
    /// <param name="quality"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task DownloadVideoAsync(string videoUrl, string outputDirectory, VideoQuality quality = VideoQuality.Best)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be null or empty.", nameof(videoUrl));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        try
        {
            var format = StringExtensions.GetVideoFormatCode(quality);
            var outputPath = Path.Combine(outputDirectory, "%(title)s.%(ext)s");
            await RunCommandAsync($"-f \"{format}\" -o \"{outputPath}\" {videoUrl}");
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// Download playlist from the playlist URL
    /// </summary>
    /// <param name="playlistUrl"></param>
    /// <param name="outputDirectory"></param>
    /// <param name="quality"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task DownloadPlaylistAsync(string playlistUrl, string outputDirectory, VideoQuality quality = VideoQuality.Best)
    {
        if (string.IsNullOrWhiteSpace(playlistUrl))
            throw new ArgumentException("Playlist URL cannot be null or empty.", nameof(playlistUrl));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        try
        {
            var format = StringExtensions.GetVideoFormatCode(quality);
            var outputPath = Path.Combine(outputDirectory, "%(playlist_title)s/%(title)s.%(ext)s");
            await RunCommandAsync($"-f \"{format}\" -o \"{outputPath}\" {playlistUrl}");
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// Download audio from the video URL
    /// </summary>
    /// <param name="videoUrl"></param>
    /// <param name="outputDirectory"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task DownloadAudioAsync(string videoUrl, string outputDirectory, AudioQuality quality = AudioQuality.BestAudio)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be null or empty.", nameof(videoUrl));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        try
        {
            var format = StringExtensions.GetAudioFormatCode(quality);
            var outputPath = Path.Combine(outputDirectory, "%(title)s.%(ext)s");           
            await RunCommandAsync($"-f {format} -o \"{outputPath}\" --extract-audio --audio-format mp3 {videoUrl}");
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// Download subtitles from the video URL
    /// </summary>
    /// <param name="videoUrl"></param>
    /// <param name="outputDirectory"></param>
    /// <returns></returns>
    public async Task DownloadSubtitlesAsync(string videoUrl, string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be null or empty.", nameof(videoUrl));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        try
        {
            var outputPath = Path.Combine(outputDirectory, "%(title)s.%(ext)s");
            await RunCommandAsync($"--write-sub --sub-lang en -o \"{outputPath}\" {videoUrl}");
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// Download thumbnail from the video URL
    /// </summary>
    /// <param name="videoUrl"></param>
    /// <param name="outputDirectory"></param>
    /// <returns></returns>
    public async Task DownloadThumbnailAsync(string videoUrl, string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be null or empty.", nameof(videoUrl));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        try
        {
            var outputPath = Path.Combine(outputDirectory, "%(title)s.%(ext)s");
            await RunCommandAsync($"--write-thumbnail -o \"{outputPath}\" {videoUrl}");
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// Download all from the video URL
    /// </summary>
    /// <param name="videoUrl"></param>
    /// <param name="outputDirectory"></param>
    /// <returns></returns>
    public async Task DownloadAllAsync(string videoUrl, string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be null or empty.", nameof(videoUrl));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        try
        {
            var outputPath = Path.Combine(outputDirectory, "%(title)s.%(ext)s");
            await RunCommandAsync($"-o \"{outputPath}\" --write-sub --sub-lang en --write-thumbnail --extract-audio --audio-format mp3 {videoUrl}");
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// Download all from the playlist URL
    /// </summary>
    /// <param name="playlistUrl"></param>
    /// <param name="outputDirectory"></param>
    /// <returns></returns>
    public async Task DownloadAllPlaylistAsync(string playlistUrl, string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(playlistUrl))
            throw new ArgumentException("Playlist URL cannot be null or empty.", nameof(playlistUrl));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        try
        {
            var outputPath = Path.Combine(outputDirectory, "%(playlist_title)s/%(title)s.%(ext)s");
            await RunCommandAsync($"-o \"{outputPath}\" --write-sub --sub-lang en --write-thumbnail --extract-audio --audio-format mp3 {playlistUrl}");
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// Get video info
    /// </summary>
    /// <param name="videoUrl"></param>
    /// <returns></returns>
    public async Task<VideoInfo?> GetVideoInfoAsync(string videoUrl)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be null or empty.", nameof(videoUrl));

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ytDlpExecutable,
                    Arguments = $"--dump-json {videoUrl}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            Console.WriteLine(output);
            process.WaitForExit();

            var videoInfo = JsonSerializer.Deserialize<VideoInfo>(output);

            LogToFile(LogType.Info, $"Video Info: Successfully fetch video info.");
            return videoInfo;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Get playlist info
    /// </summary>
    /// <param name="playlistUrl"></param>
    /// <returns></returns>
    public async Task<string> GetPlaylistInfoAsync(string playlistUrl)
    {
        if (string.IsNullOrWhiteSpace(playlistUrl))
            throw new ArgumentException("Playlist URL cannot be null or empty.", nameof(playlistUrl));

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ytDlpExecutable,
                    Arguments = $"--dump-json {playlistUrl}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            LogToFile(LogType.Info, $"Playlist Info: Successfully fetch Playlist info.");
            return output;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// Get subtitles
    /// </summary>
    /// <param name="videoUrl"></param>
    /// <returns></returns>
    public async Task<string> GetSubtitlesAsync(string videoUrl)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be null or empty.", nameof(videoUrl));

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ytDlpExecutable,
                    Arguments = $"--write-sub --sub-lang en --skip-download -o %(title)s.%(ext)s {videoUrl}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            LogToFile(LogType.Info, $"Subtitle Info: Successfully fetch subtitle info.");
            return output;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// Get thumbnail to app directory
    /// </summary>
    /// <param name="videoUrl"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<string> GetThumbnailAsync(string videoUrl)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be null or empty.", nameof(videoUrl));

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ytDlpExecutable,
                    Arguments = $"--write-thumbnail --skip-download -o %(title)s.%(ext)s {videoUrl}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            LogToFile(LogType.Info, $"Thumbnail: {output}");
            return output;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// Get available formats
    /// </summary>
    /// <param name="videoUrl"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<List<VideoFormat>> GetAvailableFormatsAsync(string videoUrl)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ytDlpExecutable,
                    Arguments = $"--list-formats {videoUrl}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            LogToFile(LogType.Info, $"Get Format: {output}");

            // Parse the result and extract format details (ID, resolution, codec, etc.)
            var formats = ParseFormats(output);
            return formats;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to fetch available formats: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parse the format details from the result
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private List<VideoFormat> ParseFormats(string result)
    {
        var formats = new List<VideoFormat>();

        // Split the result by line and parse format details
        var lines = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Skip irrelevant lines (headers or non-format lines)
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("ID") || line.StartsWith("[youtube]") || line.StartsWith("[info]"))
                continue;

            // Split by spaces and extract the format information
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Ensure there are enough parts in the line to be a valid format
            if (parts.Length < 7)
                continue;

            // Extract relevant details
            var format = new VideoFormat
            {
                ID = parts[0],
                Type = parts[1],
                Resolution = parts[2],
                FPS = parts[3],
                CH = parts[4],
                FileSize = parts.Length > 5 ? parts[5] : null, // FileSize can vary
                Codec = parts.Length > 6 ? parts[6] : null, // Codec info can vary
                AdditionalInfo = parts.Length > 7 ? string.Join(" ", parts.Skip(6)) : null
            };

            formats.Add(format);
        }

        return formats;
    }

    /// <summary>
    /// Run the yt-dlp command with the specified arguments
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task RunCommandAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ytDlpExecutable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(startInfo))
        {
            if (process == null)
                throw new InvalidOperationException("Failed to start yt-dlp process.");

            // Listen to output and capture progress
            using (var reader = process.StandardOutput)
            {
                string output = string.Empty;
                while ((output = await reader.ReadLineAsync()) != null)
                {
                    // Output includes progress information in a specific format
                    ParseProgress(output);
                }
            }

            // Capture errors if any
            using (var errorReader = process.StandardError)
            {
                string errorOutput = await errorReader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(errorOutput))
                {
                    OnErrorMessage?.Invoke(this, StringExtensions.GetErrorMessage(errorOutput));
                    LogToFile(LogType.Error, errorOutput);
                }
            }

            await process.WaitForExitAsync();
        }
    }

    /// <summary>
    /// Parse the progress output and notify the UI
    /// </summary>
    /// <param name="output"></param>
    private void ParseProgress(string output)
    {
        // Regex patterns for different stages of the process
        var extractingUrlPattern = @"\[youtube\] Extracting URL: (?<url>https?://\S+)";
        var downloadingWebpagePattern = @"\[youtube\] (?<id>\S+): Downloading webpage";
        var downloadingJsonPattern = @"\[youtube\] (?<id>\S+): Downloading (ios|mweb) player API JSON";
        var downloadingM3u8Pattern = @"\[youtube\] (?<id>\S+): Downloading m3u8 information";
        var downloadingFormatPattern = @"\[info\] (?<id>\S+): Downloading (\d+) format\(s\): (?<format>\d+)";
        var downloadDestinationPattern = @"\[download\]\s*Destination:\s*(?<path>.+)";
        var resumeDownloadPattern = @"\[download\]\s*Resuming download at byte (?<byte>\d+)";
        var downloadCompletedPattern = @"\[download\]\s*(?<path>.+?)\s*has already been downloaded";
        var downloadProgressPatternWithUnknown = @"\[download\]\s*(?<percent>\d+(\.\d+)?)%\s*of\s*(?<size>\S+)\s*at\s*(?<speed>Unknown)\s*B/s\s*ETA\s*(?<eta>Unknown)";
        var downloadProgressPattern = @"\[download\]\s*(?<percent>\d+(\.\d+)?)%\s*of\s*(?<size>\S+)\s*at\s*(?<speed>\S+)\s*ETA\s*(?<eta>\S+)";
        var downloadProgressPatternComplete = @"\[download\] (?<percent>100)% of (?<size>\S+)";
        var progressPattern = @"\[download\]\s+(?<Percentage>\d+%)\s+of\s+(?<FileSize>[\d.]+\w+)\s+in\s+(?<Time>[\d:]+)\s+at\s+(?<Speed>[\d.]+\w+/s)";

        // Match each pattern and display the appropriate progress message
        if (Regex.IsMatch(output, extractingUrlPattern))
        {
            var match = Regex.Match(output, extractingUrlPattern);
            string url = match.Groups["url"].Value;

            LogToFile(LogType.Info, $"Extracting URL: {url}");

            // Notify the UI about URL extraction
            OnProgressMessage?.Invoke(this, $"Extracting URL: {url}");
        }
        else if (Regex.IsMatch(output, downloadingWebpagePattern))
        {
            var match = Regex.Match(output, downloadingWebpagePattern);
            string id = match.Groups["id"].Value;

            LogToFile(LogType.Info, $"Downloading webpage for video ID: {id}");

            // Notify the UI that the webpage for the video is being downloaded
            OnProgressMessage?.Invoke(this, $"Downloading webpage for video ID: {id}");
        }
        else if (Regex.IsMatch(output, downloadingJsonPattern))
        {
            var match = Regex.Match(output, downloadingJsonPattern);
            string id = match.Groups["id"].Value;

            LogToFile(LogType.Info, $"Downloading player API JSON for video ID: {id}");

            // Notify the UI about downloading the player API JSON
            OnProgressMessage?.Invoke(this, $"Downloading player API JSON for video ID: {id}");
        }
        else if (Regex.IsMatch(output, downloadingM3u8Pattern))
        {
            var match = Regex.Match(output, downloadingM3u8Pattern);
            string id = match.Groups["id"].Value;

            LogToFile(LogType.Info, $"Downloading m3u8 information for video ID: {id}");

            // Notify the UI about downloading m3u8 info
            OnProgressMessage?.Invoke(this, $"Downloading m3u8 information for video ID: {id}");
        }
        else if (Regex.IsMatch(output, downloadingFormatPattern))
        {
            var match = Regex.Match(output, downloadingFormatPattern);
            string format = match.Groups["format"].Value;
            string id = match.Groups["id"].Value;

            LogToFile(LogType.Info, $"Downloading format {format} for video ID: {id}");

            // Notify the UI about downloading a specific format
            OnProgressMessage?.Invoke(this, $"Downloading format {format} for video ID: {id}");
        }
        else if (Regex.IsMatch(output, downloadDestinationPattern))
        {
            // Extract the file path from the match
            var match = Regex.Match(output, downloadDestinationPattern);
            string path = match.Groups["path"].Value;

            LogToFile(LogType.Info, $"Download destination: {path}");

            // Notify the UI about the download destination path
            OnProgressMessage?.Invoke(this, $"Download destination: {path}");
        }
        else if (Regex.IsMatch(output, resumeDownloadPattern))
        {
            // Extract the byte position from the match
            var match = Regex.Match(output, resumeDownloadPattern);
            string bytePosition = match.Groups["byte"].Value;

            LogToFile(LogType.Info, $"Resuming download at byte {bytePosition}");

            // Notify the UI that the download is resuming from a specific byte
            OnProgressDownload?.Invoke(this, new DownloadProgressEventArgs
            {
                Message = $"Resuming download at byte {bytePosition}"
            });
        }
        else if (Regex.IsMatch(output, downloadCompletedPattern))
        {
            var match = Regex.Match(output, downloadCompletedPattern);
            string path = match.Groups["path"].Value;

            LogToFile(LogType.Info, $"Download completed: {path} has already been downloaded.");

            // Notify the UI that the download has completed
            OnProgressDownload?.Invoke(this, new DownloadProgressEventArgs
            {
                Message = $"Download completed: {path} has already been downloaded."
            });
        }
        else if (Regex.IsMatch(output, downloadProgressPatternWithUnknown))
        {
            // Handle the download progress (percentage, size, speed, ETA)
            var match = Regex.Match(output, downloadProgressPatternWithUnknown);
            string percent = match.Groups["percent"].Value;
            string size = match.Groups["size"].Value;
            string speed = match.Groups["speed"].Value;
            string eta = match.Groups["eta"].Value;

            // Notify the UI with download progress
            OnProgressDownload?.Invoke(this, new DownloadProgressEventArgs
            {
                Percent = percent,
                Size = size,
                Speed = speed,
                ETA = eta,
                Message = $"Downloading: {percent}% of {size}, Speed: {speed}, ETA: {eta}"
            });
        }
        else if (Regex.IsMatch(output, downloadProgressPattern))
        {
            var match = Regex.Match(output, downloadProgressPattern);
            var percent = match.Groups["percent"].Value;
            var size = match.Groups["size"].Value;
            var speed = match.Groups["speed"].Value;
            var eta = match.Groups["eta"].Value;

            // Trigger the event
            OnProgressDownload?.Invoke(this, new DownloadProgressEventArgs
            {
                Percent = percent,
                Size = size,
                Speed = speed,
                ETA = eta,
                Message = $"Downloading: {percent}% of {size}, Speed: {speed}, ETA: {eta}"
            });
        }
        else if (Regex.IsMatch(output, downloadProgressPatternComplete))
        {
            var match = Regex.Match(output, downloadProgressPatternComplete);
            string percent = match.Groups["percent"].Value;
            string size = match.Groups["size"].Value;

            LogToFile(LogType.Info, $"Download complete: {percent}% of {size}");

            // Notify the UI when download reaches 100%
            OnProgressDownload?.Invoke(this, new DownloadProgressEventArgs
            {
                Percent = "100",
                Size = size,
                Speed = "Unknown",
                ETA = "Unknown",
                Message = $"Download complete: {percent}% of {size}"
            });
        }
        else if (Regex.IsMatch(output, progressPattern))
        {
            var match = Regex.Match(output, progressPattern);
            var percentage = match.Groups["Percentage"].Value;
            var fileSize = match.Groups["FileSize"].Value;
            var time = match.Groups["Time"].Value;
            var speed = match.Groups["Speed"].Value;

            // Trigger progress event with extracted details
            OnProgressDownload?.Invoke(this, new DownloadProgressEventArgs
            {
                Percent = percentage,
                Size = fileSize,
                Speed = speed,
                ETA = time
            });
        }
        else
        {
            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            if (output.Contains("ERROR"))
            {
                LogToFile(LogType.Error, output);
            }
            else if (output.Contains("WARNING"))
            {
                LogToFile(LogType.Warning, output);
            }
            else
            {
                LogToFile(LogType.Info, output);

                // Notify the UI about the progress
                OnProgressMessage?.Invoke(this, output);
            }
        }
    }

    /// <summary>
    /// Log messages to a file
    /// </summary>
    /// <param name="logType"></param>
    /// <param name="message"></param>
    private void LogToFile(LogType logType, string message)
    {
        try
        {
            message = $"{DateTime.Now} - {logType.ToString().ToUpper()}: {message}";
            File.AppendAllText(logPath, message + Environment.NewLine);
        }
        catch (Exception)
        {
        }
    }



}
