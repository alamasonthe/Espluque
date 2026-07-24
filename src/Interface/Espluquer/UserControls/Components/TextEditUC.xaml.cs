using Espluque.Theming.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;

namespace Espluquer.UserControls.Components
{
    public partial class TextEditUC : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(TextEditUC),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        public static readonly DependencyProperty IsWordWrapEnabledProperty =
            DependencyProperty.Register(
                nameof(IsWordWrapEnabled),
                typeof(bool),
                typeof(TextEditUC),
                new PropertyMetadata(false, OnIsWordWrapEnabledChanged));

        public static readonly DependencyProperty AreLineNumbersVisibleProperty =
            DependencyProperty.Register(
                nameof(AreLineNumbersVisible),
                typeof(bool),
                typeof(TextEditUC),
                new PropertyMetadata(true, OnAreLineNumbersVisibleChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(TextEditUC),
                new PropertyMetadata(false));

        private ScrollViewer? _editorScrollViewer;
        private bool _lineNumberUpdateQueued;

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public bool IsWordWrapEnabled
        {
            get => (bool)GetValue(IsWordWrapEnabledProperty);
            set => SetValue(IsWordWrapEnabledProperty, value);
        }

        public bool AreLineNumbersVisible
        {
            get => (bool)GetValue(AreLineNumbersVisibleProperty);
            set => SetValue(AreLineNumbersVisibleProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public TextEditUC()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            TextBoxEditor.TextChanged += OnEditorTextChanged;
            TextBoxEditor.SizeChanged += OnEditorSizeChanged;

        }

        public TextEditUC(string filePath) : this()
        {
            Text = ReadTextFile(filePath);

            WordWrapButton.Content = IconService.FluentGlyph("ic_fluent_text_wrap_20_regular");
            LineNumbersButton.Content = IconService.FluentGlyph("ic_fluent_number_row_20_regular");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _editorScrollViewer = FindVisualChild<ScrollViewer>(TextBoxEditor);

            if (_editorScrollViewer is not null)
            {
                _editorScrollViewer.ScrollChanged += OnEditorScrollChanged;
            }

            ApplyWordWrap();
            ApplyLineNumberVisibility();
            QueueLineNumberUpdate();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_editorScrollViewer is not null)
            {
                _editorScrollViewer.ScrollChanged -= OnEditorScrollChanged;
                _editorScrollViewer = null;
            }
        }

        private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is TextEditUC textEditUC)
            {
                textEditUC.QueueLineNumberUpdate();
            }
        }

        private static void OnIsWordWrapEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is TextEditUC textEditUC)
            {
                textEditUC.ApplyWordWrap();
            }
        }

        private static void OnAreLineNumbersVisibleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is TextEditUC textEditUC)
            {
                textEditUC.ApplyLineNumberVisibility();
            }
        }

        private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            QueueLineNumberUpdate();
        }

        private void OnEditorSizeChanged(object sender, SizeChangedEventArgs e)
        {
            QueueLineNumberUpdate();
        }

        private void OnEditorScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            QueueLineNumberUpdate();
        }

        private void ApplyWordWrap()
        {
            TextBoxEditor.TextWrapping = IsWordWrapEnabled ? TextWrapping.Wrap : TextWrapping.NoWrap;
            TextBoxEditor.HorizontalScrollBarVisibility = IsWordWrapEnabled ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
            WordWrapButton.Content = IconService.FluentGlyph(IsWordWrapEnabled ? "ic_fluent_text_wrap_20_regular" : "ic_fluent_text_wrap_off_20_regular");

            QueueLineNumberUpdate();
        }

        private void ApplyLineNumberVisibility()
        {
            LineNumberBackground.Visibility = AreLineNumbersVisible ? Visibility.Visible : Visibility.Collapsed;

            QueueLineNumberUpdate();
        }

        private void QueueLineNumberUpdate()
        {
            if (_lineNumberUpdateQueued)
            {
                return;
            }

            _lineNumberUpdateQueued = true;

            Dispatcher.InvokeAsync(() =>
            {
                _lineNumberUpdateQueued = false;
                UpdateLineNumbers();
            }, DispatcherPriority.Render);
        }

        private void UpdateLineNumbers()
        {
            LineNumberCanvas.Children.Clear();

            if (!IsLoaded || !AreLineNumbersVisible)
            {
                return;
            }

            int actualLineCount = TextBoxEditor.LineCount;
            int displayLineCount = Math.Max(1, actualLineCount);

            UpdateLineNumberColumnWidth(displayLineCount);

            if (actualLineCount <= 0)
            {
                return;
            }

            int firstVisibleLineIndex = TextBoxEditor.GetFirstVisibleLineIndex();
            int lastVisibleLineIndex = TextBoxEditor.GetLastVisibleLineIndex();

            if (firstVisibleLineIndex < 0 || lastVisibleLineIndex < 0)
            {
                return;
            }

            firstVisibleLineIndex = Math.Max(0, firstVisibleLineIndex);
            lastVisibleLineIndex = Math.Min(actualLineCount - 1, lastVisibleLineIndex);

            if (firstVisibleLineIndex > lastVisibleLineIndex)
            {
                return;
            }

            LineNumberCanvas.Width = LineNumberBackground.Width;
            LineNumberCanvas.Height = TextBoxEditor.ActualHeight;

            for (int lineIndex = firstVisibleLineIndex; lineIndex <= lastVisibleLineIndex; lineIndex++)
            {
                int characterIndex = TextBoxEditor.GetCharacterIndexFromLineIndex(lineIndex);
                Rect characterRect = TextBoxEditor.GetRectFromCharacterIndex(characterIndex, true);

                if (characterRect.IsEmpty)
                {
                    continue;
                }

                TextBlock lineNumberTextBlock = new()
                {
                    Text = (lineIndex + 1).ToString(),
                    Width = LineNumberBackground.Width - 8,
                    TextAlignment = TextAlignment.Right,
                    FontFamily = TextBoxEditor.FontFamily,
                    FontSize = TextBoxEditor.FontSize,
                    Opacity = 0.65
                };

                lineNumberTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "App.Text");

                Canvas.SetLeft(lineNumberTextBlock, 0);
                Canvas.SetTop(lineNumberTextBlock, characterRect.Top);

                LineNumberCanvas.Children.Add(lineNumberTextBlock);
            }
        }

        private void UpdateLineNumberColumnWidth(int lineCount)
        {
            int digitCount = Math.Max(2, lineCount.ToString().Length);
            double width = 18 + digitCount * TextBoxEditor.FontSize * 0.7;

            LineNumberBackground.Width = Math.Max(42, Math.Ceiling(width));
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    return typedChild;
                }

                T? nestedChild = FindVisualChild<T>(child);

                if (nestedChild is not null)
                {
                    return nestedChild;
                }
            }

            return null;
        }

        private static string ReadTextFile(string filePath)
        {
            using FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader streamReader = new(fileStream, detectEncodingFromByteOrderMarks: true);

            return streamReader.ReadToEnd();
        }

    }
}