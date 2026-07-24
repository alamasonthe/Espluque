using Espluque.Theming.Services;
using ICSharpCode.AvalonEdit.Highlighting;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace AvalonEditViewer
{
    public partial class TextEditorUC : UserControl
    {
        private readonly string? _filePath;

        public TextEditorUC()
        {
            InitializeComponent();
        }

        public TextEditorUC(string filePath) : this()
        {
            _filePath = filePath;
            OpenFile(filePath);

            ThemeService.ThemeChanged += ThemeService_ThemeChanged;
        }

        public void OpenFile(string filePath)
        {
            TextBoxEditor.Load(filePath);
            TextBoxEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(filePath));
            AvalonThemeService.ApplyTheme(TextBoxEditor, filePath);
        }

        private void TextEditorUC_Unloaded(object sender, RoutedEventArgs e)
        {
            ThemeService.ThemeChanged -= ThemeService_ThemeChanged;
        }

        private void ThemeService_ThemeChanged(string themeTag)
        {
            if (!string.IsNullOrWhiteSpace(_filePath))
            {
                AvalonThemeService.ApplyTheme(TextBoxEditor, _filePath);
            }
        }
    }
}
