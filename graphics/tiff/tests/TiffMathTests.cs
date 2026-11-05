using System;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffMathTests
{
    [Test]
    public void AddOffsets_ChecksUnsignedOverflow()
    {
        Assert.That(TiffMath.AddOffsets(10, 5), Is.EqualTo(15));
        Assert.Throws<OverflowException>(() => TiffMath.AddOffsets(ulong.MaxValue, 1));
    }

    [Test]
    public void AddByteRangeEnd_RejectsNegativeInputsAndOverflow()
    {
        Assert.That(TiffMath.AddByteRangeEnd(10, 5), Is.EqualTo(15));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.AddByteRangeEnd(-1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.AddByteRangeEnd(1, -5));
        Assert.Throws<OverflowException>(() => TiffMath.AddByteRangeEnd(long.MaxValue, 1));
    }

    [Test]
    public void IsByteRangeWithinLength_ValidatesOffsetsCountsAndLength()
    {
        Assert.That(TiffMath.IsByteRangeWithinLength(2, 3, 5), Is.True);
        Assert.That(TiffMath.IsByteRangeWithinLength(2, 4, 5), Is.False);
        Assert.That(TiffMath.IsByteRangeWithinLength(6, 0, 5), Is.False);
        Assert.That(TiffMath.IsByteRangeWithinLength(-1, 1, 5), Is.False);
        Assert.That(TiffMath.IsByteRangeWithinLength(1, -1, 5), Is.False);
        Assert.That(TiffMath.IsByteRangeWithinLength(1, 1, -5), Is.False);
    }

    [Test]
    public void AlignOffset_RoundsUpToAlignmentAndChecksOverflow()
    {
        Assert.That(TiffMath.AlignOffset(16, 8), Is.EqualTo(16));
        Assert.That(TiffMath.AlignOffset(17, 8), Is.EqualTo(24));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.AlignOffset(17, 0));
        Assert.Throws<OverflowException>(() => TiffMath.AlignOffset(ulong.MaxValue - 1, 8));
    }

    [Test]
    public void ClassicOffsetHelpers_ValidateThirtyTwoBitOffsetRange()
    {
        Assert.That(TiffMath.IsClassicOffset(uint.MaxValue), Is.True);
        Assert.That(TiffMath.IsClassicOffset((ulong)uint.MaxValue + 1UL), Is.False);
        Assert.That(TiffMath.RequireClassicOffset(uint.MaxValue), Is.EqualTo(uint.MaxValue));
        Assert.Throws<OverflowException>(() => TiffMath.RequireClassicOffset((ulong)uint.MaxValue + 1UL));
    }

    [Test]
    public void CountHelpers_CheckNegativeValuesAndOverflow()
    {
        Assert.That(TiffMath.MultiplyCounts(3, 4), Is.EqualTo(12));
        Assert.That(TiffMath.DivideRoundUp(10, 3), Is.EqualTo(4));
        Assert.That(TiffMath.DivideRoundUp(9, 3), Is.EqualTo(3));
        Assert.That(TiffMath.CalculateByteCountFromBits(9), Is.EqualTo(2));
        Assert.That(TiffMath.CalculateValueByteCount(4, 8), Is.EqualTo(32));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.MultiplyCounts(-1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.MultiplyCounts(1, -4));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.DivideRoundUp(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.CalculateValueByteCount(1, 0));
        Assert.Throws<OverflowException>(() => TiffMath.MultiplyCounts(long.MaxValue, 2));
    }

    [Test]
    public void RasterHelpers_CalculatePackedAndWholeByteScanlines()
    {
        Assert.That(TiffMath.CalculateScanlineByteCount(13, 1, 1), Is.EqualTo(2));
        Assert.That(TiffMath.CalculateScanlineByteCount(10, 3, 8), Is.EqualTo(30));
        Assert.That(TiffMath.CalculateScanlineByteCount(10, 1, 16), Is.EqualTo(20));
        Assert.That(TiffMath.CalculateRasterByteCount(13, 5, 1, 1), Is.EqualTo(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.CalculateScanlineByteCount(0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.CalculateScanlineByteCount(1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.CalculateScanlineByteCount(1, 1, 0));
    }

    [Test]
    public void StripHelpers_CalculateCountsRowsAndByteCounts()
    {
        Assert.That(TiffMath.CalculateStripCount(10, 4), Is.EqualTo(3));
        Assert.That(TiffMath.CalculateRowsInStrip(10, 4, 0), Is.EqualTo(4));
        Assert.That(TiffMath.CalculateRowsInStrip(10, 4, 2), Is.EqualTo(2));
        Assert.That(TiffMath.CalculateStripByteCount(10, 10, 3, 8, 4, 0), Is.EqualTo(120));
        Assert.That(TiffMath.CalculateStripByteCount(10, 10, 3, 8, 4, 2), Is.EqualTo(60));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.CalculateStripCount(0, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.CalculateStripCount(10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.CalculateRowsInStrip(10, 4, 3));
    }

    [Test]
    public void TileHelpers_CalculateFullAndEdgeTiles()
    {
        Assert.That(TiffMath.CalculateTileCountAcross(25, 10), Is.EqualTo(3));
        Assert.That(TiffMath.CalculateTileCountDown(12, 5), Is.EqualTo(3));
        Assert.That(TiffMath.CalculateTileCount(25, 12, 10, 5), Is.EqualTo(9));
        Assert.That(TiffMath.CalculateTileStorageByteCount(10, 5, 3, 8), Is.EqualTo(150));
        Assert.That(TiffMath.CalculateTileImageByteCount(25, 12, 10, 5, 3, 8, 0), Is.EqualTo(150));
        Assert.That(TiffMath.CalculateTileImageByteCount(25, 12, 10, 5, 3, 8, 8), Is.EqualTo(30));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.CalculateTileCountAcross(0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.CalculateTileCountDown(10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffMath.CalculateTileImageByteCount(25, 12, 10, 5, 3, 8, 9));
    }
}
