using System;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffDataTypeInfoTests
{
    [TestCase(TiffDataType.Byte, 1)]
    [TestCase(TiffDataType.Ascii, 1)]
    [TestCase(TiffDataType.SByte, 1)]
    [TestCase(TiffDataType.Undefined, 1)]
    [TestCase(TiffDataType.Short, 2)]
    [TestCase(TiffDataType.SShort, 2)]
    [TestCase(TiffDataType.Long, 4)]
    [TestCase(TiffDataType.SLong, 4)]
    [TestCase(TiffDataType.Float, 4)]
    [TestCase(TiffDataType.Ifd, 4)]
    [TestCase(TiffDataType.Rational, 8)]
    [TestCase(TiffDataType.SRational, 8)]
    [TestCase(TiffDataType.Double, 8)]
    [TestCase(TiffDataType.Long8, 8)]
    [TestCase(TiffDataType.SLong8, 8)]
    [TestCase(TiffDataType.Ifd8, 8)]
    public void GetElementSize_ReturnsTiffFieldTypeSizes(TiffDataType dataType, int expectedSize)
    {
        Assert.That(TiffDataTypeInfo.GetElementSize(dataType), Is.EqualTo(expectedSize));
        Assert.That(TiffDataTypeInfo.GetElementBitCount(dataType), Is.EqualTo(expectedSize * 8));
    }

    [Test]
    public void GetElementSize_RejectsUnknownTypes()
    {
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.Unknown), Is.False);
        Assert.That(TiffDataTypeInfo.IsKnown((TiffDataType)99), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffDataTypeInfo.GetElementSize(TiffDataType.Unknown));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffDataTypeInfo.GetElementSize((TiffDataType)99));
    }

    [Test]
    public void IsKnown_ReturnsTrueForAllSupportedTiffAndBigTiffTypes()
    {
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.Byte), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.Ascii), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.Short), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.Long), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.Rational), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.SByte), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.Undefined), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.SShort), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.SLong), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.SRational), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.Float), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.Double), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.Ifd), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.Long8), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.SLong8), Is.True);
        Assert.That(TiffDataTypeInfo.IsKnown(TiffDataType.Ifd8), Is.True);
    }

    [Test]
    public void CalculateValueByteCount_MultipliesElementSizeAndCount()
    {
        Assert.That(TiffDataTypeInfo.CalculateValueByteCount(TiffDataType.Byte, 4), Is.EqualTo(4));
        Assert.That(TiffDataTypeInfo.CalculateValueByteCount(TiffDataType.Short, 4), Is.EqualTo(8));
        Assert.That(TiffDataTypeInfo.CalculateValueByteCount(TiffDataType.Long, 4), Is.EqualTo(16));
        Assert.That(TiffDataTypeInfo.CalculateValueByteCount(TiffDataType.Rational, 4), Is.EqualTo(32));
        Assert.That(TiffDataTypeInfo.CalculateValueByteCount(TiffDataType.Long8, 4), Is.EqualTo(32));
        Assert.That(TiffDataTypeInfo.CalculateValueByteCount(TiffDataType.Ascii, 0), Is.EqualTo(0));
    }

    [Test]
    public void CalculateValueByteCount_RejectsNegativeCountsAndOverflow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffDataTypeInfo.CalculateValueByteCount(TiffDataType.Byte, -1));
        Assert.Throws<OverflowException>(() => TiffDataTypeInfo.CalculateValueByteCount(TiffDataType.Double, long.MaxValue));
    }

    [Test]
    public void InlineFitHelpers_UseClassicAndBigTiffValueSlotWidths()
    {
        Assert.That(TiffDataTypeInfo.ClassicInlineValueBytes, Is.EqualTo(4));
        Assert.That(TiffDataTypeInfo.BigTiffInlineValueBytes, Is.EqualTo(8));
        Assert.That(TiffDataTypeInfo.FitsClassicInline(TiffDataType.Byte, 4), Is.True);
        Assert.That(TiffDataTypeInfo.FitsClassicInline(TiffDataType.Byte, 5), Is.False);
        Assert.That(TiffDataTypeInfo.FitsClassicInline(TiffDataType.Short, 2), Is.True);
        Assert.That(TiffDataTypeInfo.FitsClassicInline(TiffDataType.Short, 3), Is.False);
        Assert.That(TiffDataTypeInfo.FitsClassicInline(TiffDataType.Long, 1), Is.True);
        Assert.That(TiffDataTypeInfo.FitsClassicInline(TiffDataType.Long, 2), Is.False);
        Assert.That(TiffDataTypeInfo.FitsBigTiffInline(TiffDataType.Long, 2), Is.True);
        Assert.That(TiffDataTypeInfo.FitsBigTiffInline(TiffDataType.Long, 3), Is.False);
        Assert.That(TiffDataTypeInfo.FitsBigTiffInline(TiffDataType.Double, 1), Is.True);
        Assert.That(TiffDataTypeInfo.FitsBigTiffInline(TiffDataType.Double, 2), Is.False);
    }

    [Test]
    public void BigTiffOnlyAndOffsetTypeHelpers_ClassifySpecialTypes()
    {
        Assert.That(TiffDataTypeInfo.IsBigTiffOnly(TiffDataType.Long8), Is.True);
        Assert.That(TiffDataTypeInfo.IsBigTiffOnly(TiffDataType.SLong8), Is.True);
        Assert.That(TiffDataTypeInfo.IsBigTiffOnly(TiffDataType.Ifd8), Is.True);
        Assert.That(TiffDataTypeInfo.IsBigTiffOnly(TiffDataType.Ifd), Is.False);
        Assert.That(TiffDataTypeInfo.IsOffsetDataType(TiffDataType.Ifd), Is.True);
        Assert.That(TiffDataTypeInfo.IsOffsetDataType(TiffDataType.Ifd8), Is.True);
        Assert.That(TiffDataTypeInfo.IsOffsetDataType(TiffDataType.Long), Is.False);
    }
}
