namespace YtdlpNetConsoleApp.Helpers;

internal static class MenuRenderer
{
    public static string ShowMainMenu()
    {
        WriteItem("1", "System Information");
        WriteItem("2", "Metadata Explorer");
        WriteItem("3", "Format Inspector");
        WriteItem("4", "Download Video");
        WriteItem("5", "Extract Audio");
        WriteItem("6", "Batch Download");
        WriteItem("7", "SponsorBlock");
        WriteItem("8", "Benchmarks");
        WriteItem("9", "Extractors List");
        WriteItem("10", "Subtitle List");
        WriteItem("11", "Live Stream Download");
        WriteItem("0", "Exit");

        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Select Option > ");
        Console.ResetColor();

        return Console.ReadLine() ?? "0";
    }

    private static void WriteItem(string key, string text)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"[{key}] ");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(text);

        Console.ResetColor();
    }
}
