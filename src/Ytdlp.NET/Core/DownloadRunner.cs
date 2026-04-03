using System.Diagnostics;

namespace ManuHub.Ytdlp.NET.Core;

public sealed class DownloadRunner
{
    private readonly ProcessFactory _factory;
    private readonly ProgressParser _progressParser;
    private readonly ILogger _logger;

    public event EventHandler<string>? OnProgress;
    public event EventHandler<string>? OnErrorMessage;
    public event EventHandler<CommandCompletedEventArgs>? OnCommandCompleted;

    public DownloadRunner(ProcessFactory factory, ProgressParser parser, ILogger logger)
    {
        _factory = factory;
        _progressParser = parser;
        _logger = logger;
    }

    public async Task RunAsync(string arguments, CancellationToken ct, bool tuneProcess = true)
    {
        using var process = _factory.Create(arguments);

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
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // ✅ Attach BEFORE Start (fix race condition)
            process.Exited += (_, _) => tcs.TrySetResult(true);

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data == null) return;

                try
                {
                    _progressParser.ParseProgress(e.Data);
                    OnProgress?.Invoke(this, e.Data);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogType.Error, $"Parse error: {ex.Message}");
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null) return;

                OnErrorMessage?.Invoke(this, e.Data);
                _logger.Log(LogType.Error, e.Data);
            };

            if (!process.Start())
                throw new YtdlpException("Failed to start yt-dlp process.");

            if (tuneProcess)
                ProcessFactory.Tune(process);

            // ✅ Start reading AFTER handlers
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // 🔥 Cancellation
            using var registration = ct.Register(() =>
            {
                if (!process.HasExited)
                {
                    _logger.Log(LogType.Info, "Cancellation requested → killing process tree");
                    ProcessFactory.SafeKill(process, _logger);
                }
            });

            // Wait for exit OR cancellation
            await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, ct));

            // Ensure process is dead
            if (!process.HasExited)
            {
                ProcessFactory.SafeKill(process, _logger);
            }

            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                ProcessFactory.SafeKill(process, _logger);
            }

            var success = process.ExitCode == 0 && !ct.IsCancellationRequested;

            var message = success
                ? "Completed successfully"
                : ct.IsCancellationRequested
                    ? "Cancelled by user"
                    : $"Failed with exit code {process.ExitCode}";

            Complete(success, message);
        }
        catch (OperationCanceledException)
        {
            Complete(false, "Cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            var msg = $"Error executing yt-dlp: {ex.Message}";
            _logger.Log(LogType.Error, msg);
            OnErrorMessage?.Invoke(this, msg);

            throw new YtdlpException(msg, ex);
        }
    }
}