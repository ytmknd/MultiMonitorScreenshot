using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MultiMonitorScreenshot
{
    /// <summary>
    /// Writes screen frames to an MP4 file by piping raw BGR0 frames into ffmpeg.
    /// ffmpeg.exe is resolved from the application folder first, then from PATH.
    /// </summary>
    internal sealed class FfmpegVideoWriter : IVideoFrameWriter
    {
        private const int BytesPerPixel = 4;

        private readonly int width;
        private readonly int height;
        private readonly Process process;
        private readonly Stream inputStream;
        private readonly byte[] frameBuffer;
        private readonly byte[] rowBuffer;
        private readonly object errorLock = new();
        private readonly Queue<string> errorLines = new();

        private bool disposed;

        public FfmpegVideoWriter(string path, int width, int height, int framesPerSecond)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Video width and height must be positive.");
            }
            if (framesPerSecond <= 0)
            {
                throw new ArgumentException("Frame rate must be positive.", nameof(framesPerSecond));
            }

            this.width = width;
            this.height = height;
            frameBuffer = new byte[width * height * BytesPerPixel];
            rowBuffer = new byte[width * BytesPerPixel];

            process = StartFfmpeg(ResolveFfmpegPath(), path, width, height, framesPerSecond);
            inputStream = process.StandardInput.BaseStream;
        }

        public void WriteFrame(Bitmap frame)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            if (frame.Width != width || frame.Height != height)
            {
                throw new ArgumentException("Frame size does not match the configured video size.", nameof(frame));
            }

            if (process.HasExited)
            {
                throw CreateFfmpegException("ffmpeg exited before recording finished.");
            }

            var rect = new Rectangle(0, 0, width, height);
            BitmapData data = frame.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            try
            {
                CopyBitmapData(data);
            }
            finally
            {
                frame.UnlockBits(data);
            }

            try
            {
                inputStream.Write(frameBuffer, 0, frameBuffer.Length);
            }
            catch (IOException ex)
            {
                throw CreateFfmpegException("Failed to write a frame to ffmpeg.", ex);
            }
            catch (ObjectDisposedException ex)
            {
                throw CreateFfmpegException("ffmpeg input stream was closed.", ex);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            try
            {
                inputStream.Flush();
            }
            catch
            {
                // ffmpeg may already have closed the pipe after an encoder error.
            }

            inputStream.Dispose();

            if (!process.WaitForExit(60_000))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best effort cleanup.
                }
                finally
                {
                    process.Dispose();
                }

                throw new InvalidOperationException("ffmpeg did not finish within 60 seconds.");
            }

            process.WaitForExit();
            int exitCode = process.ExitCode;
            process.Dispose();

            if (exitCode != 0)
            {
                throw CreateFfmpegException($"ffmpeg failed with exit code {exitCode}.");
            }
        }

        private void CopyBitmapData(BitmapData data)
        {
            int sourceStride = data.Stride;
            int rowBytes = width * BytesPerPixel;

            if (sourceStride == rowBytes)
            {
                Marshal.Copy(data.Scan0, frameBuffer, 0, frameBuffer.Length);
                return;
            }

            for (int y = 0; y < height; y++)
            {
                IntPtr source = sourceStride > 0
                    ? IntPtr.Add(data.Scan0, y * sourceStride)
                    : IntPtr.Add(data.Scan0, (height - 1 - y) * -sourceStride);

                Marshal.Copy(source, rowBuffer, 0, rowBytes);
                Buffer.BlockCopy(rowBuffer, 0, frameBuffer, y * rowBytes, rowBytes);
            }
        }

        private Process StartFfmpeg(string ffmpegPath, string outputPath, int width, int height, int fps)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            startInfo.ArgumentList.Add("-y");
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add("rawvideo");
            startInfo.ArgumentList.Add("-pix_fmt");
            startInfo.ArgumentList.Add("bgr0");
            startInfo.ArgumentList.Add("-video_size");
            startInfo.ArgumentList.Add($"{width}x{height}");
            startInfo.ArgumentList.Add("-framerate");
            startInfo.ArgumentList.Add(fps.ToString(System.Globalization.CultureInfo.InvariantCulture));
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add("pipe:0");
            startInfo.ArgumentList.Add("-an");
            startInfo.ArgumentList.Add("-c:v");
            startInfo.ArgumentList.Add("libx264");
            startInfo.ArgumentList.Add("-preset");
            startInfo.ArgumentList.Add("veryfast");
            startInfo.ArgumentList.Add("-crf");
            startInfo.ArgumentList.Add("23");
            startInfo.ArgumentList.Add("-pix_fmt");
            startInfo.ArgumentList.Add("yuv420p");
            startInfo.ArgumentList.Add("-vf");
            startInfo.ArgumentList.Add("pad=ceil(iw/2)*2:ceil(ih/2)*2");
            startInfo.ArgumentList.Add("-movflags");
            startInfo.ArgumentList.Add("+faststart");
            startInfo.ArgumentList.Add(outputPath);

            var ffmpeg = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            ffmpeg.ErrorDataReceived += (_, e) => AppendErrorLine(e.Data);

            try
            {
                if (!ffmpeg.Start())
                {
                    throw new InvalidOperationException("Failed to start ffmpeg.");
                }
            }
            catch (Exception ex)
            {
                ffmpeg.Dispose();
                throw new InvalidOperationException("ffmpeg could not be started. Install ffmpeg or place ffmpeg.exe next to the application.", ex);
            }

            ffmpeg.BeginErrorReadLine();
            return ffmpeg;
        }

        private static string ResolveFfmpegPath()
        {
            string localPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(localPath))
            {
                return localPath;
            }

            return "ffmpeg";
        }

        private InvalidOperationException CreateFfmpegException(string message, Exception? innerException = null)
        {
            string errorText = GetRecentErrorText();
            string fullMessage = string.IsNullOrWhiteSpace(errorText)
                ? message
                : $"{message}{Environment.NewLine}{errorText}";

            return innerException == null
                ? new InvalidOperationException(fullMessage)
                : new InvalidOperationException(fullMessage, innerException);
        }

        private string GetRecentErrorText()
        {
            lock (errorLock)
            {
                return string.Join(Environment.NewLine, errorLines);
            }
        }

        private void AppendErrorLine(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            lock (errorLock)
            {
                errorLines.Enqueue(line);
                while (errorLines.Count > 20)
                {
                    errorLines.Dequeue();
                }
            }
        }
    }
}
