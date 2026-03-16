﻿![Static Badge](https://img.shields.io/badge/Ytdlp.NET-red) ![NuGet Version](https://img.shields.io/nuget/v/Ytdlp.NET)  ![NuGet Downloads](https://img.shields.io/nuget/dt/Ytdlp.NET)

# Ytdlp.NET
> **v2.1**

**Ytdlp.NET** is a fluent, strongly-typed .NET wrapper around the powerful [`yt-dlp`](https://github.com/yt-dlp/yt-dlp) command-line tool. It provides an intuitive and customizable interface to download videos, extract audio, retrieve metadata, and process media from YouTube and hundreds of other supported platforms.

---

## Importanant Note

> ### Namespace migrated to ``ManuHub.Ytdlp.NET`` (Update your using directives).

### External JS Scripts Setup Guide
 - To download from YouTube, yt-dlp needs to solve JavaScript challenges presented by YouTube using an external JavaScript runtime.
 - **deno.exe** binary from denoland/deno, required as an external JavaScript runtime for yt-dlp since late 2025.
 
### Recommended: Use companion NuGet packages
> **Manuhub.Ytdlp**  
> **Manuhub.Deno**  
> **Manuhub.FFmpeg**  
> **Manuhub.FFprobe**

```text
Tools/
├─ yt-dlp.exe
├─ deno.exe
├─ ffmpeg.exe
└─ ffprobe.exe
```

In .NET projects, you can reference the tools directory at runtime or copy the executable to your output folder as part of your build process.

Example path resolution:
```csharp
var ytdlpPath = Path.Combine(AppContext.BaseDirectory, "tools", "yt-dlp.exe");
var ffmpegPath = Path.Combine(AppContext.BaseDirectory, "tools");
```

---

## ✨ Features

- **Fluent API**: Easily construct `yt-dlp` commands with chainable methods.
- **Progress & Events**: Real-time progress tracking, completion, and error callbacks.
- **Format Listing**: Retrieve and parse all available formats for any video.
- **Batch Downloads**: Download multiple videos with sequential or parallel execution.
- **Custom Command Injection**: Use `AddCustomCommand` to include advanced or new options.
- **Validated Options**: Rejects invalid yt-dlp commands with a built-in option whitelist.
- **Cross-Platform**: Works on Windows, macOS, and Linux (where `yt-dlp` is supported).
- **Output Templates**: Customize naming patterns with standard `yt-dlp` placeholders.
- **Update**: Implements update method to update latest yt-dlp version. 
---

---
## 🚀 New in v2.1

- Added high-performance probe methods for metadata extraction
- Rich Metadata model parsing via `GetMetadataAsync()`
- JSON raw metadata parsing via `GetMetadataRawAsync()`
- Available formats parsing via `GetAvailableFormatsAsync()`
- Lite metadata pasing via `GetMetadataLiteAsync()`
- Ro get best video format `GetBestVideoFormatIdAsync(URL, maxHeight: 720)`
- To get best audio format `GetBestAudioFormatIdAsync(URL)`
- Convenience methods for best format auto-selection
- Implemented custom buffer size support (bufferKb) across all probe methods for optimized memory usage.
- Improved cancellation handling
- Better progress parsing and event system

### Thread Safety & Disposal

- **Ytdlp is not thread-safe**  
  Do **not** use the same instance from multiple threads or concurrent tasks.  
  Always create a fresh instance per download operation when running in parallel.

  **Safe example (concurrent batch)**:
  ```csharp
  var tasks = urls.Select(async url =>
  {
      var y = new Ytdlp(); // new instance per task
      await y.SetFormat("best").ExecuteAsync(url);
  });
  await Task.WhenAll(tasks);
  ```

  **Unsafe (will cause race conditions)**:
  ```csharp
  var y = new Ytdlp(); // shared instance
  var tasks = urls.Select(u => y.SetFormat("best").ExecuteAsync(u));
  await Task.WhenAll(tasks);
  ```

- **Ytdlp is not thread-safe**  
  In v2.0 the class does not implement IDisposable.
  Internal resources (e.g. child processes) are cleaned up automatically when the instance is garbage-collected.
  Proper Dispose support and an immutable builder pattern (for safe reuse) are planned for later.

### Fetching Video/Playlist Metadata

```csharp
var ytdlp = new Ytdlp(ytdlpPath: $"tools\\yt-dlp.exe", logger: new ConsoleLogger());

string url = "https://www.youtube.com/watch?v=Xt50Sodg7sA";

var metadata = await ytdlp.GetMetadataAsync(url);

if (metadata == null)
{
    Console.WriteLine("No metadata returned.");
    return;
}

// Basic info
Console.WriteLine($"Type        : {metadata.Type}");
Console.WriteLine($"ID          : {metadata.Id}");
Console.WriteLine($"Title       : {metadata.Title}");
Console.WriteLine($"Description : {(metadata.Description?.Length > 120 ? metadata.Description.Substring(0, 120) + "..." : metadata.Description)}");
Console.WriteLine($"Thumbnail   : {metadata.Thumbnail}");

if (metadata.Type == "video")
{
    Console.WriteLine("  ID       NOTE              EXT   RESOLUTION   FPS   VCODEC     ACODEC    PROTOCOL   SIZE/APPROX");
    Console.WriteLine("  ──────────────────────────────────────────────────────────────────────────────────────────────");

    // Show formats (both full list and requested/selected)
    Console.WriteLine(
            $"  {f.FormatId,-8} " +
            $"{(f.FormatNote ?? "-"),-17} " +
            $"{f.Ext,-5} " +
            $"{(f.Resolution ?? "-"),-12} " +
            $"{f.Fps?.ToString("0.#") ?? "-",5} " +
            $"{(f.Vcodec ?? "-"),-10} " +
            $"{(f.Acodec ?? "-"),-9} " +
            $"{(f.Protocol ?? "-"),-10} " +
            $"{size,-12}"
        );

    //PrintFormats("Selected / requested formats", metadata.RequestedFormats);
}
```

### Auto-Selecting Best Formats
```csharp
// Get best audio-only format ID
string bestAudio = await ytdlp.GetBestAudioFormatIdAsync(url);
// → e.g. "251" (highest bitrate opus/webm)

// Get best video ≤ 720p
string bestVideo = await ytdlp.GetBestVideoFormatIdAsync(url, maxHeight: 720);
// → e.g. "136" (720p mp4/avc1)

// Download best combination
await ytdlp
    .SetFormat($"{bestVideo}+{bestAudio}/best")
    .SetOutputFolder("./downloads")
    .ExecuteAsync(url);
```

### Full Metadata + Format Selection Example
```
var metadata = await ytdlp.GetMetadataAsync(url);

var best1080p = metadata.Formats?
    .Where(f => f.Height == 1080 && f.Vcodec != "none")
    .OrderByDescending(f => f.Fps ?? 0)
    .FirstOrDefault();

if (best1080p != null)
{
    Console.WriteLine($"Best 1080p format: {best1080p.FormatId} – {best1080p.Resolution} @ {best1080p.Fps} fps");
}
```

### Full Raw JSON Metadata Example
```
var jsonObjectMetadata = await ytdlp.GetMetadataRawAsync(url);

```


## 📦 Prerequisites

**Ytdlp.NET** is a lightweight wrapper around yt-dlp — it does **not** include yt-dlp, FFmpeg, FFprobe or Deno itself.  
You have two main ways to set up the required dependencies:

- **.NET**: .NET 8.0 or higher
- **yt-dlp**: 

### Recommended: Use companion NuGet packages (easiest & portable)

We provide official build packages that automatically download and manage the latest stable binaries:

```xml
<ItemGroup>
  <PackageReference Include="ManuHub.Ytdlp" Version="*" />  
  <PackageReference Include="ManuHub.FFmpeg" Version="*" />
  <PackageReference Include="ManuHub.FFprobe" Version="*" />
  <PackageReference Include="ManuHub.Deno" Version="*" /> 
</ItemGroup>
```

```csharp
var ytdlp = new Ytdlp(ytDlpPath: @"\Tools\yt-dlp.exe");
```

## ✨ Basic Usage

### 🔽 Download a Single Video

Download a video with the best quality to a specified folder:

```csharp
var ytdlp = new Ytdlp("yt-dlp", new ConsoleLogger());

await ytdlp
    .SetFormat("best")
    .SetOutputFolder("downloads")
    .DownloadThumbnails()
    .ExecuteAsync("https://www.youtube.com/watch?v=RGg-Qx1rL9U");

```

### 🎵 Extract Audio + Embed Metadata

```csharp
await ytdlp
    .ExtractAudio("mp3")
    .EmbedMetadata()
    .SetOutputFolder("audio")
    .ExecuteAsync("https://www.youtube.com/watch?v=RGg-Qx1rL9U");
```

### 🧾 List Available Formats

```csharp
var formats = await ytdlp.GetAvailableFormatsAsync("https://youtube.com/watch?v=abc123");
foreach (var f in formats)
{
    Console.WriteLine($"ID: {f.ID}, Resolution: {f.Resolution}, VCodec: {f.VCodec}");
}
```

### 🧪 Get Video Metadata Only

```csharp
var metadata = await ytdlp.GetMetadataAsync("https://youtube.com/watch?v=abc123");
Console.WriteLine($"Title: {metadata?.Title}, Duration: {metadata?.Duration}");
```

### 📦 Batch Download

Sequential (one after another)

```csharp
await ytdlp
    .SetFormat("best")
    .SetOutputFolder("batch")
    .ExecuteBatchAsync(new[] {
        "https://youtu.be/vid1", "https://youtu.be/vid2"
    });
```

Parallel (max 3 at a time)

```csharp
await ytdlp
    .SetFormat("best")
    .SetOutputFolder("batch")
    .ExecuteBatchAsync(new[] {
        "https://youtu.be/vid1", "https://youtu.be/vid2"
    }, maxConcurrency: 3);
```

### ⚙️ Configuration & Options

#### ✅ Common Fluent Methods

#### 1. Output & Path Configuration
- ``.SetOutputFolder([Required] string outputFolderPath)``
- ``.SetTempFolder([Required] string tempFolderPath)``
- ``.SetHomeFolder([Required] string homeFolderPath)``
- ``.SetFFmpegLocation([Required] string ffmpegFolder)``
- ``.SetOutputTemplate([Required] string template)``

#### 2. Format Selection & Extraction
- ``.SetFormat([Required] string format)``
- ``.ExtractAudio(string audioFormat)``
- ``.SetResolution(string resolution)``

#### 3. Metadata & Format Fetching
- ``.WriteMetadataToJson()``
- ``.ExtractMetadataOnly()``

#### 4. Download & Post-Processing Options
- ``.EmbedMetadata()``
- ``.EmbedThumbnail()``
- ``.DownloadThumbnails()``
- ``.DownloadSubtitles(string languages = "all")``
- ``.DownloadLivestream(bool fromStart = true)``
- ``.DownloadLiveStreamRealTime()``
- ``.DownloadSections(string timeRanges)``
- ``.DownloadAudioAndVideoSeparately()``
- ``.PostProcessFiles("--audio-quality 0")``
- ``.MergePlaylistIntoSingleVideo(string format)``
- ``.ConcatenateVideos()``
- ``.ReplaceMetadata(string field, string regex, string replacement)``
- ``.SetKeepTempFiles(bool keep)``
- ``.SetDownloadTimeout(string timeout)``
- ``.SetTimeout(TimeSpan timeout)``
- ``.SetRetries(string retries)``
- ``.SetDownloadRate(string rate)``
- ``.SkipDownloaded()``

#### 5. Authentication & Security
- ``.SetAuthentication(string username, string password)``
- ``.UseCookies("cookies.txt")``
- ``.SetCustomHeader(string header, string value)``

#### 6. Network & Headers
- ``.SetUserAgent("MyApp/1.0")``
- ``.SetReferer(string referer)``
- ``.UseProxy(string proxy)``
- ``.DisableAds()``

#### 7. Playlist & Selection
- ``.SelectPlaylistItems(string items)``

#### 8. Logging & Simulation
- ``.LogToFile(string logFile)``
- ``.Simulate()``

#### 10. Advanced & Specialized Options
- ``.WithConcurrentFragments(int count)``
- ``.RemoveSponsorBlock(params string[] categories)``
- ``.EmbedSubtitles(string languages = "all", string? convertTo = null)``
- ``.CookiesFromBrowser(string browser, string? profile = null)``
- ``.GeoBypassCountry(string countryCode)``
- ``.AddCustomCommand(string command)``

#### 🧩 Add Custom yt-dlp Option

```csharp
ytdlp.AddCustomCommand("--sponsorblock-mark all");
```
Will be validated against internal whitelist. Invalid commands will trigger error logging via ILogger.

### 📡 Events

```csharp
ytdlp.OnProgressMessage += (s, msg) => Console.WriteLine($"Progress: {msg}");
ytdlp.OnErrorMessage += (s, err) => Console.WriteLine($"Error: {err}");
ytdlp.OnCommandCompleted += (success, message) => Console.WriteLine($"Finished: {message}");
ytdlp.OnOutputMessage += (s, msg) => Console.WriteLine(msg);
ytdlp.OnPostProcessingComplete += (s, msg) => Console.WriteLine($"Postprocessing: {msg}");
```

### 📄 Output Template

You can customize file naming using yt-dlp placeholders:
```csharp
ytdlp.SetOutputTemplate("%(title)s-%(id)s.%(ext)s");
```

### 🧪 Validation & Safety

All AddCustomCommand(...) calls are validated against a known safe set of yt-dlp options, minimizing the risk of malformed or unsupported commands.

### ❗ Error Handling

All exceptions are wrapped in YtdlpException:

```csharp
try
{
    await ytdlp.ExecuteAsync("https://invalid-url");
}
catch (YtdlpException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

### 🧪 Version Check

```csharp
string version = await ytdlp.GetVersionAsync();
Console.WriteLine($"yt-dlp version: {version}");
```

### 🔄 Update Check

```csharp
UpdateChannel channel = UpdateChannel.Stable; // Master, Nightly.
string version = await ytdlp.UpdateAsync(channel);
Console.WriteLine($"yt-dlp version: {version}");
```

### 💡 Tips

- For livestreams, use:
  ```csharp
  .DownloadLivestream(true)
  ```
- To skip already-downloaded videos:
  ```csharp
  .SkipDownloaded()
  ```

### 🛠 Custom Logging

Implement your own ILogger:

```csharp
public class ConsoleLogger : ILogger
{
    public void Log(LogType type, string message)
    {
        Console.WriteLine($"[{type}] {message}");
    }
}
```

### ⚠️ Deprecated 
| Deprecated method | New method |
|-------------------|------------|
| `GetFormatsDetailedAsync(string url)` | `GetMetadataAsync(string url)` or `GetAvailableFormats(string url)` |
| `GetSimpleMetadataAsync(string url)` | `GetMetadataLiteAsync(string url)` |
| `GetSimpleMetadataAsync(string url, IEnumerable<string> fields)` | `GetMetadataLiteAsync(string url, IEnumerable<string> fields)`|
| `SimpleMetadata` | `MetedataLight`|

| Deprecated model | New model      |
|------------------|----------------|
| `SimpleMetadata` | `MetedataLight`|

### Future versions 
- `IDisposable` with process cleanup
- `YtdlpBuilder` for immutable instances
- `Ytdlp.Create()` will create a `YtdlpRootBuilder()` with `General()`, `Probe()` and `Download()` for easy use
- Persistent process pool for speed
- IAsyncDisposable for async cleanup

## 🤝 Contributing

Contributions are welcome! Please submit issues or pull requests to the [GitHub repository](https://github.com/manusoft/yt-dlp-wrapper). Ensure code follows the project’s style guidelines and includes unit tests.

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/manusoft/yt-dlp-wrapper/blob/master/LICENSE.txt) file for details.

---

**Author:** Manojbabu (ManuHub)  
**Repository:** [Ytdlp.NET](https://github.com/manusoft/yt-dlp-wrapper)
