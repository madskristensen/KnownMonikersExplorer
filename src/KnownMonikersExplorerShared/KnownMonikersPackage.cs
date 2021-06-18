using System;
using System.Runtime.InteropServices;
using System.Threading;
using Community.VisualStudio.Toolkit;
using KnownMonikersExplorer.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace KnownMonikersExplorer
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [Guid(PackageGuids.guidKnownMonikersPackageString)]
    [ProvideToolWindow(typeof(KnownMonikersExplorerWindow.Pane), Style = VsDockStyle.Tabbed, DockedWidth = 300, Window = WindowGuids.DocumentWell, Orientation = ToolWindowOrientation.Left)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class KnownMonikersPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            KnownMonikersExplorerWindow.Initialize(this);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await ShowToolWindow.InitializeAsync(this);
        }
    }
}
