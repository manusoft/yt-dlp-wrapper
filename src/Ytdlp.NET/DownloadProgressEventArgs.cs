namespace ManuHub.Ytdlp;

public class DownloadProgressEventArgs : EventArgs
{
    public double Percent { get; set; }
    public string Size { get; set; } = string.Empty;
    public string Speed { get; set; } = string.Empty;
    public string ETA { get; set; } = string.Empty;
    public string Fragments { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}