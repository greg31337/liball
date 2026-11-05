namespace avif.Tests;

public class StructuredByteIoTests
{
    [Test]
    public void FixedPointValuesUseExpectedRepresentations()
    {
        byte[] buffer = new byte[10];
        ByteWriter writer = new ByteWriter(buffer.Length);

        writer.WriteFixed8_8BigEndian(buffer, -1.5);
        writer.WriteFixed16_16BigEndian(buffer, 1.5);
        writer.WriteFixed2_30BigEndian(buffer, -0.5);

        Assert.That(buffer, Is.EqualTo(new byte[]
        {
            254, 128,
            0, 1, 128, 0,
            224, 0, 0, 0
        }));

        ByteReader reader = new ByteReader(buffer.Length);
        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadFixed8_8BigEndian(buffer), Is.EqualTo(-1.5));
            Assert.That(reader.ReadFixed16_16BigEndian(buffer), Is.EqualTo(1.5));
            Assert.That(reader.ReadFixed2_30BigEndian(buffer), Is.EqualTo(-0.5));
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void InvalidFixedPointValueDoesNotWrite()
    {
        byte[] destination = [204, 204, 204, 204];
        ByteWriter writer = new ByteWriter(destination.Length);

        AvifResultCode code = writer.WriteFixed16_16BigEndian(destination, 32768.0);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(writer.Position, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 204, 204, 204, 204 }));
        });
    }

    [Test]
    public void FourCcUuidAndFractionsRoundTripExactly()
    {
        byte[] uuid =
        [
            0, 1, 2, 3, 4, 5, 6, 7,
            8, 9, 10, 11, 12, 13, 14, 15
        ];
        byte[] buffer = new byte[36];
        ByteWriter writer = new ByteWriter(buffer.Length);

        writer.WriteFourCc(buffer, "av01");
        writer.WriteUuid(buffer, uuid);
        writer.WriteUnsignedFraction32BigEndian(buffer, uint.MaxValue, 3);
        writer.WriteSignedFraction32BigEndian(buffer, -5, 7);

        ByteReader reader = new ByteReader(buffer.Length);
        FourCc? fourCc = reader.ReadFourCc(buffer);
        byte[] decodedUuid = reader.ReadUuid(buffer);
        IntegerFraction? unsignedFraction = reader.ReadUnsignedFraction32BigEndian(buffer);
        IntegerFraction? signedFraction = reader.ReadSignedFraction32BigEndian(buffer);

        Assert.Multiple(() =>
        {
            Assert.That(buffer[0..4], Is.EqualTo(new byte[] { 97, 118, 48, 49 }));
            Assert.That((fourCc ?? throw new InvalidOperationException("Invalid internal state.")).Text, Is.EqualTo("av01"));
            Assert.That(fourCc.Value, Is.EqualTo(1635135537));
            Assert.That(decodedUuid, Is.EqualTo(uuid));
            Assert.That((unsignedFraction ?? throw new InvalidOperationException("Invalid internal state.")).Numerator, Is.EqualTo(uint.MaxValue));
            Assert.That(unsignedFraction.Denominator, Is.EqualTo(3));
            Assert.That((signedFraction ?? throw new InvalidOperationException("Invalid internal state.")).Numerator, Is.EqualTo(-5));
            Assert.That(signedFraction.Denominator, Is.EqualTo(7));
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.Remaining(), Is.Zero);
        });
    }

    [Test]
    public void ZeroDenominatorIsRejectedAtomically()
    {
        byte[] encoded = [0, 0, 0, 1, 0, 0, 0, 0];
        byte[] destination = new byte[8];
        ByteReader reader = new ByteReader(encoded.Length);
        ByteWriter writer = new ByteWriter(destination.Length);

        IntegerFraction? fraction = reader.ReadUnsignedFraction32BigEndian(encoded);
        AvifResultCode code = writer.WriteSignedFraction32BigEndian(destination, 1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(fraction, Is.Null);
            Assert.That(reader.Position, Is.Zero);
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(writer.Position, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[8]));
        });
    }

    [Test]
    public void LittleEndianFractionsRoundTripWithExpectedByteOrder()
    {
        byte[] buffer = new byte[16];
        ByteWriter writer = new ByteWriter(buffer.Length);

        writer.WriteUnsignedFraction32LittleEndian(buffer, 16909060, 5);
        writer.WriteSignedFraction32LittleEndian(buffer, -2, 3);

        Assert.That(buffer[0..8], Is.EqualTo(new byte[]
        {
            4, 3, 2, 1,
            5, 0, 0, 0
        }));

        ByteReader reader = new ByteReader(buffer.Length);
        IntegerFraction? unsignedFraction = reader.ReadUnsignedFraction32LittleEndian(buffer);
        IntegerFraction? signedFraction = reader.ReadSignedFraction32LittleEndian(buffer);

        Assert.Multiple(() =>
        {
            Assert.That((unsignedFraction ?? throw new InvalidOperationException("Invalid internal state.")).Numerator, Is.EqualTo(16909060));
            Assert.That(unsignedFraction.Denominator, Is.EqualTo(5));
            Assert.That((signedFraction ?? throw new InvalidOperationException("Invalid internal state.")).Numerator, Is.EqualTo(-2));
            Assert.That(signedFraction.Denominator, Is.EqualTo(3));
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void InvalidFourCcAndUuidAreRejectedAtomically()
    {
        byte[] destination = new byte[16];
        ByteWriter fourCcWriter = new ByteWriter(destination.Length);
        ByteWriter uuidWriter = new ByteWriter(destination.Length);

        AvifResultCode fourCcCode = fourCcWriter.WriteFourCc(destination, "bad");
        AvifResultCode uuidCode = uuidWriter.WriteUuid(destination, new byte[15]);

        Assert.Multiple(() =>
        {
            Assert.That(fourCcCode, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(fourCcWriter.Position, Is.Zero);
            Assert.That(uuidCode, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(uuidWriter.Position, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[16]));
        });
    }
}
