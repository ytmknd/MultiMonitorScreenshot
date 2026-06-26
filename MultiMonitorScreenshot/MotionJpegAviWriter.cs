using System.Drawing.Imaging;

namespace MultiMonitorScreenshot
{
    /// <summary>
    /// 外部ライブラリに依存せず Motion JPEG (MJPG) 形式の AVI 動画を書き出すライター。
    /// 各フレームを System.Drawing で JPEG エンコードし、RIFF/AVI コンテナへ格納する。
    /// </summary>
    public sealed class MotionJpegAviWriter : IVideoFrameWriter
    {
        private const uint AVIF_HASINDEX = 0x10;
        private const uint AVIIF_KEYFRAME = 0x10;

        // ヘッダー内の固定サイズ（バイト構成から算出した定数）
        private const uint HDRL_SIZE = 192;
        private const uint STRL_SIZE = 116;

        private readonly FileStream stream;
        private readonly BinaryWriter writer;
        private readonly int width;
        private readonly int height;
        private readonly ImageCodecInfo jpegCodec;
        private readonly EncoderParameters encoderParameters;
        private readonly List<(uint offset, uint length)> index = new();

        // 確定後に書き戻すフィールドのファイル位置
        private long riffSizePosition;
        private long totalFramesPosition;
        private long streamLengthPosition;
        private long moviSizePosition;
        private long moviDataStart; // 'movi' FOURCC の位置（idx1 オフセットの基準）

        private bool finalized;

        public MotionJpegAviWriter(string path, int width, int height, int framesPerSecond, long jpegQuality = 80)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("動画の幅と高さは正の値である必要があります。");
            }
            if (framesPerSecond <= 0)
            {
                throw new ArgumentException("フレームレートは正の値である必要があります。", nameof(framesPerSecond));
            }

            this.width = width;
            this.height = height;

            jpegCodec = ImageCodecInfo.GetImageEncoders()
                .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid)
                ?? throw new InvalidOperationException("JPEG エンコーダが見つかりません。");
            encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, jpegQuality);

            stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: false);
            WriteHeader(framesPerSecond);
        }

        /// <summary>1 フレーム（現在の画面ビットマップ）を追記する。呼び出しは単一スレッドから行うこと。</summary>
        public void WriteFrame(Bitmap frame)
        {
            byte[] jpeg;
            using (var ms = new MemoryStream())
            {
                frame.Save(ms, jpegCodec, encoderParameters);
                jpeg = ms.ToArray();
            }

            uint offset = (uint)(stream.Position - moviDataStart);
            WriteFourCC("00dc");
            writer.Write((uint)jpeg.Length);
            writer.Write(jpeg);
            if ((jpeg.Length & 1) == 1)
            {
                writer.Write((byte)0); // チャンクは WORD 境界に揃える（サイズには含めない）
            }

            index.Add((offset, (uint)jpeg.Length));
        }

        private void WriteFourCC(string code) => writer.Write(System.Text.Encoding.ASCII.GetBytes(code));

        private void WriteHeader(int fps)
        {
            uint microSecPerFrame = (uint)(1_000_000.0 / fps);

            WriteFourCC("RIFF");
            riffSizePosition = stream.Position;
            writer.Write(0u); // RIFF サイズ（後で確定）
            WriteFourCC("AVI ");

            // hdrl リスト
            WriteFourCC("LIST");
            writer.Write(HDRL_SIZE);
            WriteFourCC("hdrl");

            // avih（MainAVIHeader, 56 バイト）
            WriteFourCC("avih");
            writer.Write(56u);
            writer.Write(microSecPerFrame);
            writer.Write(0u);            // dwMaxBytesPerSec
            writer.Write(0u);            // dwPaddingGranularity
            writer.Write(AVIF_HASINDEX); // dwFlags
            totalFramesPosition = stream.Position;
            writer.Write(0u);            // dwTotalFrames（後で確定）
            writer.Write(0u);            // dwInitialFrames
            writer.Write(1u);            // dwStreams
            writer.Write(0u);            // dwSuggestedBufferSize
            writer.Write((uint)width);   // dwWidth
            writer.Write((uint)height);  // dwHeight
            writer.Write(0u);            // dwReserved[0]
            writer.Write(0u);            // dwReserved[1]
            writer.Write(0u);            // dwReserved[2]
            writer.Write(0u);            // dwReserved[3]

            // strl リスト
            WriteFourCC("LIST");
            writer.Write(STRL_SIZE);
            WriteFourCC("strl");

            // strh（AVIStreamHeader, 56 バイト）
            WriteFourCC("strh");
            writer.Write(56u);
            WriteFourCC("vids");
            WriteFourCC("MJPG");
            writer.Write(0u);            // dwFlags
            writer.Write((short)0);      // wPriority
            writer.Write((short)0);      // wLanguage
            writer.Write(0u);            // dwInitialFrames
            writer.Write(1u);            // dwScale
            writer.Write((uint)fps);     // dwRate（dwRate/dwScale = fps）
            writer.Write(0u);            // dwStart
            streamLengthPosition = stream.Position;
            writer.Write(0u);            // dwLength（総フレーム数、後で確定）
            writer.Write(0u);            // dwSuggestedBufferSize
            writer.Write(0xFFFFFFFFu);   // dwQuality
            writer.Write(0u);            // dwSampleSize
            writer.Write((short)0);      // rcFrame.left
            writer.Write((short)0);      // rcFrame.top
            writer.Write((short)width);  // rcFrame.right
            writer.Write((short)height); // rcFrame.bottom

            // strf（BITMAPINFOHEADER, 40 バイト）
            WriteFourCC("strf");
            writer.Write(40u);
            writer.Write(40u);                        // biSize
            writer.Write(width);                      // biWidth
            writer.Write(height);                     // biHeight
            writer.Write((short)1);                   // biPlanes
            writer.Write((short)24);                  // biBitCount
            WriteFourCC("MJPG");                      // biCompression
            writer.Write((uint)(width * height * 3)); // biSizeImage
            writer.Write(0);                          // biXPelsPerMeter
            writer.Write(0);                          // biYPelsPerMeter
            writer.Write(0u);                         // biClrUsed
            writer.Write(0u);                         // biClrImportant

            // movi リスト（フレームデータ本体）
            WriteFourCC("LIST");
            moviSizePosition = stream.Position;
            writer.Write(0u); // movi サイズ（後で確定）
            moviDataStart = stream.Position;
            WriteFourCC("movi");
        }

        private void WriteIndex()
        {
            WriteFourCC("idx1");
            writer.Write((uint)(index.Count * 16));
            foreach (var (offset, length) in index)
            {
                WriteFourCC("00dc");
                writer.Write(AVIIF_KEYFRAME);
                writer.Write(offset);
                writer.Write(length);
            }
        }

        private void FinalizeFile()
        {
            if (finalized)
            {
                return;
            }
            finalized = true;

            long moviEnd = stream.Position; // フレーム本体の終端（idx1 の直前）
            WriteIndex();
            long fileEnd = stream.Position;

            stream.Position = riffSizePosition;
            writer.Write((uint)(fileEnd - 8));

            stream.Position = moviSizePosition;
            writer.Write((uint)(moviEnd - moviDataStart)); // 'movi' FOURCC + 全フレーム

            stream.Position = totalFramesPosition;
            writer.Write((uint)index.Count);

            stream.Position = streamLengthPosition;
            writer.Write((uint)index.Count);

            stream.Position = fileEnd;
            writer.Flush();
        }

        public void Dispose()
        {
            try
            {
                FinalizeFile();
            }
            finally
            {
                writer.Dispose();
                encoderParameters.Dispose();
            }
        }
    }
}
