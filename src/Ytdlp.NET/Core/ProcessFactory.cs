using System.Diagnostics;
using System.Text;

namespace ManuHub.Ytdlp.NET.Core;

public sealed class ProcessFactory
{
    private readonly string _ytdlpPath;
    private readonly string _workingDirectory;

    public ProcessFactory(string ytdlpPath, string? workingDirectory = null)
    {
        _ytdlpPath = ytdlpPath ?? throw new ArgumentNullException(nameof(ytdlpPath));
        _workingDirectory = workingDirectory ?? Environment.CurrentDirectory;
    }

    public Process Create(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            throw new ArgumentException("Arguments cannot be empty", nameof(arguments));

        var psi = new ProcessStartInfo
        {
            FileName = _ytdlpPath,
            Arguments = arguments,

            // Must for async/event-based reading
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,

            UseShellExecute = false,
            CreateNoWindow = true,

            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,

            WorkingDirectory = _workingDirectory
        };

        // Force consistent encoding (yt-dlp / python)
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        psi.Environment["PYTHONUTF8"] = "1";
        psi.Environment["LC_ALL"] = "en_US.UTF-8";
        psi.Environment["LANG"] = "en_US.UTF-8";

        var process = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true
        };

        return process;
    }

    public static void Tune(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.PriorityClass = ProcessPriorityClass.BelowNormal;
            }
        }
        catch
        {
            // Ignore platform-specific failures
        }
    }

    public static void SafeKill(Process process, ILogger? logger = null)
    {
        try
        {
            if (process.HasExited)
                return;

            // Close streams first → prevents ReadLine hang
            try { process.StandardOutput?.Close(); } catch { }
            try { process.StandardError?.Close(); } catch { }
            try { process.StandardInput?.Close(); } catch { }

            process.Kill(entireProcessTree: true);

            logger?.Log(LogType.Info, "Process killed (entire tree)");
        }
        catch (Exception ex)
        {
            logger?.Log(LogType.Error, $"Failed to kill process: {ex.Message}");
        }
    }
}