![Static Badge](https://img.shields.io/badge/Ytdlp.NET-red) ![NuGet Version](https://img.shields.io/nuget/v/Ytdlp.NET)  ![NuGet Downloads](https://img.shields.io/nuget/dt/Ytdlp.NET) ![.NET](https://img.shields.io/badge/.NET%20%7C%208%20%7C%209%20%7C%2010-blueviolet)

# Ytdlp.NET 
> v3

**Ytdlp.NET** is a fluent, strongly-typed .NET wrapper around the powerful [`yt-dlp`](https://github.com/yt-dlp/yt-dlp) command-line tool. It provides an intuitive and customizable interface to download videos, extract audio, retrieve metadata, and process media from YouTube and hundreds of other supported platforms.

Supports 1000+ sites, audio extraction, subtitles, thumbnails, SponsorBlock, cookies, proxies, concurrent fragments, post-processing, and more.

This version introduces a completely refactored API, better event handling, improved concurrency, and robust progress reporting.

> **Note:** v3 is a breaking change from v2. Read the migration guide carefully.

This version introduces an **immutable builder pattern** for safe, reusable configuration + a dedicated command object for execution, real-time progress parsing, rich events, batch support, and powerful probing (metadata & formats without downloading).

---

## ✨ Key Features

- Immutable fluent builder (`YtdlpBuilder`) → safe configuration reuse
- Dedicated `YtdlpCommand` for execution + event-driven progress
- Strongly-typed progress events (%, speed, ETA, size, fragments…)
- Rich post-processing events (merge, ffmpeg actions, fixups…)
- Batch downloads with configurable concurrency
- Probe utilities: formats list, best format IDs, metadata JSON, file size estimation
- Full control over yt-dlp flags, options, paths, JS runtimes, geo-bypass, etc.
- Logging abstraction (`ILogger`) with default console implementation
- Cancellation support & proper process cleanup (`IAsyncDisposable`)

---

## 📦 Installation

```powershell
# Stable (when released)
dotnet add package Ytdlp.NET

# If you're using a pre-release or local build
dotnet add package Ytdlp.NET --prerelease
```

**Requirements**
- .NET 8.0+
- yt-dlp executable in PATH (or provide custom path)
- Recommended: ffmpeg (for merging, audio extraction, thumbnails, chapters…)

---

## 🚀 Quick Start

```csharp
using ManuHub.Ytdlp;

// Simple best-quality video download
var command = Ytdlp.Create()
    .WithOutputFolder("Videos")
    .WithOutputTemplate("%(title)s.%(ext)s")
    .WithFormat("bestvideo+bestaudio/best")
    .Build();

command.OnProgressDownload += (s, e) =>
    Console.WriteLine($"[{e.Percent:F1}%] {e.Speed}  ETA: {e.ETA}");

await command.ExecuteAsync("https://youtu.be/dQw4w9WgXcQ");
```

**Audio-only + embed metadata & thumbnail**

```csharp
var cmd = Ytdlp.Create()
    .WithOutputFolder("Music")
    .WithExtractAudio("mp3", quality: 0)   // 0 = best
    .EmbedThumbnail()
    .EmbedMetadata()
    .EmbedChapters()
    .Build();

await cmd.ExecuteAsync("https://music.youtube.com/watch?v=...");
```

---

## 🛠 Core API

### 1. Ytdlp (static factory)

```csharp
public static YtdlpBuilder Create(string? ytDlpPath = null, ILogger? logger = null)
```

### 2. YtdlpBuilder (immutable fluent configuration)

Chain methods to configure → call `.Build()` to get `YtdlpCommand`

**Common / Most Used**

```csharp
.WithOutputFolder(string path)
.WithOutputTemplate(string template)               // e.g. "%(uploader)s/%(title)s.%(ext)s"
.WithFormat(string format)                         // "bv*+ba", "best", "137+140", ...
.WithConcurrentFragments(int count)
.WithExtractAudio(string format = "best", int quality = 5)  // mp3,aac,opus,...
.EmbedThumbnail()
.EmbedMetadata()
.EmbedChapters()
.WithDownloadSubtitles(string languages = "all", bool auto = false)
.WithCookiesFromBrowser(string browser)            // chrome, firefox, edge,...
.WithProxy(string proxy)                           // http://... or socks5://...
.WithFFmpegLocation(string path)
```

**SponsorBlock**

```csharp
.WithSponsorblockRemove(string categories = "all")     // sponsor,intro,outro,...
.WithSponsorblockMark(string categories = "all")
.NoSponsorblock()
```

**Playlist / Selection**

```csharp
.WithPlaylistItems(string indices)               // "1,3-5,-2::2"
.NoPlaylist()
.YesPlaylist()
.WithDate(string date)                           // "today-2weeks", "YYYYMMDD"
.WithAgeLimit(int years)
```

**Simulation & Verbosity**

```csharp
.Simulate()
.SkipDownload()
.Quiet()
.NoWarnings()
```

**Advanced / Less Common**

- `.WithJsRuntime(string runtime, string path)`
- `.NoJsRuntime()`
- `.WithGeoBypassCountry(string iso2)`
- `.WithGeoVerificationProxy(string url)`
- `.WithMinFileSize(string size)` / `.WithMaxFileSize(...)`
- `.WithRetries(int)` / `.WithFragmentRetries(int)`
- `.KeepFragments()`
- `.WithAddHeader(string header, string value)`
- `.WithAuthentication(string user, string pass)`
- `.WithTwoFactorCode(string code)`
- `.WithRemuxVideo(string format)`
- `.WithReEncodeVideo(...)`
- `.WithPostProcessorArg(string ppName, string args)`
- `.WithReplaceMetadata(string field, string regex, string replacement)`
- `.WithHomeFolder(string)` / `.WithTempFolder(string)`
- `.RestrictFileNames()` / `.WindowsFileNames()` / `.WithTrimFileNames(int)`
- `.WriteVideoDescription()` / `.WriteVideoMetadata()` / `.WriteComments()`
- `.WithLoadVideoMetadata(string jsonFile)`

**Build**

```csharp
YtdlpCommand Build()
YtdlpBuilder Probe()               // shortcut for probe/simulation mode
```

### 3. YtdlpCommand (execution + events)

Implements `IAsyncDisposable` (kills process on dispose if needed)

**Execution**

```csharp
Task ExecuteAsync(string url, CancellationToken ct = default)
Task<List<(string Url, Exception? Error)>> ExecuteBatchAsync(
    IEnumerable<string> urls,
    int maxConcurrency = 4,
    CancellationToken ct = default)
```

**Events** (subscribe before Execute*)

- `OnProgressDownload` → `DownloadProgressEventArgs` (Percent, Size, Speed, ETA, Fragments, Message)
- `OnProgressMessage` / `OnErrorMessage` → string
- `OnPostProcessingStarted` / `OnPostProcessingCompleted` → string
- `OnCompleteDownload` → string
- `OnProcessCompleted` → string

### 4. YtdlpProbe (static – no download)

```csharp
Task<List<Format>> GetFormatsDetailedAsync(string url, YtdlpBuilder? baseBuilder = null, CT ct = default)
Task<List<Format>> GetAvailableFormatsAsync(...)
Task<Metadata?> GetVideoMetadataAsync(...)
Task<string?> GetBestAudioFormatIdAsync(...)
Task<string?> GetBestVideoFormatIdAsync(..., int? maxHeight = null)
Task<string?> GetFileSizeAsync(...)
```

## 📖 More Examples

**Preview generated command (dry-run)**

```csharp
var builder = Ytdlp.Create().WithFormat("best").Simulate();
var cmd = builder.Build();
Console.WriteLine(string.Join(" ", builder.BuildArgs("https://...")));
// or just use --simulate in ExecuteAsync
```

**Batch with progress forwarding**

```csharp
var urls = File.ReadAllLines("urls.txt");

var builder = Ytdlp.Create()
    .WithOutputFolder("Batch")
    .WithExtractAudio("opus")
    .WithConcurrentFragments(3);

var cmd = builder.Build();

cmd.OnProgressDownload += (_, e) => Console.Write($"\r{e.Percent:F1}% {e.Speed}");

var results = await cmd.ExecuteBatchAsync(urls, maxConcurrency: 5);
```

**Choose best 1080p format manually**

```csharp
var formats = await YtdlpProbe.GetFormatsDetailedAsync(url);
var best1080 = formats
    .Where(f => f.Height is >= 1000 and <= 1100)
    .OrderByDescending(f => f.TotalBitrate)
    .FirstOrDefault();

if (best1080 != null)
{
    await Ytdlp.Create()
        .WithFormat(best1080.Id)
        .Build()
        .ExecuteAsync(url);
}
```
---

### Recommended: Use companion NuGet packages (easiest & portable)

We provide official build packages that automatically download and manage the latest stable binaries:

```xml
<ItemGroup>
  <!-- Core yt-dlp binary (recommended) -->
  <PackageReference Include="ManuHub.Ytdlp" Version="*" />

  <!-- FFmpeg + FFprobe for merging, audio extraction, thumbnails, etc. (strongly recommended) -->
  <PackageReference Include="ManuHub.FFmpeg" Version="*" />
  <PackageReference Include="ManuHub.FFprobe" Version="*" />

  <!-- Deno runtime — only needed if you use advanced JavaScript extractor features -->
  <!-- <PackageReference Include="ManuHub.Deno" Version="*" /> -->
</ItemGroup>
```

```csharp
var ytdlp = new Ytdlp(ytDlpPath: @"\Tools\yt-dlp.exe");
```

---

### ⚙️ Configuration & Options

## Full Fluent API Reference – All `.WithXxx()` Methods (as of March 2026)

### General / Core

| Method                              | Description                                                                 | Example / Notes                              |
|-------------------------------------|-----------------------------------------------------------------------------|----------------------------------------------|
| `.WithYtDlpPath(string path)`       | Custom path to yt-dlp executable                                            | `.WithYtDlpPath("yt-dlp-nightly.exe")`       |
| `.WithLogger(ILogger logger)`       | Custom logger implementation                                                | Use Serilog, Microsoft.Extensions.Logging…   |
| `.Probe()`                          | Sets probe mode (no download, metadata only)                                | Used internally by `YtdlpProbe` helpers      |
| `.AddFlag(string flag)`             | Add any raw yt-dlp flag (e.g. `--no-colors`)                                | Escape hatch for unmapped options            |
| `.AddOption(string key, string? value)` | Add any raw `--key value` pair                                           | `.AddOption("--sleep-interval", "3")`        |

### Output & Filesystem

| Method                              | Description                                                                 | Example                                      |
|-------------------------------------|-----------------------------------------------------------------------------|----------------------------------------------|
| `.WithOutputFolder(string folder)`  | Where to save downloaded files                                              | `.WithOutputFolder(@"C:\Videos")`            |
| `.WithOutputTemplate(string tpl)`   | Filename template                                                           | `"%(uploader)s - %(title)s [%(id)s].%(ext)s"`|
| `.WithHomeFolder(string path)`      | yt-dlp home/config directory (`--paths home:`)                              | Custom config/cache location                 |
| `.WithTempFolder(string path)`      | Temporary files directory (`--paths temp:`)                                 | Useful on slow/network drives                |
| `.RestrictFileNames()`              | ASCII-only, no spaces/& etc.                                                | `--restrict-filenames`                       |
| `.WindowsFileNames()`               | Force Windows-safe filenames                                                | `--windows-filenames`                        |
| `.WithTrimFileNames(int length)`    | Limit filename length (without extension)                                   | `.WithTrimFileNames(100)`                    |
| `.NoFileOverwrites()`               | Never overwrite existing files                                              | `--no-overwrites`                            |
| `.NoPartFile()`                     | Write directly (no `.part` files)                                           | `--no-part`                                  |
| `.ModificationTime()`               | Set file mtime from Last-Modified header                                    | `--mtime`                                    |

### Format & Quality Selection

| Method                              | Description                                                                 | Example                                      |
|-------------------------------------|-----------------------------------------------------------------------------|----------------------------------------------|
| `.WithFormat(string format)`        | Format selector (most important option!)                                    | `"bv*+ba/b"`, `"bestvideo[height<=?1080]+bestaudio/best"` |
| `.WithConcurrentFragments(int n)`   | DASH/HLS concurrent fragment downloads                                      | `.WithConcurrentFragments(8)`                |

### Network & Proxy

| Method                              | Description                                                                 | Example                                      |
|-------------------------------------|-----------------------------------------------------------------------------|----------------------------------------------|
| `.WithProxy(string proxy)`          | HTTP/SOCKS proxy                                                            | `"socks5://127.0.0.1:1080"`                  |
| `.WithSocketTimeout(TimeSpan ts)`   | Connection timeout                                                          | `TimeSpan.FromSeconds(30)`                   |
| `.ForceIpv4()` / `.ForceIpv6()`     | Force IPv4 or IPv6 only                                                     | Usually `.ForceIpv4()` on bad IPv6 networks  |
| `.WithGeoVerificationProxy(string)` | Proxy used only for geo-checks                                              | Separate proxy just for IP verification      |
| `.WithGeoBypassCountry(string cc)`  | Fake X-Forwarded-For country (2-letter ISO code)                            | `"JP"`, `"DE"`                               |

### Authentication & Cookies

| Method                              | Description                                                                 | Example                                      |
|-------------------------------------|-----------------------------------------------------------------------------|----------------------------------------------|
| `.WithCookiesFile(string path)`     | Netscape-format cookies.txt                                                 | `--cookies cookies.txt`                      |
| `.WithCookiesFromBrowser(string)`   | Load cookies from browser (chrome, firefox, edge, brave…)                   | `"chrome"`, `"firefox:profile_name"`         |
| `.WithAuthentication(string u, string p)` | Username + password login                                             | Age-restricted / private content             |
| `.WithTwoFactorCode(string code)`   | 2FA code (usually used together with above)                                 | One-time use                                 |

### Subtitles

| Method                              | Description                                                                 | Example                                      |
|-------------------------------------|-----------------------------------------------------------------------------|----------------------------------------------|
| `.WithDownloadSubtitles(string langs = "all", bool auto = false)` | Download .vtt/.srt subs                                      | `"en,es"`, `"all"`                           |
| `.WithEmbedSubtitles(string langs, string? convertTo = null)` | Embed subs into video (mp4/webm/mkv)                          | `convertTo: "srt"` or `"embed"`              |

### Thumbnails & Metadata

| Method                              | Description                                                                 | Example                                      |
|-------------------------------------|-----------------------------------------------------------------------------|----------------------------------------------|
| `.WithWriteThumbnails(bool allSizes = false)` | Save thumbnail(s) to disk                                    | `allSizes: true` → all resolutions           |
| `.EmbedThumbnail()`                 | Embed thumbnail as cover art                                                | Requires ffmpeg                              |
| `.EmbedMetadata()`                  | Embed title/artist/etc into file                                            | `--embed-metadata`                           |
| `.EmbedChapters()`                  | Add chapter markers                                                         | `--embed-chapters`                           |
| `.WriteVideoDescription()`          | Save .description file                                                      | `--write-description`                        |
| `.WriteVideoMetadata()`             | Save .info.json                                                             | `--write-info-json`                          |
| `.WriteComments()`                  | Include comments in .info.json                                              | `--write-comments`                           |

### Audio Extraction / Post-Processing

| Method                              | Description                                                                 | Example                                      |
|-------------------------------------|-----------------------------------------------------------------------------|----------------------------------------------|
| `.WithExtractAudio(string format = "best", int quality = 5)` | Convert to audio-only (mp3, opus, m4a…)                     | `"mp3", quality: 0` (best)                   |
| `.WithRemuxVideo(string format = "mp4")` | Remux container without re-encoding                                 | `"mkv"`, `"mp4"`                             |
| `.EmbedInfoJson()`                  | Attach .info.json as mkv attachment                                         | Useful for archiving                         |
| `.KeepVideo()`                      | Keep intermediate video after audio extraction                              | `-k`                                         |
| `.WithFFmpegLocation(string path)`  | Custom ffmpeg/ffprobe location                                              | `"C:\\Tools\\ffmpeg\\bin"`                   |

### SponsorBlock Integration

| Method                              | Description                                                                 | Example                                      |
|-------------------------------------|-----------------------------------------------------------------------------|----------------------------------------------|
| `.WithSponsorblockMark(string cats = "all")` | Create chapters for segments                                 | `"all,-preview,-interaction"`                |
| `.WithSponsorblockRemove(string cats)` | Cut out segments from final file                                 | `"sponsor,intro,outro"`                      |
| `.NoSponsorblock()`                 | Disable SponsorBlock completely                                             | `--no-sponsorblock`                          |

### Download Behavior & Limits

| Method                              | Description                                                                 | Example                                      |
|-------------------------------------|-----------------------------------------------------------------------------|----------------------------------------------|
| `.WithMinFileSize(string size)`     | Skip if smaller than (e.g. "50M")                                           | `"100M"`, `"1.5G"`                           |
| `.WithMaxFileSize(string size)`     | Skip if larger than                                                         |                                              |
| `.WithMaxDownloads(int n)`          | Stop after n files                                                          | Useful in playlists                          |
| `.WithRetries(int n)`               | Retry count (-1 = infinite)                                                 | `.WithRetries(20)`                           |
| `.WithLimitRate(string rate)`       | Throttle download speed                                                     | `"3M"`, `"500K"`                             |
| `.WithDate(string date)`            | Only videos from specific date                                              | `"today-1month"`, `"20250101"`               |
| `.WithPlaylistItems(string range)`  | Playlist index/range ("1,3-7,-5::2")                                        | Advanced slicing                             |
| `.FlatPlaylist()`                   | Don't extract playlist entries                                              | Faster listing                               |
| `.NoPlaylist()` / `.YesPlaylist()`  | Force single video or force playlist                                        | When URL can be both                         |

### JavaScript / Workarounds (2025+ heavy usage)

| Method                              | Description                                                                 | Example                                      |
|-------------------------------------|-----------------------------------------------------------------------------|----------------------------------------------|
| `.WithJsRuntime(string runtime, string path)` | Enable JS engine (deno, node, quickjs, bun)                   | `"deno:/opt/deno"`                           |
| `.NoJsRuntime()`                    | Disable all JavaScript execution                                            | Force no-JS mode                             |
| `.WithAddHeader(string h, string v)`| Custom HTTP header                                                          | `"Referer:https://example.com"`              |

### Simulation & Verbosity

| Method                  | Description                                      | Typical use-case                     |
|-------------------------|--------------------------------------------------|--------------------------------------|
| `.Simulate()`           | Don't download anything                          | Used in probes                       |
| `.SkipDownload()`       | Download metadata/files but skip video           | `--skip-download`                    |
| `.Quiet()`              | Suppress most output                             | Cleaner logs                         |
| `.NoWarnings()`         | Hide warning messages                            |                                      |

---

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

---

## Migration Guide v2 → v3

* **Namespaces and class names remain the same**, but the API is fully async and event-driven.
* Old synchronous methods are removed.
* `ExecuteAsync` replaces the old `Run` / `Download` methods.
* Batch operations are fully asynchronous with concurrency limits.
* Event handling is centralized through `YtdlpCommand`.

---

## Event Handling & Progress Reporting

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

## Notes & Tips

* Use `ExecuteBatchAsync` for multiple downloads.
* Always unsubscribe from events if using long-lived `YtdlpCommand` instances.
* Set `ConcurrentFragments` for faster downloads on multi-core systems.
* Ensure `yt-dlp` is updated for best compatibility.

---

## Changelog / Breaking Changes

### v3.0.0

* Fully async API
* Consolidated `ExecuteAsync` method
* Event-driven progress and messages
* Batch execution with concurrency
* Old v2 synchronous methods removed
* `BuildArgs` moved to `YtdlpBuilder`
* Breaking API changes require code updates

---

## 🤝 Contributing & Roadmap

Contributions are welcome! Please submit issues or pull requests to the [GitHub repository](https://github.com/manusoft/yt-dlp-wrapper). Ensure code follows the project’s style guidelines and includes unit tests.

- Target .NET 8+
- Add XML docs to more methods
- Unit tests for builder arg generation & progress parser
- Planned: more typed models (`Format`, `Metadata`), better batch progress aggregation

---

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/manusoft/yt-dlp-wrapper/blob/master/LICENSE.txt) file for details.

---

**Author:** Manojbabu (ManuHub)
**Repository:** [Ytdlp.NET](https://github.com/manusoft/yt-dlp-wrapper)
