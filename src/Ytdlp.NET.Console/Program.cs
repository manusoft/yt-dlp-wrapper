using ManuHub.Ytdlp.NET;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

internal class Program
{
    private static async Task Main(string[] args)
    {
        //Console.WriteLine("=== yt-dlp Output to Regex Converter ===\n");
        //Console.WriteLine("Paste a yt-dlp output line below (or type 'exit' to quit):\n");

        //while (true)
        //{
        //    Console.Write("> ");
        //    string? input = Console.ReadLine();

        //    if (string.IsNullOrWhiteSpace(input) || input.Trim().ToLower() == "exit")
        //        break;

        //    string regex = ConvertToRegex(input);
        //    Console.WriteLine("\nGenerated Regex Pattern:");
        //    Console.WriteLine(regex);
        //    Console.WriteLine("\n" + new string('-', 60) + "\n");
        //}


        // Must be the FIRST line — before any Console.WriteLine
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Console.Clear();
        Console.WriteLine("Ytdlp.NET Wrapper v3.0 Demo Console App");
        Console.WriteLine("----------------------------------------");

        // Initialize the wrapper (assuming yt-dlp is in PATH or specify path)
        await using var baseYtdlp = new Ytdlp(ytdlpPath: $"tools\\yt-dlp.exe", logger: new ConsoleLogger())
            .WithFFmpegLocation("tools");

        // Run all demos/tests sequentially
        //await TestGetVersionAsync(baseYtdlp);
        //await TestUpdateAsync(baseYtdlp);

        //await TestGetFormatsAsync(baseYtdlp);
        //await TestGetMetadataAsync(baseYtdlp);
        //await TestGetLiteMetadataAsync(baseYtdlp);
        //await TestGetTitleAsync(baseYtdlp);

        await TestDownloadVideoAsync(baseYtdlp);
        //await TestDownloadAudioAsync(baseYtdlp);
        //await TestBatchDownloadAsync(baseYtdlp);
        //await TestSponsorBlockAsync(baseYtdlp);
        //await TestConcurrentFragmentsAsync(baseYtdlp);
        //await TestCancellationAsync(baseYtdlp);

        var lists = await baseYtdlp.ExtractorsAsync();

        Console.WriteLine("\nAll tests completed. Press any key to exit...");
        Console.ReadKey();
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
            Console.WriteLine($"[{type}] {message}");
            Console.ResetColor();
        }
    }

    private static async Task TestGetVersionAsync(Ytdlp ytdlp)
    {
        Console.WriteLine("\nTest 1: Getting yt-dlp version...");
        var version = await ytdlp.VersionAsync();
        Console.WriteLine($"Version: {version}");
    }

    private static async Task TestUpdateAsync(Ytdlp ytdlp)
    {
        Console.WriteLine("\nTest 2: Checking yt-dlp update...");
        var version = await ytdlp.UpdateAsync(UpdateChannel.Stable);
        Console.WriteLine($"Status: {version}");
    }

    private static async Task TestGetFormatsAsync(Ytdlp ytdlp)
    {
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine("\nTest 3: Fetching available formats...");
        var url = "https://www.youtube.com/watch?v=ZGnQH0LN_98";
        var formats = await ytdlp.GetFormatsAsync(url);

        stopwatch.Stop(); // stop timer
        Console.WriteLine($"Available formats took {stopwatch.Elapsed.TotalSeconds:F3} seconds");

        Console.WriteLine($"Found {formats.Count} formats:");

        foreach (var f in formats)
        {
            Console.WriteLine(f.ToString());  // Uses Format's ToString override
        }

        // Example: Find best 1080p
        var best1080p = formats
            .Where(f => f.IsVideo && (f.Height ?? 0) == 1080)
            .OrderByDescending(f => f.Fps ?? 0)
            .FirstOrDefault();

        if (best1080p != null)
            Console.WriteLine($"\nBest 1080p: ID {best1080p.Id}, {best1080p.VideoCodec}, ~{best1080p.ApproxFileSizeBytes / 1024 / 1024} MiB");
    }

    private static async Task TestGetMetadataAsync(Ytdlp ytdlp)
    {
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine("\nTest 4: Fetching detailed metedata...");
        var token = new CancellationTokenSource().Token; // In real use, you might want to cancel if it takes too long
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);
        var url1 = "https://www.youtube.com/watch?v=983bBbJx0Mk&list=RD983bBbJx0Mk&start_radio=1&pp=ygUFc29uZ3OgBwE%3D"; //playlist
        var url2 = "https://www.youtube.com/watch?v=ZGnQH0LN_98"; // video
        var metadata = await ytdlp.GetMetadataAsync(url2, linkedCts.Token);
        stopwatch.Stop(); // stop timer

        Console.WriteLine($"Detailed metedata took {stopwatch.Elapsed.TotalSeconds:F3} seconds");

        if (metadata == null)
        {
            Console.WriteLine("No metadata returned.");
            return;
        }

        // Basic info
        Console.WriteLine($"Type        : {metadata.Type}");
        Console.WriteLine($"ID          : {metadata.Id}");
        Console.WriteLine($"Title       : {metadata.Title}");
        Console.WriteLine($"Description : {(metadata.Description?.Length > 120 ? metadata.Description.Substring(0, 120) + "..." : metadata.Description)}");
        Console.WriteLine($"Thumbnail   : {metadata.Thumbnail}");

        if (metadata.Type == "video")
        {
            // Show formats (both full list and requested/selected)
            PrintFormats("All available formats", metadata.Formats);
            //PrintFormats("Selected / requested formats", metadata.RequestedFormats);
        }
    }

    private static async Task TestGetLiteMetadataAsync(Ytdlp ytdlp)
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("\nTest 5: Fetching lite metedata...");

        var url = "https://www.youtube.com/watch?v=ZGnQH0LN_98";

        var fields = new[] { "id", "thumbnail" };
        var data = await ytdlp.GetMetadataLiteAsync(url, fields);

        stopwatch.Stop(); // stop timer
        Console.WriteLine($"Simple metedata took {stopwatch.Elapsed.TotalSeconds:F3} seconds");

        if (data != null)
        {
            Console.WriteLine($"Id: {data["id"]}");
            Console.WriteLine($"Thumbnail: {data["thumbnail"]}");
        }
    }

    // Test 6: Download a video with progress events
    private static async Task TestDownloadVideoAsync(Ytdlp ytdlpBase)
    {
        Console.WriteLine("\nTest 6: Downloading a video...");
        var url = "https://www.youtube.com/watch?v=89-i4aPOMrc"; //"https://www.dailymotion.com/video/xa3ron2"; 

        var ytdlp = ytdlpBase
            .WithFormat("ba/b")
            .WithExtractAudio(AudioFormat.Mp3)
            .WithConcurrentFragments(8)
            .WithOutputFolder("./downloads")
            //.WithHomeFolder("./downloads")
            //.WithTempFolder("./downloads/temp")
            .WithOutputTemplate("%(title)s.%(ext)s");
            //.WithEmbedMetadata()
            //.WithEmbedThumbnail()
            //.WithRemuxVideo(MediaFormat.Mp4);

        // Subscribe to events
        ytdlp.OnCommandCompleted += (sender, args) =>
        {
            if (args.Success)
                Console.WriteLine("Process completed successfully.");
            else if (args.Message.Contains("cancelled", StringComparison.OrdinalIgnoreCase))
                Console.WriteLine("Process was cancelled.");
            else
                Console.WriteLine($"Process failed: {args.Message}");
        };

        ytdlp.OnProgressDownload += (sender, args) =>
            Console.WriteLine($"Progress: {args.Percent:F2}% - {args.Speed} - ETA {args.ETA} - Size {args.Size}");

        ytdlp.OnCompleteDownload += (sender, message) =>
            Console.WriteLine($"Download complete: {message}");

        ytdlp.OnPostProcessingStart += (sender, message) =>
            Console.WriteLine($"Post-processing started: {message}");

        ytdlp.OnPostProcessingComplete += (sender, message) =>
            Console.WriteLine($"Post-processing done: {message}");

        Console.WriteLine(ytdlp.Preview(url));

       await ytdlp.DownloadAsync(url);
    }

    private static async Task TestDownloadAudioAsync(Ytdlp ytdlpBase)
    {
        Console.WriteLine("\nTest 7: Extracting audio...");
        var url = "https://www.youtube.com/watch?v=ZGnQH0LN_98";

        var ytdlp = ytdlpBase
            .WithExtractAudio(AudioFormat.Mp3)
            .WithFormat("ba")
            .WithOutputFolder("./downloads/audio");

        await ytdlp.DownloadAsync(url);
    }

    // Test 8: Batch download (concurrent)
    private static async Task TestBatchDownloadAsync(Ytdlp baseYtdlp)
    {
        Console.WriteLine("\nTest 8: Batch download (3 concurrent)...");
        var urls = new List<string>
            {
                "https://www.youtube.com/watch?v=ZGnQH0LN_98",
                "https://www.youtube.com/watch?v=983bBbJx0Mk",
                "https://www.youtube.com/watch?v=oDSEGkT6J-0"
            };

        var ytdlp = baseYtdlp
             .WithFormat("best[height<=480]")  // Lower quality for speed
             .WithOutputFolder("./downloads/batch");

        await ytdlp.DownloadBatchAsync(urls, maxConcurrency: 3);
    }

    // Test 9: SponsorBlock removal
    private static async Task TestSponsorBlockAsync(Ytdlp ytdlpBase)
    {
        Console.WriteLine("\nTest 9: Download with SponsorBlock removal...");
        var url = "https://www.youtube.com/watch?v=oDSEGkT6J-0";

        var ytdlp = ytdlpBase
            .WithFormat("best")
            .WithSponsorblockRemove("all")  // Removes sponsor, intro, etc.
            .WithOutputFolder("./downloads/sponsorblock");

        await ytdlp.DownloadAsync(url);
    }

    // Test 10: Concurrent fragments (faster download)
    private static async Task TestConcurrentFragmentsAsync(Ytdlp ytdlpBase)
    {
        Console.WriteLine("\nTest 10: Download with concurrent fragments...");
        var url = "https://www.youtube.com/watch?v=oDSEGkT6J-0";

        var ytdlp = ytdlpBase
            .WithConcurrentFragments(8)  // 8 parallel fragments
            .WithFormat("b")
            .WithOutputTemplate("%(title)s.%(ext)s")
            .WithOutputFolder("./downloads/concurrent");

        ytdlp.OnProgressDownload += (sender, args) =>
           Console.WriteLine($"Progress: {args.Percent:F2}% - {args.Speed} - ETA {args.ETA} - FRA {args.Fragments}");

        await ytdlp.DownloadAsync(url);
    }

    // Test 11: Cancellation support
    private static async Task TestCancellationAsync(Ytdlp ytdlp)
    {
        Console.WriteLine("\nTest 11: Testing cancellation (will cancel after 10 seconds)...");
        var url = "https://www.youtube.com/watch?v=zGlwuHqGVIA";  // A longer video

        var cts = new CancellationTokenSource();

        var downloadTask = ytdlp
            .WithFormat("b")
            .WithOutputTemplate("%(title)s.%(ext)s")
            .WithOutputFolder("./downloads/cancel")
            .DownloadAsync(url, cts.Token);

        ytdlp.OnCommandCompleted += (sender, args) =>
        {
            if (args.Success)
                Console.WriteLine("Download completed successfully.");
            else if (args.Message.Contains("cancelled", StringComparison.OrdinalIgnoreCase))
                Console.WriteLine("Download was cancelled.");
            else
                Console.WriteLine($"Download failed: {args.Message}");
        };

        // Simulate cancel after 20 seconds
        await Task.Delay(20000);
        cts.Cancel();

        try
        {


            await downloadTask;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Download cancelled successfully.");
        }
    }

    // Test 12: Get Title Test
    private static async Task TestGetTitleAsync(Ytdlp ytdlp)
    {
        Console.WriteLine("\nTest 12: Get Title Test");
        var url = "https://www.youtube.com/watch?v=zGlwuHqGVIA";

        try
        {
            var downloadTask = ytdlp
                .WithSimulate()
                .WithOutputTemplate("%(title)s.%(ext)s")
                .WithOutputFolder("./downloads/cancel")
                .AddFlag("--get-title")
                .DownloadAsync(url);

            await downloadTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    // Helper to format seconds into mm:ss or hh:mm:ss
    private static void PrintFormats(string title, List<FormatMetadata>? formats)
    {
        if (formats == null || formats.Count == 0)
        {
            Console.WriteLine($"\n{title}: (none)");
            return;
        }

        Console.WriteLine($"\n{title} ({formats.Count}):");

        Console.WriteLine("  ID       NOTE              EXT   RESOLUTION   FPS   VCODEC     ACODEC    PROTOCOL   SIZE/APPROX");
        Console.WriteLine("  ──────────────────────────────────────────────────────────────────────────────────────────────");

        foreach (var f in formats.Take(15)) // limit output – there can be 50–100+ formats
        {
            string size = f.Filesize.HasValue ? $"{f.Filesize / 1024 / 1024} MiB" :
                          f.FilesizeApprox.HasValue ? $"~{f.FilesizeApprox / 1024 / 1024} MiB" : "-";

            Console.WriteLine(
                $"  {f.FormatId,-8} " +
                $"{(f.FormatNote ?? "-"),-17} " +
                $"{f.Ext,-5} " +
                $"{(f.Resolution ?? "-"),-12} " +
                $"{f.Fps?.ToString("0.#") ?? "-",5} " +
                $"{(f.Vcodec ?? "-"),-10} " +
                $"{(f.Acodec ?? "-"),-9} " +
                $"{(f.Protocol ?? "-"),-10} " +
                $"{size,-12}"
            );
        }

        if (formats.Count > 15)
            Console.WriteLine($"  ... and {formats.Count - 15} more formats");
    }

    internal static class ConsoleProgress
    {
        private static int _lastPercent = -1;

        public static void Update(double percent, string? extraInfo = null)
        {
            int current = (int)Math.Round(percent);
            if (current == _lastPercent && extraInfo == null) return;

            _lastPercent = current;

            // Build bar: [=====     ] 50%
            int barWidth = 30;
            int filled = (int)(barWidth * percent / 100);
            string bar = new string('=', filled) + new string(' ', barWidth - filled);

            string line = $"\r[{bar}] {current,3}%  {extraInfo ?? ""}";

            Console.Write(line.PadRight(Console.BufferWidth - 1));
        }

        public static void Clear()
        {
            _lastPercent = -1;
            Console.Write("\r" + new string(' ', Console.BufferWidth - 1) + "\r");
        }

        public static void Complete(string message = "Done!")
        {
            Console.WriteLine($"\r{message.PadRight(Console.BufferWidth - 1)}");
            _lastPercent = -1;
        }
    }

    static string ConvertToRegex(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return "// Empty line";

        // Escape special regex characters
        string escaped = Regex.Escape(line);

        // Replace variable parts with capture groups
        escaped = Regex.Replace(escaped, @"\\\[([^\\\]]+)\\\]", @"\\[(?<processor>$1)\\]"); // [ProcessorName]
        escaped = Regex.Replace(escaped, @"(\d+\.\d+)%", @"(?<percent>\d+\.\d+)%");
        escaped = Regex.Replace(escaped, @"of\s+~?\s*([\d\.]+\s*[A-Za-z]+B)", @"of ~?(?<size>[\d\.]+\s*[A-Za-z]+B)");
        escaped = Regex.Replace(escaped, @"at\s+([\d\.]+\s*[A-Za-z]+/s)", @"at (?<speed>[\d\.]+\s*[A-Za-z]+/s)");
        escaped = Regex.Replace(escaped, @"ETA\s+([\d:]+)", @"ETA (?<eta>[\d:]+)");
        escaped = Regex.Replace(escaped, @"frag\s*(\d+/\d+)", @"frag (?<frag>\d+/\d+)");
        escaped = Regex.Replace(escaped, @"(\d+/\d+)", @"(?<item>\d+)/(?<total>\d+)"); // for playlists etc.

        // Common paths and filenames
        escaped = Regex.Replace(escaped, @"(""[^""]+"")", @"(?<path>$1)");
        escaped = Regex.Replace(escaped, @"'([^']+)'", @"(?<path>'$1')");

        // Make it more flexible
        escaped = escaped.Replace(@"\[download\]", @"\[download\]");
        escaped = escaped.Replace(@"\[info\]", @"\[info\]");

        // Add ^ and $ for full line match (recommended for yt-dlp)
        return $"^{escaped}$";
    }

}