namespace ManuHub.Ytdlp;

public class DefaultLogger : ILogger
{
    public void Log(LogType type, string message)
    {
        Console.WriteLine($"[{type}] {message}");
    }
}