    using Microsoft.VisualStudio.Imaging.Interop;

namespace KnownMonikersExplorer.ToolWindows
{
    public class KnownMonikersViewModel
    {
        public KnownMonikersViewModel(string name, ImageMoniker moniker)
        {
            Name = name;
            Moniker = moniker;
        }

        public string Name { get; set; }
        public ImageMoniker Moniker { get; set; }
    }
}
