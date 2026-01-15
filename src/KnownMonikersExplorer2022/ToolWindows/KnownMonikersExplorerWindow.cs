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
            _ = Task.Run(() => state.PopulateAsync(), cancellationToken);

            await Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            return new KnownMonikersExplorerControl(state);
        }

        [Guid("cfff3162-9c8d-4244-b0a7-e3b39a968b24")]
        public class Pane : ToolWindowPane
        {
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

                return new KnownMonikersSearchTask(dwCookie, pSearchQuery, pSearchCallback, this);
            }

            public override void ClearSearch()
            {
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

            public KnownMonikersSearchTask(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback, Pane toolWindow)
                : base(dwCookie, pSearchQuery, pSearchCallback)
            {
                _toolWindow = toolWindow;
            }

            protected override void OnStartSearch()
            {
                ErrorCode = VSConstants.S_OK;
                uint resultCount = 0;

                try
                {
                    string searchString = SearchQuery.SearchString?.Trim().ToLowerInvariant() ?? string.Empty;

                    if (_toolWindow.Content is KnownMonikersExplorerControl control)
                    {
                        IReadOnlyList<KnownMonikersViewModel> allMonikers = control.AllMonikers;

                        IEnumerable<KnownMonikersViewModel> results = string.IsNullOrEmpty(searchString)
                            ? allMonikers
                            : allMonikers.Where(m => m.MatchSearchTerm(searchString));

                        var resultsList = results.ToList();
                        resultCount = (uint)resultsList.Count;

                        ThreadHelper.Generic.Invoke(() =>
                        {
                            control.ApplyFilter(resultsList);
                        });
                    }
                }
                catch (Exception)
                {
                    ErrorCode = VSConstants.E_FAIL;
                }
                finally
                {
                    SearchResults = resultCount;
                }

                base.OnStartSearch();
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

        public IReadOnlyList<KnownMonikersViewModel> Monikers
        {
            get
            {
                return _monikers;
            }
            private set => _monikers = value;
        }

        public async Task PopulateAsync()
        {
            if (Interlocked.CompareExchange(ref _populated, 1, 0) != 0)
            {
                return; // already populated
            }

            PropertyInfo[] properties = typeof(KnownMonikers).GetProperties(BindingFlags.Static | BindingFlags.Public);
            var list = properties.Select(p => new KnownMonikersViewModel(p.Name, (ImageMoniker)p.GetValue(null, null))).ToList();

            lock (_lock)
            {
                Monikers = list;
            }
        }
    }
}
