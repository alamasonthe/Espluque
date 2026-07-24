using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ImageMagick;

namespace MagickViewer
{
    public partial class ImageViewerUC : UserControl
    {
        private string _filePath;

        public ImageViewerUC()
        {
            InitializeComponent();
        }

        public ImageViewerUC(string filePath) : this()
        {
            OpenFile(filePath);
        }

        public async Task OpenFile(string filePath)
        {
            _filePath = filePath;
            BitmapImage? bitmapImage = await LoadBitmapImageAsync(filePath);

            if (bitmapImage is not null)
            {
                ImageControl.Source = bitmapImage;
            }
        }

        private static async Task<BitmapImage?> LoadBitmapImageAsync(string filePath)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using MagickImage image = new(filePath);
                    using MemoryStream stream = new();

                    image.Write(stream, MagickFormat.Png);
                    stream.Position = 0;

                    BitmapImage bitmapImage = new();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                });
            }
            catch (MagickException)
            {
                return null;
            }
        }

    }
}
