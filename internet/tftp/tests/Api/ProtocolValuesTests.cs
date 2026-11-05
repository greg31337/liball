using tftp;

namespace tftp.Tests;

public class ProtocolValuesTests
{
    [Test]
    public void PacketValuesMatchProtocolLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TftpProtocolValues.DefaultPort, Is.EqualTo(69));
            Assert.That(TftpProtocolValues.OpcodeLength, Is.EqualTo(2));
            Assert.That(TftpProtocolValues.DataHeaderLength, Is.EqualTo(4));
            Assert.That(TftpProtocolValues.AckPacketLength, Is.EqualTo(4));
            Assert.That(TftpProtocolValues.MaximumRequestPacketLength, Is.EqualTo(512));
            Assert.That(TftpProtocolValues.MaximumDataPacketLength, Is.EqualTo(65468));
        });
    }

    [Test]
    public void BlockSizeValuesMatchProtocolLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TftpProtocolValues.DefaultBlockSize, Is.EqualTo(512));
            Assert.That(TftpProtocolValues.MinimumBlockSize, Is.EqualTo(8));
            Assert.That(TftpProtocolValues.MaximumBlockSize, Is.EqualTo(65464));
            Assert.That(
                TftpProtocolValues.MaximumDataPacketLength,
                Is.EqualTo(TftpProtocolValues.MaximumBlockSize + TftpProtocolValues.DataHeaderLength));
        });
    }

    [Test]
    public void TimeoutValuesMatchProtocolLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TftpProtocolValues.MinimumTimeoutSeconds, Is.EqualTo(1));
            Assert.That(TftpProtocolValues.MaximumTimeoutSeconds, Is.EqualTo(255));
        });
    }

    [Test]
    public void ModeNamesUseCanonicalWireSpelling()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TftpProtocolValues.NetasciiMode, Is.EqualTo("netascii"));
            Assert.That(TftpProtocolValues.OctetMode, Is.EqualTo("octet"));
            Assert.That(TftpProtocolValues.MailMode, Is.EqualTo("mail"));
        });
    }

    [Test]
    public void OptionNamesUseCanonicalWireSpelling()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TftpProtocolValues.BlockSizeOption, Is.EqualTo("blksize"));
            Assert.That(TftpProtocolValues.TimeoutOption, Is.EqualTo("timeout"));
            Assert.That(TftpProtocolValues.TransferSizeOption, Is.EqualTo("tsize"));
        });
    }
}
