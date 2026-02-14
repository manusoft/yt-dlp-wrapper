namespace YtdlpNET;

internal static class RegexPatterns
{
    // ───────────── Core / Existing Patterns (unchanged) ─────────────
    public const string ExtractingUrl = @"\[(?<source>[^\]]+)\]\s*Extracting URL:\s*(?<url>https?://\S+)";
    public const string DownloadingWebpage = @"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*Downloading\s*(?<type>pc|mweb|ios|web)?\s*webpage";
    public const string DownloadingJson = @"\[(?<source>[^\]]+)\]\s+(?<id>[^\s:]+):\s*Downloading\s*(?<type>ios|mweb|tv|android)?\s*player API JSON";
    public const string DownloadingTvClientConfig = @"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*Downloading tv client config";
    public const string DownloadingM3u8 = @"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*Downloading m3u8 information";
    public const string DownloadingManifest = @"\[hlsnative\]\s*Downloading m3u8 manifest";
    public const string TotalFragments = @"\[hlsnative\]\s*Total fragments:\s*(?<fragments>\d+)";
    public const string TestingFormat = @"\[info\]\s*Testing format\s*(?<format>[^\s]+)";
    public const string DownloadingFormat = @"\[info\]\s*(?<id>[^\s:]+):\s*(?:Downloading|Testing)\s*\d+\s*format\(s\):\s*(?<format>[^\s]+)";
    public const string DownloadingThumbnail = @"\[info\]\s*Downloading video thumbnail\s*(?<number>\d+)\s*\.\.\.";
    public const string WritingThumbnail = @"\[info\]\s*Writing video thumbnail\s*(?<number>\d+)\s*to:\s*(?<path>.+)";
    public const string DownloadDestination = @"\[download\]\s*Destination:\s*(?<path>.+)";
    public const string ResumeDownload = @"\[download\]\s*Resuming download at byte\s*(?<byte>\d+)";
    public const string DownloadAlreadyDownloaded = @"\[download\]\s*(?<path>[^\n]+?)\s*has already been downloaded";
    public const string DownloadProgress = @"\[download\]\s*(?<percent>\d+\.\d+)%\s*of\s*(?<size>[^\s]+)\s*at\s*(?<speed>[^\s]+)\s*ETA\s*(?<eta>[^\s]+)";
    public const string DownloadProgressWithFrag = @"\[download\]\s*(?<percent>\d+\.\d+)%\s*of\s*(~?\s*(?<size>[^\s]+))\s*at\s*(?<speed>[^\s]+)\s*ETA\s*(?<eta>[^\s]+)\s*\(frag\s*(?<frag>\d+/\d+)\)";
    public const string DownloadProgressComplete = @"\[download\]\s*(?<percent>100(?:\.0)?)%\s*of\s*(?<size>[^\s]+)\s*at\s*(?<speed>[^\s]+|Unknown)\s*ETA\s*(?<eta>[^\s]+|Unknown)";
    public const string UnknownError = @"\[download\]\s*Unknown error";
    public const string MergingFormats = @"\[Merger\]\s*Merging formats into\s*""(?<path>.+)""";
    public const string DeleteingOriginalFile = @"Deleting original file\s+(?<path>.+?)\s+\(pass -k to keep\)";
    public const string ExtractingMetadata = @"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*Extracting metadata";
    public const string SpecificError = @"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*ERROR:\s*(?<error>.+)";
    public const string DownloadingSubtitles = @"\[info\]\s*Downloading subtitles:\s*(?<language>[^\s]+)";

    // ───────────── New / Enhanced Patterns for v2.0 ─────────────

    // More reliable merger success detection (variation of "successfully merged")
    public const string MergerSuccess = @"(?:has been successfully merged|merged formats successfully)";

    // Post-processing generic step (helps count steps reliably)
    public const string PostProcessGeneric = @"\[(?<processor>PostProcess|ffmpeg|Merger|ConvertSubs|SponsorBlock)\]\s*(?<action>.+)";

    // SponsorBlock lines (very common now)
    public const string SponsorBlockAction = @"\[sponsorblock\]\s*(?<action>.+?)(?::\s*(?<details>.+))?$";

    // Concurrent fragments / DASH/HLS speed-up
    public const string ConcurrentFragmentRange = @"\[download\]\s*Got server-side ranges for fragment\s*(?<frag>\d+)";
    public const string ConcurrentFragmentDownloaded = @"\[download\]\s*Downloaded fragment\s*(?<frag>\d+)\s*(?:of\s*(?<total>\d+))?";

    // Subtitle conversion (when using --convert-subs)
    public const string ConvertSubs = @"\[ConvertSubs\]\s*Converting subtitle\s*(?<file>.+?)\s*to\s*(?<format>.+?)(?:\s|$)";

    // FFmpeg post-processing actions (remux, recode, etc.)
    public const string FFmpegAction = @"\[ffmpeg\]\s*(?<action>.+)";

    // Basic playlist item progress (when downloading playlists)
    public const string PlaylistItem = @"\[download\]\s*Downloading playlist:\s*(?<playlist>.+?)\s*;\s*Downloading\s*(?<item>\d+)\s*of\s*(?<total>\d+)";

    // Warning / debug lines (useful for better unknown classification)
    public const string WarningLine = @"\[warning\]\s*(?<message>.+)";
    public const string DebugLine = @"\[debug\]\s*(?<message>.+)";
}