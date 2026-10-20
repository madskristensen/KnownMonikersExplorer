using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KnownMonikersExplorer.ToolWindows;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;
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

            IReadOnlyList<KnownMonikersViewModel> results = KnownMonikersExplorerWindow.KnownMonikersSearchTask.BuildFilteredResults(
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

            Assert.AreSame(monikers, KnownMonikersExplorerWindow.KnownMonikersSearchTask.BuildFilteredResults(
                monikers, string.Empty, CancellationToken.None));
        }

        [TestMethod]
        public void CanceledSearchRejectsEvenEmptyQuery()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                Assert.Throws<OperationCanceledException>(() => KnownMonikersExplorerWindow.KnownMonikersSearchTask.BuildFilteredResults(
                    Array.Empty<KnownMonikersViewModel>(), string.Empty, cancellation.Token));
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task StoppingDuringDebounceReportsCompletionWithZeroResults()
        {
            var callback = new SearchCallback();
            var search = new TestSearchTask(callback);
            search.Begin();
            search.StopSearch();

            Assert.AreEqual(0u, await callback.Completion.Task);
            Assert.AreEqual(1, callback.CompletionCount);
            search.Cancel();
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task SupersededSearchCanCompleteAfterCancellationBeforeStart()
        {
            var callback = new SearchCallback();
            var search = new TestSearchTask(callback);
            search.Cancel();
            search.Begin();

            Assert.AreEqual(0u, await callback.Completion.Task);
            Assert.AreEqual(1, callback.CompletionCount);
            search.Cancel();
        }

        private sealed class TestSearchTask : KnownMonikersExplorerWindow.KnownMonikersSearchTask
        {
            public TestSearchTask(IVsSearchCallback callback)
                : base(1, new SearchQuery(), callback, null, Timeout.Infinite)
            {
            }

            public void Begin() => Start();

            public void StopSearch() => Stop();
        }

        private sealed class SearchQuery : IVsSearchQuery
        {
            public string SearchString => "add";
            public uint ParseError => 0;
            public uint GetTokens(uint count, IVsSearchToken[] tokens) => 0;
        }

        private sealed class SearchCallback : IVsSearchCallback
        {
            public readonly TaskCompletionSource<uint> Completion = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
            public int CompletionCount;

            public void ReportProgress(IVsSearchTask task, uint progress, uint maximum)
            {
            }

            public void ReportComplete(IVsSearchTask task, uint results)
            {
                Interlocked.Increment(ref CompletionCount);
                Completion.TrySetResult(results);
            }
        }
    }
}