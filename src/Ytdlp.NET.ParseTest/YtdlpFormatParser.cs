namespace Ytdlp.NET.ParseTest;

using System.Text.RegularExpressions;

public static class YtdlpFormatParser
{
    public static List<YtdlpFormat> Parse(string text)
    {
        var list = new List<YtdlpFormat>();

        var lines = text.Split('\n');

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("[info]") ||
                line.StartsWith("ID ") ||
                line.StartsWith("─"))
                continue;

            var parts = line.Split('│');
            if (parts.Length < 3)
                continue;

            var left = Normalize(parts[0]);
            var mid = Normalize(parts[1]);
            var right = Normalize(parts[2]);

            var format = new YtdlpFormat();

            ParseLeft(left, format);
            ParseMiddle(mid, format);
            ParseRight(right, format);

            list.Add(format);
        }

        return list;
    }

    static void ParseLeft(string left, YtdlpFormat f)
    {
        var p = left.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (p.Length < 2) return;

        f.FormatId = p[0];
        f.Ext = p[1];

        // audio only special case
        if (p.Length > 3 && p[2] == "audio" && p[3] == "only")
        {
            f.Resolution = "audio only";

            if (p.Length > 4 && int.TryParse(p[4], out var ch))
                f.Channels = ch;

            return;
        }

        if (p.Length > 2)
            f.Resolution = p[2];

        if (p.Length > 3 && int.TryParse(p[3], out var fps))
            f.Fps = fps;

        if (p.Length > 4 && int.TryParse(p[4], out var ch2))
            f.Channels = ch2;
    }

    static void ParseMiddle(string mid, YtdlpFormat f)
    {
        var p = mid.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (p.Length == 0) return;

        // storyboard rows have only protocol
        if (p.Length == 1)
        {
            f.Protocol = p[0];
            return;
        }

        f.FileSize = p.ElementAtOrDefault(0);
        f.Tbr = p.ElementAtOrDefault(1);
        f.Protocol = p.ElementAtOrDefault(2);
    }

    static void ParseRight(string right, YtdlpFormat f)
    {
        var p = right.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (p.Length == 0) return;

        int i = 0;

        // Storyboard
        if (p[0] == "images")
        {
            f.VCodec = "images";
            f.MoreInfo = string.Join(" ", p.Skip(1));
            return;
        }

        // Audio-only rows
        if (p[0] == "audio" && p.Length > 1 && p[1] == "only")
        {
            f.VCodec = "audio only";
            i = 2;

            if (i < p.Length) f.ACodec = p[i++];
            if (i < p.Length && p[i].EndsWith("k")) f.Abr = p[i++];
            if (i < p.Length && p[i].EndsWith("k")) f.Asr = p[i++];

            if (i < p.Length)
                f.MoreInfo = string.Join(" ", p.Skip(i));

            return;
        }

        // Video-only rows
        f.VCodec = p[i++];

        if (i < p.Length && p[i].EndsWith("k"))
            f.Vbr = p[i++];

        if (i < p.Length && p[i] == "video" && p.ElementAtOrDefault(i + 1) == "only")
        {
            f.ACodec = "video only";
            i += 2;

            if (i < p.Length)
                f.MoreInfo = string.Join(" ", p.Skip(i));

            return;
        }

        // Progressive rows (video + audio)
        if (i < p.Length)
            f.ACodec = p[i++];

        if (i < p.Length && p[i].EndsWith("k"))
            f.Abr = p[i++];

        if (i < p.Length && p[i].EndsWith("k"))
            f.Asr = p[i++];

        if (i < p.Length)
            f.MoreInfo = string.Join(" ", p.Skip(i));
    }

    static string Normalize(string s)
        => Regex.Replace(s.Trim(), @"\s+", " ");
}