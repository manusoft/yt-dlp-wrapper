namespace YtdlpNET;

[Obsolete("This class is no longer used and will be removed in a future version.")]
public sealed class YtdlpException : Exception
{
    public YtdlpException(string message) : base(message) { }
    public YtdlpException(string message, Exception inner) : base(message, inner) { }
}