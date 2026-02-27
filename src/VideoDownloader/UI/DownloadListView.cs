using System.Drawing.Drawing2D;
using VideoDownloader.Models;

namespace VideoDownloader.UI;

public class DownloadListView : ListView
{
    public DownloadListView()
    {
        View = View.Details;
        FullRowSelect = true;
        DoubleBuffered = true;
        OwnerDraw = true;

        Columns.Add("", 40);              // Thumbnail
        Columns.Add("Title", 320);
        Columns.Add("Progress", 180);
        Columns.Add("Speed", 100);
        Columns.Add("ETA", 80);
        Columns.Add("Status", 100);

        DrawColumnHeader += DrawHeader;
        DrawSubItem += DrawSubItemCustom;
    }

    private void DrawHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        e.Graphics.FillRectangle(
            new SolidBrush(Color.FromArgb(30, 30, 30)),
            e.Bounds);

        TextRenderer.DrawText(
            e.Graphics,
            e.Header.Text,
            Font,
            e.Bounds,
            Color.White,
            TextFormatFlags.VerticalCenter);
    }

    private void DrawSubItemCustom(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.ColumnIndex == 2)
        {
            DrawProgressBar(e);
            return;
        }

        TextRenderer.DrawText(
            e.Graphics,
            e.SubItem.Text,
            Font,
            e.Bounds,
            Color.White,
            TextFormatFlags.VerticalCenter);
    }

    private void DrawProgressBar(DrawListViewSubItemEventArgs e)
    {
        if (e.Item.Tag is not DownloadTask task)
            return;

        var rect = e.Bounds;
        rect.Inflate(-4, -6);

        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        // thread-safe painting
        if (InvokeRequired)
        {
            Invoke(() => DrawProgressBar(e));
            return;
        }

        // background
        e.Graphics.FillRectangle(Brushes.DimGray, rect);

        // calculate filled width
        var width = (int)(rect.Width * task.Progress / 100.0);
        width = Math.Max(1, width);

        var progressRect = new Rectangle(rect.X, rect.Y, width, rect.Height);

        using var brush = new LinearGradientBrush(
            progressRect,
            Color.DeepSkyBlue,
            Color.MediumBlue,
            0f);

        e.Graphics.FillRectangle(brush, progressRect);

        var txt = $"{task.Progress:0.0}%";

        TextRenderer.DrawText(
            e.Graphics,
            txt,
            Font,
            rect,
            Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    /// <summary>
    /// Call this method whenever the list updates to refresh the UI safely from any thread
    /// </summary>
    public void SafeRefresh()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(SafeRefresh));
        }
        else
        {
            Invalidate(); // triggers redraw
        }
    }
}