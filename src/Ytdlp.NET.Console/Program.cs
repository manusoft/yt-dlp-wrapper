using ManuHub.Ytdlp;
using System.Text;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Must be the FIRST line — before any Console.WriteLine
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Console.Clear();
        Console.WriteLine("yt-dlp .NET Wrapper v3.0 Demo Console App");
        Console.WriteLine("=========================================");

        var ytdlpPath = $"tools\\yt-dlp.exe";
        var consoleLogger = new ConsoleLogger();

        // Create CancellationTokenSource for Ctrl+C
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true; // prevent immediate termination
            cts.Cancel();
            Console.WriteLine("Cancellation requested...");
        };

        // Setup YtdlpBuilder
        var builder = Ytdlp.Create(ytdlpPath, consoleLogger)
            .WithFormat("mp4/b")               // bestaudio
            .WithConcurrentFragments(8)    // 8 fragment downloads in parallel
            .WithFFmpegLocation("tools")   // folder where ffmpeg.exe is located
            .WithHomeFolder(@"C:\Downloads")
            .WithTempFolder(@"C:\Downloads\Temp")
            .WithOutputTemplate("%(title)s [%(id)s].%(ext)s");

        // Build command
        var cmd = builder.Build();

        // Event hooks
        cmd.OnProgressMessage += (s, e) => Console.WriteLine($"PROG_MSG: {e}");
        cmd.OnCompleteDownload += (s, e) => Console.WriteLine($"DOWN_FIN: {e}");
        cmd.OnProgressDownload += (s, e) => Console.WriteLine($"DOWN_PROG: {e.Percent}% | {e.Size} | {e.ETA} | {e.Speed} | {e.Fragments}");
        cmd.OnPostProcessingStarted += (s, e) => Console.WriteLine($"POST_PROC_BEG: {e}");
        cmd.OnPostProcessingCompleted += (s, e) => Console.WriteLine($"POST_PROC_END: {e}");
        cmd.OnProcessCompleted += (s, e) => Console.WriteLine($"COMPLETE: {e}");
        cmd.OnErrorMessage += (s, e) => Console.WriteLine($"ERROR: {e}");

        // List of URLs
        var urls = new[]
        {
            "https://www.dailymotion.com/video/x7twwcf",
            "https://www.youtube.com/watch?v=l5U5Ij5Jtf8"
        };

        try
        {
            // Download sequentially
            foreach (var url in urls)
            {
                Console.WriteLine($"=== Downloading: {url} ===");
                await cmd.ExecuteAsync(url, cts.Token);
            }

            // Download in parallel
            //Console.WriteLine("=== Downloading in parallel ===");
            //var tasks = urls.Select(url => cmd.ExecuteAsync(url));
            //await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Download cancelled by user.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }

        Console.WriteLine("=== Test Completed ===");
        Console.ReadLine();

    }

    // Custom logger to output to console
    private class ConsoleLogger : ILogger
    {
        public void Log(LogType type, string message)
        {
            Console.ForegroundColor = type switch
            {
                LogType.Error => ConsoleColor.Red,
                LogType.Warning => ConsoleColor.Yellow,
                LogType.Debug => ConsoleColor.Gray,
                _ => ConsoleColor.White
            };
            //Console.WriteLine($"[{type}] {message}");
            Console.ResetColor();
        }
    }
}

