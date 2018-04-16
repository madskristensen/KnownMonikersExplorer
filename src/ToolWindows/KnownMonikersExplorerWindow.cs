using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

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

        public KnownMonikersExplorerWindow(object state)
            : base()
        {
            Caption = Title;

            var elm = new KnownMonikersExplorerControl();
            Content = elm;
        }
    }
}
