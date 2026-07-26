using System.Windows.Controls;

namespace TinyHtml
{
    public partial class CustomUserControl : UserControl
    {
        string _filePath;

        public CustomUserControl(string filePath)
        {
            InitializeComponent();

            _filePath = filePath;
            FilePathTextBlock.Text = _filePath;
        }
    }
}
