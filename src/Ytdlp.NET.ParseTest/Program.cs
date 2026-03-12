using Ytdlp.NET.ParseTest;

Console.WriteLine("Ytdlp Formats Parse Test");

var text = TestStrings.Test2();

var formats = YtdlpFormatParser.Parse(text);

PrintFormats(formats);

static string S(object v) => v?.ToString() ?? "";

void PrintFormats(List<YtdlpFormat> formats)
{
    PrintHeader();

    foreach (var f in formats)
    {
        var color = GetColor(f);
        Console.ForegroundColor = color;

        Console.WriteLine(
            $"{S(f.FormatId),-5}{S(f.Ext),-6}{S(f.Resolution),-12}{S(f.Fps),-5}{S(f.Channels),-4} │ " +
            $"{S(f.FileSize),-10}{S(f.Tbr),-6}{S(f.Protocol),-6} │ " +
            $"{S(f.VCodec),-14}{S(f.Vbr),-6}{S(f.ACodec),-12}{S(f.Abr),-6}{S(f.Asr),-6} {S(f.MoreInfo)}"
        );

        Console.ResetColor();
    }
}

void PrintHeader()
{
    Console.ForegroundColor = ConsoleColor.White;

    Console.WriteLine(
        $"{"ID",-5}{"EXT",-6}{"RESOLUTION",-12}{"FPS",-5}{"CH",-4} │ " +
        $"{"FILESIZE",-10}{"TBR",-6}{"PROTO",-6} │ " +
        $"{"VCODEC",-14}{"VBR",-6}{"ACODEC",-12}{"ABR",-6}{"ASR",-6} MORE INFO");

    Console.ResetColor();

    Console.WriteLine(new string('─', 120));
}

ConsoleColor GetColor(YtdlpFormat f)
{
    if (f.VCodec == "images")
        return ConsoleColor.DarkGray;     // storyboard

    if (f.Resolution == "audio only")
        return ConsoleColor.Cyan;         // audio

    if (f.ACodec == "video only")
        return ConsoleColor.Green;        // video

    return ConsoleColor.Yellow;           // progressive
}