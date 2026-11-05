using System.Net;
using System.Net.Sockets;
using tftp;

namespace tftp.Tests;

public class TransferRequestDataTests
{
    [Test]
    public void EndpointDefaultsUseTheStandardServerPort()
    {
        TftpEndpoint endpoint = new TftpEndpoint();

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Host, Is.Empty);
            Assert.That(endpoint.Port, Is.EqualTo(69));
            Assert.That(endpoint.LocalAddress, Is.Null);
            Assert.That(endpoint.AddressFamily, Is.EqualTo(AddressFamily.Unspecified));
        });
    }

    [Test]
    public void EndpointCanRepresentHostAndLocalAddressPreferences()
    {
        TftpEndpoint endpoint = new TftpEndpoint();
        endpoint.Host = "tftp.example.test";
        endpoint.Port = 1069;
        endpoint.LocalAddress = IPAddress.IPv6Loopback;
        endpoint.AddressFamily = AddressFamily.InterNetworkV6;

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Host, Is.EqualTo("tftp.example.test"));
            Assert.That(endpoint.Port, Is.EqualTo(1069));
            Assert.That(endpoint.LocalAddress, Is.EqualTo(IPAddress.IPv6Loopback));
            Assert.That(endpoint.AddressFamily, Is.EqualTo(AddressFamily.InterNetworkV6));
        });
    }

    [Test]
    public void TransferOptionsHaveBoundedPortableDefaults()
    {
        TftpTransferOptions options = new TftpTransferOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Mode, Is.EqualTo(TftpTransferMode.Octet));
            Assert.That(options.RequestBlockSize, Is.False);
            Assert.That(options.BlockSize, Is.EqualTo(512));
            Assert.That(options.RequestTimeout, Is.False);
            Assert.That(options.TimeoutSeconds, Is.EqualTo(1));
            Assert.That(options.RequestTransferSize, Is.False);
            Assert.That(options.RetryWithoutOptions, Is.False);
            Assert.That(options.InitialTimeoutSeconds, Is.EqualTo(1));
            Assert.That(options.MaximumTimeoutSeconds, Is.EqualTo(16));
            Assert.That(options.MaximumRetries, Is.EqualTo(5));
            Assert.That(options.MaximumTransferDurationSeconds, Is.EqualTo(300));
            Assert.That(options.DallyDurationSeconds, Is.EqualTo(2));
            Assert.That(options.MaximumTransferSize, Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void TransferOptionsCanRequestEveryStandardOption()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.Mode = TftpTransferMode.Netascii;
        options.RequestBlockSize = true;
        options.BlockSize = 1428;
        options.RequestTimeout = true;
        options.TimeoutSeconds = 4;
        options.RequestTransferSize = true;
        options.RetryWithoutOptions = true;
        options.InitialTimeoutSeconds = 2;
        options.MaximumTimeoutSeconds = 12;
        options.MaximumRetries = 8;
        options.MaximumTransferDurationSeconds = 120;
        options.DallyDurationSeconds = 3;
        options.MaximumTransferSize = 1048576;

        Assert.Multiple(() =>
        {
            Assert.That(options.Mode, Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(options.RequestBlockSize, Is.True);
            Assert.That(options.BlockSize, Is.EqualTo(1428));
            Assert.That(options.RequestTimeout, Is.True);
            Assert.That(options.TimeoutSeconds, Is.EqualTo(4));
            Assert.That(options.RequestTransferSize, Is.True);
            Assert.That(options.RetryWithoutOptions, Is.True);
            Assert.That(options.InitialTimeoutSeconds, Is.EqualTo(2));
            Assert.That(options.MaximumTimeoutSeconds, Is.EqualTo(12));
            Assert.That(options.MaximumRetries, Is.EqualTo(8));
            Assert.That(options.MaximumTransferDurationSeconds, Is.EqualTo(120));
            Assert.That(options.DallyDurationSeconds, Is.EqualTo(3));
            Assert.That(options.MaximumTransferSize, Is.EqualTo(1048576));
        });
    }

    [Test]
    public void ReadRequestDefaultsAreReadyForCallerConfiguration()
    {
        TftpReadRequest request = new TftpReadRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.Endpoint, Is.Not.Null);
            Assert.That(request.RemoteFilename, Is.Empty);
            Assert.That(request.Options, Is.Not.Null);
            Assert.That(request.Endpoint.Port, Is.EqualTo(69));
            Assert.That(request.Options.Mode, Is.EqualTo(TftpTransferMode.Octet));
        });
    }

    [Test]
    public void WriteRequestDistinguishesAbsentAndZeroKnownSizes()
    {
        TftpWriteRequest request = new TftpWriteRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.Endpoint, Is.Not.Null);
            Assert.That(request.RemoteFilename, Is.Empty);
            Assert.That(request.Options, Is.Not.Null);
            Assert.That(request.HasKnownSourceSize, Is.False);
            Assert.That(request.KnownSourceSize, Is.Zero);
        });

        request.HasKnownSourceSize = true;
        request.KnownSourceSize = 0;

        Assert.Multiple(() =>
        {
            Assert.That(request.HasKnownSourceSize, Is.True);
            Assert.That(request.KnownSourceSize, Is.Zero);
        });
    }

    [Test]
    public void RequestsDoNotShareMutableDefaults()
    {
        TftpReadRequest firstRead = new TftpReadRequest();
        TftpReadRequest secondRead = new TftpReadRequest();
        TftpWriteRequest firstWrite = new TftpWriteRequest();
        TftpWriteRequest secondWrite = new TftpWriteRequest();

        firstRead.Endpoint.Host = "read.example.test";
        firstRead.Options.RequestBlockSize = true;
        firstWrite.Endpoint.Host = "write.example.test";
        firstWrite.Options.RequestTransferSize = true;

        Assert.Multiple(() =>
        {
            Assert.That(secondRead.Endpoint.Host, Is.Empty);
            Assert.That(secondRead.Options.RequestBlockSize, Is.False);
            Assert.That(firstRead.Endpoint, Is.Not.SameAs(secondRead.Endpoint));
            Assert.That(firstRead.Options, Is.Not.SameAs(secondRead.Options));
            Assert.That(secondWrite.Endpoint.Host, Is.Empty);
            Assert.That(secondWrite.Options.RequestTransferSize, Is.False);
            Assert.That(firstWrite.Endpoint, Is.Not.SameAs(secondWrite.Endpoint));
            Assert.That(firstWrite.Options, Is.Not.SameAs(secondWrite.Options));
        });
    }

    [Test]
    public void TransferResultDefaultsUseProtocolDefaults()
    {
        TftpTransferResult result = new TftpTransferResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Direction, Is.EqualTo(TftpTransferDirection.Read));
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Created));
            Assert.That(result.Mode, Is.EqualTo(TftpTransferMode.Octet));
            Assert.That(result.FinalEndpoint, Is.Not.Null);
            Assert.That(result.BytesTransferred, Is.Zero);
            Assert.That(result.EncodedBytesTransferred, Is.Zero);
            Assert.That(result.ElapsedMilliseconds, Is.Zero);
            Assert.That(result.RetransmissionCount, Is.Zero);
            Assert.That(result.DuplicatePacketCount, Is.Zero);
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(512));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(1));
            Assert.That(result.HasTransferSize, Is.False);
            Assert.That(result.TransferSize, Is.Zero);
            Assert.That(result.NegotiatedOptions, Is.Empty);
        });
    }

    [Test]
    public void TransferResultsDoNotShareMutableDefaults()
    {
        TftpTransferResult first = new TftpTransferResult();
        TftpTransferResult second = new TftpTransferResult();
        first.FinalEndpoint.Host = "peer.example.test";
        first.NegotiatedOptions.Add(new TftpOption { Name = "timeout", Value = "1" });

        Assert.Multiple(() =>
        {
            Assert.That(second.FinalEndpoint.Host, Is.Empty);
            Assert.That(second.NegotiatedOptions, Is.Empty);
            Assert.That(first.FinalEndpoint, Is.Not.SameAs(second.FinalEndpoint));
            Assert.That(first.NegotiatedOptions, Is.Not.SameAs(second.NegotiatedOptions));
        });
    }
}
