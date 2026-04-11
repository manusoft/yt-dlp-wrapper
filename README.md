# ![Static Badge](https://img.shields.io/badge/ytdlp.NET-red) ![NuGet Version](https://img.shields.io/nuget/v/ytdlp.net) ![NuGet Downloads](https://img.shields.io/nuget/dt/ytdlp.net)

![Visitors](https://visitor-badge.laobi.icu/badge?page_id=manusoft/yt-dlp-wrapper)

# Ytdlp.NET

![icon](https://github.com/user-attachments/assets/2147c398-4e0f-43e2-99cb-32b34be7dc2f)

### ClipMate - MAUI.NET App - [Download](https://apps.microsoft.com/detail/9NTP1DH4CQ4X?hl=en&gl=IN&ocid=pdpshare)

<img width="986" height="693" alt="image" src="https://github.com/user-attachments/assets/39e09415-fa0c-4991-976f-8966e9c50c5b" />

---

### Video Downloader - .NET App

![Screenshot 2025-01-23 153252](https://github.com/user-attachments/assets/1b977927-ea26-4220-bd41-9f64d6716058)

[Download the latest App](https://github.com/manusoft/Ytdlp.NET/releases/download/v1.0.0/gui-app.zip)

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

await ytdlp.DownloadAsync("https://www.youtube.com/watch?v=VIDEO_ID");
```

---

# 🎧 Extract audio

```csharp
await using var ytdlp = new Ytdlp()
    .WithExtractAudio("mp3")
    .WithOutputFolder("./audio");

await ytdlp.DownloadAsync(url);
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
    .DownloadAsync(url);
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

    await ytdlp.DownloadAsync(url);
});

await Task.WhenAll(tasks);
```

**OR**

```csharp
var urls = new[] { "https://youtu.be/vid1", "https://youtu.be/vid2" };

 await using var ytdlp = new Ytdlp("tools\\yt-dlp.exe")
        .WithFormat("best")
        .WithOutputFolder("./batch");

await ytdlp.DownloadBatchAsync(urls, maxConcurrency: 3);
```

---

# 📡 Events

| Event                      | Description              |
| -------------------------- | ------------------------ |
| `OnProgressDownload`       | Download progress        |
| `OnProgressMessage`        | Informational messages   |
| `OnCompleteDownload`       | File finished            |
| `OnPostProcessingStart`    | Post‑processing start    |
| `OnPostProcessingComplete` | Post‑processing finished |
| `OnOutputMessage`          | Raw output line          |
| `OnErrorMessage`           | Error message            |
| `OnCommandCompleted`       | Process finished         |

---

# 🔧 Fluent API Methods

### General Options
* `.WithIgnoreErrors()`
* `.WithAbortOnError()`
* `.WithIgnoreConfig()`
* `.WithConfigLocations(string path)`
* `.WithPluginDirs(string path)`
* `.WithNoPluginDirs(string path)`
* `.WithJsRuntime(Runtime runtime, string runtimePath)`
* `.WithNoJsRuntime()`
* `.WithFlatPlaylist()`
* `.WithLiveFromStart()`
* `.WithWaitForVideo(TimeSpan? maxWait = null)`
* `.WithMarkWatched()`   

### Network Options
* `.WithProxy(string? proxy)`
* `.WithSocketTimeout(TimeSpan timeout)`
* `.WithForceIpv4()`
* `.WithForceIpv6()`
* `.WithEnableFileUrls()`

### Geo-restriction Options
* `.WithGeoVerificationProxy(string url)`
* `.WithGeoBypassCountry(string countryCode)`

### Video Selection
* `.WithPlaylistItems(string items)`
* `.WithMinFileSize(string size)`
* `.WithMaxFileSize(string size)`
* `.WithDate(string date)`
* `.WithDateBefore(string date)`
* `.WithDateAfter(string date)`
* `.WithMatchFilter(string filterExpression)`
* `.WithNoPlaylist()`
* `.WithYesPlaylist()`
* `.WithAgeLimit(int years)`
* `.WithDownloadArchive(string archivePath = "archive.txt")`
* `.WithMaxDownloads(int count)`
* `.WithBreakOnExisting()`

### Download Options
* `.WithConcurrentFragments(int count = 8)`
* `.WithLimitRate(string rate)`
* `.WithThrottledRate(string rate)`
* `.WithRetries(int maxRetries)`
* `.WithFileAccessRetries(int maxRetries)`
* `.WithFragmentRetries(int retries)`
* `.WithSkipUnavailableFragments()`
* `.WithAbortOnUnavailableFragments()`
* `.WithKeepFragments()`
* `.WithBufferSize(string size)`
* `.WithNoResizeBuffer()`
* `.WithPlaylistRandom()`
* `.WithHlsUseMpegts()`
* `.WithNoHlsUseMpegts()`
* `.WithDownloadSections(string regex)`

### Filesystem Options
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

### Thumbnail Options
* `.WithThumbnails(bool allSizes = false)`

### Verbosity and Simulation Options
* `.WithQuiet()`
* `.WithNoWarnings()`
* `.WithSimulate()`
* `.WithNoSimulate()`
* `.WithSkipDownload()`
* `.WithVerbose()`

### Workgrounds
* `.WithAddHeader(string header, string value)`
* `.WithSleepInterval(double seconds, double? maxSeconds = null)`
* `.WithSleepSubtitles(double seconds)`

### Video Format Options
* `.WithFormat(string format)`
* `.WithMergeOutputFormat(string format)`

### Subtitle Options
* `.WithSubtitles(string languages = "all", bool auto = false)`

### Authentication Options
* `.WithAuthentication(string username, string password)`
* `.WithTwoFactor(string code)`

### Post-Processing Options
* `.WithExtractAudio(string format, int quality = 5)`
* `.WithRemuxVideo(string format)` usage 'mp4' or 'mp4>mkv'
* `.WithRecodeVideo(string format, string? videoCodec = null, string? audioCodec = null)`
* `.WithPostprocessorArgs(PostProcessors postprocessor, string args)`
* `.WithKeepVideo()`
* `.WithNoPostOverwrites()`
* `.WithEmbedSubtitles()`
* `.WithEmbedThumbnail()`
* `.WithEmbedMetadata()`
* `.WithEmbedChapters()`
* `.WithEmbedInfoJson()`
* `.WithNoEmbedInfoJson()`
* `.WithReplaceInMetadata(string field, string regex, string replacement)`
* `.WithConcatPlaylist(string policy = "always")`
* `.WithFFmpegLocation(string? ffmpegPath)`
* `.WithConvertSubtitles(string format = "none")`
* `.WithConvertThumbnails(string format = "jpg")`
* `.WithSplitChapters() => AddFlag("--split-chapters")`
* `.WithRemoveChapters(string regex)`
* `.WithForceKeyframesAtCuts()`
* `.WithUsePostProcessor(PostProcessors postProcessor, string? postProcessorArgs = null)`

### SponsorBlock Options
* `.WithSponsorblockMark(string categories = "all")`
* `.WithSponsorblockRemove(string categories = "all")`
* `.WithNoSponsorblock()`

### Advanced Options
* `.AddFlag(string flag)`
* `.AddOption(string key, string value)`

### Downloaders
* `.WithExternalDownloader(string downloaderName, string? downloaderArgs = null)`
* `.WithAria2(int connections = 16)`
* `.WithHlsNative()`
* `.WithFfmpegAsLiveDownloader(string? extraFfmpegArgs = null)`

AND MORE ...

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

await ytdlp.DownloadAsync(url);
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
| `AddCustomCommand()`  | `AddFlag(string flag)` or `AddOption(string key, string value)` |

---

## Custom commands
```csharp
AddFlag("--no-check-certificate");
AddOption("--external-downloader", "aria2c");
```

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