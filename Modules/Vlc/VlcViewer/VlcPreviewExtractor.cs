using LibVLCSharp.Shared;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VlcMedia = LibVLCSharp.Shared.Media;

namespace VlcViewer
{
    public sealed class VlcPreviewExtractor : IDisposable
    {
        private const int MaxWidth = 1280;
        private const int MaxHeight = 720;

        private readonly TaskCompletionSource<FrameData?> _frameSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly LibVLCSharp.Shared.MediaPlayer.LibVLCVideoFormatCb _formatCallback;
        private readonly LibVLCSharp.Shared.MediaPlayer.LibVLCVideoCleanupCb _cleanupCallback;
        private readonly LibVLCSharp.Shared.MediaPlayer.LibVLCVideoLockCb _lockCallback;
        private readonly LibVLCSharp.Shared.MediaPlayer.LibVLCVideoDisplayCb _displayCallback;

        private IntPtr _buffer;
        private int _width;
        private int _height;
        private int _pitch;
        private int _lines;
        private int _frameCaptured;

        public VlcPreviewExtractor()
        {
            _formatCallback = FormatCallback;
            _cleanupCallback = CleanupCallback;
            _lockCallback = LockCallback;
            _displayCallback = DisplayCallback;
        }

        public async Task<BitmapSource?> ExtractAsync(LibVLC libVLC, string filePath, long preferredTime, CancellationToken cancellationToken)
        {
            long duration;

            using (VlcMedia probe = new VlcMedia(libVLC, filePath, FromType.FromPath))
            {
                await probe.Parse(MediaParseOptions.ParseLocal);
                duration = probe.Duration;
            }

            long targetTime = duration > 0 ? Math.Min(preferredTime, duration / 2) : 0;

            using VlcMedia media = new VlcMedia(libVLC, filePath, FromType.FromPath);

            media.AddOption(":no-audio");
            media.AddOption(":avcodec-hw=none");

            if (targetTime > 0)
            {
                string startTime = (targetTime / 1000d).ToString("0.###", CultureInfo.InvariantCulture);
                media.AddOption($":start-time={startTime}");
            }

            using LibVLCSharp.Shared.MediaPlayer mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(media);

            mediaPlayer.SetVideoFormatCallbacks(_formatCallback, _cleanupCallback);
            mediaPlayer.SetVideoCallbacks(_lockCallback, null, _displayCallback);

            if (!mediaPlayer.Play())
            {
                return null;
            }

            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            Task completedTask = await Task.WhenAny(_frameSource.Task, timeoutTask);

            FrameData? frame = completedTask == _frameSource.Task
                ? await _frameSource.Task
                : null;

            await StopAsync(mediaPlayer);

            if (frame is null)
            {
                return null;
            }

            BitmapSource bitmap = BitmapSource.Create(
                frame.Width,
                frame.Height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                frame.Pixels,
                frame.Pitch);

            bitmap.Freeze();

            return bitmap;
        }

        private uint FormatCallback(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
        {
            double scale = Math.Min(1d, Math.Min((double)MaxWidth / width, (double)MaxHeight / height));

            _width = Math.Max(2, (int)(width * scale)) & ~1;
            _height = Math.Max(2, (int)(height * scale)) & ~1;
            _pitch = Align(_width * 4);
            _lines = Align(_height);

            Marshal.Copy(Encoding.ASCII.GetBytes("RV32"), 0, chroma, 4);

            width = (uint)_width;
            height = (uint)_height;
            pitches = (uint)_pitch;
            lines = (uint)_lines;

            FreeBuffer();
            _buffer = Marshal.AllocHGlobal(_pitch * _lines);

            return 1;
        }

        private void CleanupCallback(ref IntPtr opaque)
        {
            FreeBuffer();
        }

        private IntPtr LockCallback(IntPtr opaque, IntPtr planes)
        {
            Marshal.WriteIntPtr(planes, _buffer);
            return IntPtr.Zero;
        }

        private void DisplayCallback(IntPtr opaque, IntPtr picture)
        {
            if (Interlocked.Exchange(ref _frameCaptured, 1) != 0 || _buffer == IntPtr.Zero)
            {
                return;
            }

            byte[] pixels = new byte[_pitch * _height];
            Marshal.Copy(_buffer, pixels, 0, pixels.Length);

            _frameSource.TrySetResult(new FrameData(_width, _height, _pitch, pixels));
        }

        private static async Task StopAsync(LibVLCSharp.Shared.MediaPlayer mediaPlayer)
        {
            TaskCompletionSource stoppedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnStopped(object? sender, EventArgs e)
            {
                stoppedSource.TrySetResult();
            }

            mediaPlayer.Stopped += OnStopped;
            mediaPlayer.Stop();

            await Task.WhenAny(stoppedSource.Task, Task.Delay(3000));

            mediaPlayer.Stopped -= OnStopped;
        }

        private static int Align(int value)
        {
            return value % 32 == 0 ? value : ((value / 32) + 1) * 32;
        }

        private void FreeBuffer()
        {
            IntPtr buffer = Interlocked.Exchange(ref _buffer, IntPtr.Zero);

            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public void Dispose()
        {
            FreeBuffer();
        }

        private sealed record FrameData(int Width, int Height, int Pitch, byte[] Pixels);
    }
}