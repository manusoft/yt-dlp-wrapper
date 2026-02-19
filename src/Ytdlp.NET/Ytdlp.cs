using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace YtdlpNET;

/// <summary>
/// Fluent wrapper for yt-dlp, providing methods to build commands, fetch metadata,
/// and execute downloads with progress tracking and event support.
/// </summary>
/// <remarks>
/// <strong>NOT THREAD-SAFE</strong> — do not share the same instance across threads or concurrent operations.
/// For parallel/batch downloads, create a new <see cref="Ytdlp"/> instance for each task.
///
/// Example of safe concurrent usage:
/// <code>
/// var tasks = urls.Select(u => new Ytdlp().SetFormat("best").ExecuteAsync(u));
/// await Task.WhenAll(tasks);
/// </code>
///
/// <strong>Disposal</strong>: This class does not currently implement <see cref="IDisposable"/>.
/// Resource cleanup (e.g. child processes) is handled internally. Proper disposal support
/// and an immutable builder pattern are planned for a future version.
/// </remarks>
public sealed class Ytdlp
{
    private readonly string _ytDlpPath;
    private readonly StringBuilder _commandBuilder = new();
    private readonly ProgressParser _progressParser;
    private readonly ILogger _logger;

    private string _format = "best";
    private string _outputFolder = ".";
    private string _outputTemplate = "%(title)s.%(ext)s";

    // <summary>
    /// Fired for general progress messages from yt-dlp output.
    /// </summary>
    public event EventHandler<string>? OnProgress;


    /// <summary>
    /// Fired when an error message is received from yt-dlp.
    /// </summary>
    public event EventHandler<string>? OnError;

    /// <summary>
    /// Fired when the yt-dlp process completes (success or failure/cancel).
    /// </summary>
    public event EventHandler<CommandCompletedEventArgs>? OnCommandCompleted;

    /// <summary>
    /// Fired for every output line from yt-dlp (stdout).
    /// </summary>
    public event EventHandler<string>? OnOutputMessage;

    /// <summary>
    /// Fired when download progress updates are parsed (percentage, speed, ETA).
    /// </summary>
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;

    /// <summary>
    /// Fired when a single download completes successfully.
    /// </summary>
    public event EventHandler<string>? OnCompleteDownload;

    /// <summary>
    /// Fired for informational progress messages (e.g. merging, extracting).
    /// </summary>
    public event EventHandler<string>? OnProgressMessage;

    /// <summary>
    /// Fired for error messages from yt-dlp.
    /// </summary>
    public event EventHandler<string>? OnErrorMessage;

    /// <summary>
    /// Fired when post-processing (e.g. merging, conversion) completes.
    /// </summary>
    public event EventHandler<string>? OnPostProcessingComplete;

    // Valid options set (used for custom command validation)
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
        "--no-color", "--abort-on-error", "--concurrent-fragments",
    };

    #region Constructor & Initialization

    /// <summary>
    /// Initializes a new instance of the <see cref="Ytdlp"/> class.
    /// </summary>
    /// <param name="ytdlpPath">Path to the yt-dlp executable (default: "yt-dlp").</param>
    /// <param name="logger">Optional logger instance (defaults to <see cref="DefaultLogger"/>).</param>
    /// <exception cref="YtdlpException">Thrown if yt-dlp executable is not found.</exception>
    public Ytdlp(string ytdlpPath = "yt-dlp", ILogger? logger = null)
    {
        _ytDlpPath = ValidatePath(ytdlpPath);
        if (!File.Exists(_ytDlpPath) && !IsInPath(_ytDlpPath))
            throw new YtdlpException($"yt-dlp executable not found at {_ytDlpPath}. Install yt-dlp or specify a valid path.");
        _commandBuilder = new StringBuilder();
        _progressParser = new ProgressParser(logger);
        _logger = logger ?? new DefaultLogger();

        // Subscribe to progress parser events
        _progressParser.OnOutputMessage += (s, e) => OnOutputMessage?.Invoke(this, e);
        _progressParser.OnProgressDownload += (s, e) => OnProgressDownload?.Invoke(this, e);
        _progressParser.OnCompleteDownload += (s, e) => OnCompleteDownload?.Invoke(this, e);
        _progressParser.OnProgressMessage += (s, e) => OnProgressMessage?.Invoke(this, e);
        _progressParser.OnErrorMessage += (s, e) => OnErrorMessage?.Invoke(this, e);
        _progressParser.OnPostProcessingComplete += (s, e) => OnPostProcessingComplete?.Invoke(this, e);
    }

    #endregion

    #region Output & Path Configuration

    /// <summary>
    /// Sets the output folder for downloaded files.
    /// </summary>
    /// <param name="outputFolderPath">The target output directory.</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if path is empty.</exception>
    public Ytdlp SetOutputFolder([Required] string outputFolderPath)
    {
        if (string.IsNullOrWhiteSpace(outputFolderPath))
            throw new ArgumentException("Output folder path cannot be empty.", nameof(outputFolderPath));

        _outputFolder = outputFolderPath;
        return this;
    }

    /// <summary>
    /// Sets the temporary folder path used by yt-dlp.
    /// </summary>
    /// <param name="tempFolderPath">Path to temporary folder.</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if path is empty.</exception>
    public Ytdlp SetTempFolder([Required] string tempFolderPath)
    {
        if (string.IsNullOrWhiteSpace(tempFolderPath))
            throw new ArgumentException("Temporary folder path cannot be empty.", nameof(tempFolderPath));

        _commandBuilder.Append($"--paths temp:{SanitizeInput(tempFolderPath)} ");
        return this;
    }

    /// <summary>
    /// Sets the home folder path used by yt-dlp.
    /// </summary>
    /// <param name="homeFolderPath">Path to home folder.</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if path is empty.</exception>
    public Ytdlp SetHomeFolder([Required] string homeFolderPath)
    {
        if (string.IsNullOrWhiteSpace(homeFolderPath))
            throw new ArgumentException("Home folder path cannot be empty.", nameof(homeFolderPath));

        _commandBuilder.Append($"--paths home:{SanitizeInput(homeFolderPath)} ");
        return this;
    }

    /// <summary>
    /// Specifies the location of FFmpeg executable.
    /// </summary>
    /// <param name="ffmpegFolder">Path to ffmpeg executable or folder.</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if path is empty.</exception>
    public Ytdlp SetFFmpegLocation([Required] string ffmpegFolder)
    {
        if (string.IsNullOrWhiteSpace(ffmpegFolder))
            throw new ArgumentException("FFmpeg folder cannot be empty.", nameof(ffmpegFolder));

        _commandBuilder.Append($"--ffmpeg-location {SanitizeInput(ffmpegFolder)} ");
        return this;
    }

    /// <summary>
    /// Sets the output filename template.
    /// </summary>
    /// <param name="template">Template string (e.g. "%(title)s.%(ext)s").</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if template is empty.</exception>
    public Ytdlp SetOutputTemplate([Required] string template)
    {
        if (string.IsNullOrWhiteSpace(template))
            throw new ArgumentException("Output template cannot be empty.", nameof(template));

        _outputTemplate = template.Replace("\\", "/").Trim();
        return this;
    }

    #endregion

    #region Format Selection & Extraction

    /// <summary>
    /// Sets the format selector string passed to -f/--format.
    /// </summary>
    /// <param name="format">Format string (e.g. "best", "137+251", "bv*+ba").</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp SetFormat([Required] string format)
    {
        _format = format;
        return this;
    }

    /// <summary>
    /// Configures audio-only extraction with the specified format.
    /// </summary>
    /// <param name="audioFormat">Audio format (e.g. "mp3", "m4a", "best").</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if format is empty.</exception>
    public Ytdlp ExtractAudio(string audioFormat)
    {
        if (string.IsNullOrWhiteSpace(audioFormat))
            throw new ArgumentException("Audio format cannot be empty.", nameof(audioFormat));

        _commandBuilder.Append($"--extract-audio --audio-format {SanitizeInput(audioFormat)} ");
        return this;
    }

    /// <summary>
    /// Limits video resolution by height (uses bestvideo[height<=...]).
    /// </summary>
    /// <param name="resolution">Max height (e.g. "1080", "720").</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if resolution is empty.</exception>
    public Ytdlp SetResolution(string resolution)
    {
        if (string.IsNullOrWhiteSpace(resolution))
            throw new ArgumentException("Resolution cannot be empty.", nameof(resolution));

        _commandBuilder.Append($"--format \"bestvideo[height<={SanitizeInput(resolution)}]\" ");
        return this;
    }

    #endregion

    #region Metadata & Format Fetching

    /// <summary>
    /// Appends --version to the command (useful for preview or testing).
    /// </summary>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp Version()
    {
        _commandBuilder.Append("--version ");
        return this;
    }

    /// <summary>
    /// Appends --update to the command (useful for preview or testing).
    /// </summary>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp Update()
    {
        _commandBuilder.Append("--update ");
        return this;
    }

    /// <summary>
    /// Appends --write-info-json to save metadata as JSON file.
    /// </summary>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp WriteMetadataToJson()
    {
        _commandBuilder.Append("--write-info-json ");
        return this;
    }

    /// <summary>
    /// Appends --dump-json (simulate and output metadata only).
    /// </summary>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp ExtractMetadataOnly()
    {
        _commandBuilder.Append("--dump-json ");
        return this;
    }

    #endregion

    #region Download & Post-Processing Options

    /// <summary>
    /// Embeds metadata into the output file.
    /// </summary>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp EmbedMetadata()
    {
        _commandBuilder.Append("--embed-metadata ");
        return this;
    }

    /// <summary>
    /// Embeds thumbnail into the output file.
    /// </summary>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp EmbedThumbnail()
    {
        _commandBuilder.Append("--embed-thumbnail ");
        return this;
    }

    /// <summary>
    /// Downloads thumbnails as separate files.
    /// </summary>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp DownloadThumbnails()
    {
        _commandBuilder.Append("--write-thumbnail ");
        return this;
    }

    /// <summary>
    /// Downloads subtitles in the specified languages.
    /// </summary>
    /// <param name="languages">Language codes (default: "all").</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if languages is empty.</exception>
    public Ytdlp DownloadSubtitles(string languages = "all")
    {
        _commandBuilder.Append($"--write-sub --sub-langs {SanitizeInput(languages)} ");
        return this;
    }


    public Ytdlp DownloadLivestream(bool fromStart = true)
    {
        _commandBuilder.Append(fromStart ? "--live-from-start " : "--no-live-from-start ");
        return this;
    }

    public Ytdlp DownloadLiveStreamRealTime()
    {
        _commandBuilder.Append("--live-from-start --recode-video mp4 ");
        return this;
    }

    public Ytdlp DownloadSections(string timeRanges)
    {
        if (string.IsNullOrWhiteSpace(timeRanges))
            throw new ArgumentException("Time ranges cannot be empty.", nameof(timeRanges));

        _commandBuilder.Append($"--download-sections {SanitizeInput(timeRanges)} ");
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

    public Ytdlp MergePlaylistIntoSingleVideo(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Format cannot be empty.", nameof(format));

        _commandBuilder.Append($"--merge-output-format {SanitizeInput(format)} ");
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

    /// <summary>
    /// Keeps temporary/intermediate files after processing.
    /// </summary>
    /// <param name="keep">True to keep temp files.</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp SetKeepTempFiles(bool keep)
    {
        if (keep) _commandBuilder.Append("-k");
        return this;
    }

    public Ytdlp SetDownloadTimeout(string timeout)
    {
        if (string.IsNullOrWhiteSpace(timeout))
            throw new ArgumentException("Timeout cannot be empty.", nameof(timeout));

        _commandBuilder.Append($"--download-timeout {SanitizeInput(timeout)} ");
        return this;
    }

    public Ytdlp SetTimeout(TimeSpan timeout)
    {
        if (timeout.TotalSeconds <= 0)
            throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));

        _commandBuilder.Append($"--timeout {timeout.TotalSeconds} ");
        return this;
    }

    /// <summary>
    /// Sets number of retries for failed downloads/fragments.
    /// </summary>
    /// <param name="retries">Retry count or "infinite".</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp SetRetries(string retries)
    {
        _commandBuilder.Append($"--retries {SanitizeInput(retries)} ");
        return this;
    }

    /// <summary>
    /// Limits download speed (e.g. "500K", "1M").
    /// </summary>
    /// <param name="rate">Rate limit string.</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp SetDownloadRate(string rate)
    {
        _commandBuilder.Append($"--limit-rate {SanitizeInput(rate)} ");
        return this;
    }

    /// <summary>
    /// Skips already downloaded files using an archive.
    /// </summary>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp SkipDownloaded()
    {
        _commandBuilder.Append("--download-archive downloaded.txt "); return this;
    }

    #endregion

    #region Authentication & Security

    /// <summary>
    /// Sets username and password for authentication.
    /// </summary>
    /// <param name="username">Username or email.</param>
    /// <param name="password">Password.</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp SetAuthentication(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Username and password cannot be empty.");

        _commandBuilder.Append($"--username {SanitizeInput(username)} --password {SanitizeInput(password)} ");
        return this;
    }

    /// <summary>
    /// Loads cookies from a file.
    /// </summary>
    /// <param name="cookieFile">Path to cookies file (Netscape format).</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp UseCookies(string cookieFile)
    {
        if (string.IsNullOrWhiteSpace(cookieFile))
            throw new ArgumentException("Cookie file path cannot be empty.", nameof(cookieFile));

        _commandBuilder.Append($"--cookies {SanitizeInput(cookieFile)} ");
        return this;
    }

    /// <summary>
    /// Adds a custom HTTP header.
    /// </summary>
    /// <param name="header">Header name (e.g. "Referer").</param>
    /// <param name="value">Header value.</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp SetCustomHeader(string header, string value)
    {
        if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Header and value cannot be empty.");

        _commandBuilder.Append($"--add-header \"{SanitizeInput(header)}:{SanitizeInput(value)}\" ");
        return this;
    }

    #endregion

    #region Network & Headers

    /// <summary>
    /// Sets custom User-Agent header.
    /// </summary>
    /// <param name="userAgent">User-Agent string.</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp SetUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("User agent cannot be empty.", nameof(userAgent));

        _commandBuilder.Append($"--user-agent {SanitizeInput(userAgent)} ");
        return this;
    }

    /// <summary>
    /// Sets custom Referer header.
    /// </summary>
    /// <param name="referer">Referer URL.</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp SetReferer(string referer)
    {
        if (string.IsNullOrWhiteSpace(referer))
            throw new ArgumentException("Referer URL cannot be empty.", nameof(referer));

        _commandBuilder.Append($"--referer {SanitizeInput(referer)} ");
        return this;
    }

    /// <summary>
    /// Uses a proxy server for all requests.
    /// </summary>
    /// <param name="proxy">Proxy URL (e.g. "http://host:port").</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp UseProxy(string proxy)
    {
        if (string.IsNullOrWhiteSpace(proxy))
            throw new ArgumentException("Proxy URL cannot be empty.", nameof(proxy));

        _commandBuilder.Append($"--proxy {SanitizeInput(proxy)} ");
        return this;
    }

    /// <summary>
    /// Disables advertisements where supported.
    /// </summary>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp DisableAds()
    {
        _commandBuilder.Append("--no-ads ");
        return this;
    }

    #endregion

    #region Playlist & Selection

    public Ytdlp SelectPlaylistItems(string items)
    {
        if (string.IsNullOrWhiteSpace(items))
            throw new ArgumentException("Playlist items cannot be empty.", nameof(items));

        _commandBuilder.Append($"--playlist-items {SanitizeInput(items)} ");
        return this;
    }

    #endregion

    #region Logging & Simulation

    /// <summary>
    /// Writes yt-dlp log output to a file.
    /// </summary>
    /// <param name="logFile">Path to log file.</param>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp LogToFile(string logFile)
    {
        if (string.IsNullOrWhiteSpace(logFile))
            throw new ArgumentException("Log file path cannot be empty.", nameof(logFile));

        _commandBuilder.Append($"--write-log {SanitizeInput(logFile)} ");
        return this;
    }

    /// <summary>
    /// Simulates download without saving files.
    /// </summary>
    /// <returns>The current <see cref="Ytdlp"/> instance for chaining.</returns>
    public Ytdlp Simulate()
    {
        _commandBuilder.Append("--simulate ");
        return this;
    }

    #endregion

    #region Advanced & Specialized Options

    public Ytdlp WithConcurrentFragments(int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        _commandBuilder.Append($"--concurrent-fragments {count} ");
        return this;
    }

    public Ytdlp RemoveSponsorBlock(params string[] categories)
    {
        var cats = categories.Length == 0 ? "all" : string.Join(",", categories);
        _commandBuilder.Append($"--sponsorblock-remove {SanitizeInput(cats)} ");
        return this;
    }

    public Ytdlp EmbedSubtitles(string languages = "all", string? convertTo = null)
    {
        _commandBuilder.Append($"--write-subs --sub-langs {SanitizeInput(languages)} ");
        if (!string.IsNullOrEmpty(convertTo)) _commandBuilder.Append($"--convert-subs {SanitizeInput(convertTo)} ");
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
        if (countryCode.Length != 2) throw new ArgumentException("Country code must be 2 letters.");
        _commandBuilder.Append($"--geo-bypass-country {SanitizeInput(countryCode.ToUpperInvariant())} ");
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

    #endregion

    #region Execution & Utility Methods

    /// <summary>
    /// Returns the current command string (for preview/debug).
    /// </summary>
    /// <returns>The built command line arguments.</returns>
    public string PreviewCommand()
    {
        return _commandBuilder.ToString().Trim();
    }

    public async Task<string> GetVersionAsync(CancellationToken ct = default)
    {
        var process = CreateProcess($"--version");
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();

        using (ct.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch { }
        }))
        {
            await process.WaitForExitAsync(ct);
        }

        var output = await outputTask;
        string version = output.Trim();
        _logger.Log(LogType.Info, $"yt-dlp version: {version}");
        return version;
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

    public async Task<Metadata?> GetVideoMetadataJsonAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        try
        {
            var arguments =
                $"--dump-single-json " +
                $"--no-simulate " +
                $"--skip-download " +
                $"--no-playlist " +
                $"--quiet " +
                $"--no-warnings " +
                $"{SanitizeInput(url)}";

            var process = CreateProcess(arguments);
            process.Start();

            // Use StreamReader with large buffer + explicit UTF-8
            string json;
            using (var reader = new StreamReader(
                process.StandardOutput.BaseStream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 8192,           // good default for JSON
                leaveOpen: true))           // don't close underlying stream
            {
                json = await reader.ReadToEndAsync();
            }

            // Optional: drain stderr in background (prevents blocking if warnings are many)
            _ = process.StandardError.ReadToEndAsync();

            using (ct.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(true); }
                catch { }
            }))
            {
                await process.WaitForExitAsync(ct);
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.Log(LogType.Warning, "Empty JSON output.");
                return null;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            return JsonSerializer.Deserialize<Metadata>(json, options);
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Metadata fetch cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Warning, $"Metadata fetch failed: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Format>> GetFormatsDetailedAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        try
        {
            var arguments =
                $"--dump-single-json " +
                $"--no-simulate " +
                $"--skip-download " +
                $"--no-playlist " +
                $"--quiet " +
                $"--no-warnings " +
                $"{SanitizeInput(url)}";

            var process = CreateProcess(arguments);
            process.Start();

            //var outputTask = process.StandardOutput.ReadToEndAsync();
            // Use StreamReader with large buffer + explicit UTF-8
            string json;
            using (var reader = new StreamReader(
                process.StandardOutput.BaseStream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 8192,           // good default for JSON
                leaveOpen: true))           // don't close underlying stream
            {
                json = await reader.ReadToEndAsync();
            }

            // Optional: drain stderr in background (prevents blocking if warnings are many)
            _ = process.StandardError.ReadToEndAsync();

            using (ct.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(true);
                }
                catch { }
            }))
            {
                await process.WaitForExitAsync(ct);
            }

            //var jsonOutput = await outputTask;

            if (string.IsNullOrWhiteSpace(json))
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
            var videoInfo = JsonSerializer.Deserialize<SingleVideoJson>(json, jsonOptions);

            if (videoInfo?.Formats == null || !videoInfo.Formats.Any())
            {
                _logger.Log(LogType.Warning, "No formats array in JSON or empty → falling back to -F");
                return await GetAvailableFormatsAsync(url, ct);
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
        return await GetAvailableFormatsAsync(url, ct);
    }

    public async Task<List<Format>> GetAvailableFormatsAsync(string videoUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be empty.", nameof(videoUrl));

        var process = CreateProcess($"-F {SanitizeInput(videoUrl)}");
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();

        using (ct.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch { }
        }))
        {
            await process.WaitForExitAsync(ct);
        }

        var output = await outputTask;

        _logger.Log(LogType.Info, $"Get Format: {output}");
        return ParseFormats(output);
    }

    /// <summary>
    /// Quickly fetches a lightweight set of metadata using --print (very fast, single call).
    /// Uses a custom separator to avoid parsing issues with Unicode/special characters.
    /// </summary>
    /// <param name="url">Video or playlist URL</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>SimpleMetadata object or null if parsing failed or no data</returns>
    public async Task<SimpleMetadata?> GetSimpleMetadataAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        try
        {
            // Use a rare separator that is unlikely to appear in title/description
            const string separator = "|||YTDLP.NET|||";

            var fields = new[]
            {
                "%(id)s",
                "%(title)s",
                "%(duration)s",
                "%(thumbnail)s",
                "%(view_count)s",
                "%(filesize,filesize_approx)s",
                "%(description).500s"  // limit to first 500 chars to avoid huge output
            };

            var printArg = $"--print \"{string.Join(separator, fields)}\"";

            var arguments = $"{printArg} --skip-download --no-playlist --quiet {SanitizeInput(url)}";

            var process = CreateProcess(arguments);
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();

            using (ct.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(true);
                }
                catch { }
            }))
            {
                await process.WaitForExitAsync(ct);
            }

            var output = await outputTask;

            if (string.IsNullOrWhiteSpace(output))
                return null;

            var parts = output.Trim().Split(separator);

            if (parts.Length < 6) // at least id, title, duration, thumbnail, views, size
                return null;

            return new SimpleMetadata
            {
                Id = parts[0].Trim(),
                Title = parts[1].Trim(),
                Duration = double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var dur) ? dur : null,
                Thumbnail = parts[3].Trim(),
                ViewCount = long.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var views) ? views : null,
                FileSize = long.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var size) ? size : null,
                Description = parts.Length > 6 ? parts[6].Trim() : null
            };
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Simple metadata fetch cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Warning, $"Failed to fetch simple metadata: {ex.Message}");
            return null;
        }

    }

    /// <summary>
    /// Fetches lightweight metadata using --print with user-specified fields.
    /// Returns a dictionary of requested fields (key = field name, value = string value).
    /// Supports Unicode titles/descriptions correctly.
    /// </summary>
    /// <param name="url">Video URL</param>
    /// <param name="fields">List of fields to fetch (e.g. "id", "title", "duration", "thumbnail", "view_count", "filesize", "description")</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Dictionary of field → value, or null if nothing could be fetched</returns>
    public async Task<Dictionary<string, string>?> GetSimpleMetadataAsync(string url, IEnumerable<string> fields, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        if (fields == null || !fields.Any())
            throw new ArgumentException("At least one field must be requested.", nameof(fields));

        try
        {
            const string separator = "|||YTDLP.NET|||";

            // Build print format: %(id)s|||YTDLP.NET|||%(title)s|||YTDLP.NET|||...
            var printParts = fields.Select(f => $"%({f})s");
            var printFormat = string.Join(separator, printParts);

            var arguments = $"--print \"{printFormat}\" --skip-download --no-playlist --quiet {SanitizeInput(url)}";

            var process = CreateProcess(arguments);
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();

            using (ct.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(true); }
                catch { }
            }))
            {
                await process.WaitForExitAsync(ct);
            }

            var rawOutput = await outputTask;
            if (string.IsNullOrWhiteSpace(rawOutput))
                return null;

            var parts = rawOutput.Trim().Split(separator);

            // Should have exactly as many parts as requested fields
            if (parts.Length != fields.Count())
                return null;

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            int index = 0;
            foreach (var field in fields)
            {
                var value = parts[index++].Trim();
                result[field] = value;
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Simple metadata fetch cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Warning, $"Simple metadata failed: {ex.Message}");
            return null;
        }
    }

    public async Task<string> GetBestAudioFormatIdAsync(string url, CancellationToken ct = default)
    {
        var meta = await GetVideoMetadataJsonAsync(url, ct);
        var best = meta?.Formats?
            .Where(f => f.IsAudio && (f.Abr > 0 || f.Tbr > 0))
            .OrderByDescending(f => f.Abr ?? f.Tbr ?? 0)
            .FirstOrDefault();

        return best?.FormatId ?? "bestaudio";
    }

    public async Task<string> GetBestVideoFormatIdAsync(string url, int maxHeight = 1080, CancellationToken ct = default)
    {
        var meta = await GetVideoMetadataJsonAsync(url, ct);
        var best = meta?.Formats?
            .Where(f => !f.IsAudio && f.Height.HasValue && f.Height <= maxHeight)
            .OrderByDescending(f => f.Height)
            .ThenByDescending(f => f.Fps ?? 0)
            .FirstOrDefault();

        return best?.FormatId ?? "bestvideo";
    }

    public async Task ExecuteAsync(string url, CancellationToken cancellationToken = default, string? outputTemplate = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        if (string.IsNullOrWhiteSpace(_format))
            _format = "best";

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
        string template = Path.Combine(_outputFolder, _outputTemplate.Replace("\\", "/"));

        // Build command with format and output template
        string arguments = $"{_commandBuilder} -f \"{_format}\" -o \"{template}\" {SanitizeInput(url)}";

        _logger.Log(LogType.Info, arguments);

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
        var psi = new ProcessStartInfo
        {
            FileName = _ytDlpPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,            
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        // Force Python/yt-dlp UTF-8
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        psi.Environment["PYTHONUTF8"] = "1";
        psi.Environment["LC_ALL"] = "en_US.UTF-8";
        psi.Environment["LANG"] = "en_US.UTF-8";

        return new Process { StartInfo = psi, EnableRaisingEvents = true };
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

        // escape internal quotes
        input = input.Replace("\"", "\\\"");

        // wrap with quotes (CRITICAL for paths with spaces)
        return $"\"{input}\"";
    }

    private static double? ParseDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "NA")
            return null;

        if (double.TryParse(value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var result))
            return result;

        return null;
    }

    private static long? ParseLong(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "NA")
            return null;

        if (long.TryParse(value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var result))
            return result;

        return null;
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

    private static bool IsAllowedOption(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg)) return false;
        if (ValidOptions.Contains(arg)) return true;
        if (arg.StartsWith("--") || arg.StartsWith("-")) return true;
        return false;
    }

    #endregion

}