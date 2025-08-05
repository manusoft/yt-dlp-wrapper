using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YtdlpDotNet;

public sealed class Ytdlp
{
    private readonly string _ytDlpPath;
    private readonly StringBuilder _commandBuilder;
    private readonly ProgressParser _progressParser;
    private readonly ILogger _logger;
    private string _format = "best";
    private string _outputFolder = ".";
    private string? _outputTemplate;

    // Events for progress and status updates
    public event Action<string>? OnProgress;
    public event Action<string>? OnError;
    public event Action<bool, string>? OnCommandCompleted;
    public event EventHandler<string>? OnOutputMessage;
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    public event EventHandler<string>? OnCompleteDownload;
    public event EventHandler<string>? OnProgressMessage;
    public event EventHandler<string>? OnErrorMessage;
    public event Action<object, string>? OnPostProcessingComplete;

    // Valid yt-dlp options for validation
    private static readonly HashSet<string> ValidOptions = new HashSet<string>
    {
        // General Download
        "--format", "--output", "-o", "--no-overwrites", "--continue", "--no-continue",
        "--ignore-errors", "--no-part", "--no-mtime", "--write-description", "--write-info-json",
        "--write-annotations", "--write-thumbnail", "--write-all-thumbnails", "--write-sub",
        "--write-auto-sub", "--sub-format", "--sub-langs", "--skip-download", "--no-playlist",
        "--yes-playlist", "--playlist-items", "--playlist-start", "--playlist-end", "--match-title",
        "--reject-title", "--no-check-certificate", "--user-agent", "--referer", "--cookies",
        "--add-header", "--limit-rate", "--retries", "--fragment-retries", "--timeout",
        "--source-address", "--force-ipv4", "--force-ipv6",

        // Authentication
        "--username", "--password", "--twofactor", "--netrc", "--netrc-location", "--video-password",

        // Proxy / Network
        "--proxy", "--geo-bypass", "--geo-bypass-country", "--geo-bypass-ip-block", "--no-geo-bypass",

        // Download Archive
        "--download-archive", "--max-downloads", "--min-filesize", "--max-filesize", "--date",
        "--datebefore", "--dateafter", "--match-filter",

        // Post-processing
        "--extract-audio", "--audio-format", "--audio-quality", "--recode-video",
        "--postprocessor-args", "--embed-subs", "--embed-thumbnail", "--embed-metadata",
        "--embed-chapters", "--embed-info-json", "--convert-subs", "--merge-output-format",

        // Subtitle & Thumbnail
        "--write-sub", "--write-auto-sub", "--sub-lang", "--sub-format", "--write-thumbnail",
        "--write-all-thumbnails", "--convert-subs", "--embed-subs", "--embed-thumbnail",

        // Simulation / Debug
        "--simulate", "--skip-download", "--print", "--quiet", "--no-warnings", "--verbose",
        "--dump-json", "--force-write-archive", "--no-progress", "--newline", "--write-log",

        // Advanced
        "--download-sections", "--concat-playlist", "--replace-in-metadata", "--call-home",
        "--write-pages", "--sleep-interval", "--max-sleep-interval", "--min-sleep-interval",
        "--sleep-subtitles", "--write-link", "--live-from-start", "--no-live-from-start",
        "--no-ads", "--force-keyframes-at-cuts", "--remux-video", "--no-color",
        "--paths", "--output-na-placeholder", "--playlist-random", "--sponsorblock-mark",
        "--sponsorblock-remove", "--sponsorblock-chapter-title",

        // Others (use with caution depending on context)
        "--config-location", "--write-video", "--write-audio", "--no-post-overwrites",
        "--break-on-existing", "--break-per-input", "--windows-filenames", "--restrict-filenames"
    };


    public Ytdlp(string ytDlpPath = "yt-dlp", ILogger? logger = null)
    {
        _ytDlpPath = ValidatePath(ytDlpPath);
        if (!File.Exists(_ytDlpPath) && !IsInPath(_ytDlpPath))
            throw new YtdlpException($"yt-dlp executable not found at {_ytDlpPath}. Install yt-dlp or specify a valid path.");
        _commandBuilder = new StringBuilder();
        _progressParser = new ProgressParser(logger);
        _logger = logger ?? new DefaultLogger();

        // Subscribe to progress parser events
        _progressParser.OnOutputMessage += (sender, e) => OnOutputMessage?.Invoke(this, e);
        _progressParser.OnProgressDownload += (sender, e) => OnProgressDownload?.Invoke(this, e);
        _progressParser.OnCompleteDownload += (sender, e) => OnCompleteDownload?.Invoke(this, e);
        _progressParser.OnProgressMessage += (sender, e) => OnProgressMessage?.Invoke(this, e);
        _progressParser.OnErrorMessage += (sender, e) => OnErrorMessage?.Invoke(this, e);
        _progressParser.OnPostProcessingComplete += (s, e) => OnPostProcessingComplete?.Invoke(this, e);
    }

    #region Command Building Methods
    public Ytdlp Version()
    {
        _commandBuilder.Append("--version ");
        return this;
    }

    public Ytdlp ExtractAudio(string audioFormat)
    {
        if (string.IsNullOrWhiteSpace(audioFormat))
            throw new ArgumentException("Audio format cannot be empty.", nameof(audioFormat));
        _commandBuilder.Append($"--extract-audio --audio-format {SanitizeInput(audioFormat)} ");
        return this;
    }

    public Ytdlp EmbedMetadata()
    {
        _commandBuilder.Append("--embed-metadata ");
        return this;
    }

    public Ytdlp EmbedThumbnail()
    {
        _commandBuilder.Append("--embed-thumbnail ");
        return this;
    }

    public Ytdlp SetOutputTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
            throw new ArgumentException("Output template cannot be empty.", nameof(template));
        _outputTemplate = template.Replace("\\", "/").Trim();
        return this;
    }

    public Ytdlp SelectPlaylistItems(string items)
    {
        if (string.IsNullOrWhiteSpace(items))
            throw new ArgumentException("Playlist items cannot be empty.", nameof(items));
        _commandBuilder.Append($"--playlist-items {SanitizeInput(items)} ");
        return this;
    }

    public Ytdlp SetDownloadRate(string rate)
    {
        if (string.IsNullOrWhiteSpace(rate))
            throw new ArgumentException("Download rate cannot be empty.", nameof(rate));
        _commandBuilder.Append($"--limit-rate {SanitizeInput(rate)} ");
        return this;
    }

    public Ytdlp UseProxy(string proxy)
    {
        if (string.IsNullOrWhiteSpace(proxy))
            throw new ArgumentException("Proxy URL cannot be empty.", nameof(proxy));
        _commandBuilder.Append($"--proxy {SanitizeInput(proxy)} ");
        return this;
    }

    public Ytdlp Simulate()
    {
        _commandBuilder.Append("--simulate ");
        return this;
    }

    public Ytdlp WriteMetadataToJson()
    {
        _commandBuilder.Append("--write-info-json ");
        return this;
    }

    public Ytdlp DownloadSubtitles(string languages = "all")
    {
        if (string.IsNullOrWhiteSpace(languages))
            throw new ArgumentException("Languages cannot be empty.", nameof(languages));
        _commandBuilder.Append($"--write-subs --sub-langs {SanitizeInput(languages)} ");
        return this;
    }

    public Ytdlp SetFormat([Required] string format)
    {
        _format = format;
        return this;
    }

    public Ytdlp DownloadThumbnails()
    {
        _commandBuilder.Append("--write-thumbnail ");
        return this;
    }

    public Ytdlp DownloadLivestream(bool fromStart = true)
    {
        _commandBuilder.Append(fromStart ? "--live-from-start " : "--no-live-from-start ");
        return this;
    }

    public Ytdlp SetRetries(string retries)
    {
        if (string.IsNullOrWhiteSpace(retries))
            throw new ArgumentException("Retries cannot be empty.", nameof(retries));
        _commandBuilder.Append($"--retries {SanitizeInput(retries)} ");
        return this;
    }

    public Ytdlp DownloadSections(string timeRanges)
    {
        if (string.IsNullOrWhiteSpace(timeRanges))
            throw new ArgumentException("Time ranges cannot be empty.", nameof(timeRanges));
        _commandBuilder.Append($"--download-sections {SanitizeInput(timeRanges)} ");
        return this;
    }

    public Ytdlp ConcatenateVideos()
    {
        _commandBuilder.Append("--concat-playlist always ");
        return this;
    }

    public Ytdlp ReplaceMetadata(string field, string regex, string replacement)
    {
        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(regex) || replacement == null)
            throw new ArgumentException("Metadata field, regex, and replacement cannot be empty.");
        _commandBuilder.Append($"--replace-in-metadata {SanitizeInput(field)} {SanitizeInput(regex)} {SanitizeInput(replacement)} ");
        return this;
    }

    public Ytdlp SkipDownloaded()
    {
        _commandBuilder.Append("--download-archive downloaded.txt ");
        return this;
    }

    public Ytdlp SetUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("User agent cannot be empty.", nameof(userAgent));
        _commandBuilder.Append($"--user-agent {SanitizeInput(userAgent)} ");
        return this;
    }

    public Ytdlp LogToFile(string logFile)
    {
        if (string.IsNullOrWhiteSpace(logFile))
            throw new ArgumentException("Log file path cannot be empty.", nameof(logFile));
        _commandBuilder.Append($"--write-log {SanitizeInput(logFile)} ");
        return this;
    }

    public Ytdlp UseCookies(string cookieFile)
    {
        if (string.IsNullOrWhiteSpace(cookieFile))
            throw new ArgumentException("Cookie file path cannot be empty.", nameof(cookieFile));
        _commandBuilder.Append($"--cookies {SanitizeInput(cookieFile)} ");
        return this;
    }

    public Ytdlp SetReferer(string referer)
    {
        if (string.IsNullOrWhiteSpace(referer))
            throw new ArgumentException("Referer URL cannot be empty.", nameof(referer));
        _commandBuilder.Append($"--referer {SanitizeInput(referer)} ");
        return this;
    }

    public Ytdlp MergePlaylistIntoSingleVideo(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Format cannot be empty.", nameof(format));
        _commandBuilder.Append($"--merge-output-format {SanitizeInput(format)} ");
        return this;
    }

    public Ytdlp SetCustomHeader(string header, string value)
    {
        if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Header and value cannot be empty.");
        _commandBuilder.Append($"--add-header \"{SanitizeInput(header)}:{SanitizeInput(value)}\" ");
        return this;
    }

    public Ytdlp SetResolution(string resolution)
    {
        if (string.IsNullOrWhiteSpace(resolution))
            throw new ArgumentException("Resolution cannot be empty.", nameof(resolution));
        _commandBuilder.Append($"--format \"bestvideo[height<={SanitizeInput(resolution)}]\" ");
        return this;
    }

    public Ytdlp ExtractMetadataOnly()
    {
        _commandBuilder.Append("--dump-json ");
        return this;
    }

    public Ytdlp DownloadAudioAndVideoSeparately()
    {
        _commandBuilder.Append("--write-video --write-audio ");
        return this;
    }

    public Ytdlp PostProcessFiles(string operation)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("Operation cannot be empty.", nameof(operation));
        _commandBuilder.Append($"--postprocessor-args \"{SanitizeInput(operation)}\" ");
        return this;
    }

    public Ytdlp SetKeepTempFiles(bool keep)
    {
        if (keep) _commandBuilder.Append(" -k");
        return this;
    }

    public Ytdlp SetDownloadTimeout(string timeout)
    {
        if (string.IsNullOrWhiteSpace(timeout))
            throw new ArgumentException("Timeout cannot be empty.", nameof(timeout));
        _commandBuilder.Append($"--download-timeout {SanitizeInput(timeout)} ");
        return this;
    }

    public Ytdlp SetAuthentication(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Username and password cannot be empty.");
        _commandBuilder.Append($"--username {SanitizeInput(username)} --password {SanitizeInput(password)} ");
        return this;
    }

    public Ytdlp SetOutputFolder([Required] string folderPath)
    {
        _outputFolder = folderPath;
        return this;
    }

    public Ytdlp DisableAds()
    {
        _commandBuilder.Append("--no-ads ");
        return this;
    }

    public Ytdlp DownloadLiveStreamRealTime()
    {
        _commandBuilder.Append("--live-from-start --recode-video mp4 ");
        return this;
    }

    public Ytdlp AddCustomCommand(string customCommand)
    {
        if (string.IsNullOrWhiteSpace(customCommand))
            throw new ArgumentException("Custom command cannot be empty.", nameof(customCommand));

        var commandParts = customCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (commandParts.Length == 0 || !ValidOptions.Contains(SanitizeInput(commandParts[0])))
        {
            var errorMessage = $"Invalid option: {customCommand}";
            OnError?.Invoke(errorMessage);
            _logger.Log(LogType.Error, errorMessage);
            return this;
        }

        _commandBuilder.Append($"{SanitizeInput(customCommand)} ");
        return this;
    }

    public Ytdlp SetTimeout(TimeSpan timeout)
    {
        if (timeout.TotalSeconds <= 0)
            throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));
        _commandBuilder.Append($"--timeout {timeout.TotalSeconds} ");
        return this;
    }
    #endregion

    #region Execution Methods
    public string PreviewCommand()
    {
        return _commandBuilder.ToString().Trim();
    }

    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.Log(LogType.Error, $"Failed to get yt-dlp version: {error}");
                return string.Empty;
            }

            string version = output.Trim();
            _logger.Log(LogType.Info, $"yt-dlp version: {version}");
            return version;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Error getting yt-dlp version: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<Metadata?> GetVideoMetadataJsonAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        var arguments = $"--dump-json {SanitizeInput(url)}";
        _logger.Log(LogType.Info, $"Executing dump-json for: {url}");

        var startInfo = new ProcessStartInfo
        {
            FileName = _ytDlpPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.Log(LogType.Error, $"yt-dlp error: {error}");
                throw new YtdlpException($"yt-dlp failed with error: {error}");
            }

            _logger.Log(LogType.Info, "Parsing yt-dlp metadata output");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<Metadata>(output, options);
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Error parsing yt-dlp metadata: {ex.Message}");
            throw new YtdlpException("Failed to parse yt-dlp dump-json output.", ex);
        }
    }

    public async Task<List<VideoFormat>> GetAvailableFormatsAsync(string videoUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be empty.", nameof(videoUrl));

        try
        {
            var process = CreateProcess($"-F {SanitizeInput(videoUrl)}");

            process.Start();

            // Read standard output asynchronously
            var readOutputTask = process.StandardOutput.ReadToEndAsync();

            // Wait for process to exit, observing cancellation
            using (cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true); // forcefully kill process
                    }
                }
                catch { /* ignore if already exited */ }
            }))
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            var output = await readOutputTask;

            _logger.Log(LogType.Info, $"Get Format: {output}");
            return ParseFormats(output);
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Format fetching cancelled by user.");
            throw;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to fetch available formats: {ex.Message}";
            _logger.Log(LogType.Error, errorMessage);
            throw new YtdlpException(errorMessage, ex);
        }
    }

    public async Task ExecuteAsync(string url, CancellationToken cancellationToken = default, string? outputTemplate = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        // Ensure output folder exists
        try
        {
            Directory.CreateDirectory(_outputFolder);
            _logger.Log(LogType.Info, $"Output folder: {Path.GetFullPath(_outputFolder)}");
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Failed to create output folder {_outputFolder}: {ex.Message}");
            throw new YtdlpException($"Failed to create output folder {_outputFolder}", ex);
        }

        // Reset ProgressParser for this download
        _progressParser.Reset();
        _logger.Log(LogType.Info, $"Starting download for URL: {url}");

        // Use provided template or default
        string template = Path.Combine(_outputFolder, _outputTemplate?.Replace("\\", "/")!)
            ?? Path.Combine(_outputFolder, "%(title)s.%(ext)s").Replace("\\", "/");

        // Build command with format and output template
        string arguments = $"{_commandBuilder} -f \"{_format}\" -o \"{template}\" \"{SanitizeInput(url)}\"";
        _commandBuilder.Clear(); // Clear after building arguments

        await RunYtdlpAsync(arguments, cancellationToken);
    }

    public async Task ExecuteBatchAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default)
    {
        if (urls == null || !urls.Any())
        {
            _logger.Log(LogType.Error, "No URLs provided for batch download");
            throw new YtdlpException("No URLs provided for batch download");
        }

        // Ensure output folder exists
        try
        {
            Directory.CreateDirectory(_outputFolder);
            _logger.Log(LogType.Info, $"Output folder for batch: {Path.GetFullPath(_outputFolder)}");
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Failed to create output folder {_outputFolder}: {ex.Message}");
            throw new YtdlpException($"Failed to create output folder {_outputFolder}", ex);
        }

        foreach (var url in urls)
        {
            try
            {
                await ExecuteAsync(url, cancellationToken);
            }
            catch (YtdlpException ex)
            {
                _logger.Log(LogType.Error, $"Skipping URL {url} due to error: {ex.Message}");
                continue; // Continue with next URL
            }
        }
    }

    public async Task ExecuteBatchAsync(IEnumerable<string> urls, int maxConcurrency = 3, CancellationToken cancellationToken = default)
    {
        if (urls == null || !urls.Any())
        {
            _logger.Log(LogType.Error, "No URLs provided for batch download");
            throw new YtdlpException("No URLs provided for batch download");
        }

        try
        {
            Directory.CreateDirectory(_outputFolder);
            _logger.Log(LogType.Info, $"Output folder for batch: {Path.GetFullPath(_outputFolder)}");
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Failed to create output folder {_outputFolder}: {ex.Message}");
            throw new YtdlpException($"Failed to create output folder {_outputFolder}", ex);
        }

        using SemaphoreSlim throttler = new(maxConcurrency);

        var tasks = urls.Select(async url =>
        {
            await throttler.WaitAsync();
            try
            {
                await ExecuteAsync(url, cancellationToken);
            }
            catch (YtdlpException ex)
            {
                _logger.Log(LogType.Error, $"Skipping URL {url} due to error: {ex.Message}");
            }
            finally
            {
                throttler.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    #endregion

    #region Private Helpers
    private async Task RunYtdlpAsync(string arguments, CancellationToken cancellationToken = default)
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

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
                throw new YtdlpException("Failed to start yt-dlp process.");

            // Register cancellation
            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        _logger.Log(LogType.Warning, "yt-dlp process killed due to cancellation.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogType.Error, $"Error killing process: {ex.Message}");
                }
            });

            // Read output and errors concurrently
            var outputTask = Task.Run(async () =>
            {
                using var reader = process.StandardOutput;
                string? output;
                while ((output = await reader.ReadLineAsync()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested(); // <- required
                    _progressParser.ParseProgress(output);
                    OnProgress?.Invoke(output);
                }
            }, cancellationToken);

            var errorTask = Task.Run(async () =>
            {
                using var errorReader = process.StandardError;
                string? errorOutput;
                while ((errorOutput = await errorReader.ReadLineAsync()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested(); // <- required
                    OnErrorMessage?.Invoke(this, errorOutput);
                    OnError?.Invoke(errorOutput);
                    _logger.Log(LogType.Error, errorOutput);
                }
            }, cancellationToken);

            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new YtdlpException($"yt-dlp command failed with exit code {process.ExitCode}: {error}");
            }

            var success = process.ExitCode == 0;
            var message = success ? "Process completed successfully." : $"Process failed with exit code {process.ExitCode}.";
            OnCommandCompleted?.Invoke(success, message);
            _logger.Log(success ? LogType.Info : LogType.Error, message);
        }
        catch (OperationCanceledException)
        {
            throw; // Let your caller handle this
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error executing yt-dlp: {ex.Message}";
            OnError?.Invoke(errorMessage);
            _logger.Log(LogType.Error, errorMessage);
            throw new YtdlpException(errorMessage, ex);
        }
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

    private List<VideoFormat> ParseFormats(string result)
    {
        var formats = new List<VideoFormat>();
        if (string.IsNullOrWhiteSpace(result))
        {
            _logger.Log(LogType.Warning, "Empty or null yt-dlp output");
            return formats;
        }

        var lines = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        bool isFormatSection = false;

        foreach (var line in lines)
        {
            _logger.Log(LogType.Debug, $"Parsing line: {line}");

            // Detect format section start
            if (line.Contains("[info] Available formats"))
            {
                isFormatSection = true;
                continue;
            }

            // Skip header or separator lines
            if (!isFormatSection || line.Contains("RESOLUTION") || line.StartsWith("---"))
            {
                continue;
            }

            // Skip empty or invalid lines (basic check for format line structure)
            if (!Regex.IsMatch(line, @"^[^\s]+\s+[^\s]+"))
            {
                _logger.Log(LogType.Debug, $"Stopping format parsing at non-format line: {line}");
                break;
            }

            // Split line by whitespace, preserving structure
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                _logger.Log(LogType.Warning, $"Skipping line (too few parts): {line}");
                continue;
            }

            var format = new VideoFormat();
            int index = 0;

            try
            {
                // Parse ID
                format.ID = parts[index++];

                // Check for duplicate ID
                if (formats.Any(f => f.ID == format.ID))
                {
                    _logger.Log(LogType.Warning, $"Skipping duplicate format ID: {format.ID}");
                    continue;
                }

                // Parse Extension
                format.Extension = parts[index++];

                // Parse Resolution (may include "audio only")
                if (index < parts.Length && parts[index] == "audio" && index + 1 < parts.Length && parts[index + 1] == "only")
                {
                    format.Resolution = "audio only";
                    index += 2;
                }
                else if (index < parts.Length)
                {
                    format.Resolution = parts[index++];
                }
                else
                {
                    _logger.Log(LogType.Warning, $"Skipping line (missing resolution): {line}");
                    continue;
                }

                // Parse FPS (empty for audio-only formats)
                if (format.Resolution != "audio only" && index < parts.Length && Regex.IsMatch(parts[index], @"^\d+$"))
                {
                    format.FPS = parts[index++];
                }

                // Parse Channels (marked by '|' or number)
                if (index < parts.Length && (Regex.IsMatch(parts[index], @"^\d+\|$") || Regex.IsMatch(parts[index], @"^\d+$")))
                {
                    format.Channels = parts[index].TrimEnd('|');
                    index++;
                }

                // Skip first '|' if present
                if (index < parts.Length && parts[index] == "|")
                {
                    index++;
                }

                // Parse FileSize
                if (index < parts.Length && (Regex.IsMatch(parts[index], @"^~?\d+\.\d+MiB$") || parts[index] == ""))
                {
                    format.FileSize = parts[index] == "" ? null : parts[index];
                    index++;
                }

                // Parse TBR
                if (index < parts.Length && Regex.IsMatch(parts[index], @"^\d+k$"))
                {
                    format.TBR = parts[index];
                    index++;
                }

                // Parse Protocol
                if (index < parts.Length && (parts[index] == "https" || parts[index] == "m3u8" || parts[index] == "mhtml"))
                {
                    format.Protocol = parts[index];
                    index++;
                }

                // Skip second '|' if present
                if (index < parts.Length && parts[index] == "|")
                {
                    index++;
                }

                // Parse VCodec
                if (index < parts.Length)
                {
                    if (parts[index] == "audio" && index + 1 < parts.Length && parts[index + 1] == "only")
                    {
                        format.VCodec = "audio only";
                        index += 2;
                    }
                    else if (parts[index] == "images")
                    {
                        format.VCodec = "images";
                        index++;
                    }
                    else if (Regex.IsMatch(parts[index], @"^[a-zA-Z0-9\.]+$"))
                    {
                        format.VCodec = parts[index];
                        index++;
                    }
                }

                // Parse VBR
                if (index < parts.Length && Regex.IsMatch(parts[index], @"^\d+k$"))
                {
                    format.VBR = parts[index];
                    index++;
                }

                // Parse ACodec
                if (index < parts.Length && (Regex.IsMatch(parts[index], @"^[a-zA-Z0-9\.]+$") || parts[index] == "unknown"))
                {
                    format.ACodec = parts[index];
                    index++;
                }

                // Parse ABR
                if (index < parts.Length && Regex.IsMatch(parts[index], @"^\d+k$"))
                {
                    format.ABR = parts[index];
                    index++;
                }

                // Parse ASR
                if (index < parts.Length && Regex.IsMatch(parts[index], @"^\d+k$"))
                {
                    format.ASR = parts[index];
                    index++;
                }

                // Parse MoreInfo (remaining parts)
                if (index < parts.Length)
                {
                    format.MoreInfo = string.Join(" ", parts.Skip(index)).Trim();
                    // Clean up MoreInfo to remove redundant parts
                    if (format.MoreInfo.StartsWith("|"))
                    {
                        format.MoreInfo = format.MoreInfo.Substring(1).Trim();
                    }
                    // For storyboards, ensure MoreInfo is 'storyboard' and ACodec is null
                    if (format.VCodec == "images" && format.MoreInfo != "storyboard")
                    {
                        format.ACodec = null;
                        format.MoreInfo = "storyboard";
                    }
                }

                formats.Add(format);
            }
            catch (Exception ex)
            {
                _logger.Log(LogType.Warning, $"Failed to parse line '{line}': {ex.Message}");
                continue;
            }
        }

        _logger.Log(LogType.Info, $"Parsed {formats.Count} formats");
        return formats;
    }

    private bool IsInPath(string executable)
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        return paths.Any(path => File.Exists(Path.Combine(path, executable)));
    }

    private static string ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("yt-dlp path cannot be empty.", nameof(path));
        return path;
    }

    private static string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        // Escape quotes and other potentially dangerous characters
        return input.Replace("\"", "\\\"").Replace("`", "\\`");
    }
    #endregion
}