namespace YtDlpWrapper;

/// <summary>
/// Contains the regex patterns used to parse the progress output.
/// </summary>
public static class RegexPatterns
{
    // Extracting URL: https://www.youtube.com/watch?v=...
    public const string ExtractingUrl = @"\[(?<source>\w+)\] Extracting URL: (?<url>https?://\S+)";

    // Downloading webpage
    public const string DownloadingWebpage = @"\[(?<source>\w+)\] (?<id>\S+): Downloading webpage";

    // Downloading ios/mweb player API JSON
    public const string DownloadingJson = @"\[(?<source>\w+)\] (?<id>\S+): Downloading (ios|mweb) player API JSON";

    // Downloading m3u8 information
    public const string DownloadingM3u8 = @"\[(?<source>\w+)\] (?<id>\S+): Downloading m3u8 information";

    // Downloading manifest
    public const string DownloadingManifest = @"\[hlsnative\]\s*Downloading m3u8 manifest";

    // Total fragments
    public const string TotalFragments = @"\[hlsnative\]\s*Total fragments:\s*(\d+)";

    // Downloading format
    public const string DownloadingFormat = @"\[info\] (?<id>\S+): Downloading (\d+) format\(s\): (?<format>\d+)";

    // Download destination
    public const string DownloadDestination = @"\[download\]\s*Destination:\s*(?<path>.+)";

    // Handle download resume
    public const string ResumeDownload = @"\[download\]\s*Resuming download at byte (?<byte>\d+)";

    // Handle download already completed
    public const string DownloadAlreadyDownloaded = @"\[download\]\s*(?<path>.+?)\s*has already been downloaded";

    // Handle download progress with fragments
    public const string DownloadProgressWithFrag = @"\[download\]\s*(?<percent>\d+(\.\d+)?)%\s*of\s*(~?\s*(?<size>\S+))\s*at\s*(?<speed>\S+)/s*\s*ETA\s*(?<eta>\S+)\s*\(frag\s*(?<frag>\d+/\d+)\)";

    // Handle download progress with variable progress
    //public const string DownloadProgress = @"\[download\]\s*(?<percent>\d+(\.\d+)?)%\s*of\s*(?<size>\S+)\s*at\s*(?<speed>\S+)\s*ETA\s*(?<eta>\S+)";
    //public const string DownloadProgress = @"\[download\]\s*(?<percent>\d+(\.\d+)?)%\s*of\s*(~?\s*(?<size>\S+))\s*at\s*(?<speed>\S+)\s*ETA\s*(?<eta>\S+)";
    public const string DownloadProgress = @"\[download\]\s*(?<percent>\d+(\.\d+)?)%\s*of\s*(~?\s*(?<size>\S+))\s*in\s*(?<time>\S+)\s*at\s*(?<speed>\S+)";

    // Handle complete download with fixed progress at 100%
    public const string DownloadProgressComplete = @"\[download\] (?<percent>100)% of (?<size>\S+)";

    public const string DownloadCompleted = @"\[download\]\s*(?<percent>\d+\.\d+)%\s*of\s*(?<size>\S+)\s*in\s*(?<eta>\S+)\s*at\s*(?<speed>\S+)";
    
    public const string UnknownError = @"(?<=\[download\])\s*Unknown error";

}
