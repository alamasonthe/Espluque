using System.IO;
using System.Windows.Controls;

namespace HexaEditor
{
    public partial class HexEditUC : UserControl, IDisposable
    {
        private readonly FileStream _fileStream;
        private bool _disposed;

        public HexEditUC(string filePath)
        {
            InitializeComponent();

            _fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            HexEditor.Stream = _fileStream;
            HexEditor.ReadOnlyMode = true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            HexEditor.CloseProvider();
            _fileStream.Dispose();
        }
    }
}