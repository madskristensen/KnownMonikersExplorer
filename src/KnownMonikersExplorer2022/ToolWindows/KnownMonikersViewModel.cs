using System;
using Microsoft.VisualStudio.Imaging.Interop;

namespace KnownMonikersExplorer.ToolWindows
{
    public class KnownMonikersViewModel
    {
        public KnownMonikersViewModel(string name, ImageMoniker moniker)
        {
            Name = name;
            Moniker = moniker;

            if (MonikerKeywords.Keywords.TryGetValue(name, out var filters))
            {
                Filters = filters;
            }
        }

        public string Name { get; set; }
        public ImageMoniker Moniker { get; set; }
        public string Filters { get; set; } = "";

        public bool MatchSearchTerm(string searchTerm)
        {
            return Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0
                   || Filters.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0
                   || (int.TryParse(searchTerm, out int id) && id == Moniker.Id)
                   || (Guid.TryParse(searchTerm, out Guid guid) && guid == Moniker.Guid);
        }
    }
}
