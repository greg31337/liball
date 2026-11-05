using raw;

namespace raw.Tests;

public class EndianPrimitiveTests
{
    [Test]
    public void ReadersDecodeKnownUnsignedIntegerVectors()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EndianReader.ReadUInt8(new byte[] { 0xFE }), Is.EqualTo(0xFE));
            Assert.That(EndianReader.ReadUInt16(new byte[] { 0x34, 0x12 }, RawByteOrder.LittleEndian), Is.EqualTo(0x1234));
            Assert.That(EndianReader.ReadUInt16(new byte[] { 0x12, 0x34 }, RawByteOrder.BigEndian), Is.EqualTo(0x1234));
            Assert.That(EndianReader.ReadUInt32(new byte[] { 0xEF, 0xCD, 0xAB, 0x89 }, RawByteOrder.LittleEndian), Is.EqualTo(0x89ABCDEFU));
            Assert.That(EndianReader.ReadUInt32(new byte[] { 0x89, 0xAB, 0xCD, 0xEF }, RawByteOrder.BigEndian), Is.EqualTo(0x89ABCDEFU));
            Assert.That(EndianReader.ReadUInt64(new byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }, RawByteOrder.LittleEndian), Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(EndianReader.ReadUInt64(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF }, RawByteOrder.BigEndian), Is.EqualTo(0x0123456789ABCDEFUL));
        });
    }

    [Test]
    public void ReadersDecodeKnownSignedIntegerVectors()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EndianReader.ReadInt8(new byte[] { 0xFF }), Is.EqualTo(-1));
            Assert.That(EndianReader.ReadInt16(new byte[] { 0xFE, 0xFF }, RawByteOrder.LittleEndian), Is.EqualTo(-2));
            Assert.That(EndianReader.ReadInt16(new byte[] { 0xFF, 0xFE }, RawByteOrder.BigEndian), Is.EqualTo(-2));
            Assert.That(EndianReader.ReadInt32(new byte[] { 0xEB, 0x32, 0xA4, 0xF8 }, RawByteOrder.LittleEndian), Is.EqualTo(-123456789));
            Assert.That(EndianReader.ReadInt32(new byte[] { 0xF8, 0xA4, 0x32, 0xEB }, RawByteOrder.BigEndian), Is.EqualTo(-123456789));
            Assert.That(EndianReader.ReadInt64(new byte[] { 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, RawByteOrder.LittleEndian), Is.EqualTo(-2));
            Assert.That(EndianReader.ReadInt64(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFE }, RawByteOrder.BigEndian), Is.EqualTo(-2));
        });
    }

    [Test]
    public void WritersEncodeKnownIntegerVectorsInBothOrders()
    {
        byte[] little = Enumerable.Repeat((byte)0xAA, 8).ToArray();
        byte[] big = Enumerable.Repeat((byte)0xAA, 8).ToArray();

        EndianWriter.WriteUInt64(little, 0x0123456789ABCDEFUL, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt64(big, 0x0123456789ABCDEFUL, RawByteOrder.BigEndian);

        Assert.Multiple(() =>
        {
            Assert.That(little, Is.EqualTo(new byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }));
            Assert.That(big, Is.EqualTo(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF }));
        });

        EndianWriter.WriteInt8(little, -2);
        EndianWriter.WriteUInt8(big, 0x7F);
        EndianWriter.WriteInt16(little.AsSpan(1), -2, RawByteOrder.LittleEndian);
        EndianWriter.WriteInt32(big.AsSpan(1), -123456789, RawByteOrder.BigEndian);

        Assert.Multiple(() =>
        {
            Assert.That(little.Take(3), Is.EqualTo(new byte[] { 0xFE, 0xFE, 0xFF }));
            Assert.That(big.Take(5), Is.EqualTo(new byte[] { 0x7F, 0xF8, 0xA4, 0x32, 0xEB }));
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void EveryMultiByteIntegerPrimitiveRoundTrips(RawByteOrder byteOrder)
    {
        byte[] bytes = new byte[8];

        EndianWriter.WriteUInt16(bytes, 0xFEDC, byteOrder);
        ushort value16 = EndianReader.ReadUInt16(bytes, byteOrder);
        EndianWriter.WriteInt16(bytes, -12345, byteOrder);
        short signed16 = EndianReader.ReadInt16(bytes, byteOrder);
        EndianWriter.WriteUInt32(bytes, 0xFEDCBA98U, byteOrder);
        uint value32 = EndianReader.ReadUInt32(bytes, byteOrder);
        EndianWriter.WriteInt32(bytes, -123456789, byteOrder);
        int signed32 = EndianReader.ReadInt32(bytes, byteOrder);
        EndianWriter.WriteUInt64(bytes, 0xFEDCBA9876543210UL, byteOrder);
        ulong value64 = EndianReader.ReadUInt64(bytes, byteOrder);
        EndianWriter.WriteInt64(bytes, -1234567890123456789L, byteOrder);
        long signed64 = EndianReader.ReadInt64(bytes, byteOrder);

        Assert.Multiple(() =>
        {
            Assert.That(value16, Is.EqualTo(0xFEDC));
            Assert.That(signed16, Is.EqualTo(-12345));
            Assert.That(value32, Is.EqualTo(0xFEDCBA98U));
            Assert.That(signed32, Is.EqualTo(-123456789));
            Assert.That(value64, Is.EqualTo(0xFEDCBA9876543210UL));
            Assert.That(signed64, Is.EqualTo(-1234567890123456789L));
        });
    }

    [Test]
    public void FloatingPointKnownVectorsUseRequestedByteOrder()
    {
        byte[] halfLittle = new byte[2];
        byte[] halfBig = new byte[2];
        byte[] singleLittle = new byte[4];
        byte[] singleBig = new byte[4];
        byte[] doubleLittle = new byte[8];
        byte[] doubleBig = new byte[8];

        EndianWriter.WriteHalf(halfLittle, (Half)1.5, RawByteOrder.LittleEndian);
        EndianWriter.WriteHalf(halfBig, (Half)1.5, RawByteOrder.BigEndian);
        EndianWriter.WriteSingle(singleLittle, -12.5F, RawByteOrder.LittleEndian);
        EndianWriter.WriteSingle(singleBig, -12.5F, RawByteOrder.BigEndian);
        EndianWriter.WriteDouble(doubleLittle, 1.0, RawByteOrder.LittleEndian);
        EndianWriter.WriteDouble(doubleBig, 1.0, RawByteOrder.BigEndian);

        Assert.Multiple(() =>
        {
            Assert.That(halfLittle, Is.EqualTo(new byte[] { 0x00, 0x3E }));
            Assert.That(halfBig, Is.EqualTo(new byte[] { 0x3E, 0x00 }));
            Assert.That(singleLittle, Is.EqualTo(new byte[] { 0x00, 0x00, 0x48, 0xC1 }));
            Assert.That(singleBig, Is.EqualTo(new byte[] { 0xC1, 0x48, 0x00, 0x00 }));
            Assert.That(doubleLittle, Is.EqualTo(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F }));
            Assert.That(doubleBig, Is.EqualTo(new byte[] { 0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }));
            Assert.That(EndianReader.ReadHalf(halfBig, RawByteOrder.BigEndian), Is.EqualTo((Half)1.5));
            Assert.That(EndianReader.ReadSingle(singleLittle, RawByteOrder.LittleEndian), Is.EqualTo(-12.5F));
            Assert.That(EndianReader.ReadDouble(doubleBig, RawByteOrder.BigEndian), Is.EqualTo(1.0));
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void FloatingPointRoundTripsPreserveNanPayloadBits(RawByteOrder byteOrder)
    {
        Half half = BitConverter.UInt16BitsToHalf(0x7E35);
        float single = BitConverter.UInt32BitsToSingle(0x7FC12345U);
        double value = BitConverter.UInt64BitsToDouble(0x7FF8123456789ABCUL);
        byte[] halfBytes = new byte[2];
        byte[] singleBytes = new byte[4];
        byte[] doubleBytes = new byte[8];

        EndianWriter.WriteHalf(halfBytes, half, byteOrder);
        EndianWriter.WriteSingle(singleBytes, single, byteOrder);
        EndianWriter.WriteDouble(doubleBytes, value, byteOrder);

        Assert.Multiple(() =>
        {
            Assert.That(BitConverter.HalfToUInt16Bits(EndianReader.ReadHalf(halfBytes, byteOrder)), Is.EqualTo(0x7E35));
            Assert.That(BitConverter.SingleToUInt32Bits(EndianReader.ReadSingle(singleBytes, byteOrder)), Is.EqualTo(0x7FC12345U));
            Assert.That(BitConverter.DoubleToUInt64Bits(EndianReader.ReadDouble(doubleBytes, byteOrder)), Is.EqualTo(0x7FF8123456789ABCUL));
        });
    }

    [Test]
    public void ReadersReportTruncatedPrimitiveInput()
    {
        RawFormatException? int16 = Assert.Throws<RawFormatException>(() => EndianReader.ReadInt16(new byte[1], RawByteOrder.LittleEndian));
        RawFormatException? uint32 = Assert.Throws<RawFormatException>(() => EndianReader.ReadUInt32(new byte[3], RawByteOrder.BigEndian));
        RawFormatException? value64 = Assert.Throws<RawFormatException>(() => EndianReader.ReadDouble(new byte[7], RawByteOrder.LittleEndian));

        Assert.Multiple(() =>
        {
            Assert.That((int16 ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((uint32 ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((value64 ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
        });
    }

    [Test]
    public void WritersReportShortDestinationsWithoutChangingThem()
    {
        byte[] int16Bytes = new byte[] { 0xAA };
        byte[] uint32Bytes = new byte[] { 0xAA, 0xAA, 0xAA };
        byte[] doubleBytes = Enumerable.Repeat((byte)0xAA, 7).ToArray();

        RawFormatException? int16 = Assert.Throws<RawFormatException>(() => EndianWriter.WriteInt16(int16Bytes, 1, RawByteOrder.LittleEndian));
        RawFormatException? uint32 = Assert.Throws<RawFormatException>(() => EndianWriter.WriteUInt32(uint32Bytes, 1, RawByteOrder.BigEndian));
        RawFormatException? value64 = Assert.Throws<RawFormatException>(() => EndianWriter.WriteDouble(doubleBytes, 1.0, RawByteOrder.LittleEndian));

        Assert.Multiple(() =>
        {
            Assert.That((int16 ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((uint32 ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((value64 ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That(int16Bytes, Is.EqualTo(new byte[] { 0xAA }));
            Assert.That(uint32Bytes, Is.EqualTo(new byte[] { 0xAA, 0xAA, 0xAA }));
            Assert.That(doubleBytes, Is.All.EqualTo(0xAA));
        });
    }

    [Test]
    public void InvalidByteOrderIsRejectedForReadsAndWrites()
    {
        RawByteOrder invalid = (RawByteOrder)99;
        byte[] destination = Enumerable.Repeat((byte)0xAA, 8).ToArray();

        RawFormatException? read = Assert.Throws<RawFormatException>(() => EndianReader.ReadUInt64(new byte[8], invalid));
        RawFormatException? write = Assert.Throws<RawFormatException>(() => EndianWriter.WriteUInt64(destination, 1, invalid));

        Assert.Multiple(() =>
        {
            Assert.That((read ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((write ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That(destination, Is.All.EqualTo(0xAA));
        });
    }
}
