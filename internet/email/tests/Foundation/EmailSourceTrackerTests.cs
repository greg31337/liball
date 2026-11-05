using System.Text;
using email;

namespace email.Tests;

public class EmailSourceTrackerTests
{
    [Test]
    public void TrackerCountsCrLfBareLfAndBareCr()
    {
        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWith();

        tracker.Advance(Encoding.ASCII.GetBytes("ab\r\ncd\nef\rg"));

        Assert.That(tracker.GetByteOffset(), Is.EqualTo(11));
        Assert.That(tracker.GetLineNumber(), Is.EqualTo(4));
        Assert.That(tracker.GetColumnNumber(), Is.EqualTo(2));
        EmailSourcePosition position = tracker.GetPosition();
        Assert.That(position.GetByteOffset(), Is.EqualTo(11));
        Assert.That(position.GetLineNumber(), Is.EqualTo(4));
        Assert.That(position.GetColumnNumber(), Is.EqualTo(2));
    }

    [Test]
    public void CrLfRemainsOneLineEndingAcrossChunkBoundary()
    {
        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWith();

        tracker.Advance(new byte[] { 65, 13 });
        Assert.That(tracker.GetByteOffset(), Is.EqualTo(2));
        Assert.That(tracker.GetLineNumber(), Is.EqualTo(2));
        Assert.That(tracker.GetColumnNumber(), Is.EqualTo(1));

        tracker.Advance(new byte[] { 10, 66 });
        Assert.That(tracker.GetByteOffset(), Is.EqualTo(4));
        Assert.That(tracker.GetLineNumber(), Is.EqualTo(2));
        Assert.That(tracker.GetColumnNumber(), Is.EqualTo(2));
    }

    [Test]
    public void NonLfAfterCrStartsInNewLine()
    {
        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWith();

        tracker.Advance(new byte[] { 13 });
        tracker.Advance(new byte[] { 88 });

        Assert.That(tracker.GetLineNumber(), Is.EqualTo(2));
        Assert.That(tracker.GetColumnNumber(), Is.EqualTo(2));
    }

    [Test]
    public void AdvanceIsAtomicWhenAnyCoordinateWouldOverflow()
    {
        EmailSourceTracker offsetTracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(
            new EmailSourcePosition(long.MaxValue - 1, 4, 2));
        Assert.Throws<OverflowException>(() => offsetTracker.Advance(new byte[] { 65, 66 }));
        Assert.That(offsetTracker.GetByteOffset(), Is.EqualTo(long.MaxValue - 1));
        Assert.That(offsetTracker.GetColumnNumber(), Is.EqualTo(2));

        EmailSourceTracker lineTracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(
            new EmailSourcePosition(0, long.MaxValue, 8));
        Assert.Throws<OverflowException>(() => lineTracker.Advance(new byte[] { 10 }));
        Assert.That(lineTracker.GetByteOffset(), Is.Zero);
        Assert.That(lineTracker.GetLineNumber(), Is.EqualTo(long.MaxValue));

        EmailSourceTracker columnTracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(
            new EmailSourcePosition(0, 1, long.MaxValue));
        Assert.Throws<OverflowException>(() => columnTracker.Advance(new byte[] { 65 }));
        Assert.That(columnTracker.GetByteOffset(), Is.Zero);
        Assert.That(columnTracker.GetColumnNumber(), Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void ResetReturnsTrackerToMessageOrigin()
    {
        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWith();
        tracker.Advance(new byte[] { 65, 13 });

        tracker.Reset();

        Assert.That(tracker.GetByteOffset(), Is.Zero);
        Assert.That(tracker.GetLineNumber(), Is.EqualTo(1));
        Assert.That(tracker.GetColumnNumber(), Is.EqualTo(1));
    }

    [Test]
    public void InitialPositionCannotBeNull()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailSourceTrackerOperations().CreateWithInitialPosition(null));
    }
}
