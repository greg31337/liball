using tftp;

namespace tftp.Tests;

public class PacketCodecDataAckErrorTests
{
    [Test]
    public void EncodesDataHeaderBlockAndPayload()
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = 4660;
        packet.Payload = new byte[] { 0, 127, 255 };
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] destination = Enumerable.Repeat((byte)170, 8).ToArray();

        int length = codec.GetEncodedLength(packet);
        int written = codec.Encode(packet, destination.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(length, Is.EqualTo(7));
            Assert.That(written, Is.EqualTo(7));
            Assert.That(destination, Is.EqualTo(new byte[] { 0, 3, 18, 52, 0, 127, 255, 170 }));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(512)]
    [TestCase(TftpProtocolValues.MaximumBlockSize)]
    public void RoundTripsDataPayloadBoundaries(int payloadLength)
    {
        TftpPacket sourcePacket = new TftpPacket();
        sourcePacket.Opcode = TftpOpcode.Data;
        sourcePacket.BlockNumber = 65535;
        sourcePacket.Payload = CreatePayload(payloadLength);
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(sourcePacket)];

        int written = codec.Encode(sourcePacket, encoded.AsSpan());
        TftpPacket decoded = codec.Decode(encoded.AsSpan(), payloadLength);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(TftpProtocolValues.DataHeaderLength + payloadLength));
            Assert.That(decoded.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(decoded.BlockNumber, Is.EqualTo(65535));
            Assert.That(decoded.Payload, Is.EqualTo(sourcePacket.Payload));
            Assert.That(decoded.Payload, Is.Not.SameAs(sourcePacket.Payload));
        });
    }

    [Test]
    public void DecodedDataPayloadDoesNotAliasSourceBuffer()
    {
        byte[] source = new byte[] { 0, 3, 0, 7, 10, 20, 30 };
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpPacket packet = codec.Decode(source.AsSpan(), 3);
        source[4] = 99;

        Assert.That(packet.Payload, Is.EqualTo(new byte[] { 10, 20, 30 }));
    }

    [Test]
    public void RejectsInvalidDataPayloadsForEncoding()
    {
        TftpPacket nullPayload = new TftpPacket();
        nullPayload.Opcode = TftpOpcode.Data;
        nullPayload.Payload = null!;
        TftpPacket overlongPayload = new TftpPacket();
        overlongPayload.Opcode = TftpOpcode.Data;
        overlongPayload.Payload = new byte[TftpProtocolValues.MaximumBlockSize + 1];
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException nullFailure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(nullPayload));
        TftpProtocolException overlongFailure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(overlongPayload));

        Assert.Multiple(() =>
        {
            Assert.That(nullFailure.Message, Does.Contain("cannot be null"));
            Assert.That(overlongFailure.Message, Does.Contain("maximum block size"));
        });
    }

    [TestCase(-1)]
    [TestCase(TftpProtocolValues.MaximumBlockSize + 1)]
    public void RejectsInvalidConfiguredDataLengths(int maximumDataLength)
    {
        byte[] source = new byte[] { 0, 3, 0, 1 };
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(source.AsSpan(), maximumDataLength));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("outside the protocol range"));
            Assert.That(failure.HasPacketOffset, Is.False);
        });
    }

    [Test]
    public void RejectsDataAboveConfiguredLength()
    {
        byte[] source = new byte[] { 0, 3, 0, 1, 10, 20, 30 };
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(source.AsSpan(), 2));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("configured maximum"));
            Assert.That(failure.HasPacketOffset, Is.True);
            Assert.That(failure.PacketOffset, Is.EqualTo(6));
        });
    }

    [TestCaseSource(nameof(TruncatedDataPackets))]
    public void RejectsTruncatedDataHeaders(byte[] source)
    {
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(source.AsSpan(), TftpProtocolValues.DefaultBlockSize));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("four-byte header"));
            Assert.That(failure.PacketOffset, Is.EqualTo(source.Length));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(4660)]
    [TestCase(65535)]
    public void RoundTripsAcknowledgementBlockNumbers(int blockNumber)
    {
        TftpPacket sourcePacket = new TftpPacket();
        sourcePacket.Opcode = TftpOpcode.Acknowledgement;
        sourcePacket.BlockNumber = (ushort)blockNumber;
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[TftpProtocolValues.AckPacketLength];

        int length = codec.GetEncodedLength(sourcePacket);
        int written = codec.Encode(sourcePacket, encoded.AsSpan());
        TftpPacket decoded = codec.Decode(encoded.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(length, Is.EqualTo(TftpProtocolValues.AckPacketLength));
            Assert.That(written, Is.EqualTo(TftpProtocolValues.AckPacketLength));
            Assert.That(encoded, Is.EqualTo(new byte[]
            {
                0,
                4,
                (byte)(blockNumber >> 8),
                (byte)(blockNumber & 255)
            }));
            Assert.That(decoded.Opcode, Is.EqualTo(TftpOpcode.Acknowledgement));
            Assert.That(decoded.BlockNumber, Is.EqualTo(blockNumber));
            Assert.That(decoded.Payload, Is.Empty);
        });
    }

    [TestCaseSource(nameof(InvalidAcknowledgementPackets))]
    public void RejectsAcknowledgementsThatAreNotExactlyFourBytes(byte[] source, int failureOffset)
    {
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(source.AsSpan(), 0));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("exactly four bytes"));
            Assert.That(failure.HasPacketOffset, Is.True);
            Assert.That(failure.PacketOffset, Is.EqualTo(failureOffset));
        });
    }

    [Test]
    public void EncodesErrorCodeAndMessage()
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = TftpErrorCode.FileNotFound;
        packet.ErrorMessage = "Missing";
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] destination = Enumerable.Repeat((byte)170, 13).ToArray();

        int length = codec.GetEncodedLength(packet);
        int written = codec.Encode(packet, destination.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(length, Is.EqualTo(12));
            Assert.That(written, Is.EqualTo(12));
            Assert.That(destination, Is.EqualTo(new byte[]
            {
                0, 5, 0, 1,
                77, 105, 115, 115, 105, 110, 103, 0,
                170
            }));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    public void RoundTripsStandardErrorCodes(int errorCode)
    {
        TftpPacket sourcePacket = new TftpPacket();
        sourcePacket.Opcode = TftpOpcode.Error;
        sourcePacket.ErrorCode = (TftpErrorCode)errorCode;
        sourcePacket.ErrorMessage = "Protocol failure";
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(sourcePacket)];

        int written = codec.Encode(sourcePacket, encoded.AsSpan());
        TftpPacket decoded = codec.Decode(encoded.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(encoded.Length));
            Assert.That(decoded.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(decoded.ErrorCode, Is.EqualTo((TftpErrorCode)errorCode));
            Assert.That(decoded.ErrorMessage, Is.EqualTo("Protocol failure"));
        });
    }

    [TestCase(-1)]
    [TestCase(9)]
    [TestCase(65536)]
    public void EncodesUnknownErrorCodesWithUndefinedSemantics(int errorCode)
    {
        TftpPacket sourcePacket = new TftpPacket();
        sourcePacket.Opcode = TftpOpcode.Error;
        sourcePacket.ErrorCode = (TftpErrorCode)errorCode;
        sourcePacket.ErrorMessage = "Private error";
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(sourcePacket)];

        codec.Encode(sourcePacket, encoded.AsSpan());
        TftpPacket decoded = codec.Decode(encoded.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(encoded[2], Is.Zero);
            Assert.That(encoded[3], Is.Zero);
            Assert.That(decoded.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(decoded.ErrorMessage, Is.EqualTo("Private error"));
        });
    }

    [Test]
    public void DecodesUnknownWireErrorCodeWithUndefinedSemantics()
    {
        byte[] source = new byte[] { 0, 5, 18, 52, 80, 114, 105, 118, 97, 116, 101, 0 };
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpPacket packet = codec.Decode(source.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(packet.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(packet.ErrorMessage, Is.EqualTo("Private"));
        });
    }

    [TestCase(0)]
    [TestCase(TftpProtocolValues.MaximumRequestPacketLength)]
    public void RoundTripsErrorMessageLengthBoundaries(int messageLength)
    {
        string message = new string('e', messageLength);
        TftpPacket sourcePacket = new TftpPacket();
        sourcePacket.Opcode = TftpOpcode.Error;
        sourcePacket.ErrorCode = TftpErrorCode.Undefined;
        sourcePacket.ErrorMessage = message;
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(sourcePacket)];

        int written = codec.Encode(sourcePacket, encoded.AsSpan());
        TftpPacket decoded = codec.Decode(encoded.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(5 + messageLength));
            Assert.That(encoded[written - 1], Is.Zero);
            Assert.That(decoded.ErrorMessage, Is.EqualTo(message));
        });
    }

    [TestCaseSource(nameof(InvalidErrorMessagesForEncoding))]
    public void RejectsInvalidErrorMessagesForEncoding(string? message, string messagePart)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorMessage = message!;
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(packet));

        Assert.That(failure.Message, Does.Contain(messagePart));
    }

    [TestCaseSource(nameof(MalformedErrorPackets))]
    public void RejectsMalformedErrorPackets(byte[] source, string messagePart, int failureOffset)
    {
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(source.AsSpan(), 0));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain(messagePart));
            Assert.That(failure.HasPacketOffset, Is.True);
            Assert.That(failure.PacketOffset, Is.EqualTo(failureOffset));
        });
    }

    [Test]
    public void RejectsDecodedErrorMessageAboveMaximumLength()
    {
        byte[] source = new byte[6 + TftpProtocolValues.MaximumRequestPacketLength];
        source[1] = 5;
        source.AsSpan(4, TftpProtocolValues.MaximumRequestPacketLength + 1).Fill((byte)'e');
        source[source.Length - 1] = 0;
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(source.AsSpan(), 0));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("maximum length"));
            Assert.That(failure.PacketOffset, Is.EqualTo(4));
        });
    }

    [TestCase(TftpOpcode.Data)]
    [TestCase(TftpOpcode.Acknowledgement)]
    [TestCase(TftpOpcode.Error)]
    public void RejectsSmallDestinationsWithoutMutation(TftpOpcode opcode)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = opcode;
        packet.BlockNumber = 1;
        packet.Payload = new byte[] { 10, 20 };
        TftpPacketCodec codec = new TftpPacketCodec();
        int length = codec.GetEncodedLength(packet);
        byte[] destination = Enumerable.Repeat((byte)170, length - 1).ToArray();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Encode(packet, destination.AsSpan()));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("too small"));
            Assert.That(destination, Is.All.EqualTo(170));
        });
    }

    private static IEnumerable<byte[]> TruncatedDataPackets()
    {
        yield return new byte[] { 0, 3 };
        yield return new byte[] { 0, 3, 0 };
    }

    private static IEnumerable<TestCaseData> InvalidAcknowledgementPackets()
    {
        yield return new TestCaseData(new byte[] { 0, 4 }, 2);
        yield return new TestCaseData(new byte[] { 0, 4, 0 }, 3);
        yield return new TestCaseData(new byte[] { 0, 4, 0, 1, 99 }, 4);
    }

    private static IEnumerable<TestCaseData> InvalidErrorMessagesForEncoding()
    {
        yield return new TestCaseData(null, "null");
        yield return new TestCaseData("bad\0message", "NUL");
        yield return new TestCaseData("caf\u00e9", "non-ASCII");
        yield return new TestCaseData(
            new string('e', TftpProtocolValues.MaximumRequestPacketLength + 1),
            "maximum length");
    }

    private static IEnumerable<TestCaseData> MalformedErrorPackets()
    {
        yield return new TestCaseData(new byte[] { 0, 5 }, "four-byte header", 2);
        yield return new TestCaseData(new byte[] { 0, 5, 0 }, "four-byte header", 3);
        yield return new TestCaseData(new byte[] { 0, 5, 0, 1 }, "terminator", 4);
        yield return new TestCaseData(new byte[] { 0, 5, 0, 1, 65 }, "terminator", 4);
        yield return new TestCaseData(new byte[] { 0, 5, 0, 1, 128, 0 }, "non-ASCII", 4);
        yield return new TestCaseData(new byte[] { 0, 5, 0, 1, 0, 99 }, "after", 5);
        yield return new TestCaseData(new byte[] { 0, 5, 0, 1, 65, 0, 0 }, "after", 6);
    }

    private static byte[] CreatePayload(int length)
    {
        byte[] payload = new byte[length];
        int index = 0;
        while (index < payload.Length)
        {
            payload[index] = (byte)(index % 251);
            index = index + 1;
        }

        return payload;
    }
}
