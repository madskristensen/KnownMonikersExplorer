using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using KnownMonikersExplorer.ToolWindows;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace KnownMonikersExplorer
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [Guid(PackageGuids.guidKnownMonikersPackageString)]
    [ProvideToolWindow(typeof(KnownMonikersExplorerWindow), Style = VsDockStyle.Tabbed, DockedWidth = 300, Window = "DocumentWell", Orientation = ToolWindowOrientation.Left)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class KnownMonikersPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await ShowToolWindow.InitializeAsync(this);
        }

        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        {
            if (toolWindowType.Equals(new Guid(KnownMonikersExplorerWindow.WindowGuidString)))
            {
                return this;
            }

            return null;
        }

        protected override string GetToolWindowTitle(Type toolWindowType, int id)
        {
            if (toolWindowType == typeof(KnownMonikersExplorerWindow))
            {
                return KnownMonikersExplorerWindow.Title;
            }

            return base.GetToolWindowTitle(toolWindowType, id);
        }

        protected override async Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            PropertyInfo[] properties = typeof(KnownMonikers).GetProperties(BindingFlags.Static | BindingFlags.Public);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            return new ServicesDTO
            {
                Monikers = properties.Select(p => new KnownMonikersViewModel(p.Name, (ImageMoniker)p.GetValue(null, null))),
                DTE = await GetServiceAsync(typeof(DTE)) as DTE,
                ImageService = await GetServiceAsync(typeof(SVsImageService)) as IVsImageService2
            };
        }
    }
}
