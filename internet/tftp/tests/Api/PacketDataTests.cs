using tftp;

namespace tftp.Tests;

public class PacketDataTests
{
    [Test]
    public void OptionDefaultsAreEmpty()
    {
        TftpOption option = new TftpOption();

        Assert.Multiple(() =>
        {
            Assert.That(option.Name, Is.Empty);
            Assert.That(option.Value, Is.Empty);
        });
    }

    [Test]
    public void PacketDefaultsDescribeAnEmptyOctetReadRequest()
    {
        TftpPacket packet = new TftpPacket();

        Assert.Multiple(() =>
        {
            Assert.That(packet.Opcode, Is.EqualTo(TftpOpcode.ReadRequest));
            Assert.That(packet.Filename, Is.Empty);
            Assert.That(packet.Mode, Is.EqualTo(TftpProtocolValues.OctetMode));
            Assert.That(packet.BlockNumber, Is.Zero);
            Assert.That(packet.Payload, Is.Empty);
            Assert.That(packet.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(packet.ErrorMessage, Is.Empty);
            Assert.That(packet.Options, Is.Empty);
        });
    }

    [TestCase(TftpOpcode.ReadRequest)]
    [TestCase(TftpOpcode.WriteRequest)]
    public void PacketCanRepresentRequests(TftpOpcode opcode)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = opcode;
        packet.Filename = "directory/file.txt";
        packet.Mode = TftpProtocolValues.NetasciiMode;
        packet.Options.Add(new TftpOption { Name = "blksize", Value = "1024" });
        packet.Options.Add(new TftpOption { Name = "timeout", Value = "3" });

        Assert.Multiple(() =>
        {
            Assert.That(packet.Opcode, Is.EqualTo(opcode));
            Assert.That(packet.Filename, Is.EqualTo("directory/file.txt"));
            Assert.That(packet.Mode, Is.EqualTo("netascii"));
            Assert.That(packet.Options.Select(option => option.Name),
                Is.EqualTo(new[] { "blksize", "timeout" }));
            Assert.That(packet.Options.Select(option => option.Value),
                Is.EqualTo(new[] { "1024", "3" }));
        });
    }

    [Test]
    public void PacketCanRepresentData()
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = 65535;
        packet.Payload = new byte[] { 0, 127, 255 };

        Assert.Multiple(() =>
        {
            Assert.That(packet.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(packet.BlockNumber, Is.EqualTo(65535));
            Assert.That(packet.Payload, Is.EqualTo(new byte[] { 0, 127, 255 }));
        });
    }

    [Test]
    public void PacketCanRepresentAcknowledgement()
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Acknowledgement;
        packet.BlockNumber = 42;

        Assert.Multiple(() =>
        {
            Assert.That(packet.Opcode, Is.EqualTo(TftpOpcode.Acknowledgement));
            Assert.That(packet.BlockNumber, Is.EqualTo(42));
        });
    }

    [Test]
    public void PacketCanRepresentError()
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = TftpErrorCode.AccessViolation;
        packet.ErrorMessage = "Access denied";

        Assert.Multiple(() =>
        {
            Assert.That(packet.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(packet.ErrorCode, Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(packet.ErrorMessage, Is.EqualTo("Access denied"));
        });
    }

    [Test]
    public void PacketCanRepresentOptionAcknowledgement()
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.OptionAcknowledgement;
        packet.Options.Add(new TftpOption { Name = "tsize", Value = "4096" });

        Assert.Multiple(() =>
        {
            Assert.That(packet.Opcode, Is.EqualTo(TftpOpcode.OptionAcknowledgement));
            Assert.That(packet.Options, Has.Count.EqualTo(1));
            Assert.That(packet.Options[0].Name, Is.EqualTo("tsize"));
            Assert.That(packet.Options[0].Value, Is.EqualTo("4096"));
        });
    }

    [Test]
    public void DecodeResultDefaultsDescribeFailureWithoutConsumption()
    {
        TftpPacketDecodeResult result = new TftpPacketDecodeResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Packet, Is.Not.Null);
            Assert.That(result.ConsumedLength, Is.Zero);
            Assert.That(result.FailureMessage, Is.Empty);
        });
    }

    [Test]
    public void DecodeResultCanRepresentSuccessAndFailureDetail()
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Acknowledgement;
        packet.BlockNumber = 7;

        TftpPacketDecodeResult result = new TftpPacketDecodeResult();
        result.Success = true;
        result.Packet = packet;
        result.ConsumedLength = 4;
        result.FailureMessage = "";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Packet, Is.SameAs(packet));
            Assert.That(result.ConsumedLength, Is.EqualTo(4));
            Assert.That(result.FailureMessage, Is.Empty);
        });

        result.Success = false;
        result.ConsumedLength = 0;
        result.FailureMessage = "Truncated packet";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ConsumedLength, Is.Zero);
            Assert.That(result.FailureMessage, Is.EqualTo("Truncated packet"));
        });
    }

    [Test]
    public void PacketsDoNotShareMutableDefaults()
    {
        TftpPacket first = new TftpPacket();
        TftpPacket second = new TftpPacket();
        first.Payload = new byte[] { 1 };
        first.Options.Add(new TftpOption { Name = "timeout", Value = "1" });

        Assert.Multiple(() =>
        {
            Assert.That(second.Payload, Is.Empty);
            Assert.That(second.Options, Is.Empty);
            Assert.That(first.Options, Is.Not.SameAs(second.Options));
        });
    }
}
