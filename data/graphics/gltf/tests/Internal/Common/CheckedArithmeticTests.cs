namespace gltf.Tests.Internal.Common;

[TestFixture]
public class CheckedArithmeticTests
{
    [Test]
    public void AddInt32_ReturnsExactSignedResult()
    {
        Assert.That(CheckedArithmetic.AddInt32(-7, 12), Is.EqualTo(5));
    }

    [TestCase(int.MaxValue, 1)]
    [TestCase(int.MinValue, -1)]
    public void AddInt32_RejectsOverflow(int left, int right)
    {
        Assert.Throws<OverflowException>(() => CheckedArithmetic.AddInt32(left, right));
    }

    [Test]
    public void MultiplyInt32_ReturnsExactSignedResult()
    {
        Assert.That(CheckedArithmetic.MultiplyInt32(-7, 12), Is.EqualTo(-84));
    }

    [TestCase(int.MaxValue, 2)]
    [TestCase(int.MinValue, -1)]
    public void MultiplyInt32_RejectsOverflow(int left, int right)
    {
        Assert.Throws<OverflowException>(() => CheckedArithmetic.MultiplyInt32(left, right));
    }

    [Test]
    public void NonNegativeOperations_RejectNegativeOperands()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CheckedArithmetic.AddNonNegativeInt32(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CheckedArithmetic.AddNonNegativeInt32(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CheckedArithmetic.MultiplyNonNegativeInt32(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CheckedArithmetic.MultiplyNonNegativeInt32(0, -1));
    }

    [Test]
    public void ConvertUInt32ToInt32_HandlesBoundaryAndOverflow()
    {
        Assert.That(CheckedArithmetic.ConvertUInt32ToInt32(int.MaxValue), Is.EqualTo(int.MaxValue));
        Assert.Throws<OverflowException>(() => CheckedArithmetic.ConvertUInt32ToInt32((uint)int.MaxValue + 1U));
    }
}
