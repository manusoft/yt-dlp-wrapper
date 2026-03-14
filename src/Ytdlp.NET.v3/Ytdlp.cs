namespace ManuHub.Ytdlp;

/// <summary>
/// Static factory for the v3 fluent API
/// </summary>
public static class Ytdlp
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ytDlpPath"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static YtdlpRootBuilder Create(string? ytDlpPath = null, ILogger? logger = null)
        => new YtdlpRootBuilder(ytDlpPath, logger);    
}