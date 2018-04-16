using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace KnownMonikersExplorer.ToolWindows
{
    [Guid(WindowGuidString)]
    public class KnownMonikersExplorerWindow : ToolWindowPane
    {
        public const string WindowGuidString = "5a3bfbbe-afe7-46da-a7fa-b6313fa03acc";
        public const string Title = "KnownMonikers Explorer";

        public KnownMonikersExplorerWindow()
            : this(null)
        { }

        public KnownMonikersExplorerWindow(ServicesDTO state)
            : base()
        {
            Caption = Title;

            var elm = new KnownMonikersExplorerControl(state);
            Content = elm;
        }
    }

    public class ServicesDTO
    {
        public IVsImageService2 ImageService { get; set; }
        public DTE DTE { get; set; }
    }
}
