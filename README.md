![Static Badge](https://img.shields.io/badge/ytdlp.NET-red) ![NuGet Version](https://img.shields.io/nuget/v/ytdlp.net)  ![NuGet Downloads](https://img.shields.io/nuget/dt/ytdlp.net)

# Ytdlp.NET
![icon](https://github.com/user-attachments/assets/2147c398-4e0f-43e2-99cb-32b34be7dc2f)

### ClipMate - MAUI.NET App - [Download](https://apps.microsoft.com/detail/9NTP1DH4CQ4X?hl=en&gl=IN&ocid=pdpshare)
<img width="986" height="693" alt="image" src="https://github.com/user-attachments/assets/39e09415-fa0c-4991-976f-8966e9c50c5b" />



### Video Downloader - .NET App
![Screenshot 2025-01-23 153252](https://github.com/user-attachments/assets/1b977927-ea26-4220-bd41-9f64d6716058)

[Download the latest App](https://github.com/manusoft/yt-dlp-wrapper/releases/download/v1.0.0/gui-app.zip)


A .NET wrapper for the `yt-dlp` command-line tool, providing a fluent interface to build and execute commands for downloading videos, audio, subtitles, thumbnails, and more from YouTube and other supported platforms. `Ytdlp.NET` simplifies interaction with `yt-dlp` by offering a strongly-typed API, progress parsing, and event-based feedback for real-time monitoring.

## Features

- **Fluent Interface**: Build `yt-dlp` commands with a chainable, intuitive API.
- **Progress Tracking**: Parse and monitor download progress, errors, and completion events.
- **Batch Downloading**: Download multiple videos or playlists in a single operation.
- **Format Selection**: Easily select video/audio formats, resolutions, and other options.
- **Event-Driven**: Subscribe to events for progress, errors, and command completion.
- **Customizable**: Support for custom `yt-dlp` options and advanced configurations.
- **Cross-Platform**: Compatible with Windows, macOS, and Linux (requires `yt-dlp` installed).

# Ytdlp.NET API Documentation

**Namespace**: `YtdlpNET`  
**Main Class**: `Ytdlp` (fluent wrapper around yt-dlp)

The `Ytdlp` class provides a fluent, chainable API to build yt-dlp commands, fetch metadata, list formats, and execute downloads with rich event support and progress tracking.

## Constructor

```csharp
public Ytdlp(string ytDlpPath = "yt-dlp", ILogger? logger = null)
```

- **ytDlpPath**: Path to yt-dlp executable (default: searches PATH for "yt-dlp").
- **logger**: Optional logger (falls back to `DefaultLogger`).

**Throws**: `YtdlpException` if executable is not found.

## Events

All events are invoked on the UI/main thread if possible (when used in UI apps).

| Event                        | Type                                  | Description |
|------------------------------|---------------------------------------|-------------|
| `OnProgress`                 | `EventHandler<string>`                | General progress line from stdout |
| `OnError`                    | `EventHandler<string>`                | Error line from stderr |
| `OnCommandCompleted`         | `EventHandler<CommandCompletedEventArgs>` | Process finished (success/failure/cancel) |
| `OnOutputMessage`            | `EventHandler<string>`                | Every stdout line |
| `OnProgressDownload`         | `EventHandler<DownloadProgressEventArgs>` | Parsed download progress (% / speed / ETA) |
| `OnCompleteDownload`         | `EventHandler<string>`                | Single file download completed |
| `OnProgressMessage`          | `EventHandler<string>`                | Info messages (merging, extracting, etc.) |
| `OnErrorMessage`             | `EventHandler<string>`                | Error/info messages from parser |
| `OnPostProcessingComplete`   | `EventHandler<string>`                | Post-processing (merge, convert) finished |

## Core Methods

### Output & Path Configuration

```csharp
Ytdlp SetOutputFolder(string outputFolderPath)
Ytdlp SetTempFolder(string tempFolderPath)
Ytdlp SetHomeFolder(string homeFolderPath)
Ytdlp SetOutputTemplate(string template)
Ytdlp SetFFMpegLocation(string ffmpegFolder)
```

### Format Selection

```csharp
Ytdlp SetFormat(string format)
Ytdlp ExtractAudio(string audioFormat)
Ytdlp SetResolution(string resolution)
```

### Metadata & Format Fetching

```csharp
Task<string> GetVersionAsync(CancellationToken ct = default)
Task<Metadata?> GetVideoMetadataJsonAsync(string url, CancellationToken ct = default)
Task<List<Format>> GetFormatsDetailedAsync(string url, CancellationToken ct = default)
Task<string> GetBestAudioFormatIdAsync(string url, CancellationToken ct = default)
Task<string> GetBestVideoFormatIdAsync(string url, int maxHeight = 1080, CancellationToken ct = default)
```

### Download Execution

```csharp
string PreviewCommand()
Task ExecuteAsync(string url, CancellationToken ct = default, string? outputTemplate = null)
Task ExecuteBatchAsync(IEnumerable<string> urls, CancellationToken ct = default)
Task ExecuteBatchAsync(IEnumerable<string> urls, int maxConcurrency = 3, CancellationToken ct = default)
```

### Common Options (chainable)

```csharp
Ytdlp EmbedMetadata()
Ytdlp EmbedThumbnail()
Ytdlp DownloadThumbnails()
Ytdlp DownloadSubtitles(string languages = "all")
Ytdlp SetRetries(string retries)
Ytdlp SetDownloadRate(string rate)
Ytdlp UseProxy(string proxy)
Ytdlp Simulate()
Ytdlp SkipDownloaded()
Ytdlp SetKeepTempFiles(bool keep)
```

### Advanced / Specialized Options

```csharp
Ytdlp WithConcurrentFragments(int count)
Ytdlp RemoveSponsorBlock(params string[] categories)
Ytdlp EmbedSubtitles(string languages = "all", string? convertTo = null)
Ytdlp CookiesFromBrowser(string browser, string? profile = null)
Ytdlp GeoBypassCountry(string countryCode)
Ytdlp AddCustomCommand(string customCommand)
Ytdlp SetUserAgent(string userAgent)
Ytdlp SetReferer(string referer)
Ytdlp UseCookies(string cookieFile)
Ytdlp SetCustomHeader(string header, string value)
Ytdlp SetAuthentication(string username, string password)
Ytdlp DownloadLivestream(bool fromStart = true)
Ytdlp DownloadSections(string timeRanges)
Ytdlp DownloadLiveStreamRealTime()
Ytdlp MergePlaylistIntoSingleVideo(string format)
Ytdlp SelectPlaylistItems(string items)
Ytdlp ConcatenateVideos()
Ytdlp ReplaceMetadata(string field, string regex, string replacement)
Ytdlp LogToFile(string logFile)
Ytdlp DisableAds()
Ytdlp SetTimeout(TimeSpan timeout)
Ytdlp SetDownloadTimeout(string timeout)
```

### Utility / Info

```csharp
Ytdlp Version()
Ytdlp Update()
Ytdlp WriteMetadataToJson()
Ytdlp ExtractMetadataOnly()
```

## Usage Examples

### Basic download (720p video + best audio)

```csharp
var ytdlp = new Ytdlp();

await ytdlp
    .SetFormat("bestvideo[height<=720]+bestaudio/best")
    .SetOutputFolder("./downloads")
    .SetOutputTemplate("%(title)s [%(resolution)s].%(ext)s")
    .EmbedMetadata()
    .EmbedThumbnail()
    .ExecuteAsync("https://www.youtube.com/watch?v=VIDEO_ID");
```

### Auto-select best formats

```csharp
var bestAudio = await ytdlp.GetBestAudioFormatIdAsync(url);
var bestVideo = await ytdlp.GetBestVideoFormatIdAsync(url, maxHeight: 1080);

await ytdlp
    .SetFormat($"{bestVideo}+{bestAudio}/best")
    .ExecuteAsync(url);
```

### Fetch metadata only

```csharp
var meta = await ytdlp.GetVideoMetadataJsonAsync(url);
Console.WriteLine($"Title: {meta?.Title}");
Console.WriteLine($"Duration: {meta?.Duration} s");
Console.WriteLine($"Best thumbnail: {meta?.BestThumbnailUrl}");
```

### Monitor progress

```csharp
ytdlp.OnProgressDownload += (s, e) =>
    Console.Write($"\r[{new string('=', (int)e.Percent / 3)}] {e.Percent:F1}%  {e.Speed}  ETA {e.ETA}");

await ytdlp.ExecuteAsync(url);
```

### Download best 1080p video + best audio (auto-selected)

```csharp
var ytdlp = new Ytdlp();

string url = "https://www.youtube.com/watch?v=Xt50Sodg7sA";

// Auto-select best formats
string bestVideo = await ytdlp.GetBestVideoFormatIdAsync(url, maxHeight: 1080);
string bestAudio = await ytdlp.GetBestAudioFormatIdAsync(url);

await ytdlp
    .SetFormat($"{bestVideo}+{bestAudio}/best")
    .SetOutputFolder("./downloads")
    .SetOutputTemplate("%(title)s [%(resolution)s - %(id)s].%(ext)s")
    .EmbedMetadata()
    .EmbedThumbnail()
    .ExecuteAsync(url);### 1. Download best 1080p video + best audio (auto-selected)

```csharp
var ytdlp = new Ytdlp();

string url = "https://www.youtube.com/watch?v=Xt50Sodg7sA";

// Auto-select best formats
string bestVideo = await ytdlp.GetBestVideoFormatIdAsync(url, maxHeight: 1080);
string bestAudio = await ytdlp.GetBestAudioFormatIdAsync(url);

await ytdlp
    .SetFormat($"{bestVideo}+{bestAudio}/best")
    .SetOutputFolder("./downloads")
    .SetOutputTemplate("%(title)s [%(resolution)s - %(id)s].%(ext)s")
    .EmbedMetadata()
    .EmbedThumbnail()
    .ExecuteAsync(url);
```

### Monitor download progress with a simple console bar 
```csharp
ytdlp.OnProgressDownload += (sender, args) =>
{
    ConsoleProgress.Update(args.Percent, $"{args.Speed}  ETA {args.ETA}");
};

ytdlp.OnCompleteDownload += (sender, msg) => ConsoleProgress.Complete($"Finished: {msg}");

await ytdlp
    .SetFormat("best[height<=720]")
    .ExecuteAsync(url);
```

### Fetch metadata and display video info

```csharp
var metadata = await ytdlp.GetVideoMetadataJsonAsync(url);

if (metadata != null)
{
    Console.WriteLine($"Title:       {metadata.Title}");
    Console.WriteLine($"Channel:     {metadata.Channel} ({metadata.ChannelFollowerCount:N0} followers)");
    Console.WriteLine($"Duration:    {metadata.DurationTimeSpan?.ToString(@"mm\:ss") ?? "N/A"}");
    Console.WriteLine($"Views:       {metadata.ViewCount:N0}");
    Console.WriteLine($"Likes:       {metadata.LikeCount:N0}");
    Console.WriteLine($"Best thumb:  {metadata.BestThumbnailUrl}");

    // List categories and tags
    if (metadata.Categories?.Any() == true)
        Console.WriteLine($"Categories:  {string.Join(", ", metadata.Categories)}");

    if (metadata.Tags?.Any() == true)
        Console.WriteLine($"Tags:        {string.Join(", ", metadata.Tags.Take(10))}...");
}
```
### Download only the best audio as MP3
```csharp
string bestAudioId = await ytdlp.GetBestAudioFormatIdAsync(url);

await ytdlp
    .SetFormat(bestAudioId)
    .ExtractAudio("mp3")
    .SetOutputFolder("./audio")
    .SetOutputTemplate("%(title)s - %(uploader)s.%(ext)s")
    .ExecuteAsync(url);
```

### Remove SponsorBlock segments (all categories)
```csharp
await ytdlp
    .RemoveSponsorBlock("all")  // or specific: "sponsor", "intro", "outro", etc.
    .SetFormat("best")
    .SetOutputFolder("./sponsor-free")
    .ExecuteAsync(url);
```

### Download with concurrent fragments (faster on good connections)
```csharp
await ytdlp
    .WithConcurrentFragments(8)  // 8 parallel chunks
    .SetFormat("best")
    .ExecuteAsync(url);
```
### Batch download (concurrent, 4 at a time)
```csharp
var urls = new[]
{
    "https://www.youtube.com/watch?v=VIDEO1",
    "https://www.youtube.com/watch?v=VIDEO2",
    "https://www.youtube.com/watch?v=VIDEO3",
    "https://www.youtube.com/watch?v=VIDEO4"
};

await ytdlp
    .SetFormat("best[height<=480]")  // lower quality for faster batch
    .SetOutputFolder("./batch")
    .ExecuteBatchAsync(urls, maxConcurrency: 4);
```

### Cancel a long download after 15 seconds
```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

try
{
    await ytdlp
        .SetFormat("best")
        .ExecuteAsync(url, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Download cancelled as expected.");
}
```

### Simulate & preview command first
```csharp
ytdlp
    .SetFormat("137+251")
    .EmbedMetadata()
    .SetOutputTemplate("%(title)s.%(ext)s");

Console.WriteLine("Preview command:");
Console.WriteLine(ytdlp.PreviewCommand());

// → Outputs: --embed-metadata -f "137+251" -o "%(title)s.%(ext)s"
```

### Advanced: Use cookies from browser + proxy + custom header
```csharp
await ytdlp
    .CookiesFromBrowser("chrome")
    .UseProxy("http://proxy.example.com:8080")
    .SetCustomHeader("Referer", "https://example.com")
    .SetFormat("best")
    .ExecuteAsync("https://private-video-url");
```
---

## Contributing

Contributions are welcome! Please submit issues or pull requests to the [GitHub repository](https://github.com/manusoft/yt-dlp-wrapper). Ensure code follows the project’s style guidelines and includes unit tests.

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/manusoft/yt-dlp-wrapper/blob/master/LICENSE.txt) file for details.

## License

This library is licensed under the MIT License. See LICENSE for more information.
