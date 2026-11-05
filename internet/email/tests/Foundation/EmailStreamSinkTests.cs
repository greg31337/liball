using email;

namespace email.Tests;

public class EmailStreamSinkTests
{
    [Test]
    public void CountingSinkTracksEmptySingleAndSplitCrLfWrites()
    {
        EmailCountingStream counter = new EmailCountingStream();

        counter.Write(ReadOnlySpan<byte>.Empty);
        Assert.That(counter.GetByteCount(), Is.Zero);
        Assert.That(counter.EndsWithCarriageReturn(), Is.False);
        Assert.That(counter.EndsWithCrlf(), Is.False);

        counter.Write(new byte[] { 1, 13 });
        Assert.That(counter.GetByteCount(), Is.EqualTo(2));
        Assert.That(counter.EndsWithCarriageReturn(), Is.True);
        Assert.That(counter.EndsWithCrlf(), Is.False);

        counter.WriteSegment(new byte[] { 9, 10, 9 }, 1, 1);
        Assert.That(counter.GetByteCount(), Is.EqualTo(3));
        Assert.That(counter.EndsWithCarriageReturn(), Is.False);
        Assert.That(counter.EndsWithCrlf(), Is.True);
    }

    [Test]
    public void CountingSinkValidatesEverySegmentBoundary()
    {
        EmailCountingStream counter = new EmailCountingStream();

        Assert.Throws<ArgumentNullException>(() => counter.WriteSegment(null!, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => counter.WriteSegment(new byte[1], -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => counter.WriteSegment(new byte[1], 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => counter.WriteSegment(new byte[1], 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => counter.WriteSegment(new byte[1], 1, 1));
    }
}
