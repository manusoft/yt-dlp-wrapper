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
    internal string? TempFolder;
    internal string? HomeFolder;
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
        TempFolder = null;
        HomeFolder = null;
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
        TempFolder = other.TempFolder;
        HomeFolder = other.HomeFolder;
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
        return Copy(b => b.TempFolder = Path.GetFullPath(folder));
    }

    public YtdlpBuilder WithHomeFolder([Required] string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Home folder path cannot be empty");
        return Copy(b => b.HomeFolder = Path.GetFullPath(folder));
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
        return AddOption("--username", username).AddOption("--password", password);
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

    public YtdlpBuilder WithGeoBypassCountry(string countryCode)
    {
        if (countryCode.Length != 2) throw new ArgumentException("Country code must be 2 letters.");
        return AddOption("--geo-bypass-country", countryCode.ToUpper());
    }

    public YtdlpBuilder WithPlaylistItems(string items) 
    {
        if (string.IsNullOrWhiteSpace(items))
            throw new ArgumentException("Playlist items cannot be empty.", nameof(items));
        return AddOption("--playlist-items", items);
    } 

    // Post-processing
    public YtdlpBuilder WithEmbedMetadata() => AddFlag("--embed-metadata");
    public YtdlpBuilder WithEmbedThumbnail() => AddFlag("--embed-thumbnail");
    public YtdlpBuilder WithDownloadThumbnail() => AddFlag("--write-thumbnail");
    public YtdlpBuilder WithEmbedChapters() => AddFlag("--embed-chapters");
    public YtdlpBuilder WithEmbedSubtitles(string languages = "all", string? convertTo = null)
    {
        var builder = AddOption("--write-sub --sub-langs", languages);
        if (!string.IsNullOrWhiteSpace(convertTo))
            builder = builder.AddOption("--convert-subs", convertTo);
        if(convertTo?.Equals("embed", StringComparison.OrdinalIgnoreCase) == true)
            builder = builder.AddFlag("--embed-subs");
        return builder;
    } 
    public YtdlpBuilder WithDownloadSubtitiles(string languages = "all") => AddOption("--write-sub --sub-langs", languages);
    public YtdlpBuilder WithSponsorblockRemove(string categories = "all") => Copy(b => b.SponsorblockRemoveCategories = categories);
    public YtdlpBuilder WithDownloadLivestream(bool fromStart = true) => AddFlag(fromStart ? "--live-from-start" : "--no-live-from-start");
    public YtdlpBuilder WithDownloadLiveStreamRealTime() => AddFlag("--live-from-start --recode-video mp4");
    public YtdlpBuilder WithDownloadSections(string timeRanges) => AddOption("--download-sections", timeRanges);
    public YtdlpBuilder WithDownloadAudioAndVideoSeparately() => AddFlag("--write-video --write-audio");
    public YtdlpBuilder WithPostProcessFiles(string args) => AddOption("--postprocessor-args", args);
    public YtdlpBuilder WithMergePlaylistIntoSingleVideo(string format) => AddOption("--merge-output-format", format);
    public YtdlpBuilder WithConcatenateVideos() => AddFlag("--concat-playlist always");

    public YtdlpBuilder WithReplaceMetadata(string field, string regex, string replacement)
    {
        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(regex) || replacement == null)
            throw new ArgumentException("Metadata field, regex, and replacement cannot be empty.");
        return AddFlag($"--replace-in-metadata {field} {regex} {replacement}");
    }

    public YtdlpBuilder WithKeepTempFiles() => AddFlag("-k");

    public YtdlpBuilder WithDownloadTimeout(string timeout)
    {
        if (string.IsNullOrWhiteSpace(timeout))
            throw new ArgumentException("Timeout cannot be empty.", nameof(timeout));
        return AddOption("--download-timeout", timeout);
    } 

    public YtdlpBuilder WithTimeout(string timeout)
    {
        if (string.IsNullOrWhiteSpace(timeout))
            throw new ArgumentException("Timeout cannot be empty.", nameof(timeout));
        return AddOption("--timeout", timeout);
    }

    public YtdlpBuilder WithRetries(int retries)
    {
        if (retries < 0) throw new ArgumentException("Retries cannot be negative.", nameof(retries));
        return AddOption("--retries", retries.ToString());
    }

    public YtdlpBuilder WithDownloadRate(string rate) => AddOption("--limit-rate", rate);
    public YtdlpBuilder WithSkipDownloaded() => AddFlag("--no-overwrites --download-archive downloaded.txt");

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