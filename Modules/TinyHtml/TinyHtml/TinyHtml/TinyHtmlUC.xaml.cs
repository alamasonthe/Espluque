using System.Windows.Controls;
using System.IO;

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

            HtmlViewer.Html = File.ReadAllText(filePath);
        }
    }
}
