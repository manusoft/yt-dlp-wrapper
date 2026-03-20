﻿![Static Badge](https://img.shields.io/badge/Ytdlp.NET-red) ![NuGet Version](https://img.shields.io/nuget/v/Ytdlp.NET)  ![NuGet Downloads](https://img.shields.io/nuget/dt/Ytdlp.NET)

# Ytdlp.NET

> **v3.0**

**Ytdlp.NET** is a **fluent, strongly-typed .NET wrapper** around [`yt-dlp`](https://github.com/yt-dlp/yt-dlp). It provides a fully **async, event-driven interface** for downloading videos, extracting audio, retrieving metadata, and post-processing media from YouTube and hundreds of other platforms.

---

## ⚠️ Important Notes

* **Namespace migrated**: `ManuHub.Ytdlp.NET` — update your `using` directives.
* **External JS runtime**: yt-dlp requires an external JS runtime like **deno.exe** (from [denoland/deno](https://deno.land)) for YouTube downloads with JS challenges.
* **Required tools**:

```
Tools/
├─ yt-dlp.exe
├─ deno.exe
├─ ffmpeg.exe
└─ ffprobe.exe
```

> Recommended: Use companion NuGet packages:
>
> * `ManuHub.Ytdlp`
> * `ManuHub.Deno`
> * `ManuHub.FFmpeg`
> * `ManuHub.FFprobe`

Example path resolution in .NET:

```csharp
var ytdlpPath = Path.Combine(AppContext.BaseDirectory, "tools", "yt-dlp.exe");
var ffmpegPath = Path.Combine(AppContext.BaseDirectory, "tools");
```

---

## ✨ Features

* **Fluent API**: Build yt-dlp commands with `WithXxx()` methods.
* **Immutable & thread-safe**: Each method returns a new instance, safe for parallel usage.
* **Async & IAsyncDisposable**: Automatic cleanup of child processes.
* **Progress & Events**: Real-time progress tracking and post-processing notifications.
* **Format Listing**: Retrieve and parse available formats.
* **Batch Downloads**: Sequential or parallel execution.
* **Output Templates**: Flexible naming with yt-dlp placeholders.
* **Custom Command Injection**: Add extra yt-dlp options safely.
* **Cross-platform**: Windows, macOS, Linux (where yt-dlp is supported).

---

## 🚀 New in v3.0

* Full support for `IAsyncDisposable` with `await using`.
* Immutable builder (`WithXxx`) for safe instance reuse.
* Updated examples for event-driven downloads.
* Simplified metadata fetching & format selection.
* High-performance probe methods with optional buffer size.
* Improved cancellation & error handling.

---

## 🛠 Methods
* `VersionAsync(CancellationToken ct)`
* `UpdateAsync(UpdateChannel channel, CancellationToken ct)`
* `GetMetadataAsync(string url, CancellationToken ct, int bufferKb)`
* `GetMetadataRawAsync(string url, CancellationToken ct, int bufferKb)`
* `GetFormatsAsync(string url, CancellationToken ct, int bufferKb)`
* `GetMetadataLiteAsync(string url, CancellationToken ct, int bufferKb)`
* `GetMetadataLiteAsync(string url, IEnumerable<string> fields, CancellationToken ct, int bufferKb)`
* `GetBestAudioFormatIdAsync(string url, CancellationToken ct, int bufferKb)`
* `GetBestVideoFormatIdAsync(string url, int maxHeight, CancellationToken ct, int bufferKb)`
* `ExecuteAsync(string url, CancellationToken ct)`
* `ExecuteBatchAsync(IEnumerable<string> urls, int maxConcurrency, CancellationToken ct)`



## 🔧 Thread Safety & Disposal

* **Immutable & thread-safe**: Each `WithXxx()` call returns a new instance.
* **Async disposal**: `Ytdlp` implements `IAsyncDisposable`.

### **Sequential download example**:

```csharp
await using var ytdlp = new Ytdlp("tools\\yt-dlp.exe", new ConsoleLogger())
    .WithFormat("best")
    .WithOutputFolder("./downloads");

ytdlp.OnProgressDownload += (s, e) => Console.WriteLine($"Progress: {e.Percent:F2}%");
ytdlp.OnCompleteDownload += (s, msg) => Console.WriteLine($"Download complete: {msg}");

await ytdlp.ExecuteAsync("https://www.youtube.com/watch?v=RGg-Qx1rL9U");
```

### **Parallel download example**:

```csharp
var urls = new[] { "https://youtu.be/video1", "https://youtu.be/video2" };

var tasks = urls.Select(async url =>
{
    await using var ytdlp = new Ytdlp("tools\\yt-dlp.exe", new ConsoleLogger())
        .WithFormat("best")
        .WithOutputFolder("./batch");

    ytdlp.OnProgressDownload += (s, e) => Console.WriteLine($"[{url}] {e.Percent:F2}%");
    ytdlp.OnCompleteDownload += (s, msg) => Console.WriteLine($"[{url}] Download complete: {msg}");

    await ytdlp.ExecuteAsync(url);
});

await Task.WhenAll(tasks);
```

### **Key points**:

1. Always create a **new instance per download** for parallel operations.
2. Always use `await using` for proper resource cleanup.
3. Attach events **after the `WithXxx()` call**.

---

## 📦 Basic Usage

### Download a Single Video

```csharp
await using var ytdlp = new Ytdlp("tools\\yt-dlp.exe", new ConsoleLogger())
    .WithFormat("best")
    .WithOutputFolder("./downloads")
    .WithEmbedMetadata()
    .WithEmbedThumbnail();

ytdlp.OnProgressDownload += (s, e) => Console.WriteLine($"Progress: {e.Percent:F2}%");
ytdlp.OnCompleteDownload += (s, msg) => Console.WriteLine($"Download complete: {msg}");

await ytdlp.ExecuteAsync("https://www.youtube.com/watch?v=RGg-Qx1rL9U");
```

### Extract Audio

```csharp
await using var ytdlp = new Ytdlp("tools\\yt-dlp.exe")
    .WithExtractAudio("mp3")
    .WithOutputFolder("./audio")
    .WithEmbedMetadata();

await ytdlp.ExecuteAsync("https://www.youtube.com/watch?v=RGg-Qx1rL9U");
```

---

### Fetch Metadata

```csharp
await using var ytdlp = new Ytdlp("tools\\yt-dlp.exe");

var metadata = await ytdlp.GetMetadataAsync("https://www.youtube.com/watch?v=abc123");

Console.WriteLine($"Title: {metadata?.Title}, Duration: {metadata?.Duration}");
```

---

### Fetch Formats

```csharp
await using var ytdlp = new Ytdlp("tools\\yt-dlp.exe");

var formats = await ytdlp.GetFormatsAsync("https://www.youtube.com/watch?v=abc123");

foreach(var format in formats)
    Console.WriteLine($"Id: {metadata?.Id}, Extension: {metadata?.Extension}");
```

---

### Best Format Selection

```csharp
await using var ytdlp = new Ytdlp("tools\\yt-dlp.exe");

string bestAudio = await ytdlp.GetBestAudioFormatIdAsync(url);
string bestVideo = await ytdlp.GetBestVideoFormatIdAsync(url, maxHeight: 720);

await ytdlp
    .WithFormat($"{bestVideo}+{bestAudio}/best")
    .WithOutputFolder("./downloads")
    .ExecuteAsync(url);
```

---

### Batch Downloads

```csharp
var urls = new[] { "https://youtu.be/vid1", "https://youtu.be/vid2" };

var tasks = urls.Select(async url =>
{
    await using var ytdlp = new Ytdlp("tools\\yt-dlp.exe")
        .WithFormat("best")
        .WithOutputFolder("./batch");

    await ytdlp.ExecuteAsync(url);
});

await Task.WhenAll(tasks);
```

---

### Fluent Methods (v3.0)

#### General Options
* `.WithJsRuntime(Runtime runtime, string runtimePath)`
* `.WithNoJsRuntime()`
* `.WithFlatPlaylist()`
* ` WithLiveFromStart()`
* `.WithWaitForVideo(TimeSpan? maxWait = null)`
* `.WithMarkWatched()`

#### Network Options
* `.WithProxy(string? proxy)`
* `.WithSocketTimeout(TimeSpan timeout)`
* `.WithForceIpv4()`
* `.WithForceIpv6()`
* `.WithEnableFileUrl()`

#### Geo-restriction Options
* `.WithGeoVerificationProxy(string url)`
* `.WithGeoBypassCountry(string countryCode)`

#### Filesystem Options
* `.WithHomeFolder(string path)`
* `.WithTempFolder(string path)`
* `.WithOutputFolder(string path)`
* `.WithFFmpegLocation(string path)`
* `.WithOutputTemplate(string template)`
* `.WithRestrictFilenames()`
* `.WithWindowsFilenames()`
* `.WithTrimFilenames(int length)`
* `.WithNoOverwrites()`
* `.WithForceOverwrites()`
* `.WithNoContinue()`
* `.WithNoPart()`
* `.WithMtime()`
* `.WithWriteDescription()`
* `.WithWriteInfoJson()`
* `.WithNoWritePlaylistMetafiles()`
* `.WithNoCleanInfoJson()`
* `.WriteComments()`
* `.WithNoWriteComments()`
* `.WithLoadInfoJson(string path)`
* `.WithCookiesFile(string path)`
* `.WithCookiesFromBrowser(string browser)`
* `.WithNoCacheDir()`
* `.WithRemoveCacheDir()`

#### Format & Extraction
* `.WithFormat(string format)`
* `.WithExtractAudio(string format = "mp3", int quality = 5)`
* `.With720pOrBest()`
* `.WithEmbedMetadata()`
* `.WithEmbedThumbnail()`
* `.WithEmbedChapters()`

#### Subtitles & Thumbnails
* `.WithSubtitles(string langs = "all", bool auto = false)`
* `.WithEmbedSubtitles(string langs = "all", string? convertTo = null)`
* `.WithThumbnails(bool all = false)`

#### Download Control
* `.WithConcurrentFragments(int count)`
* `.WithSponsorblockRemove(string categories = "all")`

#### Advanced Options
* `.AddFlag(string flag)`
* `.AddOption(string key, string value)`

---

### Events

```csharp
ytdlp.OnProgressDownload += (s, e) => Console.WriteLine($"Progress: {e.Percent:F2}%");
ytdlp.OnProgressMessage += (s, msg) => Console.WriteLine(msg);
ytdlp.OnCompleteDownload += (s, msg) => Console.WriteLine($"Done: {msg}");
ytdlp.OnPostProcessingComplete += (s, msg) => Console.WriteLine($"Post-processing: {msg}");
ytdlp.OnErrorMessage += (s, err) => Console.WriteLine($"Error: {err}");
ytdlp.OnOutputMessage += (s, msg) => Console.WriteLine(msg);
ytdlp.OnCommandCompleted += (s, e) => Console.WriteLine($"Command finished: {e.Command}");
```

---

### ✅ Notes

* All commands now start with `WithXxx()`.
* Immutable: no shared state; safe for parallel usage.
* Always `await using` for proper disposal.
* Deprecated old methods removed.
* Probe methods remain the same (`GetMetadataAsync`, `GetAvailableFormatsAsync`, `GetBestVideoFormatIdAsync`, etc.).

---

### License

MIT License — see [LICENSE](https://github.com/manusoft/yt-dlp-wrapper/blob/master/LICENSE.md)

**Author:** Manojbabu (ManuHub)
**Repository:** [Ytdlp.NET](https://github.com/manusoft/yt-dlp-wrapper)
