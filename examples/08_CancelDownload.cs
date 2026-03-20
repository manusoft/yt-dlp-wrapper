using System;
using System.Threading;
using System.Threading.Tasks;
using ManuHub.Ytdlp.NET;

class Program
{
    static async Task Main()
    {
        var url = "https://www.youtube.com/watch?v=VIDEO_ID";
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        await using var ytdlp = new Ytdlp()
            .WithFormat("best")
            .WithOutputFolder("./downloads");

        try
        {
            await ytdlp.ExecuteAsync(url, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Download cancelled after 15 seconds.");
        }
    }
}