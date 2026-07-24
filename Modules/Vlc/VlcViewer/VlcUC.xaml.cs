
using Espluque.Theming.Services;
using LibVLCSharp.Shared;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace VlcViewer
{
    public partial class VlcUC : UserControl, IDisposable
    {
        private readonly string? _filePath;
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;

        private const long PreferredPreviewTime = 5000;
        private bool _isPreviewExtractionStarted;
        private CancellationTokenSource? _previewCancellation;

        private bool _isMuted;

        private bool _isDisposed;

        public VlcUC(string filePath)
        {
            InitializeComponent();

            _filePath = filePath;
            PlayPauseIcon.Text = IconService.FluentGlyph("ic_fluent_play_48_regular");
            MuteIcon.Text = IconService.FluentGlyph("ic_fluent_speaker_2_24_regular");
        }

        public VlcUC()
        {
            InitializeComponent();
        }

        #region remote control

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer?.State == VLCState.Playing)
            {
                _mediaPlayer.SetPause(true);
                PlayPauseIcon.Text = IconService.FluentGlyph("ic_fluent_play_48_regular");
                return;
            }

            if (_mediaPlayer?.State == VLCState.Paused)
            {
                _mediaPlayer.SetPause(false);
                PlayPauseIcon.Text = IconService.FluentGlyph("ic_fluent_pause_48_regular");
                return;
            }

            if (string.IsNullOrWhiteSpace(_filePath) || !System.IO.File.Exists(_filePath))
            {
                return;
            }

            _libVLC ??= new LibVLC("--quiet", "--no-video-title-show");

            if (_mediaPlayer is null)
            {
                _mediaPlayer = new MediaPlayer(_libVLC);

                _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
                _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;

                VideoView.MediaPlayer = _mediaPlayer;
            }

            using Media media = new Media(_libVLC, _filePath, FromType.FromPath);

            if (_mediaPlayer.Play(media))
            {
                PreviewLayer.Visibility = Visibility.Collapsed;
                PlayPauseIcon.Text = IconService.FluentGlyph("ic_fluent_pause_48_regular");
            }
        }

        #endregion


        #region Open/Close UC

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            VideoView.MediaPlayer = null;

            if (_mediaPlayer is not null)
            {
                _mediaPlayer.TimeChanged -= MediaPlayer_TimeChanged;
                _mediaPlayer.LengthChanged -= MediaPlayer_LengthChanged;

                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }

            if (_libVLC is not null)
            {
                _libVLC.Dispose();
                _libVLC = null;
            }

            GC.SuppressFinalize(this);
        }

        #endregion


        #region splash screen image

        private async void VlcUC_IsVisibleChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            bool isVisible = (bool)e.NewValue;

            ControlOverlay.Visibility = isVisible
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (!isVisible ||
                _isPreviewExtractionStarted ||
                _isDisposed)
            {
                return;
            }

            _isPreviewExtractionStarted = true;

            await LoadPreviewAsync();
        }

        private async Task LoadPreviewAsync()
        {
            if (string.IsNullOrWhiteSpace(_filePath) || !System.IO.File.Exists(_filePath))
            {
                return;
            }

            _libVLC ??= new LibVLC("--quiet", "--no-video-title-show");

            _previewCancellation = new CancellationTokenSource();

            using VlcPreviewExtractor extractor = new();

            BitmapSource? preview = await extractor.ExtractAsync(_libVLC, _filePath, PreferredPreviewTime, _previewCancellation.Token);

            if (preview is not null && !_isDisposed)
            {
                PreviewImage.Source = preview;
            }
        }

        #endregion


        #region speaker

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer is null)
            {
                return;
            }

            _isMuted = !_isMuted;
            _mediaPlayer.Mute = _isMuted;

            MuteIcon.Text = IconService.FluentGlyph(
                _isMuted
                    ? "ic_fluent_speaker_mute_24_regular"
                    : "ic_fluent_speaker_2_24_regular");
        }

        #endregion


        #region timing

        private void MediaPlayer_TimeChanged(
    object? sender,
    MediaPlayerTimeChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                CurrentTimeText.Text = FormatTime(e.Time);
            }));
        }

        private void MediaPlayer_LengthChanged(
            object? sender,
            MediaPlayerLengthChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TotalTimeText.Text = FormatTime(e.Length);
            }));
        }

        private static string FormatTime(long milliseconds)
        {
            if (milliseconds < 0)
            {
                milliseconds = 0;
            }

            TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);

            return time.TotalHours >= 1
                ? time.ToString(@"h\:mm\:ss")
                : time.ToString(@"mm\:ss");
        }

        #endregion
    }
}