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

        var consoleLogger = new ConsoleLogger();
        var builder = Ytdlp.Create($"tools\\yt-dlp.exe", consoleLogger);
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
        var formats = await YtdlpProbe.GetAvailableFormatsAsync("https://youtu.be/dQw4w9WgXcQ", builder);
        foreach (var f in formats)
        {
            Console.WriteLine($"{f.FormatId} | {f.Extension} | {f.FormatNote}");
        }

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

