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

    public async Task RunAsync(string arguments, CancellationToken ct)
    {
        var process = _factory.Create(arguments);

        try
        {
            if (!process.Start())
                throw new YtdlpException("Failed to start yt-dlp process.");

            // Improved cancellation: Try to close streams first, then kill
            using var ctsRegistration = ct.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        _logger.Log(LogType.Info, "yt-dlp process killed due to cancellation");
                    }
                }
                catch
                {
                    // silent - already dead or disposed
                }
            });

            // Read output and error concurrently
            var outputTask = Task.Run(async () =>
            {
                string? line;
                while ((line = await process.StandardOutput.ReadLineAsync()) != null)
                {
                    ct.ThrowIfCancellationRequested();
                    _progressParser.ParseProgress(line);
                    OnProgress?.Invoke(this, line);
                }
            }, ct);

            var errorTask = Task.Run(async () =>
            {
                string? line;
                while ((line = await process.StandardError.ReadLineAsync()) != null)
                {
                    ct.ThrowIfCancellationRequested();
                    OnErrorMessage?.Invoke(this, line);
                    _logger.Log(LogType.Error, line);
                }
            }, ct);

            await Task.WhenAll(outputTask, errorTask);

            // Wait for exit (may throw OperationCanceledException)
            await process.WaitForExitAsync(ct);

            // Only throw on real failure (not cancellation)
            if (process.ExitCode != 0 && !ct.IsCancellationRequested)
            {
                throw new YtdlpException($"yt-dlp exited with code {process.ExitCode}");
            }

            // Success or intentional cancel
            var success = !ct.IsCancellationRequested;
            var message = success ? "Completed successfully" : "Cancelled by user";
            OnCommandCompleted?.Invoke(this, new CommandCompletedEventArgs(success, message));
        }
        catch (OperationCanceledException)
        {
            // Normal cancel path — no need to log again
            OnCommandCompleted?.Invoke(this, new CommandCompletedEventArgs(false, "Cancelled by user"));
            throw; // let caller handle if needed
        }
        catch (Exception ex)
        {
            var msg = $"Error executing yt-dlp: {ex.Message}";
            OnErrorMessage?.Invoke(this, msg);
            _logger.Log(LogType.Error, msg);
            throw new YtdlpException(msg, ex);
        }
    }
}