using System.Diagnostics;
using System.Text;

namespace YtDlpWrapper;

public class Ytdlp
{
    private readonly string _ytDlpPath;
    private readonly StringBuilder _commandBuilder;
    private readonly ProgressParser progressParser;

    public Ytdlp(string ytDlpPath = "yt-dlp")
    {
        _ytDlpPath = ytDlpPath;
        _commandBuilder = new StringBuilder();
        progressParser = new ProgressParser();

        // Subscribe events
        progressParser.OnOutput += (sender, e) => OnOutput?.Invoke(this, e);
        progressParser.OnProgressDownload += (sender, e) => OnProgressDownload?.Invoke(this, e);
        progressParser.OnCompleteDownload += (sender, e) => OnCompleteDownload?.Invoke(this, e);
        progressParser.OnProgressMessage += (sender, e) => OnProgressMessage?.Invoke(this, e);
    }

    // Event for progress updates
    public event Action<string> OnProgress;

    // Event for error handling
    public event Action<string> OnError;

    // Event for command completed
    public event Action<bool, string> OnCommandCompleted;


    // Event for output updates
    public event EventHandler<string> OnOutput;

    // Event to notify progress updates
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    // Event to notify download completion
    public event EventHandler<string>? OnCompleteDownload;
    // Event to notify message updates
    public event EventHandler<string>? OnProgressMessage;
    // Event to notify message updates
    public event EventHandler<string>? OnErrorMessage;


    /// <summary>
    /// Previews the current built command for debugging purposes.
    /// </summary>
    /// <returns>The current command string.</returns>
    public string PreviewCommand()
    {
        return _commandBuilder.ToString();
    }

    /// <summary>
    /// Show current version
    /// </summary>
    /// <returns></returns>
    public Ytdlp Version()
    {
        _commandBuilder.Append("--version ");
        return this;
    }

    /// <summary>
    /// Adds an option to extract audio and specify the format.
    /// </summary>
    /// <param name="audioFormat">Audio format, e.g., "mp3", "aac".</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp ExtractAudio(string audioFormat)
    {
        _commandBuilder.Append($"--extract-audio --audio-format {audioFormat} ");
        return this;
    }

    /// <summary>
    /// Adds an option to embed metadata into the output file.
    /// </summary>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp EmbedMetadata()
    {
        _commandBuilder.Append("--embed-metadata ");
        return this;
    }

    /// <summary>
    /// Adds an option to embed the thumbnail into the output file.
    /// </summary>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp EmbedThumbnail()
    {
        _commandBuilder.Append("--embed-thumbnail ");
        return this;
    }

    /// <summary>
    /// Adds an option to specify the output filename template.
    /// </summary>
    /// <param name="template">The output template.</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp SetOutputTemplate(string template)
    {
        _commandBuilder.Append($"-o \"{template}\" ");
        return this;
    }

    /// <summary>
    /// Sets options for downloading playlists.
    /// </summary>
    /// <param name="items">Specific items to download, e.g., "1,3-5".</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp SelectPlaylistItems(string items)
    {
        _commandBuilder.Append($"--playlist-items {items} ");
        return this;
    }

    /// <summary>
    /// Adds an option to set download speed limits.
    /// </summary>
    /// <param name="rate">Maximum download rate, e.g., "50K", "4.2M".</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp SetDownloadRate(string rate)
    {
        _commandBuilder.Append($"--limit-rate {rate} ");
        return this;
    }

    /// <summary>
    /// Configures proxy settings.
    /// </summary>
    /// <param name="proxy">Proxy URL, e.g., "http://127.0.0.1:8080".</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp UseProxy(string proxy)
    {
        _commandBuilder.Append($"--proxy {proxy} ");
        return this;
    }

    /// <summary>
    /// Adds an option to skip downloading and only simulate.
    /// </summary>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp Simulate()
    {
        _commandBuilder.Append("--simulate ");
        return this;
    }

    /// <summary>
    /// Adds an option to write video metadata to a JSON file.
    /// </summary>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp WriteMetadataToJson()
    {
        _commandBuilder.Append("--write-info-json ");
        return this;
    }

    /// <summary>
    /// Enables downloading video subtitles.
    /// </summary>
    /// <param name="languages">Comma-separated list of languages or "all".</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp DownloadSubtitles(string languages = "all")
    {
        _commandBuilder.Append($"--write-subs --sub-langs {languages} ");
        return this;
    }

    /// <summary>
    /// Sets video format preferences.
    /// </summary>
    /// <param name="format">The format string, e.g., "best", "mp4".</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp SetFormat(string format)
    {
        _commandBuilder.Append($"-f {format} ");
        return this;
    }

    /// <summary>
    /// Downloads video thumbnails.
    /// </summary>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp DownloadThumbnails()
    {
        _commandBuilder.Append("--write-thumbnail ");
        return this;
    }

    /// <summary>
    /// Enables or disables downloading livestreams from the start.
    /// </summary>
    /// <param name="fromStart">True to download from the start, false for live.</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp DownloadLivestream(bool fromStart = true)
    {
        _commandBuilder.Append(fromStart ? "--live-from-start " : "--no-live-from-start ");
        return this;
    }

    /// <summary>
    /// Adds an option to retry downloads on failure.
    /// </summary>
    /// <param name="retries">Number of retries, or "infinite".</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp SetRetries(string retries)
    {
        _commandBuilder.Append($"--retries {retries} ");
        return this;
    }

    /// <summary>
    /// Adds an option to download sections of a video based on time ranges.
    /// </summary>
    /// <param name="timeRanges">Time ranges, e.g., "*00:01:00-00:02:00".</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp DownloadSections(string timeRanges)
    {
        _commandBuilder.Append($"--download-sections {timeRanges} ");
        return this;
    }

    /// <summary>
    /// Enables concatenating multiple videos into a single file.
    /// </summary>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp ConcatenateVideos()
    {
        _commandBuilder.Append("--concat-playlist always ");
        return this;
    }

    /// <summary>
    /// Adds an option to modify metadata fields using regex replacements.
    /// </summary>
    /// <param name="field">Field to modify, e.g., "title".</param>
    /// <param name="regex">Regex pattern to search for.</param>
    /// <param name="replacement">Replacement text.</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp ReplaceMetadata(string field, string regex, string replacement)
    {
        _commandBuilder.Append($"--replace-in-metadata {field} {regex} {replacement} ");
        return this;
    }

    /// <summary>
    /// Enables skipping of already downloaded files.
    /// </summary>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp SkipDownloaded()
    {
        _commandBuilder.Append("--download-archive downloaded.txt ");
        return this;
    }

    /// <summary>
    /// Adds a custom user agent for the download process.
    /// </summary>
    /// <param name="userAgent">The user agent string to use.</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp SetUserAgent(string userAgent)
    {
        _commandBuilder.Append($"--user-agent {userAgent} ");
        return this;
    }

    /// <summary>
    /// Enables logging of the process to a specific file.
    /// </summary>
    /// <param name="logFile">Path to the log file.</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp LogToFile(string logFile)
    {
        _commandBuilder.Append($"--write-log {logFile} ");
        return this;
    }

    /// <summary>
    /// Configures cookies file for authentication.
    /// </summary>
    /// <param name="cookieFile">Path to the cookies file.</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp UseCookies(string cookieFile)
    {
        _commandBuilder.Append($"--cookies {cookieFile} ");
        return this;
    }

    /// <summary>
    /// Sets a custom referer header for the download process.
    /// </summary>
    /// <param name="referer">The referer URL.</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp SetReferer(string referer)
    {
        _commandBuilder.Append($"--referer {referer} ");
        return this;
    }

    /// <summary>
    /// Download Playlist as a Single Video
    /// </summary>
    /// <param name="format"></param>
    /// <returns></returns>
    public Ytdlp MergePlaylistIntoSingleVideo(string format)
    {
        _commandBuilder.Append($"--merge-output-format {format} ");
        return this;
    }

    /// <summary>
    /// Set Custom HTTP Headers
    /// </summary>
    /// <param name="header"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Ytdlp SetCustomHeader(string header, string value)
    {
        _commandBuilder.Append($"--add-header \"{header}: {value}\" ");
        return this;
    }

    /// <summary>
    /// Download Specific Format
    /// </summary>
    /// <param name="resolution"></param>
    /// <returns></returns>
    public Ytdlp SetResolution(string resolution)
    {
        _commandBuilder.Append($"--format \"bestvideo[height<={resolution}]\" ");
        return this;
    }

    /// <summary>
    /// Metadata Extraction Options
    /// </summary>
    /// <returns></returns>
    public Ytdlp ExtractMetadataOnly()
    {
        _commandBuilder.Append("--dump-json ");
        return this;
    }

    /// <summary>
    /// Download Video and Audio Separately
    /// </summary>
    /// <returns></returns>
    public Ytdlp DownloadAudioAndVideoSeparately()
    {
        _commandBuilder.Append("--write-video --write-audio ");
        return this;
    }

    /// <summary>
    /// Post-Processing Options
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public Ytdlp PostProcessFiles(string operation)
    {
        _commandBuilder.Append($"--postprocessor-args \"{operation}\" ");
        return this;
    }

    /// <summary>
    /// Limit Download Time
    /// </summary>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public Ytdlp SetDownloadTimeout(string timeout)
    {
        _commandBuilder.Append($"--download-timeout {timeout} ");
        return this;
    }

    /// <summary>
    /// Authentication Support
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public Ytdlp SetAuthentication(string username, string password)
    {
        _commandBuilder.Append($"--username {username} --password {password} ");
        return this;
    }

    /// <summary>
    /// Custom Output Folder
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    public Ytdlp SetOutputFolder(string folderPath)
    {
        _commandBuilder.Append($"-o \"{folderPath}/%(title)s.%(ext)s\" ");
        return this;
    }

    /// <summary>
    /// Enable or Disable Video Ads
    /// </summary>
    /// <returns></returns>
    public Ytdlp DisableAds()
    {
        _commandBuilder.Append("--no-ads ");
        return this;
    }

    /// <summary>
    /// Download Live Streams in Real-Time
    /// </summary>
    /// <returns></returns>
    public Ytdlp DownloadLiveStreamRealTime()
    {
        _commandBuilder.Append("--live-from-start --recode-video mp4 ");
        return this;
    }

    /// <summary>
    /// Add custom command to be executed.
    /// </summary>
    /// <param name="customCommand">Custom command string to append.</param>
    /// <returns>The current instance for chaining.</returns>
    public Ytdlp AddCustomCommand(string customCommand)
    {
        _commandBuilder.Append($"{customCommand} ");
        return this;
    }

    /// <summary>
    /// Timeouts for Commands
    /// </summary>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public Ytdlp SetTimeout(TimeSpan timeout)
    {
        _commandBuilder.Append($"--timeout {timeout.TotalSeconds} ");
        return this;
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
                    FileName = _ytDlpPath,
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
    /// Download from Multiple Sources Simultaneously
    /// </summary>
    /// <param name="urls"></param>
    /// <returns></returns>
    public async Task ExecuteBatchAsync(IEnumerable<string> urls)
    {
        var tasks = urls.Select(url => ExecuteAsync(url));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes the configured yt-dlp command on the provided URL.
    /// </summary>
    /// <param name="url">The URL of the video to process.</param>    
    public async Task ExecuteAsync(string url)
    {
        string arguments = $"{_commandBuilder}{url}";
        _commandBuilder.Clear();
        await RunYtdlpAsync(arguments);
    }

    private async Task RunYtdlpAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ytDlpPath,
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
        //var process = CreateProcess(arguments);

        //var timeoutCancellationTokenSource = new CancellationTokenSource();
        //var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60), timeoutCancellationTokenSource.Token); // Set default timeout to 60 seconds

        //process.OutputDataReceived += (sender, args) =>
        //{
        //    if (!string.IsNullOrEmpty(args.Data))
        //    {
        //        //if (args.Data.Contains("%"))
        //        //{
        //        //    progressParser.ParseProgress(args.Data);
        //        //}
        //        //else
        //        //{
        //         OnProgress?.Invoke($"Informations: {args.Data}");
        //        //}
        //        progressParser.ParseProgress(args.Data);
        //    }
        //};

        //process.ErrorDataReceived += (sender, args) =>
        //{
        //    if (!string.IsNullOrEmpty(args.Data))
        //    {
        //        OnError?.Invoke(args.Data);
        //    }
        //};

        //var processTask = Task.Run(async () =>
        //{
        //    process.Start();
        //    process.BeginOutputReadLine();
        //    process.BeginErrorReadLine();
        //    await process.WaitForExitAsync();
        //    OnCommandCompleted?.Invoke(process.ExitCode == 0, "Process completed successfully.");
        //});

        //var completedTask = await Task.WhenAny(processTask, timeoutTask);

        //if (completedTask == timeoutTask)
        //{
        //    process.Kill();
        //    OnError?.Invoke("Download process timed out.");
        //}
        //else
        //{
        //    timeoutCancellationTokenSource.Cancel(); // Cancel the timeout task if the process finishes first
        //    await processTask;
        //}

    }

    private Process CreateProcess(string arguments)
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };
    }

    // validate custom commands not implemented now

    private static readonly HashSet<string> ValidOptions = new HashSet<string>
{
    "--extract-audio", "--audio-format", "--format", "--playlist-items", "--limit-rate", "--proxy",
    "--simulate", "--write-info-json", "--write-subs", "--sub-langs", "--write-thumbnail", "--live-from-start",
    "--retries", "--download-sections", "--concat-playlist", "--replace-in-metadata", "--download-archive",
    "--user-agent", "--write-log", "--cookies", "--referer" // add more valid commands  here
};

    private Ytdlp AddCustomCommand2(string customCommand)
    {
        // Validate the command before appending it
        if (ValidOptions.Contains(customCommand.Split(' ')[0])) // Validates only by the first word of the command
        {
            _commandBuilder.Append($"{customCommand} ");
        }
        else
        {
            OnError?.Invoke($"Invalid option: {customCommand}");
        }
        return this;
    }
}
