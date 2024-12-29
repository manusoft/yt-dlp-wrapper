namespace YtDlpWrapper;

public static class StringExtensions
{
    // Get the video format code based on the quality
    public static string GetVideoFormatCode(VideoQuality quality)
    {
        return quality switch
        {
            VideoQuality.All => "all",
            VideoQuality.MergeAll => "mergeall",
            VideoQuality.Best => "best",
            VideoQuality.BestVideo => "bestvideo",
            VideoQuality.Worst => "worst",           
            VideoQuality.WorstVideo => "worstvideo",            
            _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, "Unknown video quality")
        };
    }

    // Get the audio format code based on the quality
    public static string GetAudioFormatCode(AudioQuality quality)
    {
        return quality switch
        {
            AudioQuality.BestAudio => "bestaudio",
            AudioQuality.WorstAudio => "worstaudio",
            _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, "Unknown audio quality")
        };
    }

    // Get the error message
    public static string GetErrorMessage(string message)
    {
        if (message.Contains("Requested format is not available"))
        {
            return "Requested format is not available";
        }
        else if (message.Contains("This video is unavailable"))
        {
            return "This video is unavailable";
        }
        else if (message.Contains("This video is private"))
        {
            return "This video is private";
        }
        else if (message.Contains("This video is age-restricted"))
        {
            return "This video is age-restricted";
        }
        else if (message.Contains("This video is not available in your country"))
        {
            return "This video is not available in your country";
        }
        else if (message.Contains("This video requires payment to watch"))
        {
            return "This video requires";
        }
        else if (message.Contains("[generic] Extracting URL:"))
        {
            return "Invalid URL!";
        }
        else if (message.Contains("Postprocessing"))
        {
            return "Postprocessing: ffprobe and ffmpeg not found. Please install";
        }       
        else
        {
            return "Unknown error";
        }
    }
}
