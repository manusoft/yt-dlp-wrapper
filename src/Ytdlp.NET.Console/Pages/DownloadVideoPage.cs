using ManuHub.Ytdlp.NET;
using YtdlpNetConsoleApp.Helpers;

namespace YtdlpNetConsoleApp.Pages;

internal static class DownloadVideoPage
{
    public static async Task ShowAsync(Ytdlp baseYtdlp)
    {
        Console.Clear();
        ConsoleTheme.WriteSection("Video Downloader");

        var url = ConsoleExtensions.ReadInput("Enter video URL");

        if (string.IsNullOrWhiteSpace(url))
            return;

        var ytdlp = baseYtdlp
            .WithFormat("bv+ba/b")
            .WithOutputTemplate("%(title)s.%(ext)s")
            .WithOutputFolder("./downloads")
            .WithConcurrentFragments(8)
            .WithAuthentication("user","pwd")
            .WithEmbedMetadata()
            .WithEmbedThumbnail();

        var dashboard = new ProgressDashboard();

        ytdlp.ProgressDownload += (_, p) =>
        {
            dashboard.Update(p.Percent, p.Speed, p.ETA, p.Size);
        };

        ytdlp.OutputMessage += (_, msg) =>
        {
            Console.WriteLine(msg);
        };

        ytdlp.CommandCompleted += (_, result) =>
        {
            Console.WriteLine();
            ConsoleExtensions.WriteLineColor(result.Success ? "SUCCESS" : "FAILED", result.Success ? ConsoleColor.Green : ConsoleColor.Red
            );
        };

        Console.WriteLine("\nStarting download...\n");

        Console.WriteLine(ytdlp.Preview(url));

        await ytdlp.DownloadAsync(url);

        dashboard.Complete();

        Console.WriteLine("\nPress any key...");
        Console.ReadKey();
    }
}
