![NuGet Version](https://img.shields.io/nuget/v/YTDLP-Wrapper) ![NuGet Downloads](https://img.shields.io/nuget/dt/YTDLP-Wrapper)

# YTDLP-Wrapper

![icon](https://github.com/user-attachments/assets/3848b748-ef25-4e28-9163-b7dba7e42315)

![Screenshot 2025-01-03 013123](https://github.com/user-attachments/assets/24231e54-6a4d-45fb-8237-be022bbb516a)


`YTDLP-Wrapper` is a C# wrapper around the popular `yt-dlp` command-line tool for downloading videos, audio, subtitles, thumbnails, and more from various video-sharing platforms. This wrapper provides a simple and easy-to-use API for interacting with `yt-dlp` in your C# projects.

## Features

- **Download Videos** - Download videos in various qualities.
- **Download Audio** - Download audio from videos in high quality.
- **Download Subtitles** - Download subtitles for videos.
- **Download Thumbnails** - Download video thumbnails.
- **Download Playlists** - Download all videos from a playlist.
- **Get Video/Playlist Info** - Retrieve information about videos or playlists.
- **Get Subtitles** - Retrieve subtitles of video. (stored in App base path)
- **Get Available Formats** - Get a list of available video formats for a given video.
- **Get Thumbnail** - Retrieve thumbnail of a video. (stored in App base path)

## Installation

You can install `YTDLP-Wrapper` from NuGet:

```bash
dotnet add package YTDLP-Wrapper
```

Or use the NuGet Package Manager in Visual Studio.

## Usage

### Initialize the Engine

You can create an instance of the `YtDlpEngine` class with the path to `yt-dlp.exe` (optional, defaults to `"yt-dlp.exe"`).

```csharp
var ytDlpEngine = new YtDlpEngine();
```

### Download a Video

To download a video, specify the video URL and the output directory.

```csharp
await ytDlpEngine.DownloadVideoAsync("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "C:\\Downloads");
```

### Download a Playlist

To download all videos from a playlist:

```csharp
await ytDlpEngine.DownloadPlaylistAsync("https://www.youtube.com/playlist?list=PL4cUxeGkcC9iZ1eqI2gR8SjlzzyLw60EF", "C:\\Downloads");
```

### Download Audio

To download only the audio of a video:

```csharp
await ytDlpEngine.DownloadAudioAsync("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "C:\\Downloads");
```

### Download Subtitles

To download subtitles for a video:

```csharp
await ytDlpEngine.DownloadSubtitlesAsync("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "C:\\Downloads");
```

### Download Thumbnail

To download the thumbnail of a video:

```csharp
await ytDlpEngine.DownloadThumbnailAsync("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "C:\\Downloads");
```

### Get Video Information

To retrieve information about a video:

```csharp
var videoInfo = await ytDlpEngine.GetVideoInfoAsync("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
```

### Get Available Formats

To retrieve available formats for a video:

```csharp
var formats = await ytDlpEngine.GetAvailableFormatsAsync("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
```

### Events

The `YtDlpEngine` provides events that you can subscribe to for progress updates or error handling.

- **OnProgressDownload**: Fired when there is a download progress update.
- **OnProgressMessage**: Fired for general progress messages (e.g., logging).
- **OnErrorMessage**: Fired when an error occurs during the download.

Example:

```csharp
ytDlpEngine.OnProgressDownload += (sender, args) =>
{
    Console.WriteLine($"Download progress: {args.Progress}%");
};

ytDlpEngine.OnErrorMessage += (sender, message) =>
{
    Console.WriteLine($"Error: {message}");
};
```

## Enum Definitions

The following enums are available to specify download quality:

```csharp
public enum VideoQuality
{
    All,           // All available quality
    MergeAll,      // Merge all available formats
    Best,          // Best available quality
    BestVideo,     // Best video-only quality (no audio)
    Worst,         // Worst available quality    
    WorstVideo,    // Worst video-only quality (no audio)   
}

public enum AudioQuality
{
    BestAudio,     // Best audio-only quality (no video)
    WorstAudio,    // Worst audio-only quality (no video)
}
```

## License

This library is licensed under the MIT License. See LICENSE for more information.
