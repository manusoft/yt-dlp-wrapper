namespace VideoDownloader.Core;

public sealed class DownloadSession
{
    public bool HasError { get; set; }
    public bool IsMerging { get; set; }
    public DateTime LastProgressUpdate { get; set; } = DateTime.MinValue;
}