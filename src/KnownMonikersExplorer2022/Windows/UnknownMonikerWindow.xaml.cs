using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.Imaging.Interop;

namespace KnownMonikersExplorer.Windows
{
    public partial class UnknownMonikerWindow : Window
    {
        private readonly ImageMoniker _moniker;

        public UnknownMonikerWindow(ImageMoniker moniker)
        {
            _moniker = moniker;
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            imgMoniker.Moniker = _moniker;
            txtGuid.Text = _moniker.Guid.ToString("B");
            txtId.Text = _moniker.Id.ToString();
        }

        private void CopyGuid_Click(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(_moniker.Guid.ToString("B"));
        }

        private void CopyId_Click(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(_moniker.Id.ToString());
        }

        private void CopyBoth_Click(object sender, RoutedEventArgs e)
        {
            var text = $"GUID: {_moniker.Guid:B}{Environment.NewLine}ID: {_moniker.Id}";
            Clipboard.SetText(text);
        }
    }
}
