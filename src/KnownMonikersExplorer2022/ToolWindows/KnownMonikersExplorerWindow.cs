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
using Microsoft.VisualStudio.Threading;

namespace KnownMonikersExplorer.ToolWindows
{
    public class KnownMonikersExplorerWindow : BaseToolWindow<KnownMonikersExplorerWindow>
    {
        public override string GetTitle(int toolWindowId) => "KnownMonikers Explorer";

        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            var state = new ServicesDTO();

            Package.JoinableTaskFactory.RunAsync(async () =>
            {
                await TaskScheduler.Default;
                try
                {
                    await state.PopulateAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }).FireAndForget();

            await Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            return new KnownMonikersExplorerControl(state, Package.JoinableTaskFactory);
        }

        [Guid("cfff3162-9c8d-4244-b0a7-e3b39a968b24")]
        public class Pane : ToolWindowPane
        {
            private KnownMonikersSearchTask _searchTask;

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

                ThreadHelper.ThrowIfNotOnUIThread();

                _searchTask?.Cancel();
                int debounceDelayMs = GetDebounceDelayMs(pSearchQuery.SearchString);
                _searchTask = new KnownMonikersSearchTask(dwCookie, pSearchQuery, pSearchCallback, this, debounceDelayMs);
                return _searchTask;
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
                ThreadHelper.ThrowIfNotOnUIThread();
                _searchTask?.Cancel();
                _searchTask = null;

                if (Content is KnownMonikersExplorerControl control)
                {
                    control.ClearFilter();
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _searchTask?.Cancel();
                }

                base.Dispose(disposing);
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
            private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
            private readonly CancellationToken _cancellationToken;
            private readonly object _cancellationLock = new object();
            private readonly int _debounceDelayMs;
            private bool _started;
            private bool _completed;

            public KnownMonikersSearchTask(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback, Pane toolWindow, int debounceDelayMs)
                : base(dwCookie, pSearchQuery, pSearchCallback)
            {
                _toolWindow = toolWindow;
                _cancellationToken = _cancellation.Token;
                _debounceDelayMs = debounceDelayMs;
            }

            protected override void OnStartSearch()
            {
                lock (_cancellationLock)
                {
                    _started = true;
                }

                PerformDebouncedSearchAsync().FireAndForget();
            }

            private async Task PerformDebouncedSearchAsync()
            {
                ErrorCode = VSConstants.S_OK;
                uint resultCount = 0;

                try
                {
                    await Task.Delay(_debounceDelayMs, _cancellationToken);
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_cancellationToken);

                    var searchString = SearchQuery.SearchString?.Trim().ToLowerInvariant() ?? string.Empty;
                    KnownMonikersExplorerControl control;
                    IReadOnlyList<KnownMonikersViewModel> allMonikers;

                    control = _toolWindow.Content as KnownMonikersExplorerControl;

                    if (control != null)
                    {
                        await control.InitializationCompleted.WithCancellation(_cancellationToken);
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_cancellationToken);
                        allMonikers = control.AllMonikers;
                        IReadOnlyList<KnownMonikersViewModel> resultsList = await Task.Run(
                            () => BuildFilteredResults(allMonikers, searchString, _cancellationToken),
                            _cancellationToken);

                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_cancellationToken);
                        lock (_cancellationLock)
                        {
                            _cancellationToken.ThrowIfCancellationRequested();
                            control.ApplyFilter(resultsList);
                            resultCount = (uint)resultsList.Count;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    resultCount = 0;
                }
                catch (Exception ex)
                {
                    ex.Log();
                    ErrorCode = VSConstants.E_FAIL;
                }
                finally
                {
                    lock (_cancellationLock)
                    {
                        SearchResults = _cancellationToken.IsCancellationRequested ? 0 : resultCount;
                        _completed = true;
                        _cancellation.Dispose();
                    }

                    base.OnStartSearch();
                }
            }

            internal static IReadOnlyList<KnownMonikersViewModel> BuildFilteredResults(IReadOnlyList<KnownMonikersViewModel> allMonikers, string searchString, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
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
                Cancel();
                SearchResults = 0;
            }

            internal void Cancel()
            {
                lock (_cancellationLock)
                {
                    if (!_completed)
                    {
                        _cancellation.Cancel();
                        if (!_started)
                        {
                            _completed = true;
                            _cancellation.Dispose();
                        }
                    }
                }
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

        public async Task<IReadOnlyList<KnownMonikersViewModel>> GetMonikersAsync(CancellationToken cancellationToken = default)
        {
            await PopulationCompleted.WithCancellation(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Monikers;
        }

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
                cancellationToken.ThrowIfCancellationRequested();
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
