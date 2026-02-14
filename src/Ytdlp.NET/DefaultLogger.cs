namespace YtdlpNET;

internal sealed class DefaultLogger : ILogger
{
    public void Log(LogType type, string message)
    {
        Console.WriteLine($"[{type}] {message}");
    }
}