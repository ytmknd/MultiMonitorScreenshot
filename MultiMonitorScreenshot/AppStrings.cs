using System.Globalization;

namespace MultiMonitorScreenshot
{
    internal static class AppStrings
    {
        private static readonly bool IsJapanese =
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja";

        public static string TrayStartRecordingAll => IsJapanese ? "全画面録画を開始" : "Start Recording (All)";

        public static string WindowTitle => IsJapanese
            ? "マルチモニター スクリーンショット"
            : "Multi Monitor Screenshot";

        // Mode buttons
        public static string ScreenshotModeButton => IsJapanese ? "📷 スクリーンショット" : "📷 Screenshot";
        public static string VideoModeButton       => IsJapanese ? "🎥 動画録画"          : "🎥 Video Recording";

        // Action buttons
        public static string CapturePrimaryButton => IsJapanese ? "プライマリモニター"   : "Primary Monitor";
        public static string CaptureAllButton     => IsJapanese ? "全モニター"            : "All Monitors";
        public static string RecordPrimaryButton  => IsJapanese ? "プライマリを録画"      : "Record Primary";
        public static string RecordAllButton      => IsJapanese ? "全モニターを録画"      : "Record All";
        public static string StopButton           => IsJapanese ? "■ 停止"               : "■ Stop";
        public static string OpenFolderButton     => IsJapanese ? "フォルダを開く"        : "Open Folder";

        // Status: mode hints
        public static string StatusInitial => IsJapanese
            ? "モニターをクリックしてスクリーンショットを撮影します"
            : "Click a monitor to take a screenshot";

        public static string StatusScreenshotMode => IsJapanese
            ? "撮影モード: モニター（またはボタン）をクリックしてスクリーンショットを撮影します"
            : "Screenshot mode: Click a monitor (or button) to take a screenshot";

        public static string StatusVideoMode => IsJapanese
            ? "録画モード: モニター（またはボタン）をクリックして録画を開始します"
            : "Recording mode: Click a monitor (or button) to start recording";

        // Status: dynamic messages
        public static string StatusSaved(string path) => IsJapanese
            ? $"保存しました: {path}"
            : $"Saved: {path}";

        public static string StatusError(string message) => IsJapanese
            ? $"エラー: {message}"
            : $"Error: {message}";

        public static string StatusRecording(string target, string path) => IsJapanese
            ? $"録画中（{target}）: {path}"
            : $"Recording ({target}): {path}";

        // Monitor panel labels
        public static string MonitorLabel(int number) => IsJapanese
            ? $"モニター {number}"
            : $"Monitor {number}";

        public static string PrimaryLabel => IsJapanese ? "(プライマリ)" : "(Primary)";

        // Recording target labels
        public static string PrimaryMonitorTarget => IsJapanese ? "プライマリモニター" : "Primary Monitor";
        public static string AllMonitorsTarget    => IsJapanese ? "全モニター"          : "All Monitors";

        public static string MonitorTarget(int number) => IsJapanese
            ? $"モニター {number}"
            : $"Monitor {number}";

        // Tray menu
        public static string TrayShowWindow        => IsJapanese ? "ウィンドウを表示"                     : "Show Window";
        public static string TrayHideWindow        => IsJapanese ? "ウィンドウを非表示"                   : "Hide Window";
        public static string TrayScreenshotPrimary => IsJapanese ? "スクリーンショット（プライマリ）"      : "Screenshot (Primary)";
        public static string TrayScreenshotAll     => IsJapanese ? "スクリーンショット（全画面）"          : "Screenshot (All)";
        public static string TrayStartRecording    => IsJapanese ? "録画を開始（プライマリ）"              : "Start Recording (Primary)";
        public static string TrayStopRecording     => IsJapanese ? "録画を停止"                           : "Stop Recording";
        public static string TrayExit              => IsJapanese ? "終了"                                 : "Exit";
    }
}
