![Static Badge](https://img.shields.io/badge/Ytdlp.NET-red) ![NuGet Version](https://img.shields.io/nuget/v/Ytdlp.NET)  ![NuGet Downloads](https://img.shields.io/nuget/dt/Ytdlp.NET)

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

## 📦 Prerequisites

- **.NET**: Requires .NET 8.0 or later.
- **yt-dlp**: The `yt-dlp` command-line tool must be installed and accessible in your system’s PATH or specified explicitly.
  - Install `yt-dlp` via pip:
    ```bash
    pip install -U yt-dlp
    ```
  - Verify installation:
    ```bash
    yt-dlp --version
    ```
- **FFmpeg** (optional): Required for certain operations like merging formats or extracting audio. Install via your package manager or download from [FFmpeg.org](https://ffmpeg.org/).

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

## 🤝 Contributing

Contributions are welcome! Please submit issues or pull requests to the [GitHub repository](https://github.com/manusoft/yt-dlp-wrapper). Ensure code follows the project’s style guidelines and includes unit tests.

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/manusoft/yt-dlp-wrapper/blob/master/LICENSE.txt) file for details.