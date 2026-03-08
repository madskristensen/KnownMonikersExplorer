using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Community.VisualStudio.Toolkit;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace KnownMonikersExplorer.ToolWindows
{
    public class KnownMonikersExplorerWindow : BaseToolWindow<KnownMonikersExplorerWindow>
    {
        public override string GetTitle(int toolWindowId) => "KnownMonikers Explorer";

        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            var state = new ServicesDTO();

            // Kick off background population without blocking UI thread
            _ = Task.Run(async () =>
            {
                try
                {
                    await state.PopulateAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }, cancellationToken);

            await Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            return new KnownMonikersExplorerControl(state);
        }

        [Guid("cfff3162-9c8d-4244-b0a7-e3b39a968b24")]
        public class Pane : ToolWindowPane
        {
            private CancellationTokenSource _debounceCts;
            private readonly object _debounceLock = new object();

            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.Image;
            }

            public override bool SearchEnabled => true;

            public override IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
            {
                if (pSearchQuery == null || pSearchCallback == null)
                {
                    return null;
                }

                // Cancel any pending debounced search
                CancellationToken ct;
                lock (_debounceLock)
                {
                    _debounceCts?.Cancel();
                    _debounceCts = new CancellationTokenSource();
                    ct = _debounceCts.Token;
                }

                int debounceDelayMs = GetDebounceDelayMs(pSearchQuery.SearchString);
                return new KnownMonikersSearchTask(dwCookie, pSearchQuery, pSearchCallback, this, ct, debounceDelayMs);
            }

            private static int GetDebounceDelayMs(string searchString)
            {
                int queryLength = searchString?.Trim().Length ?? 0;
                switch (queryLength)
                {
                    case 0:
                    case 1:
                        return 200;
                    case 2:
                    case 3:
                        return 300;
                    default:
                        return queryLength <= 8 ? 400 : 500;
                }
            }

            public override void ClearSearch()
            {
                lock (_debounceLock)
                {
                    _debounceCts?.Cancel();
                }

                if (Content is KnownMonikersExplorerControl control)
                {
                    control.ClearFilter();
                }
            }

            public override void ProvideSearchSettings(IVsUIDataSource pSearchSettings)
            {
                Utilities.SetValue(pSearchSettings,
                    SearchSettingsDataSource.SearchStartTypeProperty.Name,
                    (uint)VSSEARCHSTARTTYPE.SST_INSTANT);
            }
        }

        internal class KnownMonikersSearchTask : VsSearchTask
        {
            private readonly Pane _toolWindow;
            private readonly CancellationToken _debounceCt;
            private readonly int _debounceDelayMs;

            public KnownMonikersSearchTask(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback, Pane toolWindow, CancellationToken debounceCt, int debounceDelayMs)
                : base(dwCookie, pSearchQuery, pSearchCallback)
            {
                _toolWindow = toolWindow;
                _debounceCt = debounceCt;
                _debounceDelayMs = debounceDelayMs;
            }

            protected override void OnStartSearch()
            {
                // Fire-and-forget the debounced search
                _ = PerformDebouncedSearchAsync();
                base.OnStartSearch();
            }

            private async Task PerformDebouncedSearchAsync()
            {
                try
                {
                    // Wait for debounce period; if cancelled, another keystroke arrived
                    await Task.Delay(_debounceDelayMs, _debounceCt);
                }
                catch (OperationCanceledException)
                {
                    return; // Newer search superseded this one
                }

                ErrorCode = VSConstants.S_OK;
                uint resultCount = 0;

                try
                {
                    var searchString = SearchQuery.SearchString?.Trim().ToLowerInvariant() ?? string.Empty;
                    KnownMonikersExplorerControl control;
                    IReadOnlyList<KnownMonikersViewModel> allMonikers;

                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_debounceCt);
                    control = _toolWindow.Content as KnownMonikersExplorerControl;

                    if (control != null)
                    {
                        allMonikers = control.AllMonikers;
                        IReadOnlyList<KnownMonikersViewModel> resultsList = await Task.Run(
                            () => BuildFilteredResults(allMonikers, searchString, _debounceCt),
                            _debounceCt);

                        resultCount = (uint)resultsList.Count;

                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_debounceCt);
                        control.ApplyFilter(resultsList);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    ex.Log();
                    ErrorCode = VSConstants.E_FAIL;
                }
                finally
                {
                    SearchResults = resultCount;
                }
            }

            private static IReadOnlyList<KnownMonikersViewModel> BuildFilteredResults(IReadOnlyList<KnownMonikersViewModel> allMonikers, string searchString, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(searchString))
                {
                    return allMonikers;
                }

                var filtered = new List<KnownMonikersViewModel>();
                for (var i = 0; i < allMonikers.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    KnownMonikersViewModel moniker = allMonikers[i];
                    if (moniker.MatchSearchTerm(searchString))
                    {
                        filtered.Add(moniker);
                    }
                }

                return filtered;
            }

            protected override void OnStopSearch()
            {
                SearchResults = 0;
            }
        }
    }

    public class ServicesDTO
    {
        private IReadOnlyList<KnownMonikersViewModel> _monikers = Array.Empty<KnownMonikersViewModel>();
        private int _populated; // 0 = no, 1 = yes
        private readonly object _lock = new object();
        private readonly TaskCompletionSource<bool> _populationCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task PopulationCompleted => _populationCompleted.Task;

        public IReadOnlyList<KnownMonikersViewModel> Monikers
        {
            get
            {
                return _monikers;
            }
            private set => _monikers = value;
        }

        public Task PopulateAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref _populated, 1, 0) != 0)
            {
                return PopulationCompleted;
            }

            try
            {
                PropertyInfo[] properties = typeof(KnownMonikers).GetProperties(BindingFlags.Static | BindingFlags.Public);
                var list = new List<KnownMonikersViewModel>(properties.Length);

                for (var i = 0; i < properties.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    PropertyInfo property = properties[i];
                    list.Add(new KnownMonikersViewModel(property.Name, (ImageMoniker)property.GetValue(null, null)));
                }

                lock (_lock)
                {
                    Monikers = list;
                }

                _populationCompleted.TrySetResult(true);
            }
            catch (OperationCanceledException)
            {
                _populationCompleted.TrySetCanceled();
                throw;
            }
            catch (Exception ex)
            {
                _populationCompleted.TrySetException(ex);
                throw;
            }

            return PopulationCompleted;
        }
    }
}
