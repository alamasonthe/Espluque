using System.Windows;
using System.Windows.Controls;

namespace Espluquer.Usercontrols.Components
{
    public partial class KeyValueListUC : UserControl
    {
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(
                nameof(Items),
                typeof(IEnumerable<KeyValuePair<string, string?>>),
                typeof(KeyValueListUC),
                new PropertyMetadata(null));

        public IEnumerable<KeyValuePair<string, string?>>? Items
        {
            get => (IEnumerable<KeyValuePair<string, string?>>?)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public KeyValueListUC()
        {
            InitializeComponent();
        }
    }
}