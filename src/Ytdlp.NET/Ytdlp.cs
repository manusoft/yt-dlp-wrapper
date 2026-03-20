using ManuHub.Ytdlp.NET.Core;
using System.Collections.Immutable;
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
/// Each call to <see cref="ExecuteAsync"/> creates isolated runners and parsers, preventing race conditions 
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
/// <see cref="ExecuteAsync"/> call. For advanced scenarios, future versions may implement <see cref="IAsyncDisposable"/> 
/// for global disposal of resources and cancellation support.
/// </remarks>
public sealed class Ytdlp : IAsyncDisposable
{
    // ────────────────────────────────────────────── Frozen configuration
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
    private readonly ImmutableArray<(string Key, string? Value)> _options;


    // Events 
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    public event EventHandler<string>? OnProgressMessage;
    public event EventHandler<string>? OnOutputMessage;
    public event EventHandler<string>? OnCompleteDownload;
    public event EventHandler<string>? OnPostProcessingComplete;
    public event EventHandler<CommandCompletedEventArgs>? OnCommandCompleted;
    public event EventHandler<string>? OnErrorMessage;

    // Flag to prevent double disposal
    private bool _disposed = false;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Optionally, cancel running downloads (if you store CancellationTokens)
        // e.g., _cts?.Cancel();

        await Task.CompletedTask;
    }

    // ────────────────────────────────────────────── Constructors

    public Ytdlp(string ytdlpPath = "yt-dlp", ILogger? logger = null)
    {
        _ytdlpPath = ValidatePath(ytdlpPath);
        _logger = logger ?? new DefaultLogger();

        // defaults
        _outputFolder = Directory.GetCurrentDirectory();
        _tempFolder = null;
        _homeFolder = null;
        _outputTemplate = "%(title)s [%(id)s].%(ext)s";
        _format = "b";
        _concurrentFragments = null;
        _flags = ImmutableArray<string>.Empty;
        _options = ImmutableArray<(string, string?)>.Empty;
        _cookiesFile = null;
        _cookiesFromBrowser = null;
        _proxy = null;
        _ffmpegLocation = null;
        _sponsorblockRemove = null;        
    }

    // Private copy constructor – every WithXxx() uses this
    private Ytdlp(Ytdlp other,
        string? outputFolder = null,
        string? outputTemplate = null,
        string? format = null,
        int? concurrentFragments = null,
        string? cookiesFile = null,
        string? cookiesFromBrowser = null,
        string? proxy = null,
        string? ffmpegLocation = null,
        string? sponsorblockRemove = null,
        string? homeFolder = null,
        string? tempFolder = null,
        IEnumerable<string>? extraFlags = null,
        IEnumerable<(string, string?)>? extraOptions = null)
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

    // ────────────────────────────────────────────── Fluent configuration methods

    public Ytdlp WithOutputFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Output folder required");
        return new Ytdlp(this, outputFolder: Path.GetFullPath(folder));
    }

    public Ytdlp WithHomeFolder(string? path)
        => string.IsNullOrWhiteSpace(path)
            ? this
            : new Ytdlp(this, homeFolder: Path.GetFullPath(path));

    public Ytdlp WithTempFolder(string? path)
        => string.IsNullOrWhiteSpace(path)
            ? this
            : new Ytdlp(this, tempFolder: Path.GetFullPath(path));

    public Ytdlp WithFFmpegLocation(string? path)
        => string.IsNullOrWhiteSpace(path)
            ? this
            : new Ytdlp(this, ffmpegLocation: path);

    public Ytdlp WithOutputTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template)) throw new ArgumentException("Template required");
        return new Ytdlp(this, outputTemplate: template.Trim());
    }

    public Ytdlp WithFormat(string format)
        => new Ytdlp(this, format: format.Trim());

    public Ytdlp WithConcurrentFragments(int count = 8)
        => count > 0
            ? new Ytdlp(this, concurrentFragments: count)
            : this;



    public Ytdlp WithProxy(string? proxy)
        => string.IsNullOrWhiteSpace(proxy)
            ? this
            : new Ytdlp(this, proxy: proxy);

    public Ytdlp WithCookiesFile(string? path)
        => string.IsNullOrWhiteSpace(path)
            ? this
            : new Ytdlp(this, cookiesFile: Path.GetFullPath(path));

    public Ytdlp WithCookiesFromBrowser(string browser)
        => new Ytdlp(this, cookiesFromBrowser: browser);

    public Ytdlp WithSponsorblockRemove(string? categories = "all")
        => string.IsNullOrWhiteSpace(categories)
            ? this
            : new Ytdlp(this, sponsorblockRemove: categories);

    public Ytdlp WithExtractAudio(string format = "mp3", int quality = 5)
        => new Ytdlp(this,
            extraFlags: new[] { "--extract-audio" },
            extraOptions: new[]
            {
                ("--audio-format",   format),
                ("--audio-quality",  quality.ToString(CultureInfo.InvariantCulture))
            });

    public Ytdlp WithSubtitles(string langs = "all", bool auto = false)
    {
        var flags = new List<string> { "--write-subs" };
        if (auto) flags.Add("--write-auto-subs");

        return new Ytdlp(this,
            extraFlags: flags,
            extraOptions: new[] { ("--sub-langs", langs) });
    }

    public Ytdlp WithEmbedSubtitles(string langs = "all", string? convertTo = null)
    {
        var flags = new List<string> { "--embed-subs", "--write-subs" };
        var options = new List<(string, string?)> { ("--sub-langs", langs) };

        if (!string.IsNullOrWhiteSpace(convertTo))
            options.Add(("--convert-subs", convertTo));

        return new Ytdlp(this, extraFlags: flags, extraOptions: options);
    }

    public Ytdlp WithThumbnails(bool all = false)
        => new Ytdlp(this, extraFlags: new[] { all ? "--write-all-thumbnails" : "--write-thumbnail" });

    public Ytdlp WithEmbedThumbnail() => new Ytdlp(this, extraFlags: new[] { "--embed-thumbnail" });
    public Ytdlp WithEmbedMetadata() => new Ytdlp(this, extraFlags: new[] { "--embed-metadata" });
    public Ytdlp WithEmbedChapters() => new Ytdlp(this, extraFlags: new[] { "--embed-chapters" });

    public Ytdlp WithAria2(int connections = 16)
    {
        return new Ytdlp(this,
            extraOptions: new[]
            {
            ("--downloader", "aria2c"),
            ("--downloader-args", $"aria2c:-x{connections} -k1M")
            });
    }

    // 1. Playlist selection (items to download)
    public Ytdlp WithPlaylistItems(string items)
    {
        if (string.IsNullOrWhiteSpace(items))
            throw new ArgumentException("Playlist items string cannot be empty", nameof(items));
        return new Ytdlp(this, extraOptions: new[] { ("--playlist-items", items.Trim()) });
    }

    // 2. Playlist start index
    public Ytdlp WithPlaylistStart(int index)
    {
        if (index < 1) throw new ArgumentOutOfRangeException(nameof(index), "Must be >= 1");
        return new Ytdlp(this, extraOptions: new[] { ("--playlist-start", index.ToString()) });
    }

    // 3. Playlist end index
    public Ytdlp WithPlaylistEnd(int index)
    {
        if (index < 1) throw new ArgumentOutOfRangeException(nameof(index), "Must be >= 1");
        return new Ytdlp(this, extraOptions: new[] { ("--playlist-end", index.ToString()) });
    }

    // 4. Minimum filesize
    public Ytdlp WithMinFileSize(string size)
    {
        // size examples: 50k, 4.2M, 1G
        if (string.IsNullOrWhiteSpace(size))
            throw new ArgumentException("Size cannot be empty", nameof(size));
        return new Ytdlp(this, extraOptions: new[] { ("--min-filesize", size.Trim()) });
    }

    // 5. Maximum filesize
    public Ytdlp WithMaxFileSize(string size)
    {
        if (string.IsNullOrWhiteSpace(size))
            throw new ArgumentException("Size cannot be empty", nameof(size));
        return new Ytdlp(this, extraOptions: new[] { ("--max-filesize", size.Trim()) });
    }

    // 6. Date filter (upload date)
    public Ytdlp WithUploadDate(string date)
    {
        // formats: YYYYMMDD, today, yesterday, now-2weeks, etc.
        if (string.IsNullOrWhiteSpace(date))
            throw new ArgumentException("Date cannot be empty", nameof(date));
        return new Ytdlp(this, extraOptions: new[] { ("--date", date.Trim()) });
    }

    // 7. Age limit / restriction
    public Ytdlp WithAgeLimit(int years)
    {
        if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
        return new Ytdlp(this, extraOptions: new[] { ("--age-limit", years.ToString()) });
    }

    // 8. User-Agent override
    public Ytdlp WithUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("User-Agent cannot be empty", nameof(userAgent));
        return new Ytdlp(this, extraOptions: new[] { ("--user-agent", userAgent.Trim()) });
    }

    // 9. Referer override
    public Ytdlp WithReferer(string referer)
    {
        if (string.IsNullOrWhiteSpace(referer))
            throw new ArgumentException("Referer cannot be empty", nameof(referer));
        return new Ytdlp(this, extraOptions: new[] { ("--referer", referer.Trim()) });
    }

    // 10. Sleep interval between requests (anti-rate-limit)
    public Ytdlp WithSleepInterval(double seconds, double? maxSeconds = null)
    {
        if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        var opts = new List<(string, string?)> { ("--sleep-requests", seconds.ToString("F2", CultureInfo.InvariantCulture)) };
        if (maxSeconds.HasValue && maxSeconds > seconds)
        {
            opts.Add(("--max-sleep-requests", maxSeconds.Value.ToString("F2", CultureInfo.InvariantCulture)));
        }
        return new Ytdlp(this, extraOptions: opts);
    }

    // 11. Sleep between subtitle downloads
    public Ytdlp WithSleepSubtitles(double seconds)
    {
        if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        return new Ytdlp(this, extraOptions: new[] { ("--sleep-subtitles", seconds.ToString("F2", CultureInfo.InvariantCulture)) });
    }

    // 12. Download archive file (skip already downloaded)
    public Ytdlp WithDownloadArchive(string archivePath = "archive.txt")
    {
        if (string.IsNullOrWhiteSpace(archivePath))
            throw new ArgumentException("Archive path cannot be empty", nameof(archivePath));
        return new Ytdlp(this, extraOptions: new[] { ("--download-archive", Path.GetFullPath(archivePath)) });
    }

    // 13. Match title (regex include)
    public Ytdlp WithMatchTitle(string regex)
    {
        if (string.IsNullOrWhiteSpace(regex))
            throw new ArgumentException("Regex cannot be empty", nameof(regex));
        return new Ytdlp(this, extraOptions: new[] { ("--match-title", regex.Trim()) });
    }

    // 14. Reject title (regex exclude)
    public Ytdlp WithRejectTitle(string regex)
    {
        if (string.IsNullOrWhiteSpace(regex))
            throw new ArgumentException("Regex cannot be empty", nameof(regex));
        return new Ytdlp(this, extraOptions: new[] { ("--reject-title", regex.Trim()) });
    }

    // 15. Max downloads (stop after N videos)
    public Ytdlp WithMaxDownloads(int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        return new Ytdlp(this, extraOptions: new[] { ("--max-downloads", count.ToString()) });
    }

    // Nice-to-have #16
    public Ytdlp WithNoMtime() => new Ytdlp(this, extraFlags: new[] { "--no-mtime" });

    // Nice-to-have #17
    public Ytdlp WithNoCacheDir() => new Ytdlp(this, extraFlags: new[] { "--no-cache-dir" });

    // Nice-to-have #18 – very popular for high-quality + fallback
    public Ytdlp With1080pOrBest()
        => new Ytdlp(this, format: "bestvideo[height<=?1080]+bestaudio/best");

    // Nice-to-have #19
    public Ytdlp WithNoPlaylist() => new Ytdlp(this, extraFlags: new[] { "--no-playlist" });

    // Nice-to-have #20
    public Ytdlp WithYesPlaylist() => new Ytdlp(this, extraFlags: new[] { "--yes-playlist" });


    // 21. Geo-bypass country (two-letter ISO code)
    public Ytdlp WithGeoBypassCountry(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2)
            throw new ArgumentException("Geo-bypass country must be a 2-letter ISO code", nameof(countryCode));

        return new Ytdlp(this,
            extraOptions: new[] { ("--geo-bypass-country", countryCode.Trim().ToUpperInvariant()) });
    }

    // 22. No geo-bypass (disable automatic country bypass)
    public Ytdlp WithNoGeoBypass()
        => new Ytdlp(this, extraFlags: new[] { "--no-geo-bypass" });

    // 23. Match-filter (advanced filter expression)
    public Ytdlp WithMatchFilter(string filterExpression)
    {
        if (string.IsNullOrWhiteSpace(filterExpression))
            throw new ArgumentException("Match filter expression cannot be empty", nameof(filterExpression));

        return new Ytdlp(this,
            extraOptions: new[] { ("--match-filter", filterExpression.Trim()) });
    }

    // 24. Break on existing (stop when file already in archive)
    public Ytdlp WithBreakOnExisting()
        => new Ytdlp(this, extraFlags: new[] { "--break-on-existing" });

    // 25. Break on reject (stop when a video is filtered out by --match-filter)
    public Ytdlp WithBreakOnReject()
        => new Ytdlp(this, extraFlags: new[] { "--break-on-reject" });

    // 26. Postprocessor args (ppa) - most common use-cases
    public Ytdlp WithPostprocessorArgs(string postprocessorName, string arguments)
    {
        if (string.IsNullOrWhiteSpace(postprocessorName) || string.IsNullOrWhiteSpace(arguments))
            throw new ArgumentException("Both postprocessor name and arguments are required");

        string combined = $"{postprocessorName.Trim()}:{arguments.Trim()}";
        return new Ytdlp(this,
            extraOptions: new[] { ("--postprocessor-args", combined) });
    }

    // 27. Force key frames at cuts (useful when cutting with --download-sections)
    public Ytdlp WithForceKeyframesAtCuts()
        => new Ytdlp(this, extraFlags: new[] { "--force-keyframes-at-cuts" });

    // 28. Prefer free formats (when multiple formats have similar quality)
    public Ytdlp WithPreferFreeFormats()
        => new Ytdlp(this, extraFlags: new[] { "--prefer-free-formats" });

    // 29. No prefer free formats (default behavior - explicit)
    public Ytdlp WithNoPreferFreeFormats()
        => new Ytdlp(this, extraFlags: new[] { "--no-prefer-free-formats" });

    // 30. Merge output format (force container after download & post-processing)
    public Ytdlp WithMergeOutputFormat(string format)
    {
        // Common values: mp4, mkv, webm, mov, avi, flv
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Merge output format cannot be empty", nameof(format));

        return new Ytdlp(this,
            extraOptions: new[] { ("--merge-output-format", format.Trim().ToLowerInvariant()) });
    }

    // Bonus 31 – very popular shortcut
    public Ytdlp WithBestUpTo1080p()
        => new Ytdlp(this, format: "bestvideo[height<=?1080]+bestaudio/best");

    // Bonus 32
    public Ytdlp WithKeepFragments()
        => new Ytdlp(this, extraFlags: new[] { "--keep-fragments" });

    // Bonus 33 – useful for debugging
    public Ytdlp WithVerbose()
        => new Ytdlp(this, extraFlags: new[] { "--verbose" });


    // 31. Reverse playlist order
    public Ytdlp WithPlaylistReverse()
        => new Ytdlp(this, extraFlags: new[] { "--playlist-reverse" });

    // 32. Random playlist order
    public Ytdlp WithPlaylistRandom()
        => new Ytdlp(this, extraFlags: new[] { "--playlist-random" });

    // 33. Lazy playlist (process entries as received – good for very large playlists)
    public Ytdlp WithLazyPlaylist()
        => new Ytdlp(this, extraFlags: new[] { "--lazy-playlist" });

    // 34. Flat playlist (do not extract individual video URLs – faster for listing)
    public Ytdlp WithFlatPlaylist()
        => new Ytdlp(this, extraFlags: new[] { "--flat-playlist" });

    // 35. Write info.json metadata file
    public Ytdlp WithWriteInfoJson()
        => new Ytdlp(this, extraFlags: new[] { "--write-info-json" });

    // 36. Clean info.json (remove private/empty fields)
    public Ytdlp WithCleanInfoJson()
        => new Ytdlp(this, extraFlags: new[] { "--clean-info-json" });

    // 37. No clean info.json (keep all fields)
    public Ytdlp WithNoCleanInfoJson()
        => new Ytdlp(this, extraFlags: new[] { "--no-clean-info-json" });

    // 38. Simulate only (do not download anything – useful for testing/format listing)
    public Ytdlp WithSimulate()
        => new Ytdlp(this, extraFlags: new[] { "--simulate" });

    // 39. Skip actual download (but do post-processing if applicable)
    public Ytdlp WithSkipDownload()
        => new Ytdlp(this, extraFlags: new[] { "--skip-download" });

    // 40. Write description to .description file
    public Ytdlp WithWriteDescription()
        => new Ytdlp(this, extraFlags: new[] { "--write-description" });

    // 41. Keep intermediate video file after post-processing
    public Ytdlp WithKeepVideo()
        => new Ytdlp(this, extraFlags: new[] { "-k", "--keep-video" });

    // 42. Do not overwrite post-processed files
    public Ytdlp WithNoPostOverwrites()
        => new Ytdlp(this, extraFlags: new[] { "--no-post-overwrites" });

    // 43. Force keyframes at cuts (important when using --download-sections)


    // 44. Remux video into specified container format
    public Ytdlp WithRemuxVideo(string format = "mp4")
    {
        // Supported: mp4, mkv, avi, webm, flv, mov, ...
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Remux format cannot be empty", nameof(format));

        return new Ytdlp(this,
            extraOptions: new[] { ("--remux-video", format.Trim().ToLowerInvariant()) });
    }

    // 45. Recode / re-encode video into specified format
    public Ytdlp WithRecodeVideo(string format = "mp4")
    {
        // Supported: mp4, mkv, avi, webm, flv, mov, ...
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Recode format cannot be empty", nameof(format));

        return new Ytdlp(this,
            extraOptions: new[] { ("--recode-video", format.Trim().ToLowerInvariant()) });
    }

    // 46. Convert thumbnails to specified format
    public Ytdlp WithConvertThumbnails(string format = "jpg")
    {
        // Supported: jpg, png, webp
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Thumbnail format cannot be empty", nameof(format));

        return new Ytdlp(this,
            extraOptions: new[] { ("--convert-thumbnails", format.Trim().ToLowerInvariant()) });
    }

    // 48. Postprocessor arguments for Merger (most common use-case)
    public Ytdlp WithMergerArgs(string args)
        => WithPostprocessorArgs("Merger", args);

    // 49. Postprocessor arguments for ModifyChapters
    public Ytdlp WithModifyChaptersArgs(string args)
        => WithPostprocessorArgs("ModifyChapters", args);

    // 50. Postprocessor arguments for ExtractAudio
    public Ytdlp WithExtractAudioArgs(string args)
        => WithPostprocessorArgs("ExtractAudio", args);

    // Bonus – common combo: remux to mp4 + embed metadata + chapters + thumbnail
    public Ytdlp WithMp4PostProcessingPreset()
        => this
            .WithRemuxVideo("mp4")
            .WithEmbedMetadata()
            .WithEmbedChapters()
            .WithEmbedThumbnail();

    // Bonus – force mkv container (popular for archiving)
    public Ytdlp WithMkvOutput()
        => new Ytdlp(this,
            extraOptions: new[]
            {
            ("--remux-video", "mkv"),
            ("--merge-output-format", "mkv")
            });

    // 51. Download livestream from the start (when possible)
    public Ytdlp WithLiveFromStart()
        => new Ytdlp(this, extraFlags: new[] { "--live-from-start" });

    // 52. Explicitly disable downloading from the beginning of a live stream
    public Ytdlp WithNoLiveFromStart()
        => new Ytdlp(this, extraFlags: new[] { "--no-live-from-start" });

    // 53. Wait for a scheduled live stream to start
    public Ytdlp WithWaitForVideo(TimeSpan? maxWait = null)
    {
        var opts = new List<(string Key, string? Value)>();

        opts.Add(("--wait-for-video", "any"));   // "any" = wait indefinitely or until timeout

        if (maxWait.HasValue && maxWait.Value.TotalSeconds > 0)
        {
            opts.Add(("--wait-for-video", maxWait.Value.TotalSeconds.ToString("F0")));
        }

        return new Ytdlp(this, extraOptions: opts);
    }

    // 54. Wait until the live stream actually ends before finishing
    public Ytdlp WithWaitUntilLiveEnds()
        => new Ytdlp(this, extraFlags: new[] { "--wait-for-video-to-end" });

    // 55. Use mpegts container/format for HLS live streams (better compatibility in some players)
    public Ytdlp WithHlsUseMpegts()
        => new Ytdlp(this, extraFlags: new[] { "--hls-use-mpegts" });

    // 56. Do not use mpegts for HLS (use default fragmented mp4)
    public Ytdlp WithNoHlsUseMpegts()
        => new Ytdlp(this, extraFlags: new[] { "--no-hls-use-mpegts" });

    // 57. External downloader for live streams (e.g. ffmpeg, aria2c, ...)
    public Ytdlp WithExternalDownloader(string downloaderName, string? downloaderArgs = null)
    {
        if (string.IsNullOrWhiteSpace(downloaderName))
            throw new ArgumentException("Downloader name cannot be empty", nameof(downloaderName));

        var opts = new List<(string, string?)> { ("--downloader", downloaderName.Trim()) };

        if (!string.IsNullOrWhiteSpace(downloaderArgs))
        {
            opts.Add(("--downloader-args", downloaderArgs.Trim()));
        }

        return new Ytdlp(this, extraOptions: opts);
    }

    // 58. Use ffmpeg as external downloader for live streams (most common choice)
    public Ytdlp WithFfmpegAsLiveDownloader(string? extraFfmpegArgs = null)
        => WithExternalDownloader("ffmpeg", extraFfmpegArgs);

    // 59. Set fragment retries specifically useful for unstable live streams
    public Ytdlp WithFragmentRetries(int retries)
    {
        // -1 = infinite
        string value = retries < 0 ? "infinite" : retries.ToString();
        return new Ytdlp(this,
            extraOptions: new[] { ("--fragment-retries", value) });
    }

    // 60. Prefer native HLS downloader (instead of ffmpeg) – sometimes more stable
    public Ytdlp WithHlsNative()
        => new Ytdlp(this, extraOptions: new[] { ("--downloader", "hlsnative") });


    // 63. Maximum video height / resolution limit
    public Ytdlp WithMaxHeight(int height)
    {
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive");

        string formatSelector = $"bestvideo[height<={height}]+bestaudio/best";
        return new Ytdlp(this, format: formatSelector);
    }

    // 64. Maximum video height with fallback to best available
    public Ytdlp WithMaxHeightOrBest(int height)
    {
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive");

        string formatSelector = $"bestvideo[height<={height}]+bestaudio/best[height<={height}]/best";
        return new Ytdlp(this, format: formatSelector);
    }

    // 65. Best video + best audio (classic high-quality merge)
    public Ytdlp WithBestVideoPlusBestAudio()
        => new Ytdlp(this, format: "bestvideo+bestaudio/best");

    // 67. Best video up to 720p + best audio
    public Ytdlp With720pOrBest()
        => new Ytdlp(this, format: "bv*[height<=?720]+ba/best/best");
    

    // 68. Audio-only – best quality audio
    public Ytdlp WithBestAudioOnly()
        => new Ytdlp(this, format: "bestaudio");

    // 69. Prefer video formats with higher bitrate (when resolution is similar)
    public Ytdlp WithFormatSortBitrate()
        => new Ytdlp(this, extraOptions: new[] { ("-S", "br") });

    // 70. Prefer formats with higher resolution first, then bitrate
    public Ytdlp WithFormatSortResolutionThenBitrate()
        => new Ytdlp(this, extraOptions: new[] { ("-S", "res,br") });


    // Bonus A – very popular preset
    public Ytdlp WithBestUpTo1440p()
        => new Ytdlp(this, format: "bestvideo[height<=?1440]+bestaudio/best");

    // Bonus B – avoid very high resolutions (4K+)
    public Ytdlp WithNo4k()
        => new Ytdlp(this, format: "bestvideo[height<=?2160]+bestaudio/best");

    // Bonus C – audio-only with specific codec preference
    public Ytdlp WithBestM4aAudio()
        => new Ytdlp(this, format: "bestaudio[ext=m4a]/bestaudio/best");

    // 71. Restrict filenames to ASCII-only + avoid problematic characters
    public Ytdlp WithRestrictFilenames()
        => new Ytdlp(this, extraFlags: new[] { "--restrict-filenames" });

    // 72. Force Windows-compatible filenames (avoid reserved names, invalid chars)
    public Ytdlp WithWindowsFilenames()
        => new Ytdlp(this, extraFlags: new[] { "--windows-filenames" });

    // 73. Limit filename length (excluding extension)
    public Ytdlp WithTrimFilenames(int maxLength)
    {
        if (maxLength < 10)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Length should be at least 10 characters");

        return new Ytdlp(this,
            extraOptions: new[] { ("--trim-filenames", maxLength.ToString()) });
    }

    // 74. No overwrite existing files
    public Ytdlp WithNoOverwrites()
        => new Ytdlp(this, extraFlags: new[] { "--no-overwrites" });

    // 75. Force overwrite existing files
    public Ytdlp WithForceOverwrites()
        => new Ytdlp(this, extraFlags: new[] { "--force-overwrites" });

    // 76. Continue partially downloaded files
    public Ytdlp WithContinue()
        => new Ytdlp(this, extraFlags: new[] { "--continue" });

    // 77. Do not continue partially downloaded files (start from beginning)
    public Ytdlp WithNoContinue()
        => new Ytdlp(this, extraFlags: new[] { "--no-continue" });

    // 78. Use .part files during download
    public Ytdlp WithPartFiles()
        => new Ytdlp(this, extraFlags: new[] { "--part" });

    // 79. Do not use .part files (write directly to final filename)
    public Ytdlp WithNoPartFiles()
        => new Ytdlp(this, extraFlags: new[] { "--no-part" });

    // 80. Use server mtime (Last-Modified header) for file timestamp
    public Ytdlp WithMtime()
        => new Ytdlp(this, extraFlags: new[] { "--mtime" });


    public Ytdlp AddFlag(string flag)
        => new Ytdlp(this, extraFlags: new[] { flag.Trim() });

    public Ytdlp AddOption(string key, string? value = null)
        => new Ytdlp(this, extraOptions: new[] { (key.Trim(), value) });

    // ────────────────────────────────────────────── Command building (called only at execution time)

    private List<string> BuildArguments(string url)
    {
        var args = new List<string>();

        // Paths — use home, temp, and outputFolder separately
        if (!string.IsNullOrWhiteSpace(_homeFolder))
        {
            args.Add("--paths");
            args.Add($"home:{_homeFolder}");
        }

        if (!string.IsNullOrWhiteSpace(_tempFolder))
        {
            args.Add("--paths");
            args.Add($"temp:{_tempFolder}");
        }

        // Output folder is only for -o template
        if (!string.IsNullOrWhiteSpace(_outputFolder) && !string.IsNullOrWhiteSpace(_outputTemplate))
        {
            // Combine folder + template
            string fullOutputPath = Path.Combine(_outputFolder, _outputTemplate)
                                        .Replace('\\', '/'); // yt-dlp prefers forward slashes
            args.Add("-o");
            args.Add(fullOutputPath);
        }
        else if (!string.IsNullOrWhiteSpace(_outputTemplate))
        {
            args.Add("-o");
            args.Add(_outputTemplate);
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

    public string Preview(string url)
    {
        var argsList = BuildArguments(url);
        return string.Join(" ", argsList.Select(Quote));
    }

    // ────────────────────────────────────────────── Execution

    public async Task ExecuteAsync(string url, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL required", nameof(url));

        // Ensure output folder exists
        try
        {
            Directory.CreateDirectory(_outputFolder);
            _logger.Log(LogType.Info, $"Ensured output folder exists: {_outputFolder}");
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Error, $"Failed to create output folder: {ex.Message}");
            throw new YtdlpException("Failed to create output folder", ex);
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
            await download.RunAsync(arguments, ct);
        }
        finally
        {
            // Unsubscribe immediately after execution to prevent memory leaks
            progressParser.OnProgressDownload -= OnProgressDownloadHandler;
            progressParser.OnProgressMessage -= OnProgressMessageHandler;
        }
    }

    // ────────────────────────────────────────────── Helpers

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

    #region Execution & Utility Methods


    /// <summary>
    /// Retrieves the current version string of the underlying yt-dlp executable.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to abort the version check process.</param>
    /// <returns>
    /// A <see cref="string"/> representing the yt-dlp version (e.g., "2023.03.04"); 
    /// returns an empty string or throws if the binary cannot be found.
    /// </returns>
    public async Task<string> GetVersionAsync(CancellationToken ct = default)
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
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to abort the download and installation process.</param>
    /// <returns>
    /// A <see cref="string"/> containing the update log or the new version number; 
    /// returns an empty string or throws if the update process fails.
    /// </returns>
    public async Task<string> UpdateAsync(UpdateChannel channel = UpdateChannel.Stable, CancellationToken cancellationToken = default)
    {
        var output = await Probe().RunAsync($"--update-to {channel.ToString().ToLowerInvariant()}", cancellationToken);
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
    ///  Fetches video metadata from the specified URL.
    /// </summary>
    /// <param name = "url">The source URL(video or playlist) to probe.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to abort the process.</param>
    /// <param name="bufferKb">Buffer size in KB.</param>
    /// <returns>
    /// A <see cref="Metadata"/> object containing the parsed metadata output; 
    /// returns <see langword="null"/> if the process fails, returns empty, or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Metadata?> GetMetadataAsync(string url, CancellationToken ct = default, int bufferKb = 128)
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

            var json = await Probe().RunAsync(arguments, ct);

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
    /// <param name="bufferKb">The buffer size for the process output stream (default 128KB).</param>
    /// <returns>
    /// A raw JSON <see cref="object"/> containing the parsed metadata output; 
    /// returns <see langword="null"/> if the process fails, returns empty, or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<object?> GetMetadataRawAsync(string url, CancellationToken ct = default, int bufferKb = 128)
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

            var json = await Probe().RunAsync(arguments, ct);

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
    /// <param name="bufferKb">The buffer size in kilobytes for the process output stream (default 128KB).</param>
    /// <returns>
    /// A <see cref="List{Format}"/> containing all available streams; 
    /// returns an empty list or <see langword="null"/> if the probe fails or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<List<Format>> GetAvailableFormatsAsync(string url, CancellationToken ct = default, int bufferKb = 128)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Video URL cannot be empty.", nameof(url));

        var output = await Probe().RunAsync($"-F {Quote(url)}", ct, bufferKb);

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
    /// <param name="bufferKb">The buffer size in kilobytes for the process output stream (default 128KB).</param>
    /// <returns>
    /// A <see cref="MetadataLight"/> object if successful; 
    /// returns <see langword="null"/> if the process fails or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<MetadataLight?> GetMetadataLiteAsync(string url, CancellationToken ct = default, int bufferKb = 128)
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

            var output = await Probe().RunAsync(arguments, ct, bufferKb);

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
    /// <param name="bufferKb">The buffer size in kilobytes for the process output (default 128KB).</param>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> containing the requested fields and their values; 
    /// returns <see langword="null"/> if the process fails, returns no data, or is cancelled.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Dictionary<string, string>?> GetMetadataLiteAsync(string url, IEnumerable<string> fields, CancellationToken ct = default, int bufferKb = 128)
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

            var rawOutput = await Probe().RunAsync(arguments, ct, bufferKb);
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
    /// <param name="bufferKb">The buffer size in kilobytes for the process output stream (default 128KB).</param>
    /// <returns>
    /// A <see cref="string"/> representing the best audio format ID (e.g., "140"); 
    /// returns an empty string or throws if no suitable audio is found.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<string> GetBestAudioFormatIdAsync(string url, CancellationToken ct = default, int bufferKb = 128)
    {
        var meta = await GetMetadataAsync(url, ct, bufferKb);
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
    /// <param name="bufferKb">The buffer size in kilobytes for the process output (default 128KB).</param>
    /// <returns>
    /// A <see cref="string"/> representing the best video format ID (e.g., "137" or "248"); 
    /// returns an empty string or <see langword="null"/> if no suitable format is found.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<string> GetBestVideoFormatIdAsync(string url, int maxHeight = 1080, CancellationToken ct = default, int bufferKb = 128)
    {
        var meta = await GetMetadataAsync(url, ct, bufferKb);
        var best = meta?.Formats?
            .Where(f => !f.IsAudio && f.Height.HasValue && f.Height <= maxHeight)
            .OrderByDescending(f => f.Height)
            .ThenByDescending(f => f.Fps ?? 0)
            .FirstOrDefault();

        return best?.FormatId ?? "bestvideo";
    }


    private ProbeRunner Probe()
    {
        // Create isolated execution components
        var factory = new ProcessFactory(_ytdlpPath);
        return new ProbeRunner(factory, _logger);
    }


    /// <summary>
    /// Executes batch download processing for a collection of URLs with a specified concurrency limit.
    /// </summary>
    /// <param name="urls">An enumerable collection of source URLs to process.</param>
    /// <param name="maxConcurrency">The maximum number of simultaneous yt-dlp processes (default is 3).</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to stop the batch execution.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous execution of the process.
    /// </returns>
    /// <exception cref="YtdlpException"></exception>
    public async Task ExecuteBatchAsync(IEnumerable<string> urls, int maxConcurrency = 3, CancellationToken ct = default)
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
                await ExecuteAsync(url, ct);
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


