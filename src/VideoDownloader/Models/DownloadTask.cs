namespace VideoDownloader.Models;

public sealed class DownloadTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Url { get; set; } = "";
    public string Title { get; set; } = "";

    public string Format { get; set; } = "bv*+ba/b";
    public string OutputFolder { get; set; } = "";
    public string OutputTemplate { get; set; } =
        "%(title)s.%(ext)s";

    public double Progress { get; set; }
    public string Speed { get; set; } = "";
    public string ETA { get; set; } = "";

    public DownloadState State { get; set; }
}

public enum DownloadState
{
    Queued,
    Running,
    Completed,
    Error
}