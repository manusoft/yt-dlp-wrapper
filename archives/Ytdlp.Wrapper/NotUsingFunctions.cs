using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace YtDlpWrapper;

internal class NotUsingFunctions
{
    private readonly string ytDlpExecutable;

    private async Task<string> DownloadChannel(string channelUrl, string outputDirectory)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ytDlpExecutable,
                Arguments = $"-o {outputDirectory}/%(uploader)s/%(title)s.%(ext)s {channelUrl}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        process.WaitForExit();
        return output;
    }

    private async Task<string> DownloadAllChannel(string channelUrl, string outputDirectory)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ytDlpExecutable,
                Arguments = $"-o {outputDirectory}/%(uploader)s/%(title)s.%(ext)s --write-sub --sub-lang en --write-thumbnail --extract-audio --audio-format mp3 {channelUrl}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        process.WaitForExit();
        return output;
    }

    // Get playlist info
    private async Task<List<VideoInfo>> GetPlaylistInfoAsync(string playlistUrl)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ytDlpExecutable,
                Arguments = $"--dump-json {playlistUrl}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        process.WaitForExit();

        var jsonObjects = output.Split(new[] { '}' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(json => json + "}")
                        .ToList();

        var videoInfos = new List<VideoInfo>();
        foreach (var json in jsonObjects)
        {
            var videoInfo = JsonSerializer.Deserialize<VideoInfo>(json);
            videoInfos.Add(videoInfo);
        }

        return videoInfos;
        //var videoInfo = JsonSerializer.Deserialize<VideoInfo>(output);
        //return new List<VideoInfo> { videoInfo }; // Wrap it in a list if the method expects a list.

    }

    private async Task<string> GetChannelInfoAsync(string channelUrl, string outputDirectory)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ytDlpExecutable,
                Arguments = $"--dump-json -o {outputDirectory}/%(uploader)s/%(title)s.%(ext)s {channelUrl}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        process.WaitForExit();
        return output;
    }

    private async Task<string> GetAudioAsync(string videoUrl, string outputDirectory)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ytDlpExecutable,
                Arguments = $"-o {outputDirectory}/%(title)s.%(ext)s --extract-audio --audio-format mp3 --skip-download {videoUrl}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        process.WaitForExit();
        return output;
    }
}
