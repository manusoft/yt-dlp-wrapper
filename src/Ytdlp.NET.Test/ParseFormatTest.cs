using System.Text.RegularExpressions;
using YtdlpDotNet;

namespace Ytdlp.NET.Test;

public class ParseFormatTest
{
    [Fact]
    public void ParseFormats_EmptyOutput_ReturnsEmptyList()
    {
        var formats = ParseFormats("");
        Assert.Empty(formats);
    }

    [Fact]
    public void ParseFormats_NoFormatsSection_SkipsParsing()
    {
        var output = "[youtube] Extracting URL: https://www.youtube.com/watch?v=Gk0WHyRUcgM\nNo formats available";
        var formats = ParseFormats(output);
        Assert.Empty(formats);
    }

    [Fact]
    public void ParseFormats_ParsesSampleOutput()
    {
        var output = TestConstants.GetAvailableFormats();        
        var formats = ParseFormats(output);
        Assert.Equal(40, formats.Count);
        Assert.Contains(formats, f => f.ID == "233" && f.VCodec == "audio only" && f.ACodec == "unknown");
        Assert.Contains(formats, f => f.ID == "sb3" && f.VCodec == "images" && f.ACodec == null && f.MoreInfo == "storyboard");
        Assert.Contains(formats, f => f.ID == "249-drc" && f.Resolution == "audio only" && f.Channels == "2" && f.FileSize == "1.64MiB" && f.ACodec == "opus");
        Assert.Contains(formats, f => f.ID == "18" && f.Resolution == "640x360" && f.Channels == "2" && f.VCodec == "avc1.42001E" && f.ACodec == "mp4a.40.2");
    }

    public List<VideoFormat> ParseFormats(string result)
    {
        var formats = new List<VideoFormat>();
        if (string.IsNullOrWhiteSpace(result))
        {
            return formats;
        }

        var lines = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        bool isFormatSection = false;

        foreach (var line in lines)
        {
            // Detect format section start
            if (line.Contains("[info] Available formats"))
            {
                isFormatSection = true;
                continue;
            }

            // Skip header or separator lines
            if (!isFormatSection || line.Contains("RESOLUTION") || line.StartsWith("---"))
            {
                continue;
            }

            // Skip empty or invalid lines (basic check for format line structure)
            if (!Regex.IsMatch(line, @"^[^\s]+\s+[^\s]+"))
            {
                break;
            }

            // Split line by whitespace, preserving structure
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var format = new VideoFormat();
            int index = 0;

            try
            {
                // Parse ID
                format.ID = parts[index++];

                // Check for duplicate ID
                if (formats.Any(f => f.ID == format.ID))
                {
                    continue;
                }

                // Parse Extension
                format.Extension = parts[index++];

                // Parse Resolution (may include "audio only")
                if (index < parts.Length && parts[index] == "audio" && index + 1 < parts.Length && parts[index + 1] == "only")
                {
                    format.Resolution = "audio only";
                    index += 2;
                }
                else if (index < parts.Length)
                {
                    format.Resolution = parts[index++];
                }
                else
                {
                    continue;
                }

                // Parse FPS (empty for audio-only formats)
                if (format.Resolution != "audio only" && index < parts.Length && Regex.IsMatch(parts[index], @"^\d+$"))
                {
                    format.FPS = parts[index++];
                }

                // Parse Channels (marked by '|' or number)
                if (index < parts.Length && (Regex.IsMatch(parts[index], @"^\d+\|$") || Regex.IsMatch(parts[index], @"^\d+$")))
                {
                    format.Channels = parts[index].TrimEnd('|');
                    index++;
                }

                // Skip first '|' if present
                if (index < parts.Length && parts[index] == "|")
                {
                    index++;
                }

                // Parse FileSize
                if (index < parts.Length && (Regex.IsMatch(parts[index], @"^~?\d+\.\d+MiB$") || parts[index] == ""))
                {
                    format.FileSize = parts[index] == "" ? null : parts[index];
                    index++;
                }

                // Parse TBR
                if (index < parts.Length && Regex.IsMatch(parts[index], @"^\d+k$"))
                {
                    format.TBR = parts[index];
                    index++;
                }

                // Parse Protocol
                if (index < parts.Length && (parts[index] == "https" || parts[index] == "m3u8" || parts[index] == "mhtml"))
                {
                    format.Protocol = parts[index];
                    index++;
                }

                // Skip second '|' if present
                if (index < parts.Length && parts[index] == "|")
                {
                    index++;
                }

                // Parse VCodec
                if (index < parts.Length)
                {
                    if (parts[index] == "audio" && index + 1 < parts.Length && parts[index + 1] == "only")
                    {
                        format.VCodec = "audio only";
                        index += 2;
                    }
                    else if (parts[index] == "images")
                    {
                        format.VCodec = "images";
                        index++;
                    }
                    else if (Regex.IsMatch(parts[index], @"^[a-zA-Z0-9\.]+$"))
                    {
                        format.VCodec = parts[index];
                        index++;
                    }
                }

                // Parse VBR
                if (index < parts.Length && Regex.IsMatch(parts[index], @"^\d+k$"))
                {
                    format.VBR = parts[index];
                    index++;
                }

                // Parse ACodec
                if (index < parts.Length && (Regex.IsMatch(parts[index], @"^[a-zA-Z0-9\.]+$") || parts[index] == "unknown"))
                {
                    format.ACodec = parts[index];
                    index++;
                }

                // Parse ABR
                if (index < parts.Length && Regex.IsMatch(parts[index], @"^\d+k$"))
                {
                    format.ABR = parts[index];
                    index++;
                }

                // Parse ASR
                if (index < parts.Length && Regex.IsMatch(parts[index], @"^\d+k$"))
                {
                    format.ASR = parts[index];
                    index++;
                }

                // Parse MoreInfo (remaining parts)
                if (index < parts.Length)
                {
                    format.MoreInfo = string.Join(" ", parts.Skip(index)).Trim();
                    // Clean up MoreInfo to remove redundant parts
                    if (format.MoreInfo.StartsWith("|"))
                    {
                        format.MoreInfo = format.MoreInfo.Substring(1).Trim();
                    }
                    // For storyboards, ensure MoreInfo is 'storyboard' and ACodec is null
                    if (format.VCodec == "images" && format.MoreInfo != "storyboard")
                    {
                        format.ACodec = null;
                        format.MoreInfo = "storyboard";
                    }
                }

                formats.Add(format);
            }
            catch (Exception ex)
            {
                continue;
            }
        }

        return formats;
    }
}