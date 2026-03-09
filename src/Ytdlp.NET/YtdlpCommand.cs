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
    public event EventHandler<string>? OnOutputReceived;
    public event EventHandler<string>? OnErrorReceived;
    public event EventHandler? OnCompleted;

    // Downloading stages (not mutually exclusive, may overlap or repeat in some cases)
    public event EventHandler<string>? OnExtracting;                  // url
    public event EventHandler? OnDownloadingStarted;
    public event EventHandler<DownloadProgressEventArgs>? OnProgressChanged;
    public event EventHandler<string>? OnPostProcessingStarted;       // file path if available
    public event EventHandler<string>? OnPostProcessingCompleted;     // file path if available

    internal YtdlpCommand(YtdlpBuilder config)
    {
        _config = config;
        _progressParser = new ProgressParser(config.Logger);


        _progressParser.OnOutputMessage += (s, msg) => OnOutputReceived?.Invoke(this, msg);
        _progressParser.OnErrorMessage += (s, msg) => OnErrorReceived?.Invoke(this, msg);

        _progressParser.OnProgressDownload += (s, e) => OnProgressChanged?.Invoke(this, e);
        _progressParser.OnPostProcessingStarted += (s, msg) => OnPostProcessingStarted?.Invoke(this, msg);
        _progressParser.OnPostProcessingCompleted += (s, msg) => OnPostProcessingCompleted?.Invoke(this, msg);
    }

    public async Task ExecuteAsync(string url, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); // early check

        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL cannot be empty", nameof(url));

        // Create output folder if needed(still useful)
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

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        try
        {
            await _process.WaitForExitAsync(ct);

            if (_process.ExitCode != 0)
                throw new YtdlpException($"yt-dlp exited with code {_process.ExitCode}");

            OnCompleted?.Invoke(this, EventArgs.Empty);
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

    public async Task<List<(string Url, Exception? Error)>> ExecuteBatchAsync(IEnumerable<string> urls, int maxConcurrency = 4, CancellationToken ct = default)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var results = new ConcurrentBag<(string Url, Exception? Error)>();

        var tasks = urls.Select(async url =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                await new YtdlpCommand(_config).ExecuteAsync(url, ct);
                results.Add((url, null));
            }
            catch (Exception ex)
            {
                results.Add((url, ex));
                _config.Logger.Log(LogType.Error, $"Failed {url}: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.ToList();
    }

    private string BuildArguments(string url)
    {
        var sb = new StringBuilder();

        if (!_config.IsProbe)
        {
            // ─── Paths (home & temp) ────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(_config.HomeFolder))
                sb.Append($"--paths home:\"{_config.HomeFolder}\" ");

            if (!string.IsNullOrWhiteSpace(_config.TempFolder))
                sb.Append($"--paths temp:\"{_config.TempFolder}\" ");

            // ─── Output ─────────────────────────────────────────────────────────────
            // Keep template RELATIVE — do NOT combine OutputFolder here

            string relativeTemplate = _config.OutputTemplate;
            sb.Append($"-o \"{relativeTemplate}\" ");


            // ─── Format ─────────────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(_config.Format))
                sb.Append($"-f \"{_config.Format}\" ");

            // ─── Concurrent fragments ───────────────────────────────────────────────
            if (_config.ConcurrentFragments.HasValue)
                sb.Append($"--concurrent-fragments {_config.ConcurrentFragments.Value} ");

        }

        // ─── Flags ──────────────────────────────────────────────────────────────
        foreach (var flag in _config.Flags)
            sb.Append($"{flag} ");

        // ─── Key-value options ──────────────────────────────────────────────────
        foreach (var kv in _config.Options)
            sb.Append(kv.Value is null ? $"{kv.Key} " : $"{kv.Key} {Quote(kv.Value)} ");

        // ─── Special booleans & paths (all quoted where needed) ─────────────────
        if (_config.CookiesFile != null) sb.Append($"--cookies {Quote(_config.CookiesFile)} ");
        if (_config.CookiesFromBrowser != null) sb.Append($"--cookies-from-browser {_config.CookiesFromBrowser} ");
        if (_config.Proxy != null) sb.Append($"--proxy {Quote(_config.Proxy)} ");
        if (_config.FfmpegLocation != null) sb.Append($"--ffmpeg-location {Quote(_config.FfmpegLocation)} ");
        if (_config.SponsorblockRemoveCategories != null) sb.Append($"--sponsorblock-remove {Quote(_config.SponsorblockRemoveCategories)} ");


        // ─── URL (minimal sanitization) ─────────────────────────────────────────
        string safeUrl = Quote(url);
        sb.Append($" {safeUrl}");

        return sb.ToString().Trim();
    }

    private static string Quote(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "\"\"";
        // Escape " and \
        string escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escaped}\"";
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