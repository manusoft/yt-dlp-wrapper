namespace YtDlpWrapper;

public class DownloadProgressEventArgs : EventArgs
{
    public string Percent { get; set; }
    public string Size { get; set; }
    public string Speed { get; set; }
    public string ETA { get; set; }
    public string Message { get; set; }
}
