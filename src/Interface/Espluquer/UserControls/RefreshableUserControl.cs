using System.Windows;
using System.Windows.Controls;

namespace Espluquer.UserControls
{
    public class RefreshableUserControl : UserControl
    {
        private bool _isRefreshing;

        public RefreshableUserControl()
        {
            IsVisibleChanged += UserControl_IsVisibleChanged;
        }

        private async void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || _isRefreshing)
            {
                return;
            }

            _isRefreshing = true;

            try
            {
                await RefreshAsync();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        protected virtual Task RefreshAsync()
        {
            return Task.CompletedTask;
        }
    }
}