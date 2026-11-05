using icc;

namespace icc.Tests;

public class IccExtendedNumberCodecTests
{
    [Test]
    public void Float16CodecPreservesFiniteNormalSubnormalAndSignedZeroValues()
    {
        float subnormal = (float)BitConverter.UInt16BitsToHalf(0x0001);
        byte[] destination = new byte[8];
        BigEndianWriter writer = new BigEndianWriter(destination, 10L);
        IccNumberCodec.WriteFloat16(writer, 1.5f);
        IccNumberCodec.WriteFloat16(writer, -2.0f);
        IccNumberCodec.WriteFloat16(writer, subnormal);
        IccNumberCodec.WriteFloat16(writer, -0.0f);
        BigEndianReader reader = new BigEndianReader(destination, 10L);

        Assert.Multiple(() =>
        {
            Assert.That(IccNumberCodec.ReadFloat16(reader), Is.EqualTo(1.5f));
            Assert.That(IccNumberCodec.ReadFloat16(reader), Is.EqualTo(-2.0f));
            Assert.That(IccNumberCodec.ReadFloat16(reader), Is.EqualTo(subnormal));
            Assert.That(BitConverter.SingleToInt32Bits(IccNumberCodec.ReadFloat16(reader)), Is.EqualTo(BitConverter.SingleToInt32Bits(-0.0f)));
            Assert.That(destination, Is.EqualTo(new byte[] { 0x3e, 0x00, 0xc0, 0x00, 0x00, 0x01, 0x80, 0x00 }));
        });
    }

    [TestCase(0x7c00)]
    [TestCase(0xfc00)]
    [TestCase(0x7e00)]
    public void Float16CodecRejectsInfinityAndNan(int bits)
    {
        byte[] encoded = { (byte)(bits >> 8), (byte)bits };
        BigEndianReader reader = new BigEndianReader(encoded, 90L);
        BigEndianWriter writer = new BigEndianWriter(new byte[2], 90L);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.ReadFloat16(reader));
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.WriteFloat16(writer, float.PositiveInfinity));
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.WriteFloat16(writer, 70000.0f));
            Assert.That(writer.GetWritePosition(), Is.Zero);
        });
    }

    [Test]
    public void Float64CodecPreservesFiniteSubnormalAndSignedZeroValues()
    {
        byte[] destination = new byte[24];
        BigEndianWriter writer = new BigEndianWriter(destination, 0L);
        IccNumberCodec.WriteFloat64(writer, 1.5);
        IccNumberCodec.WriteFloat64(writer, double.Epsilon);
        IccNumberCodec.WriteFloat64(writer, -0.0);
        BigEndianReader reader = new BigEndianReader(destination, 0L);

        double first = IccNumberCodec.ReadFloat64(reader);
        double second = IccNumberCodec.ReadFloat64(reader);
        double third = IccNumberCodec.ReadFloat64(reader);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(1.5));
            Assert.That(second, Is.EqualTo(double.Epsilon));
            Assert.That(BitConverter.DoubleToInt64Bits(third), Is.EqualTo(BitConverter.DoubleToInt64Bits(-0.0)));
            Assert.That(destination, Is.EqualTo(new byte[]
            {
                0x3f, 0xf8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
                0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            }));
        });
    }

    [Test]
    public void Float64CodecRejectsInfinityAndNan()
    {
        byte[] infinity = { 0x7f, 0xf0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        byte[] nan = { 0x7f, 0xf8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        BigEndianWriter writer = new BigEndianWriter(new byte[8], 0L);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.ReadFloat64(new BigEndianReader(infinity, 0L)));
            Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.ReadFloat64(new BigEndianReader(nan, 0L)));
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.WriteFloat64(writer, double.NegativeInfinity));
            Assert.That(writer.GetWritePosition(), Is.Zero);
        });
    }

    [Test]
    public void DirectionalNumberCodecsMapDefinedRanges()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IccNumberCodec.EncodeAzimuthNumber(-180.0), Is.Zero);
            Assert.That(IccNumberCodec.EncodeAzimuthNumber(0.0), Is.EqualTo(0.5));
            Assert.That(IccNumberCodec.DecodeAzimuthNumber(1.0), Is.EqualTo(180.0));
            Assert.That(IccNumberCodec.EncodeZenithNumber(45.0), Is.EqualTo(0.5));
            Assert.That(IccNumberCodec.DecodeZenithNumber(1.0), Is.EqualTo(90.0));
            Assert.That(IccNumberCodec.DecodeHorizontalNumber(-1.0), Is.EqualTo(-1.0));
            Assert.That(IccNumberCodec.EncodeVerticalNumber(1.0), Is.EqualTo(1.0));
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.EncodeAzimuthNumber(181.0));
            Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.DecodeZenithNumber(-0.1));
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.EncodeHorizontalNumber(double.NaN));
            Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.DecodeVerticalNumber(1.1));
        });
    }

    [Test]
    public void SpectralRangeUsesTwoFloat16ValuesAndOneUnsignedInteger()
    {
        IccSpectralRangeNumber range = new IccSpectralRangeNumber(1.5f, 2.0f, 31);
        byte[] destination = new byte[6];
        BigEndianWriter writer = new BigEndianWriter(destination, 0L);
        IccNumberCodec.WriteSpectralRange(writer, range);
        IccSpectralRangeNumber decoded = IccNumberCodec.ReadSpectralRange(new BigEndianReader(destination, 0L));

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(new byte[] { 0x3e, 0x00, 0x40, 0x00, 0x00, 0x1f }));
            Assert.That(decoded.StartWavelength, Is.EqualTo(1.5f));
            Assert.That(decoded.EndWavelength, Is.EqualTo(2.0f));
            Assert.That(decoded.Steps, Is.EqualTo(31));
        });
    }

    [Test]
    public void ValueEncodingsUseActualFloatsAndNormalizedIntegers()
    {
        byte[] destination = new byte[9];
        BigEndianWriter writer = new BigEndianWriter(destination, 0L);
        IccNumberCodec.WriteEncodedValue(writer, IccValueEncoding.Float32, 1.5);
        IccNumberCodec.WriteEncodedValue(writer, IccValueEncoding.Float16, -2.0);
        IccNumberCodec.WriteEncodedValue(writer, IccValueEncoding.UInt16, 0.5);
        IccNumberCodec.WriteEncodedValue(writer, IccValueEncoding.UInt8, 0.5);
        BigEndianReader reader = new BigEndianReader(destination, 0L);

        Assert.Multiple(() =>
        {
            Assert.That(IccNumberCodec.ReadEncodedValue(reader, IccValueEncoding.Float32), Is.EqualTo(1.5));
            Assert.That(IccNumberCodec.ReadEncodedValue(reader, IccValueEncoding.Float16), Is.EqualTo(-2.0));
            Assert.That(IccNumberCodec.ReadEncodedValue(reader, IccValueEncoding.UInt16), Is.EqualTo(32768.0 / 65535.0));
            Assert.That(IccNumberCodec.ReadEncodedValue(reader, IccValueEncoding.UInt8), Is.EqualTo(128.0 / 255.0));
            Assert.That(destination, Is.EqualTo(new byte[] { 0x3f, 0xc0, 0x00, 0x00, 0xc0, 0x00, 0x80, 0x00, 0x80 }));
            Assert.That(IccNumberCodec.DecodeValueEncoding(3), Is.EqualTo(IccValueEncoding.UInt8));
            Assert.That(IccNumberCodec.EncodeValueEncoding(IccValueEncoding.Float32), Is.Zero);
            Assert.That(IccNumberCodec.EncodeValueEncoding(IccValueEncoding.UInt8), Is.EqualTo(3));
            Assert.That(IccNumberCodec.DecodeInterpolationHint(0), Is.EqualTo(IccInterpolationHint.None));
            Assert.That(IccNumberCodec.DecodeInterpolationHint(1), Is.EqualTo(IccInterpolationHint.Trilinear));
            Assert.That(IccNumberCodec.DecodeInterpolationHint(2), Is.EqualTo(IccInterpolationHint.Tetrahedral));
            Assert.That(IccNumberCodec.EncodeInterpolationHint(IccInterpolationHint.None), Is.Zero);
            Assert.That(IccNumberCodec.EncodeInterpolationHint(IccInterpolationHint.Tetrahedral), Is.EqualTo(2));
            Assert.That(IccNumberCodec.DecodeSparseMatrixEncoding(4), Is.EqualTo(IccSparseMatrixEncoding.Float32));
            Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.DecodeValueEncoding(4));
            Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.DecodeInterpolationHint(3));
            Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.DecodeSparseMatrixEncoding(0));
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.EncodeValueEncoding((IccValueEncoding)4));
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.EncodeInterpolationHint((IccInterpolationHint)3));
        });
    }

    [Test]
    public void TintArrayPreservesTintMajorSampleOrderAndBoundsAllocation()
    {
        IccTintArrayNumber tintArray = new IccTintArrayNumber(2, 3, IccValueEncoding.UInt8, new double[] { 0.0, 0.25, 0.5, 0.75, 1.0, 2.0 });
        byte[] destination = new byte[6];
        BigEndianWriter writer = new BigEndianWriter(destination, 0L);
        IccNumberCodec.WriteTintArray(writer, tintArray, 6);
        IccTintArrayNumber decoded = IccNumberCodec.ReadTintArray(new BigEndianReader(destination, 0L), 2, 3, IccValueEncoding.UInt8, 6);

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(new byte[] { 0x00, 0x40, 0x80, 0xbf, 0xff, 0xff }));
            Assert.That(decoded.TintCount, Is.EqualTo(2));
            Assert.That(decoded.SampleCount, Is.EqualTo(3));
            Assert.That(decoded.Values, Is.EqualTo(new double[] { 0.0, 64.0 / 255.0, 128.0 / 255.0, 191.0 / 255.0, 1.0, 1.0 }));
            Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.ReadTintArray(new BigEndianReader(destination, 0L), 2, 3, IccValueEncoding.UInt8, 5));
        });
    }

    [Test]
    public void SparseMatrixCodecUsesCompressedRowsAndRelativeCompactPadding()
    {
        IccSparseMatrixNumber matrix = new IccSparseMatrixNumber(
            2,
            4,
            IccSparseMatrixEncoding.UInt8,
            new ushort[] { 0, 2 },
            new ushort[] { 0, 3, 1 },
            new double[] { 0.0, 0.5, 1.0 },
            Array.Empty<byte>(),
            Array.Empty<byte>());
        byte[] destination = { 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9 };
        BigEndianWriter writer = new BigEndianWriter(destination, 0L);
        writer.WriteZeroBytes(3);
        IccNumberCodec.WriteSparseMatrix(writer, matrix, 20);
        IccSparseMatrixNumber decoded = IccNumberCodec.ReadSparseMatrix(new BigEndianReader(destination.AsMemory(3, 20), 3L), IccSparseMatrixEncoding.UInt8, 20);

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x00,
                0x00, 0x02, 0x00, 0x04,
                0x00, 0x00, 0x00, 0x02,
                0x00, 0x03,
                0x00, 0x00, 0x00, 0x03, 0x00, 0x01,
                0x00, 0x80, 0xff, 0x00
            }));
            Assert.That(decoded.Rows, Is.EqualTo(2));
            Assert.That(decoded.Columns, Is.EqualTo(4));
            Assert.That(decoded.RowStartOffsets, Is.EqualTo(new ushort[] { 0, 2 }));
            Assert.That(decoded.ColumnIndices, Is.EqualTo(new ushort[] { 0, 3, 1 }));
            Assert.That(decoded.Values, Is.EqualTo(new double[] { 0.0, 128.0 / 255.0, 1.0 }));
            Assert.That(decoded.IndexPadding, Is.Empty);
            Assert.That(decoded.DataPadding, Is.EqualTo(new byte[] { 0 }));
        });
    }

    [TestCase(2, 2)]
    [TestCase(3, 2)]
    [TestCase(4, 4)]
    public void SparseMatrixCodecSupportsEveryMultiByteValueEncoding(int encodingValue, int elementSize)
    {
        IccSparseMatrixEncoding encoding = (IccSparseMatrixEncoding)encodingValue;
        IccSparseMatrixNumber matrix = new IccSparseMatrixNumber(
            1,
            2,
            encoding,
            new ushort[] { 0 },
            new ushort[] { 1 },
            new double[] { 0.5 },
            Array.Empty<byte>(),
            Array.Empty<byte>());
        int encodedSize = IccNumberCodec.GetSparseMatrixEncodedSize(1, 1, elementSize, 32);
        byte[] destination = new byte[encodedSize];
        IccNumberCodec.WriteSparseMatrix(new BigEndianWriter(destination, 0L), matrix, 32);
        IccSparseMatrixNumber decoded = IccNumberCodec.ReadSparseMatrix(new BigEndianReader(destination, 0L), encoding, 32);

        Assert.Multiple(() =>
        {
            Assert.That(encodedSize, Is.EqualTo(16));
            Assert.That(decoded.Values[0], Is.EqualTo(encoding == IccSparseMatrixEncoding.UInt16 ? 32768.0 / 65535.0 : 0.5));
            Assert.That(decoded.IndexPadding, Is.EqualTo(new byte[] { 0, 0 }));
            Assert.That(decoded.DataPadding.Length, Is.EqualTo(elementSize == 2 ? 2 : 0));
        });
    }

    [Test]
    public void SparseMatrixReaderRetainsNonZeroPaddingForLaterDiagnostics()
    {
        byte[] encoded =
        {
            0x00, 0x01, 0x00, 0x02,
            0x00, 0x00,
            0x00, 0x01,
            0x00, 0x01,
            0xaa, 0xbb,
            0x80,
            0xcc, 0xdd, 0xee
        };

        IccSparseMatrixNumber decoded = IccNumberCodec.ReadSparseMatrix(new BigEndianReader(encoded, 0L), IccSparseMatrixEncoding.UInt8, 16);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Values, Is.EqualTo(new double[] { 128.0 / 255.0 }));
            Assert.That(decoded.IndexPadding, Is.EqualTo(new byte[] { 0xaa, 0xbb }));
            Assert.That(decoded.DataPadding, Is.EqualTo(new byte[] { 0xcc, 0xdd, 0xee }));
        });
    }

    [Test]
    public void SparseMatrixCodecRejectsMalformedRowsAndNonFiniteValuesBeforeWriting()
    {
        IccSparseMatrixNumber badRows = new IccSparseMatrixNumber(
            2,
            2,
            IccSparseMatrixEncoding.Float32,
            new ushort[] { 0, 2 },
            new ushort[] { 1 },
            new double[] { 1.0 },
            Array.Empty<byte>(),
            Array.Empty<byte>());
        IccSparseMatrixNumber badValue = new IccSparseMatrixNumber(
            1,
            1,
            IccSparseMatrixEncoding.Float16,
            new ushort[] { 0 },
            new ushort[] { 0 },
            new double[] { double.NaN },
            Array.Empty<byte>(),
            Array.Empty<byte>());
        byte[] destination = new byte[32];
        BigEndianWriter writer = new BigEndianWriter(destination, 0L);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.WriteSparseMatrix(writer, badRows, 32));
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.WriteSparseMatrix(writer, badValue, 32));
            Assert.That(writer.GetWritePosition(), Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[32]));
        });
    }
}
