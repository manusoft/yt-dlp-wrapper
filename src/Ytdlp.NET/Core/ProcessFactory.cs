using System.Diagnostics;
using System.Text;

namespace ManuHub.Ytdlp.NET.Core;

public sealed class ProcessFactory
{
    private readonly string _ytDlpPath;

    public ProcessFactory(string ytDlpPath)
    {
        _ytDlpPath = ytDlpPath;
    }

    public Process Create(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ytDlpPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        psi.Environment["PYTHONUTF8"] = "1";
        psi.Environment["LC_ALL"] = "en_US.UTF-8";
        psi.Environment["LANG"] = "en_US.UTF-8";

        return new Process { StartInfo = psi, EnableRaisingEvents = true };
    }
}