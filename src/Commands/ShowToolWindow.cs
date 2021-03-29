using Community.VisualStudio.Toolkit;
using KnownMonikersExplorer.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace KnownMonikersExplorer
{
    internal sealed class ShowToolWindow : BaseCommand<ShowToolWindow>
    {
        public ShowToolWindow() : base(PackageGuids.guidKnownMonikersPackageCmdSet, PackageIds.ShowToolWindowId)
        { }

        protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            return Package.ShowToolWindowAsync(
                typeof(KnownMonikersExplorerWindow),
                0,
                create: true,
                cancellationToken: Package.DisposalToken);
        }
    }
}
