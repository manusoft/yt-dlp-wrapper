using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YtDlpWrapper;

/// <summary>
/// YtDlpEngine class to interact with yt-dlp executable
/// </summary>
[Obsolete("YtdlpEngine() is deprecated. Use ytdlp() instead.")]
public class YtDlpEngine
{
    private readonly ProgressParser progressParser;

    // Event to notify progress updates
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    // Event to notify download completion
    public event EventHandler<string>? OnCompleteDownload;
    // Event to notify message updates
    public event EventHandler<string>? OnProgressMessage;
    // Event to notify message updates
    public event EventHandler<string>? OnErrorMessage;

    private readonly string ytDlpExecutable;
    private readonly string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
    private readonly string logPath = Path.Combine(AppContext.BaseDirectory, "logs", $"EngineLog_{DateTime.Today.ToString("yyyy_MM_dd")}.log");

    /// <summary>
    /// Constructor to initialize the YtDlpEngine
    /// </summary>
    /// <param name="ytDlpPath">Provide the yt-dlp.exe path</param>
    /// <exception cref="FileNotFoundException"></exception>
    public YtDlpEngine(string ytDlpPath = "yt-dlp.exe")
    {
        Logger.SetLogFilePath(logPath);

        // Log initialization
        Logger.Log(LogType.Info, "Initializing YtDlpEngine...");

        // Validate the path
        ytDlpExecutable = ValidateExecutablePath(ytDlpPath);

        Logger.Log(LogType.Info, "Engine started successfully.");

        // Inititalize progress parser
        progressParser = new ProgressParser();

        // Subscribe events
        progressParser.OnProgressDownload += (sender, e) => OnProgressDownload?.Invoke(this, e);
        progressParser.OnCompleteDownload += (sender, e) => OnCompleteDownload?.Invoke(this, e);
        progressParser.OnProgressMessage += (sender, e) => OnProgressMessage?.Invoke(this, e);
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
            Logger.Log(LogType.Error, $"yt-dlp executable not found at: {absolutePath}");
            throw new FileNotFoundException($"yt-dlp executable not found at: {absolutePath}");
        }

        Logger.Log(LogType.Info, $"yt-dlp executable found at: {absolutePath}");
        return absolutePath;
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
    public async Task DownloadVideoAsync(string videoUrl, string outputDirectory, VideoQuality quality = VideoQuality.Best, string customFormat = "")
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be null or empty.", nameof(videoUrl));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        try
        {
            if (quality == VideoQuality.Custom)
            {
                if (string.IsNullOrWhiteSpace(customFormat))
                    throw new ArgumentException("Custom format cannot be null or empty.", nameof(customFormat));

                await RunCommandAsync($"-f {customFormat} -o \"{outputDirectory}/%(title)s.%(ext)s\" {videoUrl}");
            }
            else
            {
                var format = StringExtensions.GetVideoFormatCode(quality);
                var outputPath = Path.Combine(outputDirectory, "%(title)s.%(ext)s");
                await RunCommandAsync($"-f \"{format}\" -o \"{outputPath}\" {videoUrl}");
            }
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
    public async Task DownloadPlaylistAsync(string playlistUrl, string outputDirectory, VideoQuality quality = VideoQuality.Best, string customFormat = "")
    {
        if (string.IsNullOrWhiteSpace(playlistUrl))
            throw new ArgumentException("Playlist URL cannot be null or empty.", nameof(playlistUrl));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        try
        {
            if (quality == VideoQuality.Custom)
            {
                if (string.IsNullOrWhiteSpace(customFormat))
                    throw new ArgumentException("Custom format cannot be null or empty.", nameof(customFormat));

                await RunCommandAsync($"-f {customFormat} -o \"{outputDirectory}/%(playlist_title)s/%(title)s.%(ext)s\" {playlistUrl}");
            }
            else
            {
                var format = StringExtensions.GetVideoFormatCode(quality);
                var outputPath = Path.Combine(outputDirectory, "%(playlist_title)s/%(title)s.%(ext)s");
                await RunCommandAsync($"-f \"{format}\" -o \"{outputPath}\" {playlistUrl}");
            }
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
    public async Task DownloadAudioAsync(string videoUrl, string outputDirectory, AudioQuality quality = AudioQuality.BestAudio, string customFormat = "")
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be null or empty.", nameof(videoUrl));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        try
        {
            if (quality == AudioQuality.Custom)
            {
                if (string.IsNullOrWhiteSpace(customFormat))
                    throw new ArgumentException("Custom format cannot be null or empty.", nameof(customFormat));

                await RunCommandAsync($"-f {customFormat} -o \"{outputDirectory}/%(title)s.%(ext)s\" --extract-audio --audio-format mp3 {videoUrl}");
            }
            else
            {
                var format = StringExtensions.GetAudioFormatCode(quality);
                var outputPath = Path.Combine(outputDirectory, "%(title)s.%(ext)s");
                await RunCommandAsync($"-f {format} -o \"{outputPath}\" --extract-audio --audio-format mp3 {videoUrl}");
            }
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

            Logger.Log(LogType.Info, $"Video Info: Successfully fetch video info.");
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

            Logger.Log(LogType.Info, $"Playlist Info: Successfully fetch Playlist info.");
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

            Logger.Log(LogType.Info, $"Subtitle Info: Successfully fetch subtitle info.");
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

            Logger.Log(LogType.Info, $"Thumbnail: {output}");
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
                    Arguments = $"-F {videoUrl}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            Logger.Log(LogType.Info, $"Get Format: {output}");

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
                //CH = parts[4],
                //FileSize = parts.Length > 5 ? parts[5] : null, // FileSize can vary
                //Codec = parts.Length > 6 ? parts[6] : null, // Codec info can vary
                //AdditionalInfo = parts.Length > 7 ? string.Join(" ", parts.Skip(6)) : null
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
                    progressParser.ParseProgress(output);
                }
            }

            // Capture errors if any
            using (var errorReader = process.StandardError)
            {
                string errorOutput = await errorReader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(errorOutput))
                {
                    OnErrorMessage?.Invoke(this, StringExtensions.GetErrorMessage(errorOutput));
                    Logger.Log(LogType.Error, errorOutput);
                }
            }

            await process.WaitForExitAsync();
        }
    }    
}
