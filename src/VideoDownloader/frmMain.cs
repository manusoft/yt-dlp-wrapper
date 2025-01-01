using System.Drawing;
using YtDlpWrapper;

namespace VideoDownloader;

public partial class frmMain : Form
{
    private readonly YtDlpEngine engine;
    private readonly string  downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    public frmMain()
    {
        InitializeComponent();

        try
        {
            engine = new YtDlpEngine();
            textOutput.Text = downloadPath;
        }
        catch (Exception)
        {
            UpdateStatus("Engine failed to start.");
        }
    }

    private void frmMain_Load(object sender, EventArgs e)
    {
        comboQuality.Enabled = false;
        buttonDownload.Enabled = false;
        ClearStatus();
        UpdateStatus("Engine started successfully.");
    }

    private async void buttonDownload_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(textUrl.Text))
        {
            MessageBox.Show("Please enter the video URL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        await DownloadVideoAsync(textUrl.Text, comboQuality.SelectedItem as VideoFormat);
    }

    private async Task<List<VideoFormat>> GetVideoFormatsAsync(string url)
    {
        return await engine.GetAvailableFormatsAsync(url);
    }

    private async Task DownloadVideoAsync(string url, VideoFormat quality)
    {
        UpdateStatus("Preparing to download...");
        DisableControls();
        textUrl.Clear();
        textDetail.Clear();
        textDetail.AppendText($"Downloading video from: {url}" + Environment.NewLine);

        int progress = 0;
        progressDownload.Value = progress;

        // Subscribe to the download progress event
        engine.OnErrorMessage += (sender, e) => textDetail.AppendText($"{e}" + Environment.NewLine);

        // Subscribe to the download progress event
        engine.OnProgressMessage += (sender, e) => textDetail.AppendText($"{e}" + Environment.NewLine);

        engine.OnCompleteDownload += (sender, e) =>
        {
            progressDownload.Value = 100;
            textDetail.AppendText($"{e}" + Environment.NewLine);
            ClearStatus();
        };

        // Subscribe to the download progress event
        engine.OnProgressDownload += (sender, e) =>
        {
            try
            {
                // Parse e.Percent as a double
                double percent = double.Parse(e.Percent.ToString());

                // Convert the double to an integer for progress (if needed)
                progress = (int)Math.Round(percent); // Rounds to the nearest integer
                progressDownload.Value = progress;

                // Update the status
                UpdateStatus("Downloading", progress, e.Size, e.Speed, e.ETA);
            }
            catch (Exception ex)
            {
                // Handle the exception
                Console.WriteLine($"Error parsing percent: {ex.Message}");
            }
        };


        var selectedFormat = quality;

        await engine.DownloadVideoAsync(url, textOutput.Text.Trim(), VideoQuality.Custom, quality.ID);

        EnableControls();
    }

    private async void textUrl_TextChanged(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(textUrl.Text))
        {
            comboQuality.Enabled = false;
            buttonDownload.Enabled = false;
            return;
        }

        if (!textUrl.Text.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
        {
            comboQuality.Enabled = false;
            buttonDownload.Enabled = false;
            return;
        }

        progressDownload.Style = ProgressBarStyle.Marquee;
        progressDownload.MarqueeAnimationSpeed = 10;
        textDetail.Text = "Analyzing video URL..." + Environment.NewLine;
        UpdateStatus("Analyzing...");

        var formats = await GetVideoFormatsAsync(textUrl.Text);

        if (formats != null)
        {
            comboQuality.DataSource = formats;
            comboQuality.ValueMember = "ID";
            comboQuality.DisplayMember = "Resolution";

            comboQuality.Enabled = true;
            buttonDownload.Enabled = true;
        }
        else
        {
            comboQuality.DataSource = null;
            comboQuality.Items.Add("Best");
            comboQuality.Items.Add("Worst");
        }

        progressDownload.Style = ProgressBarStyle.Blocks;
        progressDownload.MarqueeAnimationSpeed = 100;
        textDetail.Text = "Video URL analyzed." + Environment.NewLine;
        UpdateStatus("Analyzed");
    }

    private void DisableControls()
    {
        buttonBrowseFolder.Enabled = false;
        comboQuality.Enabled = false;
        buttonDownload.Enabled = false;
        textOutput.Enabled = false;
        textUrl.Enabled = false;
    }

    private void EnableControls()
    {
        buttonBrowseFolder.Enabled = true;
        textOutput.Enabled = true;
        textUrl.Enabled = true;
    }

    private void UpdateStatus(string status, int progress = 0, string size = "", string speed = "", string eta = "")
    {
        try
        {
            toolStripLabelStatus.Text = $"Status: {status}";
            toolStripLabelProgress.Text = progress == 0 ? "" : $"Progress: {progress}%";
            toolStripLabelSize.Text = string.IsNullOrEmpty(size) ? "" : $"Size: {size}";
            toolStripLabelSpeed.Text = string.IsNullOrEmpty(speed) ? "" : $"Speed: {speed}";
            toolStripLabelETA.Text = string.IsNullOrEmpty(eta) ? "" : $"ETA: {eta}";
            //toolStripProgressBar.Value = progress;
        }
        catch (Exception)
        {
        }
    }

    private void ClearStatus()
    {
        toolStripLabelStatus.Text = "Status: Idle";
        toolStripLabelProgress.Text = string.Empty;
        toolStripLabelSize.Text = string.Empty;
        toolStripLabelSpeed.Text = string.Empty;
        toolStripLabelETA.Text = string.Empty;
        //toolStripProgressBar.Value = 0;
    }

    private void buttonBrowseFolder_Click(object sender, EventArgs e)
    {
        FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
        folderBrowserDialog.Description = "Select the folder to save the video";
        folderBrowserDialog.ShowNewFolderButton = true;
        folderBrowserDialog.ShowDialog();
        textOutput.Text = folderBrowserDialog.SelectedPath;
    }
}
