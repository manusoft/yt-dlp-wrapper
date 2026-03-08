using System.Collections.Immutable;

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

    public YtdlpBuilder WithOutputFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Output folder cannot be empty");
        return Copy(b => b.OutputFolder = Path.GetFullPath(folder));
    }

    public YtdlpBuilder WithOutputTemplate(string template) => Copy(b => b.OutputTemplate = template);
    public YtdlpBuilder WithFormat(string format) => Copy(b => b.Format = format);
    public YtdlpBuilder WithConcurrentFragments(int count) => Copy(b => b.ConcurrentFragments = count > 0 ? count : null);
    public YtdlpBuilder WithSimulate(bool simulate = true) => Copy(b => b.Simulate = simulate);
    public YtdlpBuilder WithNoOverwrites(bool noOverwrites = true) => Copy(b => b.NoOverwrites = noOverwrites);
    public YtdlpBuilder WithKeepFragments(bool keep = true) => Copy(b => b.KeepFragments = keep);

    // Network/Auth
    public YtdlpBuilder WithCookiesFile(string path) => Copy(b => b.CookiesFile = path);
    public YtdlpBuilder WithCookiesFromBrowser(string browser) => Copy(b => b.CookiesFromBrowser = browser);
    public YtdlpBuilder WithReferer(string referer) => Copy(b => b.Referer = referer);
    public YtdlpBuilder WithUserAgent(string ua) => Copy(b => b.UserAgent = ua);
    public YtdlpBuilder WithProxy(string proxy) => Copy(b => b.Proxy = proxy);
    public YtdlpBuilder WithFfmpegLocation(string path) => Copy(b => b.FfmpegLocation = path);

    // Post-processing
    public YtdlpBuilder EmbedMetadata() => AddFlag("--embed-metadata");
    public YtdlpBuilder EmbedThumbnail() => AddFlag("--embed-thumbnail");
    public YtdlpBuilder EmbedChapters() => AddFlag("--embed-chapters");
    public YtdlpBuilder EmbedSubtitles() => AddFlag("--embed-subs");
    public YtdlpBuilder WithSponsorblockRemove(string categories = "all") => Copy(b => b.SponsorblockRemoveCategories = categories);

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