namespace YtdlpNET;


public sealed class YtdlpException : Exception
{
    public YtdlpException(string message) : base(message) { }
    public YtdlpException(string message, Exception inner) : base(message, inner) { }
}