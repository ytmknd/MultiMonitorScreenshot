namespace MultiMonitorScreenshot
{
    partial class Form1
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
            modePanel = new Panel();
            screenshotModeButton = new Button();
            videoModeButton = new Button();
            countdownCheckBox = new CheckBox();
            monitorDisplayPanel = new Panel();
            buttonPanel = new TableLayoutPanel();
            capturePrimaryButton = new Button();
            captureAllButton = new Button();
            stopButton = new Button();
            openFolderButton = new Button();
            statusLabel = new Label();
            modePanel.SuspendLayout();
            buttonPanel.SuspendLayout();
            SuspendLayout();
            //
            // modePanel
            //
            modePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            modePanel.Controls.Add(screenshotModeButton);
            modePanel.Controls.Add(videoModeButton);
            modePanel.Controls.Add(countdownCheckBox);
            modePanel.Location = new Point(12, 12);
            modePanel.Name = "modePanel";
            modePanel.Size = new Size(760, 40);
            modePanel.TabIndex = 0;
            //
            // screenshotModeButton
            //
            screenshotModeButton.FlatStyle = FlatStyle.Flat;
            screenshotModeButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            screenshotModeButton.Location = new Point(0, 0);
            screenshotModeButton.Name = "screenshotModeButton";
            screenshotModeButton.Size = new Size(185, 36);
            screenshotModeButton.TabIndex = 0;
            screenshotModeButton.Text = AppStrings.ScreenshotModeButton;
            screenshotModeButton.UseVisualStyleBackColor = true;
            screenshotModeButton.Click += screenshotModeButton_Click;
            //
            // videoModeButton
            //
            videoModeButton.FlatStyle = FlatStyle.Flat;
            videoModeButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            videoModeButton.Location = new Point(191, 0);
            videoModeButton.Name = "videoModeButton";
            videoModeButton.Size = new Size(185, 36);
            videoModeButton.TabIndex = 1;
            videoModeButton.Text = AppStrings.VideoModeButton;
            videoModeButton.UseVisualStyleBackColor = true;
            videoModeButton.Click += videoModeButton_Click;
            //
            // countdownCheckBox
            //
            countdownCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            countdownCheckBox.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            countdownCheckBox.Location = new Point(570, 10);
            countdownCheckBox.Name = "countdownCheckBox";
            countdownCheckBox.Size = new Size(190, 24);
            countdownCheckBox.TabIndex = 2;
            countdownCheckBox.Text = AppStrings.CountdownCheckBox;
            countdownCheckBox.UseVisualStyleBackColor = true;
            //
            // monitorDisplayPanel
            //
            monitorDisplayPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            monitorDisplayPanel.BackColor = Color.White;
            monitorDisplayPanel.BorderStyle = BorderStyle.FixedSingle;
            monitorDisplayPanel.Location = new Point(12, 58);
            monitorDisplayPanel.Name = "monitorDisplayPanel";
            monitorDisplayPanel.Size = new Size(760, 354);
            monitorDisplayPanel.TabIndex = 1;
            monitorDisplayPanel.Resize += monitorDisplayPanel_Resize;
            //
            // buttonPanel
            //
            buttonPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            buttonPanel.ColumnCount = 4;
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonPanel.RowCount = 1;
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            buttonPanel.Controls.Add(capturePrimaryButton, 0, 0);
            buttonPanel.Controls.Add(captureAllButton, 1, 0);
            buttonPanel.Controls.Add(stopButton, 2, 0);
            buttonPanel.Controls.Add(openFolderButton, 3, 0);
            buttonPanel.Location = new Point(12, 418);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(760, 50);
            buttonPanel.TabIndex = 2;
            //
            // capturePrimaryButton
            //
            capturePrimaryButton.Dock = DockStyle.Fill;
            capturePrimaryButton.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            capturePrimaryButton.Margin = new Padding(3);
            capturePrimaryButton.Name = "capturePrimaryButton";
            capturePrimaryButton.TabIndex = 0;
            capturePrimaryButton.Text = AppStrings.CapturePrimaryButton;
            capturePrimaryButton.UseVisualStyleBackColor = true;
            capturePrimaryButton.Click += capturePrimaryButton_Click;
            //
            // captureAllButton
            //
            captureAllButton.Dock = DockStyle.Fill;
            captureAllButton.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            captureAllButton.Margin = new Padding(3);
            captureAllButton.Name = "captureAllButton";
            captureAllButton.TabIndex = 1;
            captureAllButton.Text = AppStrings.CaptureAllButton;
            captureAllButton.UseVisualStyleBackColor = true;
            captureAllButton.Click += captureAllButton_Click;
            //
            // stopButton
            //
            stopButton.Dock = DockStyle.Fill;
            stopButton.Enabled = false;
            stopButton.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            stopButton.Margin = new Padding(3);
            stopButton.Name = "stopButton";
            stopButton.TabIndex = 2;
            stopButton.Text = AppStrings.StopButton;
            stopButton.UseVisualStyleBackColor = true;
            stopButton.Click += stopButton_Click;
            //
            // openFolderButton
            //
            openFolderButton.Dock = DockStyle.Fill;
            openFolderButton.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            openFolderButton.Margin = new Padding(3);
            openFolderButton.Name = "openFolderButton";
            openFolderButton.TabIndex = 3;
            openFolderButton.Text = AppStrings.OpenFolderButton;
            openFolderButton.UseVisualStyleBackColor = true;
            openFolderButton.Click += openFolderButton_Click;
            //
            // statusLabel
            //
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            statusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            statusLabel.Location = new Point(12, 471);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(760, 25);
            statusLabel.TabIndex = 3;
            statusLabel.Text = AppStrings.StatusInitial;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // Form1
            //
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(784, 505);
            Controls.Add(statusLabel);
            Controls.Add(buttonPanel);
            Controls.Add(monitorDisplayPanel);
            Controls.Add(modePanel);
            MinimumSize = new Size(800, 544);
            Name = "Form1";
            Text = AppStrings.WindowTitle;
            modePanel.ResumeLayout(false);
            buttonPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel modePanel;
        private Button screenshotModeButton;
        private Button videoModeButton;
        private CheckBox countdownCheckBox;
        private Panel monitorDisplayPanel;
        private TableLayoutPanel buttonPanel;
        private Button capturePrimaryButton;
        private Button captureAllButton;
        private Button stopButton;
        private Button openFolderButton;
        private Label statusLabel;
    }
}
