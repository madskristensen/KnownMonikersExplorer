using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;

namespace KnownMonikersExplorer.ToolWindows
{
    internal static class ImageMonikerBitmapCache
    {
        // Key: (Guid, Id, size)
        private static readonly ConcurrentDictionary<(Guid guid, int id, int size), Task<BitmapSource>> _cache = new ConcurrentDictionary<(Guid, int, int), Task<BitmapSource>>();
        private const int MaxEntries = 500; // simple cap to avoid unbounded growth

        public static Task<BitmapSource> GetBitmapAsync(ImageMoniker moniker, int size)
        {
            var key = (moniker.Guid, moniker.Id, size);
            return _cache.GetOrAdd(key, k => CreateAsync(k.guid, k.id, k.size));
        }

        private static async Task<BitmapSource> CreateAsync(Guid guid, int id, int size)
        {
            // Reconstruct moniker
            var moniker = new ImageMoniker { Guid = guid, Id = id };
            BitmapSource bmp = await moniker.ToBitmapSourceAsync(size);
            try
            {
                if (bmp.CanFreeze) bmp.Freeze();
            }
            catch { }

            TrimIfNeeded();
            return bmp;
        }

        private static void TrimIfNeeded()
        {
            if (_cache.Count <= MaxEntries)
            {
                return;
            }

            // Simple trimming strategy: remove oldest completed entries beyond half of max
            foreach (var key in _cache.Keys.Take(_cache.Count - (MaxEntries / 2)))
            {
                Task<BitmapSource> removed;
                _cache.TryRemove(key, out removed);
                if (_cache.Count <= MaxEntries) break;
            }
        }
    }
}
