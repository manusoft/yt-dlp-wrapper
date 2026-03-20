using ManuHub.Ytdlp.Core;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ManuHub.Ytdlp;

public sealed class YtdlpGeneral
{
    private readonly string? _path;
    private readonly ILogger? _logger;

    internal YtdlpGeneral(string? path, ILogger? logger)
    {
        _path = path;
        _logger = logger;
    }

    public async Task<string> VersionAsync(CancellationToken ct = default)
    {
        var result = await RunAsync("--version", ct);

        if (result.ExitCode != 0)
            throw new YtdlpException($"Failed to get yt-dlp version. {result.Error}");

        return string.IsNullOrWhiteSpace(result.Output)
            ? result.Error
            : result.Output;
    }

    public async Task<string> UpdateAsync(UpdateChannel channel = UpdateChannel.Stable, CancellationToken ct = default)
    {
        var args = $"--update-to {channel.ToString().ToLowerInvariant()}";

        var result = await RunAsync(args, ct);

        if (result.ExitCode != 0)
            throw new YtdlpException($"yt-dlp update failed. {result.Error}");

        var text = result.Output + result.Error;

        var versionMatch = Regex.Match(text, @"Updated yt-dlp to version\s+([\d.]+)");
        if (versionMatch.Success)
            return versionMatch.Groups[1].Value;

        return "Updated (version not parsed)";
    }

    private async Task<(int ExitCode, string Output, string Error)> RunAsync(string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _path ?? "yt-dlp",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) errorBuilder.AppendLine(e.Data);
        };

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        return (
            process.ExitCode,
            outputBuilder.ToString().Trim(),
            errorBuilder.ToString().Trim()
        );
    }
}