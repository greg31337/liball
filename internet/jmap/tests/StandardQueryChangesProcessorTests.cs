using System.Text.Json;

namespace jmap.Tests;

public class StandardQueryChangesProcessorTests
{
    [Test]
    public void MembershipChangesReturnStatesTotalsRemovalsAndIndexedAdditions()
    {
        StandardQueryChangesProcessor processor = Processor();
        JmapQueryChangesRequest request = Request();
        request.HasCalculateTotal = true;
        request.CalculateTotal = true;
        StandardQueryChangesData data = Data("new-state", new[] { "a", "b", "c" }, new[] { "a", "c", "d" });

        StandardQueryChangesResult result = processor.ProcessQueryChanges(request, data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Response!.AccountId, Is.EqualTo("account-1"));
            Assert.That(result.Response.OldQueryState, Is.EqualTo("old-state"));
            Assert.That(result.Response.NewQueryState, Is.EqualTo("new-state"));
            Assert.That(result.Response.HasTotal, Is.True);
            Assert.That(result.Response.Total, Is.EqualTo(3));
            Assert.That(result.Response.Removed, Is.EqualTo(new[] { "b" }));
            AssertAdded(result.Response.Added, new[] { "d" }, new long[] { 2 });
        }
    }

    [Test]
    public void ReorderedIdsAreRepresentedAsRemovalAndInsertion()
    {
        StandardQueryChangesData data = Data(
            "new-state",
            new[] { "a", "b", "c", "d" },
            new[] { "c", "a", "b", "d" });

        StandardQueryChangesResult result = Processor().ProcessQueryChanges(Request(), data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Response!.Removed, Is.EqualTo(new[] { "c" }));
            AssertAdded(result.Response.Added, new[] { "c" }, new long[] { 0 });
            Assert.That(ApplyDelta(data.OldIds, result.Response), Is.EqualTo(data.NewIds));
        }
    }

    [Test]
    public void ImmutableMatchesStayCachedWhileMutableMatchesAreReinserted()
    {
        StandardQueryChangesProcessor processor = Processor();
        StandardQueryChangesData immutableData = Data(
            "new-state",
            new[] { "a", "b", "c" },
            new[] { "a", "b", "c" });
        StandardQueryChangesData mutableData = Data(
            "new-state",
            new[] { "a", "b", "c" },
            new[] { "a", "b", "c" });
        mutableData.ReinsertIds.Add("b");

        StandardQueryChangesResult immutableResult = processor.ProcessQueryChanges(Request(), immutableData);
        StandardQueryChangesResult mutableResult = processor.ProcessQueryChanges(Request(), mutableData);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(immutableResult.Response!.Removed, Is.Empty);
            Assert.That(immutableResult.Response.Added, Is.Empty);
            Assert.That(mutableResult.Response!.Removed, Is.EqualTo(new[] { "b" }));
            AssertAdded(mutableResult.Response.Added, new[] { "b" }, new long[] { 1 });
            Assert.That(ApplyDelta(mutableData.OldIds, mutableResult.Response), Is.EqualTo(mutableData.NewIds));
        }
    }

    [Test]
    public void UpToIdErratumUsesOldAndNewBoundariesForMutableResults()
    {
        JmapQueryChangesRequest request = Request();
        request.UpToId = "anchor";
        StandardQueryChangesData data = Data(
            "new-state",
            new[] { "a", "b", "anchor", "old-after-1", "old-after-2" },
            new[] { "new-before", "anchor", "new-after-1", "new-after-2" });
        data.ReinsertIds.Add("anchor");

        StandardQueryChangesResult result = Processor().ProcessQueryChanges(request, data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Response!.Removed, Is.EqualTo(new[] { "a", "b", "anchor" }));
            AssertAdded(result.Response.Added, new[] { "new-before", "anchor" }, new long[] { 0, 1 });
            Assert.That(
                ApplyDelta(new List<string> { "a", "b", "anchor" }, result.Response),
                Is.EqualTo(new[] { "new-before", "anchor" }));
        }
    }

    [Test]
    public void UpToIdUsesIndependentOldRemovalAndNewAdditionBoundaries()
    {
        StandardQueryChangesProcessor processor = Processor();
        JmapQueryChangesRequest oldOnlyRequest = Request();
        oldOnlyRequest.UpToId = "d";
        StandardQueryChangesData oldOnlyData = Data(
            "new-state",
            new[] { "a", "b", "c", "d", "e" },
            new[] { "a", "x" });
        JmapQueryChangesRequest newOnlyRequest = Request();
        newOnlyRequest.UpToId = "x";
        StandardQueryChangesData newOnlyData = Data(
            "new-state",
            new[] { "a", "b", "c" },
            new[] { "a", "x", "d", "e" });

        StandardQueryChangesResult oldOnlyResult = processor.ProcessQueryChanges(oldOnlyRequest, oldOnlyData);
        StandardQueryChangesResult newOnlyResult = processor.ProcessQueryChanges(newOnlyRequest, newOnlyData);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(oldOnlyResult.Response!.Removed, Is.EqualTo(new[] { "b", "c", "d" }));
            AssertAdded(oldOnlyResult.Response.Added, new[] { "x" }, new long[] { 1 });
            Assert.That(newOnlyResult.Response!.Removed, Is.EqualTo(new[] { "b", "c" }));
            AssertAdded(newOnlyResult.Response.Added, new[] { "x" }, new long[] { 1 });
        }
    }

    [Test]
    public void MaxChangesIsAppliedAfterTheUpToIdOptimization()
    {
        StandardQueryChangesProcessor processor = Processor();
        StandardQueryChangesData data = Data(
            "new-state",
            new[] { "a", "b", "c" },
            new[] { "x", "a", "c" });
        JmapQueryChangesRequest tooSmall = Request();
        tooSmall.HasMaxChanges = true;
        tooSmall.MaxChanges = 1;
        JmapQueryChangesRequest bounded = Request();
        bounded.HasMaxChanges = true;
        bounded.MaxChanges = 1;
        bounded.UpToId = "a";

        StandardQueryChangesResult tooManyResult = processor.ProcessQueryChanges(tooSmall, data);
        StandardQueryChangesResult boundedResult = processor.ProcessQueryChanges(bounded, data);

        using (Assert.EnterMultipleScope())
        {
            AssertError(tooManyResult, JmapErrorCodes.TooManyChanges, JmapMethodErrorKind.TooManyChanges);
            Assert.That(boundedResult.IsSuccess, Is.True);
            Assert.That(boundedResult.Response!.Removed, Is.Empty);
            AssertAdded(boundedResult.Response.Added, new[] { "x" }, new long[] { 0 });
        }
    }

    [Test]
    public void NullMaximumIsUnlimitedAndZeroAllowsAnEmptyDelta()
    {
        StandardQueryChangesProcessor processor = Processor();
        JmapQueryChangesRequest unlimited = Request();
        unlimited.HasMaxChanges = true;
        unlimited.IsMaxChangesNull = true;
        JmapQueryChangesRequest zero = Request();
        zero.HasMaxChanges = true;
        zero.MaxChanges = 0;

        StandardQueryChangesResult unlimitedResult = processor.ProcessQueryChanges(
            unlimited,
            Data("new-state", new[] { "a", "b" }, new[] { "c", "d" }));
        StandardQueryChangesResult zeroResult = processor.ProcessQueryChanges(
            zero,
            Data("new-state", new[] { "a" }, new[] { "a" }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unlimitedResult.IsSuccess, Is.True);
            Assert.That(unlimitedResult.Response!.Removed, Has.Count.EqualTo(2));
            Assert.That(unlimitedResult.Response.Added, Has.Count.EqualTo(2));
            Assert.That(zeroResult.IsSuccess, Is.True);
            Assert.That(zeroResult.Response!.Removed, Is.Empty);
            Assert.That(zeroResult.Response.Added, Is.Empty);
        }
    }

    [Test]
    public void UnavailableHistoryReturnsCannotCalculateChanges()
    {
        StandardQueryChangesData data = new StandardQueryChangesData();
        data.CanCalculateChanges = false;

        StandardQueryChangesResult result = Processor().ProcessQueryChanges(Request(), data);

        AssertError(result, JmapErrorCodes.CannotCalculateChanges, JmapMethodErrorKind.CannotCalculateChanges);
    }

    [Test]
    public void CommonArgumentsAndQueryIdentityAreValidated()
    {
        StandardQueryChangesProcessor processor = Processor();
        StandardQueryChangesData data = Data("new-state", new string[0], new string[0]);
        JmapQueryChangesRequest missingAccount = Request();
        missingAccount.AccountId = null;
        JmapQueryChangesRequest missingState = Request();
        missingState.SinceQueryState = null;
        JmapQueryChangesRequest invalidMaximum = Request();
        invalidMaximum.HasMaxChanges = true;
        invalidMaximum.MaxChanges = -1;
        JmapQueryChangesRequest badFlags = Request();
        badFlags.IsMaxChangesNull = true;
        JmapQueryChangesRequest invalidUpTo = Request();
        invalidUpTo.UpToId = "bad/id";
        JmapQueryChangesRequest unsupportedFilter = Request();
        unsupportedFilter.Filter = Condition("unknown");
        JmapQueryChangesRequest unsupportedSort = Request();
        unsupportedSort.Sort = new List<JmapComparator> { Comparator("createdAt") };

        StandardQueryChangesResult nullResult = processor.ProcessQueryChanges(null, data);
        StandardQueryChangesResult accountResult = processor.ProcessQueryChanges(missingAccount, data);
        StandardQueryChangesResult stateResult = processor.ProcessQueryChanges(missingState, data);
        StandardQueryChangesResult maximumResult = processor.ProcessQueryChanges(invalidMaximum, data);
        StandardQueryChangesResult flagsResult = processor.ProcessQueryChanges(badFlags, data);
        StandardQueryChangesResult upToResult = processor.ProcessQueryChanges(invalidUpTo, data);
        StandardQueryChangesResult filterResult = processor.ProcessQueryChanges(unsupportedFilter, data);
        StandardQueryChangesResult sortResult = processor.ProcessQueryChanges(unsupportedSort, data);

        using (Assert.EnterMultipleScope())
        {
            AssertError(nullResult, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertError(accountResult, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertError(stateResult, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertError(maximumResult, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertError(flagsResult, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertError(upToResult, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertError(filterResult, JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter);
            AssertError(sortResult, JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort);
        }
    }

    [Test]
    public void TotalIsOmittedWhenCalculateTotalWasNotPresent()
    {
        JmapQueryChangesRequest request = Request();
        request.CalculateTotal = true;

        StandardQueryChangesResult result = Processor().ProcessQueryChanges(
            request,
            Data("new-state", new[] { "a" }, new[] { "a", "b" }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Response!.HasTotal, Is.False);
            Assert.That(result.Response.Total, Is.Zero);
        }
    }

    [Test]
    public void InvalidBackendStateListsAndReinsertionsAreProtocolFailures()
    {
        StandardQueryChangesProcessor processor = Processor();
        StandardQueryChangesData missingState = Data(null, new[] { "a" }, new[] { "a" });
        StandardQueryChangesData duplicate = Data("state", new[] { "a", "a" }, new[] { "a" });
        StandardQueryChangesData invalidId = Data("state", new[] { "a" }, new[] { "bad/id" });
        StandardQueryChangesData invalidReinsert = Data("state", new[] { "a" }, new[] { "b" });
        invalidReinsert.ReinsertIds.Add("a");

        JmapProtocolException stateError = Assert.Throws<JmapProtocolException>(() => processor.ProcessQueryChanges(Request(), missingState))!;
        JmapProtocolException duplicateError = Assert.Throws<JmapProtocolException>(() => processor.ProcessQueryChanges(Request(), duplicate))!;
        JmapProtocolException idError = Assert.Throws<JmapProtocolException>(() => processor.ProcessQueryChanges(Request(), invalidId))!;
        JmapProtocolException reinsertError = Assert.Throws<JmapProtocolException>(() => processor.ProcessQueryChanges(Request(), invalidReinsert))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stateError.Path, Is.EqualTo("newQueryState"));
            Assert.That(duplicateError.Path, Is.EqualTo("oldIds/1"));
            Assert.That(idError.Path, Is.EqualTo("newIds/0"));
            Assert.That(reinsertError.Path, Is.EqualTo("reinsertIds/0"));
        }
    }

    private static StandardQueryChangesProcessor Processor()
    {
        StandardQueryProcessor queryProcessor = new StandardQueryProcessor(
            100,
            4,
            new List<string> { "name" },
            new List<string> { "name", "size" },
            new List<string> { "name" },
            new List<string> { "i;unicode-casemap" },
            null);
        return new StandardQueryChangesProcessor(queryProcessor);
    }

    private static JmapQueryChangesRequest Request()
    {
        JmapQueryChangesRequest request = new JmapQueryChangesRequest();
        request.AccountId = "account-1";
        request.SinceQueryState = "old-state";
        return request;
    }

    private static StandardQueryChangesData Data(string? queryState, string[] oldIds, string[] newIds)
    {
        StandardQueryChangesData data = new StandardQueryChangesData();
        data.CurrentQueryState = queryState;
        data.OldIds.AddRange(oldIds);
        data.NewIds.AddRange(newIds);
        return data;
    }

    private static JmapFilterNode Condition(string property)
    {
        JmapFilterNode filter = new JmapFilterNode();
        filter.FilterProperties.Add(property, JsonSerializer.SerializeToElement("value"));
        return filter;
    }

    private static JmapComparator Comparator(string property)
    {
        JmapComparator comparator = new JmapComparator();
        comparator.Property = property;
        return comparator;
    }

    private static List<string> ApplyDelta(List<string> oldIds, JmapQueryChangesResponse response)
    {
        List<string> result = new List<string>(oldIds);
        foreach (string id in response.Removed)
        {
            result.Remove(id);
        }

        foreach (JmapAddedItem item in response.Added)
        {
            result.Insert((int)item.Index, item.Id!);
        }

        return result;
    }

    private static void AssertAdded(List<JmapAddedItem> actual, string[] ids, long[] indexes)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual.Select(item => item.Id), Is.EqualTo(ids));
            Assert.That(actual.Select(item => item.Index), Is.EqualTo(indexes));
        }
    }

    private static void AssertError(StandardQueryChangesResult result, string type, JmapMethodErrorKind kind)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Response, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Type, Is.EqualTo(type));
            Assert.That(result.Error.Kind, Is.EqualTo(kind));
        }
    }
}
