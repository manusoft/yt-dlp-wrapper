namespace ManuHub.Ytdlp;

/// <summary>
/// Static factory for the v3 fluent API
/// </summary>
public static class Ytdlp
{
    public static YtdlpBuilder Create(string? ytDlpPath = null, ILogger? logger = null) => new YtdlpBuilder(ytDlpPath, logger);
}