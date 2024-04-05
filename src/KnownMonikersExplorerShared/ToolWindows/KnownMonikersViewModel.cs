using System.Collections.Generic;
using System.Windows.Documents;

using Microsoft.VisualStudio.Imaging.Interop;

namespace KnownMonikersExplorer.ToolWindows
{
    public class KnownMonikersViewModel
    {
        public KnownMonikersViewModel(string name, ImageMoniker moniker, List<string> filters)
        {
            Name = name;
            Moniker = moniker;
            Filters = filters;
        }

        public string Name { get; set; }
        public ImageMoniker Moniker { get; set; }

        public List<string> Filters { get; set; }
    }
}
