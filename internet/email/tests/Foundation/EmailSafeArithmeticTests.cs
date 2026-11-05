using email;

namespace email.Tests;

public class EmailSafeArithmeticTests
{
    [Test]
    public void AdditionAcceptsRepresentableNonNegativeValues()
    {
        Assert.That(new EmailSafeArithmetic().CanAddNonNegative(10, 20), Is.True);
        Assert.That(new EmailSafeArithmetic().AddNonNegative(10, 20), Is.EqualTo(30));
        Assert.That(new EmailSafeArithmetic().CanAddNonNegative(long.MaxValue, 0), Is.True);
        Assert.That(new EmailSafeArithmetic().AddNonNegative(long.MaxValue, 0), Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void AdditionRejectsNegativeValuesAndOverflow()
    {
        Assert.That(new EmailSafeArithmetic().CanAddNonNegative(-1, 0), Is.False);
        Assert.That(new EmailSafeArithmetic().CanAddNonNegative(0, -1), Is.False);
        Assert.That(new EmailSafeArithmetic().CanAddNonNegative(long.MaxValue, 1), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailSafeArithmetic().AddNonNegative(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailSafeArithmetic().AddNonNegative(0, -1));
        Assert.Throws<OverflowException>(() => new EmailSafeArithmetic().AddNonNegative(long.MaxValue, 1));
    }

    [Test]
    public void MultiplicationHandlesZeroBoundariesAndOverflow()
    {
        Assert.That(new EmailSafeArithmetic().CanMultiplyNonNegative(0, long.MaxValue), Is.True);
        Assert.That(new EmailSafeArithmetic().MultiplyNonNegative(0, long.MaxValue), Is.Zero);
        Assert.That(new EmailSafeArithmetic().CanMultiplyNonNegative(long.MaxValue, 1), Is.True);
        Assert.That(new EmailSafeArithmetic().MultiplyNonNegative(long.MaxValue, 1), Is.EqualTo(long.MaxValue));
        Assert.That(new EmailSafeArithmetic().CanMultiplyNonNegative(long.MaxValue, 2), Is.False);
        Assert.Throws<OverflowException>(() => new EmailSafeArithmetic().MultiplyNonNegative(long.MaxValue, 2));
        Assert.That(new EmailSafeArithmetic().CanMultiplyNonNegative(-1, 2), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailSafeArithmetic().MultiplyNonNegative(1, -1));
    }

    [Test]
    public void CeilingDivisionAvoidsAdditionOverflow()
    {
        Assert.That(new EmailSafeArithmetic().CeilingDivideNonNegative(0, 3), Is.Zero);
        Assert.That(new EmailSafeArithmetic().CeilingDivideNonNegative(6, 3), Is.EqualTo(2));
        Assert.That(new EmailSafeArithmetic().CeilingDivideNonNegative(7, 3), Is.EqualTo(3));
        Assert.That(new EmailSafeArithmetic().CeilingDivideNonNegative(long.MaxValue, 2), Is.EqualTo(4611686018427387904));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailSafeArithmetic().CeilingDivideNonNegative(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailSafeArithmetic().CeilingDivideNonNegative(1, 0));
    }

    [Test]
    public void Int32ConversionIsCheckedAndNonNegative()
    {
        Assert.That(new EmailSafeArithmetic().ToInt32NonNegative(0), Is.Zero);
        Assert.That(new EmailSafeArithmetic().ToInt32NonNegative(int.MaxValue), Is.EqualTo(int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailSafeArithmetic().ToInt32NonNegative(-1));
        Assert.Throws<OverflowException>(() => new EmailSafeArithmetic().ToInt32NonNegative((long)int.MaxValue + 1));
    }
}
