using System.Text;
using tftp;

namespace tftp.Tests;

public class PacketCodecMalformedTests
{
    [TestCaseSource(nameof(TruncatedPackets))]
    public void RejectsEveryPacketTruncationBoundary(byte[] source)
    {
        AssertDecodeFailure(source, TftpProtocolValues.MaximumBlockSize);
    }

    [TestCaseSource(nameof(UnknownOpcodePackets))]
    public void RejectsUnknownOpcodes(byte[] source)
    {
        TftpProtocolException failure = AssertDecodeFailure(source, 0);

        Assert.That(failure.Message, Does.Contain("opcode"));
    }

    [TestCaseSource(nameof(NonAsciiPackets))]
    public void RejectsNonAsciiPacketFields(byte[] source)
    {
        TftpProtocolException failure = AssertDecodeFailure(source, 0);

        Assert.That(failure.Message, Does.Contain("non-ASCII"));
    }

    [TestCaseSource(nameof(EmbeddedTerminatorPackets))]
    public void RejectsEmbeddedTerminatorsThatLeaveMalformedData(byte[] source)
    {
        AssertDecodeFailure(source, 0);
    }

    [TestCaseSource(nameof(EmptyRequiredFieldPackets))]
    public void RejectsEmptyRequiredPacketFields(byte[] source)
    {
        TftpProtocolException failure = AssertDecodeFailure(source, 0);

        Assert.That(failure.Message, Does.Contain("empty"));
    }

    [TestCaseSource(nameof(DuplicateAndOddOptionPackets))]
    public void RejectsDuplicateAndOddOptionLists(byte[] source, string messagePart)
    {
        TftpProtocolException failure = AssertDecodeFailure(source, 0);

        Assert.That(failure.Message, Does.Contain(messagePart));
    }

    [Test]
    public void RejectsOverlongFixedAcknowledgementPacket()
    {
        byte[] source = new byte[] { 0, 4, 0, 1, 99 };

        TftpProtocolException failure = AssertDecodeFailure(source, 0);

        Assert.That(failure.Message, Does.Contain("exactly four bytes"));
    }

    [TestCase(TftpOpcode.ReadRequest)]
    [TestCase(TftpOpcode.WriteRequest)]
    public void RejectsOversizedRequestPackets(TftpOpcode opcode)
    {
        byte[] source = CreatePacket(
            opcode,
            new string('f', 504) + "\0octet\0");

        TftpProtocolException failure = AssertDecodeFailure(source, 0);

        Assert.Multiple(() =>
        {
            Assert.That(source, Has.Length.EqualTo(TftpProtocolValues.MaximumRequestPacketLength + 1));
            Assert.That(failure.Message, Does.Contain("512-byte"));
        });
    }

    [Test]
    public void RejectsDataAboveConfiguredMaximum()
    {
        byte[] source = new byte[] { 0, 3, 0, 1, 99 };

        TftpProtocolException failure = AssertDecodeFailure(source, 0);

        Assert.That(failure.Message, Does.Contain("configured maximum"));
    }

    [Test]
    public void RejectsDataAboveProtocolMaximum()
    {
        byte[] source = new byte[TftpProtocolValues.MaximumDataPacketLength + 1];
        source[1] = (byte)TftpOpcode.Data;

        TftpProtocolException failure = AssertDecodeFailure(
            source,
            TftpProtocolValues.MaximumBlockSize);

        Assert.Multiple(() =>
        {
            Assert.That(source, Has.Length.EqualTo(TftpProtocolValues.MaximumDataPacketLength + 1));
            Assert.That(failure.Message, Does.Contain("configured maximum"));
        });
    }

    private static TftpProtocolException AssertDecodeFailure(
        byte[] source,
        int maximumDataLength)
    {
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(source.AsSpan(), maximumDataLength));
        TftpPacketDecodeResult result = codec.TryDecode(source.AsSpan(), maximumDataLength);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ConsumedLength, Is.Zero);
            Assert.That(result.FailureMessage, Is.EqualTo(failure.Message));
        });
        return failure;
    }

    private static IEnumerable<TestCaseData> TruncatedPackets()
    {
        foreach (TestCaseData packetCase in CompleteMinimumPackets())
        {
            object?[] arguments = packetCase.Arguments;
            string packetName = (string)arguments[0]!;
            byte[] source = (byte[])arguments[1]!;
            int length = 0;
            while (length < source.Length)
            {
                yield return new TestCaseData(source.AsSpan(0, length).ToArray())
                    .SetName(packetName + " truncated at " + length);
                length = length + 1;
            }
        }
    }

    private static IEnumerable<TestCaseData> CompleteMinimumPackets()
    {
        yield return new TestCaseData(
            "RRQ",
            CreatePacket(TftpOpcode.ReadRequest, "f\0mail\0"));
        yield return new TestCaseData(
            "WRQ",
            CreatePacket(TftpOpcode.WriteRequest, "f\0mail\0"));
        yield return new TestCaseData(
            "DATA",
            new byte[] { 0, 3, 0, 1 });
        yield return new TestCaseData(
            "ACK",
            new byte[] { 0, 4, 0, 1 });
        yield return new TestCaseData(
            "ERROR",
            new byte[] { 0, 5, 0, 1, 0 });
        yield return new TestCaseData(
            "OACK",
            CreatePacket(TftpOpcode.OptionAcknowledgement, "x\01\0"));
    }

    private static IEnumerable<byte[]> UnknownOpcodePackets()
    {
        yield return new byte[] { 0, 0 };
        yield return new byte[] { 0, 7 };
        yield return new byte[] { 255, 255 };
    }

    private static IEnumerable<TestCaseData> NonAsciiPackets()
    {
        yield return new TestCaseData(
            new byte[] { 0, 1, 128, 0, 111, 99, 116, 101, 116, 0 })
            .SetName("RRQ non-ASCII filename");
        yield return new TestCaseData(
            new byte[] { 0, 2, 102, 0, 128, 0 })
            .SetName("WRQ non-ASCII mode");
        yield return new TestCaseData(
            new byte[] { 0, 1, 102, 0, 111, 99, 116, 101, 116, 0, 128, 0, 49, 0 })
            .SetName("RRQ non-ASCII option name");
        yield return new TestCaseData(
            new byte[] { 0, 2, 102, 0, 111, 99, 116, 101, 116, 0, 120, 0, 128, 0 })
            .SetName("WRQ non-ASCII option value");
        yield return new TestCaseData(new byte[] { 0, 5, 0, 1, 128, 0 })
            .SetName("ERROR non-ASCII message");
        yield return new TestCaseData(new byte[] { 0, 6, 128, 0, 49, 0 })
            .SetName("OACK non-ASCII option name");
        yield return new TestCaseData(new byte[] { 0, 6, 120, 0, 128, 0 })
            .SetName("OACK non-ASCII option value");
    }

    private static IEnumerable<TestCaseData> EmbeddedTerminatorPackets()
    {
        yield return new TestCaseData(CreatePacket(
            TftpOpcode.ReadRequest,
            "fi\0le\0octet\0")).SetName("RRQ embedded filename terminator");
        yield return new TestCaseData(CreatePacket(
            TftpOpcode.WriteRequest,
            "f\0oc\0tet\0")).SetName("WRQ embedded mode terminator");
        yield return new TestCaseData(new byte[] { 0, 5, 0, 1, 69, 0, 88 })
            .SetName("ERROR embedded message terminator");
        yield return new TestCaseData(CreatePacket(
            TftpOpcode.OptionAcknowledgement,
            "ti\0me\01\0")).SetName("OACK embedded option-name terminator");
        yield return new TestCaseData(CreatePacket(
            TftpOpcode.OptionAcknowledgement,
            "timeout\01\0extra\0")).SetName("OACK embedded option-value terminator");
    }

    private static IEnumerable<TestCaseData> EmptyRequiredFieldPackets()
    {
        yield return new TestCaseData(CreatePacket(
            TftpOpcode.ReadRequest,
            "\0octet\0")).SetName("RRQ empty filename");
        yield return new TestCaseData(CreatePacket(
            TftpOpcode.WriteRequest,
            "f\0\0")).SetName("WRQ empty mode");
        yield return new TestCaseData(CreatePacket(
            TftpOpcode.ReadRequest,
            "f\0octet\0\01\0")).SetName("RRQ empty option name");
        yield return new TestCaseData(CreatePacket(
            TftpOpcode.WriteRequest,
            "f\0octet\0x\0\0")).SetName("WRQ empty option value");
        yield return new TestCaseData(CreatePacket(
            TftpOpcode.OptionAcknowledgement,
            "\01\0")).SetName("OACK empty option name");
        yield return new TestCaseData(CreatePacket(
            TftpOpcode.OptionAcknowledgement,
            "x\0\0")).SetName("OACK empty option value");
    }

    private static IEnumerable<TestCaseData> DuplicateAndOddOptionPackets()
    {
        yield return new TestCaseData(
            CreatePacket(
                TftpOpcode.ReadRequest,
                "f\0octet\0timeout\01\0TIMEOUT\02\0"),
            "duplicate").SetName("RRQ duplicate options");
        yield return new TestCaseData(
            CreatePacket(
                TftpOpcode.WriteRequest,
                "f\0octet\0timeout\01\0TIMEOUT\02\0"),
            "duplicate").SetName("WRQ duplicate options");
        yield return new TestCaseData(
            CreatePacket(TftpOpcode.ReadRequest, "f\0octet\0timeout\0"),
            "without a value").SetName("RRQ odd options");
        yield return new TestCaseData(
            CreatePacket(TftpOpcode.WriteRequest, "f\0octet\0timeout\0"),
            "without a value").SetName("WRQ odd options");
        yield return new TestCaseData(
            CreatePacket(
                TftpOpcode.OptionAcknowledgement,
                "timeout\01\0TIMEOUT\02\0"),
            "duplicate").SetName("OACK duplicate options");
        yield return new TestCaseData(
            CreatePacket(TftpOpcode.OptionAcknowledgement, "timeout\0"),
            "without a value").SetName("OACK odd options");
    }

    private static byte[] CreatePacket(TftpOpcode opcode, string fields)
    {
        byte[] fieldBytes = Encoding.ASCII.GetBytes(fields);
        byte[] packet = new byte[fieldBytes.Length + 2];
        packet[0] = (byte)((int)opcode >> 8);
        packet[1] = (byte)((int)opcode & 255);
        fieldBytes.CopyTo(packet, 2);
        return packet;
    }
}
