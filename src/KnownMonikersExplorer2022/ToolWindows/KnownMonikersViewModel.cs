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

            // Precompute lowercase strings to avoid repeated allocations/comparisons
            _nameLower = Name.ToLowerInvariant();
            _filtersLower = Filters?.ToLowerInvariant() ?? string.Empty;
        }

        public string Name { get; set; }
        public ImageMoniker Moniker { get; set; }
        public string Filters { get; set; } = "";

        private readonly string _nameLower;
        private readonly string _filtersLower;

        public bool MatchSearchTerm(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return true;
            }

            // Expect caller to pass pre-lowered searchTerm when doing bulk filtering
            return _nameLower.IndexOf(searchTerm, StringComparison.Ordinal) >= 0
                   || _filtersLower.IndexOf(searchTerm, StringComparison.Ordinal) >= 0
                   || (int.TryParse(searchTerm, out var id) && id == Moniker.Id)
                   || (Guid.TryParse(searchTerm, out Guid guid) && guid == Moniker.Guid);
        }
    }
}
