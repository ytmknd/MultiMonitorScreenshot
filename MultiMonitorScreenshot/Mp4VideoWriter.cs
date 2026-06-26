using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MultiMonitorScreenshot
{
    /// <summary>動画フレームを 1 枚ずつ書き出すライターの共通インターフェース。</summary>
    public interface IVideoFrameWriter : IDisposable
    {
        void WriteFrame(Bitmap frame);
    }

    /// <summary>
    /// Windows 標準の Media Foundation Sink Writer を用いて H.264/MP4 を書き出すライター。
    /// 外部ライブラリに依存しない。MF が利用できない環境ではコンストラクタが例外を投げる
    /// （呼び出し側で MJPEG にフォールバックする）。
    /// </summary>
    internal sealed class Mp4VideoWriter : IVideoFrameWriter
    {
        private static readonly object StartupLock = new();
        private static bool mfStarted;

        private readonly int width;
        private readonly int height;
        private readonly int stride;
        private readonly long frameDuration; // 100ns 単位
        private readonly uint bufferSize;

        private IMFSinkWriter? writer;
        private uint streamIndex;
        private long frameIndex;
        private bool writing;
        private bool finalized;

        public Mp4VideoWriter(string path, int width, int height, int framesPerSecond, uint avgBitrate = 0)
        {
            if (framesPerSecond <= 0)
            {
                throw new ArgumentException("フレームレートは正の値である必要があります。", nameof(framesPerSecond));
            }

            // H.264 は偶数の幅・高さを要求するため切り下げる
            this.width = Math.Max(2, width & ~1);
            this.height = Math.Max(2, height & ~1);
            this.stride = this.width * 4;
            this.bufferSize = (uint)(this.stride * this.height);
            this.frameDuration = 10_000_000L / framesPerSecond;

            if (avgBitrate == 0)
            {
                avgBitrate = (uint)Math.Clamp(
                    (long)this.width * this.height * framesPerSecond / 10L,
                    1_000_000L, 30_000_000L);
            }

            EnsureStartup();

            try
            {
                Initialize(path, framesPerSecond, avgBitrate);
            }
            catch
            {
                ReleaseWriter();
                throw;
            }
        }

        private void Initialize(string path, int fps, uint avgBitrate)
        {
            Check(MediaFoundation.MFCreateSinkWriterFromURL(path, IntPtr.Zero, null, out writer),
                "MFCreateSinkWriterFromURL");

            ulong frameSize = Pack((uint)width, (uint)height);
            ulong frameRate = Pack((uint)fps, 1);
            ulong par = Pack(1, 1);

            // 出力タイプ（H.264）
            Check(MediaFoundation.MFCreateMediaType(out IMFAttributes outType), "MFCreateMediaType(out)");
            try
            {
                SetGuid(outType, MFGuids.MF_MT_MAJOR_TYPE, MFGuids.MFMediaType_Video);
                SetGuid(outType, MFGuids.MF_MT_SUBTYPE, MFGuids.MFVideoFormat_H264);
                SetU32(outType, MFGuids.MF_MT_AVG_BITRATE, avgBitrate);
                SetU32(outType, MFGuids.MF_MT_INTERLACE_MODE, MediaFoundation.MFVideoInterlace_Progressive);
                SetU64(outType, MFGuids.MF_MT_FRAME_SIZE, frameSize);
                SetU64(outType, MFGuids.MF_MT_FRAME_RATE, frameRate);
                SetU64(outType, MFGuids.MF_MT_PIXEL_ASPECT_RATIO, par);
                Check(writer!.AddStream(outType, out streamIndex), "AddStream");
            }
            finally
            {
                Marshal.ReleaseComObject(outType);
            }

            // 入力タイプ（RGB32, トップダウン = 正のストライド）
            Check(MediaFoundation.MFCreateMediaType(out IMFAttributes inType), "MFCreateMediaType(in)");
            try
            {
                SetGuid(inType, MFGuids.MF_MT_MAJOR_TYPE, MFGuids.MFMediaType_Video);
                SetGuid(inType, MFGuids.MF_MT_SUBTYPE, MFGuids.MFVideoFormat_RGB32);
                SetU32(inType, MFGuids.MF_MT_INTERLACE_MODE, MediaFoundation.MFVideoInterlace_Progressive);
                SetU32(inType, MFGuids.MF_MT_DEFAULT_STRIDE, (uint)stride);
                SetU64(inType, MFGuids.MF_MT_FRAME_SIZE, frameSize);
                SetU64(inType, MFGuids.MF_MT_FRAME_RATE, frameRate);
                SetU64(inType, MFGuids.MF_MT_PIXEL_ASPECT_RATIO, par);
                Check(writer!.SetInputMediaType(streamIndex, inType, null), "SetInputMediaType");
            }
            finally
            {
                Marshal.ReleaseComObject(inType);
            }

            Check(writer!.BeginWriting(), "BeginWriting");
            writing = true;
        }

        public void WriteFrame(Bitmap frame)
        {
            if (writer == null)
            {
                throw new ObjectDisposedException(nameof(Mp4VideoWriter));
            }

            // 偶数化した領域だけをロックして取り込む（端の 1px はクロップ）
            var rect = new Rectangle(0, 0, width, height);
            BitmapData data = frame.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            try
            {
                Check(MediaFoundation.MFCreateMemoryBuffer(bufferSize, out IMFMediaBuffer buffer), "MFCreateMemoryBuffer");
                try
                {
                    Check(buffer.Lock(out IntPtr dest, out _, out _), "IMFMediaBuffer.Lock");
                    int copyHr = MediaFoundation.MFCopyImage(dest, stride, data.Scan0, data.Stride, width * 4, height);
                    buffer.Unlock();
                    Check(copyHr, "MFCopyImage");
                    Check(buffer.SetCurrentLength(bufferSize), "SetCurrentLength");

                    Check(MediaFoundation.MFCreateSample(out IMFSample sample), "MFCreateSample");
                    try
                    {
                        Check(sample.AddBuffer(buffer), "AddBuffer");
                        Check(sample.SetSampleTime(frameIndex * frameDuration), "SetSampleTime");
                        Check(sample.SetSampleDuration(frameDuration), "SetSampleDuration");
                        Check(writer.WriteSample(streamIndex, sample), "WriteSample");
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(sample);
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(buffer);
                }
            }
            finally
            {
                frame.UnlockBits(data);
            }

            frameIndex++;
        }

        public void Dispose()
        {
            try
            {
                if (writer != null && writing && !finalized)
                {
                    finalized = true;
                    Check(writer.SinkFinalize(), "Finalize");
                }
            }
            finally
            {
                ReleaseWriter();
            }
        }

        private void ReleaseWriter()
        {
            if (writer != null)
            {
                Marshal.ReleaseComObject(writer);
                writer = null;
            }
        }

        private static void EnsureStartup()
        {
            lock (StartupLock)
            {
                if (!mfStarted)
                {
                    Check(MediaFoundation.MFStartup(MediaFoundation.MF_VERSION, MediaFoundation.MFSTARTUP_FULL), "MFStartup");
                    mfStarted = true;
                }
            }
        }

        private static void SetGuid(IMFAttributes a, Guid key, Guid value) =>
            Check(a.SetGUID(ref key, ref value), "SetGUID");

        private static void SetU32(IMFAttributes a, Guid key, uint value) =>
            Check(a.SetUINT32(ref key, value), "SetUINT32");

        private static void SetU64(IMFAttributes a, Guid key, ulong value) =>
            Check(a.SetUINT64(ref key, value), "SetUINT64");

        private static ulong Pack(uint high, uint low) => ((ulong)high << 32) | low;

        private static void Check(int hr, string what)
        {
            if (hr < 0)
            {
                throw new InvalidOperationException($"Media Foundation {what} に失敗しました (HRESULT 0x{hr:X8})。");
            }
        }
    }
}
