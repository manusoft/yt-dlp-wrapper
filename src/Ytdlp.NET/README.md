![Static Badge](https://img.shields.io/badge/Ytdlp.NET-red) ![NuGet Version](https://img.shields.io/nuget/v/Ytdlp.NET)  ![NuGet Downloads](https://img.shields.io/nuget/dt/Ytdlp.NET) ![.NET](https://img.shields.io/badge/.NET%20%7C%208%20%7C%209%20%7C%2010-blueviolet)

# Ytdlp.NET

**Ytdlp.NET** is a fluent, strongly-typed .NET wrapper around the powerful [`yt-dlp`](https://github.com/yt-dlp/yt-dlp) command-line tool. It provides an intuitive and customizable interface to download videos, extract audio, retrieve metadata, and process media from YouTube and hundreds of other supported platforms.

## Importanant Note

### External JS Scripts Setup Guide
 - To download from YouTube, yt-dlp needs to solve JavaScript challenges presented by YouTube using an external JavaScript runtime.
 - Supports downloading EJS script dependencies from npm (--remote-components ejs:npm)

To use this:

```csharp
 await ytdlp
    .SetOutputFolder("c:\video")
    .SetFormat("b")
    .AddCustomCommand("--restrict-filenames")
    .AddCustomCommand("--remote-components ejs:npm")
    .ExecuteAsync(YoutubeUrl, cancellationToken);
```

## 🚀 Features

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

## New in v2.0

- Rich JSON metadata parsing via `GetVideoMetadataJsonAsync`
- Detailed format selection with `GetFormatsDetailedAsync`
- Convenience methods for best format auto-selection
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

### Fetching Video Metadata

```csharp
var ytdlp = new Ytdlp();

string url = "https://www.youtube.com/watch?v=Xt50Sodg7sA";

var metadata = await ytdlp.GetVideoMetadataJsonAsync(url);

if (metadata != null)
{
    Console.WriteLine($"Title: {metadata.Title}");
    Console.WriteLine($"Duration: {metadata.Duration} seconds");
    Console.WriteLine($"Views: {metadata.ViewCount:N0}");
    Console.WriteLine($"Thumbnail: {metadata.Thumbnail}");
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
var metadata = await ytdlp.GetVideoMetadataJsonAsync(url);

var best1080p = metadata.Formats?
    .Where(f => f.Height == 1080 && f.Vcodec != "none")
    .OrderByDescending(f => f.Fps ?? 0)
    .FirstOrDefault();

if (best1080p != null)
{
    Console.WriteLine($"Best 1080p format: {best1080p.FormatId} – {best1080p.Resolution} @ {best1080p.Fps} fps");
}
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
  <!-- Core yt-dlp binary (recommended) -->
  <PackageReference Include="Ytdlp.Stable.Build" Version="*" />

  <!-- FFmpeg + FFprobe for merging, audio extraction, thumbnails, etc. (strongly recommended) -->
  <PackageReference Include="Ytdlp.FFmpeg.Build" Version="*" />
  <PackageReference Include="Ytdlp.FFprobe.Build" Version="*" />

  <!-- Deno runtime — only needed if you use advanced JavaScript extractor features -->
  <!-- <PackageReference Include="Ytdlp.Deno.Runtime" Version="*" /> -->
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
var metadata = await ytdlp.GetVideoMetadataJsonAsync("https://youtube.com/watch?v=abc123");
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
- .SetFormat(string format)
- .SetOutputFolder(string path)
- .ExtractAudio(string format)
- .EmbedMetadata()
- .DownloadThumbnails()
- .DownloadSubtitles("en")
- .UseCookies("cookies.txt")
- .SetUserAgent("MyApp/1.0")
- .Simulate()
- .DisableAds()
- .SetDownloadTimeout("30")
- .SetAuthentication(username, password)
- .PostProcessFiles("--audio-quality 0")
- .AddCustomCommand(string command)

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

To preview what command will run:

```csharp
string preview = ytdlp.PreviewCommand();
Console.WriteLine(preview);
```

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

### Future versions 
- `IDisposable` with process cleanup
- `YtdlpBuilder` for immutable instances
- Persistent process pool for speed
- IAsyncDisposable for async cleanup

## 🤝 Contributing

Contributions are welcome! Please submit issues or pull requests to the [GitHub repository](https://github.com/manusoft/yt-dlp-wrapper). Ensure code follows the project’s style guidelines and includes unit tests.

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/manusoft/yt-dlp-wrapper/blob/master/LICENSE.txt) file for details.

# Ytdlp.NET v3 - Modern .NET YouTube Downloader

![NuGet](https://img.shields.io/nuget/v/Ytdlp.NET)
![Downloads](https://img.shields.io/nuget/dt/Ytdlp.NET)

---

## 1. Introduction

**Ytdlp.NET v3** is a modern, fully asynchronous .NET wrapper around `yt-dlp` for downloading videos and audio from YouTube and other supported sites. This version introduces a completely refactored API, better event handling, improved concurrency, and robust progress reporting.

> **Note:** v3 is a breaking change from v2. Read the migration guide carefully.

---

## 2. Installation

Install via NuGet:

```powershell
Install-Package Ytdlp.NET -Version 3.0.0
```

Make sure you have `yt-dlp.exe` accessible on your system, or provide a path via `YtdlpBuilder.YtDlpPath`.

---

## 3. Migration Guide v2 → v3

* **Namespaces and class names remain the same**, but the API is fully async and event-driven.
* Old synchronous methods are removed.
* `ExecuteAsync` replaces the old `Run` / `Download` methods.
* Batch operations are fully asynchronous with concurrency limits.
* Event handling is centralized through `YtdlpCommand`.

### Example:

```csharp
// v2
var result = ytdlp.Download(url);

// v3
var command = new YtdlpCommand(builder);
await command.ExecuteAsync(url);
```

---

## 4. Quick Start

```csharp
var builder = new YtdlpBuilder()
{
    OutputFolder = "Downloads",
    Format = "bestvideo+bestaudio",
    YtDlpPath = "yt-dlp.exe"
};

var command = new YtdlpCommand(builder);

command.OnProgressDownload += (s, e) => Console.WriteLine($"Progress: {e.Percent}%");
command.OnCompleteDownload += (s, url) => Console.WriteLine($"Completed: {url}");

await command.ExecuteAsync("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
```

---

## 5. Core Classes & Usage

### YtdlpBuilder

Builder class for configuration.

```csharp
var builder = new YtdlpBuilder()
{
    OutputFolder = "Downloads",
    Format = "bestaudio",
    HomeFolder = "C:\\YtdlpHome",
    TempFolder = "C:\\YtdlpTemp",
    ConcurrentFragments = 4,
    YtDlpPath = "yt-dlp.exe",
    Logger = new ConsoleLogger()
};

// Optional flags
builder.Flags.Add("--no-check-certificate");

// Key-value options
builder.Options.Add(("--retries", "10"));
```

### YtdlpCommand

Handles execution and event forwarding.

```csharp
var command = new YtdlpCommand(builder);

// Event subscriptions
command.OnProgressDownload += (s, e) => Console.WriteLine($"{e.Percent}% {e.Speed}");
command.OnProgressMessage += (s, msg) => Console.WriteLine(msg);
command.OnPostProcessingStarted += (s, msg) => Console.WriteLine("Post-processing started: " + msg);
command.OnPostProcessingCompleted += (s, msg) => Console.WriteLine("Post-processing completed: " + msg);
command.OnCompleteDownload += (s, url) => Console.WriteLine("Completed: " + url);
command.OnProcessCompleted += (s, msg) => Console.WriteLine("Process done: " + msg);
command.OnErrorMessage += (s, msg) => Console.WriteLine("Error: " + msg);
```

---

## 6. Command Options / Arguments

* `OutputFolder` - Where files are saved.
* `Format` - Video/audio format, e.g., `bestvideo+bestaudio`.
* `ConcurrentFragments` - Number of fragments to download in parallel.
* `Flags` - Additional yt-dlp flags.
* `Options` - Key-value options.
* `CookiesFile` / `CookiesFromBrowser` - Authentication.
* `Proxy` - HTTP/SOCKS proxy.
* `FfmpegLocation` - Custom FFmpeg path.
* `SponsorblockRemoveCategories` - Remove sponsor segments.

All options are forwarded automatically to yt-dlp.

---

## 7. Event Handling & Progress Reporting

Events provide full real-time feedback:

| Event                     | Description                               |
| ------------------------- | ----------------------------------------- |
| OnProgressDownload        | Fires periodically with download progress |
| OnProgressMessage         | Any general message from yt-dlp           |
| OnPostProcessingStarted   | When post-processing (like merge) starts  |
| OnPostProcessingCompleted | When post-processing completes            |
| OnCompleteDownload        | When a download finishes                  |
| OnProcessCompleted        | Process finished (exit)                   |
| OnErrorMessage            | Error messages                            |

```csharp
command.OnProgressDownload += (s, e) =>
    Console.WriteLine($"{e.Percent}% | {e.DownloadedMB}/{e.TotalMB} MB");
```

---

## 8. Examples

### Simple Download

```csharp
await command.ExecuteAsync(url);
```

### Audio-Only Download

```csharp
builder.Format = "bestaudio";
await command.ExecuteAsync(url);
```

### Batch Download

```csharp
var urls = new[] { url1, url2, url3 };
var results = await command.ExecuteBatchAsync(urls, maxConcurrency: 3);

foreach(var r in results)
    Console.WriteLine(r.Url + (r.Error == null ? " OK" : " Failed"));
```

### Custom Proxy & Cookies

```csharp
builder.Proxy = "socks5://127.0.0.1:1080";
builder.CookiesFile = "cookies.txt";
await command.ExecuteAsync(url);
```

---

## 9. Advanced Usage

### Running Probes

```csharp
var output = await YtdlpCommand.RunProbeAsync(command, url, ct);
Console.WriteLine(output);
```

### Cancellation

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await command.ExecuteAsync(url, cts.Token);
```

### Capture Stdout & Errors

* v3 automatically captures both stdout and stderr via events.
* Use `OnProgressMessage` and `OnErrorMessage`.

---

## 10. Error Handling

* `YtdlpException` for yt-dlp related errors.
* Exceptions are thrown if:

  * Process exits with non-zero code
  * Probe fails
  * Output folder cannot be created

```csharp
try
{
    await command.ExecuteAsync(url);
}
catch(YtdlpException ex)
{
    Console.WriteLine("Download failed: " + ex.Message);
}
```

---

## 11. Notes & Tips

* Use `ExecuteBatchAsync` for multiple downloads.
* Always unsubscribe from events if using long-lived `YtdlpCommand` instances.
* Set `ConcurrentFragments` for faster downloads on multi-core systems.
* Ensure `yt-dlp` is updated for best compatibility.

---

## 12. Changelog / Breaking Changes

### v3.0.0

* Fully async API
* Consolidated `ExecuteAsync` method
* Event-driven progress and messages
* Batch execution with concurrency
* Old v2 synchronous methods removed
* `BuildArgs` moved to `YtdlpBuilder`
* Breaking API changes require code updates

---

**Author:** Your Name
**License:** MIT
**Repository:** [https://github.com/YourRepo/Ytdlp.NET](https://github.com/YourRepo/Ytdlp.NET)
