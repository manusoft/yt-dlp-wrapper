using System.Text.Json;
using VideoDownloader.Models;

namespace VideoDownloader.Engine;

public sealed class PlaylistPipeline
{
    public List<PlaylistItem> Parse(string json)
    {
        var list = new List<PlaylistItem>();

        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("entries", out var entries))
            return list;

        foreach (var e in entries.EnumerateArray())
        {
            var url = e.GetProperty("url").GetString();
            var title = e.GetProperty("title").GetString();

            if (url == null)
                continue;

            list.Add(new PlaylistItem
            {
                Url = url,
                Title = title ?? url
            });
        }

        return list;
    }
}