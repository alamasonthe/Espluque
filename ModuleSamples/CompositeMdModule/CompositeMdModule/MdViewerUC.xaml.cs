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

            if (File.Exists(filePath))
            {
                MarkdownViewer.Markdown = File.ReadAllText(filePath);
            }
        }
    }
}
