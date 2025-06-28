using YtDlpWrapper;

Console.Clear();

string outputDirectory = @"C:\Downloads";

// Initialize the YtDlpEngine
//var ytDlpEngine = new YtDlpEngine();

// Subscribe to the download progress event
//ytDlpEngine.OnProgressMessage += (sender, e) => Console.WriteLine($"{e}");

// Subscribe to the download progress event
//ytDlpEngine.OnErrorMessage += (sender, e) => Console.WriteLine($"{e}");

// Subscribe to the download progress event
//ytDlpEngine.OnProgressDownload += (sender, e) => Console.WriteLine($"Downloading: {e.Percent}% of {e.Size} ETA:{e.ETA}");

// Input video URL and output directory
//Console.Write("Enter the video URL: ");
//var videoUrl = Console.ReadLine();

//if (string.IsNullOrEmpty(videoUrl))
//{
//    Console.WriteLine("Invalid URL");
//    return;
//}

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
//await ytDlpEngine.DownloadVideoAsync(videoUrl, outputDirectory, VideoQuality.Worst);

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

var ytdlp = new Ytdlp($"{AppContext.BaseDirectory}\\Tools\\yt-dlp.exe");

// Subscribe to the detailed progress event
//ytdlp.OnDetailedProgress += progress =>
//{
//    Console.WriteLine($"Progress: {progress.Percentage}%");
//    Console.WriteLine($"Downloaded: {progress.DownloadedSize}");
//    Console.WriteLine($"Speed: {progress.Speed}");
//    Console.WriteLine($"Time Left: {progress.TimeLeft}");
//};

// Subscribe to the error event
ytdlp.OnProgressMessage += (sender, e) => Console.WriteLine($"{e}");

// Subscribe to the download progress event
ytdlp.OnErrorMessage += (sender, e) => Console.WriteLine($"{e}");

// Subscribe to the download progress event
ytdlp.OnProgressDownload += (sender, e) => Console.WriteLine($"Downloading: {e.Percent}% of {e.Size} ETA:{e.ETA}");


//ytdlp.OnProgressDownload += (sender, e) =>
//{
//    Console.WriteLine($"{e.Percent} {e.Size} {e.Speed} {e.ETA} {e.Fragments}");
//};

// Configure the command (e.g., set output template, show progress)

//var fortmats = await ytdlp.GetAvailableFormatsAsync(videoUrl);

//foreach (var format in fortmats)
//{
//    Console.WriteLine($"{format.ID} {format.Type} {format.Resolution} {format.FPS}");
//}

// Input video URL and output directory
Console.Write("Enter the video URL: ");
var videoUrl = Console.ReadLine();

if (string.IsNullOrEmpty(videoUrl))
{
    Console.WriteLine("Invalid URL");
    return;
}

await ytdlp.SetOutputFolder(outputDirectory)
           //.Version()
           .ExecuteAsync(videoUrl);
