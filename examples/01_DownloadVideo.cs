using System;
using System.Threading.Tasks;
using ManuHub.Ytdlp.NET;

class Program
{
    static async Task Main()
    {
        await using var ytdlp = new Ytdlp()
            .WithFormat("bestvideo[height<=720]+bestaudio/best")
            .WithOutputFolder("./downloads")
            .WithOutputTemplate("%(title)s [%(resolution)s].%(ext)s")
            .EmbedMetadata()
            .EmbedThumbnail();

        ytdlp.OnProgressDownload += (s, e) =>
            Console.Write($"\rProgress: {e.Percent:F1}%  ETA: {e.ETA}  Speed: {e.Speed}");
        ytdlp.OnCompleteDownload += (s, msg) =>
            Console.WriteLine($"\nDownload completed: {msg}");

        await ytdlp.ExecuteAsync("https://www.youtube.com/watch?v=VIDEO_ID");
    }
}