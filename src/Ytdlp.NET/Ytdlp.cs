using System.Diagnostics;
using System.Text;

namespace ManuHub.Ytdlp;

/// <summary>
/// Static factory for the v3 fluent API
/// </summary>
public static class Ytdlp
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ytDlpPath"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static YtdlpBuilder Create(string? ytDlpPath = null, ILogger? logger = null) => new YtdlpBuilder(ytDlpPath, logger);

    /// <summary>
    /// Gets the current version of the yt-dlp executable.
    /// Returns the version string (e.g. "2025.03.10") or throws on failure.
    /// </summary>
    /// <param name="ytDlpPath">Optional custom path to yt-dlp executable (defaults to builder default)</param>
    /// <param name="ct">Cancellation token</param>
    public static async Task<string> VersionAsync(string? ytDlpPath = null, CancellationToken ct = default)
    {
        ytDlpPath ??= "yt-dlp"; // fallback

        var psi = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = "--version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        string output = outputBuilder.ToString().Trim();
        string error = errorBuilder.ToString().Trim();

        if (process.ExitCode != 0)
        {
            throw new YtdlpException($"Failed to get yt-dlp version. Exit code: {process.ExitCode}. Error: {error}");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            // Sometimes --version writes to stderr in older versions
            return error.Trim();
        }

        return output;
    }

    /// <summary>
    /// Updates yt-dlp to the latest version.
    /// Returns the new version string after update, or throws on failure.
    /// </summary>
    /// <param name="ytDlpPath">Optional custom path to yt-dlp executable</param>
    /// <param name="nightly">If true, installs the nightly build instead of stable</param>
    /// <param name="ct">Cancellation token</param>
    public static async Task<string> UpdateAsync(string? ytDlpPath = null, bool nightly = false, CancellationToken ct = default)
    {
        ytDlpPath ??= "yt-dlp";

        var args = nightly ? "--update-to nightly" : "--update";

        var psi = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        string output = outputBuilder.ToString().Trim();
        string error = errorBuilder.ToString().Trim();

        if (process.ExitCode != 0)
        {
            throw new YtdlpException($"yt-dlp update failed (exit code {process.ExitCode}). Output: {output}. Error: {error}");
        }

        // Parse the "Updated yt-dlp to version 2025.XX.XX" line
        var versionMatch = System.Text.RegularExpressions.Regex.Match(output + error, @"Updated yt-dlp to version\s+([\d.]+)");
        if (versionMatch.Success)
        {
            return versionMatch.Groups[1].Value;
        }

        // Fallback: sometimes it just says "yt-dlp is up to date" or prints version
        var versionLine = output.Split('\n').FirstOrDefault(l => l.Trim().StartsWith("yt-dlp") || l.Contains("."));
        return versionLine?.Trim() ?? "Updated (exact version not parsed)";
    }
}