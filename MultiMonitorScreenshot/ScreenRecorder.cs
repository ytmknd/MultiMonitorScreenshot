using System.Diagnostics;
using System.Drawing.Imaging;
using System.Threading;

namespace MultiMonitorScreenshot
{
    /// <summary>
    /// 指定した矩形（仮想スクリーン座標）の画面を一定間隔で取り込み、
    /// MJPEG AVI として保存する画面レコーダー。取り込みは専用スレッドで行う。
    /// </summary>
    public sealed class ScreenRecorder : IDisposable
    {
        private readonly Rectangle bounds;
        private readonly int framesPerSecond;
        private readonly ManualResetEventSlim stopSignal = new(false);

        private IVideoFrameWriter? videoWriter;
        private Thread? captureThread;
        private volatile bool recording;
        private volatile Exception? captureError;

        public string OutputPath { get; private set; }
        public bool IsRecording => recording;

        public ScreenRecorder(Rectangle bounds, string outputPath, int framesPerSecond = 10)
        {
            this.bounds = bounds;
            this.framesPerSecond = framesPerSecond;
            OutputPath = outputPath;
        }

        public void Start()
        {
            if (recording)
            {
                return;
            }

            videoWriter = CreateWriter();
            recording = true;
            stopSignal.Reset();
            captureThread = new Thread(CaptureLoop)
            {
                IsBackground = true,
                Name = "ScreenRecorder",
            };
            captureThread.Start();
        }

        // MP4 を ffmpeg で書き出す。ffmpeg.exe はアプリと同じフォルダ、または PATH から探す。
        private IVideoFrameWriter CreateWriter()
        {
            return new FfmpegVideoWriter(OutputPath, bounds.Width, bounds.Height, framesPerSecond);
        }

        private void CaptureLoop()
        {
            var interval = TimeSpan.FromSeconds(1.0 / framesPerSecond);
            var stopwatch = Stopwatch.StartNew();
            var nextFrameAt = TimeSpan.Zero;

            try
            {
                using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppRgb);
                using var graphics = Graphics.FromImage(bitmap);

                while (recording)
                {
                    graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    videoWriter!.WriteFrame(bitmap);

                    nextFrameAt += interval;
                    var wait = nextFrameAt - stopwatch.Elapsed;
                    if (wait > TimeSpan.Zero)
                    {
                        stopSignal.Wait(wait);
                    }
                }
            }
            catch (Exception ex)
            {
                captureError = ex;
                recording = false;
            }
        }

        /// <summary>録画を停止し、AVI ファイルを確定する。取り込み中に発生した例外があれば送出する。</summary>
        public void Stop()
        {
            if (!recording && captureThread == null)
            {
                return;
            }

            recording = false;
            stopSignal.Set();
            captureThread?.Join();
            captureThread = null;

            videoWriter?.Dispose();
            videoWriter = null;

            if (captureError != null)
            {
                var error = captureError;
                captureError = null;
                throw new InvalidOperationException($"録画中にエラーが発生しました: {error.Message}", error);
            }
        }

        public void Dispose()
        {
            try
            {
                if (recording || captureThread != null)
                {
                    recording = false;
                    stopSignal.Set();
                    captureThread?.Join();
                    captureThread = null;
                    videoWriter?.Dispose();
                    videoWriter = null;
                }
            }
            finally
            {
                stopSignal.Dispose();
            }
        }
    }
}
