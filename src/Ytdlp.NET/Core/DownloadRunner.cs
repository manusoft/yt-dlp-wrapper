using System.Text;

namespace ManuHub.Ytdlp.NET.Core;

public sealed class DownloadRunner
{
    private readonly ProcessFactory _factory;
    private readonly ProgressParser _progressParser;
    private readonly ILogger _logger;

    public event EventHandler<string>? OnProgress;
    public event EventHandler<string>? OnOutput;
    public event EventHandler<string>? OnError;
    public event EventHandler<CommandCompletedEventArgs>? OnCommandCompleted;

    public DownloadRunner(ProcessFactory factory, ProgressParser parser, ILogger logger)
    {
        _factory = factory;
        _progressParser = parser;
        _logger = logger;
    }

    public async Task RunAsync(string args, CancellationToken ct, bool tuneProcess = true)
    {
        using var process = _factory.Create(args);
        int completed = 0;

        void Complete(bool success, string message)
        {
            if (Interlocked.Exchange(ref completed, 1) == 0)
                OnCommandCompleted?.Invoke(this, new CommandCompletedEventArgs(success, message));
        }

        try
        {
            if (!process.Start())
                throw new YtdlpException("Failed to start yt-dlp process.");

            if (tuneProcess)
                ProcessFactory.Tune(process);

            // ---------------------------
            // STDOUT reader
            // ---------------------------
            var stdoutTask = Task.Run(async () =>
            {
                using var reader = new StreamReader(
                    process.StandardOutput.BaseStream,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 8192,
                    leaveOpen: true);

                while (!ct.IsCancellationRequested)
                {
                    var readTask = reader.ReadLineAsync();
                    var completedTask = await Task.WhenAny(readTask, Task.Delay(Timeout.Infinite, ct));
                    if (completedTask != readTask) break;

                    var line = await readTask;
                    if (line == null) break;

                    try
                    {
                        _progressParser.ParseProgress(line);
                        OnProgress?.Invoke(this, line);
                        OnOutput?.Invoke(this, line);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogType.Error, $"Parse error: {ex.Message}");
                    }
                }
            }, ct);

            // ---------------------------
            // STDERR reader
            // ---------------------------
            var stderrTask = Task.Run(async () =>
            {
                using var reader = new StreamReader(
                    process.StandardError.BaseStream,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 8192,
                    leaveOpen: true);

                while (!ct.IsCancellationRequested)
                {
                    var readTask = reader.ReadLineAsync();
                    var completedTask = await Task.WhenAny(readTask, Task.Delay(Timeout.Infinite, ct));
                    if (completedTask != readTask) break;

                    var line = await readTask;
                    if (line == null) break;

                    OnError?.Invoke(this, line);
                    _logger.Log(LogType.Error, line);
                }
            }, ct);

            // ---------------------------
            // Cancellation handling
            // ---------------------------
            using var registration = ct.Register(() =>
            {
                if (!process.HasExited)
                {
                    _logger.Log(LogType.Info, "Cancellation requested → killing process tree");
                    ProcessFactory.SafeKill(process, _logger);
                }
            });

            // ---------------------------
            // Wait for ALL
            // ---------------------------
            await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync(ct));

            // ---------------------------
            // Final safety kill
            // ---------------------------
            if (!process.HasExited)
                ProcessFactory.SafeKill(process);

            var success = process.ExitCode == 0 && !ct.IsCancellationRequested;
            var message = success ? "Completed successfully"
                : ct.IsCancellationRequested ? "Cancelled by user"
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
            OnError?.Invoke(this, msg);

            throw new YtdlpException(msg, ex);
        }
    }    
}