using System.Drawing;
using YtDlpWrapper;

namespace VideoDownloader;

public partial class frmMain : Form
{
    private readonly YtDlpEngine engine;

    public frmMain()
    {
        InitializeComponent();
        engine = new YtDlpEngine();
    }

    private void frmMain_Load(object sender, EventArgs e)
    {
        comboQuality.Enabled = false;
        buttonDownload.Enabled = false;
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

        engine.OnCompleteDownload += (sender, e) => textDetail.AppendText($"Completed {e}" + Environment.NewLine);

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
            }
            catch (Exception ex)
            {
                // Handle the exception
                Console.WriteLine($"Error parsing percent: {ex.Message}");
            }
        };

        // Subscribe to the download complete event
        engine.OnCompleteDownload += (sender, e) => textDetail.AppendText($"{e}" + Environment.NewLine);

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
    }

    private void DisableControls()
    {
        comboQuality.Enabled = false;
        buttonDownload.Enabled = false;
        textOutput.Enabled = false;
        textUrl.Enabled = false;
    }

    private void EnableControls()
    {
        textOutput.Enabled = true;
        textUrl.Enabled = true;
    }
}
