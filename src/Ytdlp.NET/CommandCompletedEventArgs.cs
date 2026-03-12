namespace YtdlpNET;

public class CommandCompletedEventArgs : EventArgs
{
    public bool Success { get; }
    public string Message { get; }
    public CommandCompletedEventArgs(bool success, string message) => (Success, Message) = (success, message);
}