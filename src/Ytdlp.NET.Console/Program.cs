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

        var ConsoleLogger = new ConsoleLogger();
        var builder = Ytdlp.Create($"tools\\yt-dlp.exe", ConsoleLogger);
        var command = builder.WithFormat("b").Build();
        await command.ExecuteAsync("https://www.youtube.com/watch?v=bkst470K_n4");


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

