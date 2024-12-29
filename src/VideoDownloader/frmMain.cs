using YtDlpWrapper;

namespace VideoDownloader;

public partial class frmMain : Form
{
    public frmMain()
    {
        InitializeComponent();
    }

    private void frmMain_Load(object sender, EventArgs e)
    {
        comboQuality.SelectedIndex = 1;
    }

    private async void buttonDownload_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(textUrl.Text))
        {
            MessageBox.Show("Please enter the video URL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        await DownloadVideoAsync(textUrl.Text, comboQuality.SelectedIndex);
    }

    private async Task DownloadVideoAsync(string url, int quality)
    {
        textUrl.Clear();
        textDetail.Clear();

        var engine = new YtDlpEngine();

        // Subscribe to the download progress event
        engine.OnErrorMessage += (sender, e) => textDetail.AppendText($"{e}" + Environment.NewLine);

        // Subscribe to the download progress event
        engine.OnProgressMessage += (sender, e) => textDetail.AppendText($"{e}" + Environment.NewLine);

        // Subscribe to the download progress event
        engine.OnProgressDownload += (sender, e) => textDetail.AppendText($"Downloading: {e.Percent}% of {e.Size} ETA:{e.ETA}" + Environment.NewLine);

        // Subscribe to the download complete event
        engine.OnCompleteDownload += (sender, e) => textDetail.AppendText($"{e}" + Environment.NewLine);        

        switch (quality)
        {
            case 0:
                await engine.DownloadVideoAsync(url, textOutput.Text.Trim(), VideoQuality.MergeAll);
                break;
            case 1:
                await engine.DownloadVideoAsync(url, textOutput.Text.Trim(), VideoQuality.Worst);
                break;
        }
    }
}
