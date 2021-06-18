using Community.VisualStudio.Toolkit;
using KnownMonikersExplorer.ToolWindows;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace KnownMonikersExplorer
{
    [Command(PackageIds.ShowToolWindowId)]
    internal sealed class ShowToolWindow : BaseCommand<ShowToolWindow>
    {
        protected override Task ExecuteAsync(OleMenuCmdEventArgs e) =>
            KnownMonikersExplorerWindow.ShowAsync();
    }
}
