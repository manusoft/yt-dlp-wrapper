namespace Ytdlp.NET;


public interface ILogger
{
    void Log(LogType type, string message);
}