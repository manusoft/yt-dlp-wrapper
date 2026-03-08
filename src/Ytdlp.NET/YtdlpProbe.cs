using ManuHub.Ytdlp.Models;
using System.Text;
using System.Text.Json;

namespace ManuHub.Ytdlp;

/// <summary>
/// Static probe utilities for yt-dlp metadata and format information (no actual download).
/// Uses yt-dlp --dump-json, --list-formats, etc., and captures output in memory.
/// </summary>
public static class YtdlpProbe
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Fetches full video metadata (including formats list) using --dump-json.
    /// Returns null if parsing fails or yt-dlp errors.
    /// </summary>
    public static async Task<Metadata?> GetVideoMetadataAsync(string url, YtdlpBuilder? baseBuilder = null, CancellationToken ct = default)
    {
        var builder = (baseBuilder ?? Ytdlp.Create())
            .AddFlag("--dump-json")
            .AddFlag("--no-download")
            .AddFlag("--quiet")
            .AddFlag("--no-warnings")
            .AddFlag("--no-playlist"); // single video only

        var command = builder.Build();

        string rawJson = await RunProbeAsync(command, url, ct);

        if (string.IsNullOrWhiteSpace(rawJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Metadata>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            return null; // or throw new YtdlpException("Failed to parse metadata JSON", ex);
        }
    }

    /// <summary>
    /// Gets the list of available formats (parsed from --list-formats output).
    /// This is text-based parsing (not JSON), suitable for quick format listing.
    /// </summary>
    public static async Task<List<Format>> GetAvailableFormatsAsync(string url, YtdlpBuilder? baseBuilder = null, CancellationToken ct = default)
    {
        var builder = (baseBuilder ?? Ytdlp.Create())
            .AddFlag("--list-formats")
            .AddFlag("--no-download")
            .AddFlag("--quiet");

        var command = builder.Build();
        string output = await RunProbeAsync(command, url, ct);

        var formats = new List<Format>();
        bool inFormatTable = false;

        using var reader = new StringReader(output);
        string? line;

        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            if (line.Contains("format code") || line.StartsWith("[info] Available"))
            {
                inFormatTable = true;
                continue;
            }

            if (inFormatTable && line.Contains("extension"))
                continue; // header

            if (inFormatTable && !string.IsNullOrWhiteSpace(line))
            {
                // Rough parsing of format line (can be improved with regex)
                var parts = System.Text.RegularExpressions.Regex.Split(line.Trim(), @"\s+");
                if (parts.Length < 3) continue;

                var format = new Format
                {
                    FormatId = parts[0],
                    Extension = parts[1],
                    FormatNote = string.Join(" ", parts, 2, parts.Length - 2)
                };

                // Try to extract resolution/fps from note if possible
                if (format.FormatNote.Contains("p"))
                {
                    var resMatch = System.Text.RegularExpressions.Regex.Match(format.FormatNote, @"(\d+)p");
                    if (resMatch.Success && int.TryParse(resMatch.Groups[1].Value, out int h))
                    {
                        format.Height = h;
                    }
                }

                formats.Add(format);
            }
        }

        return formats;
    }


    /// <summary>
    /// Gets the full video metadata as a parsed object (requires Metadata class).
    /// Falls back to raw JSON string if parsing fails or Metadata not available.
    /// </summary>
    public static async Task<object> GetVideoMetadataRawAsync(string url, YtdlpBuilder? configBuilder = null, CancellationToken ct = default)
    {
        var builder = configBuilder ?? Ytdlp.Create();
        builder = builder
            .AddFlag("--dump-json")
            .AddFlag("--no-download")
            .AddFlag("--quiet")
            .AddFlag("--no-warnings");

        var command = builder.Build();

        string jsonOutput = await RunProbeAsync(command, url, ct);

        try
        {
            // Assuming you have a Metadata class from v2
            var metadata = JsonSerializer.Deserialize<Metadata>(jsonOutput);
            if (metadata != null) return metadata;
        }
        catch (JsonException)
        {
            // Fallback if no Metadata class or parse fails
        }

        return jsonOutput; // raw JSON as string fallback
    }

    /// <summary>
    /// Gets detailed list of available formats (parsed into Format objects or raw lines).
    /// </summary>
    public static async Task<List<object>> GetFormatsRawAsync(string url, YtdlpBuilder? configBuilder = null, CancellationToken ct = default)
    {
        var builder = configBuilder ?? Ytdlp.Create();
        builder = builder
            .AddFlag("--list-formats")
            .AddFlag("--no-download")
            .AddFlag("--quiet");

        var command = builder.Build();

        string output = await RunProbeAsync(command, url, ct);

        var formats = new List<object>();

        // Simple line-by-line parsing (yt-dlp --list-formats output is tabular text)
        using var reader = new StringReader(output);
        string? line;
        bool inTable = false;

        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith("[info] Available") || line.Contains("format code"))
            {
                inTable = true;
                continue;
            }

            if (inTable && line.Contains("extension"))
            {
                // Header line - skip
                continue;
            }

            if (inTable)
            {
                // Try to parse format line (rough, can be improved with regex)
                // Example: 137          mp4        1920x1080  1080p  4416k , avc1.640028, 30fps (best)
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    formats.Add(new
                    {
                        FormatId = parts[0],
                        Extension = parts[1],
                        Resolution = parts.Length > 2 ? parts[2] : null,
                        Note = string.Join(" ", parts, 3, parts.Length - 3)
                    });
                }
                else
                {
                    formats.Add(line); // fallback raw
                }
            }
        }

        return formats;
    }

    /// <summary>
    /// Gets the format ID of the best audio-only format.
    /// </summary>
    public static async Task<string?> GetBestAudioFormatIdAsync(
        string url,
        YtdlpBuilder? baseBuilder = null,
        CancellationToken ct = default)
    {
        var builder = (baseBuilder ?? Ytdlp.Create())
            .AddOption("--print", "%(bestaudio/format_id)s")
            .AddFlag("--no-download")
            .AddFlag("--quiet")
            .AddFlag("--no-warnings");

        var command = builder.Build();
        string output = await RunProbeAsync(command, url, ct);

        return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
    }

    /// <summary>
    /// Gets the format ID of the best video format, optionally capped by max height.
    /// </summary>
    public static async Task<string?> GetBestVideoFormatIdAsync(
        string url,
        int? maxHeight = null,
        YtdlpBuilder? baseBuilder = null,
        CancellationToken ct = default)
    {
        string selector = "bestvideo";
        if (maxHeight.HasValue)
            selector += $"[height<=?{maxHeight.Value}]";

        selector += "/best";

        var builder = (baseBuilder ?? Ytdlp.Create())
            .WithFormat(selector)
            .AddOption("--print", "%(format_id)s")
            .AddFlag("--no-download")
            .AddFlag("--quiet");

        var command = builder.Build();
        string output = await RunProbeAsync(command, url, ct);

        return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
    }

    // ──────────────────────────────────────────────
    // Internal helper: Run yt-dlp probe and capture stdout
    // ──────────────────────────────────────────────

    private static async Task<string> RunProbeAsync(YtdlpCommand command, string url, CancellationToken ct)
    {
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        void OnOutput(object? s, string line) => outputBuilder.AppendLine(line);
        void OnError(object? s, string line) => errorBuilder.AppendLine(line);

        command.OutputReceived += OnOutput;
        command.ErrorReceived += OnError;

        try
        {
            await command.ExecuteAsync(url, ct);
            string result = outputBuilder.ToString().Trim();

            if (errorBuilder.Length > 0)
            {
                throw new YtdlpException($"Probe error: {errorBuilder}");
            }

            return result;
        }
        finally
        {
            command.OutputReceived -= OnOutput;
            command.ErrorReceived -= OnError;
        }
    }
}
