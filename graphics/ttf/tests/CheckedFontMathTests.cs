using ttf;

namespace ttf.Tests;

public class CheckedFontMathTests
{
    [Test]
    public void SignedAndUnsignedIntReinterpretationPreservesAllBits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                CheckedFontMath.ReinterpretInt32AsUInt32(int.MinValue),
                Is.EqualTo(0x80000000U));
            Assert.That(
                CheckedFontMath.ReinterpretInt32AsUInt32(-1),
                Is.EqualTo(uint.MaxValue));
            Assert.That(
                CheckedFontMath.ReinterpretInt32AsUInt32(int.MaxValue),
                Is.EqualTo(0x7FFFFFFFU));
            Assert.That(
                CheckedFontMath.ReinterpretUInt32AsInt32(0x80000000U),
                Is.EqualTo(int.MinValue));
            Assert.That(
                CheckedFontMath.ReinterpretUInt32AsInt32(uint.MaxValue),
                Is.EqualTo(-1));
            Assert.That(
                CheckedFontMath.ReinterpretUInt32AsInt32(0x7FFFFFFFU),
                Is.EqualTo(int.MaxValue));
        });
    }

    [Test]
    public void IntOffsetAdditionAcceptsBoundaries()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CheckedFontMath.AddOffset(0, 0), Is.EqualTo(0));
            Assert.That(CheckedFontMath.AddOffset(int.MaxValue, 0), Is.EqualTo(int.MaxValue));
            Assert.That(CheckedFontMath.AddOffset(0, int.MaxValue), Is.EqualTo(int.MaxValue));
            Assert.That(CheckedFontMath.AddOffset(1, int.MaxValue - 1), Is.EqualTo(int.MaxValue));
        });
    }

    [Test]
    public void IntOffsetAdditionRejectsNegativeAndOverflowingInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => CheckedFontMath.AddOffset(-1, 0));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.AddOffset(0, -1));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.AddOffset(int.MaxValue, 1));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.AddOffset(1, int.MaxValue));
        });
    }

    [Test]
    public void LongOffsetAdditionAcceptsBoundaries()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CheckedFontMath.AddOffset(0L, 0L), Is.EqualTo(0L));
            Assert.That(CheckedFontMath.AddOffset(long.MaxValue, 0L), Is.EqualTo(long.MaxValue));
            Assert.That(CheckedFontMath.AddOffset(1L, long.MaxValue - 1), Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void LongOffsetAdditionRejectsNegativeAndOverflowingInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => CheckedFontMath.AddOffset(-1L, 0L));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.AddOffset(0L, -1L));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.AddOffset(long.MaxValue, 1L));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.AddOffset(1L, long.MaxValue));
        });
    }

    [Test]
    public void CountAdditionAcceptsBoundariesAndRejectsOverflow()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CheckedFontMath.AddCounts(0, 0), Is.EqualTo(0));
            Assert.That(CheckedFontMath.AddCounts(1, int.MaxValue - 1), Is.EqualTo(int.MaxValue));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.AddCounts(-1, 0));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.AddCounts(0, -1));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.AddCounts(int.MaxValue, 1));
        });
    }

    [Test]
    public void CountMultiplicationAcceptsBoundariesAndZero()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CheckedFontMath.MultiplyCount(0, int.MaxValue), Is.EqualTo(0));
            Assert.That(CheckedFontMath.MultiplyCount(int.MaxValue, 0), Is.EqualTo(0));
            Assert.That(CheckedFontMath.MultiplyCount(int.MaxValue, 1), Is.EqualTo(int.MaxValue));
            Assert.That(CheckedFontMath.MultiplyCount(1, int.MaxValue), Is.EqualTo(int.MaxValue));
            Assert.That(CheckedFontMath.MultiplyCount(46340, 46340), Is.EqualTo(2147395600));
        });
    }

    [Test]
    public void CountMultiplicationRejectsNegativeAndOverflowingInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => CheckedFontMath.MultiplyCount(-1, 0));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.MultiplyCount(0, -1));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.MultiplyCount(int.MaxValue, 2));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.MultiplyCount(46341, 46341));
        });
    }

    [Test]
    public void RangeEndChecksProductAndAddition()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CheckedFontMath.GetRangeEnd(10, 3, 4), Is.EqualTo(22));
            Assert.That(CheckedFontMath.GetRangeEnd(int.MaxValue, 0, int.MaxValue), Is.EqualTo(int.MaxValue));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.GetRangeEnd(1, int.MaxValue, 2));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.GetRangeEnd(int.MaxValue, 1, 1));
        });
    }

    [TestCase(0, 4, 0)]
    [TestCase(1, 4, 4)]
    [TestCase(2, 4, 4)]
    [TestCase(3, 4, 4)]
    [TestCase(4, 4, 4)]
    [TestCase(5, 4, 8)]
    [TestCase(5, 3, 6)]
    [TestCase(6, 3, 6)]
    [TestCase(int.MaxValue, 1, int.MaxValue)]
    public void AlignRoundsUpWithoutAssumingPowerOfTwo(int value, int alignment, int expected)
    {
        Assert.That(CheckedFontMath.Align(value, alignment), Is.EqualTo(expected));
    }

    [Test]
    public void AlignmentRejectsInvalidAndOverflowingInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => CheckedFontMath.Align(-1, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => CheckedFontMath.Align(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CheckedFontMath.Align(1, -1));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.Align(int.MaxValue, 2));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.Align(int.MaxValue - 1, 4));
        });
    }

    [Test]
    public void FourByteAlignmentAndPaddingAreConsistent()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CheckedFontMath.AlignFour(0), Is.EqualTo(0));
            Assert.That(CheckedFontMath.AlignFour(1), Is.EqualTo(4));
            Assert.That(CheckedFontMath.AlignFour(4), Is.EqualTo(4));
            Assert.That(CheckedFontMath.AlignFour(5), Is.EqualTo(8));
            Assert.That(CheckedFontMath.GetPadding(0, 4), Is.EqualTo(0));
            Assert.That(CheckedFontMath.GetPadding(1, 4), Is.EqualTo(3));
            Assert.That(CheckedFontMath.GetPadding(2, 4), Is.EqualTo(2));
            Assert.That(CheckedFontMath.GetPadding(3, 4), Is.EqualTo(1));
        });
    }

    [TestCase(1000, 1000, 12.0, 12.0)]
    [TestCase(-250, 1000, 12.0, -3.0)]
    [TestCase(500, 2048, 10.0, 2.44140625)]
    [TestCase(int.MaxValue, int.MaxValue, 1.0, 1.0)]
    [TestCase(1234, 1000, 0.0, 0.0)]
    public void ScaleDesignUnitsConvertsCoordinates(
        int designUnits,
        int unitsPerEm,
        double pointSize,
        double expected)
    {
        Assert.That(
            CheckedFontMath.ScaleDesignUnits(designUnits, unitsPerEm, pointSize),
            Is.EqualTo(expected));
    }

    [Test]
    public void ScaleDesignUnitsRejectsInvalidScaleInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => CheckedFontMath.ScaleDesignUnits(1, 0, 12));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.ScaleDesignUnits(1, -1, 12));
            Assert.Throws<ArgumentOutOfRangeException>(() => CheckedFontMath.ScaleDesignUnits(1, 1000, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => CheckedFontMath.ScaleDesignUnits(1, 1000, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => CheckedFontMath.ScaleDesignUnits(1, 1000, double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => CheckedFontMath.ScaleDesignUnits(1, 1000, double.NegativeInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => CheckedFontMath.ScaleDesignUnits(
                int.MaxValue,
                1,
                double.MaxValue));
        });
    }

    [TestCase(0, 1000, 0)]
    [TestCase(500, 1000, 500)]
    [TestCase(-500, 1000, -500)]
    [TestCase(1, 2000, 1)]
    [TestCase(-1, 2000, -1)]
    [TestCase(1, 3000, 0)]
    [TestCase(-1, 3000, 0)]
    [TestCase(3, 2000, 2)]
    [TestCase(-3, 2000, -2)]
    [TestCase(500, 2048, 244)]
    [TestCase(1024, 2048, 500)]
    [TestCase(int.MaxValue, 1000, int.MaxValue)]
    [TestCase(int.MinValue, 1000, int.MinValue)]
    public void PdfUnitRoundingUsesExactMidpointAwayFromZero(
        int designUnits,
        int unitsPerEm,
        int expected)
    {
        Assert.That(CheckedFontMath.RoundPdfUnits(designUnits, unitsPerEm), Is.EqualTo(expected));
    }

    [Test]
    public void PdfUnitRoundingRejectsInvalidOrUnrepresentableResults()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => CheckedFontMath.RoundPdfUnits(1, 0));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.RoundPdfUnits(1, -1));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.RoundPdfUnits(int.MaxValue, 1));
            Assert.Throws<FontFormatException>(() => CheckedFontMath.RoundPdfUnits(int.MinValue, 1));
        });
    }
}
