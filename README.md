![Static Badge](https://img.shields.io/badge/ytdlp_executable-red) ![NuGet Version](https://img.shields.io/nuget/v/YTDLP-Executable)  ![NuGet Downloads](https://img.shields.io/nuget/dt/YTDLP-Executable)

![Static Badge](https://img.shields.io/badge/ytdlp.NET-red) ![NuGet Version](https://img.shields.io/nuget/v/ytdlp.net)  ![NuGet Downloads](https://img.shields.io/nuget/dt/ytdlp.net)

# Ytdlp.NET
![icon](https://github.com/user-attachments/assets/2147c398-4e0f-43e2-99cb-32b34be7dc2f)

### ClipMate - MAUI.NET App
<img width="986" height="693" alt="5" src="https://github.com/user-attachments/assets/4bdf89e0-c94b-4163-8b9d-8a051bab0cd8" />


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

### Prerequisites

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

## Usage

### Basic Example: Download a Single Video

Download a video with the best quality to a specified folder:

```csharp
using System;
using System.Threading.Tasks;
using YtdlpDotNet;

class Program
{
    static async Task Main()
    {
        var ytdlp = new Ytdlp("yt-dlp", new ConsoleLogger());

        // Subscribe to progress events
        ytdlp.OnProgressMessage += (sender, message) => Console.WriteLine($"Progress: {message}");
        ytdlp.OnCompleteDownload += (sender, message) => Console.WriteLine($"Complete: {message}");
        ytdlp.OnErrorMessage += (sender, message) => Console.WriteLine($"Error: {message}");

        try
        {
            await ytdlp
                .SetFormat("b") // Best quality
                .SetOutputFolder("./downloads")
                .DownloadThumbnails() // Include thumbnails
                .ExecuteAsync("https://www.youtube.com/watch?v=RGg-Qx1rL9U");
        }
        catch (YtdlpException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private class ConsoleLogger : ILogger
    {
        public void Log(LogType type, string message)
        {
            Console.WriteLine($"[{type}] {message}");
        }
    }
}
```

### Example: Batch Download Multiple Videos

Download multiple videos in a batch:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YtdlpDotNet;

class Program
{
    static async Task Main()
    {
        var ytdlp = new Ytdlp("yt-dlp", new ConsoleLogger());

        // Subscribe to events
        ytdlp.OnProgressMessage += (sender, message) => Console.WriteLine($"Progress: {message}");
        ytdlp.OnCompleteDownload += (sender, message) => Console.WriteLine($"Complete: {message}");
        ytdlp.OnErrorMessage += (sender, message) => Console.WriteLine($"Error: {message}");

        var videoUrls = new List<string>
        {
            "https://www.youtube.com/watch?v=RGg-Qx1rL9U",
            "https://www.youtube.com/watch?v=iBVxogg5QwE"
        };

        try
        {
            await ytdlp
                .SetFormat("b")
                .SetOutputFolder("./batch_downloads")
                .ExecuteBatchAsync(videoUrls);
        }
        catch (YtdlpException ex)
        {
            Console.WriteLine($"Failed to batch download: {ex.Message}");
        }
    }

    private class ConsoleLogger : ILogger
    {
        public void Log(LogType type, string message)
        {
            Console.WriteLine($"[{type}] {message}");
        }
    }
}
```

### Example: Get Available Formats

Retrieve available formats for a video:

```csharp
using System;
using System.Threading.Tasks;
using YtdlpDotNet;

class Program
{
    static async Task Main()
    {
        var ytdlp = new Ytdlp("yt-dlp", new ConsoleLogger());

        try
        {
            var formats = await ytdlp.GetAvailableFormatsAsync("https://www.youtube.com/watch?v=RGg-Qx1rL9U");
            foreach (var format in formats)
            {
                Console.WriteLine($"ID: {format.ID}, Extension: {format.Extension}, Resolution: {format.Resolution}, VCodec: {format.VCodec}");
            }
        }
        catch (YtdlpException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private class ConsoleLogger : ILogger
    {
        public void Log(LogType type, string message)
        {
            Console.WriteLine($"[{type}] {message}");
        }
    }
}
```

### Example: Extract Audio with Metadata

Extract audio in MP3 format and embed metadata:

```csharp
using System;
using System.Threading.Tasks;
using YtdlpDotNet;

class Program
{
    static async Task Main()
    {
        var ytdlp = new Ytdlp("yt-dlp", new ConsoleLogger());

        try
        {
            await ytdlp
                .ExtractAudio("mp3")
                .EmbedMetadata()
                .SetOutputFolder("./audio_downloads")
                .ExecuteAsync("https://www.youtube.com/watch?v=RGg-Qx1rL9U");
        }
        catch (YtdlpException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private class ConsoleLogger : ILogger
    {
        public void Log(LogType type, string message)
        {
            Console.WriteLine($"[{type}] {message}");
        }
    }
}
```

## Configuration

- **Custom yt-dlp Path**: Specify a custom path to the `yt-dlp` executable if it’s not in the system PATH:
  ```csharp
  var ytdlp = new Ytdlp("/path/to/yt-dlp");
  ```

- **Custom Logger**: Implement a custom `ILogger` for logging:
  ```csharp
  public class CustomLogger : ILogger
  {
      public void Log(LogType type, string message)
      {
          // Custom logging logic (e.g., write to file or logging framework)
          Console.WriteLine($"[{type}] {message}");
      }
  }
  var ytdlp = new Ytdlp("yt-dlp", new CustomLogger());
  ```

- **Event Subscriptions**: Subscribe to events for real-time feedback:
  ```csharp
  ytdlp.OnProgressDownload += (sender, args) => Console.WriteLine($"Progress: {args.Percent}% of {args.Size}");
  ytdlp.OnError += (message) => Console.WriteLine($"Error: {message}");
  ytdlp.OnCommandCompleted += (success, message) => Console.WriteLine($"Command: {message}");
  ```

## Supported yt-dlp Options

`Ytdlp.NET` supports a wide range of `yt-dlp` options, including:
- `--extract-audio`, `--audio-format`
- `--format`, `--playlist-items`
- `--write-subs`, `--write-thumbnail`
- `--live-from-start`, `--download-sections`
- `--user-agent`, `--cookies`, `--referer`
- And more (see the `Ytdlp` class for full list).

Use `AddCustomCommand` for unsupported options, ensuring they are valid `yt-dlp` commands.

## Error Handling

The library throws `YtdlpException` for errors during execution. Always wrap calls in a try-catch block:

```csharp
try
{
    await ytdlp.ExecuteAsync("https://www.youtube.com/watch?v=invalid");
}
catch (YtdlpException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Notes

- **Thread Safety**: The `Ytdlp.NET` class is not thread-safe. Create a new instance for concurrent operations.
- **Performance**: Batch downloads are executed sequentially. For parallel downloads, manage multiple `Ytdlp.NET` instances.
- **Version Compatibility**: Tested with `yt-dlp` version 2025.06.30 and later. Run `GetVersionAsync` to verify:
  ```csharp
  string version = await ytdlp.GetVersionAsync();
  Console.WriteLine($"yt-dlp version: {version}");
  ```

## Contributing

Contributions are welcome! Please submit issues or pull requests to the [GitHub repository](https://github.com/manusoft/yt-dlp-wrapper). Ensure code follows the project’s style guidelines and includes unit tests.

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/manusoft/yt-dlp-wrapper/blob/master/LICENSE.txt) file for details.

## License

This library is licensed under the MIT License. See LICENSE for more information.
