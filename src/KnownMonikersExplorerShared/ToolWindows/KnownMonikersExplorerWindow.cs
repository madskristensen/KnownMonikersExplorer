using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;

namespace KnownMonikersExplorer.ToolWindows
{
    public class KnownMonikersExplorerWindow : BaseToolWindow<KnownMonikersExplorerWindow>
    {
        public override string GetTitle(int toolWindowId) => "KnownMonikers Explorer";

        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            PropertyInfo[] properties = typeof(KnownMonikers).GetProperties(BindingFlags.Static | BindingFlags.Public);

            var state = new ServicesDTO
            {
                Monikers = properties.Select(p => new KnownMonikersViewModel(p.Name, (ImageMoniker)p.GetValue(null, null)))
            };

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
        }
    }

    public class ServicesDTO
    {
        public IEnumerable<KnownMonikersViewModel> Monikers { get; set; }
    }
}
