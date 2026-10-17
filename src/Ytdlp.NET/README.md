﻿![Static Badge](https://img.shields.io/badge/Ytdlp.NET-red) ![NuGet Version](https://img.shields.io/nuget/v/Ytdlp.NET)  ![NuGet Downloads](https://img.shields.io/nuget/dt/Ytdlp.NET)

# Ytdlp.NET

> **Ytdlp.NET** is a **fluent, strongly-typed, and **immutable** .NET wrapper** around [`yt-dlp`](https://github.com/yt-dlp/yt-dlp). It provides a fully **asynchronous, event-driven interface** for downloading, metadata extraction, and media processing from **YouTube** and hundreds of other platforms.
   
---

## ✨ Features

- **Fluent API** — Build yt-dlp commands with a rich set of `WithXxx()` methods
- **Immutable & thread-safe** — Each method returns a new instance, safe for parallel usage
- **Progress & Events** — Real-time progress tracking and post-processing notifications
- **Format & Metadata Listing** — Retrieve and parse available formats, subtitles, and deep hierarchical metadata
- **Batch Downloads** — Sequential or parallel execution with concurrency control
- **Output Templates** — Flexible naming using yt-dlp placeholders
- **Custom Command Injection** — Safely add extra yt-dlp options or run raw commands
- **Lifecycle Refinement: No disposal required.** The library no longer implements ``IDisposable`` or ``IAsyncDisposable``. Instances are plain configuration objects.
- **Source-generated JSON** — High-performance, Native AOT-friendly metadata serialization
- **Cross-platform** — Windows, macOS, and Linux

---

## 🚀 New in v4.x.x

- **Livestream Progress** - Implements livestream progress support.
- **Source-generated JSON** — Switched to `System.Text.Json` source generation via `YtdlpJsonContext` for faster metadata deserialization and full Native AOT support
- **Expanded Fluent API** — Dozens of new methods covering network, playlist, error handling, retry, fragment, and downloader options
- **Partial Class Refactor** — Split the large fluent builder into logical partial classes (General, Authentication, Download, etc.) for better maintainability and IntelliSense
- **YtdlpPreset Enum** — Strongly-typed preset aliases for common configurations
- **SponsorBlock Enhancements** — Added `WithSponsorblockChapterTitle()` and `WithSponsorblockApi()` for custom chapter titles and API endpoints
- **Improved Consistency** — Moved `AddFlag` / `AddOption` to the main class and refined validation across the API

---

## 🔧 Required Tools

**Namespace:** `ManuHub.Ytdlp.NET`

**External JS Runtime**  
yt-dlp requires an external JavaScript runtime (such as **deno.exe** from [denoland/deno](https://deno.land)) to solve JavaScript challenges on platforms like YouTube.

**Recommended Companion Packages**

| Package | Description |
|---------|-------------|
| ManuHub.Ytdlp | Core download engine |
| ManuHub.Deno | JavaScript challenge resolution |
| ManuHub.FFmpeg | Post-processing, merging, and conversion |
| ManuHub.FFprobe | Format probing and metadata extraction |

**Example Path Resolution**

```csharp
var ytdlpPath = Path.Combine(AppContext.BaseDirectory, "tools", "yt-dlp.exe");
var ffmpegPath = Path.Combine(AppContext.BaseDirectory, "tools");
```

---

## 🧬 Core Concepts

### No Disposal Required

**Ytdlp** holds no unmanaged resources. Create instances, share them, and let the GC collect them. All internal runners are created per-call.

### Immutable Fluent API

Every configuration method (e.g., ``WithOutputFolder``, ``WithFormat``) returns a new instance, ensuring the original is never modified. This makes branching configurations safe and clean.

### Thread Safety

A single ``Ytdlp`` instance can be shared across threads. Each execution creates isolated internal runners, allowing concurrent downloads without synchronization.

### Secure Authentication

Implemented secure authentication handling for various scenarios, including standard username/password and Adobe Pass authentication.

- .WithAuthentication(string username, string password)
- .WithAdobePassAuthentication(string mso, string username, string password)

> It securely handles credentials by passing them via standard input to the yt-dlp process, avoiding exposure in command-line arguments or logs. The library ensures that sensitive information is not stored in memory longer than necessary and is properly disposed of after use.

---

## 🚀 Quick Start

### 1. Basic Download

```csharp
var ytdlp = new Ytdlp("yt-dlp.exe")
    .WithOutputFolder("./downloads")
    .WithBestVideoPlusBestAudio()
    .WithEmbedMetadata();

// Subscribe to events
ytdlp.ProgressDownload += (s, e) => Console.WriteLine($"Progress: {e.Percent:F2}%");
ytdlp.DownloadCompleted += (s, msg) => Console.WriteLine($"Finished: {msg}");

// Execute
await ytdlp.DownloadAsync("https://www.youtube.com/watch?v=XXX");
```

### 2. Immutable Configuration Branching

```csharp
// Define a shared base configuration
var baseConfig = new Ytdlp("yt-dlp.exe").WithOutputFolder("./media");

// Create specialized versions
var audioOnly = baseConfig.WithBestAudioOnly();
var highRes = baseConfig.WithMaxHeightOrBest(1080);

// baseConfig, audioOnly, and highRes are independent, thread-safe instances
await Task.WhenAll(
    audioOnly.DownloadAsync(url1),
    highRes.DownloadAsync(url2)
);
```

---

## ⚡ Advanced Execution & Control

For power users who need to run custom commands that bypass the fluent builder, use `ExecuteRawAsync`.  
This method acts as an escape hatch for non-standard, experimental, or highly specific yt-dlp flags.

### How It Works

The method automatically switches between two output modes:

| Mode | When | Behavior |
|------|------|----------|
| **Streaming** | You provide an `Action<string>` to `onLineReceived` | Output is streamed line-by-line in real time (ideal for progress and logs) |
| **Capture** | You pass `null` (or omit) `onLineReceived` | The entire output is collected into `result.FullOutput` (ideal for JSON or one-off queries) |

### Examples

```csharp
var ytdlp = new Ytdlp("yt-dlp.exe");

// 1. Capture Mode – Extract full metadata as JSON
var jsonResult = await ytdlp.ExecuteRawAsync(
    "--dump-json https://www.youtube.com/watch?v=dQw4w9WgXcQ");
Console.WriteLine($"Metadata JSON: {jsonResult.FullOutput}");

// 2. Capture Mode – List available formats
var formatsResult = await ytdlp.ExecuteRawAsync(
    "--list-formats https://www.youtube.com/watch?v=dQw4w9WgXcQ");
Console.WriteLine($"Available Formats:\n{formatsResult.FullOutput}");

// 3. Streaming Mode – Download with custom flags + live logs
await ytdlp.ExecuteRawAsync(
    "--extract-audio --audio-format mp3 --embed-thumbnail https://www.youtube.com/watch?v=dQw4w9WgXcQ",
    onLineReceived: line => Console.WriteLine($"[yt-dlp] {line}"));

// 4. Capture Mode – Get direct stream URL
var urlResult = await ytdlp.ExecuteRawAsync(
    "--get-url -f best https://www.youtube.com/watch?v=dQw4w9WgXcQ");
Console.WriteLine($"Direct Stream URL: {urlResult.FullOutput.Trim()}");

// 5. Streaming Mode – Download playlist range with progress
await ytdlp.ExecuteRawAsync(
    "--playlist-items 1-5 --embed-subs --sub-langs en https://www.youtube.com/playlist?list=PLrEnWoR732-HnL9lzX129zUjQ0M9b8Xg7",
    onLineReceived: line => Console.WriteLine($"[Playlist] {line}"));
```

> **Note**  
> `ExecuteRawAsync` handles process security and output formatting, but logical validation of the flags you pass remains your responsibility.  
> Prefer the fluent `WithXxx()` methods for all standard download tasks.

---

## 📦 Usage Examples

### Fetch Metadata

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe");

var metadata = await ytdlp.GetMetadataAsync("https://www.youtube.com/watch?v=abc123");

Console.WriteLine($"Title: {metadata?.Title}, Duration: {metadata?.Duration}");
```

### Deep Metadata Extraction

```csharp
var metadata = await ytdlp.GetDeepMetadataAsync(url);

foreach (var root in metadata.Entries ?? [])
{
    foreach (var item in root.Traverse())
    {
        Console.WriteLine(item.Title);
    }
}
```

### Parallel Execution

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe")
    .WithFormat("best")
    .WithOutputFolder("./batch");

var urls = new[] { "https://youtu.be/vid1", "https://youtu.be/vid2" };

// Safe: Concurrent usage of the same instance
await ytdlp.DownloadBatchAsync(urls, maxConcurrency: 3);
```

### Extract Audio

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe")
    .WithExtractAudio(AudioFormat.Mp3, 5)
    .WithOutputFolder("./audio")
    .WithEmbedThumbnail()
    .WithEmbedMetadata();

await ytdlp.DownloadAsync("https://www.youtube.com/watch?v=RGg-Qx1rL9U");
```

### Download a Playlist

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe")
    .WithFormat("best")
    .WithOutputFolder("./playlists")
    .WithPlaylistStart(1)
    .WithPlaylistEnd(5)
    .OutputTemplate("%(playlist)s/%(playlist_index)s - %(title)s.%(ext)s");

await ytdlp.DownloadAsync("https://www.youtube.com/playlist?list=PL12345");
```

### Fetch Formats

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe");

var formats = await ytdlp.GetFormatsAsync("https://www.youtube.com/watch?v=abc123");

foreach(var format in formats)
    Console.WriteLine($"Id: {metadata?.Id}, Extension: {metadata?.Extension}");
```

### Best Format Selection

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe");

string bestAudio = await ytdlp.GetBestAudioFormatIdAsync(url);
string bestVideo = await ytdlp.GetBestVideoFormatIdAsync(url, maxHeight: 720);

await ytdlp
    .WithFormat($"{bestVideo}+{bestAudio}/best")
    .WithOutputFolder("./downloads")
    .DownloadAsync(url);
```

### Get Subtitles

```csharp
var ytdlp = new Ytdlp("tools\\yt-dlp.exe");
var subtitles = await ytdlp.GetSubtitlesAsync("https://www.youtube.com/watch?v=abc123");
foreach (var sub in subtitles)
{
    Console.WriteLine($"Language: {sub.Language}, Format: {sub.Format}, Url: {sub.Url}");
}
```

### Get Adobe Pass MSO List

```csharp
var msoList = await ytdlp.GetAdobePassListAsync();
```

---

## 📡 Events

| Event | Description |
|-------|-------------|
| ProgressDownload | Reports download progress (percentage, speed, ETA, etc.) |
| ProgressMessage | Emits informational progress messages |
| DownloadCompleted | Raised when a file has finished downloading |
| PostProcessingStarted | Raised when post-processing begins |
| PostProcessingCompleted | Raised when post-processing has finished |
| OutputMessage | Emits a raw output line from yt-dlp |
| ErrorMessage | Emits an error message |
| CommandCompleted | Raised when the entire process has finished |


### Example
```csharp
// Progress events
ytdlp.ProgressDownload += (s, e) => Console.WriteLine($"{e.Percent:F1}%  {e.Speed}  ETA {e.ETA}");
ytdlp.ProgressMessage += (s, msg) => Console.WriteLine(msg);

// Output events
ytdlp.ErrorMessage += (s, err) => Console.WriteLine($"Error: {err}");
ytdlp.OutputMessage += (s, msg) => Console.WriteLine(msg);

// Lifecycle events
ytdlp.DownloadCompleted += (s, msg) => Console.WriteLine($"Finished: {msg}");
ytdlp.CommandCompleted += (s, e) => Console.WriteLine($"Command finished: {e.Command}");

// Post-Processing events
ytdlp.PostProcessingStarted += (s, msg) => Console.WriteLine($"Post-processing-start: {msg}");
ytdlp.PostProcessingCompleted += (s, msg) => Console.WriteLine($"Post-processing-complete: {msg}");
```

---

## 🛠 Core API Reference

### Probe

| Method | Description |
|--------|-------------|
| VersionAsync() | Returns the installed yt-dlp version |
| UpdateAsync(UpdateChannel channel, string specificVersion) | Updates yt-dlp to the specified channel or exact version |
| GetExtractorsAsync() | Lists all available extractors |
| GetAdobePassListAsync() | Retrieves the list of supported Adobe Pass providers |
| GetSubtitlesAsync(string url) | Fetches available subtitles for the given URL |
| GetMetadataAsync(string url) | Retrieves parsed metadata for the given URL |
| GetMetadataRawAsync(string url) | Retrieves raw (unparsed) metadata for the given URL |
| GetDeepMetadataAsync(string url) | Retrieves deep/parsed metadata (more detailed) |
| GetDeepMetadataRawAsync(string url) | Retrieves deep raw metadata |
| GetFormatsAsync(string url) | Lists all available formats for the given URL |
| GetMetadataLiteAsync(string url) | Retrieves lightweight metadata |
| GetMetadataLiteAsync(string url, IEnumerable\<string\> fields) | Retrieves lightweight metadata for specific fields only |
| GetBestAudioFormatIdAsync(string url) | Returns the format ID of the best available audio |
| GetBestVideoFormatIdAsync(string url, int maxHeight) | Returns the format ID of the best video up to the specified height |

### Download

| Method | Description |
|--------|-------------|
| DownloadAsync(string url) | Downloads a single URL |
| DownloadBatchAsync(IEnumerable\<string\> urls, int maxConcurrency) | Downloads multiple URLs concurrently with the specified limit |

### Helpers

| Method | Description |
|--------|-------------|
| Traverse() | For easy iteration over nested playlist entries |

### Advanced Execution

| Method | Description |
|--------|-------------|
| ExecuteRawAsync(string arguments, Action\<string\>? onLineReceived = null, CancellationToken ct = default, bool tuneProcess = true) | Executes yt-dlp with raw arguments and optional line-by-line output handling |

---

## 🏗️ Fluent Builder API Reference

**Authentication Options**

| Method | Description |
|--------|-------------|
| WithAuthentication(string username, string password) | Sets username and password for HTTP authentication |
| WithTwoFactor(string code) | Provides a two-factor authentication code |
| WithVideoPassword(string password) | Sets a password required to access the video |
| WithAdobePassAuthentication(string mso, string username, string password) | Authenticates using Adobe Pass with the specified MSO, username and password |

**Download Options**

| Method | Description |
|--------|-------------|
| WithConcurrentFragments(int count = 8) | Downloads multiple fragments in parallel (default 8) |
| WithLimitRate(string rate) | Limits download speed (e.g. "50K" or "4.2M") |
| WithThrottledRate(string rate) | Minimum download rate before throttling is applied |
| WithRetries(int maxRetries) | Number of retries for network errors |
| WithFileAccessRetries(int maxRetries) | Number of retries for file access errors |
| WithFragmentRetries(int retries) | Number of retries for fragment downloads |
| WithRetrySleep(string retrySleepExpression) | Custom sleep expression between retries |
| WithRetrySleep(int seconds, string? type = null) | Fixed sleep time between retries |
| WithLinearRetrySleep(int start, int? end = null, int? step = null, string? type = null) | Linear backoff sleep between retries |
| WithExponentialRetrySleep(int start, int? end = null, double? @base = null, string? type = null) | Exponential backoff sleep between retries |
| WithSkipUnavailableFragments() | Skips unavailable fragments instead of aborting |
| WithAbortOnUnavailableFragments() | Aborts download if any fragment is unavailable |
| WithKeepFragments() | Keeps downloaded fragments after merging |
| WithBufferSize(string size) | Sets the size of the download buffer |
| WithNoResizeBuffer() | Disables automatic buffer resizing |
| WithHttpChunkSize(string size) | Sets HTTP chunk size as a string |
| WithHttpChunkSize(long bytes) | Sets HTTP chunk size in bytes |
| WithPlaylistRandom() | Downloads playlist items in random order |
| WithLazyPlaylist() | Processes playlist items only when needed |
| WithNoLazyPlaylist() | Disables lazy playlist processing |
| WithHlsUseMpegts() | Forces HLS downloads to use MPEG-TS container |
| WithNoHlsUseMpegts() | Disables forcing MPEG-TS for HLS |
| WithDownloadSections(string regex) | Downloads only specific sections matching the regex |
| WithDownloader(string downloader) | Specifies the external downloader to use |
| WithDownloader(string downloader, params string[] protocols) | Specifies downloader for particular protocols |
| WithDownloaderArgs(string downloaderName, string args) | Passes additional arguments to a specific downloader |

**Extractor Options**

| Method | Description |
|--------|-------------|
| WithExtractorRetries(int retries) | Number of retries for extractor failures |
| WithInfiniteExtractorRetries() | Retries extractor indefinitely |
| WithAllowDynamicMpd() | Allows dynamic MPD manifests |
| WithIgnoreDynamicMpd() | Ignores dynamic MPD manifests |
| WithHlsSplitDiscontinuity() | Splits HLS streams on discontinuity tags |
| WithNoHlsSplitDiscontinuity() | Does not split HLS on discontinuity tags |
| WithExtractorArgs(string extractorKey, string args) | Passes custom arguments to a specific extractor |

**Filesystem Options**

| Method | Description |
|--------|-------------|
| WithHomeFolder(string path) | Sets the home directory for configuration and cache |
| WithTempFolder(string path) | Sets the temporary directory for downloads |
| WithOutputFolder(string path) | Sets the output directory for finished files |
| WithFFmpegLocation(string path) | Specifies the path to the FFmpeg binary |
| WithOutputTemplate(string template) | Sets the output filename template |
| WithRestrictFilenames() | Restricts filenames to ASCII characters only |
| WithWindowsFilenames() | Forces Windows-compatible filenames |
| WithTrimFilenames(int length) | Trims filenames to the specified maximum length |
| WithNoOverwrites() | Prevents overwriting existing files |
| WithForceOverwrites() | Forces overwriting of existing files |
| WithNoContinue() | Disables resuming of partially downloaded files |
| WithNoPart() | Does not use .part files during download |
| WithMtime() | Sets the file modification time from the video metadata |
| WithWriteDescription() | Writes the video description to a separate file |
| WithWriteInfoJson() | Writes video metadata to an .info.json file |
| WithNoWritePlaylistMetafiles() | Disables writing of playlist metadata files |
| WithNoCleanInfoJson() | Keeps the .info.json file after download |
| WriteComments() | Writes video comments to a separate file |
| WithNoWriteComments() | Disables writing of comments |
| WithLoadInfoJson(string path) | Loads video information from an existing .info.json file |
| WithCookiesFile(string path) | Loads cookies from the specified file |
| WithCookiesFromBrowser(string browser) | Loads cookies from the specified browser |
| WithNoCacheDir() | Disables the use of a cache directory |
| WithRemoveCacheDir() | Removes the cache directory after use |

**General Options**

| Method | Description |
|--------|-------------|
| WithIgnoreErrors() | Continues on download/extraction errors |
| WithAbortOnError() | Aborts on the first error |
| WithIgnoreConfig() | Ignores configuration files |
| WithConfigLocations(string path) | Specifies location of configuration files |
| WithPluginDirs(string path) | Adds a directory to search for plugins |
| WithNoPluginDirs(string path) | Removes a directory from the plugin search path |
| WithJsRuntime(Runtime runtime, string runtimePath) | Sets the JavaScript runtime to use |
| WithNoJsRuntime() | Disables the JavaScript runtime |
| WithRemoteComponent(string component) | Enables a specific remote component |
| WithRemoteComponents(params string[] components) | Enables multiple remote components |
| WithNoRemoteComponents() | Disables all remote components |
| WithFlatPlaylist() | Lists playlist entries without extracting full info |
| WithLiveFromStart() | Downloads live streams from the beginning |
| WithWaitForVideo(TimeSpan? maxWait = null) | Waits for a scheduled video to become available |
| WithMarkWatched() | Marks the video as watched on the platform |
| WithNoMarkWatched() | Does not mark the video as watched |
| WithColor(string policy, string? stream = null) | Controls colored output |
| WithCompatOptions(string options) | Enables compatibility options |
| WithAlias(string alias, string options) | Defines a custom option alias |
| WithPresetAlias(string preset) | Applies a named preset alias |
| WithPresetAlias(YtdlpPreset preset) | Applies a predefined YtdlpPreset |

**Geo-restriction Options**

| Method | Description |
|--------|-------------|
| WithGeoVerificationProxy(string url) | Uses a proxy for geo-verification |
| WithGeoBypassCountry(string countryCode) | Bypasses geo-restriction as if from the given country |

**Network Options**

| Method | Description |
|--------|-------------|
| WithProxy(string? proxy) | Sets an HTTP/HTTPS/SOCKS proxy |
| WithSocketTimeout(TimeSpan timeout) | Sets the network socket timeout |
| WithSourceAddress(string ipAddress) | Binds to a specific local IP address |
| WithImpersonate(string? client) | Impersonates a specific client (browser/device) |
| WithImpersonateAny() | Impersonates a random supported client |
| WithForceIpv4() | Forces the use of IPv4 |
| WithForceIpv6() | Forces the use of IPv6 |
| WithEnableFileUrls() | Allows downloading from file:// URLs |

**Post-Processing Options**

| Method | Description |
|--------|-------------|
| WithExtractAudio(string format, int quality = 5) | Extracts audio in the specified format and quality |
| WithRemuxVideo(string format) | Remuxes video into the specified container (e.g. "mp4" or "mp4>mkv") |
| WithRecodeVideo(string format, string? videoCodec = null, string? audioCodec = null) | Recodes video using the given format and optional codecs |
| WithPostprocessorArgs(PostProcessors postprocessor, string args) | Passes arguments to a specific post-processor |
| WithKeepVideo() | Keeps the original video file after post-processing |
| WithNoPostOverwrites() | Prevents overwriting of post-processed files |
| WithEmbedSubtitles() | Embeds subtitles into the video file |
| WithEmbedThumbnail() | Embeds the thumbnail into the video file |
| WithEmbedMetadata() | Embeds metadata into the video file |
| WithEmbedChapters() | Embeds chapter information into the video file |
| WithEmbedInfoJson() | Embeds the .info.json data into the video file |
| WithNoEmbedInfoJson() | Disables embedding of .info.json data |
| WithReplaceInMetadata(string field, string regex, string replacement) | Replaces text in a metadata field using regex |
| WithConcatPlaylist(string policy = "always") | Concatenates playlist items according to the policy |
| WithFFmpegLocation(string? ffmpegPath) | Sets the path to FFmpeg (post-processing overload) |
| WithConvertSubtitles(string format = "none") | Converts subtitles to the specified format |
| WithConvertThumbnails(string format = "jpg") | Converts thumbnails to the specified format |
| WithSplitChapters() | Splits the video into separate files by chapters |
| WithRemoveChapters(string regex) | Removes chapters matching the given regex |
| WithForceKeyframesAtCuts() | Forces keyframes at cut points during processing |
| WithUsePostProcessor(PostProcessors postProcessor, string? postProcessorArgs = null) | Enables a specific post-processor with optional arguments |

**SponsorBlock Options**

| Method | Description |
|--------|-------------|
| WithSponsorblockMark(string categories = "all") | Marks SponsorBlock segments of the given categories |
| WithSponsorblockRemove(string categories = "all") | Removes SponsorBlock segments of the given categories |
| WithNoSponsorblock() | Disables SponsorBlock integration |

**Subtitle Options**

| Method | Description |
|--------|-------------|
| WithSubtitles(string languages = "all", bool auto = false) | Downloads subtitles for the specified languages (optionally including auto-generated) |

**Thumbnail Options**

| Method | Description |
|--------|-------------|
| WithThumbnails(bool allSizes = false) | Downloads the video thumbnail (all sizes if true) |

**Verbosity and Simulation Options**

| Method | Description |
|--------|-------------|
| WithQuiet() | Suppresses most output messages |
| WithNoWarnings() | Suppresses warning messages |
| WithSimulate() | Simulates the download without actually downloading |
| WithNoSimulate() | Disables simulation mode |
| WithSkipDownload() | Extracts information but skips the actual download |
| WithVerbose() | Enables verbose output |

**Video Selection**

| Method | Description |
|--------|-------------|
| WithPlaylistItems(string items) | Downloads only the specified playlist items (e.g. "1,3-5") |
| WithPlaylistItems(params int[] indices) | Downloads only the specified playlist indices |
| WithPlaylistRange(int? start = null, int? stop = null, int? step = null) | Downloads a range of playlist items |
| WithMinFileSize(string size) | Downloads only files larger than the specified size |
| WithMaxFileSize(string size) | Downloads only files smaller than the specified size |
| WithDate(string date) | Downloads only videos matching the exact date |
| WithDateBefore(string date) | Downloads only videos uploaded before the date |
| WithDateAfter(string date) | Downloads only videos uploaded after the date |
| WithMatchFilter(string filterExpression) | Applies a custom match filter expression |
| WithNoMatchFilters() | Disables all match filters |
| WithBreakMatchFilter(string filter) | Stops downloading when the match filter is no longer satisfied |
| WithNoBreakMatchFilters() | Disables break-on-match-filter behavior |
| WithNoPlaylist() | Downloads only the single video, not the whole playlist |
| WithYesPlaylist() | Forces downloading the whole playlist |
| WithAgeLimit(int years) | Skips videos restricted by age limit |
| WithDownloadArchive(string archivePath = "archive.txt") | Records downloaded videos in an archive file |
| WithNoDownloadArchive() | Disables the download archive |
| WithMaxDownloads(int count) | Limits the maximum number of videos to download |
| WithBreakOnExisting() | Stops when an already-downloaded video is encountered |
| WithNoBreakOnExisting() | Continues even if videos already exist in the archive |
| WithBreakPerInput() | Applies break conditions per input URL |
| WithNoBreakPerInput() | Applies break conditions across all inputs |
| WithSkipPlaylistAfterErrors(int allowedFailures) | Skips the rest of the playlist after too many errors |

**Video Format Options**

| Method | Description |
|--------|-------------|
| WithFormat(string format) | Selects the video/audio format using yt-dlp format syntax |
| WithMergeOutputFormat(string format) | Sets the container format used when merging video and audio |

**Workarounds**

| Method | Description |
|--------|-------------|
| WithAddHeader(string header, string value) | Adds a custom HTTP header to all requests |
| WithSleepInterval(double seconds, double? maxSeconds = null) | Sleeps between downloads (optionally with a random range) |
| WithSleepSubtitles(double seconds) | Sleeps before downloading each subtitle |

**Downloaders**

| Method | Description |
|--------|-------------|
| WithAria2(int connections = 16) | Uses aria2c as the external downloader with the given number of connections |
| WithHlsNative() | Forces the use of the native HLS downloader |
| WithFfmpegAsLiveDownloader(string? extraFfmpegArgs = null) | Uses FFmpeg as the live stream downloader |

**Bonus**

| Method | Description |
|--------|-------------|
| With1440pOrBest() | Prefers 1440p or the best available quality |
| With1080pOrBest() | Prefers 1080p or the best available quality |
| With720pOrBest() | Prefers 720p or the best available quality |
| WithMp4PostProcessingPreset() | Applies a preset that outputs MP4 with common post-processing |
| WithMkvOutput() | Forces the final output container to MKV |
| WithMaxHeight(int height) | Limits the maximum video height |
| WithMaxHeightOrBest(int height) | Limits height but falls back to the best available if necessary |
| WithBestVideoPlusBestAudio() | Selects the best video stream + best audio stream |
| WithBestAudioOnly() | Downloads only the best available audio |
| WithNo4k() | Excludes 4K (2160p) and higher resolutions |
| WithBestM4aAudio() | Prefers the best M4A audio stream |

**Advanced Options**

| Method | Description |
|--------|-------------|
| AddFlag(string flag) | Adds a raw command-line flag |
| AddOption(string key, string value) | Adds a raw key-value option |

---

## ⚙️ Customization

If you need specific arguments not covered by the fluent API:

```csharp
ytdlp.AddFlag("--no-check-certificate")
     .AddOption("--external-downloader", "aria2c")
     .DownloadAsync(url);
```

---

## 🔄 Migration Guide: Upgrading to v4

Version 4.0.0 is a major release that refines the API for better maintainability and removes the overhead of manual lifecycle management.

> **Note:** The primary breaking change is the removal of `IDisposable`/`IAsyncDisposable`. You no longer need to dispose of your `Ytdlp` instances.

### 1. Key Changes at a Glance

| Feature | v3.x | v4.x |
| --- | --- | --- |
| **Lifecycle** | Required `IAsyncDisposable` | **No disposal required** |
| **Architecture** | Immutable Fluent API | Immutable Fluent API (Refactored) |
| **Core Process** | `ProcessFactory` | `ProcessFactory` (Refactored) |
| **Core Runner** | `ProbeRunner` `DownloadRunner` | `ProcessRunner` |


### 2. Side-by-Side Comparison

#### ❌ Legacy API (v3)

```csharp
// Previously required disposal
await using var ytdlp = new Ytdlp()
    .WithFormat("best")
    .WithOutputFolder("./downloads");

await ytdlp.DownloadAsync(url);

```

#### ✅ New API (v4)

```csharp
// Cleaner: No disposal required
var ytdlp = new Ytdlp()
    .WithFormat("best")
    .WithOutputFolder("./downloads");

await ytdlp.DownloadAsync(url);

```

### 3. Why the change?

We have streamlined the `Ytdlp` lifecycle. Because the instance does not hold unmanaged resources that require explicit cleanup, we have removed the `IDisposable` and `IAsyncDisposable` interfaces.

* **Cleaner Code:** Your codebase is now free of `await using` or `using` statements for `Ytdlp` instances.
* **Refactored Core:** The internal `ProcessFactory` has been updated and introduce `ProcessRunner` to handle process execution more efficiently without needing to manage the object lifecycle manually.

### 4. Migration Checklist

* [ ] **Remove `await using` or `using`:** Simply delete the disposal keywords where you instantiate `Ytdlp`.
* [ ] **Verify Events:** Ensure event subscriptions are attached to the instance used for the specific execution.

---

## 💡 Notes

- **Dependencies:** Ensure ``yt-dlp`` (and optionally ``FFmpeg``/``FFprobe``) are available on your system path or point to their specific locations via ``WithFfmpegLocation()`` (if configured).
- **Performance:** ``tuneProcess: true`` (default) is enabled for download methods to optimize output buffer management.

---

## 📜 License

MIT License — see [LICENSE](https://github.com/manusoft/Ytdlp.NET/blob/master/LICENSE.md)

**Author:** Manojbabu (ManuHub)   
**Repository:** [Ytdlp.NET](https://github.com/manusoft/Ytdlp.NET)
