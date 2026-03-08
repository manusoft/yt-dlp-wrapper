namespace ManuHub.Ytdlp.Core;

public static class RegexPatterns
{
    public static string GenericSourceLine = @"\[(?<source>[^\]]+)\]\s*(?<content>.+)";

    public static string GenericDownloadingWebpage = @"\[(?<source>[^\]]+)\]\s*(?<id>[\w-]{11}|[^\s:]+):\s*Downloading\s*(?<type>.*)\s*webpage";
    public static string GenericDownloadingPlayer = @"\[(?<source>[^\]]+)\]\s*(?<id>[\w-]{11}|[^\s:]+):\s*Downloading\s*(?<type>.*)\s*player\s*(?<player>[\w-]+)?";
    public static string GenericJsChallenge = @"\[(?<source>[^\]]+)\]\s*\[jsc:(?<engine>.*?)\]\s*Solving JS challenges using (?<engine>.*)";
    public static string GenericDownloadingManifest = @"\[(?<source>[^\]]+)\]\s*Downloading m3u8 manifest";
    public static string GenericDownloadingSegments = @"\[(?<source>[^\]]+)\]\s*Downloading (?<count>\d+) segments";
    public static string GenericDownloadingFragments = @"\[(?<source>[^\]]+)\]\s*Downloading (?<count>\d+) fragments?";

    public static string GenericPostProcessing = @"\[(?<source>Merger|FixupM3u8|MoveFiles|VideoRemuxer|ffmpeg|ConvertSubs|SponsorBlock|PostProcess)\]\s*(?<action>.+)";

    // ORIGINAL
    public static string ExtractingUrl = @"\[(?<source>[^\]]+)\]\s*Extracting URL:\s*(?<url>https?://\S+)";
    public static string DownloadingWebpage = @"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*Downloading\s*(?<type>pc|mweb|ios|web)?\s*webpage";
    public static string DownloadingJson = @"\[(?<source>[^\]]+)\]\s+(?<id>[^\s:]+):\s*Downloading\s*(?<type>ios|mweb|tv|android)?\s*player API JSON";
    public static string DownloadingTvClientConfig = @"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*Downloading tv client config";
    public static string DownloadingM3u8 = @"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*Downloading m3u8 information";
    public static string DownloadingManifest = @"\[hlsnative\]\s*Downloading m3u8 manifest";
    public static string TotalFragments = @"\[hlsnative\]\s*Total fragments:\s*(?<fragments>\d+)";
    public static string TestingFormat = @"\[info\]\s*Testing format\s*(?<format>[^\s]+)";
    public static string DownloadingFormat = @"\[info\]\s*(?<id>[^\s:]+):\s*(?:Downloading|Testing)\s*\d+\s*format\(s\):\s*(?<format>[^\s]+)";
    public static string DownloadingThumbnail = @"\[info\]\s*Downloading video thumbnail\s*(?<number>\d+)\s*\.\.\.";
    public static string WritingThumbnail = @"\[info\]\s*Writing video thumbnail\s*(?<number>\d+)\s*to:\s*(?<path>.+)";
    public static string DownloadDestination = @"\[download\]\s*Destination:\s*(?<path>.+)";
    public static string ResumeDownload = @"\[download\]\s*Resuming download at byte\s*(?<byte>\d+)";
    public static string DownloadAlreadyDownloaded = @"\[download\]\s*(?<path>[^\n]+?)\s*has already been downloaded";
    public static string DownloadProgress = @"\[download\]\s*(?<percent>\d+\.\d+)%\s*of\s*(?<size>[^\s]+)\s*at\s*(?<speed>[^\s]+)\s*ETA\s*(?<eta>[^\s]+)";
    public static string DownloadProgressWithFrag = @"\[download\]\s*(?<percent>\d+\.\d+)%\s*of\s*(~?\s*(?<size>[^\s]+))\s*at\s*(?<speed>[^\s]+)\s*ETA\s*(?<eta>[^\s]+)\s*\(frag\s*(?<frag>\d+/\d+)\)";
    public static string DownloadProgressComplete = @"\[download\]\s*100%\s*of\s*(?<size>[\d,.]+[KMGTPEZi]?B)\s*in\s*(?<time>[\d:]+)\s*at\s*(?<speed>[\d,.]+[KMGTPEZi]?B/s)\s*$";
    public static string UnknownError = @"\[download\]\s*Unknown error";
    public static string DeleteingOriginalFile = @"Deleting original file\s+(?<path>.+?)\s+\(pass -k to keep\)";
    public static string MergingFormats = @"\[Merger\]\s*Merging formats into\s*""(?<path>.+)""";
    public static string ExtractingMetadata = @"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*Extracting metadata";
    public static string SpecificError = @"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*ERROR:\s*(?<error>.+)";
    public static string DownloadingSubtitles = @"\[info\]\s*Downloading subtitles:\s*(?<language>[^\s]+)";

    public static string MergerSuccess = @"(?:has been successfully merged|merged formats successfully)";
    public static string PostProcessGeneric = @"\[(?<processor>PostProcess|ffmpeg|Merger|ConvertSubs|SponsorBlock)\]\s*(?<action>.+)";

    public static string SponsorBlockAction = @"\[sponsorblock\]\s*(?<action>.+?)(?::\s*(?<details>.+))?$";

    public static string ConcurrentFragmentRange = @"\[download\]\s*Got server-side ranges for fragment\s*(?<frag>\d+)";
    public static string ConcurrentFragmentDownloaded = @"\[download\]\s*Downloaded fragment\s*(?<frag>\d+)\s*(?:of\s*(?<total>\d+))?";    
    
    public static string ConvertSubs = @"\[ConvertSubs\]\s*Converting subtitle\s*(?<file>.+?)\s*to\s*(?<format>.+?)(?:\s|$)";
    public static string FFmpegAction = @"\[ffmpeg\]\s*(?<action>.+)";

    public static string PlaylistItem = @"\[download\]\s*Downloading playlist:\s*(?<playlist>.+?)\s*;\s*Downloading\s*(?<item>\d+)\s*of\s*(?<total>\d+)";

    public static string WarningLine = @"\[warning\]\s*(?<message>.+)";
    public static string DebugLine = @"\[debug\]\s*(?<message>.+)";

    // ──────────────────────────────────────────────
    // YouTube / extractor related (very common)
    // ──────────────────────────────────────────────
    public static string YoutubeDownloadingWebpage = @"\[youtube\] (?<id>[\w-]{11}): Downloading webpage";
    public static string YoutubeDownloadingPlayer = @"\[youtube\] (?<id>[\w-]{11}): Downloading player (?<player>[\w-]+)";
    public static string YoutubeJsChallenge = @"\[youtube\] \[jsc:(?<engine>.*?)\] Solving JS challenges using (?<engine>.*)";
    public static string YoutubeInfoFormat = @"\[info\] (?<id>[\w-]{11}): Downloading 1 format\(s\): (?<formats>[\d+]+)";
    public static string InfoDestination = @"\[info\] Writing video description to: (?<path>.*)";
    public static string InfoThumbnail = @"\[info\] Writing video thumbnail to: (?<path>.*)";

    // ──────────────────────────────────────────────
    // Download / fragment progress (more variations)
    // ──────────────────────────────────────────────
    public static string DownloadDestinationDash = @"\[download\] Destination: (?<path>.*) \(DASH\)";
    public static string DownloadFragProgress = @"\[download\] (?<percent>[\d.]+)% .* \(frag (?<frag>\d+)/(?<total>\d+)\)";
    public static string DownloadSpeedOnly = @"\[download\] (?<speed>[\d.]+[KMGT]iB/s)";

    // ──────────────────────────────────────────────
    // HLS / DASH / m3u8 related
    // ──────────────────────────────────────────────
    public static string HlsnativeDownloadingManifest = @"\[hlsnative\] Downloading m3u8 manifest";
    public static string HlsnativeDownloadingSegments = @"\[hlsnative\] Downloading (?<count>\d+) segments";
    public static string DashDownloadingFragments = @"\[dashsegments\] Downloading (?<count>\d+) fragments?";

    // ──────────────────────────────────────────────
    // Post-processing & merging
    // ──────────────────────────────────────────────
    public static string PostprocessorFfmpeg = @"\[ffmpeg\] (?<action>.*)";
    public static string MergerDeletingOriginal = @"\[Merger\] Deleting original file (?<path>.*)";
    public static string VideoRemuxing = @"\[VideoRemuxer\] Remuxing video from (?<from>.*) to (?<to>.*)";
    public static string FFmpegConcat = @"\[ffmpeg\] Concatenating (?<count>\d+) files";

    // ──────────────────────────────────────────────
    // Errors & warnings (more specific)
    // ──────────────────────────────────────────────
    public static string WarningUnavailable = @"\[warning\] The video is unavailable";
    public static string ErrorSignatureExtraction = @"ERROR: Signature extraction failed";
    public static string ErrorAgeRestricted = @"ERROR: Age restricted video";

    // ──────────────────────────────────────────────
    // Playlist / multi-video
    // ──────────────────────────────────────────────
    public static string PlaylistDownload = @"\[download\] Downloading playlist: (?<title>.*) - (?<count>\d+) videos";
    public static string PlaylistItemProgress = @"\[download\] Downloading playlist item (?<current>\d+) of (?<total>\d+)";
}