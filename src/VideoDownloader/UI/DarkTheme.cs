namespace VideoDownloader.UI;

public static class DarkTheme
{
    public static Color Back = Color.FromArgb(32, 32, 32);
    public static Color Panel = Color.FromArgb(45, 45, 48);
    public static Color Fore = Color.White;

    public static void Apply(Control root)
    {
        root.BackColor = Back;
        root.ForeColor = Fore;

        foreach (Control c in root.Controls)
            Apply(c);
    }
}