namespace ManuHub.Ytdlp.NET;

public enum PostProcessors
{
    Merger,
    ModifyChapters,
    SplitChapters,
    ExtractAudio,
    VideoRemuxer,
    VideoConvertor,
    Metadata,
    EmbedSubtitle,
    EmbedThumbnail,
    SubtitlesConvertor,
    ThumbnailsConvertor,
    FixupStretched,
    FixupM4a,
    FixupM3u8,
    FixupTimestamp,
    FixupDuration
}