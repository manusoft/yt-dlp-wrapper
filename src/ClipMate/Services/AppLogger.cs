using YtdlpDotNet;

namespace ClipMate.Services;

public class AppLogger : ILogger
{
    private static readonly object LogLock = new();
    private static string _logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"Log_{DateTime.Now.ToString("yyyyMMdd")}.log");

    public void Log(LogType type, string message)
    {
        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}] {message}";
            Console.WriteLine(_logFilePath);
            lock (LogLock)
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}
