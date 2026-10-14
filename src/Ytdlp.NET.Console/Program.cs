using ManuHub.Ytdlp.NET;
using System.Text;
using YtdlpNetConsoleApp.Helpers;
using YtdlpNetConsoleApp.Pages;

namespace YtdlpNetConsoleApp;

internal class Program
{
    private static Ytdlp? _ytdlp;

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Console.Title = "Ytdlp.NET Interactive Demo";

        try
        {
            ConsoleTheme.ShowSplash();

            await InitializeAsync();

            while (true)
            {
                Console.Clear();

                ConsoleTheme.WriteBanner();

                var choice = MenuRenderer.ShowMainMenu();

                switch (choice)
                {
                    case "1":
                        await SystemInfoPage.ShowAsync(_ytdlp!);
                        break;

                    case "2":
                        await MetadataPage.ShowAsync(_ytdlp!);
                        break;

                    case "3":
                        await FormatsPage.ShowAsync(_ytdlp!);
                        break;

                    case "4":
                        await DownloadVideoPage.ShowAsync(_ytdlp!);
                        break;

                    case "5":
                        await DownloadAudioPage.ShowAsync(_ytdlp!);
                        break;

                    case "6":
                        await BatchDownloadPage.ShowAsync(_ytdlp!);
                        break;

                    case "7":
                        await SponsorBlockPage.ShowAsync(_ytdlp!);
                        break;

                    case "8":
                        await BenchmarkPage.ShowAsync(_ytdlp!);
                        break;

                    case "9":
                        await ExtractorPage.ShowAsync(_ytdlp!);
                        break;

                    case "10":
                        await SubtitlePage.ShowAsync(_ytdlp!);
                        break;

                    case "11":
                        await LivetreamDownloadPage.ShowAsync(_ytdlp!);
                        break;

                    case "0":
                        return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.ResetColor();

            Console.ReadKey();
        }
    }

    private static async Task InitializeAsync()
    {
        ConsoleTheme.WriteSection("Initializing Environment");

        var toolsPath = Path.Combine(AppContext.BaseDirectory, "tools");

        var checks = new List<(string Name, string Path, bool Required)>
        {
            ("yt-dlp",  Path.Combine(toolsPath, "yt-dlp.exe"),  true),
            ("ffmpeg",  Path.Combine(toolsPath, "ffmpeg.exe"),  true),
            ("ffprobe", Path.Combine(toolsPath, "ffprobe.exe"), false),
            ("deno",    Path.Combine(toolsPath, "deno.exe"),    false),
        };

        Console.WriteLine("Checking dependencies...\n");

        bool allRequiredOk = true;

        foreach (var (name, path, required) in checks)
        {
            bool exists = File.Exists(path);

            if (exists)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✔ {name,-10} OK");
            }
            else
            {
                if (required)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✖ {name,-10} MISSING (REQUIRED)");
                    allRequiredOk = false;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠ {name,-10} missing (optional)");
                }
            }

            Console.ResetColor();
        }

        Console.WriteLine();

        if (!allRequiredOk)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Fatal: Required dependencies are missing.");
            Console.WriteLine("Place missing files inside /tools folder and restart.");
            Console.ResetColor();

            Console.ReadKey();
            Environment.Exit(1);
            return;
        }

        // Initialize Ytdlp only if dependencies are OK
        _ytdlp = new Ytdlp(ytdlpPath: Path.Combine(toolsPath, "yt-dlp.exe"), logger: new DemoLogger())
            .WithFFmpegLocation(toolsPath);

        Console.WriteLine("Checking yt-dlp version...\n");

        try
        {
            var version = await _ytdlp.VersionAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✔ yt-dlp version: {version}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✖ Failed to execute yt-dlp");
            Console.WriteLine(ex.Message);
            Console.ResetColor();

            Console.ReadKey();
            Environment.Exit(1);
            return;
        }

        // Ensure folders exist
        var folders = new[]
        {
            "downloads",
            "downloads/audio",
            "downloads/batch",
            "downloads/sponsorblock",
            "downloads/temp"
        };

        Console.WriteLine("\nChecking folders...");

        foreach (var folder in folders)
        {
            Directory.CreateDirectory(folder);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✔ {folder}");
            Console.ResetColor();
        }

        Console.WriteLine("\nEnvironment ready.\n");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Press any key to continue...");
        Console.ResetColor();

        Console.ReadKey();
    }
}
