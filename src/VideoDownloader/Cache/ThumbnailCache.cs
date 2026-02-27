namespace VideoDownloader.Cache;

public static class ThumbnailCache
{
    private static readonly Dictionary<string, Image> _cache = new();

    public static Image? Get(string path)
    {
        if (_cache.TryGetValue(path, out var img))
            return img;

        if (!File.Exists(path))
            return null;

        img = Image.FromFile(path);
        _cache[path] = img;

        return img;
    }
}