using ManuHub.Ytdlp.Core;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace ManuHub.Ytdlp;

public sealed class YtdlpCommand : IAsyncDisposable
{
    private readonly YtdlpBuilder _config;
    private Process? _process;
    private readonly ProgressParser _progressParser;

    // Common
    public event EventHandler<DownloadProgressEventArgs>? OnProgressDownload;
    public event EventHandler<string>? OnProgressMessage;
    public event EventHandler<string>? OnPostProcessingStarted;
    public event EventHandler<string>? OnPostProcessingCompleted;
    public event EventHandler<string>? OnCompleteDownload;
    public event EventHandler<string>? OnProcessCompleted;
    public event EventHandler<string>? OnErrorMessage;

    internal YtdlpCommand(YtdlpBuilder config)
    {
        _config = config;
        _progressParser = new ProgressParser(config.Logger);

        _progressParser.OnProgressDownload += (s, e) => OnProgressDownload?.Invoke(this, e);
        _progressParser.OnProgressMessage += (s, msg) => OnProgressMessage?.Invoke(this, msg);
        _progressParser.OnPostProcessingStarted += (s, msg) => OnPostProcessingStarted?.Invoke(this, msg);
        _progressParser.OnPostProcessingCompleted += (s, msg) => OnPostProcessingCompleted?.Invoke(this, msg);
        _progressParser.OnCompleteDownload += (s, msg) => OnCompleteDownload?.Invoke(this, msg);
        _progressParser.OnErrorMessage += (s, msg) => OnErrorMessage?.Invoke(this, msg);       
    }

    private int _isRunning; // 0 = not running, 1 = running

    public async Task ExecuteAsync(string url, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); 

        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL cannot be empty", nameof(url));

        // Ensure output folder exists
        try
        {
            if (!_config.IsProbe)
            {
                Directory.CreateDirectory(_config.OutputFolder);
                _config.Logger.Log(LogType.Info, $"Ensured output folder exists: {_config.OutputFolder}");
            }
        }
        catch (Exception ex)
        {
            _config.Logger.Log(LogType.Error, $"Failed to create output folder: {ex.Message}");
            throw new YtdlpException("Failed to create output folder", ex);
        }

        // Build args using YtdlpBuilder
        var argsList = _config.BuildArgs(url);
        string arguments = string.Join(" ", argsList.Select(Quote));
        _config.Logger.Log(LogType.Info, $"Executing yt-dlp {arguments}");

        var psi = new ProcessStartInfo
        {
            FileName = _config.YtDlpPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Directory.Exists(_config.OutputFolder)
                ? _config.OutputFolder
                : Directory.GetCurrentDirectory()
        };

        _process = new Process { StartInfo = psi };

        _process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null) _progressParser.ParseProgress(e.Data);
        };
        _process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null) _progressParser.ParseProgress(e.Data);
        };

        // Thread-safety: prevent concurrent execution
        if (Interlocked.Exchange(ref _isRunning, 1) == 1)
            throw new InvalidOperationException("Command is already running.");

        try
        {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            await _process.WaitForExitAsync(ct);

            if (_process.ExitCode != 0)
                throw new YtdlpException($"yt-dlp exited with code {_process.ExitCode}");

            OnProcessCompleted?.Invoke(this, "Process completed successfully.");
            _config.Logger.Log(LogType.Info, "Execution completed successfully.");
        }
        catch (OperationCanceledException)
        {
            await DisposeAsync();
            throw;
        }
        finally
        {
            _progressParser.Reset();
            Interlocked.Exchange(ref _isRunning, 0); // Reset running flag
        }
    }

    /// <summary>
    /// Quotes an argument if it contains spaces or special characters
    /// </summary>
    private static string Quote(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "\"\"";
        // Escape " and \
        string escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
    }

    public async Task<List<(string Url, Exception? Error)>> ExecuteBatchAsync(IEnumerable<string> urls, int maxConcurrency = 4, CancellationToken ct = default)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var results = new ConcurrentBag<(string Url, Exception? Error)>();

        var tasks = urls.Select(async url =>
        {
            await semaphore.WaitAsync(ct);

            try
            {
                var command = new YtdlpCommand(_config);

                // Forward all events to the batch caller
                command.OnProgressDownload += (s, e) => OnProgressDownload?.Invoke(s, e);
                command.OnProgressMessage += (s, e) => OnProgressMessage?.Invoke(s, e);
                command.OnPostProcessingStarted += (s, e) => OnPostProcessingStarted?.Invoke(s, e);
                command.OnPostProcessingCompleted += (s, e) => OnPostProcessingCompleted?.Invoke(s, e);
                command.OnCompleteDownload += (s, e) => OnCompleteDownload?.Invoke(s, e);
                command.OnProcessCompleted += (s, e) => OnProcessCompleted?.Invoke(s, e);
                command.OnErrorMessage += (s, e) => OnErrorMessage?.Invoke(s, e);

                await command.ExecuteAsync(url, ct);
                results.Add((url, null));
            }
            catch (Exception ex)
            {
                results.Add((url, ex));
                _config.Logger.Log(LogType.Error, $"Failed to download {url}: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.ToList();
    }

    public async ValueTask DisposeAsync()
    {
        if (_process == null || _process.HasExited) return;

        try
        {
            if (!_process.HasExited)
                _process.Kill(true);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _config.Logger.Log(LogType.Warning, $"Dispose error: {ex.Message}");
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }
}