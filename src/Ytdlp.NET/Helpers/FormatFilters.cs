using ManuHub.Ytdlp.Models;

namespace ManuHub.Ytdlp.Helpers;

public static class FormatFilters
{

    public static IEnumerable<Format> Audio(IEnumerable<Format> formats)
    {
        return formats.Where(f => f.IsAudioOnly);
    }

    public static IEnumerable<Format> Video(IEnumerable<Format> formats)
    {
        return formats.Where(f => f.HasVideo);
    }   
}
