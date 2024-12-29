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
        if(string.IsNullOrEmpty(textUrl.Text))
        {
            MessageBox.Show("Please enter the video URL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        await DownloadVideoAsync(textUrl.Text, comboQuality.Text);
    }

    private async Task DownloadVideoAsync(string url, string quality)
    {
        textUrl.Clear();
        textDetail.Clear();

        var engine = new YtDlpEngine();

        // Subscribe to the download progress event
        engine.OnProgressMessage += (sender, e) => textDetail.AppendText($"{e}" + Environment.NewLine);

        // Subscribe to the download progress event
        engine.OnErrorMessage += (sender, e) => textDetail.AppendText($"{e}" + Environment.NewLine);

        // Subscribe to the download progress event
        engine.OnProgressDownload += (sender, e) => textDetail.AppendText($"Downloading: {e.Percent}% of {e.Size} ETA:{e.ETA}" + Environment.NewLine);

        if(quality == "Best")
        {
            await engine.DownloadVideoAsync(url, textOutput.Text.Trim());
        }
        else
        {
            await engine.DownloadVideoAsync(url, Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),  VideoQuality.Worst);
        }       
    }
}
