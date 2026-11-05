using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogResultTests
{
    [Test]
    public void Success_CreatesSuccessfulOutcome()
    {
        LogResult result = LogResult.Success();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.IsFailure, Is.False);
        Assert.That(result.IsDropped, Is.False);
        Assert.That(result.IsDiagnostic, Is.False);
        Assert.That(result.ShouldContinue(), Is.True);
        Assert.That(result.Code, Is.EqualTo(""));
        Assert.That(result.Message, Is.EqualTo(""));
    }

    [Test]
    public void Failure_CreatesFailureOutcome()
    {
        LogResult result = LogResult.FailureCode("sink.write", "writer failed");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.IsDropped, Is.False);
        Assert.That(result.IsDiagnostic, Is.False);
        Assert.That(result.ShouldContinue(), Is.False);
        Assert.That(result.Code, Is.EqualTo("sink.write"));
        Assert.That(result.Message, Is.EqualTo("writer failed"));
        Assert.That(result.FailureCount, Is.EqualTo(1));
    }

    [Test]
    public void Dropped_CreatesDroppedOutcomeWithCount()
    {
        LogResult result = LogResult.DroppedCountResult("queue full", 3);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.IsFailure, Is.False);
        Assert.That(result.IsDropped, Is.True);
        Assert.That(result.IsDiagnostic, Is.False);
        Assert.That(result.ShouldContinue(), Is.False);
        Assert.That(result.Message, Is.EqualTo("queue full"));
        Assert.That(result.DroppedCount, Is.EqualTo(3));
    }

    [Test]
    public void Diagnostic_CreatesSuccessfulDiagnosticOutcome()
    {
        LogResult result = LogResult.DiagnosticCode("selflog", "sink lagging");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.IsFailure, Is.False);
        Assert.That(result.IsDropped, Is.False);
        Assert.That(result.IsDiagnostic, Is.True);
        Assert.That(result.ShouldContinue(), Is.True);
        Assert.That(result.Code, Is.EqualTo("selflog"));
        Assert.That(result.Message, Is.EqualTo("sink lagging"));
        Assert.That(result.DiagnosticCount, Is.EqualTo(1));
    }

    [Test]
    public void Constructor_NormalizesNullTextAndNegativeCounts()
    {
        LogResult result = new LogResult(false, true, true, true, null, null, -1, -2, -3);

        Assert.That(result.Code, Is.EqualTo(""));
        Assert.That(result.Message, Is.EqualTo(""));
        Assert.That(result.DroppedCount, Is.EqualTo(0));
        Assert.That(result.FailureCount, Is.EqualTo(0));
        Assert.That(result.DiagnosticCount, Is.EqualTo(0));
    }

    [Test]
    public void Combine_AggregatesCountersAndPreservesFirstDetail()
    {
        LogResult result = LogResult.Combine(
            new LogResult[]
            {
                LogResult.Success(),
                LogResult.DiagnosticCode("diagnostic", "diagnostic message"),
                LogResult.DroppedCountResult("dropped message", 2),
                LogResult.FailureCode("failure", "failure message")
            });

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.IsDropped, Is.True);
        Assert.That(result.IsDiagnostic, Is.True);
        Assert.That(result.Code, Is.EqualTo("diagnostic"));
        Assert.That(result.Message, Is.EqualTo("diagnostic message"));
        Assert.That(result.DroppedCount, Is.EqualTo(2));
        Assert.That(result.FailureCount, Is.EqualTo(1));
        Assert.That(result.DiagnosticCount, Is.EqualTo(1));
    }
}
