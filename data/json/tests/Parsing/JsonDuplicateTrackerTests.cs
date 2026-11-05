namespace json.Tests;

public class JsonDuplicateTrackerTests
{
    [Test]
    public void PreserveAppendsEveryOccurrenceInSourceOrder()
    {
        JsonDuplicateTracker tracker = new JsonDuplicateTracker(JsonDuplicateKeyPolicy.Preserve);
        tracker.EnterObject();

        AssertDecision(
            tracker.RegisterPropertyName("name"),
            JsonDuplicateDecisionKind.Append,
            false,
            0);
        AssertDecision(
            tracker.RegisterPropertyName("name"),
            JsonDuplicateDecisionKind.Append,
            true,
            1);
        AssertDecision(
            tracker.RegisterPropertyName("other"),
            JsonDuplicateDecisionKind.Append,
            false,
            2);
        AssertDecision(
            tracker.RegisterPropertyName("name"),
            JsonDuplicateDecisionKind.Append,
            true,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(tracker.GetPolicy(), Is.EqualTo(JsonDuplicateKeyPolicy.Preserve));
            Assert.That(tracker.GetCurrentRetainedMemberCount(), Is.EqualTo(4));
            Assert.That(tracker.ExitObject(), Is.EqualTo(4));
        });
    }

    [Test]
    public void RejectThrowsBeforeChangingTheScope()
    {
        JsonDuplicateTracker tracker = new JsonDuplicateTracker(JsonDuplicateKeyPolicy.Reject);
        tracker.EnterObject();
        AssertDecision(
            tracker.RegisterPropertyName("duplicate"),
            JsonDuplicateDecisionKind.Append,
            false,
            0);

        JsonParseException exception = Assert.Throws<JsonParseException>(
            () => tracker.RegisterPropertyName("duplicate"))
            ?? throw new InvalidOperationException("Expected JsonParseException.");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            Assert.That(exception.Diagnostic.ActualToken, Is.EqualTo("duplicate"));
            Assert.That(exception.Diagnostic.Depth, Is.EqualTo(1));
            Assert.That(tracker.GetCurrentRetainedMemberCount(), Is.EqualTo(1));
        });

        AssertDecision(
            tracker.RegisterPropertyName("next"),
            JsonDuplicateDecisionKind.Append,
            false,
            1);
    }

    [Test]
    public void KeepFirstSkipsDuplicatesAndRetainsTheirOriginalIndex()
    {
        JsonDuplicateTracker tracker = new JsonDuplicateTracker(JsonDuplicateKeyPolicy.KeepFirst);
        tracker.EnterObject();

        AssertDecision(
            tracker.RegisterPropertyName("first"),
            JsonDuplicateDecisionKind.Append,
            false,
            0);
        AssertDecision(
            tracker.RegisterPropertyName("second"),
            JsonDuplicateDecisionKind.Append,
            false,
            1);
        AssertDecision(
            tracker.RegisterPropertyName("first"),
            JsonDuplicateDecisionKind.Skip,
            true,
            0);
        AssertDecision(
            tracker.RegisterPropertyName("second"),
            JsonDuplicateDecisionKind.Skip,
            true,
            1);

        Assert.That(tracker.GetCurrentRetainedMemberCount(), Is.EqualTo(2));
    }

    [Test]
    public void KeepLastReplacesThePreviouslyRetainedIndex()
    {
        JsonDuplicateTracker tracker = new JsonDuplicateTracker(JsonDuplicateKeyPolicy.KeepLast);
        tracker.EnterObject();

        AssertDecision(
            tracker.RegisterPropertyName("first"),
            JsonDuplicateDecisionKind.Append,
            false,
            0);
        AssertDecision(
            tracker.RegisterPropertyName("second"),
            JsonDuplicateDecisionKind.Append,
            false,
            1);
        AssertDecision(
            tracker.RegisterPropertyName("first"),
            JsonDuplicateDecisionKind.Replace,
            true,
            0);
        AssertDecision(
            tracker.RegisterPropertyName("first"),
            JsonDuplicateDecisionKind.Replace,
            true,
            0);
        AssertDecision(
            tracker.RegisterPropertyName("second"),
            JsonDuplicateDecisionKind.Replace,
            true,
            1);

        Assert.That(tracker.GetCurrentRetainedMemberCount(), Is.EqualTo(2));
    }

    [Test]
    public void ComparisonUsesExactDecodedOrdinalUtf16Names()
    {
        JsonDuplicateTracker tracker = new JsonDuplicateTracker(JsonDuplicateKeyPolicy.Preserve);
        tracker.EnterObject();

        AssertDecision(tracker.RegisterPropertyName(""), JsonDuplicateDecisionKind.Append, false, 0);
        AssertDecision(tracker.RegisterPropertyName(""), JsonDuplicateDecisionKind.Append, true, 1);
        AssertDecision(tracker.RegisterPropertyName("name"), JsonDuplicateDecisionKind.Append, false, 2);
        AssertDecision(tracker.RegisterPropertyName("name"), JsonDuplicateDecisionKind.Append, true, 3);
        AssertDecision(tracker.RegisterPropertyName("Name"), JsonDuplicateDecisionKind.Append, false, 4);
        AssertDecision(tracker.RegisterPropertyName("é"), JsonDuplicateDecisionKind.Append, false, 5);
        AssertDecision(tracker.RegisterPropertyName("e\u0301"), JsonDuplicateDecisionKind.Append, false, 6);
        AssertDecision(tracker.RegisterPropertyName("a/b~"), JsonDuplicateDecisionKind.Append, false, 7);
        AssertDecision(tracker.RegisterPropertyName("a/b~"), JsonDuplicateDecisionKind.Append, true, 8);

        Assert.That(tracker.GetCurrentRetainedMemberCount(), Is.EqualTo(9));
    }

    [Test]
    public void NestedObjectsHaveIndependentDuplicateScopes()
    {
        JsonDuplicateTracker tracker = new JsonDuplicateTracker(JsonDuplicateKeyPolicy.KeepFirst);
        tracker.EnterObject();
        AssertDecision(tracker.RegisterPropertyName("same"), JsonDuplicateDecisionKind.Append, false, 0);

        tracker.EnterObject();
        Assert.Multiple(() =>
        {
            Assert.That(tracker.GetObjectDepth(), Is.EqualTo(2));
            AssertDecision(tracker.RegisterPropertyName("same"), JsonDuplicateDecisionKind.Append, false, 0);
            AssertDecision(tracker.RegisterPropertyName("same"), JsonDuplicateDecisionKind.Skip, true, 0);
            Assert.That(tracker.ExitObject(), Is.EqualTo(1));
        });

        Assert.Multiple(() =>
        {
            Assert.That(tracker.GetObjectDepth(), Is.EqualTo(1));
            AssertDecision(tracker.RegisterPropertyName("same"), JsonDuplicateDecisionKind.Skip, true, 0);
            Assert.That(tracker.GetCurrentRetainedMemberCount(), Is.EqualTo(1));
        });

        tracker.EnterObject();
        AssertDecision(tracker.RegisterPropertyName("same"), JsonDuplicateDecisionKind.Append, false, 0);
    }

    [Test]
    public void ResetClearsAllScopesWithoutChangingThePolicy()
    {
        JsonDuplicateTracker tracker = new JsonDuplicateTracker(JsonDuplicateKeyPolicy.KeepLast);
        tracker.EnterObject();
        tracker.RegisterPropertyName("outer");
        tracker.EnterObject();
        tracker.RegisterPropertyName("inner");

        tracker.ResetDuplicates();

        Assert.Multiple(() =>
        {
            Assert.That(tracker.GetObjectDepth(), Is.Zero);
            Assert.That(tracker.GetPolicy(), Is.EqualTo(JsonDuplicateKeyPolicy.KeepLast));
            Assert.Throws<InvalidOperationException>(() => tracker.GetCurrentRetainedMemberCount());
            Assert.Throws<InvalidOperationException>(() => tracker.ExitObject());
        });

        tracker.EnterObject();
        AssertDecision(tracker.RegisterPropertyName("outer"), JsonDuplicateDecisionKind.Append, false, 0);
    }

    [Test]
    public void UndefinedPolicyIsRejected()
    {
        JsonParseException exception = Assert.Throws<JsonParseException>(
            () => new JsonDuplicateTracker((JsonDuplicateKeyPolicy)99))
            ?? throw new InvalidOperationException("Expected JsonParseException.");

        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
    }

    static void AssertDecision(
        JsonDuplicateDecision decision,
        JsonDuplicateDecisionKind expectedKind,
        bool expectedDuplicate,
        int expectedMemberIndex)
    {
        Assert.Multiple(() =>
        {
            Assert.That(decision.Kind, Is.EqualTo(expectedKind));
            Assert.That(decision.IsDuplicate, Is.EqualTo(expectedDuplicate));
            Assert.That(decision.MemberIndex, Is.EqualTo(expectedMemberIndex));
        });
    }
}
