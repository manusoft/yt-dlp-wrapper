namespace YtdlpNET;

[Obsolete("This class is no longer used and will be removed in a future version.")]
public interface ILogger
{
    void Log(LogType type, string message);
}