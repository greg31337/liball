using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentsViewLocalRowQueryTests
{
    [Test]
    public void OffsetQueryFiltersSortsProjectsAndPreservesLocalProvenance()
    {
        string path = CreateTable(
            "patent_id\tpatent_title\tscore\tpatent_date\n" +
            "p1\tBattery Alpha\t2\t2024-01-01\n" +
            "p2\tUnrelated\t100\t2024-02-01\n" +
            "p3\tBattery Gamma\t10\t2024-03-01\n" +
            "p4\tBattery Beta\t7.5\t2023-12-31\n");
        try
        {
            PatentsViewLocalRowQueryRequest request = CreateRequest(path);
            request.Search.Filters = new[] { Leaf("patent_title", SearchOperator.Contains, "battery") };
            request.Search.Ranges = new[] { new SearchRange { Field = "patent_date", LowerValue = "2024-01-01" } };
            request.Search.Sorts = new[]
            {
                new SearchSort { Field = "score", Direction = SortDirection.Descending },
                new SearchSort { Field = "patent_id", Direction = SortDirection.Ascending }
            };
            request.Search.FieldSelection.Fields = new[] { "patent_id", "patent_title" };
            request.Search.OffsetPage.Offset = 0;
            request.Search.OffsetPage.Limit = 10;

            PatentsViewLocalRowQueryResult result = PatentsViewLocalRowQuery.Execute(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.Rows.Select(row => row.Fields["patent_id"]), Is.EqualTo(new[] { "p3", "p1" }));
                Assert.That(result.Rows[0].Fields.Keys, Is.EqualTo(new[] { "patent_id", "patent_title" }));
                Assert.That(result.Rows[0].ReadValue("score"), Is.Null);
                Assert.That(result.Rows[0].Provenance.Source, Is.EqualTo(DataProvenance.LocalMirror));
                Assert.That(result.Rows[0].Provenance.DatasetIdentifier, Is.EqualTo("g_patent"));
                Assert.That(result.Rows[0].Provenance.DatasetVersion, Is.EqualTo("2026-09"));
                Assert.That(result.Rows[0].Provenance.IsAuthoritative, Is.False);
                Assert.That(result.Rows[0].Provenance.FieldSources.Select(item => item.FieldPath), Is.EqualTo(new[] { "patent_id", "patent_title" }));
                Assert.That(result.Rows[0].Provenance.FieldSources.All(item => item.Source == DataProvenance.PatentsView), Is.True);
                Assert.That(result.Rows[0].Provenance.FieldSources.All(item => item.Delivery == DataProvenance.LocalMirror), Is.True);
                Assert.That(result.Metadata.Provenance, Is.EqualTo(DataProvenance.LocalMirror));
                Assert.That(result.Metadata.FieldSources.Select(item => item.FieldPath), Is.EqualTo(new[] { "patent_id", "patent_title" }));
                Assert.That(result.Metadata.FieldSources.All(item => item.Delivery == DataProvenance.LocalMirror), Is.True);
                Assert.That(result.Metadata.HasTotal, Is.True);
                Assert.That(result.Metadata.Total, Is.EqualTo(2));
                Assert.That(result.Metadata.ReturnedCount, Is.EqualTo(2));
                Assert.That(result.Metadata.HasOffsetPage, Is.True);
                Assert.That(result.ScannedRowCount, Is.EqualTo(4));
                Assert.That(result.MatchedRowCount, Is.EqualTo(2));
                Assert.That(result.IsComplete, Is.True);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void CursorPagesResumeAfterStableMultiFieldSortValues()
    {
        string path = CreateTable(
            "patent_id\tpatent_date\ttitle\n" +
            "p4\t2024-02-01\tFourth\n" +
            "p2\t2024-01-01\tSecond\n" +
            "p3\t2024-02-01\tThird\n" +
            "p1\t2024-01-01\tFirst\n");
        try
        {
            PatentsViewLocalRowQueryRequest firstRequest = CreateRequest(path);
            firstRequest.Search.UseCursorPage = true;
            firstRequest.Search.CursorPage.Size = 2;
            firstRequest.Search.Sorts = new[]
            {
                new SearchSort { Field = "patent_date", Direction = SortDirection.Ascending },
                new SearchSort { Field = "patent_id", Direction = SortDirection.Ascending }
            };

            PatentsViewLocalRowQueryResult first = PatentsViewLocalRowQuery.Execute(firstRequest);
            PatentsViewLocalRowQueryRequest secondRequest = CreateRequest(path);
            secondRequest.Search.UseCursorPage = true;
            secondRequest.Search.CursorPage.Size = 2;
            secondRequest.Search.CursorPage.AfterValues = first.Metadata.NextCursorPage.AfterValues;
            secondRequest.Search.Sorts = firstRequest.Search.Sorts;
            PatentsViewLocalRowQueryResult second = PatentsViewLocalRowQuery.Execute(secondRequest);

            Assert.Multiple(() =>
            {
                Assert.That(first.Rows.Select(row => row.Fields["patent_id"]), Is.EqualTo(new[] { "p1", "p2" }));
                Assert.That(first.Metadata.HasNextCursorPage, Is.True);
                Assert.That(first.Metadata.NextCursorPage.AfterValues, Is.EqualTo(new[] { "2024-01-01", "p2" }));
                Assert.That(first.IsComplete, Is.False);
                Assert.That(second.Rows.Select(row => row.Fields["patent_id"]), Is.EqualTo(new[] { "p3", "p4" }));
                Assert.That(second.Metadata.HasNextCursorPage, Is.False);
                Assert.That(second.Metadata.HasTotal, Is.True);
                Assert.That(second.IsComplete, Is.True);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ScanAndResultLimitsReturnBoundedPartialState()
    {
        string path = CreateTable(
            "patent_id\tscore\n" +
            "p1\t1\n" +
            "p2\t2\n" +
            "p3\t3\n" +
            "p4\t4\n" +
            "p5\t5\n");
        try
        {
            PatentsViewLocalRowQueryRequest request = CreateRequest(path);
            request.MaximumScannedRows = 3;
            request.Search.MaximumResults = 2;
            request.Search.OffsetPage.Limit = 10;

            PatentsViewLocalRowQueryResult result = PatentsViewLocalRowQuery.Execute(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.ScannedRowCount, Is.EqualTo(3));
                Assert.That(result.MatchedRowCount, Is.EqualTo(3));
                Assert.That(result.Rows.Select(row => row.Fields["patent_id"]), Is.EqualTo(new[] { "p1", "p2" }));
                Assert.That(result.ReachedScanLimit, Is.True);
                Assert.That(result.ReachedResultLimit, Is.True);
                Assert.That(result.HasNextOffset, Is.True);
                Assert.That(result.NextOffset, Is.EqualTo(2));
                Assert.That(result.Metadata.HasTotal, Is.False);
                Assert.That(result.IsComplete, Is.False);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void OffsetCoordinatesSelectTheRequestedSortedWindow()
    {
        string path = CreateTable(
            "patent_id\tscore\n" +
            "p3\t30\n" +
            "p1\t10\n" +
            "p4\t40\n" +
            "p2\t20\n");
        try
        {
            PatentsViewLocalRowQueryRequest request = CreateRequest(path);
            request.Search.OffsetPage.Offset = 1;
            request.Search.OffsetPage.Limit = 2;

            PatentsViewLocalRowQueryResult result = PatentsViewLocalRowQuery.Execute(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.Rows.Select(row => row.Fields["patent_id"]), Is.EqualTo(new[] { "p2", "p3" }));
                Assert.That(result.Metadata.OffsetPage.Offset, Is.EqualTo(1));
                Assert.That(result.Metadata.OffsetPage.Limit, Is.EqualTo(2));
                Assert.That(result.NextOffset, Is.EqualTo(3));
                Assert.That(result.HasNextOffset, Is.True);
                Assert.That(result.IsComplete, Is.False);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void MissingAndExtraColumnsRemainObservableInLocalResults()
    {
        string path = CreateTable(
            "patent_id\tpatent_title\n" +
            "p1\n" +
            "p2\tSecond patent\tunexpected\n");
        try
        {
            PatentsViewLocalRowQueryResult result = PatentsViewLocalRowQuery.Execute(CreateRequest(path));

            Assert.Multiple(() =>
            {
                Assert.That(result.Rows, Has.Length.EqualTo(2));
                Assert.That(result.Rows[0].HasMissingColumns, Is.True);
                Assert.That(result.Rows[0].Fields["patent_title"], Is.EqualTo(string.Empty));
                Assert.That(result.Rows[0].ExtraValues, Is.Empty);
                Assert.That(result.Rows[1].HasExtraColumns, Is.True);
                Assert.That(result.Rows[1].ExtraValues, Is.EqualTo(new[] { "unexpected" }));
                Assert.That(result.IsComplete, Is.True);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void MalformedLocalFilesPreserveDelimitedParserDiagnostics()
    {
        string quotedPath = CreateTable(
            "patent_id\tpatent_title\n" +
            "p1\t\"unterminated");
        string duplicateHeaderPath = CreateTable(
            "patent_id\tPatent Id\n" +
            "p1\tp2\n");
        try
        {
            ParseException quoting = Assert.Throws<ParseException>(() => PatentsViewLocalRowQuery.Execute(CreateRequest(quotedPath)))!;
            ParseException schema = Assert.Throws<ParseException>(() => PatentsViewLocalRowQuery.Execute(CreateRequest(duplicateHeaderPath)))!;

            Assert.Multiple(() =>
            {
                Assert.That(quoting.Format, Is.EqualTo(DocumentFormat.Csv));
                Assert.That(quoting.SourceName, Is.EqualTo(quotedPath));
                Assert.That(quoting.DataPath, Is.EqualTo("row[2].column[2]"));
                Assert.That(quoting.LineNumber, Is.EqualTo(2));
                Assert.That(schema.Format, Is.EqualTo(DocumentFormat.Text));
                Assert.That(schema.SourceName, Is.EqualTo("g_patent"));
                Assert.That(schema.DataPath, Is.EqualTo("Patent Id"));
                Assert.That(schema.LineNumber, Is.EqualTo(2));
            });
        }
        finally
        {
            File.Delete(quotedPath);
            File.Delete(duplicateHeaderPath);
        }
    }

    [Test]
    public void CallerCancellationStopsBeforeOpeningTheTable()
    {
        string path = CreateTable("patent_id\n" + "p1\n");
        try
        {
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            PatentsViewLocalRowQueryRequest request = CreateRequest(path);
            request.Search.Context.CancellationToken = cancellation.Token;

            Assert.Throws<OperationCanceledException>(() => PatentsViewLocalRowQuery.Execute(request));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void CallerCancellationInterruptsAnActiveLocalTableScan()
    {
        byte[] content = Encoding.UTF8.GetBytes(
            "patent_id\tpatent_title\n" +
            "p1\tFirst patent\n" +
            "p2\tSecond patent\n");
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        using CancelingLocalQueryStream input = new CancelingLocalQueryStream(content, cancellation);
        PatentsViewLocalRowQueryRequest request = CreateRequest("streamed.tsv");
        request.Search.Context.CancellationToken = cancellation.Token;

        OperationCanceledException failure = Assert.Throws<OperationCanceledException>(() => PatentsViewLocalRowQuery.ScanStream(request, input, cancellation.Token));

        Assert.Multiple(() =>
        {
            Assert.That(input.HasCancelled, Is.True);
            Assert.That(failure.CancellationToken, Is.EqualTo(cancellation.Token));
        });
    }

    [Test]
    public void CursorQueriesRejectNonUniqueKeysAndAllQueriesRequireSorts()
    {
        string path = CreateTable(
            "patent_id\tyear\n" +
            "p1\t2024\n" +
            "p2\t2024\n");
        try
        {
            PatentsViewLocalRowQueryRequest unsorted = CreateRequest(path);
            unsorted.Search.Sorts = Array.Empty<SearchSort>();
            ValidationException sortFailure = Assert.Throws<ValidationException>(() => PatentsViewLocalRowQuery.Execute(unsorted))!;

            PatentsViewLocalRowQueryRequest duplicate = CreateRequest(path);
            duplicate.Search.UseCursorPage = true;
            duplicate.Search.CursorPage.Size = 1;
            duplicate.Search.Sorts = new[] { new SearchSort { Field = "year" } };
            ValidationException uniquenessFailure = Assert.Throws<ValidationException>(() => PatentsViewLocalRowQuery.Execute(duplicate))!;

            Assert.Multiple(() =>
            {
                Assert.That(sortFailure.ErrorCode, Is.EqualTo("DETERMINISTIC_SORT_REQUIRED"));
                Assert.That(uniquenessFailure.ErrorCode, Is.EqualTo("DETERMINISTIC_SORT_REQUIRED"));
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static SearchFilter Leaf(string field, SearchOperator searchOperator, string value)
    {
        return new SearchFilter { Field = field, Operator = searchOperator, Value = value };
    }

    private static PatentsViewLocalRowQueryRequest CreateRequest(string path)
    {
        SearchRequest search = new SearchRequest
        {
            Sorts = new[] { new SearchSort { Field = "patent_id", Direction = SortDirection.Ascending } },
            OffsetPage = new OffsetPage { Offset = 0, Limit = 100 },
            MaximumResults = 100,
            Context = new RequestContext()
        };
        PatentsViewLocalTableInfo table = new PatentsViewLocalTableInfo
        {
            Family = DatasetFamily.PatentsViewGrant,
            TableIdentifier = "g_patent",
            FilePath = path,
            Delimiter = '\t',
            FileExists = true,
            Provenance = new PatentDataProvenance
            {
                Source = DataProvenance.LocalMirror,
                DatasetIdentifier = "g_patent",
                DatasetVersion = "2026-09",
                IsAuthoritative = false
            }
        };
        return new PatentsViewLocalRowQueryRequest
        {
            Table = table,
            Search = search,
            MaximumScannedRows = 100,
            Limits = new UsptoLimits { MaximumResults = 100 }
        };
    }

    private static string CreateTable(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), "uspto-local-query-" + Guid.NewGuid().ToString("N") + ".tsv");
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return path;
    }
}

internal class CancelingLocalQueryStream : MemoryStream
{
    internal CancellationTokenSource Cancellation;
    internal bool HasCancelled = false;

    internal CancelingLocalQueryStream(byte[] content, CancellationTokenSource cancellation)
        : base(content, false)
    {
        Cancellation = cancellation;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = base.Read(buffer, offset, count);
        CancelAfterRead();
        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        int read = base.Read(buffer);
        CancelAfterRead();
        return read;
    }

    private void CancelAfterRead()
    {
        if (!HasCancelled)
        {
            HasCancelled = true;
            Cancellation.Cancel();
        }
    }
}
