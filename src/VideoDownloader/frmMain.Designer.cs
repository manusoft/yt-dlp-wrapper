namespace VideoDownloader
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pictureBox1 = new PictureBox();
            textUrl = new TextBox();
            textOutput = new TextBox();
            comboQuality = new ComboBox();
            textDetail = new TextBox();
            buttonDownload = new Button();
            progressDownload = new ProgressBar();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.icon;
            pictureBox1.Location = new Point(12, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(82, 80);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // textUrl
            // 
            textUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textUrl.Font = new Font("Segoe UI", 12F);
            textUrl.Location = new Point(100, 12);
            textUrl.Name = "textUrl";
            textUrl.PlaceholderText = "Enter video URL here ...";
            textUrl.Size = new Size(572, 29);
            textUrl.TabIndex = 1;
            textUrl.TextChanged += textUrl_TextChanged;
            // 
            // textOutput
            // 
            textOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textOutput.Font = new Font("Segoe UI", 12F);
            textOutput.Location = new Point(100, 53);
            textOutput.Name = "textOutput";
            textOutput.Size = new Size(327, 29);
            textOutput.TabIndex = 2;
            textOutput.Text = "C:\\downloads";
            // 
            // comboQuality
            // 
            comboQuality.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            comboQuality.DropDownStyle = ComboBoxStyle.DropDownList;
            comboQuality.Font = new Font("Segoe UI", 12F);
            comboQuality.FormattingEnabled = true;
            comboQuality.Location = new Point(433, 53);
            comboQuality.Name = "comboQuality";
            comboQuality.Size = new Size(114, 29);
            comboQuality.TabIndex = 3;
            // 
            // textDetail
            // 
            textDetail.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textDetail.BackColor = SystemColors.Window;
            textDetail.Location = new Point(12, 114);
            textDetail.Multiline = true;
            textDetail.Name = "textDetail";
            textDetail.ReadOnly = true;
            textDetail.ScrollBars = ScrollBars.Vertical;
            textDetail.Size = new Size(660, 265);
            textDetail.TabIndex = 4;
            // 
            // buttonDownload
            // 
            buttonDownload.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            buttonDownload.Location = new Point(553, 53);
            buttonDownload.Name = "buttonDownload";
            buttonDownload.Size = new Size(119, 29);
            buttonDownload.TabIndex = 5;
            buttonDownload.Text = "DOWNLOAD";
            buttonDownload.UseVisualStyleBackColor = true;
            buttonDownload.Click += buttonDownload_Click;
            // 
            // progressDownload
            // 
            progressDownload.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressDownload.Location = new Point(12, 93);
            progressDownload.Name = "progressDownload";
            progressDownload.Size = new Size(660, 15);
            progressDownload.TabIndex = 6;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            ClientSize = new Size(684, 391);
            Controls.Add(progressDownload);
            Controls.Add(buttonDownload);
            Controls.Add(textDetail);
            Controls.Add(comboQuality);
            Controls.Add(textOutput);
            Controls.Add(textUrl);
            Controls.Add(pictureBox1);
            Font = new Font("Segoe UI", 10F);
            MaximizeBox = false;
            MinimumSize = new Size(700, 430);
            Name = "frmMain";
            Text = "Video Downloader v1.0.0 - Manuhub ";
            Load += frmMain_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBox1;
        private TextBox textUrl;
        private TextBox textOutput;
        private ComboBox comboQuality;
        private TextBox textDetail;
        private Button buttonDownload;
        private ProgressBar progressDownload;
    }
}
