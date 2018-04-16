using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace KnownMonikersExplorer.ToolWindows
{
    public partial class KnownMonikersExplorerControl : UserControl
    {
        private const string _filterPlaceholder = "Search";
        private readonly ServicesDTO _state;

        public KnownMonikersExplorerControl(ServicesDTO state)
        {
            Loaded += OnLoaded;
            InitializeComponent();
            _state = state;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            list.KeyUp += List_KeyUp;
            list.MouseDoubleClick += Export_Click;
            txtFilter.Focus();

            PropertyInfo[] properties = typeof(KnownMonikers).GetProperties(BindingFlags.Static | BindingFlags.Public);

            list.ItemsSource = properties.Select(p => new KnownMonikersViewModel(p.Name, (ImageMoniker)p.GetValue(null, null)));

            var view = (CollectionView)CollectionViewSource.GetDefaultView(list.ItemsSource);
            view.Filter = UserFilter;
        }

        private void List_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Export_Click(this, new RoutedEventArgs());
            }
            else  if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Copy_Click(this, new RoutedEventArgs());
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
                return ((item as KnownMonikersViewModel).Name.IndexOf(txtFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = ((TextBox)sender).Text;
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
            var model = (KnownMonikersViewModel)list.SelectedItem;
            var export = new ExportMonikerWindow(model, _state.ImageService);

            var hwnd = new IntPtr(_state.DTE.MainWindow.HWnd);
            var window = (System.Windows.Window)HwndSource.FromHwnd(hwnd).RootVisual;
            export.Owner = window;

            export.ShowDialog();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var model = (KnownMonikersViewModel)list.SelectedItem;
            Clipboard.SetText(model.Name);
        }
    }
}
