namespace YtdlpNET;

[Obsolete("This class is no longer used and will be removed in a future version.")]
public class CommandCompletedEventArgs : EventArgs
{
    public bool Success { get; }
    public string Message { get; }
    public CommandCompletedEventArgs(bool success, string message) => (Success, Message) = (success, message);
}