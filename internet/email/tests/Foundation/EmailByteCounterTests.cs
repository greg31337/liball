using email;

namespace email.Tests;

public class EmailByteCounterTests
{
    [Test]
    public void CounterAddsLengthsAndResets()
    {
        EmailByteCounter counter = new EmailByteCounterOperations().CreateWithInitialByteCount(5);

        counter.Add(7);
        counter.AddBytes(new byte[] { 1, 2, 3 });

        Assert.That(counter.GetByteCount(), Is.EqualTo(15));
        counter.Reset();
        Assert.That(counter.GetByteCount(), Is.Zero);
    }

    [Test]
    public void CounterDetectsOverflowWithoutChangingValue()
    {
        EmailByteCounter counter = new EmailByteCounterOperations().CreateWithInitialByteCount(long.MaxValue - 1);

        Assert.That(counter.CanAdd(1), Is.True);
        Assert.That(counter.CanAdd(2), Is.False);
        Assert.Throws<OverflowException>(() => counter.Add(2));
        Assert.That(counter.GetByteCount(), Is.EqualTo(long.MaxValue - 1));

        counter.Add(1);
        Assert.That(counter.GetByteCount(), Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void CounterRejectsNegativeCounts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailByteCounterOperations().CreateWithInitialByteCount(-1));
        EmailByteCounter counter = new EmailByteCounterOperations().CreateWith();
        Assert.That(counter.CanAdd(-1), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => counter.Add(-1));
        Assert.That(counter.GetByteCount(), Is.Zero);
    }
}
