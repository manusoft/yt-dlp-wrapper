namespace ManuHub.Ytdlp.Core;

public static class RegexPatterns
{
    public static string ExtractingUrl = @"Extracting URL: (?<url>.*)";
    public static string DownloadingWebpage = @"\[youtube\] (?<id>[\w-]{11}): Downloading (?<type>.*) webpage";
    public static string DownloadingJson = @"\[youtube\] (?<id>[\w-]{11}): Downloading (?<type>.*) player API JSON";
    public static string DownloadingTvClientConfig = @"\[youtube\] (?<id>[\w-]{11}): Downloading tv client config";
    public static string DownloadingM3u8 = @"\[youtube\] (?<id>[\w-]{11}): Downloading m3u8 information";
    public static string DownloadingManifest = @"Downloading manifest";
    public static string TotalFragments = @"Total fragments: (?<fragments>\d+)";
    public static string TestingFormat = @"Testing format (?<format>.*)";
    public static string DownloadingFormat = @"\[download\] Downloading format (?<format>\d+) for video ID: (?<id>[\w-]{11})";
    public static string DownloadingThumbnail = @"\[download\] Downloading video thumbnail (?<number>\d+)";
    public static string WritingThumbnail = @"\[download\] Writing video thumbnail (?<number>\d+) to: (?<path>.*)";
    public static string DownloadDestination = @"\[download\] Destination: (?<path>.*)";
    public static string ResumeDownload = @"\[download\] Resuming from byte (?<byte>\d+)";
    public static string DownloadAlreadyDownloaded = @"\[download\] (?<path>.*) has already been downloaded";
    public static string DownloadProgressComplete = @"\[download\] (?<percent>100.0%) of (?<size>[\d.]+[KMG]?iB)";
    public static string DownloadProgressWithFrag = @"\[download\] (?<percent>[\d.]+%) of (?<size>[\~]?[\d.]+[KMG]?iB) at (?<speed>[\d.]+[KMG]?iB/s) ETA (?<eta>[\d:]+) \((?<frag>\d+/\d+)\)";
    public static string DownloadProgress = @"\[download\] (?<percent>[\d.]+%) of (?<size>[\~]?[\d.]+[KMG]?iB) at (?<speed>[\d.]+[KMG]?iB/s) ETA (?<eta>[\d:]+)";
    public static string UnknownError = @"ERROR: (?<error>.*)";
    public static string MergingFormats = @"\[Merger\] Merging formats into ""(?<path>.*)""";
    public static string ExtractingMetadata = @"\[youtube\] (?<id>[\w-]{11}): Extracting metadata";
    public static string SpecificError = @"ERROR: (?<error>.*)";
    public static string DownloadingSubtitles = @"\[info\] Downloading subtitles for (?<language>.*)";
    public static string DeleteingOriginalFile = @"\[Merger\] Deleting original file (?<file>.*)";
    public static string MergerSuccess = @"\[Merger\] Successfully merged";
    public static string SponsorBlockAction = @"\[SponsorBlock\] (?<action>.*)(?<details>.*)?";
    public static string ConcurrentFragmentRange = @"\[download\] Fragment (?<frag>.*)";
    public static string ConvertSubs = @"\[ffmpeg\] Converting subtitle (?<file>.*) to (?<format>.*)";
    public static string FFmpegAction = @"\[ffmpeg\] (?<action>.*)";
    public static string PostProcessGeneric = @"\[postprocess\] (?<action>.*)";
    public static string PlaylistItem = @"\[download\] Downloading playlist item (?<item>\d+)/(?<total>\d+): (?<playlist>.*)";
}