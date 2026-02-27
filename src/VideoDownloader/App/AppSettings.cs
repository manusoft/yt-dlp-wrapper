namespace VideoDownloader.App;

public static class AppSettings
{
    public static string Tools =>
        Path.Combine(AppContext.BaseDirectory, "tools");

    public static string YtDlp =>
        Path.Combine(Tools, "yt-dlp.exe");

    public static string FFmpeg =>
        Path.Combine(Tools, "ffmpeg.exe");

    public static int MaxParallelDownloads = 3;

    public static string OutputFolder =>
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
}