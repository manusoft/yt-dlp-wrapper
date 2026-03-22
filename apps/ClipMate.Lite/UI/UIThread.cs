namespace VideoDownloader.UI;

public static class UIThread
{
    public static void Run(Control control, Action action)
    {
        if (control.InvokeRequired)
            control.Invoke(action);
        else
            action();
    }
}