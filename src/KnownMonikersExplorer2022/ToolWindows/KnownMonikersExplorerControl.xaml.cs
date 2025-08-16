using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KnownMonikersExplorer.Windows;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;

namespace KnownMonikersExplorer.ToolWindows
{
    public partial class KnownMonikersExplorerControl : UserControl
    {
        private readonly ServicesDTO _state;
        private readonly ObservableCollection<KnownMonikersViewModel> _filtered = new ObservableCollection<KnownMonikersViewModel>();
        private CancellationTokenSource _filterCts;
        private string _lastFilter = string.Empty;

        public KnownMonikersExplorerControl(ServicesDTO state)
        {
            Loaded += OnLoaded;
            _state = state;
            DataContext = this;
            InitializeComponent();
        }

        public IEnumerable<KnownMonikersViewModel> Monikers => _filtered;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            list.KeyUp += List_KeyUp;
            list.MouseDoubleClick += Export_Click;
            txtFilter.KeyDown += TxtFilter_KeyDown;
            _ = txtFilter.Focus();
            // Initial population once background load done
            _ = Task.Run(WaitAndBindInitialAsync);
        }

        private void TxtFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && !string.IsNullOrEmpty(txtFilter.Text))
            {
                txtFilter.Clear();
                e.Handled = true;
            }
        }

        private async Task WaitAndBindInitialAsync()
        {
            // Wait for background population to complete (simple polling)
            while (_state.Monikers.Count == 0)
            {
                await Task.Delay(50);
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ApplyFilterCore(string.Empty, _state.Monikers); // show all
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = ((TextBox)sender).Text.Trim();
            QueueFilter(text);
        }

        private void QueueFilter(string text)
        {
            // Debounce + cancel previous
            _filterCts?.Cancel();
            _filterCts = new CancellationTokenSource();
            CancellationToken token = _filterCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(180, token); // debounce
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    var lowered = text.ToLowerInvariant();
                    // If user is adding characters and previous filter was a prefix, incremental filtering
                    IEnumerable<KnownMonikersViewModel> source = _state.Monikers;
                    if (!string.IsNullOrEmpty(_lastFilter) && lowered.StartsWith(_lastFilter) && _filtered.Count > 0)
                    {
                        source = _filtered.ToList(); // snapshot current subset
                    }

                    IEnumerable<KnownMonikersViewModel> results = string.IsNullOrEmpty(lowered)
                        ? source
                        : source.Where(m => m.MatchSearchTerm(lowered)).ToList();

                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
                    if (!token.IsCancellationRequested)
                    {
                        ApplyFilterCore(lowered, results);
                    }
                }
                catch (TaskCanceledException) { }
            }, token);
        }

        private void ApplyFilterCore(string loweredFilter, IEnumerable<KnownMonikersViewModel> items)
        {
            _lastFilter = loweredFilter;
            _filtered.Clear();
            foreach (KnownMonikersViewModel vm in items)
            {
                _filtered.Add(vm);
            }
        }

        private void List_KeyUp(object sender, KeyEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (e.Key == Key.Enter)
            {
                Export_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                CopyName_Click(this, new RoutedEventArgs());
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var model = (KnownMonikersViewModel)list.SelectedItem;
            if (model == null)
            {
                return;
            }
            var export = new ExportMonikerWindow(model)
            {
                Owner = Application.Current.MainWindow
            };

            _ = export.ShowDialog();
        }

        private void CopyName_Click(object sender, RoutedEventArgs e)
        {
            var model = (KnownMonikersViewModel)list.SelectedItem;
            if (model != null)
            {
                Clipboard.SetText(model.Name);
            }
        }

        private void CopyGuidAndId_Click(object sender, RoutedEventArgs e)
        {
            var model = (KnownMonikersViewModel)list.SelectedItem;
            if (model != null)
            {
                Clipboard.SetText($"{model.Moniker.Guid}, {model.Moniker.Id}");
            }
        }

        private void List_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem listViewItem = VisualTreeHelperExtensions.FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

            if (listViewItem is null)
            {
                e.Handled = true;
            }
        }

        internal bool SelectMoniker(ImageMoniker moniker)
        {
            // Search in full list if not found in filtered view
            KnownMonikersViewModel match = _filtered.FirstOrDefault(x => x.Moniker.Equals(moniker)) ?? _state.Monikers.FirstOrDefault((x) => x.Moniker.Equals(moniker));
            if (match != null)
            {
                list.SelectedItem = match;
                list.ScrollIntoView(match);
                return true;
            }
            return false;
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
