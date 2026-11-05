using tftp;

namespace tftp.Tests;

public class ExceptionBehaviorTests
{
    [Test]
    public void ProtocolExceptionCarriesMessageAndOptionalOffset()
    {
        TftpProtocolException withOffset = new TftpProtocolException(
            "Missing string terminator",
            true,
            17);
        TftpProtocolException withoutOffset = new TftpProtocolException(
            "Invalid local packet",
            false,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(withOffset, Is.AssignableTo<Exception>());
            Assert.That(withOffset.Message, Is.EqualTo("Missing string terminator"));
            Assert.That(withOffset.HasPacketOffset, Is.True);
            Assert.That(withOffset.PacketOffset, Is.EqualTo(17));
            Assert.That(withoutOffset.Message, Is.EqualTo("Invalid local packet"));
            Assert.That(withoutOffset.HasPacketOffset, Is.False);
            Assert.That(withoutOffset.PacketOffset, Is.Zero);
        });
    }

    [Test]
    public void TimeoutExceptionCarriesLimitAndTransferMetrics()
    {
        TftpTimeoutException retryTimeout = new TftpTimeoutException(
            "Retry limit reached",
            true,
            5,
            31000);
        TftpTimeoutException durationTimeout = new TftpTimeoutException(
            "Duration limit reached",
            false,
            3,
            300000);

        Assert.Multiple(() =>
        {
            Assert.That(retryTimeout, Is.AssignableTo<Exception>());
            Assert.That(retryTimeout.Message, Is.EqualTo("Retry limit reached"));
            Assert.That(retryTimeout.RetryLimitReached, Is.True);
            Assert.That(retryTimeout.RetransmissionCount, Is.EqualTo(5));
            Assert.That(retryTimeout.ElapsedMilliseconds, Is.EqualTo(31000));
            Assert.That(durationTimeout.RetryLimitReached, Is.False);
            Assert.That(durationTimeout.RetransmissionCount, Is.EqualTo(3));
            Assert.That(durationTimeout.ElapsedMilliseconds, Is.EqualTo(300000));
        });
    }

    [Test]
    public void RemoteErrorExceptionCarriesPeerErrorDetails()
    {
        TftpRemoteErrorException exception = new TftpRemoteErrorException(
            TftpErrorCode.FileNotFound,
            "No such file");

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.AssignableTo<Exception>());
            Assert.That(exception.Message, Is.EqualTo("No such file"));
            Assert.That(exception.ErrorCode, Is.EqualTo(TftpErrorCode.FileNotFound));
            Assert.That(exception.RemoteMessage, Is.EqualTo("No such file"));
        });
    }

    [Test]
    public void OptionNegotiationExceptionCarriesRejectedOption()
    {
        TftpOptionNegotiationException exception = new TftpOptionNegotiationException(
            "Server increased block size",
            "blksize",
            "2048");

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.AssignableTo<Exception>());
            Assert.That(exception.Message, Is.EqualTo("Server increased block size"));
            Assert.That(exception.OptionName, Is.EqualTo("blksize"));
            Assert.That(exception.OptionValue, Is.EqualTo("2048"));
        });
    }

    [Test]
    public void StorageExceptionCarriesOperationAndUnderlyingFailure()
    {
        IOException innerException = new IOException("Disk unavailable");
        TftpStorageException exception = new TftpStorageException(
            "Could not publish upload",
            "CommitWrite",
            innerException);

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.AssignableTo<Exception>());
            Assert.That(exception.Message, Is.EqualTo("Could not publish upload"));
            Assert.That(exception.Operation, Is.EqualTo("CommitWrite"));
            Assert.That(exception.InnerException, Is.SameAs(innerException));
        });
    }

    [Test]
    public void StorageExceptionAllowsUnavailableUnderlyingFailure()
    {
        TftpStorageException exception = new TftpStorageException(
            "Storage provider failed",
            "OpenRead",
            null);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Storage provider failed"));
            Assert.That(exception.Operation, Is.EqualTo("OpenRead"));
            Assert.That(exception.InnerException, Is.Null);
        });
    }
}
