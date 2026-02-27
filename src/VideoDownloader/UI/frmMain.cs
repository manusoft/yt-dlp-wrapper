using VideoDownloader.Core;
using VideoDownloader.Models;
using VideoDownloader.UI;

namespace VideoDownloader;

public partial class frmMain : Form
{
    private readonly DownloadManager _manager = new();
    private readonly Dictionary<Guid, ListViewItem> _rows = new();

    public frmMain()
    {
        InitializeComponent();

        Text = "Video Downloader Ultimate";

        CreateToolbar();

        _manager.Updated += RefreshUI;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        DarkTheme.Apply(this);
    }

    private void CreateToolbar()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40
        };

        var txt = new TextBox
        {
            Name = "txtUrl",
            Width = 500,
            Left = 10,
            Top = 8
        };

        var btn = new Button
        {
            Text = "Add",
            Left = 520,
            Width = 80,
            Top = 6
        };

        btn.Click += (_, __) =>
        {
            AddDownload(txt.Text);
            txt.Clear();
        };

        panel.Controls.Add(txt);
        panel.Controls.Add(btn);

        Controls.Add(panel);
    }

    private void AddDownload(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        var task = new DownloadTask
        {
            Url = url,
            OutputFolder = App.AppSettings.OutputFolder,
            Title = url
        };

        // create a row in ListView
        var row = new ListViewItem(""); // thumbnail placeholder
        row.SubItems.Add(task.Title);
        row.SubItems.Add(""); // progress column, owner-drawn
        row.SubItems.Add(""); // speed
        row.SubItems.Add(""); // ETA
        row.SubItems.Add("Queued"); // state

        row.Tag = task;
 
        _rows[task.Id] = row;
        listViewDowloads.Items.Add(row);

        // enqueue download
        _manager.Enqueue(task);
    }

    private void RefreshUI()
    {
        UIThread.Run(listViewDowloads, () =>
        {
            foreach (ListViewItem item in listViewDowloads.Items)
            {
                if (item.Tag is not DownloadTask task)
                    continue;

                item.SubItems[2].Text = task.Progress.ToString();
                item.SubItems[3].Text = task.Speed;
                item.SubItems[4].Text = task.ETA;
                item.SubItems[5].Text = task.State.ToString();
            }

            // trigger owner-draw redraw
            listViewDowloads.Refresh();
        });
    }
}
