using System.IO;
using System.Windows.Controls;
using Util;

namespace HexaEditor
{
    public partial class HexEditUC : UserControl, IDisposable
    {
        private readonly FileStream? _fileStream;
        private bool _disposed;

        public HexEditUC(string filePath)
        {
            InitializeComponent();

            Result<FileStream> fileStreamResult = Util.File.OpenRead(filePath);
            if (!fileStreamResult.IsSuccess)
            {
                HexEditor.ReadOnlyMode = true;
                return;
            }

            _fileStream = fileStreamResult.Value!;
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
            _fileStream?.Dispose();
        }
    }
}