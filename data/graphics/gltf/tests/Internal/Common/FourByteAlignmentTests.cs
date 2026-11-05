namespace gltf.Tests.Internal.Common;

[TestFixture]
public class FourByteAlignmentTests
{
    [TestCase(0, true)]
    [TestCase(4, true)]
    [TestCase(6, false)]
    [TestCase(-4, false)]
    public void IsAligned_RecognizesNonNegativeMultiplesOfFour(int value, bool expected)
    {
        Assert.That(FourByteAlignment.IsAligned(value), Is.EqualTo(expected));
    }

    [TestCase(0, 0, 0)]
    [TestCase(1, 3, 4)]
    [TestCase(2, 2, 4)]
    [TestCase(3, 1, 4)]
    [TestCase(4, 0, 4)]
    public void PaddingAndAlignment_ReturnExpectedValues(int value, int expectedPadding, int expectedAlignment)
    {
        Assert.That(FourByteAlignment.GetPadding(value), Is.EqualTo(expectedPadding));
        Assert.That(FourByteAlignment.Align(value), Is.EqualTo(expectedAlignment));
    }

    [Test]
    public void Alignment_RejectsNegativeAndOverflowingValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FourByteAlignment.GetPadding(-1));
        Assert.Throws<OverflowException>(() => FourByteAlignment.Align(int.MaxValue));
    }
}
