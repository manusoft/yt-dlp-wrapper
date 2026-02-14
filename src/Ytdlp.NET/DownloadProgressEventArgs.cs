namespace Ytdlp.NET;

public class DownloadProgressEventArgs : EventArgs
{
    public double Percent { get; set; } = default!;     // Percentage of download (0 to 100)
    public string Size { get; set; } = default!;        // Download size in bytes or MiB
    public string Speed { get; set; } = default!;       // Speed in MiB/s or similar units
    public string ETA { get; set; } = default!;         // Estimated Time of Arrival for completion
    public string Fragments { get; set; } = default!;   // Number of fragments downloaded (if applicable)
    public string Message { get; set; } = default!;     // Any additional message
}