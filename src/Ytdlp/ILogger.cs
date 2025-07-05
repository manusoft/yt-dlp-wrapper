namespace YtdlpDotNet;


public interface ILogger
{
    void Log(LogType type, string message);
}