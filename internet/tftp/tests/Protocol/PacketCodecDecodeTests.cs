using System.Text;
using tftp;

namespace tftp.Tests;

public class PacketCodecDecodeTests
{
    [TestCaseSource(nameof(ValidPackets))]
    public void TryDecodeDispatchesEveryPacketType(
        byte[] source,
        int maximumDataLength,
        TftpOpcode expectedOpcode)
    {
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpPacketDecodeResult result = codec.TryDecode(source.AsSpan(), maximumDataLength);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Packet.Opcode, Is.EqualTo(expectedOpcode));
            Assert.That(result.ConsumedLength, Is.EqualTo(source.Length));
            Assert.That(result.FailureMessage, Is.Empty);
        });
    }

    [TestCaseSource(nameof(InvalidPackets))]
    public void TryDecodeReportsMalformedPacketsWithoutConsumption(
        byte[] source,
        int maximumDataLength,
        string messagePart)
    {
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpPacketDecodeResult result = codec.TryDecode(source.AsSpan(), maximumDataLength);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Packet, Is.Not.Null);
            Assert.That(result.ConsumedLength, Is.Zero);
            Assert.That(result.FailureMessage, Does.Contain(messagePart));
        });
    }

    [Test]
    public void TryDecodeReturnsTheDecodedPacketFields()
    {
        byte[] source = CreatePacket(
            TftpOpcode.WriteRequest,
            "upload.bin\0NeTaScIi\0timeout\03\0");
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpPacketDecodeResult result = codec.TryDecode(source.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Packet.Filename, Is.EqualTo("upload.bin"));
            Assert.That(result.Packet.Mode, Is.EqualTo("NeTaScIi"));
            Assert.That(result.Packet.Options, Has.Count.EqualTo(1));
            Assert.That(result.Packet.Options[0].Name, Is.EqualTo("timeout"));
            Assert.That(result.Packet.Options[0].Value, Is.EqualTo("3"));
        });
    }

    [Test]
    public void NonDataPacketsIgnoreTheConfiguredDataLimit()
    {
        byte[] source = new byte[] { 0, 4, 0, 7 };
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpPacket throwingPacket = codec.Decode(source.AsSpan(), -1);
        TftpPacketDecodeResult result = codec.TryDecode(source.AsSpan(), -1);

        Assert.Multiple(() =>
        {
            Assert.That(throwingPacket.BlockNumber, Is.EqualTo(7));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Packet.BlockNumber, Is.EqualTo(7));
        });
    }

    private static IEnumerable<TestCaseData> ValidPackets()
    {
        yield return new TestCaseData(
            CreatePacket(TftpOpcode.ReadRequest, "file\0octet\0"),
            0,
            TftpOpcode.ReadRequest);
        yield return new TestCaseData(
            CreatePacket(TftpOpcode.WriteRequest, "file\0netascii\0blksize\0512\0"),
            0,
            TftpOpcode.WriteRequest);
        yield return new TestCaseData(
            new byte[] { 0, 3, 0, 1, 10, 20 },
            2,
            TftpOpcode.Data);
        yield return new TestCaseData(
            new byte[] { 0, 4, 0, 1 },
            0,
            TftpOpcode.Acknowledgement);
        yield return new TestCaseData(
            CreatePacket(TftpOpcode.Error, "\0\0Missing\0"),
            0,
            TftpOpcode.Error);
        yield return new TestCaseData(
            CreatePacket(TftpOpcode.OptionAcknowledgement, "vendor\0anything\0"),
            0,
            TftpOpcode.OptionAcknowledgement);
    }

    private static IEnumerable<TestCaseData> InvalidPackets()
    {
        yield return new TestCaseData(Array.Empty<byte>(), 0, "Two bytes");
        yield return new TestCaseData(new byte[] { 0 }, 0, "Two bytes");
        yield return new TestCaseData(new byte[] { 0, 99 }, 0, "opcode");
        yield return new TestCaseData(
            CreatePacket(TftpOpcode.ReadRequest, "file\0unknown\0"),
            0,
            "mode");
        yield return new TestCaseData(new byte[] { 0, 3, 0 }, 3, "four-byte header");
        yield return new TestCaseData(new byte[] { 0, 3, 0, 1, 1 }, 0, "configured maximum");
        yield return new TestCaseData(new byte[] { 0, 3, 0, 1 }, -1, "protocol range");
        yield return new TestCaseData(new byte[] { 0, 4, 0 }, 0, "exactly four");
        yield return new TestCaseData(new byte[] { 0, 5, 0, 1, 65 }, 0, "terminator");
        yield return new TestCaseData(new byte[] { 0, 5, 0, 1, 0, 1 }, 0, "after");
        yield return new TestCaseData(new byte[] { 0, 6 }, 0, "at least one");
        yield return new TestCaseData(
            CreatePacket(TftpOpcode.OptionAcknowledgement, "timeout\01\0TIMEOUT\02\0"),
            0,
            "duplicate");
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
