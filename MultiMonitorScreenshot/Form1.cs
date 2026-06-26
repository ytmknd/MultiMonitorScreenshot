namespace MultiMonitorScreenshot
{
    public partial class Form1 : Form
    {
        // 撮影モード（静止画 / 動画）
        private enum CaptureMode { Screenshot, Video }

        private List<MonitorPanel> monitorPanels = new List<MonitorPanel>();
        private const int SCALE_FACTOR = 10; // モニター解像度を1/10にスケールして表示
        private const int RECORDING_FPS = 10; // 録画のフレームレート

        private CaptureMode currentMode = CaptureMode.Screenshot;
        private ScreenRecorder? recorder;
        private Screen? recordingScreen;

        private NotifyIcon? notifyIcon;
        private ToolStripMenuItem? trayShowHideItem;
        private ToolStripMenuItem? trayRecordToggleItem;
        private bool isClosingToExit;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!isClosingToExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MinimizeToTray();
                return;
            }

            UnregisterHotkeys();
            recorder?.Dispose();
            recorder = null;
            notifyIcon?.Dispose();
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            SetupMonitorDisplay();
            SetMode(CaptureMode.Screenshot);
            SetupTrayIcon();
            RegisterHotkeys();
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

                // クリックは現在のモード（撮影 / 録画）に応じて動作する
                panel.Click += (s, e) => OnMonitorPanelClicked(panel);

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

        // ===== モード切替 =====

        private void screenshotModeButton_Click(object? sender, EventArgs e) => SetMode(CaptureMode.Screenshot);

        private void videoModeButton_Click(object? sender, EventArgs e) => SetMode(CaptureMode.Video);

        private void SetMode(CaptureMode mode)
        {
            // 録画中はモードを切り替えない（状態の矛盾を防ぐ）
            if (recorder != null)
            {
                return;
            }

            currentMode = mode;
            UpdateModeButtonStyles();
            UpdatePanelColors();

            bool video = mode == CaptureMode.Video;
            capturePrimaryButton.Text = video ? AppStrings.RecordPrimaryButton : AppStrings.CapturePrimaryButton;
            captureAllButton.Text = video ? AppStrings.RecordAllButton : AppStrings.CaptureAllButton;

            statusLabel.ForeColor = SystemColors.ControlText;
            statusLabel.Text = video ? AppStrings.StatusVideoMode : AppStrings.StatusScreenshotMode;
        }

        private void UpdateModeButtonStyles()
        {
            StyleModeButton(screenshotModeButton, currentMode == CaptureMode.Screenshot);
            StyleModeButton(videoModeButton, currentMode == CaptureMode.Video);
        }

        private static void StyleModeButton(Button button, bool active)
        {
            button.BackColor = active ? Color.SteelBlue : Color.Gainsboro;
            button.ForeColor = active ? Color.White : Color.DimGray;
            button.FlatAppearance.BorderColor = active ? Color.SteelBlue : Color.Silver;
        }

        // モードと録画状態に応じてモニターパネルの色を更新する。
        private void UpdatePanelColors()
        {
            foreach (var panel in monitorPanels)
            {
                bool isTarget = recorder != null
                    && (recordingScreen == null || panel.Screen.Equals(recordingScreen));
                panel.IsRecordingTarget = isTarget;

                if (isTarget)
                {
                    panel.BackColor = Color.Red;
                }
                else if (currentMode == CaptureMode.Video)
                {
                    panel.BackColor = Color.MistyRose; // 録画モードであることを示す淡い赤
                }
                else
                {
                    panel.BackColor = panel.Screen.Primary ? Color.LightBlue : Color.LightGray;
                }

                panel.Invalidate();
            }
        }

        // ===== クリック処理（モード依存）=====

        private void OnMonitorPanelClicked(MonitorPanel panel)
        {
            // 録画中はどのパネルをクリックしても録画を停止する
            if (recorder != null)
            {
                StopRecording();
                return;
            }

            if (currentMode == CaptureMode.Video)
            {
                StartRecording(panel.Screen.Bounds, AppStrings.MonitorTarget(panel.MonitorIndex + 1), panel.Screen);
            }
            else
            {
                CaptureScreen(panel.Screen);
            }
        }

        private void capturePrimaryButton_Click(object sender, EventArgs e)
        {
            var primary = Screen.PrimaryScreen!;
            if (currentMode == CaptureMode.Video)
            {
                StartRecording(primary.Bounds, AppStrings.PrimaryMonitorTarget, primary);
            }
            else
            {
                CaptureScreen(primary);
            }
        }

        private void captureAllButton_Click(object sender, EventArgs e)
        {
            if (currentMode == CaptureMode.Video)
            {
                // 全モニターを囲む矩形（仮想スクリーン全体）を録画。対象 null = 全パネル
                StartRecording(SystemInformation.VirtualScreen, AppStrings.AllMonitorsTarget, null);
            }
            else
            {
                CaptureAllScreens();
            }
        }

        private void stopButton_Click(object sender, EventArgs e) => StopRecording();

        // ===== スクリーンショット =====

        private void CaptureScreen(Screen screen)
        {
            try
            {
                var bounds = screen.Bounds;
                string fullPath = CaptureBounds(bounds);

                statusLabel.Text = AppStrings.StatusSaved(fullPath);
                statusLabel.ForeColor = Color.Green;
                ShowTrayBalloon(AppStrings.StatusSaved(fullPath));

                FlashMonitorPanel(screen);
            }
            catch (Exception ex)
            {
                statusLabel.Text = AppStrings.StatusError(ex.Message);
                statusLabel.ForeColor = Color.Red;
            }
        }

        private void CaptureAllScreens()
        {
            try
            {
                // 全モニターを囲む矩形（仮想スクリーン全体）を1枚でキャプチャする
                var bounds = SystemInformation.VirtualScreen;
                string fullPath = CaptureBounds(bounds);

                statusLabel.Text = AppStrings.StatusSaved(fullPath);
                statusLabel.ForeColor = Color.Green;
                ShowTrayBalloon(AppStrings.StatusSaved(fullPath));

                foreach (var screen in Screen.AllScreens)
                {
                    FlashMonitorPanel(screen);
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = AppStrings.StatusError(ex.Message);
                statusLabel.ForeColor = Color.Red;
            }
        }

        private static string GetScreenshotFolder() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");

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
                string screenshotPath = GetScreenshotFolder();

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
            // 録画中はパネル色が録画状態を示しているため、フラッシュしない
            if (recorder != null)
            {
                return;
            }

            var panel = monitorPanels.FirstOrDefault(p => p.Screen == screen);
            if (panel != null)
            {
                var originalColor = panel.BackColor;
                panel.BackColor = Color.Yellow;
                await Task.Delay(200);
                // フラッシュ中にモード等が変わっている可能性があるため、状態から色を再計算
                if (recorder == null)
                {
                    UpdatePanelColors();
                }
                else
                {
                    panel.BackColor = originalColor;
                }
            }
        }

        // ===== 録画 =====

        // 指定した矩形（仮想スクリーン座標）の録画を開始する。targetScreen=null は全モニター。
        private void StartRecording(Rectangle bounds, string targetLabel, Screen? targetScreen)
        {
            if (recorder != null)
            {
                return;
            }

            try
            {
                string folder = GetScreenshotFolder();
                Directory.CreateDirectory(folder);

                // ファイル名を生成（日時付き）。MP4 が使えない環境では録画側で .avi に切り替わる
                string fileName = $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
                string fullPath = Path.Combine(folder, fileName);

                recorder = new ScreenRecorder(bounds, fullPath, RECORDING_FPS);
                recorder.Start();
                recordingScreen = targetScreen;

                // 実際の保存先（フォールバック時は拡張子が変わる）を反映する
                string actualPath = recorder.OutputPath;

                UpdateControlsForRecordingState(true);
                UpdatePanelColors();
                statusLabel.Text = AppStrings.StatusRecording(targetLabel, actualPath);
                statusLabel.ForeColor = Color.Red;
                ShowTrayBalloon(AppStrings.StatusRecording(targetLabel, actualPath), ToolTipIcon.Warning);
            }
            catch (Exception ex)
            {
                recorder?.Dispose();
                recorder = null;
                recordingScreen = null;
                UpdateControlsForRecordingState(false);
                UpdatePanelColors();
                statusLabel.Text = AppStrings.StatusError(ex.Message);
                statusLabel.ForeColor = Color.Red;
            }
        }

        private void StopRecording()
        {
            if (recorder == null)
            {
                return;
            }

            string fullPath = recorder.OutputPath;
            try
            {
                recorder.Stop();
                recorder.Dispose();
                recorder = null;
                recordingScreen = null;

                statusLabel.Text = AppStrings.StatusSaved(fullPath);
                statusLabel.ForeColor = Color.Green;
                ShowTrayBalloon(AppStrings.StatusSaved(fullPath));
            }
            catch (Exception ex)
            {
                recorder?.Dispose();
                recorder = null;
                recordingScreen = null;
                statusLabel.Text = AppStrings.StatusError(ex.Message);
                statusLabel.ForeColor = Color.Red;
                ShowTrayBalloon(AppStrings.StatusError(ex.Message), ToolTipIcon.Error);
            }
            finally
            {
                UpdateControlsForRecordingState(false);
                UpdatePanelColors();
            }
        }

        // 録画状態に応じて操作系コントロールの有効/無効を切り替える。
        private void UpdateControlsForRecordingState(bool recording)
        {
            stopButton.Enabled = recording;
            stopButton.ForeColor = recording ? Color.Red : SystemColors.ControlText;

            capturePrimaryButton.Enabled = !recording;
            captureAllButton.Enabled = !recording;
            screenshotModeButton.Enabled = !recording;
            videoModeButton.Enabled = !recording;

            if (trayRecordToggleItem != null)
            {
                trayRecordToggleItem.Text = recording
                    ? AppStrings.TrayStopRecording
                    : AppStrings.TrayStartRecording;
            }
        }

        private void openFolderButton_Click(object sender, EventArgs e)
        {
            string screenshotPath = GetScreenshotFolder();

            if (!Directory.Exists(screenshotPath))
            {
                Directory.CreateDirectory(screenshotPath);
            }

            System.Diagnostics.Process.Start("explorer.exe", screenshotPath);
        }

        // ===== システムトレイ =====

        private void SetupTrayIcon()
        {
            var trayMenu = new ContextMenuStrip();

            trayShowHideItem = new ToolStripMenuItem(AppStrings.TrayHideWindow);
            trayShowHideItem.Click += (_, _) => ToggleWindowVisibility();
            trayMenu.Items.Add(trayShowHideItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            var ssItem = new ToolStripMenuItem(AppStrings.TrayScreenshotPrimary);
            ssItem.ShortcutKeyDisplayString = "Ctrl+Alt+S";
            ssItem.Click += (_, _) => CaptureScreen(Screen.PrimaryScreen!);
            trayMenu.Items.Add(ssItem);

            var ssAllItem = new ToolStripMenuItem(AppStrings.TrayScreenshotAll);
            ssAllItem.ShortcutKeyDisplayString = "Ctrl+Alt+A";
            ssAllItem.Click += (_, _) => CaptureAllScreens();
            trayMenu.Items.Add(ssAllItem);

            trayRecordToggleItem = new ToolStripMenuItem(AppStrings.TrayStartRecording);
            trayRecordToggleItem.ShortcutKeyDisplayString = "Ctrl+Alt+R";
            trayRecordToggleItem.Click += (_, _) => TrayToggleRecording();
            trayMenu.Items.Add(trayRecordToggleItem);

            var recordAllItem = new ToolStripMenuItem(AppStrings.TrayStartRecordingAll);
            recordAllItem.ShortcutKeyDisplayString = "Ctrl+Alt+Shift+R";
            recordAllItem.Click += (_, _) => StartAllRecordingFromHotkey();
            trayMenu.Items.Add(recordAllItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem(AppStrings.TrayExit);
            exitItem.Click += (_, _) => { isClosingToExit = true; Application.Exit(); };
            trayMenu.Items.Add(exitItem);

            notifyIcon = new NotifyIcon
            {
                Icon = this.Icon,
                ContextMenuStrip = trayMenu,
                Text = AppStrings.WindowTitle,
                Visible = true,
            };
            notifyIcon.DoubleClick += (_, _) => RestoreFromTray();
        }

        private void ToggleWindowVisibility()
        {
            if (Visible) MinimizeToTray();
            else RestoreFromTray();
        }

        private void MinimizeToTray()
        {
            Hide();
            if (trayShowHideItem != null)
            {
                trayShowHideItem.Text = AppStrings.TrayShowWindow;
            }
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            if (trayShowHideItem != null)
            {
                trayShowHideItem.Text = AppStrings.TrayHideWindow;
            }
        }

        private void TrayToggleRecording()
        {
            if (recorder != null)
            {
                StopRecording();
                return;
            }

            SetMode(CaptureMode.Video);
            var primary = Screen.PrimaryScreen!;
            StartRecording(primary.Bounds, AppStrings.PrimaryMonitorTarget, primary);
        }

        private void StartAllRecordingFromHotkey()
        {
            if (recorder != null)
            {
                return;
            }

            SetMode(CaptureMode.Video);
            StartRecording(SystemInformation.VirtualScreen, AppStrings.AllMonitorsTarget, null);
        }

        private void ShowTrayBalloon(string message, ToolTipIcon tipIcon = ToolTipIcon.Info)
        {
            if (!Visible)
            {
                notifyIcon?.ShowBalloonTip(3000, AppStrings.WindowTitle, message, tipIcon);
            }
        }

        // ===== グローバルホットキー =====

        private void RegisterHotkeys()
        {
            uint mods = GlobalHotkey.MOD_CONTROL | GlobalHotkey.MOD_ALT | GlobalHotkey.MOD_NOREPEAT;
            GlobalHotkey.RegisterHotKey(Handle, GlobalHotkey.IdScreenshotPrimary, mods, (uint)Keys.S);
            GlobalHotkey.RegisterHotKey(Handle, GlobalHotkey.IdScreenshotAll,     mods, (uint)Keys.A);
            GlobalHotkey.RegisterHotKey(Handle, GlobalHotkey.IdRecordToggle,      mods, (uint)Keys.R);
            GlobalHotkey.RegisterHotKey(Handle, GlobalHotkey.IdRecordAll, mods | GlobalHotkey.MOD_SHIFT, (uint)Keys.R);
        }

        private void UnregisterHotkeys()
        {
            GlobalHotkey.UnregisterHotKey(Handle, GlobalHotkey.IdScreenshotPrimary);
            GlobalHotkey.UnregisterHotKey(Handle, GlobalHotkey.IdScreenshotAll);
            GlobalHotkey.UnregisterHotKey(Handle, GlobalHotkey.IdRecordToggle);
            GlobalHotkey.UnregisterHotKey(Handle, GlobalHotkey.IdRecordAll);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == GlobalHotkey.WM_HOTKEY)
            {
                switch ((int)m.WParam)
                {
                    case GlobalHotkey.IdScreenshotPrimary:
                        CaptureScreen(Screen.PrimaryScreen!);
                        break;
                    case GlobalHotkey.IdScreenshotAll:
                        CaptureAllScreens();
                        break;
                    case GlobalHotkey.IdRecordToggle:
                        TrayToggleRecording();
                        break;
                    case GlobalHotkey.IdRecordAll:
                        StartAllRecordingFromHotkey();
                        break;
                }
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
            {
                MinimizeToTray();
            }
        }
    }

    // モニター表示用のカスタムパネル
    public class MonitorPanel : Panel
    {
        public Screen Screen { get; }
        public int MonitorIndex { get; }

        // 録画対象として録画中であることを示す（赤 + 「● REC」表示）
        public bool IsRecordingTarget { get; set; }

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
            string text = AppStrings.MonitorLabel(MonitorIndex + 1);
            if (Screen.Primary)
            {
                text += $"\n{AppStrings.PrimaryLabel}";
            }
            text += $"\n{Screen.Bounds.Width}x{Screen.Bounds.Height}";

            using (Font font = new Font("Segoe UI", 10, FontStyle.Bold))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                g.DrawString(text, font, Brushes.Black,
                    new RectangleF(0, 0, Width, Height), sf);
            }

            // 録画中の対象には「● REC」を上部に重ねて表示
            if (IsRecordingTarget)
            {
                using (Font recFont = new Font("Segoe UI", 11, FontStyle.Bold))
                using (StringFormat topFmt = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Near,
                })
                {
                    g.DrawString("● REC", recFont, Brushes.DarkRed,
                        new RectangleF(0, 4, Width, 24), topFmt);
                }
            }
        }
    }
}
