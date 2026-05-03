using System.Text;

namespace ManuHub.Ytdlp.NET.Core;

public sealed class ProbeRunner
{
    private readonly ProcessFactory _factory;
    private readonly ILogger _logger;

    public event EventHandler<string>? OnErrorMessage;
    public event EventHandler<CommandCompletedEventArgs>? OnCommandCompleted;

    public ProbeRunner(ProcessFactory factory, ILogger logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<string?> RunAsync(string args, CancellationToken ct = default, bool tuneProcess = true, int bufferKb = 256)
    {
        // Reasonable buffer: 256 KB default (good for large JSON), min 64 KB
        if (bufferKb < 64) bufferKb = 64;
        int bufferSize = bufferKb * 1024;

        using var process = _factory.Create(args);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int completed = 0;

        void Complete(bool success, string message)
        {
            if (Interlocked.Exchange(ref completed, 1) == 0)
            {
                OnCommandCompleted?.Invoke(this, new CommandCompletedEventArgs(success, message));
            }
        }

        try
        {
            // Attach Exited handler BEFORE starting
            process.Exited += (_, _) => tcs.TrySetResult(true);

            // Handle stderr (warnings, errors, verbose info from yt-dlp)
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OnErrorMessage?.Invoke(this, e.Data);
                    _logger.Log(LogType.Warning, e.Data);
                }
            };

            if (!process.Start())
                throw new YtdlpException("Failed to start yt-dlp probe process.");

            if (tuneProcess)
                ProcessFactory.Tune(process);

            process.BeginErrorReadLine();   // Only stderr uses events

            // 🔥 Large-buffered reader for heavy JSON on stdout
            using var reader = new StreamReader(
                process.StandardOutput.BaseStream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: bufferSize,
                leaveOpen: true);

            // Start reading asynchronously
            var readTask = reader.ReadToEndAsync(ct);   // Pass ct for better cancellation support

            // Cancellation support
            using var registration = ct.Register(() =>
            {
                if (!process.HasExited)
                {
                    _logger.Log(LogType.Info, "Probe cancellation requested → SafeKill");
                    ProcessFactory.SafeKill(process, _logger);
                }
            });

            // Wait for either process exit, read completion, or cancellation
            await Task.WhenAny(tcs.Task, readTask, Task.Delay(Timeout.Infinite, ct));

            // Ensure process is terminated if still running
            if (!process.HasExited)
                ProcessFactory.SafeKill(process, _logger);

            // Wait for clean exit
            await process.WaitForExitAsync(ct);

            // Get the full output
            string output = await readTask;

            // Determine success
            bool success = process.ExitCode == 0
                        && !ct.IsCancellationRequested
                        && !string.IsNullOrWhiteSpace(output);

            string message = success ? "Probe completed successfully" :
                            ct.IsCancellationRequested ? "Probe cancelled by user" :
                            $"Probe failed with exit code {process.ExitCode}";

            Complete(success, message);

            // Return trimmed output only on real success
            return success ? output.Trim() : null;
        }
        catch (OperationCanceledException)
        {
            Complete(false, "Probe cancelled by user");
            _logger.Log(LogType.Warning, "Probe was cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            var msg = $"Error executing yt-dlp probe: {ex.Message}";
            _logger.Log(LogType.Warning, msg);
            OnErrorMessage?.Invoke(this, msg);
            Complete(false, msg);
            return null;
        }
    }
}

