using System.IO;
using System.Windows.Controls;
using Util;

namespace TinyHtml
{
    public partial class TinyHtmlUC : UserControl
    {
        private readonly string _filePath;
        public TinyHtmlUC()
        {
            InitializeComponent();
        }

        public TinyHtmlUC(string filePath) : this()
        {
            _filePath = filePath;

            Result<FileStream> fileStreamResult = Util.File.OpenRead(filePath);
            if (!fileStreamResult.IsSuccess)
            {
                HtmlViewer.Html = $"{fileStreamResult.Error!.Code} - {fileStreamResult.Error.Message}";
                return;
            }

            using FileStream fileStream = fileStreamResult.Value!;
            using StreamReader streamReader = new(fileStream);
            HtmlViewer.Html = streamReader.ReadToEnd();
        }
    }
}
