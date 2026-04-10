namespace ManuHub.Ytdlp.NET;

internal static class RegexPatterns
{
    // ───────────── Core ─────────────
    public const string DownloadDestination = @"\[download\]\s*Destination:\s*(?<path>.+)";
    public const string ResumeDownload = @"\[download\]\s*Resuming download at byte\s*(?<byte>\d+)";
    public const string DownloadAlreadyDownloaded = @"\[download\]\s*(?<path>[^\n]+?)\s*has already been downloaded";
    public const string DownloadProgress = @"\[download\]\s+(?:(?<percent>[\d\.]+)%(?:\s+of\s+\~?\s*(?<total>[\d\.\w]+))?\s+at\s+(?:(?<speed>[\d\.\w]+\/s)|[\w\s]+)\s+ETA\s(?<eta>[\d\:]+))?";
                                         //@"\[download\]\s*(?<percent>\d+\.\d+)%\s*of\s*(?<size>[^\s]+)\s*at\s*(?<speed>[^\s]+)\s*ETA\s*(?<eta>[^\s]+)";
    public const string DownloadProgressWithFrag = @"\[download\]\s*(?<percent>\d+\.\d+)%\s*of\s*(~?\s*(?<size>[^\s]+))\s*at\s*(?<speed>[^\s]+)\s*ETA\s*(?<eta>[^\s]+)\s*\(frag\s*(?<frag>\d+/\d+)\)";
    public const string DownloadProgressComplete = @"\[download\]\s*(?<percent>100(?:\.0)?)%\s*of\s*(?<size>[^\s]+)\s*at\s*(?<speed>[^\s]+|Unknown)\s*ETA\s*(?<eta>[^\s]+|Unknown)";
    public const string UnknownError = @"\[download\]\s*Unknown error";
    public const string MergingFormats = @"\[Merger\]\s*Merging formats into\s*""(?<path>.+)""";
    public const string DeleteingOriginalFile = @"Deleting original file\s+(?<path>.+?)\s+\(pass -k to keep\)";
    public const string ExtractingMetadata = @"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*Extracting metadata";
    public const string SpecificError = @"\[(?<source>[^\]]+)\]\s*(?<id>[^\s:]+):\s*ERROR:\s*(?<error>.+)";
    public const string DownloadingSubtitles = @"\[info\]\s*Downloading subtitles:\s*(?<language>[^\s]+)";

    // Basic playlist item progress (when downloading playlists)
    public const string PlaylistItem = @"\[download\]\s*Downloading playlist:\s*(?<playlist>.+?)\s*;\s*Downloading\s*(?<item>\d+)\s*of\s*(?<total>\d+)";

    // Warning / debug lines (useful for better unknown classification)
    public const string WarningLine = @"\[warning\]\s*(?<message>.+)";
    public const string DebugLine = @"\[debug\]\s*(?<message>.+)";

    public const string FixupM3u8 = @"\[FixupM3u8\]\s*(?<action>.+)";
    public const string VideoRemuxer = @"\[VideoRemuxer\]\s*(?<action>.+)";
    public const string Metadata = @"\[Metadata\]\s*(?<action>.+)";
    public const string ThumbnailsConvertor = @"\[ThumbnailsConvertor\]\s*(?<action>.+)";
    public const string EmbedThumbnail = @"\[EmbedThumbnail\]\s*(?<action>.+)";
    public const string MoveFiles = @"\[MoveFiles\]\s*(?<action>.+)";

    // Generic fallback for any unknown post-processor
    public const string PostProcessorGeneric = @"\[(?<processor>FixupM3u8|VideoRemuxer|Metadata|ThumbnailsConvertor|EmbedThumbnail|MoveFiles|Merger|ffmpeg|ConvertSubs|SponsorBlock)\]\s*(?<action>.+)";
}