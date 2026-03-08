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
        Console.WriteLine("yt-dlp .NET Wrapper v2.0 Demo Console App");
        Console.WriteLine("----------------------------------------");

        var ytdlpPath = $"tools\\yt-dlp.exe";
        var consoleLogger = new ConsoleLogger();
        var builder = Ytdlp.Create(ytdlpPath, consoleLogger);
        //var command = builder.WithFormat("b").Build();
        //await command.ExecuteAsync("https://www.youtube.com/watch?v=bkst470K_n4");

        // Raw metadata JSON
        //var rawjson = await YtdlpProbe.GetVideoMetadataRawAsync("https://youtu.be/dQw4w9WgXcQ", builder) ?? "Error";        

        // With custom config (e.g. cookies, proxy)
        //var builder = Ytdlp.Create().WithCookiesFile("cookies.txt");
        //var rawformats = await YtdlpProbe.GetFormatsRawAsync("https://youtu.be/dQw4w9WgXcQ", builder);

        // Full metadata with formats list
        //var metadata = await YtdlpProbe.GetVideoMetadataAsync("https://www.youtube.com/watch?v=dQw4w9WgXcQ", builder);
        //if (metadata != null)
        //{
        //    Console.WriteLine($"Title: {metadata.Title}");
        //    Console.WriteLine($"Duration: {metadata.Duration} s");
        //    Console.WriteLine($"Formats count: {metadata.Formats?.Count ?? 0}");
        //}

        // Best video ≤ 720p
        //string? video720p = await YtdlpProbe.GetBestVideoFormatIdAsync("https://youtu.be/dQw4w9WgXcQ", maxHeight: 720, builder);

        // Best audio format ID
        //string? bestAudio = await YtdlpProbe.GetBestAudioFormatIdAsync("https://youtu.be/dQw4w9WgXcQ", builder);

        // List formats (text parsed)
        //var formats = await YtdlpProbe.GetAvailableFormatsAsync("https://youtu.be/dQw4w9WgXcQ", builder);
        //foreach (var f in formats)
        //{
        //    Console.WriteLine($"{f.Id} | {f.Extension} | {f.Note}");
        //}


        string version = await Ytdlp.VersionAsync(ytdlpPath);
        Console.WriteLine($"Current yt-dlp version: {version}");

        //builder 
        //    .WithFormat("best")
        //    .WithOutputFolder("C:\\downloads");

        //var urls = new[] {
        //    "https://youtu.be/dQw4w9WgXcQ",
        //    "https://youtu.be/9bZkp7q19f0",
        //    "https://youtu.be/kXYiU_JCYtU"
        //};

        //var tasks = urls.Select(url => builder.Build().ExecuteAsync(url));
        //await Task.WhenAll(tasks);

        var cmd = builder
            .WithFormat("b")
            .WithConcurrentFragments(8)
            .WithFFmpegLocation($"tools")            
            .WithHomeFolder(@"C:\Downloads")
            .WithTempFolder(@"C:\Downloads\Temp")
            .WithOutputTemplate( "%(title)s [%(id)s].%(ext)s")
            .Build();


        //cmd.OnExtracting += (s, url) => Console.WriteLine($"[Stage] Extracting: {url}");
        //cmd.OnDownloadingStarted += (s, e) => Console.WriteLine("[Stage] Downloading started");
        //cmd.OnPostProcessingStarted += (s, path) => Console.WriteLine($"[Stage] Processing started: {path}");
        //cmd.OnPostProcessingCompleted += (s, path) => Console.WriteLine($"[Stage] Processing completed: {path}");
        //cmd.OnCompleted += (s, e) => Console.WriteLine("[Stage] Finished!");
        //cmd.OnProgressChanged += (s, e) => Console.WriteLine($"[Progress] {e.Message}");

        await cmd.ExecuteAsync("https://www.youtube.com/watch?v=Cqln0nwjcYo");

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
}

