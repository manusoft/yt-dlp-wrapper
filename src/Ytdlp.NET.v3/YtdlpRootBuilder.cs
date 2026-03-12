namespace ManuHub.Ytdlp;

public sealed class YtdlpRootBuilder
{
    private readonly string? _path;
    private readonly ILogger? _logger;

    internal YtdlpRootBuilder(string? path, ILogger? logger)
    {
        _path = path;
        _logger = logger;
    }

    public YtdlpGeneral General() => new(_path, _logger);

    public YtdlpProbe Probe(string url) => new(url, _path, _logger);

    public YtdlpBuilder Download() => new(_path, _logger);
}
