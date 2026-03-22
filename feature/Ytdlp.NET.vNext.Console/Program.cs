using ManuHub.Ytdlp;
using ManuHub.Ytdlp.Core;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("==== Ytdlp Interactive Console App ====");

        // Ask for yt-dlp executable path
        Console.Write("Enter path to yt-dlp executable (or leave empty to use default): ");
        string? ytDlpPath = Console.ReadLine();
        ytDlpPath = string.IsNullOrWhiteSpace(ytDlpPath) ? "tools\\yt-dlp.exe" : ytDlpPath.Trim();

        // Optional FFmpeg path for downloads
        Console.Write("Enter path to ffmpeg executable (for downloads) or leave empty: ");
        string? ffmpegPath = Console.ReadLine();
        ffmpegPath = string.IsNullOrWhiteSpace(ffmpegPath) ? "tools" : ffmpegPath.Trim();

        var root = Ytdlp.Create(ytDlpPath);

        while (true)
        {
            Console.WriteLine("\nSelect mode:");
            Console.WriteLine("1) General (version / update)");
            Console.WriteLine("2) Probe (formats / metadata)");
            Console.WriteLine("3) Download (single or batch)");
            Console.WriteLine("0) Exit");
            Console.Write("Choice: ");
            string? choice = Console.ReadLine();

            if (choice == "0") break;

            try
            {
                switch (choice)
                {
                    case "1":
                        await GeneralModeAsync(root);
                        break;
                    case "2":
                        await ProbeModeAsync(root);
                        break;
                    case "3":
                        await DownloadModeAsync(root, ffmpegPath);
                        break;
                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        Console.WriteLine("Goodbye!");
    }

    // ================= General =================
    static async Task GeneralModeAsync(YtdlpRootBuilder root)
    {
        var general = root.General();
        Console.WriteLine("\nGeneral Mode:");
        Console.WriteLine("1) Get yt-dlp version");
        Console.WriteLine("2) Update yt-dlp");
        Console.Write("Choice: ");
        string? gChoice = Console.ReadLine();

        switch (gChoice)
        {
            case "1":
                string version = await general.VersionAsync();
                Console.WriteLine($"yt-dlp version: {version}");
                break;
            case "2":
                Console.Write("Use nightly channel? (y/n): ");
                bool nightly = (Console.ReadLine()?.Trim().ToLower() == "y");
                string updatedVersion = await general.UpdateAsync(
                    nightly ? UpdateChannel.Nightly : UpdateChannel.Stable);
                Console.WriteLine($"yt-dlp updated to: {updatedVersion}");
                break;
            default:
                Console.WriteLine("Invalid choice.");
                break;
        }
    }

    // ================= Probe =================
    static async Task ProbeModeAsync(YtdlpRootBuilder root)
    {
        Console.Write("Enter video URL: ");
        string? url = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(url)) return;

        var probe = root.Probe(url.Trim());

        Console.WriteLine("\nProbe Mode:");
        Console.WriteLine("1) List available formats");
        Console.WriteLine("2) Get detailed formats (JSON)");
        Console.WriteLine("3) Get video metadata");
        Console.WriteLine("4) Get best audio format");
        Console.WriteLine("5) Get best video format");
        Console.WriteLine("6) Get file size approx");
        Console.Write("Choice: ");
        string? pChoice = Console.ReadLine();

        switch (pChoice)
        {
            case "1":
                var formats = await probe.GetAvailableFormatsAsync();
                Console.WriteLine("Available formats:");
                foreach (var f in formats)
                    Console.WriteLine($"- {f.Id} | {f.Extension} | {f.Resolution} | {f.Note}");
                break;

            case "2":
                var detailed = await probe.GetFormatsDetailedAsync();
                Console.WriteLine("Detailed formats:");
                foreach (var f in detailed)
                    Console.WriteLine($"- {f.Id} | {f.Extension} | {f.Resolution} | {f.VideoCodec}/{f.AudioCodec} | {f.FileSizeApprox} bytes");
                break;

            case "3":
                var metadata = await probe.GetVideoMetadataAsync();
                if (metadata != null)
                {
                    Console.WriteLine($"Title: {metadata.Title}");
                    Console.WriteLine($"Uploader: {metadata.Uploader}");
                    Console.WriteLine($"Duration: {metadata.Duration} sec");
                    Console.WriteLine($"View count: {metadata.ViewCount}");
                }
                else
                    Console.WriteLine("Failed to fetch metadata.");
                break;

            case "4":
                var bestAudioId = await probe.GetBestAudioFormatIdAsync();
                    Console.WriteLine($"BestAudioId: {bestAudioId}");
                break;

            case "5":
                var bestVideoId = await probe.GetBestVideoFormatIdAsync();
                    Console.WriteLine($"BestVideoId: {bestVideoId}");
                break;

            case "6":
                var fileSize = await probe.GetFileSizeAsync();
                    Console.WriteLine($"FileSize: {fileSize}");
                break;

            default:
                Console.WriteLine("Invalid choice.");
                break;
        }
    }


    // ================= Download =================
    static async Task DownloadModeAsync(YtdlpRootBuilder root, string? ffmpegPath)
    {
        Console.WriteLine("Enter video URLs (comma-separated for batch, or single URL): ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) return;

        var urls = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        bool isBatch = urls.Count > 1;

        Console.WriteLine("Choose format:");
        Console.WriteLine("1) bestvideo+bestaudio (default)");
        Console.WriteLine("2) bestaudio only");
        Console.WriteLine("3) Custom (yt-dlp format selector)");
        Console.Write("Choice: ");
        string? fChoice = Console.ReadLine();

        string format = fChoice switch
        {
            "2" => "bestaudio",
            "3" => AskCustomFormat(),
            _ => "bestvideo+bestaudio"
        };

        var downloadBuilder = root.Download()
            .WithFormat(format)
            .WithOutputFolder("Downloads");

        if (!string.IsNullOrWhiteSpace(ffmpegPath))
            downloadBuilder.WithFFmpegLocation(ffmpegPath);

        if (isBatch)
        {
            Console.WriteLine($"\nStarting batch download for {urls.Count} videos...\n");

            var command = downloadBuilder.Build();            

            // Subscribe to events
            command.OnProgressDownload += (s, e) =>
            {
                Console.WriteLine($"Progress: {e.Percent:F1}% - {e.Speed} - ETA: {e.ETA}");
            };
            command.OnCompleteDownload += (s, file) =>
            {
                Console.WriteLine($"Download completed: {file}");
            };
            command.OnErrorMessage += (s, msg) =>
            {
                Console.WriteLine($"Error: {msg}");
            };

            var results = await command.ExecuteBatchAsync(urls, maxConcurrency: 2);

            foreach (var r in results)
            {
                if (r.Error != null)
                    Console.WriteLine($"Failed: {r.Url} - {r.Error.Message}");
                else
                    Console.WriteLine($"Success: {r.Url}");
            }
        }
        else
        {
            string url = urls[0];
            var command = downloadBuilder.Build();

            // Subscribe to events
            command.OnProgressDownload += (s, e) =>
            {
                Console.WriteLine($"Progress: {e.Percent:F1}% - {e.Speed} - ETA: {e.ETA}");
            };
            command.OnCompleteDownload += (s, file) =>
            {
                Console.WriteLine($"Download completed: {file}");
            };
            command.OnErrorMessage += (s, msg) =>
            {
                Console.WriteLine($"Error: {msg}");
            };

            Console.WriteLine($"\nStarting download: {url}\n");
            await command.ExecuteAsync(url);
        }
    }

    static string AskCustomFormat()
    {
        Console.Write("Enter yt-dlp format selector (e.g., 'bestvideo[height<=1080]+bestaudio'): ");
        string? custom = Console.ReadLine();
        return string.IsNullOrWhiteSpace(custom) ? "bestvideo+bestaudio" : custom.Trim();
    }
}

//internal class Program
//{


//    static async Task Main(string[] args)
//    {
//         Replace with your local yt-dlp path if needed
//        string ytDlpPath = "tools\\yt-dlp.exe";

//        string videoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

//        var ytdlp = Ytdlp.Create(ytDlpPath);

//        Console.WriteLine("=== GENERAL MODE ===");
//        await TestGeneralAsync(ytdlp);

//        Console.WriteLine("\n=== PROBE MODE ===");
//        await TestProbeAsync(ytdlp, videoUrl);

//        Console.WriteLine("\n=== DOWNLOAD MODE ===");
//        await TestDownloadAsync(ytdlp, videoUrl);
//    }

//    static async Task TestGeneralAsync(YtdlpRootBuilder root)
//    {
//        try
//        {
//            var general = root.General();

//            string version = await general.VersionAsync();
//            Console.WriteLine($"yt-dlp Version: {version}");

//             Optional: update yt-dlp (will actually try to update)
//             string updatedVersion = await general.UpdateAsync();
//             Console.WriteLine($"yt-dlp Updated Version: {updatedVersion}");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"General mode error: {ex.Message}");
//        }
//    }

//    static async Task TestProbeAsync(YtdlpRootBuilder root, string url)
//    {
//        try
//        {
//            var probe = root.Probe(url);

//             Get all formats detailed
//            var formats = await probe.GetFormatsDetailedAsync();
//            Console.WriteLine("Available formats:");
//            foreach (var f in formats)
//            {
//                Console.WriteLine($"  {f.Id} | {f.Extension} | {f.Resolution} | {f.VideoCodec}/{f.AudioCodec}");
//            }

//             Get best audio and video format IDs
//            string? bestAudio = await probe.GetBestAudioFormatIdAsync();
//            string? bestVideo = await probe.GetBestVideoFormatIdAsync();

//            Console.WriteLine($"Best audio format ID: {bestAudio}");
//            Console.WriteLine($"Best video format ID: {bestVideo}");

//             Get file size
//            string? fileSize = await probe.GetFileSizeAsync();
//            Console.WriteLine($"Approx file size: {fileSize ?? "unknown"} bytes");

//             Get metadata
//            var metadata = await probe.GetVideoMetadataAsync();
//            Console.WriteLine($"Video title: {metadata?.Title}");
//            Console.WriteLine($"Uploader: {metadata?.Uploader}");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Probe mode error: {ex.Message}");
//        }
//    }

//    static async Task TestDownloadAsync(YtdlpRootBuilder root, string url)
//    {
//        try
//        {
//            var download = root.Download()
//                .WithFormat("bestvideo+bestaudio")
//                .WithFFmpegLocation(toolsPath)
//                .WithOutputFolder("Downloads");

//            var command = download.Build();

//             Subscribe to events
//            command.OnProgressDownload += (s, e) =>
//            {
//                Console.WriteLine($"Progress: {e.Percent:F1}% - {e.Speed} - ETA: {e.ETA}");
//            };
//            command.OnCompleteDownload += (s, file) =>
//            {
//                Console.WriteLine($"Download completed: {file}");
//            };
//            command.OnErrorMessage += (s, msg) =>
//            {
//                Console.WriteLine($"Error: {msg}");
//            };

//            await command.ExecuteAsync(url);
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Download mode error: {ex.Message}");
//        }
//    }
//}


