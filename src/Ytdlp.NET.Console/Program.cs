using YtdlpDotNet;

// Initialize Ytdlp with a custom logger
var logger = new ConsoleLogger();
var ytdlp = new Ytdlp($"tools\\yt-dlp.exe", logger);
string version = await ytdlp.GetVersionAsync();
Console.Clear();
Console.WriteLine($"Version: {version}");

// Subscribe to events for progress and error handling
ytdlp.OnProgress += message => Console.WriteLine($"Progress: {message}");
ytdlp.OnError += error => Console.WriteLine($"Error: {error}");
ytdlp.OnCommandCompleted += (success, message) => Console.WriteLine($"Completed: {message} (Success: {success})");
ytdlp.OnOutputMessage += (sender, message) => Console.WriteLine($"Output: {message}");
ytdlp.OnProgressMessage += (sender, message) => Console.WriteLine($"Progress Message: {message}");
ytdlp.OnProgressDownload += (sender, e) => Console.WriteLine($"Download Progress: {e.Message}");
ytdlp.OnCompleteDownload += (sender, message) => Console.WriteLine($"Download Complete: {message}");
ytdlp.OnErrorMessage += (sender, message) => Console.WriteLine($"Error Message: {message}");

var videoUrl = "https://www.youtube.com/watch?v=JOIqPThxFb8";

// Example 1: Download a video with specific options
//Console.WriteLine("=== Example 1: Downloading a video ===");
//try
//{
//    await ytdlp
//        .SetFormat("bestvideo+bestaudio")
//        .SetOutputFolder("./downloads")
//        .EmbedThumbnail()
//        .EmbedMetadata()
//        .SetDownloadRate("1M")
//        .ExecuteAsync(videoUrl);

//    Console.WriteLine($"Command Preview: {ytdlp.PreviewCommand()}");
//}
//catch (YtdlpException ex)
//{
//    Console.WriteLine($"Failed to download video: {ex.Message}");
//}

// Example 2: Extract audio from a video
//Console.WriteLine("\n=== Example 2: Extracting audio ===");
//try
//{
//    await ytdlp
//        .ExtractAudio("mp3")
//        .SetOutputTemplate("%(title)s.%(ext)s")
//        .DownloadSubtitles("en")
//        .ExecuteAsync(videoUrl);
//}
//catch (YtdlpException ex)
//{
//    Console.WriteLine($"Failed to extract audio: {ex.Message}");
//}

// Example 3: Get available formats for a video
//Console.WriteLine("=== Retrieving Available Formats ===");
//try
//{
//    var formats = await ytdlp.GetAvailableFormatsAsync(videoUrl);
//    Console.WriteLine($"Parsed {formats.Count} formats:");
//    foreach (var format in formats)
//    {
//        Console.WriteLine($"ID: {format.ID}, Ext: {format.Extension}, Resolution: {format.Resolution}, " +
//                          $"FPS: {format.FPS ?? "N/A"}, Channels: {format.Channels ?? "N/A"}, " +
//                          $"FileSize: {format.FileSize ?? "N/A"}, TBR: {format.TBR ?? "N/A"}, " +
//                          $"Protocol: {format.Protocol ?? "N/A"}, VCodec: {format.VCodec ?? "N/A"}, " +
//                          $"VBR: {format.VBR ?? "N/A"}, ACodec: {format.ACodec ?? "N/A"}, " +
//                          $"ABR: {format.ABR ?? "N/A"}, ASR: {format.ASR ?? "N/A"}, " +
//                          $"MoreInfo: {format.MoreInfo ?? "N/A"}");
//    }
//}
//catch (YtdlpException ex)
//{
//    Console.WriteLine($"Failed to retrieve formats: {ex.Message}");
//}

// Example 4: Batch download multiple videos
//Console.WriteLine("\n=== Example 4: Batch downloading ===");
//var videoUrls = new List<string>
//        {
//            "https://www.youtube.com/watch?v=RGg-Qx1rL9U",
//            "https://www.youtube.com/watch?v=iBVxogg5QwE"
//        };
//try
//{
//    await ytdlp
//        .SetFormat("b")
//        .SetOutputFolder("./batch_downloads")
//        .ExecuteBatchAsync(videoUrls);
//}
//catch (YtdlpException ex)
//{
//    Console.WriteLine($"Failed to batch download: {ex.Message}");
//}


var metadata = await ytdlp.GetVideoMetadataJsonAsync(videoUrl);
Console.WriteLine($"Title: {metadata?.Title}, Duration: {metadata?.Formats?.Count}");

// Custom logger implementation for demonstration
public class ConsoleLogger : ILogger
{
    public void Log(LogType type, string message)
    {
        Console.WriteLine($"[{type}] {message}");
    }
}