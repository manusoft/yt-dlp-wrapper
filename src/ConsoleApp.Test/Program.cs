using YtDlpWrapper;
using static System.Net.Mime.MediaTypeNames;

string outputDirectory = @"C:\Downloads";

// Initialize the YtDlpEngine
var ytDlpEngine = new YtDlpEngine();

// Subscribe to the download progress event
ytDlpEngine.OnProgressMessage += (sender, e) => Console.WriteLine($"{e}");

// Subscribe to the download progress event
ytDlpEngine.OnErrorMessage += (sender, e) => Console.WriteLine($"{e}");

// Subscribe to the download progress event
ytDlpEngine.OnProgressDownload += (sender, e) => Console.WriteLine($"Downloading: {e.Percent}% of {e.Size} ETA:{e.ETA}");

// Input video URL and output directory
Console.Write("Enter the video URL: ");
var videoUrl = Console.ReadLine();

if (string.IsNullOrEmpty(videoUrl))
{
    Console.WriteLine("Invalid URL");
    return;
}

// Start downloading the video
//var videoInfo = await ytDlpEngine.GetVideoInfo(videoUrl);
//Console.WriteLine(videoInfos);

// Get available formats
//var formats = await ytDlpEngine.GetAvailableFormatsAsync(videoUrl);
//foreach (var format in formats)
//{
//    Console.WriteLine($"{format.ID} - {format.Type} -{format.Resolution} - {format.AdditionalInfo}");
//}

//Start downloading the video
await ytDlpEngine.DownloadVideoAsync(videoUrl, outputDirectory, VideoQuality.All);

// Start downloading the playlist
// await ytDlpEngine.DownloadPlaylistAsync(videoUrl, outputDirectory);

// Start downloading the audio
//await ytDlpEngine.DownloadAudioAsync(videoUrl, outputDirectory, AudioQuality.WorstAudio);

// Start downloading the subtitiles // not works
//await ytDlpEngine.DownloadSubtitlesAsync(videoUrl, outputDirectory);

// Start downloading the thumbnail
// await ytDlpEngine.DownloadThumbnailAsync(videoUrl, outputDirectory);

// Start downloading the all
//await ytDlpEngine.DownloadAllAsync(videoUrl, outputDirectory);

// Start downloading the all playlist
//await ytDlpEngine.DownloadAllPlaylistAsync(videoUrl, outputDirectory);

// Get thumbnail to app folder
//var thumnail = await ytDlpEngine.GetThumbnailAsync(videoUrl);
//Console.WriteLine(thumnail);
