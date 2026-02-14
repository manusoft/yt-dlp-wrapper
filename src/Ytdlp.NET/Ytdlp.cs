using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace YtdlpNET;

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
    public event EventHandler<string>? OnProgress;
    public event EventHandler<string>? OnError;
    public event EventHandler<CommandCompletedEventArgs>? OnCommandCompleted;
    public event EventHandler<string>? OnOutputMessage;
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    public event EventHandler<string>? OnCompleteDownload;
    public event EventHandler<string>? OnProgressMessage;
    public event EventHandler<string>? OnErrorMessage;
    public event EventHandler<string>? OnPostProcessingComplete;

    // Valid yt-dlp options for validation
    private static readonly HashSet<string> ValidOptions = new HashSet<string>(StringComparer.Ordinal)
    {
        // ───────── Core ─────────
        "--help","--version","--update","--update-to","--no-update",
        "--config-location","--ignore-config",

        // ───────── Output / Files ─────────
        "--output","-o","--paths","--output-na-placeholder",
        "--restrict-filenames","--windows-filenames",
        "--trim-filenames","--no-overwrites","--force-overwrites",
        "--continue","--no-continue","--part","--no-part",
        "--mtime","--no-mtime",

        // ───────── Format selection ─────────
        "--format","-f","--format-sort","-S",
        "--format-sort-force","--S-force",
        "--format-sort-reset","--no-format-sort-force",
        "--merge-output-format",
        "--prefer-free-formats","--no-prefer-free-formats",
        "--check-formats","--check-all-formats","--no-check-formats",
        "--list-formats","-F",
        "--video-multistreams","--no-video-multistreams",
        "--audio-multistreams","--no-audio-multistreams",

        // ───────── Playlist ─────────
        "--playlist-items","--playlist-start","--playlist-end",
        "--playlist-random","--no-playlist","--yes-playlist",
        "--flat-playlist","--no-flat-playlist","--concat-playlist",
        "--playlist-reverse",

        // ───────── Network / Geo ─────────
        "--proxy","--source-address","--force-ipv4","--force-ipv6",
        "--geo-bypass","--no-geo-bypass",
        "--geo-bypass-country","--geo-bypass-ip-block",
        "--timeout","--socket-timeout",
        "--retries","--fragment-retries",
        "--retry-sleep","--file-access-retries",
        "--http-chunk-size","--limit-rate","--throttled-rate",

        // ───────── Auth / Cookies ─────────
        "--username","--password","--twofactor",
        "--video-password","--netrc","--netrc-location",
        "--cookies","--cookies-from-browser",
        "--add-header","--user-agent","--referer",
        "--age-limit",

        // ───────── Filters ─────────
        "--match-title","--reject-title","--match-filter",
        "--min-filesize","--max-filesize",
        "--date","--datebefore","--dateafter",
        "--download-archive","--force-write-archive",
        "--break-on-existing","--break-per-input",
        "--max-downloads",

        // ───────── Subtitles / Thumbnails ─────────
        "--write-sub","--write-auto-sub",
        "--sub-lang","--sub-langs","--sub-format",
        "--convert-subs","--embed-subs",
        "--write-thumbnail","--write-all-thumbnails",
        "--embed-thumbnail",

        // ───────── Metadata ─────────
        "--write-description","--write-info-json",
        "--write-annotations","--write-chapters",
        "--embed-metadata","--embed-info-json",
        "--embed-chapters","--replace-in-metadata",

        // ───────── Post-processing ─────────
        "--extract-audio","-x",
        "--audio-format","--audio-quality",
        "--recode-video","--remux-video",
        "--postprocessor-args","--ffmpeg-location",
        "--force-keyframes-at-cuts",

        // ───────── Live / Streaming ─────────
        "--live-from-start","--no-live-from-start",
        "--wait-for-video","--wait-for-video-to-end",
        "--hls-use-mpegts","--no-hls-use-mpegts",
        "--downloader","--downloader-args",

        // ───────── SponsorBlock ─────────
        "--sponsorblock-mark","--sponsorblock-remove",
        "--sponsorblock-chapter-title","--sponsorblock-api",

        // ───────── JS / Extractor ─────────
        "--js-runtimes","--remote-components",
        "--extractor-args","--force-generic-extractor",

        // ───────── Debug / Simulation ─────────
        "--simulate","--skip-download",
        "--dump-json","-j",
        "--dump-single-json","-J",
        "--print","--print-to-file",
        "--quiet","--no-warnings","--verbose",
        "--newline","--progress","--no-progress",
        "--console-title","--write-log",

         // ───────── Misc ─────────
        "--call-home","--write-pages","--write-link",
        "--sleep-interval","--min-sleep-interval",
        "--max-sleep-interval","--sleep-subtitles",
        "--no-color", "--abort-on-error",
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
    public Ytdlp WithConcurrentFragments(int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        _commandBuilder.Append($"--concurrent-fragments {count} ");
        return this;
    }

    public Ytdlp RemoveSponsorBlock(params string[] categories)
    {
        // categories: sponsor, intro, outro, selfpromo, preview, filler, interaction, music_offtopic, poi_highlight, all
        var cats = categories.Length == 0 ? "all" : string.Join(",", categories);
        _commandBuilder.Append($"--sponsorblock-remove {SanitizeInput(cats)} ");
        return this;
    }

    public Ytdlp EmbedSubtitles(string languages = "all", string? convertTo = null)
    {
        _commandBuilder.Append($"--write-subs --sub-langs {SanitizeInput(languages)} ");
        if (!string.IsNullOrEmpty(convertTo))
            _commandBuilder.Append($"--convert-subs {SanitizeInput(convertTo)} ");
        if (convertTo?.Equals("embed", StringComparison.OrdinalIgnoreCase) == true)
            _commandBuilder.Append("--embed-subs ");
        return this;
    }

    public Ytdlp CookiesFromBrowser(string browser, string? profile = null)
    {
        var arg = profile != null ? $"{browser}:{profile}" : browser;
        _commandBuilder.Append($"--cookies-from-browser {SanitizeInput(arg)} ");
        return this;
    }

    public Ytdlp GeoBypassCountry(string countryCode)
    {
        if (countryCode.Length != 2)
            throw new ArgumentException("Country code must be 2 letters.", nameof(countryCode));
        _commandBuilder.Append($"--geo-bypass-country {SanitizeInput(countryCode.ToUpperInvariant())} ");
        return this;
    }

    public Ytdlp Version()
    {
        _commandBuilder.Append("--version ");
        return this;
    }

    public Ytdlp Update()
    {
        _commandBuilder.Append("--update ");
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

    public Ytdlp SetOutputFolder([Required] string outputFolderPath)
    {
        if (string.IsNullOrWhiteSpace(outputFolderPath))
            throw new ArgumentException("Output folder path cannot be empty.", nameof(outputFolderPath));
        _outputFolder = outputFolderPath;
        return this;
    }

    public Ytdlp SetTempFolder([Required] string tempFolderPath)
    {
        if (string.IsNullOrWhiteSpace(tempFolderPath))
            throw new ArgumentException("Temporary folder path cannot be empty.", nameof(tempFolderPath));
        _commandBuilder.Append($"--paths temp:{SanitizeInput(tempFolderPath)} ");
        return this;
    }

    public Ytdlp SetHomeFolder([Required] string homeFolderPath)
    {
        if (string.IsNullOrWhiteSpace(homeFolderPath))
            throw new ArgumentException("Home folder path cannot be empty.", nameof(homeFolderPath));
        _commandBuilder.Append($"--paths home:{SanitizeInput(homeFolderPath)} ");
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

        var parts = customCommand
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizeInput)
            .ToArray();

        // Validate only option tokens (flags)
        foreach (var part in parts)
        {
            if (part.StartsWith("-", StringComparison.Ordinal) && !IsAllowedOption(part))
            {
                var errorMessage = $"Invalid yt-dlp option: {part}";
                OnError?.Invoke(this, errorMessage);
                _logger.Log(LogType.Error, errorMessage);
                return this;
            }
        }

        _commandBuilder.Append(' ').Append(string.Join(' ', parts));

        return this;
    }

    public Ytdlp SetTimeout(TimeSpan timeout)
    {
        if (timeout.TotalSeconds <= 0)
            throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));
        _commandBuilder.Append($"--timeout {timeout.TotalSeconds} ");
        return this;
    }

    public Ytdlp SetFFMpegLocation([Required] string ffmpegFolder)
    {
        if (string.IsNullOrWhiteSpace(ffmpegFolder))
            throw new ArgumentException("FFmpeg path cannot be empty.", nameof(ffmpegFolder));
        _commandBuilder.Append($"--ffmpeg-location {SanitizeInput(ffmpegFolder)} ");
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
            var process = CreateProcess($"--version");

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

    public async Task<string> UpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var process = CreateProcess("-U");

            process.Start();

            // Read output and error concurrently
            var readOutputTask = process.StandardOutput.ReadToEndAsync();
            var readErrorTask = process.StandardError.ReadToEndAsync();

            using (cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(true);
                }
                catch { }
            }))
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            var output = await readOutputTask;
            var error = await readErrorTask;

            // Log both
            if (!string.IsNullOrWhiteSpace(output))
                _logger.Log(LogType.Info, output.Trim());
            if (!string.IsNullOrWhiteSpace(error))
                _logger.Log(LogType.Error, error.Trim());

            // Analyze output for professional messages
            if (output.Contains("Updated", StringComparison.OrdinalIgnoreCase))
                return "yt-dlp was successfully updated to the latest version.";

            if (output.Contains("up to date", StringComparison.OrdinalIgnoreCase))
                return "yt-dlp is already up to date.";

            return "yt-dlp update check completed (no changes detected).";
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Error updating yt-dlp: {ex.Message}");
            return $"yt-dlp update failed: {ex.Message}";
        }
    }

    public async Task<Metadata?> GetVideoMetadataJsonAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        try
        {
            var process = CreateProcess($"--dump-single-json --skip-download --no-playlist --no-warnings --quiet {SanitizeInput(url)}");

            process.Start();

            // Read standard output asynchronously
            var readOutputTask = process.StandardOutput.ReadToEndAsync();

            // Drain stderr so it never blocks (we don't await unless needed)
            //var readErrorTask = Task.Run(() => process.StandardError.ReadToEndAsync());

            // Wait for process to exit, observing cancellation
            using (cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(true); // forcefully kill process
                }
                catch { /* ignore if already exited */ }
            }))
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            // Get stdout result
            var output = await readOutputTask;

            // Optionally capture errors for logging
            //var errorOutput = await readErrorTask;
            //if (!string.IsNullOrWhiteSpace(errorOutput))
            //    _logger.Log(LogType.Debug, $"yt-dlp stderr: {errorOutput}");

            _logger.Log(LogType.Info, $"Get Format: {output}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<Metadata>(output, options);
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

    public async Task<SimpleMetadata?> GetSimpleMetadataAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        try
        {
            var arguments =
                $"--skip-download --no-playlist --quiet " +
                $"--print \"%(id)s|||%(title)s|||%(duration)s|||%(thumbnail)s|||%(view_count)s|||%(filesize,filesize_approx)s|||%(description)s\" " +
                $"{SanitizeInput(url)}";

            var process = CreateProcess(arguments);

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();

            using (cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(true);
                }
                catch { }
            }))
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            var output = await outputTask;

            if (string.IsNullOrWhiteSpace(output))
                return null;

            var parts = output
                .Trim()
                .Split("|||", StringSplitOptions.None);

            var metadata = new SimpleMetadata
            {
                Id = parts.ElementAtOrDefault(0),
                Title = parts.ElementAtOrDefault(1),
                Duration = ParseDouble(parts.ElementAtOrDefault(2)),
                Thumbnail = parts.ElementAtOrDefault(3),
                ViewCount = ParseLong(parts.ElementAtOrDefault(4)),
                FileSize = ParseLong(parts.ElementAtOrDefault(5)),
                Description = parts.ElementAtOrDefault(6)
            };

            return metadata;
        }
        catch (Exception ex)
        {
            throw new YtdlpException($"Failed to fetch simple metadata: {ex.Message}", ex);
        }
    }

    private static double? ParseDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "NA")
            return null;

        if (double.TryParse(value,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var result))
            return result;

        return null;
    }

    private static long? ParseLong(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "NA")
            return null;

        if (long.TryParse(value,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var result))
            return result;

        return null;
    }



    public async Task<List<Format>> GetAvailableFormatsAsync(string videoUrl, CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Gets detailed format information using --dump-single-json for richer metadata (height/width separate, exact bitrates, filesize, etc.).
    /// Falls back to text-based -F parsing if JSON fails or returns no formats.
    /// </summary>
    /// <param name="url">The video/URL to query</param>
    /// <param name="cancellationToken">Cancellation support</param>
    /// <returns>List of enriched Format objects, sorted roughly by quality (as returned by yt-dlp)</returns>
    /// <exception cref="YtdlpException">On execution or parsing failure (unless fallback succeeds)</exception>
    public async Task<List<Format>> GetFormatsDetailedAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        try
        {
            // Execute yt-dlp --dump-single-json
            var arguments = $"--dump-single-json {SanitizeInput(url)}";
            var process = CreateProcess(arguments);

            process.Start();

            var readOutputTask = process.StandardOutput.ReadToEndAsync();

            using (cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(true);
                }
                catch { }
            }))
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            var jsonOutput = await readOutputTask;

            if (string.IsNullOrWhiteSpace(jsonOutput))
            {
                _logger.Log(LogType.Warning, "Empty JSON output from --dump-single-json");
                throw new YtdlpException("No data returned from format query.");
            }

            // JSON options
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Deserialize
            var videoInfo = JsonSerializer.Deserialize<SingleVideoJson>(jsonOutput, jsonOptions);

            if (videoInfo?.Formats == null || !videoInfo.Formats.Any())
            {
                _logger.Log(LogType.Warning, "No formats array in JSON or empty → falling back to -F");
                return await GetAvailableFormatsAsync(url, cancellationToken);
            }

            // Map in one clean pass — no modification during enumeration
            var detailedFormats = new List<Format>(videoInfo.Formats.Count);

            foreach (var f in videoInfo.Formats)
            {
                if (string.IsNullOrEmpty(f.FormatId))
                    continue;

                var fmt = new Format
                {
                    Id = f.FormatId!,
                    Extension = f.Ext ?? string.Empty,
                    Height = f.Height,
                    Width = f.Width,
                    // Build resolution fallback
                    Resolution = !string.IsNullOrEmpty(f.Resolution)
                                            ? f.Resolution
                                            : (f.Height.HasValue ? $"{f.Height}p" : "audio only"),
                    Fps = f.Fps,
                    Channels = f.AudioChannels?.ToString(),
                    AudioSampleRate = f.Asr,
                    TotalBitrate = f.Tbr?.ToString(CultureInfo.InvariantCulture),
                    VideoBitrate = f.Vbr?.ToString(CultureInfo.InvariantCulture),
                    AudioBitrate = f.Abr?.ToString(CultureInfo.InvariantCulture),
                    VideoCodec = f.Vcodec == "none" ? null : f.Vcodec,
                    AudioCodec = f.Acodec == "none" ? null : f.Acodec,
                    Protocol = f.Protocol,
                    Language = f.Language,
                    FileSizeApprox = f.FilesizeApprox?.ToString("N0") ?? f.Filesize?.ToString("N0"),
                    ApproxFileSizeBytes = f.FilesizeApprox ?? f.Filesize,
                    Note = f.FormatNote,
                    MoreInfo = f.FormatNote,
                };

                detailedFormats.Add(fmt);
            }

            if (detailedFormats.Count > 0)
            {
                _logger.Log(LogType.Info, $"Successfully parsed {detailedFormats.Count} detailed formats from JSON");
                return detailedFormats;
            }

            _logger.Log(LogType.Warning, "JSON parsed but no valid formats after filtering → fallback");
        }
        catch (JsonException jex)
        {
            _logger.Log(LogType.Warning, $"JSON deserialization failed: {jex.Message} → falling back");
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Format fetch cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Unexpected error in GetFormatsDetailedAsync: {ex.Message} → fallback");
        }

        // Ultimate fallback
        return await GetAvailableFormatsAsync(url, cancellationToken);
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
        using var process = CreateProcess(arguments);

        try
        {
            if (!process.Start())
                throw new YtdlpException("Failed to start yt-dlp process.");

            // Improved cancellation: Try to close streams first, then kill
            using var ctsRegistration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        _logger.Log(LogType.Info, "yt-dlp process killed due to cancellation");
                    }
                }
                catch
                {
                    // silent - already dead or disposed
                }
            });

            // Read output and error concurrently
            var outputTask = Task.Run(async () =>
            {
                string? line;
                while ((line = await process.StandardOutput.ReadLineAsync()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _progressParser.ParseProgress(line);
                    OnProgress?.Invoke(this, line);
                }
            }, cancellationToken);

            var errorTask = Task.Run(async () =>
            {
                string? line;
                while ((line = await process.StandardError.ReadLineAsync()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    OnErrorMessage?.Invoke(this, line);
                    OnError?.Invoke(this, line);
                    _logger.Log(LogType.Error, line);
                }
            }, cancellationToken);

            await Task.WhenAll(outputTask, errorTask);

            // Wait for exit (may throw OperationCanceledException)
            await process.WaitForExitAsync(cancellationToken);

            // Only throw on real failure (not cancellation)
            if (process.ExitCode != 0 && !cancellationToken.IsCancellationRequested)
            {
                throw new YtdlpException($"yt-dlp exited with code {process.ExitCode}");
            }

            // Success or intentional cancel
            var success = !cancellationToken.IsCancellationRequested;
            var message = success ? "Completed successfully" : "Cancelled by user";
            OnCommandCompleted?.Invoke(this, new CommandCompletedEventArgs(success, message));
        }
        catch (OperationCanceledException)
        {
            // Normal cancel path — no need to log again
            OnCommandCompleted?.Invoke(this, new CommandCompletedEventArgs(false, "Cancelled by user"));
            throw; // let caller handle if needed
        }
        catch (Exception ex)
        {
            var msg = $"Error executing yt-dlp: {ex.Message}";
            OnError?.Invoke(this, msg);
            _logger.Log(LogType.Error, msg);
            throw new YtdlpException(msg, ex);
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

    private List<Format> ParseFormats(string result)
    {
        var formats = new List<Format>();
        if (string.IsNullOrWhiteSpace(result)) return formats;

        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        bool inFormatSection = false;

        foreach (var line in lines)
        {
            if (line.Contains("[info] Available formats")) { inFormatSection = true; continue; }
            if (!inFormatSection || line.Contains("RESOLUTION") || line.StartsWith("---")) continue;
            if (string.IsNullOrWhiteSpace(line) || !Regex.IsMatch(line, @"^\S+\s+\S+")) break;

            try
            {
                var format = Format.FromParsedLine(line);
                if (!string.IsNullOrEmpty(format.Id) && !formats.Exists(f => f.Id == format.Id))
                    formats.Add(format);
            }
            catch (Exception ex)
            {
                _logger.Log(LogType.Warning, $"Failed parsing format line: {line} → {ex.Message}");
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

    private static bool IsAllowedOption(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
            return false;

        // Known safe options
        if (ValidOptions.Contains(arg))
            return true;

        // Forward-compatible: allow unknown yt-dlp flags
        if (arg.StartsWith("--", StringComparison.Ordinal))
            return true;

        // Short flags (-f, -S, -x)
        if (arg.StartsWith("-", StringComparison.Ordinal))
            return true;

        return false;
    }

    #endregion
}