using System;
using System.Threading.Tasks;
using ManuHub.Ytdlp.NET;

class Program
{
    static async Task Main()
    {
        var url = "https://www.youtube.com/watch?v=VIDEO_ID";

        await using var ytdlp = new Ytdlp();

        string bestVideo = await ytdlp.GetBestVideoFormatIdAsync(url, maxHeight: 1080);
        string bestAudio = await ytdlp.GetBestAudioFormatIdAsync(url);

        await ytdlp
            .WithFormat($"{bestVideo}+{bestAudio}/best")
            .WithOutputFolder("./downloads")
            .WithOutputTemplate("%(title)s [%(resolution)s - %(id)s].%(ext)s")
            .EmbedMetadata()
            .EmbedThumbnail()
            .ExecuteAsync(url);
    }
}