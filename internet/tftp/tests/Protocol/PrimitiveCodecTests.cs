using System.Text;
using tftp;

namespace tftp.Tests;

public class PrimitiveCodecTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(4660)]
    [TestCase(65535)]
    public void BigEndianCodecRoundTripsUInt16Values(int value)
    {
        byte[] buffer = new byte[] { 170, 170, 170, 170 };

        BigEndianCodec.WriteUInt16(buffer.AsSpan(), 1, (ushort)value);
        ushort decoded = BigEndianCodec.ReadUInt16(buffer.AsSpan(), 1);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(value));
            Assert.That(buffer[0], Is.EqualTo(170));
            Assert.That(buffer[1], Is.EqualTo((value >> 8) & 255));
            Assert.That(buffer[2], Is.EqualTo(value & 255));
            Assert.That(buffer[3], Is.EqualTo(170));
        });
    }

    [Test]
    public void BigEndianCodecRejectsInvalidReadBounds()
    {
        byte[] buffer = new byte[] { 1, 2 };

        TftpProtocolException negative = Assert.Throws<TftpProtocolException>(
            () => BigEndianCodec.ReadUInt16(buffer.AsSpan(), -1));
        TftpProtocolException shortBuffer = Assert.Throws<TftpProtocolException>(
            () => BigEndianCodec.ReadUInt16(buffer.AsSpan(), 1));
        TftpProtocolException empty = Assert.Throws<TftpProtocolException>(
            () => BigEndianCodec.ReadUInt16(ReadOnlySpan<byte>.Empty, 0));

        Assert.Multiple(() =>
        {
            Assert.That(negative.HasPacketOffset, Is.False);
            Assert.That(shortBuffer.HasPacketOffset, Is.True);
            Assert.That(shortBuffer.PacketOffset, Is.EqualTo(1));
            Assert.That(empty.PacketOffset, Is.Zero);
        });
    }

    [Test]
    public void BigEndianCodecRejectsInvalidWriteBoundsWithoutMutation()
    {
        byte[] buffer = new byte[] { 9, 9 };

        TftpProtocolException negative = Assert.Throws<TftpProtocolException>(
            () => BigEndianCodec.WriteUInt16(buffer.AsSpan(), -1, 1));
        TftpProtocolException shortBuffer = Assert.Throws<TftpProtocolException>(
            () => BigEndianCodec.WriteUInt16(buffer.AsSpan(), 1, 1));
        TftpProtocolException empty = Assert.Throws<TftpProtocolException>(
            () => BigEndianCodec.WriteUInt16(Span<byte>.Empty, 0, 1));

        Assert.Multiple(() =>
        {
            Assert.That(negative.HasPacketOffset, Is.False);
            Assert.That(shortBuffer.HasPacketOffset, Is.True);
            Assert.That(shortBuffer.PacketOffset, Is.EqualTo(1));
            Assert.That(empty.PacketOffset, Is.Zero);
            Assert.That(buffer, Is.EqualTo(new byte[] { 9, 9 }));
        });
    }

    [Test]
    public void AsciiReaderReturnsValueAndNextOffset()
    {
        byte[] source = new byte[] { 99, 97, 116, 0, 88, 89 };

        ZeroTerminatedAsciiReadResult result = ZeroTerminatedAscii.ReadField(
            source.AsSpan(),
            0,
            3,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo("cat"));
            Assert.That(result.NextOffset, Is.EqualTo(4));
        });
    }

    [Test]
    public void AsciiReaderSupportsOffsetsAndAllowedEmptyFields()
    {
        byte[] source = new byte[] { 255, 0, 65, 0 };

        ZeroTerminatedAsciiReadResult empty = ZeroTerminatedAscii.ReadField(
            source.AsSpan(),
            1,
            0,
            true);
        ZeroTerminatedAsciiReadResult value = ZeroTerminatedAscii.ReadField(
            source.AsSpan(),
            empty.NextOffset,
            1,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Value, Is.Empty);
            Assert.That(empty.NextOffset, Is.EqualTo(2));
            Assert.That(value.Value, Is.EqualTo("A"));
            Assert.That(value.NextOffset, Is.EqualTo(4));
        });
    }

    [Test]
    public void AsciiReaderRejectsMissingAndOverlongFields()
    {
        byte[] missingTerminator = Encoding.ASCII.GetBytes("abc");
        byte[] overlong = new byte[] { 97, 98, 99, 0 };

        TftpProtocolException missing = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.ReadField(missingTerminator.AsSpan(), 0, 3, false));
        TftpProtocolException tooLong = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.ReadField(overlong.AsSpan(), 0, 2, false));

        Assert.Multiple(() =>
        {
            Assert.That(missing.Message, Does.Contain("terminator"));
            Assert.That(missing.PacketOffset, Is.Zero);
            Assert.That(tooLong.Message, Does.Contain("maximum length"));
            Assert.That(tooLong.PacketOffset, Is.Zero);
        });
    }

    [Test]
    public void AsciiReaderRejectsEmptyRequiredAndNonAsciiFields()
    {
        byte[] empty = new byte[] { 0 };
        byte[] nonAscii = new byte[] { 65, 128, 0 };

        TftpProtocolException emptyFailure = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.ReadField(empty.AsSpan(), 0, 4, false));
        TftpProtocolException asciiFailure = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.ReadField(nonAscii.AsSpan(), 0, 4, false));

        Assert.Multiple(() =>
        {
            Assert.That(emptyFailure.Message, Does.Contain("empty"));
            Assert.That(asciiFailure.Message, Does.Contain("non-ASCII"));
            Assert.That(asciiFailure.PacketOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void AsciiReaderRejectsInvalidBounds()
    {
        byte[] source = new byte[] { 65, 0 };

        TftpProtocolException negativeOffset = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.ReadField(source.AsSpan(), -1, 1, false));
        TftpProtocolException excessiveOffset = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.ReadField(source.AsSpan(), 3, 1, false));
        TftpProtocolException negativeMaximum = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.ReadField(source.AsSpan(), 0, -1, false));
        TftpProtocolException endOffset = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.ReadField(source.AsSpan(), source.Length, 1, false));

        Assert.Multiple(() =>
        {
            Assert.That(negativeOffset.HasPacketOffset, Is.False);
            Assert.That(excessiveOffset.PacketOffset, Is.EqualTo(3));
            Assert.That(negativeMaximum.HasPacketOffset, Is.False);
            Assert.That(endOffset.Message, Does.Contain("terminator"));
        });
    }

    [Test]
    public void AsciiWriterWritesTerminatorAndReturnsNextOffset()
    {
        byte[] destination = new byte[] { 9, 9, 9, 9, 9, 9 };

        int nextOffset = ZeroTerminatedAscii.WriteField(
            "cat",
            destination.AsSpan(),
            1,
            3,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(nextOffset, Is.EqualTo(5));
            Assert.That(destination, Is.EqualTo(new byte[] { 9, 99, 97, 116, 0, 9 }));
            Assert.That(ZeroTerminatedAscii.GetEncodedLength("cat", 3, false), Is.EqualTo(4));
        });
    }

    [Test]
    public void AsciiWriterSupportsAllowedEmptyFields()
    {
        byte[] destination = new byte[] { 7, 7 };

        int nextOffset = ZeroTerminatedAscii.WriteField(
            "",
            destination.AsSpan(),
            1,
            0,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(nextOffset, Is.EqualTo(2));
            Assert.That(destination, Is.EqualTo(new byte[] { 7, 0 }));
            Assert.That(ZeroTerminatedAscii.GetEncodedLength("", 0, true), Is.EqualTo(1));
        });
    }

    [Test]
    public void AsciiWriterRejectsInvalidText()
    {
        TftpProtocolException nullValue = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.GetEncodedLength(null!, 5, true));
        TftpProtocolException empty = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.GetEncodedLength("", 5, false));
        TftpProtocolException embeddedNull = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.GetEncodedLength("a\0b", 5, false));
        TftpProtocolException nonAscii = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.GetEncodedLength("café", 5, false));
        TftpProtocolException tooLong = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.GetEncodedLength("abc", 2, false));
        TftpProtocolException negativeMaximum = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.GetEncodedLength("a", -1, false));

        Assert.Multiple(() =>
        {
            Assert.That(nullValue.Message, Does.Contain("null"));
            Assert.That(empty.Message, Does.Contain("empty"));
            Assert.That(embeddedNull.Message, Does.Contain("NUL"));
            Assert.That(nonAscii.Message, Does.Contain("non-ASCII"));
            Assert.That(tooLong.Message, Does.Contain("maximum length"));
            Assert.That(negativeMaximum.Message, Does.Contain("cannot be negative"));
        });
    }

    [Test]
    public void AsciiWriterRejectsInvalidDestinationBoundsWithoutMutation()
    {
        byte[] destination = new byte[] { 9, 9, 9 };

        TftpProtocolException negativeOffset = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.WriteField("a", destination.AsSpan(), -1, 1, false));
        TftpProtocolException shortDestination = Assert.Throws<TftpProtocolException>(
            () => ZeroTerminatedAscii.WriteField("abc", destination.AsSpan(), 0, 3, false));

        Assert.Multiple(() =>
        {
            Assert.That(negativeOffset.HasPacketOffset, Is.False);
            Assert.That(shortDestination.HasPacketOffset, Is.True);
            Assert.That(shortDestination.PacketOffset, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 9, 9, 9 }));
        });
    }
}
