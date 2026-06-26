using System.Runtime.InteropServices;

namespace MultiMonitorScreenshot
{
    internal static class GlobalHotkey
    {
        public const int WM_HOTKEY = 0x0312;

        public const int IdScreenshotPrimary = 1;
        public const int IdScreenshotAll     = 2;
        public const int IdRecordToggle      = 3;
        public const int IdRecordAll         = 4;

        public const uint MOD_CONTROL  = 0x0002;
        public const uint MOD_ALT      = 0x0001;
        public const uint MOD_SHIFT    = 0x0004;
        public const uint MOD_NOREPEAT = 0x4000;

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
