using tftp;

namespace tftp.Tests;

public class ProtocolEnumTests
{
    [Test]
    public void OpcodesMatchWireValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)TftpOpcode.ReadRequest, Is.EqualTo(1));
            Assert.That((int)TftpOpcode.WriteRequest, Is.EqualTo(2));
            Assert.That((int)TftpOpcode.Data, Is.EqualTo(3));
            Assert.That((int)TftpOpcode.Acknowledgement, Is.EqualTo(4));
            Assert.That((int)TftpOpcode.Error, Is.EqualTo(5));
            Assert.That((int)TftpOpcode.OptionAcknowledgement, Is.EqualTo(6));
        });
    }

    [Test]
    public void ErrorCodesMatchWireValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)TftpErrorCode.Undefined, Is.EqualTo(0));
            Assert.That((int)TftpErrorCode.FileNotFound, Is.EqualTo(1));
            Assert.That((int)TftpErrorCode.AccessViolation, Is.EqualTo(2));
            Assert.That((int)TftpErrorCode.StorageExhausted, Is.EqualTo(3));
            Assert.That((int)TftpErrorCode.IllegalOperation, Is.EqualTo(4));
            Assert.That((int)TftpErrorCode.UnknownTransferId, Is.EqualTo(5));
            Assert.That((int)TftpErrorCode.FileAlreadyExists, Is.EqualTo(6));
            Assert.That((int)TftpErrorCode.NoSuchUser, Is.EqualTo(7));
            Assert.That((int)TftpErrorCode.OptionNegotiationFailed, Is.EqualTo(8));
        });
    }

    [Test]
    public void TransferModesHaveStableValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)TftpTransferMode.Octet, Is.EqualTo(0));
            Assert.That((int)TftpTransferMode.Netascii, Is.EqualTo(1));
        });
    }

    [Test]
    public void TransferDirectionsHaveStableValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)TftpTransferDirection.Read, Is.EqualTo(0));
            Assert.That((int)TftpTransferDirection.Write, Is.EqualTo(1));
        });
    }

    [Test]
    public void TransferStatesFollowLifecycleOrder()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)TftpTransferState.Created, Is.EqualTo(0));
            Assert.That((int)TftpTransferState.Resolving, Is.EqualTo(1));
            Assert.That((int)TftpTransferState.Requesting, Is.EqualTo(2));
            Assert.That((int)TftpTransferState.Negotiating, Is.EqualTo(3));
            Assert.That((int)TftpTransferState.Sending, Is.EqualTo(4));
            Assert.That((int)TftpTransferState.Receiving, Is.EqualTo(5));
            Assert.That((int)TftpTransferState.Dallying, Is.EqualTo(6));
            Assert.That((int)TftpTransferState.Completed, Is.EqualTo(7));
            Assert.That((int)TftpTransferState.Failed, Is.EqualTo(8));
            Assert.That((int)TftpTransferState.Cancelled, Is.EqualTo(9));
        });
    }

    [Test]
    public void ServerStopModesHaveStableValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)TftpServerStopMode.Graceful, Is.EqualTo(0));
            Assert.That((int)TftpServerStopMode.Immediate, Is.EqualTo(1));
        });
    }
}
