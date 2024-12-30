namespace YtDlpWrapper;

public class DownloadProgressEventArgs : EventArgs
{
    public double Percent { get; set; }        // Percentage of download (0 to 100)
    public string Size { get; set; }           // Download size in bytes or MiB
    public string Speed { get; set; }          // Speed in MiB/s or similar units
    public string ETA { get; set; }          // Estimated Time of Arrival for completion
    public string Fragments { get; set; }         // Number of fragments downloaded (if applicable)
    public string Message { get; set; }        // Any additional message
}
