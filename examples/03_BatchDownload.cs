using System;
using System.Threading.Tasks;
using ManuHub.Ytdlp.NET;

class Program
{
    static async Task Main()
    {
        var urls = new[]
        {
            "https://www.youtube.com/watch?v=VID1",
            "https://www.youtube.com/watch?v=VID2",
            "https://www.youtube.com/watch?v=VID3"
        };

        await using var ytdlp = new Ytdlp()
            .WithFormat("best[height<=480]")
            .WithOutputFolder("./batch");

        ytdlp.OnProgressDownload += (s, e) =>
            Console.WriteLine($"{e.Percent:F1}% - {e.Speed} - ETA {e.ETA}");

        await ytdlp.ExecuteBatchAsync(urls, maxConcurrency: 3);
    }
}