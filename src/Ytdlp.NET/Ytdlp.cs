using ManuHub.Ytdlp.NET.Core;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ManuHub.Ytdlp.NET;

/// <summary>
/// Fluent wrapper for yt-dlp, providing methods to build commands, fetch metadata,
/// and execute downloads with progress tracking and event support.
/// </summary>
/// <remarks>
/// <strong>THREAD-SAFE:</strong> Multiple threads can safely use the same <see cref="Ytdlp"/> instance concurrently.
/// Each call to <see cref="DownloadAsync"/> creates isolated runners and parsers, preventing race conditions 
/// and shared state issues.
///
/// Example of safe concurrent usage:
/// <code>
/// var ytdlp = new Ytdlp()
///     .WithOutputFolder(@"D:\Downloads\YouTube")
///     .WithFormat("best");
///
/// var tasks = urls.Select(url => ytdlp.ExecuteAsync(url));
/// await Task.WhenAll(tasks);
/// </code>
///
/// <strong>Event forwarding:</strong>
/// All progress and output events are forwarded from the internal runners and parsers. 
/// Subscriptions are safe per execution and cleaned up automatically to prevent memory leaks.
///
/// <strong>Fluent builder:</strong> All configuration methods (e.g., <see cref="WithOutputFolder"/>, 
/// <see cref="WithFormat"/>, <see cref="WithExtractAudio"/>) return a new instance. This preserves 
/// immutability and thread-safety.
///
/// <strong>Resource cleanup:</strong> Internal runners and parsers are disposed automatically after each 
/// <see cref="DownloadAsync"/> call. For advanced scenarios, future versions may implement <see cref="IAsyncDisposable"/> 
/// for global disposal of resources and cancellation support.
/// </remarks>
public sealed class Ytdlp : IAsyncDisposable
{
    #region Frozen configuration
    private readonly string _ytdlpPath;
    private readonly ILogger _logger;

    private readonly string? _outputFolder;
    private readonly string? _homeFolder;
    private readonly string? _tempFolder;
    private readonly string _outputTemplate;
    private readonly string _format;
    private readonly string? _cookiesFile;
    private readonly string? _cookiesFromBrowser;
    private readonly string? _proxy;
    private readonly string? _ffmpegLocation;
    private readonly string? _sponsorblockRemove;
    private readonly int? _concurrentFragments;

    private readonly ImmutableArray<string> _flags;
    private readonly ImmutableArray<(string Key, string Value)> _options;
    #endregion

    #region Events
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    public event EventHandler<string>? OnProgressMessage;
    public event EventHandler<string>? OnOutputMessage;
    public event EventHandler<string>? OnCompleteDownload;
    public event EventHandler<string>? OnPostProcessingComplete;
    public event EventHandler<CommandCompletedEventArgs>? OnCommandCompleted;
    public event EventHandler<string>? OnErrorMessage;
    #endregion

    #region Flag to prevent double disposal
    private bool _disposed = false;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Optionally, cancel running downloads (if you store CancellationTokens)
        // e.g., _cts?.Cancel();

        await Task.CompletedTask;
    }
    #endregion

    #region Constructors

    public Ytdlp(string ytdlpPath = "yt-dlp", ILogger? logger = null)
    {
        _ytdlpPath = ValidatePath(ytdlpPath);
        _logger = logger ?? new DefaultLogger();

        // defaults
        _outputFolder = null;
        _tempFolder = null;
        _homeFolder = null;
        _outputTemplate = "%(title)s [%(id)s].%(ext)s";
        _format = "b";
        _concurrentFragments = null;
        _flags = ImmutableArray<string>.Empty;
        _options = ImmutableArray<(string, string)>.Empty;
        _cookiesFile = null;
        _cookiesFromBrowser = null;
        _proxy = null;
        _ffmpegLocation = null;
        _sponsorblockRemove = null;
    }

    // Private copy constructor – every WithXxx() uses this
    private Ytdlp(Ytdlp other,
        string? outputFolder = null,
        string? homeFolder = null,
        string? tempFolder = null,
        string? outputTemplate = null,
        string? format = null,
        int? concurrentFragments = null,
        string? cookiesFile = null,
        string? cookiesFromBrowser = null,
        string? proxy = null,
        string? ffmpegLocation = null,
        string? sponsorblockRemove = null,
        IEnumerable<string>? extraFlags = null,
        IEnumerable<(string, string)>? extraOptions = null)
    {
        _ytdlpPath = other._ytdlpPath;
        _logger = other._logger;

        _outputFolder = outputFolder ?? other._outputFolder;
        _homeFolder = homeFolder ?? other._homeFolder;
        _tempFolder = tempFolder ?? other._tempFolder;
        _outputTemplate = outputTemplate ?? other._outputTemplate;

        _format = format ?? other._format;
        _concurrentFragments = concurrentFragments ?? other._concurrentFragments;
        _cookiesFile = cookiesFile ?? other._cookiesFile;
        _cookiesFromBrowser = cookiesFromBrowser ?? other._cookiesFromBrowser;
        _proxy = proxy ?? other._proxy;
        _ffmpegLocation = ffmpegLocation ?? other._ffmpegLocation;
        _sponsorblockRemove = sponsorblockRemove ?? other._sponsorblockRemove;

        _flags = extraFlags is null ? other._flags : other._flags.AddRange(extraFlags);
        _options = extraOptions is null ? other._options : other._options.AddRange(extraOptions);
    }

    #endregion

    // ==================================================================================================================
    // Fluent configuration methods
    // ==================================================================================================================

    #region General Options

    /// <summary>
    /// Ignore download and postprocessing errors. The download will be considered successful even if the postprocessing fails
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithIgnoreErrors() => AddFlag("--ignore-errors");

    /// <summary>
    /// IgAbort downloading of further videos if an error occurs 
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithAbortOnError() => AddFlag("--abort-on-error");

    /// <summary>
    /// Don't load any more configuration files except those given to <see cref="WithConfigLocations(string)"/>.
    /// For backward compatibility, if this option is found inside the system configuration file, the user configuration is not loaded.
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithIgnoreConfig() => AddFlag("--ignore-config");

    /// <summary>
    /// Location of the main configuration file;either the path to the config or its containing directory ("-" for stdin). 
    /// Can be used multiple times and inside other configuration files.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Ytdlp WithConfigLocations(string path) 
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Config folder path required");
         return AddOption("--config-locations", Path.GetFullPath(path)); 
    }

    /// <summary>
    /// Path to an additional directory to search for plugins. This option can be used multiple times to add multiple directories.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Ytdlp WithPluginDirs(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("plugin folder path required");
        return AddOption("--plugin-dirs", path);
    }

    /// <summary>
    /// Clear plugin directories to search, including defaults and those provided by previous <see cref="WithPluginDirs(string)"/>
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public Ytdlp WithNoPluginDirs(string path) => AddFlag("--no-plugin-dirs");

    /// <summary>
    /// Additional JavaScript runtime to enable, with an optional location for the runtime (either the path to the binary or its containing directory).
    /// This option can be used multiple times to enable multiple runtimes. Supported runtimes are (in order of priority, from highest to lowest): deno, node, quickjs, bun.
    /// Only "deno" is enabled by default. The highest priority runtime that is both enabled and available will be used. 
    /// In order to use a lower priority runtime when "deno" is available, <see cref="WithNoJsRuntime"/> needs to be passed before enabling other runtimes
    /// </summary>
    /// <param name="runtime">Supported runtimes are deno, node, quickjs, bun</param>
    /// <param name="runtimePath"></param>
    public Ytdlp WithJsRuntime(Runtime runtime, string path)
    {
        var builder = $"{runtime}:{path}";
        return AddOption("--js-runtime", builder);
    }

    /// <summary>
    /// Clear JavaScript runtimes to enable, including defaults and those provided by <see cref="WithJsRuntime(Runtime, string)"/>
    /// </summary>
    public Ytdlp WithNoJsRuntime() => AddFlag("--no-js-runtime");

    /// <summary>
    /// Do not extract a playlist's URL result entries; some entry metadata may be missing and downloading may be bypassed
    /// </summary>
    public Ytdlp WithFlatPlaylist() => AddFlag("--flat-playlist");

    /// <summary>
    /// Download livestreams from the start. Currently experimental and only supported for YouTube, Twitch, and TVer.
    /// </summary>
    public Ytdlp WithLiveFromStart() => AddFlag("--live-from-start");

    /// <summary>
    /// Wait for scheduled streams to become available.Pass the minimum number of seconds(or range) to wait between retries
    /// </summary>
    /// <param name="maxWait"></param>
    /// <returns></returns>
    public Ytdlp WithWaitForVideo(TimeSpan? maxWait = null)
    {
        var opts = new List<(string Key, string? Value)>();

        opts.Add(("--wait-for-video", "any"));   // "any" = wait indefinitely or until timeout

        if (maxWait.HasValue && maxWait.Value.TotalSeconds > 0)
        {
            opts.Add(("--wait-for-video", maxWait.Value.TotalSeconds.ToString("F0")));
        }

        return new Ytdlp(this, extraOptions: opts!);
    }

    /// <summary>
    /// Mark videos watched (even with Simulate())
    /// </summary>
    public Ytdlp WithMarkWatched() => AddFlag("--mark-watched");

    #endregion

    #region Network Options

    /// <summary>
    /// Use the specified HTTP/HTTPS/SOCKS proxy. To enable SOCKS proxy, specify a proper scheme, e.g. socks5://user:pass@127.0.0.1:1080/.
    /// </summary>
    /// <param name="url">Pass in an empty string for direct connection</param>
    public Ytdlp WithProxy(string? proxy) => string.IsNullOrWhiteSpace(proxy) ? this : new Ytdlp(this, proxy: proxy);

    /// <summary>
    /// Time to wait before giving up, in seconds
    /// </summary>
    /// <param name="timeout"></param>
    public Ytdlp WithSocketTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero) return this;
        double seconds = timeout.TotalSeconds;
        return AddOption("--socket-timeout", seconds.ToString("F0"));
    }

    /// <summary>
    /// Make all connections via IPv4
    /// </summary>
    public Ytdlp WithForceIpv4() => AddFlag("--force-ipv4");

    /// <summary>
    /// Make all connections via IPv6
    /// </summary>
    public Ytdlp WithForceIpv6() => AddFlag("--force-ipv6");

    /// <summary>
    /// Enable file:// URLs. This is disabled by default for security reasons.
    /// </summary>
    public Ytdlp WithEnableFileUrls() => AddFlag("--enable-file-urls");

    #endregion

    #region Geo-restriction

    /// <summary>
    /// Use this proxy to verify the IP address for some geo-restricted sites. 
    /// The default proxy specified by <see cref="WithProxy(string?)"/> (or none, if the option is not present) is used for the actual downloading
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public Ytdlp WithGeoVerificationProxy(string url) => AddOption("--geo-verification-proxy", url);

    /// <summary>
    /// How to fake X-Forwarded-For HTTP header to try bypassing geographic restriction. One of "default" (only when known to be useful),
    /// "never", an IP block in CIDR notation, or a two-letter ISO 3166-2 country code
    /// </summary>
    /// <param name="countryCode"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithGeoBypassCountry(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2) throw new ArgumentException("Country code must be 2 letters.");
        return AddOption("--xff", countryCode.ToUpper());
    }

    #endregion

    #region Video Selection

    /// <summary>
    /// Comma-separated playlist_index of the items to download. You can specify a range using "[START]:[STOP][:STEP]".
    /// For backward compatibility, START-STOP is also supported. Use negative indices to count from the right and negative STEP to download in reverse order.
    /// E.g. "1:3,7,-5::2" used on a playlist of size 15 will download the items at index 1,2,3,7,11,13,15
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithPlaylistItems(string items)
    {
        if (string.IsNullOrWhiteSpace(items))
            throw new ArgumentException("Playlist items string cannot be empty.", nameof(items));
        return AddOption("--playlist-items", items.Trim());
    }

    /// <summary>
    /// Abort download if filesize is smaller than SIZE
    /// </summary>
    /// <param name="size">e.g. 50k or 44.6M</param>
    public Ytdlp WithMinFileSize(string size)
    {
        // size examples: 50k, 4.2M, 1G
        if (string.IsNullOrWhiteSpace(size))
            throw new ArgumentException("Size cannot be empty", nameof(size));
        return AddOption("--min-filesize", size.Trim());
    }

    /// <summary>
    /// Abort download if filesize is larger than SIZE
    /// </summary>
    /// <param name="size">e.g. 50k or 44.6M</param>
    public Ytdlp WithMaxFileSize(string size)
    {
        if (string.IsNullOrWhiteSpace(size))
            throw new ArgumentException("Size cannot be empty", nameof(size));
        return AddOption("--max-filesize", size.Trim());
    }

    /// <summary>
    /// Download only videos uploaded on this date.
    /// The date can be "YYYYMMDD" or in the format [now|today|yesterday][-N[day|week|month|year]].
    /// E.g. "--date today-2weeks" downloads only videos uploaded on the same day two weeks ago
    /// </summary>
    /// <param name="date">"today-2weeks" or "YYYYMMDD"</param>
    public Ytdlp WithDate(string date)
    {
        // formats: YYYYMMDD, today, yesterday, now-2weeks, etc.
        if (string.IsNullOrWhiteSpace(date))
            throw new ArgumentException("Date cannot be empty", nameof(date));
        return AddOption("--date", date.Trim());
    }

    /// <summary>
    /// Download only videos uploaded on or before this date. The date formats accepted are the same as <see cref="WithDate(string)"/>
    /// </summary>
    /// <param name="date">"today-2weeks" or "YYYYMMDD"</param>
    public Ytdlp WithDateBefore(string date)
    {
        // formats: YYYYMMDD, today, yesterday, now-2weeks, etc.
        if (string.IsNullOrWhiteSpace(date))
            throw new ArgumentException("Date cannot be empty", nameof(date));
        return AddOption("--datebefore", date.Trim());
    }

    /// <summary>
    /// Download only videos uploaded on or after this date. The date formats accepted are the same as <see cref="WithDate(string)"/>
    /// </summary>
    /// <param name="date">"today-2weeks" or "YYYYMMDD"</param>
    public Ytdlp WithDateAfter(string date)
    {
        // formats: YYYYMMDD, today, yesterday, now-2weeks, etc.
        if (string.IsNullOrWhiteSpace(date))
            throw new ArgumentException("Date cannot be empty", nameof(date));
        return AddOption("--dateafter", date.Trim());
    }

    /// <summary>
    /// Generic video filter. Any "OUTPUT TEMPLATE" field can be compared with a number or a string using the operators defined in "Filtering Formats".
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithMatchFilter(string filterExpression)
    {
        if (string.IsNullOrWhiteSpace(filterExpression))
            throw new ArgumentException("Match filter expression cannot be empty", nameof(filterExpression));

        return AddOption("--match-filter", filterExpression.Trim());
    }

    /// <summary>
    /// Download only the video, if the URL refers to a video and a playlist
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithNoPlaylist() => AddFlag("--no-playlist");

    /// <summary>
    /// Download the playlist, if the URL refers to a video and a playlist
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithYesPlaylist() => AddFlag("--yes-playlist");

    /// <summary>
    /// Download only videos suitable for the given age
    /// </summary>
    /// <param name="years"></param>
    public Ytdlp WithAgeLimit(int years)
    {
        if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
        return AddOption("--age-limit", years.ToString());
    }

    /// <summary>
    /// Download only videos not listed in the archive file. Record the IDs of all downloaded videos in it
    /// </summary>
    /// <param name="archivePath"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithDownloadArchive(string archivePath = "archive.txt")
    {
        if (string.IsNullOrWhiteSpace(archivePath))
            throw new ArgumentException("Archive path cannot be empty", nameof(archivePath));
        return AddOption("--download-archive", Path.GetFullPath(archivePath));
    }

    /// <summary>
    /// Abort after downloading number files
    /// </summary>
    /// <param name="count"></param>
    public Ytdlp WithMaxDownloads(int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        return AddOption("--max-downloads", count.ToString());
    }

    /// <summary>
    /// Stop the download process when encountering a file that is in the archive supplied with the <see cref="WithDownloadArchive(string)" /> option
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithBreakOnExisting() => AddFlag("--break-on-existing");

    #endregion

    #region Download Options

    /// <summary>
    /// Number of fragments of a dash/hlsnative video that should be downloaded concurrently (default is 1)
    /// </summary>
    /// <param name="count"></param>
    public Ytdlp WithConcurrentFragments(int count = 8) => count > 0 ? new Ytdlp(this, concurrentFragments: count) : this;

    /// <summary>
    /// Maximum download rate in bytes per second
    /// </summary>
    /// <param name="rate">e.g. 50K or 4.2M</param>
    public Ytdlp WithLimitRate(string rate) => AddOption("--limit-rate", rate);

    /// <summary>
    /// Minimum download rate in bytes per second below which throttling is assumed and the video data is re-extracted
    /// </summary>
    /// <param name="rate">e.g. 100K</param>
    public Ytdlp WithThrottledRate(string rate) => AddOption("--throttled-rate", rate);

    /// <summary>
    /// Number of retries (default is 10), or -1 for "infinite"
    /// </summary>
    /// <param name="maxRetries"></param>
    public Ytdlp WithRetries(int maxRetries) => AddOption("--retries", maxRetries < 0 ? "infinite" : maxRetries.ToString());

    /// <summary>
    /// Number of times to retry on file access error (default is 3), or -1 for "infinite"
    /// </summary>
    /// <param name="maxRetries"></param>
    public Ytdlp WithFileAccessRetries(int maxRetries) => AddOption("--file-access-retries", maxRetries < 0 ? "infinite" : maxRetries.ToString());

    /// <summary>
    /// Number of retries for a fragment (default is 10), or -1 for "infinite" (DASH, hlsnative and ISM)
    /// </summary>
    /// <param name="maxRetries"></param>
    public Ytdlp WithFragmentRetries(int retries)
    {
        // -1 = infinite
        string value = retries < 0 ? "infinite" : retries.ToString();
        return AddOption("--fragment-retries", value);
    }

    /// <summary>
    /// Skip unavailable fragments for DASH, hlsnative and ISM downloads (default)
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithSkipUnavailableFragments() => AddFlag("--skip-unavailable-fragments");

    /// <summary>
    /// Abort download if a fragment is unavailable
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithAbortOnUnavailableFragments() => AddFlag("--abort-on-unavailable-fragments");

    /// <summary>
    /// Keep downloaded fragments on disk after downloading is finished
    /// </summary>
    public Ytdlp WithKeepFragments() => AddFlag("--keep-fragments");

    /// <summary>
    /// Size of download buffer, (default is 1024) 
    /// </summary>
    /// <param name="size">e.g. 1024 or 16K</param>
    public Ytdlp WithBufferSize(string size) => AddOption("--buffer-size", size);

    /// <summary>
    /// Do not automatically adjust the buffer size
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithNoResizeBuffer() => AddFlag("--no-resize-buffer");

    /// <summary>
    /// Download playlist videos in random order
    /// </summary>
    public Ytdlp WithPlaylistRandom() => AddFlag("--playlist-random");

    /// <summary>
    /// Use the mpegts container for HLS videos; allowing some players to play the video while downloading, 
    /// and reducing the chance of file corruption if download is interrupted. This is enabled by default for live streams
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithHlsUseMpegts() => AddFlag("--hls-use-mpegts");

    /// <summary>
    /// Do not use the mpegts container for HLS videos. This is default when not downloading live streams
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithNoHlsUseMpegts() => AddFlag("--no-hls-use-mpegts");


    /// <summary>
    /// Download only chapters that match the regular expression. A "*" prefix denotes time-range instead of chapter.
    /// Negative timestamps are calculated from the end. "*from-url" can be used to download between the "start_time" and "end_time" extracted from the URL.
    /// Needs ffmpeg. This option can be used multiple times to download multiple sections
    /// </summary>
    /// <param name="regex">e.g. "*10:15-inf", "intro"</param>
    /// <returns></returns>
    public Ytdlp WithDownloadSections(string regex)
    {
        if (string.IsNullOrWhiteSpace(regex)) return this;
        return AddOption("--download-sections", regex);
    }


    #endregion

    #region Filesystem Options

    /// <summary>
    /// Sets the home folder for yt-dlp (used for config or as base directory).
    /// Path is automatically normalized and quoted.
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithHomeFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Home folder path required");
        return new Ytdlp(this, homeFolder: Path.GetFullPath(path));
    }

    /// <summary>
    /// Sets the temporary folder for yt-dlp intermediate files (fragments, etc.).
    /// Path is automatically normalized and quoted.
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithTempFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Temp folder path required");
        return new Ytdlp(this, tempFolder: Path.GetFullPath(path));
    }

    /// <summary>
    /// Sets the output folder
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithOutputFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Output folder path required");
        return new Ytdlp(this, outputFolder: Path.GetFullPath(path));
    }

    /// <summary>
    /// Output filename template
    /// </summary>
    /// <param name="template"></param>
    public Ytdlp WithOutputTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template)) throw new ArgumentException("Template required");
        return new Ytdlp(this, outputTemplate: template.Trim());
    }

    /// <summary>
    /// Restrict filenames to only ASCII characters, and avoid "&" and spaces in filenames
    /// </summary>
    public Ytdlp WithRestrictFilenames() => AddFlag("--restrict-filenames");

    /// <summary>
    /// Force filenames to be Windows-compatible
    /// </summary>
    public Ytdlp WithWindowsFilenames() => AddFlag("--windows-filenames");

    /// <summary>
    /// Limit the filename length (excluding extension) to the specified number of characters
    /// </summary>
    /// <param name="length"></param>
    public Ytdlp WithTrimFilenames(int length)
    {
        if (length < 10)
            throw new ArgumentOutOfRangeException(nameof(length), "Length should be at least 10 characters");

        return AddOption("--trim-filenames", length.ToString());
    }

    /// <summary>
    /// Do not overwrite any files
    /// </summary>
    public Ytdlp WithNoOverwrites() => AddFlag("--no-overwrites");

    /// <summary>
    /// Overwrite all video and metadata files. This option includes <see cref="WithNoContinue" />
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithForceOverwrites() => AddFlag("--force-overwrites");

    /// <summary>
    /// Do not resume partially downloaded fragments. If the file is not fragmented, restart download of the entire file
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithNoContinue() => AddFlag("--no-continue");

    /// <summary>
    /// Do not use .part files - write directly into output file
    /// </summary>
    public Ytdlp WithNoPart() => AddFlag("--no-part");

    /// <summary>
    /// Use the Last-modified header to set the file modification time
    /// </summary>
    public Ytdlp WithMtime() => AddFlag("--mtime");

    /// <summary>
    /// Write video description to a .description file
    /// </summary>
    public Ytdlp WithWriteDescription() => AddFlag("--write-description");

    /// <summary>
    /// Write video metadata to a .info.json file (this may contain personal information)
    /// </summary>
    public Ytdlp WithWriteInfoJson() => AddFlag("--write-info-json");

    /// <summary>
    /// Do not write playlist metadata when using <see cref="WithWriteInfoJson"/>, <see cref="WithWriteDescription"/>
    /// </summary>
    public Ytdlp WithNoWritePlaylistMetafiles() => AddFlag("--no-write-playlist-metafiles");

    /// <summary>
    /// Write all fields to the infojson
    /// </summary>
    public Ytdlp WithNoCleanInfoJson() => AddFlag("--no-clean-info-json");

    /// <summary>
    /// Retrieve video comments to be placed in the infojson. The comments are fetched even without this option if the extraction is known to be quick
    /// </summary>
    public Ytdlp WriteComments() => AddFlag("--write-comments");

    /// <summary>
    /// Do not retrieve video comments unless the extraction is known to be quick
    /// </summary>
    public Ytdlp WithNoWriteComments() => AddFlag("--no-write-comments");

    /// <summary>
    /// JSON file containing the video information (created with the WriteVideoMetadata() option)
    /// </summary>
    /// <param name="path">*.json</param>
    public Ytdlp WithLoadInfoJson(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Json file path cannot be empty.", nameof(path));
        return AddOption("--load-info-json", path);
    }

    /// <summary>
    /// Netscape formatted file to read cookies from and dump cookie jar in
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithCookiesFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Cookie file path cannot be empty.", nameof(path));
        return new Ytdlp(this, cookiesFile: Path.GetFullPath(path));
    }

    /// <summary>
    /// The name of the browser to load cookies from. Currently supported browsers are: brave, chrome, chromium, edge, firefox, opera, safari, vivaldi, whale.
    /// Optionally, the KEYRING used for decrypting Chromium cookies on Linux, the name/path of the PROFILE to load cookies from, and the CONTAINER name (if Firefox) 
    /// ("none" for no container) can be given with their respective separators. By default, all containers of the most recently accessed profile are used.
    /// keyrings are: basictext, gnomekeyring, kwallet, kwallet5, kwallet6
    /// </summary>
    /// <param name="browser"></param>
    public Ytdlp WithCookiesFromBrowser(string browser) => new Ytdlp(this, cookiesFromBrowser: browser);

    /// <summary>
    /// Disable filesystem caching
    /// </summary>
    public Ytdlp WithNoCacheDir() => AddFlag("--no-cache-dir");

    /// <summary>
    /// Delete all filesystem cache files
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithRemoveCacheDir() => AddFlag("--rm-cache-dir");

    #endregion

    #region Thumbnail Options

    /// <summary>
    /// Write thumbnail image to disk / Write all thumbnail image formats to disk
    /// </summary>
    /// <param name="allSizes"></param>
    /// <returns></returns>
    public Ytdlp WithThumbnails(bool allSizes = false)
    {
        if (allSizes)
            return AddFlag("--write-all-thumbnails");

        return AddFlag("--write-thumbnail");
    }


    #endregion

    #region Verbosity and Simulation Options

    /// <summary>
    /// Activate quiet mode. If used with --verbose, print the log to stderr
    /// </summary>
    public Ytdlp WithQuiet() => AddFlag("--quiet");

    /// <summary>
    /// Ignore warnings
    /// </summary>
    public Ytdlp WithNoWarnings() => AddFlag("--no-warnings");

    /// <summary>
    /// Do not download the video and do not write anything to disk
    /// </summary>
    public Ytdlp WithSimulate() => AddFlag("--simulate");

    /// <summary>
    /// Download the video even if printing/listing options are used
    /// </summary>
    public Ytdlp WithNoSimulate() => AddFlag("--no-simulate");

    /// <summary>
    /// Do not download the video but write all related files (Alias: --no-download)
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithSkipDownload() => AddFlag("--skip-download");

    /// <summary>
    /// Print various debugging information
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithVerbose() => AddFlag("--verbose");

    #endregion

    #region Workgrounds

    /// <summary>
    /// Specify a custom HTTP header and its value. You can use this option multiple times
    /// </summary>
    /// <param name="header">"Referer" "User-Agent"</param>
    /// <param name="value">"URL", "UA"</param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithAddHeader(string header, string value)
    {
        if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Header and value cannot be empty.");
        return AddOption("--add-headers", $"{header}:{value}");
    }

    /// <summary>
    ///  Number of seconds to sleep between requests during data extraction, Maximum number of seconds to sleep. 
    ///  Can only be used along with --min-sleep-interval
    /// </summary>
    /// <param name="seconds"></param>
    /// <param name="maxSeconds"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Ytdlp WithSleepInterval(double seconds, double? maxSeconds = null)
    {
        if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        var opts = new List<(string, string?)> { ("--sleep-requests", seconds.ToString("F2", CultureInfo.InvariantCulture)) };
        if (maxSeconds.HasValue && maxSeconds > seconds)
        {
            opts.Add(("--max-sleep-requests", maxSeconds.Value.ToString("F2", CultureInfo.InvariantCulture)));
        }
        return new Ytdlp(this, extraOptions: opts!);
    }

    /// <summary>
    /// Number of seconds to sleep before each subtitle download
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Ytdlp WithSleepSubtitles(double seconds)
    {
        if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        return AddOption("--sleep-subtitles", seconds.ToString("F2", CultureInfo.InvariantCulture));
    }

    #endregion

    #region Video Format Options

    /// <summary>
    /// Video format code
    /// </summary>
    /// <param name="format"></param>
    public Ytdlp WithFormat(string format) => new Ytdlp(this, format: format.Trim());

    /// <summary>
    /// Containers that may be used when merging formats, separated by "/", e.g. "mp4/mkv" Ignored if no merge is required.
    /// </summary>
    /// <param name="format">(currently supported: avi, flv, mkv, mov, mp4, webm)</param>
    /// <returns></returns>
    public Ytdlp WithMergeOutputFormat(string format)
    {
        // Common values: mp4, mkv, webm, mov, avi, flv
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Merge output format cannot be empty", nameof(format));

        return AddOption("--merge-output-format", format.Trim().ToLowerInvariant());
    }

    #endregion

    #region Subtitle Options 

    /// <summary>
    /// Write subtitle file
    /// </summary>
    /// <param name="languages">Languages of the subtitles to download (can be regex) or "all" separated by commas, e.g."en.*,ja"
    /// (where "en.*" is a regex pattern that matches "en" followed by 0 or more of any character).
    /// </param>
    /// <param name="auto">Write automatically generated subtitle file</param>
    public Ytdlp WithSubtitles(string languages = "all", bool auto = false)
    {
        var flags = new List<string> { "--write-subs" };
        if (auto) flags.Add("--write-auto-subs");

        return new Ytdlp(this, extraFlags: flags, extraOptions: new[] { ("--sub-langs", languages) });
    }

    #endregion

    #region Authentication Options

    /// <summary>
    /// Login with this account ID and account password.
    /// </summary>
    /// <param name="username">Account ID</param>
    /// <param name="password">Account password</param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithAuthentication(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Username and password cannot be empty.");
        return this
            .AddOption("--username", username)
            .AddOption("--password", password);
    }

    /// <summary>
    /// Two-factor authentication code
    /// </summary>
    /// <param name="code">Two-factor Code</param>
    /// <returns></returns>
    public Ytdlp WithTwoFactor(string code) => AddOption("--twofactor", code);

    #endregion

    #region Post-Processing Options

    /// <summary>
    /// Convert video files to audio-only files (requires ffmpeg and ffprobe).        
    /// </summary>
    /// <param name="format">Formats currently supported: best (default),aac, alac, flac, m4a, mp3, opus, vorbis, wav).</param>
    /// <param name="quality">Audio quality (0–10, lower = better). Default: 5 (medium)</param>
    public Ytdlp WithExtractAudio(AudioFormat format = AudioFormat.Best, int quality = 5)
    {
        return this
            .AddFlag("--extract-audio")
            .AddOption("--audio-format", format.ToString().ToLowerInvariant())
            .AddOption("--audio-quality", quality.ToString());
    }

    /// <summary>
    /// Remux the video into another container if necessary (requires ffmpeg and ffprobe)
    /// If the target container does not support the video/audio codec, remuxing will fail. You can specify multiple rules; 
    /// e.g. "aac>m4a/mov>mp4/mkv" will remux aac to m4a, mov to mp4 and anything else to mkv
    /// </summary>
    /// <param name="format">(currently supported: avi, flv, gif, mkv, mov, mp4, webm, aac, aiff, alac, flac, m4a, mka, mp3, ogg, opus, vorbis, wav).</param>
    public Ytdlp WithRemuxVideo(MediaFormat format = MediaFormat.Mp4) => AddOption("--remux-video", format.ToString().ToLowerInvariant());


    /// <summary>
    /// Re-encode the video into another format if necessary. The syntax and supported formats are the same as WithRemuxVideo()
    /// </summary>
    /// <param name="format">(currently supported: avi, flv, gif, mkv, mov, mp4, webm, aac, aiff, alac, flac, m4a, mka, mp3, ogg, opus, vorbis, wav).</param>
    /// <param name="videoCodec"></param>
    /// <param name="audioCodec"></param>
    public Ytdlp WithRecodeVideo(MediaFormat format = MediaFormat.Mp4, string? videoCodec = null, string? audioCodec = null)
    {
        var builder = AddOption("--recode-video", format.ToString().ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(videoCodec))
            builder = builder.AddOption("--video-codec", videoCodec);
        if (!string.IsNullOrWhiteSpace(audioCodec))
            builder = builder.AddOption("--audio-codec", audioCodec);
        return builder;
    }

    /// <summary>
    /// Give these arguments to the postprocessors. Specify the postprocessor/executable name and to give the argument to the specified
    /// </summary>
    /// <param name="postprocessor">Supported PP are: Merger, ModifyChapters, SplitChapters, ExtractAudio, 
    /// VideoRemuxer, VideoConvertor, Metadata, EmbedSubtitle, EmbedThumbnail, SubtitlesConvertor, ThumbnailsConvertor, 
    /// FixupStretched, FixupM4a, FixupM3u8, FixupTimestamp and FixupDuration.</param>
    /// <param name="args"></param>
    public Ytdlp WithPostprocessorArgs(PostProcessors postprocessor, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            throw new ArgumentException("Both postprocessor name and arguments are required");

        string combined = $"{postprocessor.ToString().Trim()}:{args.Trim()}";
        return AddOption("--postprocessor-args", combined);
    }

    /// <summary>
    /// Keep the intermediate video file on disk after post-processing
    /// </summary>
    public Ytdlp WithKeepVideo() => AddFlag("-k");

    /// <summary>
    /// Do not overwrite post-processed files
    /// </summary>
    public Ytdlp WithNoPostOverwrites() => AddFlag("--no-post-overwrites");

    /// <summary>
    /// Embed subtitles in the video (only for mp4, webm and mkv videos)
    /// </summary>
    /// <param name="languages"></param>
    /// <param name="convertTo"></param>
    public Ytdlp WithEmbedSubtitles(string languages = "all", string? convertTo = null)
    {
        var builder = AddFlag("--sub-langs")
            .AddOption("--write-sub", languages);
        if (!string.IsNullOrWhiteSpace(convertTo))
            builder = builder.AddOption("--convert-subs", convertTo);
        if (convertTo?.Equals("embed", StringComparison.OrdinalIgnoreCase) == true)
            builder = builder.AddFlag("--embed-subs");
        return builder;
    }

    /// <summary>
    /// Embed thumbnail in the video as cover art
    /// </summary>
    public Ytdlp WithEmbedThumbnail() => AddFlag("--embed-thumbnail");

    /// <summary>
    /// Embed metadata to the video file
    /// </summary>
    public Ytdlp WithEmbedMetadata() => AddFlag("--embed-metadata");

    /// <summary>
    /// Add chapter markers to the video file
    /// </summary>
    public Ytdlp WithEmbedChapters() => AddFlag("--embed-chapters");

    /// <summary>
    /// Embed the infojson as an attachment to mkv/mka video files
    /// </summary>
    public Ytdlp WithEmbedInfoJson() => AddFlag("--embed-info-json");

    /// <summary>
    /// Do not embed the infojson as an attachment to the video file
    /// </summary>
    public Ytdlp WithNoEmbedInfoJson() => AddFlag("--no-embed-info-json");

    /// <summary>
    /// Replace text in a metadata field using the given regex. This option can be used multiple times.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="regex"></param>
    /// <param name="replacement"></param>
    /// <exception cref="ArgumentException"></exception>
    public Ytdlp WithReplaceInMetadata(string field, string regex, string replacement)
    {
        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(regex) || replacement == null)
            throw new ArgumentException("Metadata field, regex, and replacement cannot be empty.");
        return AddFlag($"--replace-in-metadata {field} {regex} {replacement}");
    }

    /// <summary>
    /// Concatenate videos in a playlist. All the video files must have the same codecs and number of streams to be concatenable
    /// </summary>
    /// <param name="policy">never, always, multi_video (default; only when the videos form a single show)</param>
    public Ytdlp WithConcatPlaylist(string policy = "always") => AddOption("--concat-playlist", policy);

    /// <summary>
    /// Location of the ffmpeg binary
    /// </summary>
    /// <param name="ffmpegPath">Either the path to the binary or its containing directory</param>
    public Ytdlp WithFFmpegLocation(string? ffmpegPath)
    {
        if (string.IsNullOrWhiteSpace(ffmpegPath)) return this;
        return new Ytdlp(this, ffmpegLocation: ffmpegPath);
    }

    /// <summary>
    /// Convert the thumbnails to another format. You can specify multiple rules using similar WithRemuxVideo().
    /// </summary>
    /// <param name="format">(currently supported: jpg, png, webp)</param>
    /// <returns></returns>
    public Ytdlp WithConvertThumbnails(string format = "jpg")
    {
        // Supported: jpg, png, webp
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Thumbnail format cannot be empty", nameof(format));

        return AddOption("--convert-thumbnails", format.Trim().ToLowerInvariant());
    }

    /// <summary>
    /// Force keyframes at cuts when downloading/splitting/removing sections. 
    /// This is slow due to needing a re-encode, but the resulting video may have fewer artifacts around the cuts
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithForceKeyframesAtCuts() => AddFlag("--force-keyframes-at-cuts");

    #endregion

    #region SponsorBlock Options

    /// <summary>
    /// SponsorBlock categories to create chapters for, separated by commas. 
    /// Available categories are sponsor, intro, outro, selfpromo, preview, filler, interaction, music_offtopic, hook, poi_highlight, chapter, all and default (=all).
    /// You can prefix the category with a "-" to exclude it. E.g. SponsorBlockMark("all,-preview)
    /// </summary>
    /// <param name="categories"></param>
    /// <returns></returns>
    public Ytdlp WithSponsorblockMark(string categories = "all") => AddOption("--sponsorblock-mark", categories);

    /// <summary>
    /// SponsorBlock categories to be removed from the video file, separated by commas. 
    /// If a category is present in both mark and remove, remove takes precedence. Working and available categories are the same as for WithSponsorblockMark()
    /// </summary>
    /// <param name="categories"></param>
    /// <returns></returns>
    public Ytdlp WithSponsorblockRemove(string categories = "all") => new Ytdlp(this, sponsorblockRemove: categories);

    /// <summary>
    /// Disable both WithSponsorblockMark() and WithSponsorblockRemove() options and do not use any sponsorblock features
    /// </summary>
    /// <returns></returns>
    public Ytdlp WithNoSponsorblock() => AddFlag("--no-sponsorblock");

    #endregion

    #region Core
    public Ytdlp AddFlag(string flag) => new Ytdlp(this, extraFlags: new[] { flag.Trim() });

    public Ytdlp AddOption(string key, string value) => new Ytdlp(this, extraOptions: new[] { (key.Trim(), value) });
    #endregion

    #region Downloaders
    public Ytdlp WithExternalDownloader(string downloaderName, string? downloaderArgs = null)
    {
        if (string.IsNullOrWhiteSpace(downloaderName))
            throw new ArgumentException("Downloader name cannot be empty", nameof(downloaderName));

        var opts = new List<(string, string?)> { ("--downloader", downloaderName.Trim()) };

        if (!string.IsNullOrWhiteSpace(downloaderArgs))
        {
            opts.Add(("--downloader-args", downloaderArgs.Trim()));
        }

        return new Ytdlp(this, extraOptions: opts!);
    }

    public Ytdlp WithAria2(int connections = 16)
    {
        return new Ytdlp(this, extraOptions: new[]
            {
            ("--downloader", "aria2c"),
            ("--downloader-args", $"aria2c:-x{connections} -k1M")
            });
    }

    public Ytdlp WithHlsNative() => AddOption("--downloader", "hlsnative");

    public Ytdlp WithFfmpegAsLiveDownloader(string? extraFfmpegArgs = null) => WithExternalDownloader("ffmpeg", extraFfmpegArgs);

    #endregion

    #region Redundant options

    /// <summary>
    /// Playlist start index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Ytdlp WithPlaylistStart(int index)
    {
        if (index < 1) throw new ArgumentOutOfRangeException(nameof(index), "Must be >= 1");
        return AddOption("--playlist-start", index.ToString());
    }

    /// <summary>
    /// Playlist end index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Ytdlp WithPlaylistEnd(int index)
    {
        if (index < 1) throw new ArgumentOutOfRangeException(nameof(index), "Must be >= 1");
        return AddOption("--playlist-end", index.ToString());
    }

    public Ytdlp WithUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("User-Agent cannot be empty", nameof(userAgent));
        return AddOption("--user-agent", userAgent.Trim());
    }

    public Ytdlp WithReferer(string referer)
    {
        if (string.IsNullOrWhiteSpace(referer))
            throw new ArgumentException("Referer cannot be empty", nameof(referer));
        return AddOption("--referer", referer.Trim());
    }

    public Ytdlp WithMatchTitle(string regex)
    {
        if (string.IsNullOrWhiteSpace(regex))
            throw new ArgumentException("Regex cannot be empty", nameof(regex));
        return AddOption("--match-title", regex.Trim());
    }

    public Ytdlp WithRejectTitle(string regex)
    {
        if (string.IsNullOrWhiteSpace(regex))
            throw new ArgumentException("Regex cannot be empty", nameof(regex));
        return AddOption("--reject-title", regex.Trim());
    }

    public Ytdlp WithBreakOnReject() => AddFlag("--break-on-reject");
    #endregion

    #region Bonus

    public Ytdlp WithBestUpTo1440p() => new Ytdlp(this, format: "bestvideo[height<=?1440]+bestaudio/best");
    public Ytdlp With1080pOrBest() => new Ytdlp(this, format: "bestvideo[height<=?1080]+bestaudio/best");

    public Ytdlp WithBestUpTo1080p() => new Ytdlp(this, format: "bestvideo[height<=?1080]+bestaudio/best");

    public Ytdlp With720pOrBest() => new Ytdlp(this, format: "bv*[height<=?720]+ba/best/best");

    public Ytdlp WithMp4PostProcessingPreset()
        => this
            .WithRemuxVideo(MediaFormat.Mp4)
            .WithEmbedMetadata()
            .WithEmbedChapters()
            .WithEmbedThumbnail();

    public Ytdlp WithMkvOutput()
        => this
            .WithRemuxVideo(MediaFormat.Mkv)
            .WithMergeOutputFormat("mkv");

    public Ytdlp WithMaxHeight(int height)
    {
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive");

        string formatSelector = $"bestvideo[height<={height}]+bestaudio/best";
        return new Ytdlp(this, format: formatSelector);
    }

    public Ytdlp WithMaxHeightOrBest(int height)
    {
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive");

        string formatSelector = $"bestvideo[height<={height}]+bestaudio/best[height<={height}]/best";
        return new Ytdlp(this, format: formatSelector);
    }

    public Ytdlp WithBestVideoPlusBestAudio() => new Ytdlp(this, format: "bestvideo+bestaudio/best");

    public Ytdlp WithBestAudioOnly() => new Ytdlp(this, format: "bestaudio");

    public Ytdlp WithNo4k() => new Ytdlp(this, format: "bestvideo[height<=?2160]+bestaudio/best");

    public Ytdlp WithBestM4aAudio() => new Ytdlp(this, format: "bestaudio[ext=m4a]/bestaudio/best");
    #endregion

    // ==================================================================================================================
    // Probe and Download Functions
    // ==================================================================================================================

    #region Execution & Utility Methods

    /// <summary>
    /// Command preview ofr debug operatons
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public string Preview(string url)
    {
        var argsList = BuildArguments(url);
        return string.Join(" ", argsList.Select(Quote));
    }

    /// <summary>
    /// Retrieves the current version string of the underlying yt-dlp executable.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to abort the version check process.</param>
    /// <returns>
    /// A <see cref="string"/> representing the yt-dlp version (e.g., "2023.03.04"); 
    /// returns an empty string or throws if the binary cannot be found.
    /// </returns>
    public async Task<string> VersionAsync(CancellationToken ct = default)
    {
        var output = await Probe().RunAsync("--version", ct);
        string version = output is null ? string.Empty : output.Trim();
        _logger.Log(LogType.Info, $"yt-dlp version: {version}");
        return version;
    }

    /// <summary>
    /// Updates the underlying yt-dlp binary to the latest version on the specified release channel.
    /// </summary>
    /// <param name="channel">The release channel to pull updates from (Master, Nightly, Stable.).</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to abort the download and installation process.</param>
    /// <returns>
    /// A <see cref="string"/> containing the update log or the new version number; 
    /// returns an empty string or throws if the update process fails.
    /// </returns>
    public async Task<string> UpdateAsync(UpdateChannel channel = UpdateChannel.Stable, CancellationToken ct = default)
    {
        var output = await Probe().RunAsync($"--update-to {channel.ToString().ToLowerInvariant()}", ct);
        if (output is null)
            return string.Empty;

        // Analyze output for professional messages
        if (output.Contains("Updated", StringComparison.OrdinalIgnoreCase))
            return "yt-dlp was successfully updated to the latest version.";

        if (output.Contains("up to date", StringComparison.OrdinalIgnoreCase))
            return "yt-dlp is already up to date.";

        return "yt-dlp update check completed (no changes detected).";


    }

    /// <summary>
    /// List all supported extractors and exit
    /// </summary>
    /// <param name="ct"></param>    
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <param name="bufferKb">Buffer size in KB.</param>
    /// <returns>List of extractor names</returns>
    public async Task<List<string>> ExtractorsAsync(CancellationToken ct = default, bool tuneProcess= true, int bufferKb = 256)
    {
        try
        {
            List<string> list = new();
            var result = await Probe().RunAsync("--list-extractors", ct, tuneProcess, bufferKb);

            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.Log(LogType.Warning, "Empty extractor list.");
                return list;
            }

            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var line in lines)
                list.Add(line);

            return list;
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Extractors fetch cancelled.");
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Warning, $"Extrators fetch failed: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    ///  Fetches video metadata from the specified URL.
    /// </summary>
    /// <param name = "url">The source URL(video or playlist) to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <param name="bufferKb">Buffer size in KB.</param>
    /// <returns>
    /// A <see cref="Metadata"/> object containing the parsed metadata output; 
    /// returns <see langword="null"/> if the process fails, returns empty, or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Metadata?> GetMetadataAsync(string url, CancellationToken ct = default, bool tuneProcess = true, int bufferKb = 256)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        try
        {
            var arguments =
                $"--dump-single-json " +
                $"--simulate " +
                $"--skip-download " +
                $"--flat-playlist " +
                $"--lazy-playlist " +
                $"--quiet " +
                $"--no-warnings " +
                $"{Quote(url)}";

            if(ct.IsCancellationRequested)
                Debug.WriteLine("Cancellation requested before starting the process.");

            var json = await Probe().RunAsync(arguments, ct, tuneProcess, bufferKb);

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.Log(LogType.Warning, "Empty JSON output.");
                return null;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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

    /// <summary>
    /// Fetches raw JSON metadata the specified URL.
    /// </summary>
    /// <param name="url">The source URL (video or playlist) to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <param name="bufferKb">The buffer size for the process output stream (default 128KB).</param>
    /// <returns>
    /// A raw JSON <see cref="object"/> containing the parsed metadata output; 
    /// returns <see langword="null"/> if the process fails, returns empty, or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<object?> GetMetadataRawAsync(string url, CancellationToken ct = default, bool tuneProcess = true, int bufferKb = 256)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        try
        {
            var arguments =
                $"--dump-single-json " +
                $"--simulate " +
                $"--skip-download " +
                $"--flat-playlist " +
                $"--lazy-playlist " +
                $"--quiet " +
                $"--no-warnings " +
                $"{Quote(url)}";

            var json = await Probe().RunAsync(arguments, ct, tuneProcess, bufferKb);

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.Log(LogType.Warning, "Empty JSON output.");
                return null;
            }

            return json;
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

    /// <summary>
    /// Retrieves a list of all available stream formats for a given URL.
    /// </summary>
    /// <param name="url">The video or playlist URL to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <param name="bufferKb">The buffer size in kilobytes for the process output stream (default 128KB).</param>
    /// <returns>
    /// A <see cref="List{Format}"/> containing all available streams; 
    /// returns an empty list or <see langword="null"/> if the probe fails or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<List<Format>> GetFormatsAsync(string url, CancellationToken ct = default, bool tuneProcess = true, int bufferKb = 256)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Video URL cannot be empty.", nameof(url));

        var output = await Probe().RunAsync($"-F {Quote(url)}", ct,tuneProcess, bufferKb);

        if (string.IsNullOrWhiteSpace(output))
        {
            _logger.Log(LogType.Info, $"Empty format result.");
            return new List<Format>();
        }

        return ParseFormats(output);
    }

    /// <summary>
    /// Fetches a lightweight version of video metadata.
    /// </summary>
    /// <param name="url">The video or playlist URL to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <param name="bufferKb">The buffer size in kilobytes for the process output stream (default 128KB).</param>
    /// <returns>
    /// A <see cref="MetadataLight"/> object if successful; 
    /// returns <see langword="null"/> if the process fails or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<MetadataLight?> GetMetadataLiteAsync(string url, CancellationToken ct = default, bool tuneProcess = true, int bufferKb = 256)
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

            var arguments = $"{printArg} --skip-download --no-playlist --quiet {Quote(url)}";

            var output = await Probe().RunAsync(arguments, ct, tuneProcess, bufferKb);

            if (string.IsNullOrWhiteSpace(output))
                return null;

            var parts = output.Trim().Split(separator);

            if (parts.Length < 6) // at least id, title, duration, thumbnail, views, size
                return null;

            return new MetadataLight
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
    /// Fetches a specific subset of metadata fields from the specified URL.
    /// </summary>
    /// <param name="url">The source URL to probe.</param>
    /// <param name="fields">A collection of field names to extract (e.g., "title", "uploader").</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to abort the yt-dlp process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <param name="bufferKb">The buffer size in kilobytes for the process output (default 128KB).</param>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> containing the requested fields and their values; 
    /// returns <see langword="null"/> if the process fails, returns no data, or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Dictionary<string, string>?> GetMetadataLiteAsync(string url, IEnumerable<string> fields, CancellationToken ct = default, bool tuneProcess = true, int bufferKb = 256)
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

            var arguments = $"--print \"{printFormat}\" --skip-download --no-playlist --quiet {Quote(url)}";

            var rawOutput = await Probe().RunAsync(arguments, ct, tuneProcess, bufferKb);
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

    /// <summary>
    /// Probes the specified URL to find the ID of the best available audio format.
    /// </summary>
    /// <param name="url">The video or playlist URL to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <param name="bufferKb">The buffer size in kilobytes for the process output stream (default 128KB).</param>
    /// <returns>
    /// A <see cref="string"/> representing the best audio format ID (e.g., "140"); 
    /// returns an empty string or throws if no suitable audio is found.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<string> GetBestAudioFormatIdAsync(string url, CancellationToken ct = default, bool tuneProcess = true, int bufferKb = 256)
    {
        var meta = await GetMetadataAsync(url, ct,tuneProcess, bufferKb);
        var best = meta?.Formats?
            .Where(f => f.IsAudio && (f.Abr > 0 || f.Tbr > 0))
            .OrderByDescending(f => f.Abr ?? f.Tbr ?? 0)
            .FirstOrDefault();

        return best?.FormatId ?? "bestaudio";
    }

    /// <summary>
    /// Probes the specified URL to find the ID of the best available video format within the specified height.
    /// </summary>
    /// <param name="url">The source URL to probe for video formats.</param>
    /// <param name="maxHeight">The maximum vertical resolution allowed (default 1080p).</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to cancel the underlying yt-dlp process.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <param name="bufferKb">The buffer size in kilobytes for the process output (default 128KB).</param>
    /// <returns>
    /// A <see cref="string"/> representing the best video format ID (e.g., "137" or "248"); 
    /// returns an empty string or <see langword="null"/> if no suitable format is found.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<string> GetBestVideoFormatIdAsync(string url, int maxHeight = 1080, CancellationToken ct = default, bool tuneProcess = true, int bufferKb = 256)
    {
        var meta = await GetMetadataAsync(url, ct,tuneProcess, bufferKb);
        var best = meta?.Formats?
            .Where(f => !f.IsAudio && f.Height.HasValue && f.Height <= maxHeight)
            .OrderByDescending(f => f.Height)
            .ThenByDescending(f => f.Fps ?? 0)
            .FirstOrDefault();

        return best?.FormatId ?? "bestvideo";
    }

    /// <summary>
    /// Executes download processing for a URL.
    /// </summary>
    /// <param name="url">The source URL to download.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to stop the execution.</param>
    /// <param name="tuneProcess">Whether to tune the process for better performance (true by default). If false, the process will use the default buffer size and may have slower output processing.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="YtdlpException"></exception>
    public async Task DownloadAsync(string url, CancellationToken ct = default, bool tuneProcess = true)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL required", nameof(url));

        // Ensure directories exist if needed
        try
        {
            if (!string.IsNullOrWhiteSpace(_outputFolder))
                Directory.CreateDirectory(_outputFolder);

            if (!string.IsNullOrWhiteSpace(_homeFolder))
                Directory.CreateDirectory(_homeFolder);

            if (!string.IsNullOrWhiteSpace(_tempFolder))
                Directory.CreateDirectory(_tempFolder);
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Failed to create necessary folders: {ex.Message}");
            throw new YtdlpException("Failed to create required folders", ex);
        }

        var argsList = BuildArguments(url);
        var arguments = string.Join(" ", argsList.Select(Quote));

        _logger.Log(LogType.Info, $"Executing: {_ytdlpPath} {arguments}");

        // Create isolated execution components
        var factory = new ProcessFactory(_ytdlpPath);
        var progressParser = new ProgressParser(_logger);
        var download = new DownloadRunner(factory, progressParser, _logger);

        // Forward progress events locally inside this method
        void OnProgressDownloadHandler(object? s, DownloadProgressEventArgs e)
            => OnProgressDownload?.Invoke(this, e);

        void OnProgressMessageHandler(object? s, string msg)
            => OnProgressMessage?.Invoke(this, msg);

        // Attach progress handlers
        progressParser.OnProgressDownload += OnProgressDownloadHandler;
        progressParser.OnProgressMessage += OnProgressMessageHandler;

        // Forward other events
        progressParser.OnOutputMessage += (_, e) => OnOutputMessage?.Invoke(this, e);
        progressParser.OnCompleteDownload += (_, e) => OnCompleteDownload?.Invoke(this, e);
        progressParser.OnErrorMessage += (_, e) => OnErrorMessage?.Invoke(this, e);
        progressParser.OnPostProcessingComplete += (_, e) => OnPostProcessingComplete?.Invoke(this, e);

        download.OnCommandCompleted += (_, e) => OnCommandCompleted?.Invoke(this, e);

        try
        {
            await download.RunAsync(arguments, ct, tuneProcess);
        }
        finally
        {
            // Unsubscribe immediately after execution to prevent memory leaks
            progressParser.OnProgressDownload -= OnProgressDownloadHandler;
            progressParser.OnProgressMessage -= OnProgressMessageHandler;
        }
    }

    /// <summary>
    /// Executes batch download processing for a collection of URLs with a specified concurrency limit.
    /// </summary>
    /// <param name="urls">An enumerable collection of source URLs to process.</param>
    /// <param name="maxConcurrency">The maximum number of simultaneous yt-dlp processes (default is 3).</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to stop the batch execution.</param>
    /// <param name="tuneProcess">Whether to tune the processes for better performance (true by default). If false, the processes will use the default buffer size and may have slower output processing.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous execution of the process.
    /// </returns>
    /// <exception cref="YtdlpException"></exception>
    public async Task DownloadBatchAsync(IEnumerable<string> urls, int maxConcurrency = 3, CancellationToken ct = default, bool tuneProcess = true)
    {
        if (urls == null || !urls.Any())
        {
            _logger.Log(LogType.Error, "No URLs provided for batch download");
            throw new YtdlpException("No URLs provided for batch download");
        }

        using SemaphoreSlim throttler = new(maxConcurrency);

        var tasks = urls.Select(async url =>
        {
            await throttler.WaitAsync();
            try
            {
                await DownloadAsync(url, ct, tuneProcess);
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

    #region Helpers

    // Get probe runner
    private ProbeRunner Probe()
    {
        // Create isolated execution components
        var factory = new ProcessFactory(_ytdlpPath);
        return new ProbeRunner(factory, _logger);
    }

    private List<string> BuildArguments(string url)
    {
        var args = new List<string>();

        bool usingAbsoluteOutput = !string.IsNullOrWhiteSpace(_outputFolder);

        if (usingAbsoluteOutput && !string.IsNullOrWhiteSpace(_tempFolder))
        {
            _logger.Log(LogType.Debug, "Temp folder ignored because absolute output template is used.");
        }

        // temp folder
        if (!usingAbsoluteOutput && !string.IsNullOrWhiteSpace(_tempFolder))
        {
            args.Add("--paths");
            args.Add($"temp:{_tempFolder.Replace("\\", "/")}");
        }

        // home folder only if NOT using absolute output
        if (!usingAbsoluteOutput && !string.IsNullOrWhiteSpace(_homeFolder))
        {
            args.Add("--paths");
            args.Add($"home:{_homeFolder.Replace("\\", "/")}");
        }

        // Output template
        if (!string.IsNullOrWhiteSpace(_outputTemplate))
        {
            args.Add("-o");

            if (usingAbsoluteOutput)
            {
                var full = Path.Combine(_outputFolder!, _outputTemplate)
                    .Replace("\\", "/");

                args.Add(full);
            }
            else
            {
                args.Add(_outputTemplate);
            }
        }

        // Format
        if (!string.IsNullOrWhiteSpace(_format))
        {
            args.Add("-f");
            args.Add(_format);
        }

        // Concurrent fragments
        if (_concurrentFragments > 1)
        {
            args.Add("--concurrent-fragments");
            args.Add(_concurrentFragments.Value.ToString());
        }

        // Flags
        if (_flags.Length > 0)
            args.AddRange(_flags);

        // Options
        if (_options.Length > 0)
        {
            foreach (var kv in _options)
            {
                args.Add(kv.Key);
                if (kv.Value != null)
                    args.Add(kv.Value);
            }
        }

        // Special single-value options
        if (_cookiesFile is not null) { args.Add("--cookies"); args.Add(_cookiesFile); }
        if (_cookiesFromBrowser is not null) { args.Add("--cookies-from-browser"); args.Add(Quote(_cookiesFromBrowser)); }
        if (_proxy is not null) { args.Add("--proxy"); args.Add(_proxy); }
        if (_ffmpegLocation is not null) { args.Add("--ffmpeg-location"); args.Add(_ffmpegLocation); }
        if (_sponsorblockRemove is not null) { args.Add("--sponsorblock-remove"); args.Add(_sponsorblockRemove); }

        // URL last
        args.Add(url);

        return args;
    }

    private static string ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("yt-dlp path cannot be empty");

        if (!File.Exists(path) && !IsExecutableInPath(path))
            throw new FileNotFoundException($"yt-dlp executable not found: {path}");

        return path;
    }

    private static bool IsExecutableInPath(string name)
    {
        return Environment.GetEnvironmentVariable("PATH")?
            .Split(Path.PathSeparator)
            .Any(p => File.Exists(Path.Combine(p, name))) ?? false;
    }

    private static string Quote(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "\"\"";
        // Escape " and \
        string escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
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

    #endregion

}


