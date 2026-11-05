using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class EciDecoderTests
{
    [TestCase(0)]
    [TestCase(26)]
    [TestCase(127)]
    [TestCase(128)]
    [TestCase(16383)]
    [TestCase(16384)]
    [TestCase(811799)]
    [TestCase(999999)]
    public void RoundTripsEveryDesignatorBoundary(int assignment)
    {
        BitBuffer buffer = new BitBuffer(0);
        EciEncoder.EncodeAssignment(assignment, buffer);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());

        int decoded = EciDecoder.DecodeAssignment(reader);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(assignment));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [TestCase(14)]
    [TestCase(19)]
    [TestCase(36)]
    [TestCase(1799)]
    [TestCase(400000)]
    [TestCase(799997)]
    [TestCase(811800)]
    [TestCase(999999)]
    public void PreservesUnregisteredAssignmentsAndRawDesignatorBytes(int assignment)
    {
        byte[] expectedData = EciEncoder.GetDesignatorBytes(assignment);
        BitReader reader = new BitReader(expectedData, expectedData.Length * 8);

        QrSegment segment = EciDecoder.DecodeControlSegment(reader);

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Eci));
            Assert.That(segment.Data, Is.EqualTo(expectedData));
            Assert.That(segment.Data, Is.Not.SameAs(expectedData));
            Assert.That(segment.EciAssignment, Is.EqualTo(assignment));
            Assert.That(EciRegistry.IsRegistered(assignment), Is.False);
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void DecodesSequentialDesignatorsAtAnUnalignedPosition()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);
        EciEncoder.EncodeAssignment(26, buffer);
        EciEncoder.EncodeAssignment(128, buffer);
        EciEncoder.EncodeAssignment(999999, buffer);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());
        reader.SkipBits(3);

        Assert.Multiple(() =>
        {
            Assert.That(EciDecoder.DecodeAssignment(reader), Is.EqualTo(26));
            Assert.That(EciDecoder.DecodeAssignment(reader), Is.EqualTo(128));
            Assert.That(EciDecoder.DecodeAssignment(reader), Is.EqualTo(999999));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [TestCase(new byte[] { 0x80, 0x7F })]
    [TestCase(new byte[] { 0xC0, 0x00, 0x80 })]
    [TestCase(new byte[] { 0xCF, 0x42, 0x40 })]
    [TestCase(new byte[] { 0xE0 })]
    [TestCase(new byte[] { 0xFF })]
    public void RejectsNonMinimalOutOfRangeAndReservedDesignators(byte[] bytes)
    {
        BitReader reader = new BitReader(bytes, bytes.Length * 8);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            EciDecoder.DecodeAssignment(reader))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
        });
    }

    [TestCase(new byte[] { 0x00 }, 7)]
    [TestCase(new byte[] { 0x80 }, 8)]
    [TestCase(new byte[] { 0x80, 0x00 }, 15)]
    [TestCase(new byte[] { 0xC0 }, 8)]
    [TestCase(new byte[] { 0xC0, 0x40 }, 16)]
    [TestCase(new byte[] { 0xC0, 0x40, 0x00 }, 23)]
    public void RejectsTruncatedDesignatorsBeforeReaderPositionChanges(byte[] bytes, int bitLength)
    {
        BitReader reader = new BitReader(bytes, bitLength);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            EciDecoder.DecodeAssignment(reader))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsNullReader()
    {
        QrInputException exception = Assert.Throws<QrInputException>(() =>
            EciDecoder.DecodeAssignment(null!))!;

        Assert.That(exception.InputName, Is.EqualTo("reader"));
    }

    [TestCase("7F", 127)]
    [TestCase("8080", 128)]
    [TestCase("BFFF", 16383)]
    [TestCase("C04000", 16384)]
    [TestCase("CF423F", 999999)]
    public void ReadsGoldenDesignatorsAtEveryBitOffsetAndLeavesFollowingBitsUntouched(string hex, int assignment)
    {
        byte[] bytes = Convert.FromHexString(hex);
        for (int offset = 0; offset < 8; offset++)
        {
            BitBuffer buffer = new BitBuffer(0);
            buffer.AppendBits(0, offset);
            foreach (byte value in bytes) buffer.AppendBits(value, 8);
            buffer.AppendBits(0b10101, 5);
            BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());
            reader.SkipBits(offset);

            Assert.That(EciDecoder.TryDecodeAssignment(reader), Is.EqualTo(assignment), $"offset {offset}");
            Assert.That(reader.GetPosition(), Is.EqualTo(offset + bytes.Length * 8));
            Assert.That(reader.ReadBits(5), Is.EqualTo(0b10101));
            Assert.That(reader.GetRemainingBitCount(), Is.Zero);
        }
    }

    [TestCase("7F")]
    [TestCase("8080")]
    [TestCase("C04000")]
    public void EveryTruncationRestoresTheStartingPositionForBothDecodeApis(string hex)
    {
        byte[] bytes = Convert.FromHexString(hex);
        for (int offset = 0; offset < 8; offset++)
        {
            BitBuffer buffer = new BitBuffer(0);
            buffer.AppendBits(0, offset);
            foreach (byte value in bytes) buffer.AppendBits(value, 8);
            for (int available = 0; available < bytes.Length * 8; available++)
            {
                BitReader reader = new BitReader(buffer.GetBytes(), offset + available);
                reader.SkipBits(offset);
                Assert.That(EciDecoder.TryDecodeAssignment(reader), Is.EqualTo(-1), $"offset {offset}, available {available}");
                Assert.That(reader.GetPosition(), Is.EqualTo(offset));
                Assert.That(Assert.Throws<QrDecodeException>(() => EciDecoder.DecodeAssignment(reader))!.FailureReason,
                    Is.EqualTo(QrFailureReason.InvalidData));
                Assert.That(reader.GetPosition(), Is.EqualTo(offset));
                Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(available));
            }
        }
    }
}
