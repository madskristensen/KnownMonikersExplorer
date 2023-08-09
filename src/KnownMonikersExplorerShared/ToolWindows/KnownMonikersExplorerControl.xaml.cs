using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace KnownMonikersExplorer.ToolWindows
{
    public partial class KnownMonikersExplorerControl : UserControl
    {
        private readonly ServicesDTO _state;

        public KnownMonikersExplorerControl(ServicesDTO state)
        {
            Loaded += OnLoaded;
            _state = state;
            DataContext = this;
            InitializeComponent();
        }

        public IEnumerable<KnownMonikersViewModel> Monikers => _state.Monikers;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            list.KeyUp += List_KeyUp;
            list.MouseDoubleClick += Export_Click;
            txtFilter.Focus();

            var view = (CollectionView)CollectionViewSource.GetDefaultView(list.ItemsSource);
            view.Filter = UserFilter;
        }

        private void List_KeyUp(object sender, KeyEventArgs e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (e.Key == Key.Enter)
            {
                Export_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                CopyName_Click(this, new RoutedEventArgs());
            }
        }

        private bool UserFilter(object item)
        {
            if (string.IsNullOrEmpty(txtFilter.Text))
            {
                return true;
            }
            else
            {
                return (item as KnownMonikersViewModel).Name.IndexOf(txtFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = ((TextBox)sender).Text;
            RefreshAsync(text).ConfigureAwait(false);
        }

        private async Task RefreshAsync(string text)
        {
            await Task.Delay(200);

            if (text == txtFilter.Text)
            {
                CollectionViewSource.GetDefaultView(list.ItemsSource).Refresh();
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var model = (KnownMonikersViewModel)list.SelectedItem;
            var export = new ExportMonikerWindow(model, _state.DTE);

            export.Owner = Application.Current.MainWindow;

            export.ShowDialog();
        }

        private void CopyName_Click(object sender, RoutedEventArgs e)
        {
            var model = (KnownMonikersViewModel)list.SelectedItem;
            Clipboard.SetText(model.Name);
        }

        private void CopyGuidAndId_Click(object sender, RoutedEventArgs e)
        {
            var model = (KnownMonikersViewModel)list.SelectedItem;
            Clipboard.SetText($"{model.Moniker.Guid}, {model.Moniker.Id}");
        }

        private void List_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem listViewItem = VisualTreeHelperExtensions.FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

            if (listViewItem is null)
            {
                e.Handled = true;
            }
        }
    }

    internal static class VisualTreeHelperExtensions
    {
        internal static T FindAncestor<T>(DependencyObject dependencyObject) where T : class
        {
            DependencyObject target = dependencyObject;

            do
            {
                target = VisualTreeHelper.GetParent(target);
            }
            while (target != null && !(target is T));

            return target as T;
        }
    }
}
