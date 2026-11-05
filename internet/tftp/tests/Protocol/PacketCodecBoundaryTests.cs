using tftp;

namespace tftp.Tests;

public class PacketCodecBoundaryTests
{
    [TestCaseSource(nameof(MinimumLegalPackets))]
    public void MinimumLegalPacketsRoundTripUsingExactSizeSpans(
        TftpPacket packet,
        int expectedLength)
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[expectedLength];

        int encodedLength = codec.GetEncodedLength(packet);
        int written = codec.Encode(packet, encoded.AsSpan());
        TftpPacket decoded = codec.Decode(
            encoded.AsSpan(),
            TftpProtocolValues.MaximumBlockSize);
        byte[] reencoded = new byte[codec.GetEncodedLength(decoded)];
        int reencodedLength = codec.Encode(decoded, reencoded.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(encodedLength, Is.EqualTo(expectedLength));
            Assert.That(written, Is.EqualTo(expectedLength));
            Assert.That(decoded.Opcode, Is.EqualTo(packet.Opcode));
            Assert.That(reencodedLength, Is.EqualTo(expectedLength));
            Assert.That(reencoded, Is.EqualTo(encoded));
        });
    }

    [TestCaseSource(nameof(MinimumLegalPackets))]
    public void EveryPacketTypeRejectsOneByteShortDestinationWithoutMutation(
        TftpPacket packet,
        int expectedLength)
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] destination = Enumerable.Repeat((byte)170, expectedLength - 1).ToArray();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Encode(packet, destination.AsSpan()));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("too small"));
            Assert.That(destination, Is.All.EqualTo(170));
        });
    }

    [TestCaseSource(nameof(MaximumLegalPackets))]
    public void MaximumLegalPacketsRoundTrip(
        TftpPacket packet,
        int expectedLength)
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[expectedLength];

        int encodedLength = codec.GetEncodedLength(packet);
        int written = codec.Encode(packet, encoded.AsSpan());
        TftpPacket decoded = codec.Decode(
            encoded.AsSpan(),
            TftpProtocolValues.MaximumBlockSize);
        byte[] reencoded = new byte[codec.GetEncodedLength(decoded)];
        int reencodedLength = codec.Encode(decoded, reencoded.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(encodedLength, Is.EqualTo(expectedLength));
            Assert.That(written, Is.EqualTo(expectedLength));
            Assert.That(decoded.Opcode, Is.EqualTo(packet.Opcode));
            Assert.That(reencodedLength, Is.EqualTo(expectedLength));
            Assert.That(reencoded, Is.EqualTo(encoded));
        });
    }

    private static IEnumerable<TestCaseData> MinimumLegalPackets()
    {
        yield return new TestCaseData(
            CreateRequest(TftpOpcode.ReadRequest, "f", TftpProtocolValues.MailMode),
            9).SetName("Minimum RRQ");
        yield return new TestCaseData(
            CreateRequest(TftpOpcode.WriteRequest, "f", TftpProtocolValues.MailMode),
            9).SetName("Minimum WRQ");
        yield return new TestCaseData(
            CreateData(Array.Empty<byte>()),
            TftpProtocolValues.DataHeaderLength).SetName("Minimum DATA");
        yield return new TestCaseData(
            CreateAcknowledgement(),
            TftpProtocolValues.AckPacketLength).SetName("Minimum ACK");
        yield return new TestCaseData(
            CreateError(""),
            TftpProtocolValues.DataHeaderLength + 1).SetName("Minimum ERROR");
        yield return new TestCaseData(
            CreateOptionAcknowledgement("x", "1"),
            6).SetName("Minimum OACK");
    }

    private static IEnumerable<TestCaseData> MaximumLegalPackets()
    {
        yield return new TestCaseData(
            CreateRequest(
                TftpOpcode.ReadRequest,
                new string('f', 503),
                TftpProtocolValues.OctetMode),
            TftpProtocolValues.MaximumRequestPacketLength).SetName("Maximum RRQ");
        yield return new TestCaseData(
            CreateRequest(
                TftpOpcode.WriteRequest,
                new string('f', 503),
                TftpProtocolValues.OctetMode),
            TftpProtocolValues.MaximumRequestPacketLength).SetName("Maximum WRQ");
        yield return new TestCaseData(
            CreateData(new byte[TftpProtocolValues.MaximumBlockSize]),
            TftpProtocolValues.MaximumDataPacketLength).SetName("Maximum DATA");
        yield return new TestCaseData(
            CreateAcknowledgement(),
            TftpProtocolValues.AckPacketLength).SetName("Maximum ACK");
        yield return new TestCaseData(
            CreateError(new string('e', TftpProtocolValues.MaximumRequestPacketLength)),
            TftpProtocolValues.DataHeaderLength +
                TftpProtocolValues.MaximumRequestPacketLength +
                1).SetName("Maximum ERROR");
        yield return new TestCaseData(
            CreateOptionAcknowledgement(
                "x",
                new string('v', TftpProtocolValues.MaximumRequestPacketLength - 5)),
            TftpProtocolValues.MaximumRequestPacketLength).SetName("Maximum OACK");
    }

    private static TftpPacket CreateRequest(TftpOpcode opcode, string filename, string mode)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = opcode;
        packet.Filename = filename;
        packet.Mode = mode;
        return packet;
    }

    private static TftpPacket CreateData(byte[] payload)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = 65535;
        packet.Payload = payload;
        return packet;
    }

    private static TftpPacket CreateAcknowledgement()
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Acknowledgement;
        packet.BlockNumber = 65535;
        return packet;
    }

    private static TftpPacket CreateError(string message)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = TftpErrorCode.OptionNegotiationFailed;
        packet.ErrorMessage = message;
        return packet;
    }

    private static TftpPacket CreateOptionAcknowledgement(string name, string value)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.OptionAcknowledgement;
        packet.Options.Add(new TftpOption { Name = name, Value = value });
        return packet;
    }
}
