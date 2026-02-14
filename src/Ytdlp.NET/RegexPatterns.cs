namespace Ytdlp.NET;

internal static class RegexPatterns
{
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
}