using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class VirtualizationProviderCoreTests
{
    [Test]
    public void RequestsAdvanceGenerationsDetachContextAndCancelSupersededWork()
    {
        VirtualizationProviderCore core = new VirtualizationProviderCore();
        UiSort[] sorts = new UiSort[] { new UiSort { Key = "name", Direction = UiSortDirection.Ascending, Priority = 0 } };
        UiFilter[] filters = new UiFilter[]
        {
            new UiFilter
            {
                Key = "status",
                Operator = "equals",
                Value = Text("active"),
                IgnoreCase = true
            }
        };
        UiGroup[] groups = new UiGroup[]
        {
            new UiGroup { Key = "region", Label = "Region", ItemKeys = new string[] { "west" }, IsExpanded = true }
        };

        VirtualRangeProviderOperation first = core.BeginRangeProviderRequest(10, 5, sorts, filters, groups, "tree-parent");
        sorts[0].Key = "mutated";
        filters[0].Value.StringValue = "mutated";
        groups[0].ItemKeys[0] = "mutated";
        first.Request!.Sorts[0].Key = "mutated result";
        UiRangeRequest pending = core.ReadPendingRangeProviderRequest()!;
        VirtualRangeProviderOperation second = core.BeginRangeProviderRequest(20, 3, null, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Applied, Is.True);
            Assert.That(first.IsLoading, Is.True);
            Assert.That(first.Request.Generation, Is.EqualTo(1));
            Assert.That(first.Rows.Single().Key, Is.EqualTo("$loading:1"));
            Assert.That(first.Rows.Single().IsPlaceholder, Is.True);
            Assert.That(pending.Sorts.Single().Key, Is.EqualTo("name"));
            Assert.That(pending.Filters.Single().Value.StringValue, Is.EqualTo("active"));
            Assert.That(pending.Groups.Single().ItemKeys, Is.EqualTo(new string[] { "west" }));
            Assert.That(pending.ParentKey, Is.EqualTo("tree-parent"));
            Assert.That(second.Request!.Generation, Is.EqualTo(2));
            Assert.That(second.Cancelled, Is.True);
            Assert.That(second.CancelledGeneration, Is.EqualTo(1));
            Assert.That(core.ReadPendingRangeProviderRequest()!.Start, Is.EqualTo(20));
        });
    }

    [Test]
    public void CancellationAndNewerRequestsRejectStaleResultsWithoutChangingPendingState()
    {
        VirtualizationProviderCore core = new VirtualizationProviderCore();
        VirtualRangeProviderOperation first = core.BeginRangeProviderRequest(0, 2, null, null, null, null);
        VirtualRangeProviderOperation cancelled = core.CancelRangeProviderRequest(first.Request!.Generation);
        VirtualRangeProviderOperation afterCancellation = core.AcceptRangeProviderResult(Range(1, 0, false, 0, false, Item("old")));
        VirtualRangeProviderOperation second = core.BeginRangeProviderRequest(2, 2, null, null, null, null);
        VirtualRangeProviderOperation stale = core.AcceptRangeProviderResult(Range(1, 0, false, 0, false, Item("old")));
        UiRangeRequest stillPending = core.ReadPendingRangeProviderRequest()!;
        VirtualRangeProviderOperation accepted = core.AcceptRangeProviderResult(Range(2, 2, false, 0, false, Item("new")));

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Success, Is.True);
            Assert.That(cancelled.Cancelled, Is.True);
            Assert.That(cancelled.CancelledGeneration, Is.EqualTo(1));
            Assert.That(afterCancellation.Success, Is.False);
            Assert.That(afterCancellation.Stale, Is.True);
            Assert.That(afterCancellation.Code, Is.EqualTo("virtualization.stale-result"));
            Assert.That(second.Request!.Generation, Is.EqualTo(2));
            Assert.That(stale.Success, Is.False);
            Assert.That(stale.Stale, Is.True);
            Assert.That(stillPending.Generation, Is.EqualTo(2));
            Assert.That(stillPending.Start, Is.EqualTo(2));
            Assert.That(accepted.Success, Is.True);
            Assert.That(accepted.Result!.Items.Single().Key, Is.EqualTo("new"));
            Assert.That(core.ReadPendingRangeProviderRequest(), Is.Null);
        });
    }

    [Test]
    public void PartialAndEndResultsPreserveUnknownTotalSemantics()
    {
        VirtualizationProviderCore core = new VirtualizationProviderCore();
        core.BeginRangeProviderRequest(10, 4, null, null, null, null);
        VirtualRangeProviderOperation partial = core.AcceptRangeProviderResult(Range(1, 10, false, 0, false, Item("ten"), Item("eleven")));
        partial.Result!.Items[0].Key = "mutated";
        UiRangeResult storedPartial = core.ReadCurrentRangeProviderResult()!;

        core.BeginRangeProviderRequest(12, 4, null, null, null, null);
        VirtualRangeProviderOperation unknownEnd = core.AcceptRangeProviderResult(Range(2, 12, false, 0, true, Item("twelve")));
        core.BeginRangeProviderRequest(13, 4, null, null, null, null);
        VirtualRangeProviderOperation knownEnd = core.AcceptRangeProviderResult(Range(3, 13, true, 14, false, Item("thirteen")));

        Assert.Multiple(() =>
        {
            Assert.That(partial.IsPartial, Is.True);
            Assert.That(partial.IsLoading, Is.True);
            Assert.That(partial.IsEnd, Is.False);
            Assert.That(partial.Rows.Select(row => row.Key), Is.EqualTo(new string[] { "ten", "eleven", "$loading:1" }));
            Assert.That(storedPartial.Items[0].Key, Is.EqualTo("ten"));
            Assert.That(storedPartial.HasTotalCount, Is.False);
            Assert.That(unknownEnd.IsPartial, Is.False);
            Assert.That(unknownEnd.IsEnd, Is.True);
            Assert.That(unknownEnd.Result!.HasTotalCount, Is.False);
            Assert.That(unknownEnd.Rows.Select(row => row.Key), Is.EqualTo(new string[] { "twelve" }));
            Assert.That(knownEnd.IsEnd, Is.True);
            Assert.That(knownEnd.Result!.HasTotalCount, Is.True);
            Assert.That(knownEnd.Result.TotalCount, Is.EqualTo(14));
        });
    }

    [Test]
    public void ProviderErrorsCreateRetryRowsAndRetryTheDetachedRequestContext()
    {
        VirtualizationProviderCore core = new VirtualizationProviderCore();
        UiSort[] sorts = new UiSort[] { new UiSort { Key = "rank", Direction = UiSortDirection.Descending, Priority = 0 } };
        UiFilter[] filters = new UiFilter[] { new UiFilter { Key = "visible", Operator = "equals", Value = Text("true") } };
        UiGroup[] groups = new UiGroup[] { new UiGroup { Key = "category", ItemKeys = new string[] { "a", "b" } } };
        core.BeginRangeProviderRequest(4, 3, sorts, filters, groups, "branch");
        UiDiagnostic diagnostic = new UiDiagnostic
        {
            Severity = UiDiagnosticSeverity.Error,
            Code = "provider.offline",
            Message = "Provider unavailable",
            Location = "/provider"
        };
        UiRangeResult failedResult = Range(1, 4, false, 0, false);
        failedResult.Diagnostic = diagnostic;

        VirtualRangeProviderOperation failed = core.AcceptRangeProviderResult(failedResult);
        diagnostic.Message = "mutated";
        failed.Result!.Diagnostic!.Message = "mutated result";
        UiRangeResult storedFailure = core.ReadCurrentRangeProviderResult()!;
        VirtualRangeProviderOperation retry = core.RetryRangeProviderRequest();
        retry.Request!.Groups[0].ItemKeys[0] = "mutated retry";
        UiRangeRequest pendingRetry = core.ReadPendingRangeProviderRequest()!;
        VirtualRangeProviderOperation recovered = core.AcceptRangeProviderResult(Range(2, 4, false, 0, true, Item("recovered")));
        VirtualRangeProviderOperation nothingToRetry = core.RetryRangeProviderRequest();

        Assert.Multiple(() =>
        {
            Assert.That(failed.Success, Is.True);
            Assert.That(failed.HasError, Is.True);
            Assert.That(failed.CanRetry, Is.True);
            Assert.That(failed.Code, Is.EqualTo("virtualization.provider-error"));
            Assert.That(failed.Rows.Select(row => row.Key), Is.EqualTo(new string[] { "$error:1", "$retry:1" }));
            Assert.That(failed.Rows.Select(row => row.IsPlaceholder), Is.All.True);
            Assert.That(storedFailure.Diagnostic!.Message, Is.EqualTo("Provider unavailable"));
            Assert.That(retry.Request.Generation, Is.EqualTo(2));
            Assert.That(retry.Request.Start, Is.EqualTo(4));
            Assert.That(retry.Request.Count, Is.EqualTo(3));
            Assert.That(retry.Request.Sorts.Single().Key, Is.EqualTo("rank"));
            Assert.That(retry.Request.Filters.Single().Key, Is.EqualTo("visible"));
            Assert.That(retry.Request.ParentKey, Is.EqualTo("branch"));
            Assert.That(pendingRetry.Groups.Single().ItemKeys, Is.EqualTo(new string[] { "a", "b" }));
            Assert.That(recovered.Success, Is.True);
            Assert.That(recovered.HasError, Is.False);
            Assert.That(nothingToRetry.Success, Is.False);
            Assert.That(nothingToRetry.Code, Is.EqualTo("virtualization.nothing-to-retry"));
        });
    }

    [Test]
    public void RejectsMalformedRequestsAndMatchingResults()
    {
        VirtualizationProviderCore core = new VirtualizationProviderCore();
        UiSort invalidSort = new UiSort { Key = "sort", Direction = UiSortDirection.None };
        UiFilter invalidFilter = new UiFilter { Key = "filter", Operator = " " };
        UiGroup invalidGroup = new UiGroup { Key = " " };

        Assert.Multiple(() =>
        {
            Assert.That(() => core.BeginRangeProviderRequest(-1, 1, null, null, null, null), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.BeginRangeProviderRequest(0, 0, null, null, null, null), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => core.BeginRangeProviderRequest(0, 1, new UiSort[] { invalidSort }, null, null, null), Throws.ArgumentException);
            Assert.That(() => core.BeginRangeProviderRequest(0, 1, null, new UiFilter[] { invalidFilter }, null, null), Throws.ArgumentException);
            Assert.That(() => core.BeginRangeProviderRequest(0, 1, null, null, new UiGroup[] { invalidGroup }, null), Throws.ArgumentException);
        });

        core.BeginRangeProviderRequest(0, 1, null, null, null, null);
        UiRangeResult wrongStart = Range(1, 1, false, 0, false, Item("one"));
        UiRangeResult tooMany = Range(1, 0, false, 0, false, Item("one"), Item("two"));
        UiRangeResult duplicate = Range(1, 0, false, 0, false, Item("same"), Item("same"));
        UiRangeResult invalidTotal = Range(1, 0, true, 0, false, Item("one"));

        Assert.Multiple(() =>
        {
            Assert.That(() => core.AcceptRangeProviderResult(null), Throws.ArgumentNullException);
            Assert.That(() => core.AcceptRangeProviderResult(wrongStart), Throws.ArgumentException);
            Assert.That(() => core.AcceptRangeProviderResult(tooMany), Throws.ArgumentException);
            Assert.That(() => core.AcceptRangeProviderResult(duplicate), Throws.ArgumentException);
            Assert.That(() => core.AcceptRangeProviderResult(invalidTotal), Throws.ArgumentException);
        });
    }

    private static UiRangeResult Range(int generation, int start, bool hasTotalCount, int totalCount, bool isEnd, params UiCollectionItem[] items)
    {
        return new UiRangeResult
        {
            Generation = generation,
            Start = start,
            Items = items,
            HasTotalCount = hasTotalCount,
            TotalCount = totalCount,
            IsEnd = isEnd
        };
    }

    private static UiCollectionItem Item(string key)
    {
        return new UiCollectionItem { Key = key, Value = Text(key) };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
