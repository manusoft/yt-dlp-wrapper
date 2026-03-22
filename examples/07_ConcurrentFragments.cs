using System;
using System.Threading.Tasks;
using ManuHub.Ytdlp.NET;

class Program
{
    static async Task Main()
    {
        var url = "https://www.youtube.com/watch?v=VIDEO_ID";

        await using var ytdlp = new Ytdlp()
            .WithConcurrentFragments(8)
            .WithFormat("best")
            .WithOutputFolder("./downloads");

        await ytdlp.ExecuteAsync(url);
    }
}