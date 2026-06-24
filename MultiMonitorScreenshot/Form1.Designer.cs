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
            monitorDisplayPanel = new Panel();
            buttonPanel = new Panel();
            capturePrimaryButton = new Button();
            captureAllButton = new Button();
            openFolderButton = new Button();
            statusLabel = new Label();
            buttonPanel.SuspendLayout();
            SuspendLayout();
            //
            // monitorDisplayPanel
            //
            monitorDisplayPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            monitorDisplayPanel.BackColor = Color.White;
            monitorDisplayPanel.BorderStyle = BorderStyle.FixedSingle;
            monitorDisplayPanel.Location = new Point(12, 12);
            monitorDisplayPanel.Name = "monitorDisplayPanel";
            monitorDisplayPanel.Size = new Size(760, 400);
            monitorDisplayPanel.TabIndex = 0;
            //
            // buttonPanel
            //
            buttonPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            buttonPanel.Controls.Add(openFolderButton);
            buttonPanel.Controls.Add(captureAllButton);
            buttonPanel.Controls.Add(capturePrimaryButton);
            buttonPanel.Location = new Point(12, 418);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(760, 50);
            buttonPanel.TabIndex = 1;
            //
            // capturePrimaryButton
            //
            capturePrimaryButton.Font = new Font("メイリオ", 10F, FontStyle.Regular, GraphicsUnit.Point);
            capturePrimaryButton.Location = new Point(3, 3);
            capturePrimaryButton.Name = "capturePrimaryButton";
            capturePrimaryButton.Size = new Size(180, 44);
            capturePrimaryButton.TabIndex = 0;
            capturePrimaryButton.Text = "プライマリモニター";
            capturePrimaryButton.UseVisualStyleBackColor = true;
            capturePrimaryButton.Click += capturePrimaryButton_Click;
            //
            // captureAllButton
            //
            captureAllButton.Font = new Font("メイリオ", 10F, FontStyle.Regular, GraphicsUnit.Point);
            captureAllButton.Location = new Point(189, 3);
            captureAllButton.Name = "captureAllButton";
            captureAllButton.Size = new Size(180, 44);
            captureAllButton.TabIndex = 1;
            captureAllButton.Text = "全モニター";
            captureAllButton.UseVisualStyleBackColor = true;
            captureAllButton.Click += captureAllButton_Click;
            //
            // openFolderButton
            //
            openFolderButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            openFolderButton.Font = new Font("メイリオ", 10F, FontStyle.Regular, GraphicsUnit.Point);
            openFolderButton.Location = new Point(577, 3);
            openFolderButton.Name = "openFolderButton";
            openFolderButton.Size = new Size(180, 44);
            openFolderButton.TabIndex = 2;
            openFolderButton.Text = "フォルダを開く";
            openFolderButton.UseVisualStyleBackColor = true;
            openFolderButton.Click += openFolderButton_Click;
            //
            // statusLabel
            //
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            statusLabel.Font = new Font("メイリオ", 9F, FontStyle.Regular, GraphicsUnit.Point);
            statusLabel.Location = new Point(12, 471);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(760, 25);
            statusLabel.TabIndex = 2;
            statusLabel.Text = "モニターをクリックするか、ボタンを押してスクリーンショットを撮影します";
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 505);
            Controls.Add(statusLabel);
            Controls.Add(buttonPanel);
            Controls.Add(monitorDisplayPanel);
            MinimumSize = new Size(800, 544);
            Name = "Form1";
            Text = "マルチモニター スクリーンショット";
            buttonPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel monitorDisplayPanel;
        private Panel buttonPanel;
        private Button capturePrimaryButton;
        private Button captureAllButton;
        private Button openFolderButton;
        private Label statusLabel;
    }
}
