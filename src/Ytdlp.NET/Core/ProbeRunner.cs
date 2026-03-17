using System.Text;

namespace ManuHub.Ytdlp.NET.Core;

public sealed class ProbeRunner
{
    private readonly ProcessFactory _factory;
    private readonly ILogger _logger;

    public ProbeRunner(ProcessFactory factory, ILogger logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<string?> RunAsync(string args, CancellationToken ct = default, int bufferKb = 128)
    {
        var process = _factory.Create(args);

        // Validate buffer size: minimum 8 KB
        if (bufferKb < 8) bufferKb = 8;
        int bufferSize = bufferKb * 1024;

        try
        {
            process.Start();

            // Use StreamReader with large buffer + explicit UTF-8
            string output;
            using (var reader = new StreamReader(process.StandardOutput.BaseStream,
                                                 Encoding.UTF8,
                                                 detectEncodingFromByteOrderMarks: false,
                                                 bufferSize: bufferSize,     // default 8kb for JSON
                                                 leaveOpen: true))           // don't close underlying stream
            {
                output = await reader.ReadToEndAsync();
            }

            // Optional: drain stderr in background (prevents blocking if warnings are many)
            _ = Task.Run(() => process.StandardError.ReadToEndAsync(), ct);

            using (ct.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(true); } catch { }
            }))
            {
                await process.WaitForExitAsync(ct);
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                _logger.Log(LogType.Warning, "Empty output.");
                return null;
            }

            return output;
        }
        catch (OperationCanceledException)
        {
            _logger.Log(LogType.Warning, "Process cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogType.Warning, $"Process failed: {ex.Message}");
            return null;
        }
    }
}
