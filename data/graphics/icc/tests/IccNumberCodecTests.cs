using icc;

namespace icc.Tests;

public class IccNumberCodecTests
{
    [Test]
    public void FixedPointCodecsDecodeTheirExactRepresentableRanges()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IccNumberCodec.DecodeS15Fixed16(int.MinValue), Is.EqualTo(-32768.0));
            Assert.That(IccNumberCodec.DecodeS15Fixed16(0x00018000), Is.EqualTo(1.5));
            Assert.That(IccNumberCodec.DecodeS15Fixed16(int.MaxValue), Is.EqualTo(32767.9999847412109375));
            Assert.That(IccNumberCodec.DecodeU16Fixed16(uint.MaxValue), Is.EqualTo(65535.9999847412109375));
            Assert.That(IccNumberCodec.DecodeU1Fixed15(0x8000), Is.EqualTo(1.0));
            Assert.That(IccNumberCodec.DecodeU8Fixed8(0x0180), Is.EqualTo(1.5));
        });
    }

    [Test]
    public void FixedPointCodecsRoundHalfAwayFromZeroAndSaturate()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IccNumberCodec.EncodeS15Fixed16(0.5 / 65536.0), Is.EqualTo(1));
            Assert.That(IccNumberCodec.EncodeS15Fixed16(-0.5 / 65536.0), Is.EqualTo(-1));
            Assert.That(IccNumberCodec.EncodeS15Fixed16(-40000.0), Is.EqualTo(int.MinValue));
            Assert.That(IccNumberCodec.EncodeS15Fixed16(40000.0), Is.EqualTo(int.MaxValue));
            Assert.That(IccNumberCodec.EncodeU16Fixed16(-1.0), Is.Zero);
            Assert.That(IccNumberCodec.EncodeU16Fixed16(70000.0), Is.EqualTo(uint.MaxValue));
            Assert.That(IccNumberCodec.EncodeU1Fixed15(3.0), Is.EqualTo(ushort.MaxValue));
            Assert.That(IccNumberCodec.EncodeU8Fixed8(300.0), Is.EqualTo(ushort.MaxValue));
        });
    }

    [Test]
    public void FixedPointCodecsRejectNonFiniteValues()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.EncodeS15Fixed16(double.NaN));
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.EncodeU16Fixed16(double.PositiveInfinity));
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.EncodeU1Fixed15(double.NegativeInfinity));
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.EncodeU8Fixed8(double.NaN));
        });
    }

    [Test]
    public void Float32CodecRoundTripsNormalValuesAndSignedZero()
    {
        byte[] encoded = new byte[12];
        BigEndianWriter writer = new BigEndianWriter(encoded, 20L);
        IccNumberCodec.WriteFloat32(writer, 1.5f);
        IccNumberCodec.WriteFloat32(writer, -2.25f);
        IccNumberCodec.WriteFloat32(writer, -0.0f);
        BigEndianReader reader = new BigEndianReader(encoded, 20L);

        float first = IccNumberCodec.ReadFloat32(reader);
        float second = IccNumberCodec.ReadFloat32(reader);
        float third = IccNumberCodec.ReadFloat32(reader);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(1.5f));
            Assert.That(second, Is.EqualTo(-2.25f));
            Assert.That(BitConverter.SingleToInt32Bits(third), Is.EqualTo(BitConverter.SingleToInt32Bits(-0.0f)));
            Assert.That(encoded, Is.EqualTo(new byte[] { 0x3f, 0xc0, 0x00, 0x00, 0xc0, 0x10, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00 }));
        });
    }

    [TestCase(0x7f800000u)]
    [TestCase(0xff800000u)]
    [TestCase(0x7fc00000u)]
    [TestCase(0x00000001u)]
    [TestCase(0x807fffffu)]
    public void Float32ReaderRejectsForbiddenBitPatterns(uint bits)
    {
        byte[] encoded =
        {
            (byte)(bits >> 24),
            (byte)(bits >> 16),
            (byte)(bits >> 8),
            (byte)bits
        };
        BigEndianReader reader = new BigEndianReader(encoded, 123L);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.ReadFloat32(reader))!;

        Assert.That(exception.Message, Does.Contain("123"));
    }

    [Test]
    public void Float32WriterRejectsForbiddenValuesWithoutWriting()
    {
        byte[] destination = { 1, 2, 3, 4 };
        BigEndianWriter writer = new BigEndianWriter(destination, 456L);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.WriteFloat32(writer, float.NaN));
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.WriteFloat32(writer, float.PositiveInfinity));
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.WriteFloat32(writer, float.Epsilon));
            Assert.That(writer.GetWritePosition(), Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        });
    }

    [Test]
    public void CompositeNumberCodecsRoundTripEveryField()
    {
        byte[] destination = new byte[40];
        BigEndianWriter writer = new BigEndianWriter(destination, 0L);
        IccXyzNumber xyz = new IccXyzNumber
        {
            X = 0.5,
            Y = 1.0,
            Z = -0.25
        };
        IccDateTime dateTime = new IccDateTime
        {
            Year = 2026,
            Month = 9,
            Day = 11,
            Hour = 14,
            Minute = 35,
            Second = 59
        };
        IccPositionNumber position = new IccPositionNumber(0xf0000000, 0x01020304);
        IccResponse16Number response = new IccResponse16Number(0xabcd, 0x1234, -1.5);

        IccNumberCodec.WriteXyzNumber(writer, xyz);
        IccNumberCodec.WriteDateTimeNumber(writer, dateTime);
        IccNumberCodec.WritePositionNumber(writer, position);
        IccNumberCodec.WriteResponse16Number(writer, response);
        BigEndianReader reader = new BigEndianReader(destination, 0L);

        IccXyzNumber decodedXyz = IccNumberCodec.ReadXyzNumber(reader);
        IccDateTime decodedDateTime = IccNumberCodec.ReadDateTimeNumber(reader);
        IccPositionNumber decodedPosition = IccNumberCodec.ReadPositionNumber(reader);
        IccResponse16Number decodedResponse = IccNumberCodec.ReadResponse16Number(reader);

        Assert.Multiple(() =>
        {
            Assert.That(decodedXyz.X, Is.EqualTo(0.5));
            Assert.That(decodedXyz.Y, Is.EqualTo(1.0));
            Assert.That(decodedXyz.Z, Is.EqualTo(-0.25));
            Assert.That(decodedDateTime.Year, Is.EqualTo(2026));
            Assert.That(decodedDateTime.Month, Is.EqualTo(9));
            Assert.That(decodedDateTime.Day, Is.EqualTo(11));
            Assert.That(decodedDateTime.Hour, Is.EqualTo(14));
            Assert.That(decodedDateTime.Minute, Is.EqualTo(35));
            Assert.That(decodedDateTime.Second, Is.EqualTo(59));
            Assert.That(decodedPosition.Offset, Is.EqualTo(0xf0000000));
            Assert.That(decodedPosition.Size, Is.EqualTo(0x01020304));
            Assert.That(decodedResponse.DeviceCode, Is.EqualTo(0xabcd));
            Assert.That(decodedResponse.Reserved, Is.EqualTo(0x1234));
            Assert.That(decodedResponse.Measurement, Is.EqualTo(-1.5));
            Assert.That(reader.GetRemainingCount(), Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x80, 0x00,
                0x00, 0x01, 0x00, 0x00,
                0xff, 0xff, 0xc0, 0x00,
                0x07, 0xea, 0x00, 0x09, 0x00, 0x0b,
                0x00, 0x0e, 0x00, 0x23, 0x00, 0x3b,
                0xf0, 0x00, 0x00, 0x00,
                0x01, 0x02, 0x03, 0x04,
                0xab, 0xcd, 0x12, 0x34,
                0xff, 0xfe, 0x80, 0x00
            }));
        });
    }

    [Test]
    public void CompositeWritesValidateBeforeChangingDestination()
    {
        byte[] destination = new byte[12];
        BigEndianWriter writer = new BigEndianWriter(destination, 0L);
        IccXyzNumber xyz = new IccXyzNumber();
        xyz.X = 1.0;
        xyz.Y = double.NaN;
        xyz.Z = 1.0;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => IccNumberCodec.WriteXyzNumber(writer, xyz));
            Assert.That(writer.GetWritePosition(), Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[12]));
        });
    }

    [Test]
    public void CompositeReadsRequireTheWholeValueBeforeAdvancing()
    {
        BigEndianReader xyzReader = new BigEndianReader(new byte[11], 0L);
        BigEndianReader dateReader = new BigEndianReader(new byte[11], 0L);
        BigEndianReader positionReader = new BigEndianReader(new byte[7], 0L);
        BigEndianReader responseReader = new BigEndianReader(new byte[7], 0L);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.ReadXyzNumber(xyzReader));
            Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.ReadDateTimeNumber(dateReader));
            Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.ReadPositionNumber(positionReader));
            Assert.Throws<IccProfileFormatException>(() => IccNumberCodec.ReadResponse16Number(responseReader));
            Assert.That(xyzReader.GetReadPosition(), Is.Zero);
            Assert.That(dateReader.GetReadPosition(), Is.Zero);
            Assert.That(positionReader.GetReadPosition(), Is.Zero);
            Assert.That(responseReader.GetReadPosition(), Is.Zero);
        });
    }
}
