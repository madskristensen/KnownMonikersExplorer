using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    public partial class KnownMonikersExplorerControl : UserControl, INotifyPropertyChanged
    {
        private readonly ServicesDTO _state;
        private IReadOnlyList<KnownMonikersViewModel> _filtered;

        public event PropertyChangedEventHandler PropertyChanged;

        public KnownMonikersExplorerControl(ServicesDTO state)
        {
            Loaded += OnLoaded;
            _state = state;
            _filtered = Array.Empty<KnownMonikersViewModel>();
            DataContext = this;
            InitializeComponent();
        }

        public IEnumerable<KnownMonikersViewModel> Monikers => _filtered;

        /// <summary>
        /// Gets all monikers for search filtering.
        /// </summary>
        public IReadOnlyList<KnownMonikersViewModel> AllMonikers => _state.Monikers;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            list.KeyUp += List_KeyUp;
            list.MouseDoubleClick += Export_Click;
            // Initial population once background load done
            _ = Task.Run(WaitAndBindInitialAsync);
        }

        private async Task WaitAndBindInitialAsync()
        {
            // Wait for background population to complete (simple polling)
            while (_state.Monikers.Count == 0)
            {
                await Task.Delay(50);
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ApplyFilter(_state.Monikers); // show all
        }

        /// <summary>
        /// Applies the filtered results from the native tool window search.
        /// </summary>
        public void ApplyFilter(IReadOnlyList<KnownMonikersViewModel> items)
        {
            _filtered = items;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Monikers)));
        }

        /// <summary>
        /// Clears the filter and shows all monikers.
        /// </summary>
        public void ClearFilter()
        {
            ApplyFilter(_state.Monikers);
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

        /// <summary>
        /// Attempts to select the specified moniker in the list.
        /// </summary>
        /// <param name="moniker">The moniker to select.</param>
        /// <param name="needsClearSearch">True if the search filter needs to be cleared (moniker was found in full list but not in filtered view).</param>
        /// <returns>True if the moniker was found and selected, false otherwise.</returns>
        internal bool SelectMoniker(ImageMoniker moniker, out bool needsClearSearch)
        {
            needsClearSearch = false;

            // First try to find in the current filtered view
            KnownMonikersViewModel match = _filtered.FirstOrDefault(x => x.Moniker.Equals(moniker));

            if (match == null)
            {
                // Not found in filtered view, search the full list
                match = _state.Monikers.FirstOrDefault(x => x.Moniker.Equals(moniker));
                if (match != null)
                {
                    // Signal that the search needs to be cleared
                    needsClearSearch = true;
                    // Clear the filter to show all monikers so the selection is visible
                    ClearFilter();
                }
            }

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
