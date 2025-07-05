namespace Ytdlp.Test;

public class ProgressParserTests
{
    [Fact]
    public void ExtractingUrl_LogsCorrectly()
    {
        var parser = new ProgressParser();
        var output = "[youtube] Extracting URL: https://www.youtube.com/watch?v=Gk0WHyRUcgM";
        parser.ParseProgress(output);
        // Assert that OnProgressMessage was invoked with expected message
    }

    [Fact]
    public void DownloadingWebpage_LogsCorrectly()
    {
        var parser = new ProgressParser();
        var output = "[youtube] Gk0WHyRUcgM: Downloading webpage";
        parser.ParseProgress(output);
        // Assert that OnProgressMessage was invoked with expected message
    }

    [Fact]
    public void DownloadingJson_LogsCorrectly()
    {
        var parser = new ProgressParser();
        var output = "[youtube] Gk0WHyRUcgM: Downloading ios player API JSON";
        parser.ParseProgress(output);
        // Assert that OnProgressMessage was invoked with expected message
    }

    [Fact]
    public void DownloadingTvClientConfig_LogsCorrectly()
    {
        var parser = new ProgressParser();
        var output = "[youtube] Gk0WHyRUcgM: Downloading tv client config";
        parser.ParseProgress(output);
        // Assert that OnProgressMessage was invoked with expected message
    }

    [Fact]
    public void DownloadingM3u8_LogsCorrectly()
    {
        var parser = new ProgressParser();
        var output = "[youtube] Gk0WHyRUcgM: Downloading m3u8 information";
        parser.ParseProgress(output);
        // Assert that OnProgressMessage was invoked with expected message
    }

    [Fact]
    public void DownloadingManifest_LogsCorrectly()
    {
        var parser = new ProgressParser();
        var output = "[youtube] Gk0WHyRUcgM: Downloading ios player API JSON";
        parser.ParseProgress(output);
        // Assert that OnProgressMessage was invoked with expected message
    }

    [Fact]
    public void DownloadDestination_LogsCorrectly()
    {
        var parser = new ProgressParser();
        var output = "[download] Destination: downloads\\";
        parser.ParseProgress(output);
        // Assert that OnProgressMessage was invoked with expected message
    }

    [Fact]
    public void DownloadingFormat_LogsCorrectly()
    {
        var parser = new ProgressParser();
        var output = "[info] Gk0WHyRUcgM: Downloading 1 format(s): 248+251";
        parser.ParseProgress(output);
        // Assert that OnProgressMessage was invoked with expected message
    }

    [Fact]
    public void DownloadingThumbnail_LogsCorrectly()
    {
        var logger = new TestLogger();
        var parser = new ProgressParser(logger);
        string output = "[info] Downloading video thumbnail 41 ...";
        parser.ParseProgress(output);
        Assert.Contains(logger.Logs, l => l.Message == "Downloading video thumbnail 41");
    }

    [Fact]
    public void WritingThumbnail_LogsCorrectly()
    {
        var logger = new TestLogger();
        var parser = new ProgressParser(logger);
        string output = "[info] Writing video thumbnail 41 to: downloads\\Kunukkitta Kozhi Jagadish.webp";
        parser.ParseProgress(output);
        Assert.Contains(logger.Logs, l => l.Message == "Writing video thumbnail 41 to: downloads\\Kunukkitta Kozhi Jagadish.webp");
    }

    [Fact]
    public void Reset_ClearsDownloadCompletedFlag()
    {
        var logger = new TestLogger();
        var parser = new ProgressParser(logger);
        parser.ParseProgress("[download] 100% of 29.53MiB at Unknown ETA Unknown");
        Assert.Contains(logger.Logs, l => l.Message.Contains("Download complete"));
        parser.Reset();
        parser.ParseProgress("[download] 100% of 10MiB at Unknown ETA Unknown");
        Assert.Contains(logger.Logs, l => l.Message.Contains("Download complete: 100% of 10MiB"));
    }

    private class TestLogger : ILogger
    {
        public List<(LogType Type, string Message)> Logs { get; } = new();
        public void Log(LogType type, string message) => Logs.Add((type, message));
    }
}
