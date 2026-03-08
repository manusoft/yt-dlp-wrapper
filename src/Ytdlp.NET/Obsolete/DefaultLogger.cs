namespace YtdlpNET;

[Obsolete("This class is no longer used and will be removed in a future version.")]
internal sealed class DefaultLogger : ILogger
{
    public void Log(LogType type, string message)
    {
        Console.WriteLine($"[{type}] {message}");
    }
}