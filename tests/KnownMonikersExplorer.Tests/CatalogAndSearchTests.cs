using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KnownMonikersExplorer.ToolWindows;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KnownMonikersExplorer.Tests
{
    [TestClass]
    public sealed class CatalogAndSearchTests
    {
        [TestMethod]
        public async Task CatalogWaitsForPopulationAndReturnsKnownMonikers()
        {
            var state = new ServicesDTO();
            Task<IReadOnlyList<KnownMonikersViewModel>> pending = state.GetMonikersAsync();

            Assert.IsFalse(pending.IsCompleted);
            await state.PopulateAsync();
            IReadOnlyList<KnownMonikersViewModel> monikers = await pending;

            KnownMonikersViewModel add = monikers.Single(item => item.Name == "Add");
            Assert.AreEqual(KnownMonikers.Add, add.Moniker);
            Assert.AreSame(monikers, await state.GetMonikersAsync());
            await state.PopulateAsync();
            Assert.AreSame(monikers, state.Monikers);
        }

        [TestMethod]
        public async Task CancelingWaiterDoesNotCancelCatalogPopulation()
        {
            var state = new ServicesDTO();
            using (var cancellation = new CancellationTokenSource())
            {
                Task<IReadOnlyList<KnownMonikersViewModel>> pending = state.GetMonikersAsync(cancellation.Token);
                cancellation.Cancel();

#pragma warning disable VSTHRD003 // This test observes an existing request to verify cancellation; no UI thread is involved.
                await Assert.ThrowsAsync<OperationCanceledException>(async () => await pending);
#pragma warning restore VSTHRD003
                Assert.IsFalse(state.PopulationCompleted.IsCompleted);
            }

            await state.PopulateAsync();
            Assert.IsTrue((await state.GetMonikersAsync()).Any(item => item.Name == "Add"));
        }

        [TestMethod]
        public async Task PreCanceledPopulationCompletesReadinessAsCanceled()
        {
            var state = new ServicesDTO();
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(async () => await state.PopulateAsync(cancellation.Token));
            }

            Assert.IsTrue(state.PopulationCompleted.IsCanceled);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await state.GetMonikersAsync());
        }

        [TestMethod]
        public async Task SearchAfterPopulationUsesQueryRatherThanReturningEntireCatalog()
        {
            var state = new ServicesDTO();
            Task<IReadOnlyList<KnownMonikersViewModel>> pending = state.GetMonikersAsync();
            await state.PopulateAsync();

            IReadOnlyList<KnownMonikersViewModel> results = MonikerSearch.Filter(
                await pending, "add", CancellationToken.None);

            Assert.IsTrue(results.Any(item => item.Name == "Add"));
            Assert.IsFalse(results.Any(item => item.Name == "AboutBox"));
            Assert.IsTrue(results.All(item => item.MatchSearchTerm("add")));
        }

        [TestMethod]
        public void EmptyQueryPreservesCatalogAndOrdering()
        {
            var monikers = new[]
            {
                new KnownMonikersViewModel("Add", KnownMonikers.Add),
                new KnownMonikersViewModel("AboutBox", KnownMonikers.AboutBox)
            };

            Assert.AreSame(monikers, MonikerSearch.Filter(
                monikers, string.Empty, CancellationToken.None));
        }

        [TestMethod]
        public void CanceledSearchRejectsEvenEmptyQuery()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                Assert.Throws<OperationCanceledException>(() => MonikerSearch.Filter(
                    Array.Empty<KnownMonikersViewModel>(), string.Empty, cancellation.Token));
            }
        }
    }
}