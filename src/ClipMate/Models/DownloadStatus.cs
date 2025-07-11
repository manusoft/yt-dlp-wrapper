namespace ClipMate.Models;

public enum DownloadStatus
{
    Pending,
    Analyzing,
    Downloading,
    Merging,
    Completed,
    Failed,
    Warning,
    Cancelled
}