using uspto;

namespace uspto.Tests;

public class CursorPaginationEngineTests
{
    [Test]
    public void MultiFieldAfterCursorIsPropagatedInSortOrder()
    {
        ScriptedCursorPageSource source = new ScriptedCursorPageSource(new CursorPageResult[]
        {
            CreatePage(new string[] { "a", "b" }, true, new string[] { "2025-01-02", "b" }, true, 3L),
            CreatePage(new string[] { "c" }, false, new string[0], true, 3L)
        });
        SearchRequest request = CreateRequest(2, 10, 10);

        CursorPaginationResult result = PaginationEngine.AggregateCursorPages(source, request, new UsptoLimits(), "patent_id");

        Assert.Multiple(() =>
        {
            Assert.That(result.Records, Is.EqualTo(new object[] { "a", "b", "c" }));
            Assert.That(result.PagesRead, Is.EqualTo(2));
            Assert.That(result.HasTotal, Is.True);
            Assert.That(result.Total, Is.EqualTo(3L));
            Assert.That(result.Completed, Is.True);
            Assert.That(result.HasNextCursorPage, Is.False);
            Assert.That(source.RequestedAfterValues[0], Is.Empty);
            Assert.That(source.RequestedAfterValues[1], Is.EqualTo(new string[] { "2025-01-02", "b" }));
            Assert.That(source.RequestedSizes, Is.EqualTo(new int[] { 2, 2 }));
        });
    }

    [Test]
    public void CallerStartingCursorIsCopiedAndForwarded()
    {
        ScriptedCursorPageSource source = new ScriptedCursorPageSource(new CursorPageResult[]
        {
            CreatePage(new string[] { "c" }, false, new string[0], false, 0L)
        });
        SearchRequest request = CreateRequest(100, 10, 10);
        request.CursorPage.AfterValues = new string[] { "2025-01-02", "b" };

        CursorPaginationResult result = PaginationEngine.AggregateCursorPages(source, request, new UsptoLimits(), "patent_id");
        request.CursorPage.AfterValues[0] = "caller-change";

        Assert.Multiple(() =>
        {
            Assert.That(source.RequestedAfterValues[0], Is.EqualTo(new string[] { "2025-01-02", "b" }));
            Assert.That(result.Records, Is.EqualTo(new object[] { "c" }));
            Assert.That(result.Completed, Is.True);
        });
    }

    [Test]
    public void PageSizeAllowsOneThousandAndRejectsLargerValues()
    {
        ScriptedCursorPageSource validSource = new ScriptedCursorPageSource(new CursorPageResult[]
        {
            CreatePage(new string[0], false, new string[0], false, 0L)
        });
        SearchRequest valid = CreateRequest(UsptoDefaults.PatentsViewMaximumPageSize, 1, UsptoDefaults.PatentsViewMaximumPageSize);
        SearchRequest invalid = CreateRequest(UsptoDefaults.PatentsViewMaximumPageSize + 1, 1, 10);

        CursorPaginationResult result = PaginationEngine.AggregateCursorPages(validSource, valid, new UsptoLimits(), "patent_id");
        ValidationException exception = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateCursorPages(new ScriptedCursorPageSource(new CursorPageResult[0]), invalid, new UsptoLimits(), "patent_id"));

        Assert.Multiple(() =>
        {
            Assert.That(validSource.RequestedSizes, Is.EqualTo(new int[] { 1000 }));
            Assert.That(result.EndedOnEmptyPage, Is.True);
            Assert.That(exception.ErrorCode, Is.EqualTo("PAGE_SIZE_OUT_OF_RANGE"));
        });
    }

    [Test]
    public void RemainingResultBudgetReducesTheRequestedPageSize()
    {
        ScriptedCursorPageSource source = new ScriptedCursorPageSource(new CursorPageResult[]
        {
            CreatePage(new string[] { "a", "b", "c" }, true, new string[] { "2025-01-03", "c" }, false, 0L)
        });
        SearchRequest request = CreateRequest(1000, 10, 3);

        CursorPaginationResult result = PaginationEngine.AggregateCursorPages(source, request, new UsptoLimits(), "patent_id");

        Assert.Multiple(() =>
        {
            Assert.That(source.RequestedSizes, Is.EqualTo(new int[] { 3 }));
            Assert.That(result.Records, Is.EqualTo(new object[] { "a", "b", "c" }));
            Assert.That(result.ReachedResultLimit, Is.True);
            Assert.That(result.HasNextCursorPage, Is.True);
            Assert.That(result.NextCursorPage.AfterValues, Is.EqualTo(new string[] { "2025-01-03", "c" }));
            Assert.That(result.NextCursorPage.Size, Is.EqualTo(1000));
        });
    }

    [Test]
    public void RepeatedCursorIsRejectedBeforeAnotherPageRequest()
    {
        ScriptedCursorPageSource source = new ScriptedCursorPageSource(new CursorPageResult[]
        {
            CreatePage(new string[] { "b" }, true, new string[] { "2025-01-01", "a" }, false, 0L)
        });
        SearchRequest request = CreateRequest(10, 10, 10);
        request.CursorPage.AfterValues = new string[] { "2025-01-01", "a" };

        ValidationException exception = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateCursorPages(source, request, new UsptoLimits(), "patent_id"));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("DUPLICATE_CURSOR"));
            Assert.That(source.RequestedAfterValues, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RepeatedPagesAndOverlappingRecordsAreRejected()
    {
        ScriptedCursorPageSource duplicatePage = new ScriptedCursorPageSource(new CursorPageResult[]
        {
            CreatePage(new string[] { "a", "b" }, true, new string[] { "2025-01-01", "b" }, false, 0L),
            CreatePage(new string[] { "a", "b" }, true, new string[] { "2025-01-02", "b" }, false, 0L)
        });
        ScriptedCursorPageSource overlappingRecord = new ScriptedCursorPageSource(new CursorPageResult[]
        {
            CreatePage(new string[] { "a", "b" }, true, new string[] { "2025-01-01", "b" }, false, 0L),
            CreatePage(new string[] { "b", "c" }, true, new string[] { "2025-01-02", "c" }, false, 0L)
        });

        ValidationException page = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateCursorPages(duplicatePage, CreateRequest(2, 10, 10), new UsptoLimits(), "patent_id"));
        ValidationException record = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateCursorPages(overlappingRecord, CreateRequest(2, 10, 10), new UsptoLimits(), "patent_id"));

        Assert.Multiple(() =>
        {
            Assert.That(page.ErrorCode, Is.EqualTo("DUPLICATE_PAGE"));
            Assert.That(record.ErrorCode, Is.EqualTo("DUPLICATE_RECORD"));
        });
    }

    [Test]
    public void CursorValuesMustMatchAllConfiguredSortFields()
    {
        ScriptedCursorPageSource source = new ScriptedCursorPageSource(new CursorPageResult[0]);
        SearchRequest request = CreateRequest(10, 10, 10);
        request.CursorPage.AfterValues = new string[] { "only-one" };
        ScriptedCursorPageSource invalidNextSource = new ScriptedCursorPageSource(new CursorPageResult[]
        {
            CreatePage(new string[] { "a" }, true, new string[] { "only-one" }, false, 0L)
        });

        ValidationException initial = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateCursorPages(source, request, new UsptoLimits(), "patent_id"));
        ValidationException next = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateCursorPages(invalidNextSource, CreateRequest(10, 10, 10), new UsptoLimits(), "patent_id"));

        Assert.Multiple(() =>
        {
            Assert.That(initial.ErrorCode, Is.EqualTo("CURSOR_SORT_MISMATCH"));
            Assert.That(next.ErrorCode, Is.EqualTo("CURSOR_SORT_MISMATCH"));
        });
    }

    [Test]
    public void StableSortIsRequiredAsTheFinalSortField()
    {
        ScriptedCursorPageSource source = new ScriptedCursorPageSource(new CursorPageResult[0]);
        SearchRequest request = CreateRequest(10, 10, 10);
        request.Sorts = new SearchSort[]
        {
            new SearchSort { Field = "patent_id", Direction = SortDirection.Ascending },
            new SearchSort { Field = "patent_date", Direction = SortDirection.Descending }
        };

        ValidationException exception = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateCursorPages(source, request, new UsptoLimits(), "patent_id"));

        Assert.That(exception.ErrorCode, Is.EqualTo("DETERMINISTIC_SORT_REQUIRED"));
    }

    [Test]
    public void PageLimitPreservesTheNextCursorForContinuation()
    {
        ScriptedCursorPageSource source = new ScriptedCursorPageSource(new CursorPageResult[]
        {
            CreatePage(new string[] { "a" }, true, new string[] { "2025-01-01", "a" }, true, 10L)
        });
        SearchRequest request = CreateRequest(10, 5, 10);
        UsptoLimits limits = new UsptoLimits();
        limits.MaximumPages = 1;

        CursorPaginationResult result = PaginationEngine.AggregateCursorPages(source, request, limits, "patent_id");

        Assert.Multiple(() =>
        {
            Assert.That(result.ReachedPageLimit, Is.True);
            Assert.That(result.Completed, Is.False);
            Assert.That(result.HasNextCursorPage, Is.True);
            Assert.That(result.NextCursorPage.AfterValues, Is.EqualTo(new string[] { "2025-01-01", "a" }));
        });
    }

    [Test]
    public void ChangingTotalsAndPageCountsAreRejected()
    {
        ScriptedCursorPageSource changingTotal = new ScriptedCursorPageSource(new CursorPageResult[]
        {
            CreatePage(new string[] { "a" }, true, new string[] { "2025-01-01", "a" }, true, 3L),
            CreatePage(new string[] { "b" }, false, new string[0], true, 4L)
        });
        CursorPageResult badCountPage = CreatePage(new string[] { "a" }, false, new string[0], false, 0L);
        badCountPage.Metadata.ReturnedCount = 2;
        ScriptedCursorPageSource badCount = new ScriptedCursorPageSource(new CursorPageResult[] { badCountPage });

        ValidationException total = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateCursorPages(changingTotal, CreateRequest(1, 10, 10), new UsptoLimits(), "patent_id"));
        ValidationException count = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateCursorPages(badCount, CreateRequest(1, 10, 10), new UsptoLimits(), "patent_id"));

        Assert.Multiple(() =>
        {
            Assert.That(total.ErrorCode, Is.EqualTo("TOTAL_MISMATCH"));
            Assert.That(count.ErrorCode, Is.EqualTo("PAGE_COUNT_MISMATCH"));
        });
    }

    [Test]
    public void CursorTerminationBeforeTheExactTotalIsRejected()
    {
        ScriptedCursorPageSource missingNext = new ScriptedCursorPageSource(new CursorPageResult[]
        {
            CreatePage(new string[] { "a", "b" }, false, new string[0], true, 3L)
        });
        ScriptedCursorPageSource earlyEmpty = new ScriptedCursorPageSource(new CursorPageResult[]
        {
            CreatePage(new string[] { "a", "b" }, true, new string[] { "2025-01-02", "b" }, true, 3L),
            CreatePage(new string[0], false, new string[0], true, 3L)
        });

        ValidationException missingNextFailure = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateCursorPages(missingNext, CreateRequest(2, 10, 10), new UsptoLimits(), "patent_id"));
        ValidationException earlyEmptyFailure = Assert.Throws<ValidationException>(() => PaginationEngine.AggregateCursorPages(earlyEmpty, CreateRequest(2, 10, 10), new UsptoLimits(), "patent_id"));

        Assert.Multiple(() =>
        {
            Assert.That(missingNextFailure.ErrorCode, Is.EqualTo("TOTAL_COUNT_MISMATCH"));
            Assert.That(earlyEmptyFailure.ErrorCode, Is.EqualTo("TOTAL_COUNT_MISMATCH"));
        });
    }

    [Test]
    public void CallerCancellationStopsBeforeFetchingACursorPage()
    {
        ScriptedCursorPageSource source = new ScriptedCursorPageSource(new CursorPageResult[0]);
        SearchRequest request = CreateRequest(10, 10, 10);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        request.Context.CancellationToken = cancellation.Token;

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() => PaginationEngine.AggregateCursorPages(source, request, new UsptoLimits(), "patent_id"));

        Assert.Multiple(() =>
        {
            Assert.That(exception.CancellationToken, Is.EqualTo(cancellation.Token));
            Assert.That(source.RequestedAfterValues, Is.Empty);
        });
    }

    private static SearchRequest CreateRequest(int size, int maximumPages, int maximumResults)
    {
        SearchRequest request = new SearchRequest();
        request.UseCursorPage = true;
        request.CursorPage.Size = size;
        request.MaximumPages = maximumPages;
        request.MaximumResults = maximumResults;
        request.Sorts = new SearchSort[]
        {
            new SearchSort { Field = "patent_date", Direction = SortDirection.Descending },
            new SearchSort { Field = "patent_id", Direction = SortDirection.Ascending }
        };
        return request;
    }

    private static CursorPageResult CreatePage(string[] identifiers, bool hasNext, string[] nextValues, bool hasTotal, long total)
    {
        object[] records = new object[identifiers.Length];
        for (int index = 0; index < identifiers.Length; index++)
        {
            records[index] = identifiers[index];
        }

        CursorPageResult page = new CursorPageResult();
        page.Records = records;
        page.RecordIdentifiers = identifiers;
        page.Metadata.ReturnedCount = records.Length;
        page.Metadata.HasNextCursorPage = hasNext;
        page.Metadata.NextCursorPage.AfterValues = nextValues;
        page.Metadata.HasTotal = hasTotal;
        page.Metadata.Total = total;
        return page;
    }
}

internal class ScriptedCursorPageSource : ICursorPageSource
{
    internal Queue<CursorPageResult> Pages = new Queue<CursorPageResult>();
    internal List<string[]> RequestedAfterValues = new List<string[]>();
    internal List<int> RequestedSizes = new List<int>();

    internal ScriptedCursorPageSource(CursorPageResult[] pages)
    {
        foreach (CursorPageResult page in pages)
        {
            Pages.Enqueue(page);
        }
    }

    public CursorPageResult FetchCursorPage(string[] afterValues, int size, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequestedAfterValues.Add(afterValues);
        RequestedSizes.Add(size);
        return Pages.Dequeue();
    }
}
