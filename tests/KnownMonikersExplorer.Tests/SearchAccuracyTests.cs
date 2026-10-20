using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using KnownMonikersExplorer.ToolWindows;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KnownMonikersExplorer.Tests
{
    [TestClass]
    public sealed class SearchAccuracyTests
    {
        [TestMethod]
        public async Task EveryCatalogNameMapsToItsAuthoritativeGuidAndId()
        {
            var state = new ServicesDTO();
            await state.PopulateAsync();
            IReadOnlyList<KnownMonikersViewModel> actual = await state.GetMonikersAsync();
            PropertyInfo[] expected = typeof(KnownMonikers).GetProperties(BindingFlags.Public | BindingFlags.Static);

            CollectionAssert.AreEquivalent(expected.Select(property => property.Name).ToArray(), actual.Select(item => item.Name).ToArray());
            Assert.AreEqual(actual.Count, actual.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count());

            foreach (PropertyInfo property in expected)
            {
                var moniker = (ImageMoniker)property.GetValue(null);
                KnownMonikersViewModel item = actual.Single(candidate => candidate.Name == property.Name);
                Assert.AreEqual(moniker.Guid, item.Moniker.Guid, property.Name);
                Assert.AreEqual(moniker.Id, item.Moniker.Id, property.Name);
            }
        }

        [TestMethod]
        [DataRow(null, "AboutBox,Add,Actor")]
        [DataRow("", "AboutBox,Add,Actor")]
        [DataRow(" \t ", "AboutBox,Add,Actor")]
        [DataRow("add", "Add")]
        [DataRow("  aDd  ", "Add")]
        [DataRow("abo", "AboutBox")]
        [DataRow("insert", "Add")]
        [DataRow("HUMAN", "Actor")]
        [DataRow("222", "Add")]
        [DataRow(" 333 ", "Actor")]
        [DataRow("{11111111-1111-1111-1111-111111111111}", "AboutBox,Add")]
        [DataRow("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA", "Actor")]
        [DataRow("999", "")]
        [DataRow("00000000-0000-0000-0000-000000000001", "")]
        [DataRow("no-such-moniker", "")]
        [DataRow("human insert", "")]
        public void QueriesReturnExactlyTheExpectedOrderedMatches(string query, string expectedNames)
        {
            IReadOnlyList<KnownMonikersViewModel> result = MonikerSearch.Filter(CreateCatalog(), query, CancellationToken.None);
            string[] expected = expectedNames.Length == 0 ? Array.Empty<string>() : expectedNames.Split(',');

            CollectionAssert.AreEqual(expected, result.Select(item => item.Name).ToArray());
        }

        [TestMethod]
        public void FilteringPreservesOriginalInstancesAndDoesNotModifyCatalog()
        {
            KnownMonikersViewModel[] catalog = CreateCatalog();
            IReadOnlyList<KnownMonikersViewModel> result = MonikerSearch.Filter(catalog, "222", CancellationToken.None);

            Assert.AreEqual(1, result.Count);
            Assert.AreSame(catalog[1], result[0]);
            CollectionAssert.AreEqual(new[] { "AboutBox", "Add", "Actor" }, catalog.Select(item => item.Name).ToArray());
        }

        [TestMethod]
        public void EmptyCatalogHasNoMatches()
        {
            Assert.AreEqual(0, MonikerSearch.Filter(Array.Empty<KnownMonikersViewModel>(), "add", CancellationToken.None).Count);
        }

        [TestMethod]
        public void CancellationDuringFilteringDoesNotReturnPartialResults()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                var catalog = new CancelingCatalog(CreateCatalog(), cancellation);
                Assert.Throws<OperationCanceledException>(() => MonikerSearch.Filter(catalog, "a", cancellation.Token));
                Assert.AreEqual(2, catalog.Visited);
            }
        }

        [TestMethod]
        public async Task AlreadyCanceledReadIsRejectedEvenWhenCatalogIsReady()
        {
            var state = new ServicesDTO();
            await state.PopulateAsync();
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(async () => await state.GetMonikersAsync(cancellation.Token));
            }
        }

        private static KnownMonikersViewModel[] CreateCatalog()
        {
            var firstGuid = new Guid("11111111-1111-1111-1111-111111111111");
            return new[]
            {
                new KnownMonikersViewModel("AboutBox", new ImageMoniker { Guid = firstGuid, Id = 111 }),
                new KnownMonikersViewModel("Add", new ImageMoniker { Guid = firstGuid, Id = 222 }),
                new KnownMonikersViewModel("Actor", new ImageMoniker { Guid = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Id = 333 })
            };
        }

        private sealed class CancelingCatalog : IReadOnlyList<KnownMonikersViewModel>
        {
            private readonly IReadOnlyList<KnownMonikersViewModel> _items;
            private readonly CancellationTokenSource _cancellation;

            public CancelingCatalog(IReadOnlyList<KnownMonikersViewModel> items, CancellationTokenSource cancellation)
            {
                _items = items;
                _cancellation = cancellation;
            }

            public int Visited { get; private set; }
            public int Count => _items.Count;
            public KnownMonikersViewModel this[int index] => _items[index];

            public IEnumerator<KnownMonikersViewModel> GetEnumerator()
            {
                foreach (KnownMonikersViewModel item in _items)
                {
                    if (++Visited == 2)
                    {
                        _cancellation.Cancel();
                    }

                    yield return item;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}