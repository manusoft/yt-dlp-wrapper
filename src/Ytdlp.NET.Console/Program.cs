using ManuHub.Ytdlp.NET;
using System.Diagnostics;
using System.Text;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Must be the FIRST line — before any Console.WriteLine
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Console.Clear();
        Console.WriteLine("yt-dlp .NET Wrapper v2.0 Demo Console App");
        Console.WriteLine("----------------------------------------");

        // Initialize the wrapper (assuming yt-dlp is in PATH or specify path)
        var ytdlp = new Ytdlp(ytdlpPath: $"tools\\yt-dlp.exe", logger: new ConsoleLogger());
        ytdlp.SetFFmpegLocation($"tools");

        // Run all demos/tests sequentially
        //await TestGetVersionAsync(ytdlp);
        //await TestGetFormatsAsync(ytdlp);
        //await TestGetFormatsDetailedAsync(ytdlp);
        await TestGetMetadataAsync(ytdlp);
        //await TestGetSimpleMetadataAsync(ytdlp);
        //await TestDownloadVideoAsync(ytdlp);
        //await TestDownloadAudioAsync(ytdlp);
        // await TestBatchDownloadAsync(ytdlp);
        // await TestSponsorBlockAsync(ytdlp);
        // await TestConcurrentFragmentsAsync(ytdlp);
        // await TestCancellationAsync(ytdlp);
        //await TestGetTitleAsync(ytdlp);


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


    // Test 1: Get yt-dlp version
    private static async Task TestGetVersionAsync(Ytdlp ytdlp)
    {
        Console.WriteLine("\nTest 1: Getting yt-dlp version...");
        var version = await ytdlp.GetVersionAsync();
        Console.WriteLine($"Version: {version}");
    }

    // Test 2: Get detailed formats
    private static async Task TestGetFormatsAsync(Ytdlp ytdlp)
    {
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine("\nTest 2: Fetching available formats...");
        var url = "https://www.youtube.com/watch?v=Xt50Sodg7sA";
        var formats = await ytdlp.GetAvailableFormatsAsync(url);

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

    // Test 3: Get detailed formats
    private static async Task TestGetFormatsDetailedAsync(Ytdlp ytdlp)
    {
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine("\nTest 3: Fetching detailed formats...");
        var url = "https://www.youtube.com/watch?v=cbGywxIH4mI";
        var formats = await ytdlp.GetAvailableFormatsAsync(url);

        stopwatch.Stop(); // stop timer
        Console.WriteLine($"Detailed formats took {stopwatch.Elapsed.TotalSeconds:F3} seconds");

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

    // Test 4: Get metedata 
    private static async Task TestGetMetadataAsync(Ytdlp ytdlp)
    {
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine("\nTest 4: Fetching detailed metedata...");

        var url1 = "https://www.youtube.com/watch?v=YyepU5ztLf4&list=PLXCoHsJ9oLef1c83KIbl9_h7tYyodL15J"; //playlist
        var url2 = "https://www.youtube.com/watch?v=JOIqPThxFb8"; // video
        var metadata = await ytdlp.GetMetadataAsync(url2);
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

    // Test 5: Get simple metedata 
    private static async Task TestGetSimpleMetadataAsync(Ytdlp ytdlp)
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("\nTest 5: Fetching simple metedata...");

        var url = "https://www.youtube.com/watch?v=1K7OkfKAx24";

        var fields = new[] { "id", "thumbnail" };
        var data = await ytdlp.GetMetadataLiteAsync(url, fields);

        stopwatch.Stop(); // stop timer
        Console.WriteLine($"Simple metedata took {stopwatch.Elapsed.TotalSeconds:F3} seconds");

        if (data != null)
        {
            Console.WriteLine($"Id: {data["id"]}");
            Console.WriteLine($"Thumbnail: {data["thumbnail"]}");
        }



        // Basic info
        //Console.WriteLine($"ID          : {metadata.Id}");
        //Console.WriteLine($"Title       : {metadata.Title}");
        //Console.WriteLine($"Duration    : {metadata.Duration}");
        //Console.WriteLine($"Thumbnail   : {metadata.Thumbnail}");
        //Console.WriteLine($"View Count  : {metadata.ViewCount}");
        //Console.WriteLine($"FileSize    : {metadata.FileSize.ToString() ?? "NA"}");
        //Console.WriteLine($"Description : {(metadata.Description?.Length > 120 ? metadata.Description.Substring(0, 120) + "..." : metadata.Description)}");
    }

    // Test 6: Download a video with progress events
    private static async Task TestDownloadVideoAsync(Ytdlp ytdlp)
    {
        Console.WriteLine("\nTest 6: Downloading a video...");
        var url = "https://www.youtube.com/watch?v=OOdEdZ5lYQc";

        // Subscribe to events
        ytdlp.OnProgressDownload += (sender, args) =>
            Console.WriteLine($"Progress: {args.Percent:F2}% - {args.Speed} - ETA {args.ETA}");

        ytdlp.OnCompleteDownload += (sender, message) =>
            Console.WriteLine($"Download complete: {message}");

        ytdlp.OnPostProcessingComplete += (sender, message) =>
            Console.WriteLine($"Post-processing done: {message}");

        await ytdlp
            .SetFormat("bv[height<=720]+ba/b")  // 720p max
            .SetOutputFolder("./downloads")
            .SetOutputTemplate("%(title)s.%(ext)s")
            .ExecuteAsync(url);
    }

    // Test 7 Extract audio only
    private static async Task TestDownloadAudioAsync(Ytdlp ytdlp)
    {
        Console.WriteLine("\nTest 7: Extracting audio...");
        var url = "https://www.youtube.com/watch?v=Xt50Sodg7sA";

        await ytdlp
            .ExtractAudio("mp3")
            .SetFormat("ba")
            .SetOutputFolder("./downloads/audio")
            .ExecuteAsync(url);
    }

    // Test 8: Batch download (concurrent)
    private static async Task TestBatchDownloadAsync(Ytdlp ytdlp)
    {
        Console.WriteLine("\nTest 8: Batch download (3 concurrent)...");
        var urls = new List<string>
            {
                "https://www.youtube.com/watch?v=oRpzM-I2p-I",
                "https://www.youtube.com/watch?v=scRQR-FRfIo",
                "https://www.youtube.com/watch?v=xeOttl1d2bo"
            };

        await ytdlp
            .SetFormat("best[height<=480]")  // Lower quality for speed
            .SetOutputFolder("./downloads/batch")
            .ExecuteBatchAsync(urls, maxConcurrency: 3);
    }

    // Test 9: SponsorBlock removal
    private static async Task TestSponsorBlockAsync(Ytdlp ytdlp)
    {
        Console.WriteLine("\nTest 9: Download with SponsorBlock removal...");
        var url = "https://www.youtube.com/watch?v=JOIqPThxFb8";

        await ytdlp
            .SetFormat("best")
            .RemoveSponsorBlock("all")  // Removes sponsor, intro, etc.
            .SetOutputFolder("./downloads/sponsorblock")
            .ExecuteAsync(url);
    }

    // Test 10: Concurrent fragments (faster download)
    private static async Task TestConcurrentFragmentsAsync(Ytdlp ytdlp)
    {
        Console.WriteLine("\nTest 10: Download with concurrent fragments...");
        var url = "https://www.youtube.com/watch?v=BxX31pR0EcU";

        await ytdlp
            .WithConcurrentFragments(8)  // 8 parallel fragments
            .SetFormat("b")
            .SetOutputTemplate("%(title)s.%(ext)s")
            .SetOutputFolder("./downloads/concurrent")
            .ExecuteAsync(url);
    }

    // Test 11: Cancellation support
    private static async Task TestCancellationAsync(Ytdlp ytdlp)
    {
        Console.WriteLine("\nTest 11: Testing cancellation (will cancel after 10 seconds)...");
        var url = "https://www.youtube.com/watch?v=JOIqPThxFb8";  // A longer video

        var cts = new CancellationTokenSource();
        var downloadTask = ytdlp
            .SetFormat("b")
            .SetOutputTemplate("%(title)s.%(ext)s")
            .SetOutputFolder("./downloads/cancel")
            .ExecuteAsync(url, cts.Token);

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
        var url = "https://www.youtube.com/watch?v=cbGywxIH4mI";

        try
        {
            var downloadTask = ytdlp
                .AddCustomCommand("-e")
                .AddCustomCommand("--no-simulate --no-warning")
                .SetOutputTemplate("%(title)s.%(ext)s")
                .SetOutputFolder("./downloads/cancel")
                .ExecuteAsync(url);

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

}

public class Rootobject
{
    public string id { get; set; }
    public string title { get; set; }
    public object availability { get; set; }
    public object channel_follower_count { get; set; }
    public string description { get; set; }
    public object[] tags { get; set; }
    public Thumbnail[] thumbnails { get; set; }
    public string modified_date { get; set; }
    public int view_count { get; set; }
    public int playlist_count { get; set; }
    public string channel { get; set; }
    public string channel_id { get; set; }
    public string uploader_id { get; set; }
    public string uploader { get; set; }
    public string channel_url { get; set; }
    public string uploader_url { get; set; }
    public string _type { get; set; }
    public Entry[] entries { get; set; }
    public string extractor_key { get; set; }
    public string extractor { get; set; }
    public string webpage_url { get; set; }
    public string original_url { get; set; }
    public string webpage_url_basename { get; set; }
    public string webpage_url_domain { get; set; }
    public object release_year { get; set; }
    public int epoch { get; set; }
    public __Files_To_Move __files_to_move { get; set; }
    public _Version _version { get; set; }
}

public class __Files_To_Move
{
}

public class _Version
{
    public string version { get; set; }
    public object current_git_head { get; set; }
    public string release_git_head { get; set; }
    public string repository { get; set; }
}

public class Thumbnail
{
    public string url { get; set; }
    public int height { get; set; }
    public int width { get; set; }
    public string id { get; set; }
    public string resolution { get; set; }
}

public class Entry
{
    public string _type { get; set; }
    public string ie_key { get; set; }
    public string id { get; set; }
    public string url { get; set; }
    public string title { get; set; }
    public object description { get; set; }
    public int duration { get; set; }
    public string channel_id { get; set; }
    public string channel { get; set; }
    public string channel_url { get; set; }
    public string uploader { get; set; }
    public string uploader_id { get; set; }
    public string uploader_url { get; set; }
    public Thumbnail1[] thumbnails { get; set; }
    public object timestamp { get; set; }
    public object release_timestamp { get; set; }
    public object availability { get; set; }
    public int view_count { get; set; }
    public object live_status { get; set; }
    public object channel_is_verified { get; set; }
    public object __x_forwarded_for_ip { get; set; }
}

public class Thumbnail1
{
    public string url { get; set; }
    public int height { get; set; }
    public int width { get; set; }
}
