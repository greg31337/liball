using uspto;

namespace uspto.Tests;

public class PaginationEngineTests
{
    [Test]
    public void AggregatesOffsetPagesUntilTheReportedTotal()
    {
        ScriptedOffsetPageSource source = new ScriptedOffsetPageSource(new OffsetPageResult[]
        {
            CreatePage(0L, 2, new string[] { "a", "b" }, true, 5L),
            CreatePage(2L, 2, new string[] { "c", "d" }, true, 5L),
            CreatePage(4L, 2, new string[] { "e" }, true, 5L)
        });
        SearchRequest request = CreateRequest(0L, 2, 10, 10);

        OffsetPaginationResult result = PaginationEngine.AggregateOffsetPages(source, request, new UsptoLimits(), "id");

        Assert.Multiple(() =>
        {
            Assert.That(result.Records, Is.EqualTo(new object[] { "a", "b", "c", "d", "e" }));
            Assert.That(result.PagesRead, Is.EqualTo(3));
            Assert.That(result.HasTotal, Is.True);
            Assert.That(result.Total, Is.EqualTo(5L));
            Assert.That(result.NextOffset, Is.EqualTo(5L));
            Assert.That(result.Completed, Is.True);
            Assert.That(result.EndedOnEmptyPage, Is.False);
            Assert.That(result.ReachedPageLimit, Is.False);
            Assert.That(result.ReachedResultLimit, Is.False);
            Assert.That(source.RequestedOffsets, Is.EqualTo(new long[] { 0L, 2L, 4L }));
            Assert.That(source.RequestedLimits, Is.EqualTo(new int[] { 2, 2, 2 }));
        });
    }

    [Test]
    public void EmptyPageTerminatesAggregationWithoutATotal()
    {
        ScriptedOffsetPageSource source = new ScriptedOffsetPageSource(new OffsetPageResult[]
        {
            CreatePage(10L, 3, new string[] { "a", "b" }, false, 0L),
            CreatePage(12L, 3, new string[0], false, 0L)
        });
        SearchRequest request = CreateRequest(10L, 3, 10, 10);

        OffsetPaginationResult result = PaginationEngine.AggregateOffsetPages(source, request, new UsptoLimits(), "id");

        Assert.Multiple(() =>
        {
            Assert.That(result.Records, Is.EqualTo(new object[] { "a", "b" }));
            Assert.That(result.PagesRead, Is.EqualTo(2));
            Assert.That(result.NextOffset, Is.EqualTo(12L));
            Assert.That(result.Completed, Is.True);
            Assert.That(result.EndedOnEmptyPage, Is.True);
            Assert.That(result.HasTotal, Is.False);
        });
    }

    [Test]
    public void CallerResultLimitTruncatesWithoutSkippingAResumeOffset()
    {
        ScriptedOffsetPageSource source = new ScriptedOffsetPageSource(new OffsetPageResult[]
        {
            CreatePage(0L, 2, new string[] { "a", "b" }, true, 10L),
            CreatePage(2L, 2, new string[] { "c", "d" }, true, 10L)
        });
        SearchRequest request = CreateRequest(0L, 2, 10, 3);

        OffsetPaginationResult result = PaginationEngine.AggregateOffsetPages(source, request, new UsptoLimits(), "id");

        Assert.Multiple(() =>
        {
            Assert.That(result.Records, Is.EqualTo(new object[] { "a", "b", "c" }));
            Assert.That(result.NextOffset, Is.EqualTo(3L));
            Assert.That(result.Completed, Is.False);
            Assert.That(result.ReachedResultLimit, Is.True);
            Assert.That(result.ReachedPageLimit, Is.False);
        });
    }

    [Test]
    public void GlobalPageLimitCapsTheCallerLimit()
    {
        ScriptedOffsetPageSource source = new ScriptedOffsetPageSource(new OffsetPageResult[]
        {
            CreatePage(0L, 2, new string[] { "a", "b" }, true, 10L)
        });
        SearchRequest request = CreateRequest(0L, 2, 5, 10);
        UsptoLimits limits = new UsptoLimits();
        limits.MaximumPages = 1;

        OffsetPaginationResult result = PaginationEngine.AggregateOffsetPages(source, request, limits, "id");

        Assert.Multiple(() =>
        {
            Assert.That(result.PagesRead, Is.EqualTo(1));
            Assert.That(result.NextOffset, Is.EqualTo(2L));
            Assert.That(result.Completed, Is.False);
            Assert.That(result.ReachedPageLimit, Is.True);
        });
    }

    [Test]
    public void DuplicatePageIdentifiersStopUnstablePagination()
    {
        ScriptedOffsetPageSource source = new ScriptedOffsetPageSource(new OffsetPageResult[]
        {
            CreatePage(0L, 2, new string[] { "a", "b" }, false, 0L),
            CreatePage(2L, 2, new string[] { "a", "b" }, false, 0L)
        });
        SearchRequest request = CreateRequest(0L, 2, 10, 10);

        ValidationException exception = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(source, request, new UsptoLimits(), "id"));

        Assert.That(exception.ErrorCode, Is.EqualTo("DUPLICATE_PAGE"));
    }

    [Test]
    public void OverlappingRecordsAcrossDifferentOffsetPagesAreRejected()
    {
        ScriptedOffsetPageSource source = new ScriptedOffsetPageSource(new OffsetPageResult[]
        {
            CreatePage(0L, 2, new string[] { "a", "b" }, false, 0L),
            CreatePage(2L, 2, new string[] { "b", "c" }, false, 0L)
        });

        ValidationException exception = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(source, CreateRequest(0L, 2, 10, 10), new UsptoLimits(), "id"));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("DUPLICATE_RECORD"));
            Assert.That(source.RequestedOffsets, Is.EqualTo(new long[] { 0L, 2L }));
        });
    }

    [Test]
    public void EmptyOffsetPageBeforeTheExactTotalIsRejected()
    {
        ScriptedOffsetPageSource source = new ScriptedOffsetPageSource(new OffsetPageResult[]
        {
            CreatePage(0L, 2, new string[] { "a", "b" }, true, 3L),
            CreatePage(2L, 2, new string[0], true, 3L)
        });

        ValidationException exception = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(source, CreateRequest(0L, 2, 10, 10), new UsptoLimits(), "id"));

        Assert.That(exception.ErrorCode, Is.EqualTo("TOTAL_COUNT_MISMATCH"));
    }

    [Test]
    public void StableTieBreakerMustBeTheFinalUniqueSort()
    {
        ScriptedOffsetPageSource source = new ScriptedOffsetPageSource(new OffsetPageResult[0]);
        SearchRequest missingSort = CreateRequest(0L, 10, 1, 10);
        missingSort.Sorts = new SearchSort[0];
        SearchRequest wrongFinalSort = CreateRequest(0L, 10, 1, 10);
        wrongFinalSort.Sorts = new SearchSort[]
        {
            new SearchSort { Field = "id", Direction = SortDirection.Ascending },
            new SearchSort { Field = "date", Direction = SortDirection.Descending }
        };
        SearchRequest duplicateSort = CreateRequest(0L, 10, 1, 10);
        duplicateSort.Sorts = new SearchSort[]
        {
            new SearchSort { Field = "ID", Direction = SortDirection.Ascending },
            new SearchSort { Field = "id", Direction = SortDirection.Descending }
        };

        ValidationException missing = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(source, missingSort, new UsptoLimits(), "id"));
        ValidationException wrongFinal = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(source, wrongFinalSort, new UsptoLimits(), "id"));
        ValidationException duplicate = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(source, duplicateSort, new UsptoLimits(), "id"));

        Assert.Multiple(() =>
        {
            Assert.That(missing.ErrorCode, Is.EqualTo("DETERMINISTIC_SORT_REQUIRED"));
            Assert.That(wrongFinal.ErrorCode, Is.EqualTo("DETERMINISTIC_SORT_REQUIRED"));
            Assert.That(duplicate.ErrorCode, Is.EqualTo("DUPLICATE_SORT"));
        });
    }

    [Test]
    public void OffsetAndCallerLimitsMustBePositiveAndInRange()
    {
        ScriptedOffsetPageSource source = new ScriptedOffsetPageSource(new OffsetPageResult[0]);
        SearchRequest negativeOffset = CreateRequest(-1L, 10, 1, 10);
        SearchRequest zeroPageSize = CreateRequest(0L, 0, 1, 10);
        SearchRequest zeroPages = CreateRequest(0L, 10, 0, 10);
        SearchRequest zeroResults = CreateRequest(0L, 10, 1, 0);

        ValidationException offset = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(source, negativeOffset, new UsptoLimits(), "id"));
        ValidationException pageSize = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(source, zeroPageSize, new UsptoLimits(), "id"));
        ValidationException pages = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(source, zeroPages, new UsptoLimits(), "id"));
        ValidationException results = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(source, zeroResults, new UsptoLimits(), "id"));

        Assert.Multiple(() =>
        {
            Assert.That(offset.ErrorCode, Is.EqualTo("OFFSET_OUT_OF_RANGE"));
            Assert.That(pageSize.ErrorCode, Is.EqualTo("PAGE_SIZE_OUT_OF_RANGE"));
            Assert.That(pages.ErrorCode, Is.EqualTo("PAGE_LIMIT_OUT_OF_RANGE"));
            Assert.That(results.ErrorCode, Is.EqualTo("RESULT_LIMIT_OUT_OF_RANGE"));
        });
    }

    [Test]
    public void ReturnedCountAndOffsetCoordinatesMustMatchThePage()
    {
        OffsetPageResult wrongCountPage = CreatePage(0L, 2, new string[] { "a" }, false, 0L);
        wrongCountPage.Metadata.ReturnedCount = 2;
        OffsetPageResult wrongOffsetPage = CreatePage(1L, 2, new string[] { "a" }, false, 0L);
        ScriptedOffsetPageSource wrongCount = new ScriptedOffsetPageSource(new OffsetPageResult[] { wrongCountPage });
        ScriptedOffsetPageSource wrongOffset = new ScriptedOffsetPageSource(new OffsetPageResult[] { wrongOffsetPage });
        SearchRequest request = CreateRequest(0L, 2, 10, 10);

        ValidationException count = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(wrongCount, request, new UsptoLimits(), "id"));
        ValidationException offset = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(wrongOffset, request, new UsptoLimits(), "id"));

        Assert.Multiple(() =>
        {
            Assert.That(count.ErrorCode, Is.EqualTo("PAGE_COUNT_MISMATCH"));
            Assert.That(offset.ErrorCode, Is.EqualTo("PAGE_OFFSET_MISMATCH"));
        });
    }

    [Test]
    public void TotalsMustRemainConsistentAndCoverReturnedRecords()
    {
        ScriptedOffsetPageSource changingTotal = new ScriptedOffsetPageSource(new OffsetPageResult[]
        {
            CreatePage(0L, 1, new string[] { "a" }, true, 3L),
            CreatePage(1L, 1, new string[] { "b" }, true, 4L)
        });
        ScriptedOffsetPageSource countBeyondTotal = new ScriptedOffsetPageSource(new OffsetPageResult[]
        {
            CreatePage(0L, 2, new string[] { "a", "b" }, true, 1L)
        });
        SearchRequest singlePageRequest = CreateRequest(0L, 2, 10, 10);

        ValidationException changed = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(changingTotal, CreateRequest(0L, 1, 10, 10), new UsptoLimits(), "id"));
        ValidationException beyond = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(countBeyondTotal, singlePageRequest, new UsptoLimits(), "id"));

        Assert.Multiple(() =>
        {
            Assert.That(changed.ErrorCode, Is.EqualTo("TOTAL_MISMATCH"));
            Assert.That(beyond.ErrorCode, Is.EqualTo("TOTAL_COUNT_MISMATCH"));
        });
    }

    [Test]
    public void OffsetOverflowIsRejectedBeforeAnotherRequest()
    {
        long offset = long.MaxValue - 1L;
        ScriptedOffsetPageSource source = new ScriptedOffsetPageSource(new OffsetPageResult[]
        {
            CreatePage(offset, 2, new string[] { "a", "b" }, false, 0L)
        });
        SearchRequest request = CreateRequest(offset, 2, 10, 10);

        ValidationException exception = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateOffsetPages(source, request, new UsptoLimits(), "id"));

        Assert.That(exception.ErrorCode, Is.EqualTo("OFFSET_OVERFLOW"));
    }

    [Test]
    public void CallerCancellationStopsBeforeFetchingAPage()
    {
        ScriptedOffsetPageSource source = new ScriptedOffsetPageSource(new OffsetPageResult[0]);
        SearchRequest request = CreateRequest(0L, 2, 10, 10);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        request.Context.CancellationToken = cancellation.Token;

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() => PaginationEngine.AggregateOffsetPages(source, request, new UsptoLimits(), "id"));

        Assert.Multiple(() =>
        {
            Assert.That(exception.CancellationToken, Is.EqualTo(cancellation.Token));
            Assert.That(source.RequestedOffsets, Is.Empty);
        });
    }

    private static SearchRequest CreateRequest(long offset, int limit, int maximumPages, int maximumResults)
    {
        SearchRequest request = new SearchRequest();
        request.OffsetPage.Offset = offset;
        request.OffsetPage.Limit = limit;
        request.MaximumPages = maximumPages;
        request.MaximumResults = maximumResults;
        request.Sorts = new SearchSort[]
        {
            new SearchSort { Field = "date", Direction = SortDirection.Descending },
            new SearchSort { Field = "id", Direction = SortDirection.Ascending }
        };
        return request;
    }

    private static OffsetPageResult CreatePage(long offset, int limit, string[] identifiers, bool hasTotal, long total)
    {
        object[] records = new object[identifiers.Length];
        for (int index = 0; index < identifiers.Length; index++)
        {
            records[index] = identifiers[index];
        }

        OffsetPageResult page = new OffsetPageResult();
        page.Records = records;
        page.RecordIdentifiers = identifiers;
        page.Metadata.ReturnedCount = records.Length;
        page.Metadata.HasOffsetPage = true;
        page.Metadata.OffsetPage.Offset = offset;
        page.Metadata.OffsetPage.Limit = limit;
        page.Metadata.HasTotal = hasTotal;
        page.Metadata.Total = total;
        return page;
    }
}

internal class ScriptedOffsetPageSource : IOffsetPageSource
{
    internal Queue<OffsetPageResult> Pages = new Queue<OffsetPageResult>();
    internal List<long> RequestedOffsets = new List<long>();
    internal List<int> RequestedLimits = new List<int>();

    internal ScriptedOffsetPageSource(OffsetPageResult[] pages)
    {
        foreach (OffsetPageResult page in pages)
        {
            Pages.Enqueue(page);
        }
    }

    public OffsetPageResult FetchOffsetPage(long offset, int limit, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequestedOffsets.Add(offset);
        RequestedLimits.Add(limit);
        return Pages.Dequeue();
    }
}
