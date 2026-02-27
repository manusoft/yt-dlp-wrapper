namespace VideoDownloader.Cache;

public static class MetadataCache
{
    private static readonly Dictionary<string, object> _cache = new();

    public static bool TryGet(string url, out object? data)
        => _cache.TryGetValue(url, out data);

    public static void Store(string url, object data)
        => _cache[url] = data;
}