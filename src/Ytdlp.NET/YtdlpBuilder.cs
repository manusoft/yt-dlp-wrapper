using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace ManuHub.Ytdlp;

public sealed class YtdlpBuilder
{
    // Frozen state
    internal string YtDlpPath;
    internal ILogger Logger;
    internal string OutputFolder;
    internal string OutputTemplate;
    internal string Format;
    internal int? ConcurrentFragments;
    internal ImmutableArray<string> Flags;
    internal ImmutableDictionary<string, string?> Options;
    internal bool Simulate;
    internal bool NoOverwrites;
    internal bool KeepFragments;
    internal string? CookiesFile;
    internal string? CookiesFromBrowser;
    internal string? Referer;
    internal string? UserAgent;
    internal string? Proxy;
    internal string? FfmpegLocation;
    internal string? SponsorblockRemoveCategories;

    public YtdlpBuilder(string? ytDlpPath = null, ILogger? logger = null)
    {
        YtDlpPath = ytDlpPath ?? "yt-dlp"; // or resolve from path
        Logger = logger ?? new DefaultLogger();
        OutputFolder = Directory.GetCurrentDirectory();
        OutputTemplate = "%(title)s [%(id)s].%(ext)s";
        Format = "b";
        ConcurrentFragments = null;
        Flags = ImmutableArray<string>.Empty;
        Options = ImmutableDictionary<string, string?>.Empty;
        Simulate = false;
        NoOverwrites = false;
        KeepFragments = false;
        CookiesFile = null;
        CookiesFromBrowser = null;
        Referer = null;
        UserAgent = null;
        Proxy = null;
        FfmpegLocation = null;
        SponsorblockRemoveCategories = null;
    }

    private YtdlpBuilder(YtdlpBuilder other)
    {
        YtDlpPath = other.YtDlpPath;
        Logger = other.Logger;
        OutputFolder = other.OutputFolder;
        OutputTemplate = other.OutputTemplate;
        Format = other.Format;
        ConcurrentFragments = other.ConcurrentFragments;
        Flags = other.Flags;
        Options = other.Options;
        Simulate = other.Simulate;
        NoOverwrites = other.NoOverwrites;
        KeepFragments = other.KeepFragments;
        CookiesFile = other.CookiesFile;
        CookiesFromBrowser = other.CookiesFromBrowser;
        Referer = other.Referer;
        UserAgent = other.UserAgent;
        Proxy = other.Proxy;
        FfmpegLocation = other.FfmpegLocation;
        SponsorblockRemoveCategories = other.SponsorblockRemoveCategories;
    }

    // Core
    public YtdlpBuilder WithYtDlpPath(string path) => Copy(b => b.YtDlpPath = path);
    public YtdlpBuilder WithLogger(ILogger logger) => Copy(b => b.Logger = logger);

    public YtdlpBuilder WithOutputFolder([Required] string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Output folder cannot be empty");
        return Copy(b => b.OutputFolder = Path.GetFullPath(folder));
    }

    public YtdlpBuilder WithTempFolder([Required] string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Temporary folder path cannot be empty");
        return this.AddOption("--paths temp:", folder);
    }

    public YtdlpBuilder WithHomeFolder([Required] string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Home folder path cannot be empty");
        return this.AddOption("--paths home:", folder);
    }

    public YtdlpBuilder WithOutputTemplate(string template) => Copy(b => b.OutputTemplate = template);
    public YtdlpBuilder WithFormat(string format) => Copy(b => b.Format = format);
    public YtdlpBuilder WithConcurrentFragments(int count) => Copy(b => b.ConcurrentFragments = count > 0 ? count : null);
    public YtdlpBuilder WithSimulate(bool simulate = true) => Copy(b => b.Simulate = simulate);
    public YtdlpBuilder WithNoOverwrites(bool noOverwrites = true) => Copy(b => b.NoOverwrites = noOverwrites);
    public YtdlpBuilder WithKeepFragments(bool keep = true) => Copy(b => b.KeepFragments = keep);

    // Network/Auth
    public YtdlpBuilder WithAuthentication(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Username and password cannot be empty.");
        return this
            .AddOption("--username", username)
            .AddOption("--password", password);
    }

    public YtdlpBuilder WithCookiesFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Cookie file path cannot be empty.", nameof(path));
        return Copy(b => b.CookiesFile = path);
    }

    public YtdlpBuilder WithCookiesFromBrowser(string browser) => Copy(b => b.CookiesFromBrowser = browser);
    public YtdlpBuilder WithReferer(string referer) => Copy(b => b.Referer = referer);

    public YtdlpBuilder WithCustomHeader(string header, string value)
    {
        if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Header and value cannot be empty.");
        return AddOption("--add-header", $"{header}:{value}");
    }

    public YtdlpBuilder WithUserAgent(string ua) => Copy(b => b.UserAgent = ua);
    public YtdlpBuilder WithProxy(string proxy) => Copy(b => b.Proxy = proxy);
    public YtdlpBuilder WithFfmpegLocation(string path) => Copy(b => b.FfmpegLocation = path);
    public YtdlpBuilder WithDisableAds() => AddFlag("--no-ads");

    public YtdlpBuilder WithPlaylistItems(string items)
    {
        if (string.IsNullOrWhiteSpace(items))
            throw new ArgumentException("Playlist items cannot be empty.", nameof(items));
        return AddOption("--playlist-items", items);
    }

    // Audio extraction
    /// <summary>
    /// Configures yt-dlp to extract audio only and convert to the specified format.
    /// Adds --extract-audio and --audio-format flags.
    /// </summary>
    /// <param name="format">Audio format: "mp3", "m4a", "opus", "wav", etc. Default: "mp3"</param>
    /// <param name="quality">Audio quality (0–10, lower = better). Default: 5 (medium)</param>
    public YtdlpBuilder WithExtractAudio(string format = "mp3", int quality = 5)
    {
        return this
            .AddFlag("--extract-audio")
            .AddOption("--audio-format", format)
            .AddOption("--audio-quality", quality.ToString());
    }

    // Subtitles
    public YtdlpBuilder WithDownloadSubtitles(string languages = "all", bool autoGenerated = false)
    {
        var b = AddFlag("--write-subs");

        if (autoGenerated)
            b = b.AddFlag("--write-auto-subs");

        return b.AddOption("--sub-langs", languages);
    }

    public YtdlpBuilder WithEmbedSubtitles(string languages = "all", string? convertTo = null)
    {
        var builder = AddOption("--write-sub --sub-langs", languages);
        if (!string.IsNullOrWhiteSpace(convertTo))
            builder = builder.AddOption("--convert-subs", convertTo);
        if (convertTo?.Equals("embed", StringComparison.OrdinalIgnoreCase) == true)
            builder = builder.AddFlag("--embed-subs");
        return builder;
    }

    // Thumbnails
    public YtdlpBuilder WithDownloadThumbnails(bool allSizes = false)
    {
        if (allSizes)
            return AddFlag("--write-all-thumbnails");

        return AddFlag("--write-thumbnail");
    }

    public YtdlpBuilder EmbedThumbnail() => AddFlag("--embed-thumbnail");

    // Retries & Connection Stability
    public YtdlpBuilder WithRetries(int maxRetries)
    {
        // yt-dlp allows -1 for infinite
        return AddOption("--retries", maxRetries < 0 ? "infinite" : maxRetries.ToString());
    }

    public YtdlpBuilder WithFragmentRetries(int maxRetries) => AddOption("--fragment-retries", maxRetries < 0 ? "infinite" : maxRetries.ToString());
    
    // Rate Limiting
    public YtdlpBuilder WithRateLimit(string rateLimit)
    {
        // Validate basic format (optional improvement)
        if (string.IsNullOrWhiteSpace(rateLimit))
            return this;

        return AddOption("--limit-rate", rateLimit);
    }

    // Timeout & Network
    public YtdlpBuilder WithTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero) return this;
        double seconds = timeout.TotalSeconds;
        return AddOption("--socket-timeout", seconds.ToString("F0"));
    }

    public YtdlpBuilder WithDownloadTimeout(string timeout)
    {
        if (string.IsNullOrWhiteSpace(timeout))
            throw new ArgumentException("Timeout cannot be empty.", nameof(timeout));
        return AddOption("--download-timeout", timeout);
    }

    public YtdlpBuilder ForceIpv4() => AddFlag("--force-ipv4");
    public YtdlpBuilder ForceIpv6() => AddFlag("--force-ipv6");

    // Geo & Bypass
    public YtdlpBuilder WithGeoBypassCountry(string countryCode)
    {
        if (countryCode.Length != 2) throw new ArgumentException("Country code must be 2 letters.");
        return AddOption("--geo-bypass-country", countryCode.ToUpper());
    }

    // File Metadata & Cache
    public YtdlpBuilder NoMtime() => AddFlag("--no-mtime");
    public YtdlpBuilder NoCacheDir() => AddFlag("--no-cache-dir");
    public YtdlpBuilder KeepTempFiles() => AddFlag("-k");


    // Post-processing
    public YtdlpBuilder EmbedMetadata() => AddFlag("--embed-metadata");
    public YtdlpBuilder EmbedChapters() => AddFlag("--embed-chapters");      
    public YtdlpBuilder DownloadAudioAndVideoSeparately() => AddFlag("--write-video --write-audio");
    public YtdlpBuilder ConcatenateVideos() => AddFlag("--concat-playlist always");

    public YtdlpBuilder WithReplaceMetadata(string field, string regex, string replacement)
    {
        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(regex) || replacement == null)
            throw new ArgumentException("Metadata field, regex, and replacement cannot be empty.");
        return AddFlag($"--replace-in-metadata {field} {regex} {replacement}");
    }
   
    public YtdlpBuilder SkipDownloaded() => AddFlag("--no-overwrites --download-archive downloaded.txt");

    // Force generic extractor (fallback when specific extractor fails)
    public YtdlpBuilder ForceGenericExtractor()
    {
        return AddFlag("--force-generic-extractor");
    }

    // Custom postprocessor arguments
    public YtdlpBuilder WithPostprocessorArg(string postprocessorName, string args)
    {
        if (string.IsNullOrWhiteSpace(postprocessorName) || string.IsNullOrWhiteSpace(args))
            return this;

        string combined = $"{postprocessorName}:{args}";
        return AddOption("--postprocessor-args", combined);
    }

    // Livestream handling
    public YtdlpBuilder DownloadLiveStreamRealTime() => AddFlag("--live-from-start --recode-video mp4");

    public YtdlpBuilder WithDownloadLivestream(bool fromStart = true, bool waitForVideo = false)
    {
        var b = this;

        if (fromStart)
            b = b.AddFlag("--live-from-start");

        if (waitForVideo)
            b = b.AddFlag("--wait-for-video");

        return b;
    }

    public YtdlpBuilder WithDownloadSections(string sections)
    {
        if (string.IsNullOrWhiteSpace(sections)) return this;
        return AddOption("--download-sections", sections);
    }

    // Playlist merging / concatenation
    public YtdlpBuilder MergePlaylistIntoSingleFile()
    {
        return this
            .AddFlag("--no-split-by-chapter")  // avoid splitting by chapters
            .AddFlag("--concat-playlist");     // concatenate into one file
    }

    // SponsorBlock mark (instead of remove)
    public YtdlpBuilder WithSponsorblockRemove(string categories = "all") => Copy(b => b.SponsorblockRemoveCategories = categories);

    public YtdlpBuilder SponsorBlockMark(string categories = "all")
    {
        return AddOption("--sponsorblock-mark", categories);
    }

    // Generic
    public YtdlpBuilder AddFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag) || Flags.Contains(flag)) return this;
        return Copy(b => b.Flags = b.Flags.Add(flag.Trim()));
    }

    public YtdlpBuilder AddOption(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key)) return this;
        return Copy(b => b.Options = b.Options.SetItem(key.Trim(), value));
    }

    private YtdlpBuilder Copy(Action<YtdlpBuilder> modifier)
    {
        var clone = new YtdlpBuilder(this);
        modifier(clone);
        return clone;
    }

    public YtdlpCommand Build()
    {
        return new YtdlpCommand(this);
    }
}