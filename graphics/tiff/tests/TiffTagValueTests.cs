using System;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffTagValueTests
{
    [Test]
    public void UnsignedScalar_StoresSingleValueAndCalculatesByteCount()
    {
        TiffTagValue value = TiffTagValue.CreateUnsignedScalar(256, TiffDataType.Long, 1024);

        Assert.That(value.TagId, Is.EqualTo(256));
        Assert.That(value.DataType, Is.EqualTo(TiffDataType.Long));
        Assert.That(value.Count, Is.EqualTo(1));
        Assert.That(value.IsScalar(), Is.True);
        Assert.That(value.IsUnsigned(), Is.True);
        Assert.That(value.UnsignedValues, Is.EqualTo(new ulong[] { 1024 }));
        Assert.That(value.GetValueByteCount(), Is.EqualTo(4));
    }

    [Test]
    public void UnsignedArray_DeepCopiesInputValues()
    {
        ulong[] source = new ulong[] { 1, 2, 3 };

        TiffTagValue value = TiffTagValue.CreateUnsignedArray(273, TiffDataType.Long, source);
        source[0] = 99;

        Assert.That(value.Count, Is.EqualTo(3));
        Assert.That(value.UnsignedValues, Is.EqualTo(new ulong[] { 1, 2, 3 }));
        Assert.That(value.GetValueByteCount(), Is.EqualTo(12));
    }

    [Test]
    public void SignedArray_StoresSignedValues()
    {
        TiffTagValue value = TiffTagValue.CreateSignedArray(1, TiffDataType.SLong, new long[] { -1, 2 });

        Assert.That(value.Count, Is.EqualTo(2));
        Assert.That(value.IsSigned(), Is.True);
        Assert.That(value.SignedValues, Is.EqualTo(new long[] { -1, 2 }));
        Assert.That(value.GetValueByteCount(), Is.EqualTo(8));
    }

    [Test]
    public void Ascii_StoresTextAndCountIncludesNulTerminator()
    {
        TiffTagValue value = TiffTagValue.CreateAscii(270, "hello");
        TiffTagValue empty = TiffTagValue.CreateAscii(271, null!);

        Assert.That(value.DataType, Is.EqualTo(TiffDataType.Ascii));
        Assert.That(value.IsAscii(), Is.True);
        Assert.That(value.Text, Is.EqualTo("hello"));
        Assert.That(value.Count, Is.EqualTo(6));
        Assert.That(value.GetValueByteCount(), Is.EqualTo(6));
        Assert.That(empty.Text, Is.EqualTo(""));
        Assert.That(empty.Count, Is.EqualTo(1));
    }

    [Test]
    public void RationalArray_StoresDeepCopiedRationalValues()
    {
        TiffRational[] source = new TiffRational[] { TiffRational.Create(1, 2), TiffRational.Create(3, 4) };

        TiffTagValue value = TiffTagValue.CreateRationalArray(282, TiffDataType.Rational, source);
        source[0].Numerator = 99;

        Assert.That(value.Count, Is.EqualTo(2));
        Assert.That(value.IsRational(), Is.True);
        Assert.That(value.RationalValues[0].Numerator, Is.EqualTo(1));
        Assert.That(value.RationalValues[0].Denominator, Is.EqualTo(2));
        Assert.That(value.RationalValues[1].Numerator, Is.EqualTo(3));
        Assert.That(value.RationalValues[1].Denominator, Is.EqualTo(4));
        Assert.That(value.GetValueByteCount(), Is.EqualTo(16));
    }

    [Test]
    public void SignedRationalScalar_UsesSameRationalContainer()
    {
        TiffTagValue value = TiffTagValue.CreateRationalScalar(1, TiffDataType.SRational, -1, 2);

        Assert.That(value.Count, Is.EqualTo(1));
        Assert.That(value.RationalValues[0].Numerator, Is.EqualTo(-1));
        Assert.That(value.RationalValues[0].Denominator, Is.EqualTo(2));
        Assert.That(value.GetValueByteCount(), Is.EqualTo(8));
    }

    [Test]
    public void FloatingArray_StoresFloatAndDoubleValuesAsDoubles()
    {
        TiffTagValue floatValue = TiffTagValue.CreateFloatingArray(1, TiffDataType.Float, new double[] { 1.5, 2.5 });
        TiffTagValue doubleValue = TiffTagValue.CreateFloatingScalar(2, TiffDataType.Double, 3.5);

        Assert.That(floatValue.Count, Is.EqualTo(2));
        Assert.That(floatValue.IsFloating(), Is.True);
        Assert.That(floatValue.FloatingValues, Is.EqualTo(new double[] { 1.5, 2.5 }));
        Assert.That(floatValue.GetValueByteCount(), Is.EqualTo(8));
        Assert.That(doubleValue.Count, Is.EqualTo(1));
        Assert.That(doubleValue.GetValueByteCount(), Is.EqualTo(8));
    }

    [Test]
    public void RawValue_PreservesTypedCountAndCopiesRawBytes()
    {
        byte[] raw = new byte[] { 1, 2, 3, 4 };

        TiffTagValue value = TiffTagValue.CreateRaw(65000, TiffDataType.Short, 2, raw);
        raw[0] = 99;

        Assert.That(value.IsRaw, Is.True);
        Assert.That(value.Count, Is.EqualTo(2));
        Assert.That(value.GetValueByteCount(), Is.EqualTo(4));
        Assert.That(value.GetRawByteCount(), Is.EqualTo(4));
        Assert.That(value.RawBytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(value.IsUnsigned(), Is.False);
    }

    [Test]
    public void Copy_DeepCopiesAllMutableStorage()
    {
        TiffTagValue value = TiffTagValue.CreateRationalArray(282, TiffDataType.Rational, new TiffRational[] { TiffRational.Create(1, 2) });

        TiffTagValue copy = value.Copy();
        value.RationalValues[0].Numerator = 99;

        Assert.That(copy.TagId, Is.EqualTo(282));
        Assert.That(copy.DataType, Is.EqualTo(TiffDataType.Rational));
        Assert.That(copy.Count, Is.EqualTo(1));
        Assert.That(copy.RationalValues[0].Numerator, Is.EqualTo(1));
        Assert.That(copy.RationalValues[0].Denominator, Is.EqualTo(2));
    }

    [Test]
    public void FactoryMethods_RejectMismatchedDataTypeCategories()
    {
        Assert.Throws<ArgumentException>(() => TiffTagValue.CreateUnsignedScalar(1, TiffDataType.SLong, 1));
        Assert.Throws<ArgumentException>(() => TiffTagValue.CreateSignedScalar(1, TiffDataType.Long, -1));
        Assert.Throws<ArgumentException>(() => TiffTagValue.CreateRationalScalar(1, TiffDataType.Double, 1, 2));
        Assert.Throws<ArgumentException>(() => TiffTagValue.CreateFloatingScalar(1, TiffDataType.Long, 1.0));
        Assert.Throws<ArgumentException>(() => TiffTagValue.CreateRaw(1, TiffDataType.Unknown, 0, new byte[0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => TiffTagValue.CreateRaw(1, TiffDataType.Byte, -1, new byte[0]));
    }

    [Test]
    public void CategoryHelpers_ClassifySupportedStorageKinds()
    {
        Assert.That(TiffTagValue.IsUnsignedDataType(TiffDataType.Byte), Is.True);
        Assert.That(TiffTagValue.IsUnsignedDataType(TiffDataType.Ifd8), Is.True);
        Assert.That(TiffTagValue.IsSignedDataType(TiffDataType.SLong8), Is.True);
        Assert.That(TiffTagValue.IsRationalDataType(TiffDataType.Rational), Is.True);
        Assert.That(TiffTagValue.IsRationalDataType(TiffDataType.SRational), Is.True);
        Assert.That(TiffTagValue.IsFloatingDataType(TiffDataType.Float), Is.True);
        Assert.That(TiffTagValue.IsFloatingDataType(TiffDataType.Double), Is.True);
        Assert.That(TiffTagValue.IsFloatingDataType(TiffDataType.Long), Is.False);
    }
}
