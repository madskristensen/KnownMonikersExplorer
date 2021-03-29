using EnvDTE80;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace KnownMonikersExplorer.ToolWindows
{
    [Guid(WindowGuidString)]
    public class KnownMonikersExplorerWindow : ToolWindowPane
    {
        public const string WindowGuidString = "cfff3162-9c8d-4244-b0a7-e3b39a968b24";
        public const string Title = "KnownMonikers Explorer";

        public KnownMonikersExplorerWindow()
            : this(null)
        { }

        public KnownMonikersExplorerWindow(ServicesDTO state)
            : base()
        {
            Caption = Title;
            BitmapImageMoniker = KnownMonikers.Image;

            var elm = new KnownMonikersExplorerControl(state);
            Content = elm;
        }
    }

    public class ServicesDTO
    {
        public IVsImageService2 ImageService { get; set; }
        public DTE2 DTE { get; set; }
        public IEnumerable<KnownMonikersViewModel> Monikers { get; set; }
    }
}
