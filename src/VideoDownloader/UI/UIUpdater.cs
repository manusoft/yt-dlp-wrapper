namespace VideoDownloader.UI;

public static class UIUpdater
{
    public static void Safe(Control c, Action action)
    {
        if (c.InvokeRequired)
            c.BeginInvoke(action);
        else
            action();
    }
}