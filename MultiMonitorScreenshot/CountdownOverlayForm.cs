namespace MultiMonitorScreenshot
{
    // 撮影対象モニターの中央に大きく数字を表示する、半透明のカウントダウン用オーバーレイ。
    // フォーカスを奪わず（アクティブ化しない）、常に最前面に表示する。
    public class CountdownOverlayForm : Form
    {
        private string number = string.Empty;

        public CountdownOverlayForm(Screen screen)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.Black;
            Opacity = 0.55;
            DoubleBuffered = true;
            Size = new Size(240, 240);
            Location = new Point(
                screen.Bounds.X + (screen.Bounds.Width - Width) / 2,
                screen.Bounds.Y + (screen.Bounds.Height - Height) / 2);

            // 角丸の円形に近いバッジ形状にする
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                int d = 40;
                path.AddArc(0, 0, d, d, 180, 90);
                path.AddArc(Width - d, 0, d, d, 270, 90);
                path.AddArc(Width - d, Height - d, d, d, 0, 90);
                path.AddArc(0, Height - d, d, d, 90, 90);
                path.CloseFigure();
                Region = new Region(path);
            }
        }

        // Show() してもフォーカス/アクティブ化を奪わないようにする
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_NOACTIVATE = 0x08000000;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        public void SetNumber(string text)
        {
            number = text;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            using (Font font = new Font("Segoe UI", 96, FontStyle.Bold))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                g.DrawString(number, font, Brushes.White, new RectangleF(0, 0, Width, Height), sf);
            }
        }
    }
}
