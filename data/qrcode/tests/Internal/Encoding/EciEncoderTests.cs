using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class EciEncoderTests
{
    [TestCase(0, 8)]
    [TestCase(127, 8)]
    [TestCase(128, 16)]
    [TestCase(16383, 16)]
    [TestCase(16384, 24)]
    [TestCase(999999, 24)]
    public void CalculatesExactDesignatorBitLengths(int assignment, int expectedBitLength)
    {
        Assert.That(EciEncoder.CalculateBitLength(assignment), Is.EqualTo(expectedBitLength));
    }

    [TestCase(0, new byte[] { 0x00 })]
    [TestCase(26, new byte[] { 0x1A })]
    [TestCase(127, new byte[] { 0x7F })]
    [TestCase(128, new byte[] { 0x80, 0x80 })]
    [TestCase(16383, new byte[] { 0xBF, 0xFF })]
    [TestCase(16384, new byte[] { 0xC0, 0x40, 0x00 })]
    [TestCase(811799, new byte[] { 0xCC, 0x63, 0x17 })]
    [TestCase(999999, new byte[] { 0xCF, 0x42, 0x3F })]
    public void ProducesExactShortestDesignatorBytes(int assignment, byte[] expected)
    {
        Assert.That(EciEncoder.GetDesignatorBytes(assignment), Is.EqualTo(expected));
    }

    [Test]
    public void EncodesAtAnUnalignedPositionWithoutChangingPrefixBits()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);

        EciEncoder.EncodeAssignment(128, buffer);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetBitLength(), Is.EqualTo(19));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xB0, 0x10, 0x00 }));
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
    public void CreatesLosslessControlSegmentsForUnregisteredAssignments(int assignment)
    {
        QrSegment segment = EciEncoder.CreateControlSegment(assignment);

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Eci));
            Assert.That(segment.CharacterCount, Is.EqualTo(0));
            Assert.That(segment.Data, Is.EqualTo(EciEncoder.GetDesignatorBytes(assignment)));
            Assert.That(segment.EciAssignment, Is.EqualTo(assignment));
            Assert.That(EciRegistry.IsRegistered(assignment), Is.False);
        });
    }

    [Test]
    public void RejectsOutOfRangeAssignmentsBeforeChangingDestination()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);

        QrInputException negative = Assert.Throws<QrInputException>(() =>
            EciEncoder.EncodeAssignment(-1, buffer))!;
        QrInputException tooLarge = Assert.Throws<QrInputException>(() =>
            EciEncoder.EncodeAssignment(1000000, buffer))!;

        Assert.Multiple(() =>
        {
            Assert.That(negative.InputName, Is.EqualTo("assignment"));
            Assert.That(tooLarge.InputName, Is.EqualTo("assignment"));
            Assert.That(buffer.GetBitLength(), Is.EqualTo(3));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xA0 }));
        });
    }

    [Test]
    public void RejectsNullDestination()
    {
        QrInputException exception = Assert.Throws<QrInputException>(() =>
            EciEncoder.EncodeAssignment(26, null!))!;

        Assert.That(exception.InputName, Is.EqualTo("destination"));
    }
}
