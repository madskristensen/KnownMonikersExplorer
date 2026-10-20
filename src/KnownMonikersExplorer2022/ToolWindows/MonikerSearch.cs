using System.Collections.Generic;
using System.Threading;

namespace KnownMonikersExplorer.ToolWindows
{
    internal static class MonikerSearch
    {
        internal static IReadOnlyList<KnownMonikersViewModel> Filter(IReadOnlyList<KnownMonikersViewModel> monikers, string query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            query = query?.Trim().ToLowerInvariant() ?? string.Empty;
            if (query.Length == 0)
            {
                return monikers;
            }

            var results = new List<KnownMonikersViewModel>();
            foreach (KnownMonikersViewModel moniker in monikers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (moniker.MatchSearchTerm(query))
                {
                    results.Add(moniker);
                }
            }

            return results;
        }
    }
}