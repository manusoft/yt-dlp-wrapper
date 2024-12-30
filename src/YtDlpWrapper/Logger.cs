namespace YtDlpWrapper;

public class Logger
{
    private static readonly object LogLock = new();
    private static string _logFilePath;

    static Logger()
    {
        string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        _logFilePath = Path.Combine(logDirectory, "application.log");
    }

    public static void Log(LogType logType, string message)
    {
        try
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {logType.ToString().ToUpper()}: {message}";

            lock (LogLock)
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            // Optionally handle logging failures (e.g., fallback to console logging).
            Console.Error.WriteLine($"Logging failed: {ex.Message}");
        }
    }

    public static void SetLogFilePath(string filePath)
    {
        _logFilePath = filePath;
    }
}