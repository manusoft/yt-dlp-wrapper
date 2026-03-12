using ManuHub.Ytdlp.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ManuHub.Ytdlp;

/// <summary>
/// Static probe utilities for yt-dlp metadata and format information (no actual download).
/// Uses yt-dlp --dump-json, --list-formats, etc., and captures output in memory.
/// </summary>
public sealed class YtdlpProbe : IAsyncDisposable
{
    private Process? _process;
    private readonly string _url;
    private readonly string? _path;
    private readonly ILogger? _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal YtdlpProbe(string url, string? path, ILogger? logger)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        _url = url;
        _path = path;
        _logger = logger;
    }

    // ──────────────────────────────────────────────
    // Core probe methods
    // ──────────────────────────────────────────────

    public async Task<List<Format>> GetFormatsDetailedAsync(CancellationToken ct = default)
    {
        var args = $"--dump-single-json --simulate --skip-download --no-playlist --quiet --no-warnings {Quote(_url)}";

        var result = await RunAsync(args, ct);

        if (string.IsNullOrWhiteSpace(result.Output))
            throw new YtdlpException("Empty JSON response");

        try
        {

            var info = JsonSerializer.Deserialize<MetadataLite>(result.Output, JsonOptions);
            if (info?.Formats == null || info.Formats.Count == 0)
                return await GetAvailableFormatsAsync(ct);

            return MapFormats(info.Formats);
        }
        catch (JsonException)
        {
            return await GetAvailableFormatsAsync(ct);
        }
    }

    public async Task<List<Format>> GetAvailableFormatsAsync(CancellationToken ct = default)
    {
        var args = $"--list-formats --simulate --skip-download --no-playlist --quiet --no-warnings {Quote(_url)}";

        var result = await RunAsync(args, ct);

        if (result.ExitCode != 0)
            throw new YtdlpException($"Failed to get yt-dlp version. {result.Error}");

        return ParseFormats(result.Output);
    }

    public async Task<Metadata?> GetVideoMetadataAsync(CancellationToken ct = default)
    {
        var args = $"--dump-single-json --simulate --skip-download --no-playlist --quiet --no-warnings {Quote(_url)}";

        var result = await RunAsync(args, ct);

        if (string.IsNullOrWhiteSpace(result.Output)) return null;

        try
        {
            return JsonSerializer.Deserialize<Metadata>(result.Output, JsonOptions);
        }
        catch (JsonException ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public async Task<string?> GetBestAudioFormatIdAsync(CancellationToken ct = default)
    {
        var args = $"-f {Quote("bestaudio")} --print {Quote("%(format_id)s")}  {Quote(_url)}";

        var result = await RunAsync(args, ct);

        if (string.IsNullOrWhiteSpace(result.Output)) return null;

        return string.IsNullOrWhiteSpace(result.Output) ? null : result.Output.Trim();
    }

    public async Task<string?> GetBestVideoFormatIdAsync(int? maxHeight = null, CancellationToken ct = default)
    {
        string selector = "bestvideo";
        if (maxHeight.HasValue)
            selector += $"[height<=?{maxHeight.Value}]";
        selector += "/best";

        var args = $"-f {Quote(selector)} --print {Quote("%(format_id)s")}  {Quote(_url)}";

        var result = await RunAsync(args, ct);

        if (string.IsNullOrWhiteSpace(result.Output)) return null;

        return string.IsNullOrWhiteSpace(result.Output) ? null : result.Output.Trim();
    }

    public async Task<string?> GetFileSizeAsync(CancellationToken ct = default)
    {
        var args = $"--print {Quote("%(filesize,filesize_approx)s")} {Quote(_url)}";

        var result = await RunAsync(args, ct);

        if (string.IsNullOrWhiteSpace(result.Output)) return null;

        return string.IsNullOrWhiteSpace(result.Output) ? null : result.Output.Trim();
    }

    // ──────────────────────────────────────────────
    // Internal helper: run yt-dlp and capture output
    // ──────────────────────────────────────────────

    private int _isRunning; // 0 = not running, 1 = running

    private async Task<(int ExitCode, string Output, string Error)> RunAsync(string args, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var psi = new ProcessStartInfo
        {
            FileName = _path ?? "yt-dlp",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _process = new Process { StartInfo = psi };

        // Thread-safety: prevent concurrent execution
        if (Interlocked.Exchange(ref _isRunning, 1) == 1)
            throw new InvalidOperationException("Command is already running.");

        try
        {
            _process.Start();

            string output;
            string error;

            // Read stdout
            using (var stdout = new StreamReader(
                _process.StandardOutput.BaseStream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 8192,
                leaveOpen: true))
            {
                output = await stdout.ReadToEndAsync();
            }

            // Read stderr
            using (var stderr = new StreamReader(
                _process.StandardError.BaseStream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 4096,
                leaveOpen: true))
            {
                error = await stderr.ReadToEndAsync();
            }

            using (ct.Register(() =>
            {
                try
                {
                    if (!_process.HasExited)
                        _process.Kill(true);
                }
                catch { }
            }))
            {
                await _process.WaitForExitAsync(ct);
            }

            return (_process.ExitCode, output.Trim(), error.Trim());
        }
        catch (OperationCanceledException)
        {
            await DisposeAsync();
            throw;
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, 0); // Reset running flag
        }
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
            _logger?.Log(LogType.Warning, $"Dispose error: {ex.Message}");
        }
        finally
        {
            _process.Dispose();
            _process = null;
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

    // ──────────────────────────────────────────────
    // Parsing helpers
    // ──────────────────────────────────────────────

    private static List<Format> ParseFormats(string result)
    {
        var formats = new List<Format>();
        if (string.IsNullOrWhiteSpace(result)) return formats;

        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line_ in lines)
        {
            var line = line_.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            if (line.Contains("RESOLUTION") || line.StartsWith("---") || line.StartsWith("ID EXT") || line.Contains("FILESIZE TBR PROTO"))
                continue;

            if (line.StartsWith("[") && line.Contains("]")) break;

            try
            {
                var format = Format.FromParsedLine(line);
                if (!string.IsNullOrEmpty(format.Id) && !formats.Any(f => f.Id == format.Id))
                    formats.Add(format);
            }
            catch { }
        }

        return formats;
    }

    private static List<Format> MapFormats(List<FormatLite> formats)
    {
        var result = new List<Format>(formats.Count);
        foreach (var f in formats)
        {
            if (string.IsNullOrEmpty(f.FormatId)) continue;

            result.Add(new Format
            {
                Id = f.FormatId,
                Extension = f.Ext ?? "",
                Height = f.Height,
                Width = f.Width,
                Resolution = !string.IsNullOrEmpty(f.Resolution)
                    ? f.Resolution
                    : (f.Height.HasValue ? $"{f.Height}p" : "audio only"),
                Fps = f.Fps,
                Channels = f.AudioChannels?.ToString(),
                AudioSampleRate = f.Asr,
                TotalBitrate = f.Tbr?.ToString(),
                VideoBitrate = f.Vbr?.ToString(),
                AudioBitrate = f.Abr?.ToString(),
                VideoCodec = f.Vcodec == "none" ? null : f.Vcodec,
                AudioCodec = f.Acodec == "none" ? null : f.Acodec,
                Protocol = f.Protocol,
                Language = f.Language,
                FileSize = f.Filesize.ToString(),
                FileSizeApprox = f.FilesizeApprox ?? f.Filesize,
                Note = f.FormatNote,
                MoreInfo = f.FormatNote
            });
        }
        return result;
    }
}
