using email;

namespace email.Tests;

public class EmailRangeTests
{
    [Test]
    public void RangeUsesHalfOpenOffsets()
    {
        EmailRange range = new EmailRange(10, 5);

        Assert.That(range.GetStartOffset(), Is.EqualTo(10));
        Assert.That(range.GetLength(), Is.EqualTo(5));
        Assert.That(range.GetEndOffsetExclusive(), Is.EqualTo(15));
        Assert.That(range.IsEmpty(), Is.False);
        Assert.That(range.ContainsOffset(-1), Is.False);
        Assert.That(range.ContainsOffset(9), Is.False);
        Assert.That(range.ContainsOffset(10), Is.True);
        Assert.That(range.ContainsOffset(14), Is.True);
        Assert.That(range.ContainsOffset(15), Is.False);
    }

    [Test]
    public void RangeRejectsNegativeValuesAndOverflow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailRange(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailRange(0, -1));
        Assert.Throws<OverflowException>(() => new EmailRange(long.MaxValue, 1));

        EmailRange maximum = new EmailRange(long.MaxValue - 1, 1);
        Assert.That(maximum.GetEndOffsetExclusive(), Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void ContainsAndIntersectsHandleEmptyAndAdjacentRanges()
    {
        EmailRange outer = new EmailRange(10, 10);
        EmailRange inner = new EmailRange(12, 3);
        EmailRange emptyAtEnd = new EmailRange(20, 0);
        EmailRange adjacent = new EmailRange(20, 2);

        Assert.That(outer.ContainsRange(inner), Is.True);
        Assert.That(outer.ContainsRange(emptyAtEnd), Is.True);
        Assert.That(outer.Intersects(inner), Is.True);
        Assert.That(outer.Intersects(emptyAtEnd), Is.False);
        Assert.That(outer.Intersects(adjacent), Is.False);
        Assert.Throws<ArgumentNullException>(() => outer.ContainsRange(null));
        Assert.Throws<ArgumentNullException>(() => outer.Intersects(null));
    }

    [Test]
    public void SliceIsRelativeAndCannotEscapeParent()
    {
        EmailRange range = new EmailRange(100, 20);
        EmailRange slice = range.Slice(5, 10);
        EmailRange emptyEnd = range.Slice(20, 0);

        Assert.That(slice.EqualsRange(new EmailRange(105, 10)), Is.True);
        Assert.That(emptyEnd.EqualsRange(new EmailRange(120, 0)), Is.True);
        Assert.That(slice.EqualsRange(null!), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => range.Slice(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => range.Slice(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => range.Slice(19, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => range.Slice(21, 0));
    }
}
