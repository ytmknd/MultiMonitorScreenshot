namespace MultiMonitorScreenshot
{
    public partial class Form1 : Form
    {
        private List<MonitorPanel> monitorPanels = new List<MonitorPanel>();
        private const int SCALE_FACTOR = 10; // モニター解像度を1/10にスケールして表示

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            SetupMonitorDisplay();
        }

        private void SetupMonitorDisplay()
        {
            // 既存のパネルをクリア
            foreach (var panel in monitorPanels)
            {
                monitorDisplayPanel.Controls.Remove(panel);
                panel.Dispose();
            }
            monitorPanels.Clear();

            var screens = Screen.AllScreens;

            // DPIスケーリングの違いなどで生じるモニター間の空白座標を軸ごとに詰める。
            // (Windowsの設定アプリと同様に、隣接モニターを密着させて表示するため)
            var xCompress = BuildAxisCompressor(screens.Select(s => (s.Bounds.X, s.Bounds.Right)));
            var yCompress = BuildAxisCompressor(screens.Select(s => (s.Bounds.Y, s.Bounds.Bottom)));

            // 空白を除去した後の各モニターの矩形
            var layoutBounds = screens.Select(s => new Rectangle(
                xCompress(s.Bounds.X),
                yCompress(s.Bounds.Y),
                xCompress(s.Bounds.Right) - xCompress(s.Bounds.X),
                yCompress(s.Bounds.Bottom) - yCompress(s.Bounds.Y))).ToArray();

            // 全モニターの範囲を計算（空白除去後の座標で）
            int minX = layoutBounds.Min(b => b.X);
            int minY = layoutBounds.Min(b => b.Y);
            int maxX = layoutBounds.Max(b => b.Right);
            int maxY = layoutBounds.Max(b => b.Bottom);

            int totalWidth = maxX - minX;
            int totalHeight = maxY - minY;

            // 表示エリアのサイズに合わせてスケール計算
            double scaleX = (double)monitorDisplayPanel.Width / totalWidth;
            double scaleY = (double)monitorDisplayPanel.Height / totalHeight;
            double scale = Math.Min(scaleX, scaleY) * 0.9; // 余白を持たせるため0.9倍

            // 各モニターのパネルを作成
            for (int i = 0; i < screens.Length; i++)
            {
                var screen = screens[i];
                var bounds = layoutBounds[i];
                var panel = new MonitorPanel(screen, i);

                // スケールした座標とサイズを計算
                int x = (int)((bounds.X - minX) * scale);
                int y = (int)((bounds.Y - minY) * scale);
                int width = (int)(bounds.Width * scale);
                int height = (int)(bounds.Height * scale);

                panel.Location = new Point(x + 10, y + 10);
                panel.Size = new Size(width, height);
                panel.BorderStyle = BorderStyle.FixedSingle;
                panel.Cursor = Cursors.Hand;
                panel.BackColor = screen.Primary ? Color.LightBlue : Color.LightGray;

                // クリックイベント
                panel.Click += (s, e) => CaptureScreen(panel.Screen);

                monitorDisplayPanel.Controls.Add(panel);
                monitorPanels.Add(panel);
            }
        }

        // 各モニターが占める区間の外側にある空白（どのモニターも使っていない座標範囲）を
        // 取り除く座標変換を返す。これによりモニター間の不自然な隙間が詰められる。
        private static Func<int, int> BuildAxisCompressor(IEnumerable<(int start, int end)> intervals)
        {
            var sorted = intervals.OrderBy(iv => iv.start).ToList();

            // 重なり・隣接区間をマージ
            var merged = new List<(int start, int end)>();
            foreach (var iv in sorted)
            {
                if (merged.Count > 0 && iv.start <= merged[^1].end)
                {
                    merged[^1] = (merged[^1].start, Math.Max(merged[^1].end, iv.end));
                }
                else
                {
                    merged.Add(iv);
                }
            }

            // マージ後の区間の間にある空白を収集
            var gaps = new List<(int start, int length)>();
            for (int i = 1; i < merged.Count; i++)
            {
                int gapStart = merged[i - 1].end;
                int gapLength = merged[i].start - merged[i - 1].end;
                if (gapLength > 0)
                {
                    gaps.Add((gapStart, gapLength));
                }
            }

            // 座標cより手前にある空白の合計を差し引いて詰めた座標を返す
            return c =>
            {
                int shift = 0;
                foreach (var gap in gaps)
                {
                    if (gap.start < c)
                    {
                        shift += Math.Min(gap.length, c - gap.start);
                    }
                }
                return c - shift;
            };
        }

        private void CaptureScreen(Screen screen)
        {
            try
            {
                var bounds = screen.Bounds;
                string fullPath = CaptureBounds(bounds);

                // 成功メッセージ
                statusLabel.Text = $"保存しました: {fullPath}";
                statusLabel.ForeColor = Color.Green;

                // フラッシュエフェクト
                FlashMonitorPanel(screen);
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"エラー: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
        }

        // 指定した矩形（仮想スクリーン座標）をキャプチャして PNG 保存し、保存先パスを返す
        private string CaptureBounds(Rectangle bounds)
        {
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                // 保存先ディレクトリを取得
                string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string screenshotPath = Path.Combine(picturesPath, "スクリーンショット");

                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(screenshotPath))
                {
                    Directory.CreateDirectory(screenshotPath);
                }

                // ファイル名を生成（日時付き、ミリ秒まで含めて重複を防止）
                string fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                string fullPath = Path.Combine(screenshotPath, fileName);

                // 保存
                bitmap.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);

                return fullPath;
            }
        }

        private async void FlashMonitorPanel(Screen screen)
        {
            var panel = monitorPanels.FirstOrDefault(p => p.Screen == screen);
            if (panel != null)
            {
                var originalColor = panel.BackColor;
                panel.BackColor = Color.Yellow;
                await Task.Delay(200);
                panel.BackColor = originalColor;
            }
        }

        private void captureAllButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 全モニターを囲む矩形（仮想スクリーン全体）を1枚でキャプチャする
                var bounds = SystemInformation.VirtualScreen;
                string fullPath = CaptureBounds(bounds);

                // 成功メッセージ
                statusLabel.Text = $"保存しました: {fullPath}";
                statusLabel.ForeColor = Color.Green;

                // 全モニターのパネルをフラッシュ
                foreach (var screen in Screen.AllScreens)
                {
                    FlashMonitorPanel(screen);
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"エラー: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
        }

        private void capturePrimaryButton_Click(object sender, EventArgs e)
        {
            CaptureScreen(Screen.PrimaryScreen!);
        }

        private void openFolderButton_Click(object sender, EventArgs e)
        {
            string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string screenshotPath = Path.Combine(picturesPath, "スクリーンショット");

            if (!Directory.Exists(screenshotPath))
            {
                Directory.CreateDirectory(screenshotPath);
            }

            System.Diagnostics.Process.Start("explorer.exe", screenshotPath);
        }
    }

    // モニター表示用のカスタムパネル
    public class MonitorPanel : Panel
    {
        public Screen Screen { get; }
        public int MonitorIndex { get; }

        public MonitorPanel(Screen screen, int index)
        {
            Screen = screen;
            MonitorIndex = index;
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // モニター番号を表示
            string text = $"モニター {MonitorIndex + 1}";
            if (Screen.Primary)
            {
                text += "\n(プライマリ)";
            }
            text += $"\n{Screen.Bounds.Width}x{Screen.Bounds.Height}";

            using (Font font = new Font("メイリオ", 10, FontStyle.Bold))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                g.DrawString(text, font, Brushes.Black, 
                    new RectangleF(0, 0, Width, Height), sf);
            }
        }
    }
}
