# ![Static Badge](https://img.shields.io/badge/ytdlp.NET-red) ![NuGet Version](https://img.shields.io/nuget/v/ytdlp.net) ![NuGet Downloads](https://img.shields.io/nuget/dt/ytdlp.net)

![Visitors](https://visitor-badge.laobi.icu/badge?page_id=manusoft/yt-dlp-wrapper)

# Ytdlp.NET

![icon](https://github.com/user-attachments/assets/2147c398-4e0f-43e2-99cb-32b34be7dc2f)

### ClipMate - MAUI.NET App - [Download](https://apps.microsoft.com/detail/9NTP1DH4CQ4X?hl=en&gl=IN&ocid=pdpshare)

<img width="986" height="693" alt="image" src="https://github.com/user-attachments/assets/39e09415-fa0c-4991-976f-8966e9c50c5b" />

---

### Video Downloader - .NET App

![Screenshot 2025-01-23 153252](https://github.com/user-attachments/assets/1b977927-ea26-4220-bd41-9f64d6716058)

[Download the latest App](https://github.com/manusoft/yt-dlp-wrapper/releases/download/v1.0.0/gui-app.zip)

---

# Ytdlp.NET

**Ytdlp.NET** is a modern **.NET wrapper for `yt-dlp`** that provides a fluent, strongly‑typed API for downloading videos, extracting audio, fetching metadata, and monitoring progress.

The library exposes **event‑driven progress reporting**, **metadata probing**, and **safe command construction** while staying very close to the native `yt-dlp` functionality.

---

# ✨ Features

* Fluent API (`WithXxx()` methods)
* Immutable design (thread‑safe instances)
* Real‑time progress events
* Metadata & format probing
* Batch downloads
* Cancellation support
* Cross‑platform support
* Strongly typed event system
* Async execution
* `IAsyncDisposable` support

---

# 🚀 New in v3.0

Major redesign for reliability and modern .NET usage.

### Highlights

* Immutable **fluent builder API**
* `IAsyncDisposable` implemented
* Thread‑safe usage
* Simplified event handling
* Improved metadata probing
* Better cancellation support
* Safer command building

---

# 🔧 Required Tools

`yt-dlp` relies on external tools.

Recommended folder structure:

```
tools/
├─ yt-dlp.exe
├─ ffmpeg.exe
├─ ffprobe.exe
└─ deno.exe
```

Example usage:

```csharp
var ytdlpPath = Path.Combine("tools", "yt-dlp.exe");
```

---

# 🔧 Basic Usage

### Download a video

```csharp
await using var ytdlp = new Ytdlp("tools\\yt-dlp.exe")
    .WithFormat("best")
    .WithOutputFolder("./downloads")
    .WithOutputTemplate("%(title)s.%(ext)s");

ytdlp.OnProgressDownload += (s, e) =>
    Console.WriteLine($"{e.Percent:F1}% {e.Speed} ETA {e.ETA}");

await ytdlp.ExecuteAsync("https://www.youtube.com/watch?v=VIDEO_ID");
```

---

# 🎧 Extract audio

```csharp
await using var ytdlp = new Ytdlp()
    .WithExtractAudio("mp3")
    .WithOutputFolder("./audio");

await ytdlp.ExecuteAsync(url);
```

---

# 📊 Monitor Progress

```csharp
ytdlp.OnProgressDownload += (s, e) =>
{
    Console.WriteLine($"{e.Percent:F1}%  {e.Speed}  ETA {e.ETA}");
};

ytdlp.OnCompleteDownload += (s, msg) =>
{
    Console.WriteLine($"Finished: {msg}");
};
```

---

# 📦 Fetch Metadata

```csharp
await using var ytdlp = new Ytdlp();

var metadata = await ytdlp.GetMetadataAsync(url);

Console.WriteLine(metadata?.Title);
Console.WriteLine(metadata?.Duration);
```

---

# 🎬 Auto‑Select Best Formats

```csharp
await using var ytdlp = new Ytdlp();

string bestVideo = await ytdlp.GetBestVideoFormatIdAsync(url, 1080);
string bestAudio = await ytdlp.GetBestAudioFormatIdAsync(url);

await ytdlp
    .WithFormat($"{bestVideo}+{bestAudio}/best")
    .ExecuteAsync(url);
```

---

# ⚡ Parallel Downloads

```csharp
var urls = new[]
{
    "https://youtu.be/video1",
    "https://youtu.be/video2"
};

var tasks = urls.Select(async url =>
{
    await using var ytdlp = new Ytdlp()
        .WithFormat("best")
        .WithOutputFolder("./batch");

    await ytdlp.ExecuteAsync(url);
});

await Task.WhenAll(tasks);
```

---

# 📡 Events

| Event                      | Description              |
| -------------------------- | ------------------------ |
| `OnProgressDownload`       | Download progress        |
| `OnProgressMessage`        | Informational messages   |
| `OnCompleteDownload`       | File finished            |
| `OnPostProcessingComplete` | Post‑processing finished |
| `OnOutputMessage`          | Raw output line          |
| `OnErrorMessage`           | Error message            |
| `OnCommandCompleted`       | Process finished         |

---

# 🔧 Fluent API Methods

### Output

```
WithOutputFolder()
WithTempFolder()
WithHomeFolder()
WithOutputTemplate()
WithFFmpegLocation()
```

### Formats

```
WithFormat()
With720pOrBest()
WithExtractAudio()
```

### Metadata

```
GetMetadataAsync()
GetBestAudioFormatIdAsync()
GetBestVideoFormatIdAsync()
GetAvailableFormatsAsync()
```

### Features

```
WithEmbedMetadata()
WithEmbedThumbnail()
WithEmbedSubtitles()
WithSubtitles()
WithConcurrentFragments()
WithSponsorblockRemove()
```

### Network

```
WithProxy()
WithCookiesFile()
WithCookiesFromBrowser()
```

### Advanced

```
AddFlag()
AddOption()
AddCustomCommand()
```

---

# 🔄 Upgrade Guide (v2 → v3)

v3 introduces a **new immutable fluent API**.

Old mutable commands were removed.

---

## ❌ Old API (v2)

```csharp
var ytdlp = new Ytdlp();

await ytdlp
    .SetFormat("best")
    .SetOutputFolder("./downloads")
    .ExecuteAsync(url);
```

---

## ✅ New API (v3)

```csharp
await using var ytdlp = new Ytdlp()
    .WithFormat("best")
    .WithOutputFolder("./downloads");

await ytdlp.ExecuteAsync(url);
```

---

## Method changes

| v2                    | v3                     |
| --------------------- | ---------------------- |
| `SetFormat()`         | `WithFormat()`         |
| `SetOutputFolder()`   | `WithOutputFolder()`   |
| `SetTempFolder()`     | `WithTempFolder()`     |
| `SetOutputTemplate()` | `WithOutputTemplate()` |
| `SetFFMpegLocation()` | `WithFFmpegLocation()` |
| `ExtractAudio()`      | `WithExtractAudio()`   |
| `UseProxy()`          | `WithProxy()`          |

---

## Important behavior changes

### Instances are immutable

Every `WithXxx()` call returns a **new instance**.

```csharp
var baseYtdlp = new Ytdlp();

var download = baseYtdlp
    .WithFormat("best")
    .WithOutputFolder("./downloads");
```

---

### Event subscription

Attach events **to the configured instance**.

```csharp
var download = baseYtdlp.WithFormat("best");

download.OnProgressDownload += ...
```

---

### Proper disposal

Use **`await using`** for automatic cleanup.

```csharp
await using var ytdlp = new Ytdlp();
```

---

# 🧪 Example Apps

* ClipMate MAUI downloader
* Windows GUI downloader
* Console examples

---

# 🤝 Contributing

Contributions are welcome!

Open issues or PRs on GitHub.

---

# 📜 License

MIT License

See:

[https://github.com/manusoft/yt-dlp-wrapper/blob/master/LICENSE.txt](https://github.com/manusoft/yt-dlp-wrapper/blob/master/LICENSE.txt)

---

# 👨‍💻 Author

**Manoj Babu**
ManuHub