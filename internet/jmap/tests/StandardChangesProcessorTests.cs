namespace jmap.Tests;

public class StandardChangesProcessorTests
{
    [Test]
    public void ChangesAreCoalescedIntoDisjointPreferredClassifications()
    {
        StandardChangesProcessor processor = new StandardChangesProcessor(100);
        JmapChangesRequest request = Request("account-1", "old");
        StandardChangesData data = Data(
            "current",
            Change("created", StandardChangeKind.Created, "s1"),
            Change("created", StandardChangeKind.Updated, "s2"),
            Change("updated", StandardChangeKind.Updated, "s3"),
            Change("updated", StandardChangeKind.Updated, "s4"),
            Change("destroyed", StandardChangeKind.Updated, "s5"),
            Change("destroyed", StandardChangeKind.Destroyed, "s6"),
            Change("transient", StandardChangeKind.Created, "s7"),
            Change("transient", StandardChangeKind.Destroyed, "s8"),
            Change("deleted", StandardChangeKind.Destroyed, "current"));

        StandardChangesResult result = processor.ProcessChanges(request, data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Response!.AccountId, Is.EqualTo("account-1"));
            Assert.That(result.Response.OldState, Is.EqualTo("old"));
            Assert.That(result.Response.NewState, Is.EqualTo("current"));
            Assert.That(result.Response.HasMoreChanges, Is.False);
            Assert.That(result.Response.Created, Is.EqualTo(new[] { "created" }));
            Assert.That(result.Response.Updated, Is.EqualTo(new[] { "updated" }));
            Assert.That(result.Response.Destroyed, Is.EqualTo(new[] { "destroyed", "deleted" }));
        }
    }

    [Test]
    public void NoChangesStillAdvancesToTheCurrentState()
    {
        StandardChangesProcessor processor = new StandardChangesProcessor(100);
        StandardChangesData data = Data("current");

        StandardChangesResult result = processor.ProcessChanges(Request("account-1", "old"), data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.OldState, Is.EqualTo("old"));
            Assert.That(result.Response.NewState, Is.EqualTo("current"));
            Assert.That(result.Response.HasMoreChanges, Is.False);
            Assert.That(result.Response.Created, Is.Empty);
            Assert.That(result.Response.Updated, Is.Empty);
            Assert.That(result.Response.Destroyed, Is.Empty);
        }
    }

    [Test]
    public void ExplicitMaxChangesProducesAnIntermediateStatePage()
    {
        StandardChangesProcessor processor = new StandardChangesProcessor(100);
        JmapChangesRequest request = Request("account-1", "old");
        request.HasMaxChanges = true;
        request.MaxChanges = 2;
        StandardChangesData data = Data(
            "s3",
            Change("new-1", StandardChangeKind.Created, "s1"),
            Change("changed-1", StandardChangeKind.Updated, "s2"),
            Change("gone-1", StandardChangeKind.Destroyed, "s3"));

        StandardChangesResult result = processor.ProcessChanges(request, data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.NewState, Is.EqualTo("s2"));
            Assert.That(result.Response.HasMoreChanges, Is.True);
            Assert.That(result.Response.Created, Is.EqualTo(new[] { "new-1" }));
            Assert.That(result.Response.Updated, Is.EqualTo(new[] { "changed-1" }));
            Assert.That(result.Response.Destroyed, Is.Empty);
        }
    }

    [Test]
    public void PagingUsesTheLatestPrefixThatFitsAfterCoalescing()
    {
        StandardChangesProcessor processor = new StandardChangesProcessor(2);
        StandardChangesData data = Data(
            "s5",
            Change("a", StandardChangeKind.Created, "s1"),
            Change("b", StandardChangeKind.Created, "s2"),
            Change("c", StandardChangeKind.Created, "s3"),
            Change("a", StandardChangeKind.Destroyed, "s4"),
            Change("d", StandardChangeKind.Updated, "s5"));

        StandardChangesResult result = processor.ProcessChanges(Request("account-1", "old"), data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.NewState, Is.EqualTo("s4"));
            Assert.That(result.Response.HasMoreChanges, Is.True);
            Assert.That(result.Response.Created, Is.EqualTo(new[] { "b", "c" }));
            Assert.That(result.Response.Updated, Is.Empty);
            Assert.That(result.Response.Destroyed, Is.Empty);
        }
    }

    [Test]
    public void CoalescingCanKeepManyEventsWithinTheChangeLimit()
    {
        StandardChangesProcessor processor = new StandardChangesProcessor(1);
        StandardChangesData data = Data(
            "s4",
            Change("temporary", StandardChangeKind.Created, "s1"),
            Change("temporary", StandardChangeKind.Updated, "s2"),
            Change("temporary", StandardChangeKind.Destroyed, "s3"),
            Change("remaining", StandardChangeKind.Updated, "s4"));

        StandardChangesResult result = processor.ProcessChanges(Request("account-1", "old"), data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.NewState, Is.EqualTo("s4"));
            Assert.That(result.Response.HasMoreChanges, Is.False);
            Assert.That(result.Response.Created, Is.Empty);
            Assert.That(result.Response.Updated, Is.EqualTo(new[] { "remaining" }));
            Assert.That(result.Response.Destroyed, Is.Empty);
        }
    }

    [Test]
    public void NullMaxChangesUsesTheConfiguredDefault()
    {
        StandardChangesProcessor processor = new StandardChangesProcessor(2);
        JmapChangesRequest request = Request("account-1", "old");
        request.HasMaxChanges = true;
        request.IsMaxChangesNull = true;
        StandardChangesData data = Data(
            "s3",
            Change("a", StandardChangeKind.Created, "s1"),
            Change("b", StandardChangeKind.Updated, "s2"),
            Change("c", StandardChangeKind.Destroyed, "s3"));

        StandardChangesResult result = processor.ProcessChanges(request, data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.HasMoreChanges, Is.True);
            Assert.That(result.Response.NewState, Is.EqualTo("s2"));
            Assert.That(result.Response.Created.Count + result.Response.Updated.Count + result.Response.Destroyed.Count, Is.EqualTo(2));
        }
    }

    [Test]
    public void InvalidCommonArgumentsMapToInvalidArguments()
    {
        StandardChangesProcessor processor = new StandardChangesProcessor(10);
        StandardChangesData data = Data("current");
        JmapChangesRequest invalidMaximum = Request("account-1", "old");
        invalidMaximum.HasMaxChanges = true;
        invalidMaximum.MaxChanges = 0;
        JmapChangesRequest inconsistentNull = Request("account-1", "old");
        inconsistentNull.IsMaxChangesNull = true;

        StandardChangesResult missingRequest = processor.ProcessChanges(null, data);
        StandardChangesResult missingAccount = processor.ProcessChanges(Request(null, "old"), data);
        StandardChangesResult missingState = processor.ProcessChanges(Request("account-1", null), data);
        StandardChangesResult zeroMaximum = processor.ProcessChanges(invalidMaximum, data);
        StandardChangesResult badFlags = processor.ProcessChanges(inconsistentNull, data);

        using (Assert.EnterMultipleScope())
        {
            AssertInvalidArguments(missingRequest);
            AssertInvalidArguments(missingAccount);
            AssertInvalidArguments(missingState);
            AssertInvalidArguments(zeroMaximum);
            AssertInvalidArguments(badFlags);
        }
    }

    [Test]
    public void UnavailableHistoryAndIntermediateStatesMapToCannotCalculateChanges()
    {
        StandardChangesProcessor processor = new StandardChangesProcessor(1);
        StandardChangesData unavailable = Data("current");
        unavailable.CanCalculateChanges = false;
        StandardChangesData noIntermediate = Data(
            "s2",
            Change("a", StandardChangeKind.Created, null),
            Change("b", StandardChangeKind.Updated, null));
        noIntermediate.CanCalculateIntermediateStates = false;

        StandardChangesResult historyResult = processor.ProcessChanges(Request("account-1", "old"), unavailable);
        StandardChangesResult pagingResult = processor.ProcessChanges(Request("account-1", "old"), noIntermediate);

        using (Assert.EnterMultipleScope())
        {
            AssertCannotCalculate(historyResult);
            AssertCannotCalculate(pagingResult);
        }
    }

    [Test]
    public void InvalidBackendChangeDataIsAProtocolFailure()
    {
        StandardChangesProcessor processor = new StandardChangesProcessor(10);
        StandardChangesData missingState = Data(null);
        StandardChangesData invalidId = Data(
            "current",
            Change("bad/id", StandardChangeKind.Updated, "current"));
        StandardChangesData invalidKind = Data(
            "current",
            Change("m1", (StandardChangeKind)99, "current"));

        JmapProtocolException stateError = Assert.Throws<JmapProtocolException>(
            () => processor.ProcessChanges(Request("account-1", "old"), missingState))!;
        JmapProtocolException idError = Assert.Throws<JmapProtocolException>(
            () => processor.ProcessChanges(Request("account-1", "old"), invalidId))!;
        JmapProtocolException kindError = Assert.Throws<JmapProtocolException>(
            () => processor.ProcessChanges(Request("account-1", "old"), invalidKind))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stateError.Path, Is.EqualTo("newState"));
            Assert.That(idError.Path, Is.EqualTo("changes/0/id"));
            Assert.That(kindError.Path, Is.EqualTo("changes/0/kind"));
        }
    }

    private static JmapChangesRequest Request(string? accountId, string? sinceState)
    {
        JmapChangesRequest request = new JmapChangesRequest();
        request.AccountId = accountId;
        request.SinceState = sinceState;
        return request;
    }

    private static StandardChangesData Data(string? currentState, params StandardChange[] changes)
    {
        StandardChangesData data = new StandardChangesData();
        data.CurrentState = currentState;
        data.Changes.AddRange(changes);
        return data;
    }

    private static StandardChange Change(string id, StandardChangeKind kind, string? stateAfter)
    {
        StandardChange change = new StandardChange();
        change.Id = id;
        change.Kind = kind;
        change.StateAfter = stateAfter;
        return change;
    }

    private static void AssertInvalidArguments(StandardChangesResult result)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Response, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Type, Is.EqualTo(JmapErrorCodes.InvalidArguments));
            Assert.That(result.Error.Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
        }
    }

    private static void AssertCannotCalculate(StandardChangesResult result)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Response, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Type, Is.EqualTo(JmapErrorCodes.CannotCalculateChanges));
            Assert.That(result.Error.Kind, Is.EqualTo(JmapMethodErrorKind.CannotCalculateChanges));
        }
    }
}
