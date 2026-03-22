using System;
using System.Threading.Tasks;
using ManuHub.Ytdlp.NET;

class Program
{
    static async Task Main()
    {
        await using var ytdlp = new Ytdlp();

        var url = "https://www.youtube.com/watch?v=VIDEO_ID";
        var metadata = await ytdlp.GetMetadataAsync(url);

        if (metadata != null)
        {
            Console.WriteLine($"Type: {metadata.Type}"); // Playlist / Video
            Console.WriteLine($"Title: {metadata.Title}");
            Console.WriteLine($"Duration: {metadata.DurationTimeSpan}");
            Console.WriteLine($"Uploader: {metadata.Uploader}");
            Console.WriteLine($"Views: {metadata.ViewCount:N0}");
            Console.WriteLine($"Categories: {string.Join(", ", metadata.Categories ?? Array.Empty<string>())}");
        }
    }
}