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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            textUrl = new TextBox();
            textOutput = new TextBox();
            comboQuality = new ComboBox();
            textDetail = new TextBox();
            buttonDownload = new Button();
            progressDownload = new ProgressBar();
            statusStrip1 = new StatusStrip();
            toolStripLabelStatus = new ToolStripStatusLabel();
            toolStripLabelProgress = new ToolStripStatusLabel();
            toolStripLabelSize = new ToolStripStatusLabel();
            toolStripLabelSpeed = new ToolStripStatusLabel();
            toolStripLabelETA = new ToolStripStatusLabel();
            buttonBrowseFolder = new Button();
            folderBrowserDialog1 = new FolderBrowserDialog();
            radioAuto = new RadioButton();
            radioCustom = new RadioButton();
            checkAutoClose = new CheckBox();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // textUrl
            // 
            textUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textUrl.Font = new Font("Segoe UI", 12F);
            textUrl.Location = new Point(12, 12);
            textUrl.Name = "textUrl";
            textUrl.PlaceholderText = "Enter video URL here ...";
            textUrl.Size = new Size(660, 29);
            textUrl.TabIndex = 1;
            textUrl.TextChanged += textUrl_TextChanged;
            // 
            // textOutput
            // 
            textOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textOutput.Font = new Font("Segoe UI", 12F);
            textOutput.Location = new Point(12, 47);
            textOutput.Name = "textOutput";
            textOutput.Size = new Size(618, 29);
            textOutput.TabIndex = 2;
            textOutput.Text = "C:\\downloads";
            // 
            // comboQuality
            // 
            comboQuality.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            comboQuality.DropDownStyle = ComboBoxStyle.DropDownList;
            comboQuality.Font = new Font("Segoe UI", 12F);
            comboQuality.FormattingEnabled = true;
            comboQuality.Location = new Point(157, 82);
            comboQuality.Name = "comboQuality";
            comboQuality.Size = new Size(206, 29);
            comboQuality.TabIndex = 3;
            // 
            // textDetail
            // 
            textDetail.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textDetail.BackColor = SystemColors.Window;
            textDetail.Location = new Point(12, 138);
            textDetail.Multiline = true;
            textDetail.Name = "textDetail";
            textDetail.ReadOnly = true;
            textDetail.ScrollBars = ScrollBars.Vertical;
            textDetail.Size = new Size(660, 292);
            textDetail.TabIndex = 4;
            // 
            // buttonDownload
            // 
            buttonDownload.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            buttonDownload.Location = new Point(553, 82);
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
            progressDownload.Location = new Point(12, 117);
            progressDownload.Name = "progressDownload";
            progressDownload.Size = new Size(660, 15);
            progressDownload.TabIndex = 6;
            // 
            // statusStrip1
            // 
            statusStrip1.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripLabelStatus, toolStripLabelProgress, toolStripLabelSize, toolStripLabelSpeed, toolStripLabelETA });
            statusStrip1.Location = new Point(0, 439);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(684, 22);
            statusStrip1.TabIndex = 7;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripLabelStatus
            // 
            toolStripLabelStatus.Name = "toolStripLabelStatus";
            toolStripLabelStatus.Size = new Size(669, 17);
            toolStripLabelStatus.Spring = true;
            toolStripLabelStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // toolStripLabelProgress
            // 
            toolStripLabelProgress.Name = "toolStripLabelProgress";
            toolStripLabelProgress.Size = new Size(0, 17);
            // 
            // toolStripLabelSize
            // 
            toolStripLabelSize.Name = "toolStripLabelSize";
            toolStripLabelSize.Size = new Size(0, 17);
            // 
            // toolStripLabelSpeed
            // 
            toolStripLabelSpeed.Name = "toolStripLabelSpeed";
            toolStripLabelSpeed.Size = new Size(0, 17);
            // 
            // toolStripLabelETA
            // 
            toolStripLabelETA.Name = "toolStripLabelETA";
            toolStripLabelETA.Size = new Size(0, 17);
            // 
            // buttonBrowseFolder
            // 
            buttonBrowseFolder.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            buttonBrowseFolder.Location = new Point(636, 47);
            buttonBrowseFolder.Name = "buttonBrowseFolder";
            buttonBrowseFolder.Size = new Size(36, 29);
            buttonBrowseFolder.TabIndex = 8;
            buttonBrowseFolder.Text = "...";
            buttonBrowseFolder.UseVisualStyleBackColor = true;
            buttonBrowseFolder.Click += buttonBrowseFolder_Click;
            // 
            // radioAuto
            // 
            radioAuto.AutoSize = true;
            radioAuto.Checked = true;
            radioAuto.Location = new Point(13, 85);
            radioAuto.Name = "radioAuto";
            radioAuto.Size = new Size(57, 23);
            radioAuto.TabIndex = 9;
            radioAuto.TabStop = true;
            radioAuto.Text = "Auto";
            radioAuto.UseVisualStyleBackColor = true;
            radioAuto.CheckedChanged += radioAuto_CheckedChanged;
            // 
            // radioCustom
            // 
            radioCustom.AutoSize = true;
            radioCustom.Location = new Point(76, 85);
            radioCustom.Name = "radioCustom";
            radioCustom.Size = new Size(75, 23);
            radioCustom.TabIndex = 10;
            radioCustom.Text = "Custom";
            radioCustom.UseVisualStyleBackColor = true;
            radioCustom.CheckedChanged += radioCustom_CheckedChanged;
            // 
            // checkAutoClose
            // 
            checkAutoClose.AutoSize = true;
            checkAutoClose.Location = new Point(401, 86);
            checkAutoClose.Name = "checkAutoClose";
            checkAutoClose.Size = new Size(146, 23);
            checkAutoClose.TabIndex = 11;
            checkAutoClose.Text = "Close automatically";
            checkAutoClose.UseVisualStyleBackColor = true;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            ClientSize = new Size(684, 461);
            Controls.Add(checkAutoClose);
            Controls.Add(radioCustom);
            Controls.Add(radioAuto);
            Controls.Add(buttonBrowseFolder);
            Controls.Add(statusStrip1);
            Controls.Add(progressDownload);
            Controls.Add(buttonDownload);
            Controls.Add(textDetail);
            Controls.Add(comboQuality);
            Controls.Add(textOutput);
            Controls.Add(textUrl);
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimumSize = new Size(700, 450);
            Name = "frmMain";
            Text = "Video Downloader v1.1.0 - Manuhub ";
            Load += frmMain_Load;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TextBox textUrl;
        private TextBox textOutput;
        private ComboBox comboQuality;
        private TextBox textDetail;
        private Button buttonDownload;
        private ProgressBar progressDownload;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripLabelStatus;
        private ToolStripStatusLabel toolStripLabelSpeed;
        private ToolStripStatusLabel toolStripLabelProgress;
        private ToolStripStatusLabel toolStripLabelETA;
        private ToolStripStatusLabel toolStripLabelSize;
        private Button buttonBrowseFolder;
        private FolderBrowserDialog folderBrowserDialog1;
        private RadioButton radioAuto;
        private RadioButton radioCustom;
        private CheckBox checkAutoClose;
    }
}
