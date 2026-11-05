using whois;

namespace whois.Tests;

public class ByteAccumulatorTests
{
    [Test]
    public void StartsEmptyWithoutOverflow()
    {
        ByteAccumulator accumulator = new ByteAccumulator(4);

        Assert.That(accumulator.GetCount(), Is.Zero);
        Assert.That(accumulator.HasOverflowed(), Is.False);
        Assert.That(accumulator.GetBytes(), Is.Empty);
    }

    [Test]
    public void AppendsFragmentedSpansInOrder()
    {
        ByteAccumulator accumulator = new ByteAccumulator(8);
        byte[] first = new byte[] { 1, 2, 3 };
        byte[] second = new byte[] { 4, 5 };

        bool firstAccepted = accumulator.Append(first.AsSpan(1, 2));
        bool secondAccepted = accumulator.Append(second);

        Assert.That(firstAccepted, Is.True);
        Assert.That(secondAccepted, Is.True);
        Assert.That(accumulator.GetCount(), Is.EqualTo(4));
        Assert.That(accumulator.GetBytes(), Is.EqualTo(new byte[] { 2, 3, 4, 5 }));
        Assert.That(accumulator.HasOverflowed(), Is.False);
    }

    [Test]
    public void ExactMaximumIsAcceptedWithoutOverflow()
    {
        ByteAccumulator accumulator = new ByteAccumulator(4);

        bool accepted = accumulator.Append(new byte[] { 1, 2, 3, 4 });

        Assert.That(accepted, Is.True);
        Assert.That(accumulator.GetCount(), Is.EqualTo(4));
        Assert.That(accumulator.HasOverflowed(), Is.False);
        Assert.That(accumulator.GetBytes(), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void OverflowRetainsOnlyConfiguredPrefix()
    {
        ByteAccumulator accumulator = new ByteAccumulator(5);
        Assert.That(accumulator.Append(new byte[] { 1, 2, 3 }), Is.True);

        bool accepted = accumulator.Append(new byte[] { 4, 5, 6, 7 });

        Assert.That(accepted, Is.False);
        Assert.That(accumulator.GetCount(), Is.EqualTo(5));
        Assert.That(accumulator.HasOverflowed(), Is.True);
        Assert.That(accumulator.GetBytes(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void LaterAppendsCannotChangeRetainedOverflowPrefix()
    {
        ByteAccumulator accumulator = new ByteAccumulator(3);
        byte[] supplied = new byte[] { 10, 20, 30, 40 };
        Assert.That(accumulator.Append(supplied), Is.False);
        supplied[0] = 99;

        bool accepted = accumulator.Append(new byte[] { 50 });

        Assert.That(accepted, Is.False);
        Assert.That(accumulator.GetCount(), Is.EqualTo(3));
        Assert.That(accumulator.GetBytes(), Is.EqualTo(new byte[] { 10, 20, 30 }));
    }

    [Test]
    public void ReturnedBytesAreAnIndependentCopy()
    {
        ByteAccumulator accumulator = new ByteAccumulator(3);
        accumulator.Append(new byte[] { 1, 2 });
        byte[] first = accumulator.GetBytes();
        first[0] = 9;

        byte[] second = accumulator.GetBytes();

        Assert.That(second, Is.EqualTo(new byte[] { 1, 2 }));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void RejectsNonpositiveMaximum(int maximum)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ByteAccumulator(maximum));
    }
}
