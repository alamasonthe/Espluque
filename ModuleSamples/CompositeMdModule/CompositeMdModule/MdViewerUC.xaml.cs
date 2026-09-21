using System.Windows.Controls;
using System.IO;

namespace CompositeMdModule
{
    public partial class MdViewerUC : UserControl
    {
        string _filePath;

        public MdViewerUC(string filePath)
        {
            InitializeComponent();

            _filePath = filePath;

            var fileStreamResult = Util.File.OpenRead(filePath);
            if (!fileStreamResult.IsSuccess)
            {
                MarkdownViewer.Markdown = $"{fileStreamResult.Error!.Code} - {fileStreamResult.Error.Message}";
                return;
            }

            using FileStream fileStream = fileStreamResult.Value!;
            using StreamReader streamReader = new(fileStream);
            MarkdownViewer.Markdown = streamReader.ReadToEnd();
        }
    }
}
