namespace ClipMate.Models;

public enum DownloadStatus
{    
    Analyzing,
    Pending,
    Downloading,
    Merging,
    Completed,
    Failed,
    Warning,
    Cancelled
}