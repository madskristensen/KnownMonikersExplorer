using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace KnownMonikersExplorer.ToolWindows
{
    internal static class ImageMonikerBitmapCache
    {
        private struct CacheEntry
        {
            public Task<BitmapSource> Task;
            public long InsertionOrder;
        }

        // Key: (Guid, Id, size)
        private static readonly ConcurrentDictionary<(Guid guid, int id, int size), CacheEntry> _cache = new ConcurrentDictionary<(Guid, int, int), CacheEntry>();
        private const int _maxEntries = 400; // simple cap to avoid unbounded growth
        private static long _insertionCounter;
        private static int _trimming; // 0 = not trimming, 1 = trimming

        public static Task<BitmapSource> GetBitmapAsync(ImageMoniker moniker, int size)
        {
            (Guid Guid, int Id, int size) key = (moniker.Guid, moniker.Id, size);
            CacheEntry entry = _cache.GetOrAdd(key, k => new CacheEntry
            {
                Task = CreateAsync(k.guid, k.id, k.size),
                InsertionOrder = Interlocked.Increment(ref _insertionCounter)
            });
            return entry.Task;
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
            if (_cache.Count <= _maxEntries)
            {
                return;
            }

            // Ensure only one thread trims at a time
            if (Interlocked.CompareExchange(ref _trimming, 1, 0) != 0)
            {
                return;
            }

            try
            {
                // Find and remove entries with the lowest insertion order
                var toRemove = _cache.Count - (_maxEntries / 2);
                if (toRemove <= 0) return;

                // Find threshold: entries older than this will be removed
                var threshold = _insertionCounter - _maxEntries;

                foreach (KeyValuePair<(Guid guid, int id, int size), CacheEntry> kvp in _cache)
                {
                    if (kvp.Value.InsertionOrder < threshold)
                    {
                        _cache.TryRemove(kvp.Key, out _);
                        if (_cache.Count <= _maxEntries) break;
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _trimming, 0);
            }
        }
    }
}

