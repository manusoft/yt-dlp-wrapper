using ManuHub.Ytdlp.Core;
using System.Diagnostics;
using System.Text;

namespace ManuHub.Ytdlp;

public sealed class YtdlpCommand : IAsyncDisposable
{
    private readonly YtdlpBuilder _config;
    private Process? _process;
    private readonly ProgressParser _progressParser;

    public event EventHandler<DownloadProgressEventArgs>? ProgressChanged;
    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? ErrorReceived;
    public event EventHandler? Completed;
    public event EventHandler<string>? PostProcessingCompleted;

    internal YtdlpCommand(YtdlpBuilder config)
    {
        _config = config;
        _progressParser = new ProgressParser(config.Logger);

        _progressParser.OnProgressDownload += (s, e) => ProgressChanged?.Invoke(this, e);
        _progressParser.OnOutputMessage += (s, msg) => OutputReceived?.Invoke(this, msg);
        _progressParser.OnErrorMessage += (s, msg) => ErrorReceived?.Invoke(this, msg);
        _progressParser.OnCompleteDownload += (s, _) => Completed?.Invoke(this, EventArgs.Empty);
        _progressParser.OnPostProcessingComplete += (s, msg) => PostProcessingCompleted?.Invoke(this, msg);
    }

    public async Task ExecuteAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL cannot be empty", nameof(url));

        string arguments = BuildArguments(url);
        _config.Logger.Log(LogType.Info, $"Executing yt-dlp {arguments}");

        var psi = new ProcessStartInfo
        {
            FileName = _config.YtDlpPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _config.OutputFolder
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

        Directory.CreateDirectory(_config.OutputFolder);

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        try
        {
            await _process.WaitForExitAsync(ct);
            if (_process.ExitCode != 0)
                throw new YtdlpException($"yt-dlp exited with code {_process.ExitCode}");
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
        }
    }

    public async Task ExecuteBatchAsync(IEnumerable<string> urls, int maxConcurrency = 4, CancellationToken ct = default)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = new List<Task>();
        foreach (var url in urls)
        {
            await semaphore.WaitAsync(ct);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await ExecuteAsync(url, ct);
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct));
        }
        await Task.WhenAll(tasks);
    }

    private string BuildArguments(string url)
    {
        var sb = new StringBuilder();

        // Output
        string template = Path.Combine(_config.OutputFolder, _config.OutputTemplate.Replace("\\", "/"));
        sb.Append($"-o \"{template}\" ");

        // Format
        if (!string.IsNullOrWhiteSpace(_config.Format))
            sb.Append($"-f \"{_config.Format}\" ");

        // Concurrent
        if (_config.ConcurrentFragments.HasValue)
            sb.Append($"--concurrent-fragments {_config.ConcurrentFragments.Value} ");

        // Flags
        foreach (var flag in _config.Flags)
            sb.Append($"{flag} ");

        // Options
        foreach (var kv in _config.Options)
            sb.Append(kv.Value is null ? $"{kv.Key} " : $"{kv.Key} \"{kv.Value}\" ");

        // Booleans / specials
        if (_config.Simulate) sb.Append("--simulate ");
        if (_config.NoOverwrites) sb.Append("--no-overwrites ");
        if (_config.KeepFragments) sb.Append("--keep-fragments ");
        if (_config.CookiesFile != null) sb.Append($"--cookies \"{_config.CookiesFile}\" ");
        if (_config.CookiesFromBrowser != null) sb.Append($"--cookies-from-browser {_config.CookiesFromBrowser} ");
        if (_config.Referer != null) sb.Append($"--referer \"{_config.Referer}\" ");
        if (_config.UserAgent != null) sb.Append($"--user-agent \"{_config.UserAgent}\" ");
        if (_config.Proxy != null) sb.Append($"--proxy \"{_config.Proxy}\" ");
        if (_config.FfmpegLocation != null) sb.Append($"--ffmpeg-location \"{_config.FfmpegLocation}\" ");
        if (_config.SponsorblockRemoveCategories != null) sb.Append($"--sponsorblock-remove {_config.SponsorblockRemoveCategories} ");

        // URL
        sb.Append($" \"{url.Replace("\"", "\\\"")}\"");  // basic sanitize

        return sb.ToString().Trim();
    }

    public async ValueTask DisposeAsync()
    {
        if (_process == null || _process.HasExited) return;

        try
        {
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