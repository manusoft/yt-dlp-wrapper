using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace ManuHub.Ytdlp;

public sealed class YtdlpBuilder
{
    #region Frozen state

    internal string YtDlpPath;
    internal bool IsProbe;
    internal ILogger Logger;
    internal string OutputFolder;
    internal string OutputTemplate;
    internal string Format;
    internal int? ConcurrentFragments;
    internal ImmutableArray<string> Flags;
    internal ImmutableArray<(string Key, string? Value)> Options;
    internal string? CookiesFile;
    internal string? CookiesFromBrowser;
    internal string? Proxy;
    internal string? FfmpegLocation;
    internal string? SponsorblockRemoveCategories;
    internal string? HomeFolder; // yt-dlp --config-location or working dir
    internal string? TempFolder; // temporary files during download

    #endregion

    #region Constructors
    public YtdlpBuilder(string? ytDlpPath = null, ILogger? logger = null)
    {
        YtDlpPath = ytDlpPath ?? "yt-dlp"; // or resolve from path
        IsProbe = false;
        Logger = logger ?? new DefaultLogger();
        OutputFolder = Directory.GetCurrentDirectory();
        OutputTemplate = "%(title)s [%(id)s].%(ext)s";
        Format = "b";
        ConcurrentFragments = null;
        Flags = ImmutableArray<string>.Empty;
        Options = ImmutableArray<(string Key, string? Value)>.Empty;
        CookiesFile = null;
        CookiesFromBrowser = null;
        Proxy = null;
        FfmpegLocation = null;
        SponsorblockRemoveCategories = null;
        HomeFolder = null; // no default, user must set if needed
        TempFolder = null; // same
    }

    private YtdlpBuilder(YtdlpBuilder other)
    {
        YtDlpPath = other.YtDlpPath;
        IsProbe = other.IsProbe;
        Logger = other.Logger;
        OutputFolder = other.OutputFolder;
        OutputTemplate = other.OutputTemplate;
        Format = other.Format;
        ConcurrentFragments = other.ConcurrentFragments;
        Flags = other.Flags;
        Options = other.Options;
        CookiesFile = other.CookiesFile;
        CookiesFromBrowser = other.CookiesFromBrowser;
        Proxy = other.Proxy;
        FfmpegLocation = other.FfmpegLocation;
        SponsorblockRemoveCategories = other.SponsorblockRemoveCategories;
        HomeFolder = other.HomeFolder;
        TempFolder = other.TempFolder;
    }

    #endregion

    #region General Options

    /// <summary>
    /// Additional JavaScript runtime to enable, with an optional location for the runtime (either the path to the binary or its containing directory).
    /// This option can be used multiple times to enable multiple runtimes. Supported runtimes are (in order of priority, from highest to lowest): deno, node, quickjs, bun.
    /// Only "deno" is enabled by default. The highest priority runtime that is both enabled and available will be used. 
    /// In order to use a lower priority runtime when "deno" is available, NoJsRuntime() needs to be passed before enabling other runtimes
    /// </summary>
    /// <param name="runtime">Supported runtimes are deno, node, quickjs, bun</param>
    /// <param name="runtimePath"></param>
    public YtdlpBuilder WithJsRuntime(string runtime, string runtimePath)
    {
        var builder = $"{runtime}:{runtimePath}";
        return AddOption("--js-runtime", builder);
    }

    /// <summary>
    /// Clear JavaScript runtimes to enable, including defaults and those provided by WithJsRuntime()
    /// </summary>
    public YtdlpBuilder NoJsRuntime() => AddFlag("--no-js-runtime");

    /// <summary>
    /// Do not extract a playlist's URL result entries; some entry metadata may be missing and downloading may be bypassed
    /// </summary>
    public YtdlpBuilder FlatPlaylist() => AddFlag("--flat-playlist");

    /// <summary>
    /// Download livestreams from the start. Currently experimental and only supported for YouTube, Twitch, and TVer.
    /// </summary>
    public YtdlpBuilder DownloadLivestream() => AddFlag("--live-from-start");

    /// <summary>
    /// Mark videos watched (even with Simulate())
    /// </summary>
    public YtdlpBuilder MarkWatched() => AddFlag("--mark-watched");

    #endregion

    #region Network Options

    /// <summary>
    /// Use the specified HTTP/HTTPS/SOCKS proxy. To enable SOCKS proxy, specify a proper scheme, e.g. socks5://user:pass@127.0.0.1:1080/.
    /// </summary>
    /// <param name="url">Pass in an empty string for direct connection</param>
    public YtdlpBuilder WithProxy(string proxy) => Copy(b => b.Proxy = proxy);

    /// <summary>
    /// Time to wait before giving up, in seconds
    /// </summary>
    /// <param name="timeout"></param>
    public YtdlpBuilder WithSocketTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero) return this;
        double seconds = timeout.TotalSeconds;
        return AddOption("--socket-timeout", seconds.ToString("F0"));
    }

    /// <summary>
    /// Make all connections via IPv4
    /// </summary>
    public YtdlpBuilder ForceIpv4() => AddFlag("--force-ipv4");

    /// <summary>
    /// Make all connections via IPv6
    /// </summary>
    public YtdlpBuilder ForceIpv6() => AddFlag("--force-ipv6");

    /// <summary>
    /// Enable file:// URLs. This is disabled by default for security reasons.
    /// </summary>
    public YtdlpBuilder EnableFileUrl() => AddFlag("--enable-file-url");

    #endregion

    #region Geo-restriction

    /// <summary>
    /// Use this proxy to verify the IP address for some geo-restricted sites. 
    /// The default proxy specified by WithProxy() (or none, if the option is not present) is used for the actual downloading
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public YtdlpBuilder WithGeoVerificationProxy(string url) => AddOption("--geo-verification-proxy", url);

    /// <summary>
    /// How to fake X-Forwarded-For HTTP header to try bypassing geographic restriction. One of "default" (only when known to be useful),
    /// "never", an IP block in CIDR notation, or a two-letter ISO 3166-2 country code
    /// </summary>
    /// <param name="countryCode"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public YtdlpBuilder WithGeoBypassCountry(string countryCode)
    {
        if (countryCode.Length != 2) throw new ArgumentException("Country code must be 2 letters.");
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
    public YtdlpBuilder WithPlaylistItems(string items)
    {
        if (string.IsNullOrWhiteSpace(items))
            throw new ArgumentException("Playlist items cannot be empty.", nameof(items));
        return AddOption("--playlist-items", items);
    }


    /// <summary>
    /// Abort download if filesize is smaller than SIZE
    /// </summary>
    /// <param name="size">e.g. 50k or 44.6M</param>
    public YtdlpBuilder WithMinFileSize(string size) => AddOption("--min-filesize", size);

    /// <summary>
    /// Abort download if filesize is larger than SIZE
    /// </summary>
    /// <param name="size">e.g. 50k or 44.6M</param>
    public YtdlpBuilder WithMaxFileSize(string size) => AddOption("--max-filesize", size);

    /// <summary>
    /// Download only videos uploaded on this date.
    /// The date can be "YYYYMMDD" or in the format [now|today|yesterday][-N[day|week|month|year]].
    /// E.g. "--date today-2weeks" downloads only videos uploaded on the same day two weeks ago
    /// </summary>
    /// <param name="date">"today-2weeks" or "YYYYMMDD"</param>
    public YtdlpBuilder WithDate(string date) => AddOption("--date", date);

    /// <summary>
    /// Download only the video, if the URL refers to a video and a playlist
    /// </summary>
    /// <returns></returns>
    public YtdlpBuilder NoPlaylist() => AddFlag("--no-playlist");

    /// <summary>
    /// Download the playlist, if the URL refers to a video and a playlist
    /// </summary>
    /// <returns></returns>
    public YtdlpBuilder YesPlaylist()=> AddFlag("--yes-playlist");

    /// <summary>
    /// Download only videos suitable for the given age
    /// </summary>
    /// <param name="years"></param>
    public YtdlpBuilder WithAgeLimit(int years) => AddOption("--age-limit", years.ToString());

    /// <summary>
    /// Abort after downloading number files
    /// </summary>
    /// <param name="number"></param>
    public YtdlpBuilder WithMaxDownloads(int number) => AddOption("--max-downloads", number.ToString());

    #endregion

    #region Download Options

    /// <summary>
    /// Number of fragments of a dash/hlsnative video that should be downloaded concurrently (default is 1)
    /// </summary>
    /// <param name="count"></param>
    public YtdlpBuilder WithConcurrentFragments(int count) => Copy(b => b.ConcurrentFragments = count > 0 ? count : null);

    /// <summary>
    /// Maximum download rate in bytes per second
    /// </summary>
    /// <param name="rate">e.g. 50K or 4.2M</param>
    public YtdlpBuilder WithLimitRate(string rate) => AddOption("--limit-rate", rate);

    /// <summary>
    /// Minimum download rate in bytes per second below which throttling is assumed and the video data is re-extracted
    /// </summary>
    /// <param name="rate">e.g. 100K</param>
    public YtdlpBuilder WithThrottledRate(string rate) => AddOption("--throttled-rate", rate);

    /// <summary>
    /// Number of retries (default is 10), or -1 for "infinite"
    /// </summary>
    /// <param name="maxRetries"></param>
    public YtdlpBuilder WithRetries(int maxRetries) => AddOption("--retries", maxRetries < 0 ? "infinite" : maxRetries.ToString());

    /// <summary>
    /// Number of times to retry on file access error (default is 3), or -1 for "infinite"
    /// </summary>
    /// <param name="maxRetries"></param>
    public YtdlpBuilder WithFileAccessRetries(int maxRetries) => AddOption("--file-access-retries", maxRetries < 0 ? "infinite" : maxRetries.ToString());

    /// <summary>
    /// Number of retries for a fragment (default is 10), or -1 for "infinite" (DASH, hlsnative and ISM)
    /// </summary>
    /// <param name="maxRetries"></param>
    public YtdlpBuilder WithFragmentRetries(int maxRetries) => AddOption("--fragment-retries", maxRetries < 0 ? "infinite" : maxRetries.ToString());

    /// <summary>
    /// Keep downloaded fragments on disk after downloading is finished
    /// </summary>
    public YtdlpBuilder KeepFragments() => AddFlag("--keep-fragments");

    /// <summary>
    /// Size of download buffer, (default is 1024) 
    /// </summary>
    /// <param name="size">e.g. 1024 or 16K</param>
    public YtdlpBuilder WithBufferSize(string size) => AddOption("--buffer-size", size);

    /// <summary>
    /// Download playlist videos in random order
    /// </summary>
    public YtdlpBuilder PlaylistRandom() => AddFlag("--playlist-random");

    /// <summary>
    /// Process entries in the playlist as they are received. This disables n_entries, PlaylistRandom() and --playlist-reverse
    /// </summary>
    public YtdlpBuilder LazyPlaylist() => AddFlag("--lazy-playlist");

    /// <summary>
    /// Download only chapters that match the regular expression. A "*" prefix denotes time-range instead of chapter.
    /// Negative timestamps are calculated from the end. "*from-url" can be used to download between the "start_time" and "end_time" extracted from the URL.
    /// Needs ffmpeg. This option can be used multiple times to download multiple sections
    /// </summary>
    /// <param name="regex">e.g. "*10:15-inf", "intro"</param>
    /// <returns></returns>
    public YtdlpBuilder WithDownloadSections(string regex)
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
    /// <exception cref="ArgumentException"></exception>
    public YtdlpBuilder WithHomeFolder(string? homeFolder)
    {
        if (string.IsNullOrWhiteSpace(homeFolder)) throw new ArgumentException("Home folder path cannot be empty");
        return Copy(b => b.HomeFolder = Path.GetFullPath(homeFolder));
    }

    /// <summary>
    /// Sets the temporary folder for yt-dlp intermediate files (fragments, etc.).
    /// Path is automatically normalized and quoted.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public YtdlpBuilder WithTempFolder(string? tempFolder)
    {
        if (string.IsNullOrWhiteSpace(tempFolder)) throw new ArgumentException("Temp folder path cannot be empty");
        return Copy(b => b.TempFolder = Path.GetFullPath(tempFolder));
    }

    /// <summary>
    /// Sets the output folder
    /// </summary>
    /// <param name="folder"></param>
    /// <exception cref="ArgumentException"></exception>
    public YtdlpBuilder WithOutputFolder([Required] string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Output folder cannot be empty");
        return Copy(b => b.OutputFolder = Path.GetFullPath(folder));
    }

    /// <summary>
    /// Output filename template
    /// </summary>
    /// <param name="template"></param>
    public YtdlpBuilder WithOutputTemplate(string template) => Copy(b => b.OutputTemplate = template);

    /// <summary>
    /// Restrict filenames to only ASCII characters, and avoid "&" and spaces in filenames
    /// </summary>
    public YtdlpBuilder RestrictFileNames() => AddFlag("--restrict-filenames");

    /// <summary>
    /// Force filenames to be Windows-compatible
    /// </summary>
    public YtdlpBuilder WindowsFileNames() => AddFlag("--windows-filenames");

    /// <summary>
    /// Limit the filename length (excluding extension) to the specified number of characters
    /// </summary>
    /// <param name="length"></param>
    public YtdlpBuilder WithTrimFileNames(int length) => AddOption("--trim-filenames", length.ToString());

    /// <summary>
    /// Do not overwrite any files
    /// </summary>
    public YtdlpBuilder NoFileOverwrites() => AddFlag("--no-overwrites");

    /// <summary>
    /// Do not use .part files - write directly into output file
    /// </summary>
    public YtdlpBuilder NoPartFile() => AddFlag("--no-part");

    /// <summary>
    /// Use the Last-modified header to set the file modification time
    /// </summary>
    public YtdlpBuilder ModificationTime() => AddFlag("--mtime");


    /// <summary>
    /// Write video description to a .description file
    /// </summary>
    public YtdlpBuilder WriteVideoDescription() => AddFlag("--write-description");

    /// <summary>
    /// Write video metadata to a .info.json file (this may contain personal information)
    /// </summary>
    public YtdlpBuilder WriteVideoMetadata() => AddFlag("--write-info-json");

    /// <summary>
    /// Do not write playlist metadata when using WriteVideoMetadata(), WriteVideoDescription()
    /// </summary>
    public YtdlpBuilder NoWritePlaylistMetaFiles() => AddFlag("--no-write-playlist-metafiles");

    /// <summary>
    /// Write all fields to the infojson
    /// </summary>
    public YtdlpBuilder NoCleanInfoJson() => AddFlag("--no-clean-info-json");

    /// <summary>
    /// Retrieve video comments to be placed in the infojson. The comments are fetched even without this option if the extraction is known to be quick
    /// </summary>
    public YtdlpBuilder WriteComments() => AddFlag("--write-comments");

    /// <summary>
    /// Do not retrieve video comments unless the extraction is known to be quick
    /// </summary>
    public YtdlpBuilder NoWriteComments() => AddFlag("--no-write-comments");

    /// <summary>
    /// JSON file containing the video information (created with the WriteVideoMetadata() option)
    /// </summary>
    /// <param name="fileName">*.json</param>
    public YtdlpBuilder WithLoadVideoMetadata(string fileName) => AddOption("--load-info-json", fileName);

    /// <summary>
    /// Netscape formatted file to read cookies from and dump cookie jar in
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="ArgumentException"></exception>
    public YtdlpBuilder WithCookiesFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Cookie file path cannot be empty.", nameof(path));
        return Copy(b => b.CookiesFile = path);
    }

    /// <summary>
    /// The name of the browser to load cookies from. Currently supported browsers are: brave, chrome, chromium, edge, firefox, opera, safari, vivaldi, whale.
    /// Optionally, the KEYRING used for decrypting Chromium cookies on Linux, the name/path of the PROFILE to load cookies from, and the CONTAINER name (if Firefox) 
    /// ("none" for no container) can be given with their respective separators. By default, all containers of the most recently accessed profile are used.
    /// keyrings are: basictext, gnomekeyring, kwallet, kwallet5, kwallet6
    /// </summary>
    /// <param name="browser"></param>
    public YtdlpBuilder WithCookiesFromBrowser(string browser) => Copy(b => b.CookiesFromBrowser = browser);

    /// <summary>
    /// Disable filesystem caching
    /// </summary>
    public YtdlpBuilder NoCacheDir() => AddFlag("--no-cache-dir");

    #endregion

    #region Thumbnail Options

    /// <summary>
    /// Write thumbnail image to disk / Write all thumbnail image formats to disk
    /// </summary>
    /// <param name="allSizes"></param>
    /// <returns></returns>
    public YtdlpBuilder WithWriteThumbnails(bool allSizes = false)
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
    public YtdlpBuilder Quiet() => AddFlag("--quiet");

    /// <summary>
    /// Ignore warnings
    /// </summary>
    public YtdlpBuilder NoWarnings() => AddFlag("--no-warnings");

    /// <summary>
    /// Do not download the video and do not write anything to disk
    /// </summary>
    public YtdlpBuilder Simulate() => AddFlag("--simulate");

    /// <summary>
    /// Download the video even if printing/listing options are used
    /// </summary>
    public YtdlpBuilder NoSimulate() => AddFlag("--no-simulate");

    /// <summary>
    /// Do not download the video but write all related files (Alias: --no-download)
    /// </summary>
    /// <returns></returns>
    public YtdlpBuilder SkipDownload() => AddFlag("--skip-download");

    #endregion

    #region Workgrounds

    /// <summary>
    /// Specify a custom HTTP header and its value. You can use this option multiple times
    /// </summary>
    /// <param name="header">"Referer" "User-Agent"</param>
    /// <param name="value">"URL", "UA"</param>
    /// <exception cref="ArgumentException"></exception>
    public YtdlpBuilder WithAddHeader(string header, string value)
    {
        if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Header and value cannot be empty.");
        return AddOption("--add-headers", $"{header}:{value}");
    }

    #endregion

    #region Video Format Options

    /// <summary>
    /// Video format code
    /// </summary>
    /// <param name="format"></param>
    public YtdlpBuilder WithFormat(string format) => Copy(b => b.Format = format);

    #endregion

    #region Subtitle Options 

    /// <summary>
    /// Write subtitle file
    /// </summary>
    /// <param name="languages">Languages of the subtitles to download (can be regex) or "all" separated by commas, e.g."en.*,ja"
    /// (where "en.*" is a regex pattern that matches "en" followed by 0 or more of any character).
    /// </param>
    /// <param name="autoGenerated">Write automatically generated subtitle file</param>
    public YtdlpBuilder WithDownloadSubtitles(string languages = "all", bool autoGenerated = false)
    {
        var b = AddFlag("--write-subs");

        if (autoGenerated)
            b = b.AddFlag("--write-auto-subs");

        return b.AddOption("--sub-langs", languages);
    }

    #endregion

    #region Authentication Options

    /// <summary>
    /// Login with this account ID and account password.
    /// </summary>
    /// <param name="username">Account ID</param>
    /// <param name="password">Account password</param>
    /// <exception cref="ArgumentException"></exception>
    public YtdlpBuilder WithAuthentication(string username, string password)
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
    public YtdlpBuilder WithTwoFactorCode(string code) => AddOption("--twofactor", code);

    #endregion

    #region Post-Processing Options

    /// <summary>
    /// Convert video files to audio-only files (requires ffmpeg and ffprobe).        
    /// </summary>
    /// <param name="format">Formats currently supported: best (default),aac, alac, flac, m4a, mp3, opus, vorbis, wav).</param>
    /// <param name="quality">Audio quality (0–10, lower = better). Default: 5 (medium)</param>
    public YtdlpBuilder WithExtractAudio(string format = "best", int quality = 5)
    {
        return this
            .AddFlag("--extract-audio")
            .AddOption("--audio-format", format)
            .AddOption("--audio-quality", quality.ToString());
    }

    /// <summary>
    /// Remux the video into another container if necessary (requires ffmpeg and ffprobe)
    /// If the target container does not support the video/audio codec, remuxing will fail. You can specify multiple rules; 
    /// e.g. "aac>m4a/mov>mp4/mkv" will remux aac to m4a, mov to mp4 and anything else to mkv
    /// </summary>
    /// <param name="format">(currently supported: avi, flv, gif, mkv, mov, mp4, webm, aac, aiff, alac, flac, m4a, mka, mp3, ogg, opus, vorbis, wav).</param>
    public YtdlpBuilder WithRemuxVideo(string format = "mp4") => AddOption("--remux-video", format);

    /// <summary>
    /// Re-encode the video into another format if necessary. The syntax and supported formats are the same as WithRemuxVideo()
    /// </summary>
    /// <param name="format">(currently supported: avi, flv, gif, mkv, mov, mp4, webm, aac, aiff, alac, flac, m4a, mka, mp3, ogg, opus, vorbis, wav).</param>
    /// <param name="videoCodec"></param>
    /// <param name="audioCodec"></param>
    public YtdlpBuilder WithReEncodeVideo(string format = "mp4", string? videoCodec = null, string? audioCodec = null)
    {
        var builder = AddOption("--recode-video", format);
        if (!string.IsNullOrWhiteSpace(videoCodec))
            builder = builder.AddOption("--video-codec", videoCodec);
        if (!string.IsNullOrWhiteSpace(audioCodec))
            builder = builder.AddOption("--audio-codec", audioCodec);
        return builder;
    }

    /// <summary>
    /// Give these arguments to the postprocessors. Specify the postprocessor/executable name and to give the argument to the specified
    /// </summary>
    /// <param name="postProcessorName">Supported PP are: Merger, ModifyChapters, SplitChapters, ExtractAudio, 
    /// VideoRemuxer, VideoConvertor, Metadata, EmbedSubtitle, EmbedThumbnail, SubtitlesConvertor, ThumbnailsConvertor, 
    /// FixupStretched, FixupM4a, FixupM3u8, FixupTimestamp and FixupDuration.</param>
    /// <param name="args"></param>
    public YtdlpBuilder WithPostProcessorArg(string postProcessorName, string args)
    {
        if (string.IsNullOrWhiteSpace(postProcessorName) || string.IsNullOrWhiteSpace(args))
            return this;

        string combined = $"{postProcessorName}:{args}";
        return AddOption("--postprocessor-args", combined);
    }

    /// <summary>
    /// Keep the intermediate video file on disk after post-processing
    /// </summary>
    public YtdlpBuilder KeepVideo() => AddFlag("-k");

    /// <summary>
    /// Do not overwrite post-processed files
    /// </summary>
    public YtdlpBuilder NoPostOverwrites() => AddFlag("--no-post-overwrites");

    /// <summary>
    /// Embed subtitles in the video (only for mp4, webm and mkv videos)
    /// </summary>
    /// <param name="languages"></param>
    /// <param name="convertTo"></param>
    public YtdlpBuilder WithEmbedSubtitles(string languages = "all", string? convertTo = null)
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
    public YtdlpBuilder EmbedThumbnail() => AddFlag("--embed-thumbnail");

    /// <summary>
    /// Embed metadata to the video file
    /// </summary>
    public YtdlpBuilder EmbedMetadata() => AddFlag("--embed-metadata");

    /// <summary>
    /// Add chapter markers to the video file
    /// </summary>
    public YtdlpBuilder EmbedChapters() => AddFlag("--embed-chapters");

    /// <summary>
    /// Embed the infojson as an attachment to mkv/mka video files
    /// </summary>
    public YtdlpBuilder EmbedInfoJson() => AddFlag("--embed-info-json");

    /// <summary>
    /// Do not embed the infojson as an attachment to the video file
    /// </summary>
    public YtdlpBuilder NoEmbedInfoJson() => AddFlag("--no-embed-info-json");

    /// <summary>
    /// Replace text in a metadata field using the given regex. This option can be used multiple times.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="regex"></param>
    /// <param name="replacement"></param>
    /// <exception cref="ArgumentException"></exception>
    public YtdlpBuilder WithReplaceMetadata(string field, string regex, string replacement)
    {
        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(regex) || replacement == null)
            throw new ArgumentException("Metadata field, regex, and replacement cannot be empty.");
        return AddFlag($"--replace-in-metadata {field} {regex} {replacement}");
    }

    /// <summary>
    /// Concatenate videos in a playlist. All the video files must have the same codecs and number of streams to be concatenable
    /// </summary>
    /// <param name="policy">never, always, multi_video (default; only when the videos form a single show)</param>
    public YtdlpBuilder WithConcatPlaylist(string policy = "always") => AddOption("--concat-playlist", policy);

    /// <summary>
    /// Location of the ffmpeg binary
    /// </summary>
    /// <param name="ffmpegPath">Either the path to the binary or its containing directory</param>
    public YtdlpBuilder WithFFmpegLocation(string? ffmpegPath)
    {
        if (string.IsNullOrWhiteSpace(ffmpegPath)) return this;
        return Copy(b => b.FfmpegLocation = ffmpegPath);
    }

    /// <summary>
    /// Convert the thumbnails to another format. You can specify multiple rules using similar WithRemuxVideo().
    /// </summary>
    /// <param name="format">(currently supported: jpg, png, webp)</param>
    /// <returns></returns>
    public YtdlpBuilder WithConvertthumbnails(string format = "none") => AddOption("--convert-thumbnails", format);

    #endregion

    #region SponsorBlock Options

    /// <summary>
    /// SponsorBlock categories to create chapters for, separated by commas. 
    /// Available categories are sponsor, intro, outro, selfpromo, preview, filler, interaction, music_offtopic, hook, poi_highlight, chapter, all and default (=all).
    /// You can prefix the category with a "-" to exclude it. E.g. SponsorBlockMark("all,-preview)
    /// </summary>
    /// <param name="categories"></param>
    /// <returns></returns>
    public YtdlpBuilder WithSponsorblockMark(string categories = "all") => AddOption("--sponsorblock-mark", categories);

    /// <summary>
    /// SponsorBlock categories to be removed from the video file, separated by commas. 
    /// If a category is present in both mark and remove, remove takes precedence. Working and available categories are the same as for WithSponsorblockMark()
    /// </summary>
    /// <param name="categories"></param>
    /// <returns></returns>
    public YtdlpBuilder WithSponsorblockRemove(string categories = "all") => Copy(b => b.SponsorblockRemoveCategories = categories);

    /// <summary>
    /// Disable both WithSponsorblockMark() and WithSponsorblockRemove() options and do not use any sponsorblock features
    /// </summary>
    /// <returns></returns>
    public YtdlpBuilder NoSponsorblock() => AddFlag("--no-sponsorblock");
    #endregion

    #region Generic Core
    
    private YtdlpBuilder Copy(Action<YtdlpBuilder> modifier)
    {
        var clone = new YtdlpBuilder(this);
        modifier(clone);
        return clone;
    }

    public YtdlpBuilder WithYtDlpPath(string path) => Copy(b => b.YtDlpPath = path);

    public YtdlpBuilder WithLogger(ILogger logger) => Copy(b => b.Logger = logger);

    public YtdlpBuilder AddFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag) || Flags.Contains(flag)) return this;
        return Copy(b => b.Flags = b.Flags.Add(flag.Trim()));
    }

    public YtdlpBuilder AddOption(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key)) return this;
        return Copy(b => b.Options = b.Options.Add((key.Trim(), value)));
    }

    public YtdlpCommand Build() => new YtdlpCommand(this);
    
    public YtdlpBuilder Probe() => Copy(b => b.IsProbe = true);

    public IReadOnlyList<string> BuildArgs(string url)
    {
        var args = new List<string>();

        if (!IsProbe)
        {
            // ─── Paths (home & temp) ────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(HomeFolder))
            {
                args.Add("--paths");
                args.Add($"home:{HomeFolder}");
            }
                       
            if (!string.IsNullOrWhiteSpace(TempFolder))
            {
                args.Add("--paths");
                args.Add($"temp:{TempFolder}");
            }

            // ─── Output ─────────────────────────────────────────────────────────────
            // Keep template RELATIVE — do NOT combine OutputFolder here
            if (!string.IsNullOrWhiteSpace(OutputTemplate))
            {
                args.Add("-o");
                args.Add(OutputTemplate);
            }

            // ─── Format ─────────────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(Format))
            {
                args.Add("-f");
                args.Add(Format);
            }

            // ─── Concurrent fragments ───────────────────────────────────────────────
            if (ConcurrentFragments.HasValue)
            {
                args.Add("--concurrent-fragments");
                args.Add(ConcurrentFragments.Value.ToString());
            }
        }

        // ─── Flags ──────────────────────────────────────────────────────────────
        if (Flags.Length > 0)
            args.AddRange(Flags);

        // ─── Key-value options ──────────────────────────────────────────────────
        if (Options.Length > 0)
        {
            foreach (var kv in Options)
            {
                args.Add(kv.Key);
                if (kv.Value != null)
                    args.Add(kv.Value);
            }
        }

        // ─── Special booleans & paths ───────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(CookiesFile))
        {
            args.Add("--cookies");
            args.Add(CookiesFile);
        }

        if (!string.IsNullOrWhiteSpace(CookiesFromBrowser))
        {
            args.Add("--cookies-from-browser");
            args.Add(CookiesFromBrowser);
        }

        if (!string.IsNullOrWhiteSpace(Proxy))
        {
            args.Add("--proxy");
            args.Add(Proxy);
        }

        if (!string.IsNullOrWhiteSpace(FfmpegLocation))
        {
            args.Add("--ffmpeg-location");
            args.Add(FfmpegLocation);
        }

        if (!string.IsNullOrWhiteSpace(SponsorblockRemoveCategories))
        {
            args.Add("--sponsorblock-remove");
            args.Add(SponsorblockRemoveCategories);
        }

        // ─── URL  ──────────────────────────────────────────────────────────────
        args.Add(url);

        return args.AsReadOnly();
    }

    #endregion

}