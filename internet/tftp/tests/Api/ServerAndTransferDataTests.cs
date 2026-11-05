using System.Net;
using tftp;

namespace tftp.Tests;

public class ServerAndTransferDataTests
{
    [Test]
    public void TransferEventDefaultsDescribeCreatedReadTransfer()
    {
        TftpTransferEvent transferEvent = new TftpTransferEvent();

        Assert.Multiple(() =>
        {
            Assert.That(transferEvent.Direction, Is.EqualTo(TftpTransferDirection.Read));
            Assert.That(transferEvent.State, Is.EqualTo(TftpTransferState.Created));
            Assert.That(transferEvent.Mode, Is.EqualTo(TftpTransferMode.Octet));
            Assert.That(transferEvent.Peer, Is.Not.Null);
            Assert.That(transferEvent.BlockNumber, Is.Zero);
            Assert.That(transferEvent.BytesTransferred, Is.Zero);
            Assert.That(transferEvent.EncodedBytesTransferred, Is.Zero);
            Assert.That(transferEvent.RetransmissionCount, Is.Zero);
            Assert.That(transferEvent.DuplicatePacketCount, Is.Zero);
            Assert.That(transferEvent.NegotiatedOptions, Is.Empty);
            Assert.That(transferEvent.HasError, Is.False);
            Assert.That(transferEvent.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(transferEvent.Message, Is.Empty);
            Assert.That(transferEvent.ServerRequest, Is.Null);
        });
    }

    [Test]
    public void TransferEventCanRepresentProgressAndErrorDetails()
    {
        TftpServerRequest request = new TftpServerRequest();
        request.Filename = "upload.bin";

        TftpTransferEvent transferEvent = new TftpTransferEvent();
        transferEvent.Direction = TftpTransferDirection.Write;
        transferEvent.State = TftpTransferState.Failed;
        transferEvent.Mode = TftpTransferMode.Netascii;
        transferEvent.Peer.Host = "192.0.2.10";
        transferEvent.Peer.Port = 49152;
        transferEvent.BlockNumber = 12;
        transferEvent.BytesTransferred = 6000;
        transferEvent.EncodedBytesTransferred = 6144;
        transferEvent.RetransmissionCount = 2;
        transferEvent.DuplicatePacketCount = 1;
        transferEvent.NegotiatedOptions.Add(
            new TftpOption { Name = "blksize", Value = "1024" });
        transferEvent.HasError = true;
        transferEvent.ErrorCode = TftpErrorCode.StorageExhausted;
        transferEvent.Message = "Storage limit reached";
        transferEvent.ServerRequest = request;

        Assert.Multiple(() =>
        {
            Assert.That(transferEvent.Direction, Is.EqualTo(TftpTransferDirection.Write));
            Assert.That(transferEvent.State, Is.EqualTo(TftpTransferState.Failed));
            Assert.That(transferEvent.Mode, Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(transferEvent.Peer.Host, Is.EqualTo("192.0.2.10"));
            Assert.That(transferEvent.Peer.Port, Is.EqualTo(49152));
            Assert.That(transferEvent.BlockNumber, Is.EqualTo(12));
            Assert.That(transferEvent.BytesTransferred, Is.EqualTo(6000));
            Assert.That(transferEvent.EncodedBytesTransferred, Is.EqualTo(6144));
            Assert.That(transferEvent.RetransmissionCount, Is.EqualTo(2));
            Assert.That(transferEvent.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(transferEvent.NegotiatedOptions, Has.Count.EqualTo(1));
            Assert.That(transferEvent.HasError, Is.True);
            Assert.That(transferEvent.ErrorCode, Is.EqualTo(TftpErrorCode.StorageExhausted));
            Assert.That(transferEvent.Message, Is.EqualTo("Storage limit reached"));
            Assert.That(transferEvent.ServerRequest, Is.SameAs(request));
        });
    }

    [Test]
    public void TransferResultCanRepresentCompletedNegotiatedTransfer()
    {
        TftpTransferResult result = new TftpTransferResult();
        result.Direction = TftpTransferDirection.Write;
        result.State = TftpTransferState.Completed;
        result.Mode = TftpTransferMode.Netascii;
        result.FinalEndpoint.Host = "203.0.113.5";
        result.FinalEndpoint.Port = 50000;
        result.BytesTransferred = 1000;
        result.EncodedBytesTransferred = 1015;
        result.ElapsedMilliseconds = 250;
        result.RetransmissionCount = 1;
        result.DuplicatePacketCount = 2;
        result.EffectiveBlockSize = 1024;
        result.EffectiveTimeoutSeconds = 3;
        result.HasTransferSize = true;
        result.TransferSize = 1015;
        result.NegotiatedOptions.Add(new TftpOption { Name = "tsize", Value = "1015" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Direction, Is.EqualTo(TftpTransferDirection.Write));
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(result.Mode, Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(result.FinalEndpoint.Host, Is.EqualTo("203.0.113.5"));
            Assert.That(result.FinalEndpoint.Port, Is.EqualTo(50000));
            Assert.That(result.BytesTransferred, Is.EqualTo(1000));
            Assert.That(result.EncodedBytesTransferred, Is.EqualTo(1015));
            Assert.That(result.ElapsedMilliseconds, Is.EqualTo(250));
            Assert.That(result.RetransmissionCount, Is.EqualTo(1));
            Assert.That(result.DuplicatePacketCount, Is.EqualTo(2));
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(1024));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(3));
            Assert.That(result.HasTransferSize, Is.True);
            Assert.That(result.TransferSize, Is.EqualTo(1015));
            Assert.That(result.NegotiatedOptions, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ServerBindingDefaultsToStandardIpv4Wildcard()
    {
        TftpServerBinding binding = new TftpServerBinding();

        Assert.Multiple(() =>
        {
            Assert.That(binding.Address, Is.EqualTo(IPAddress.Any));
            Assert.That(binding.Port, Is.EqualTo(69));
        });

        binding.Address = IPAddress.IPv6Any;
        binding.Port = 0;

        Assert.Multiple(() =>
        {
            Assert.That(binding.Address, Is.EqualTo(IPAddress.IPv6Any));
            Assert.That(binding.Port, Is.Zero);
        });
    }

    [Test]
    public void ServerOptionsHaveBoundedDefaults()
    {
        TftpServerOptions options = new TftpServerOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Bindings, Is.Empty);
            Assert.That(options.EnableBlockSize, Is.True);
            Assert.That(options.MaximumBlockSize, Is.EqualTo(65464));
            Assert.That(options.EnableTimeout, Is.True);
            Assert.That(options.EnableTransferSize, Is.True);
            Assert.That(options.MaximumSessions, Is.EqualTo(64));
            Assert.That(options.MaximumFilenameLength, Is.EqualTo(255));
            Assert.That(options.MaximumRequestPacketLength, Is.EqualTo(512));
            Assert.That(options.MaximumTransferSize, Is.EqualTo(long.MaxValue));
            Assert.That(options.InitialTimeoutSeconds, Is.EqualTo(1));
            Assert.That(options.MaximumTimeoutSeconds, Is.EqualTo(16));
            Assert.That(options.MaximumRetries, Is.EqualTo(5));
            Assert.That(options.MaximumSessionDurationSeconds, Is.EqualTo(300));
            Assert.That(options.DallyDurationSeconds, Is.EqualTo(2));
            Assert.That(options.StopMode, Is.EqualTo(TftpServerStopMode.Graceful));
            Assert.That(options.DirectoryStorage, Is.Null);
        });
    }

    [Test]
    public void ServerOptionsCanRepresentPolicyAndResourceLimits()
    {
        TftpServerOptions options = new TftpServerOptions();
        options.Bindings.Add(new TftpServerBinding { Address = IPAddress.IPv6Any, Port = 1069 });
        options.EnableBlockSize = false;
        options.MaximumBlockSize = 1400;
        options.EnableTimeout = false;
        options.EnableTransferSize = false;
        options.MaximumSessions = 10;
        options.MaximumFilenameLength = 120;
        options.MaximumRequestPacketLength = 400;
        options.MaximumTransferSize = 1000000;
        options.InitialTimeoutSeconds = 2;
        options.MaximumTimeoutSeconds = 10;
        options.MaximumRetries = 3;
        options.MaximumSessionDurationSeconds = 60;
        options.DallyDurationSeconds = 4;
        options.StopMode = TftpServerStopMode.Immediate;
        options.DirectoryStorage = new TftpDirectoryStorageOptions();
        options.DirectoryStorage.RootPath = "/srv/tftp";

        Assert.Multiple(() =>
        {
            Assert.That(options.Bindings, Has.Count.EqualTo(1));
            Assert.That(options.Bindings[0].Address, Is.EqualTo(IPAddress.IPv6Any));
            Assert.That(options.Bindings[0].Port, Is.EqualTo(1069));
            Assert.That(options.EnableBlockSize, Is.False);
            Assert.That(options.MaximumBlockSize, Is.EqualTo(1400));
            Assert.That(options.EnableTimeout, Is.False);
            Assert.That(options.EnableTransferSize, Is.False);
            Assert.That(options.MaximumSessions, Is.EqualTo(10));
            Assert.That(options.MaximumFilenameLength, Is.EqualTo(120));
            Assert.That(options.MaximumRequestPacketLength, Is.EqualTo(400));
            Assert.That(options.MaximumTransferSize, Is.EqualTo(1000000));
            Assert.That(options.InitialTimeoutSeconds, Is.EqualTo(2));
            Assert.That(options.MaximumTimeoutSeconds, Is.EqualTo(10));
            Assert.That(options.MaximumRetries, Is.EqualTo(3));
            Assert.That(options.MaximumSessionDurationSeconds, Is.EqualTo(60));
            Assert.That(options.DallyDurationSeconds, Is.EqualTo(4));
            Assert.That(options.StopMode, Is.EqualTo(TftpServerStopMode.Immediate));
            Assert.That(options.DirectoryStorage.RootPath, Is.EqualTo("/srv/tftp"));
        });
    }

    [Test]
    public void ServerRequestDistinguishesAbsentAndZeroAnnouncedSizes()
    {
        TftpServerRequest request = new TftpServerRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.Filename, Is.Empty);
            Assert.That(request.Direction, Is.EqualTo(TftpTransferDirection.Read));
            Assert.That(request.Mode, Is.EqualTo(TftpTransferMode.Octet));
            Assert.That(request.ClientEndpoint, Is.Not.Null);
            Assert.That(request.RequestedOptions, Is.Empty);
            Assert.That(request.EffectiveOptions, Is.Empty);
            Assert.That(request.HasAnnouncedSize, Is.False);
            Assert.That(request.AnnouncedSize, Is.Zero);
        });

        request.HasAnnouncedSize = true;
        request.AnnouncedSize = 0;

        Assert.Multiple(() =>
        {
            Assert.That(request.HasAnnouncedSize, Is.True);
            Assert.That(request.AnnouncedSize, Is.Zero);
        });
    }

    [Test]
    public void ServerStatusDefaultsDescribeStoppedServer()
    {
        TftpServerStatus status = new TftpServerStatus();

        Assert.Multiple(() =>
        {
            Assert.That(status.IsRunning, Is.False);
            Assert.That(status.IsDraining, Is.False);
            Assert.That(status.BoundEndpoints, Is.Empty);
            Assert.That(status.ActiveTransferCount, Is.Zero);
            Assert.That(status.AcceptedTransferCount, Is.Zero);
            Assert.That(status.CompletedTransferCount, Is.Zero);
            Assert.That(status.FailedTransferCount, Is.Zero);
        });
    }

    [Test]
    public void ServerStatusCanRepresentRunningAndDrainingState()
    {
        TftpServerStatus status = new TftpServerStatus();
        status.IsRunning = true;
        status.IsDraining = true;
        status.BoundEndpoints.Add(new TftpServerBinding { Address = IPAddress.Loopback, Port = 40000 });
        status.ActiveTransferCount = 2;
        status.AcceptedTransferCount = 10;
        status.CompletedTransferCount = 7;
        status.FailedTransferCount = 1;

        Assert.Multiple(() =>
        {
            Assert.That(status.IsRunning, Is.True);
            Assert.That(status.IsDraining, Is.True);
            Assert.That(status.BoundEndpoints, Has.Count.EqualTo(1));
            Assert.That(status.BoundEndpoints[0].Address, Is.EqualTo(IPAddress.Loopback));
            Assert.That(status.BoundEndpoints[0].Port, Is.EqualTo(40000));
            Assert.That(status.ActiveTransferCount, Is.EqualTo(2));
            Assert.That(status.AcceptedTransferCount, Is.EqualTo(10));
            Assert.That(status.CompletedTransferCount, Is.EqualTo(7));
            Assert.That(status.FailedTransferCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ServerAndEventModelsDoNotShareMutableDefaults()
    {
        TftpServerOptions firstOptions = new TftpServerOptions();
        TftpServerOptions secondOptions = new TftpServerOptions();
        TftpServerRequest firstRequest = new TftpServerRequest();
        TftpServerRequest secondRequest = new TftpServerRequest();
        TftpServerStatus firstStatus = new TftpServerStatus();
        TftpServerStatus secondStatus = new TftpServerStatus();
        TftpTransferEvent firstEvent = new TftpTransferEvent();
        TftpTransferEvent secondEvent = new TftpTransferEvent();

        firstOptions.Bindings.Add(new TftpServerBinding());
        firstRequest.ClientEndpoint.Host = "client.example.test";
        firstRequest.RequestedOptions.Add(new TftpOption());
        firstRequest.EffectiveOptions.Add(new TftpOption());
        firstStatus.BoundEndpoints.Add(new TftpServerBinding());
        firstEvent.Peer.Host = "peer.example.test";
        firstEvent.NegotiatedOptions.Add(new TftpOption());

        Assert.Multiple(() =>
        {
            Assert.That(secondOptions.Bindings, Is.Empty);
            Assert.That(secondRequest.ClientEndpoint.Host, Is.Empty);
            Assert.That(secondRequest.RequestedOptions, Is.Empty);
            Assert.That(secondRequest.EffectiveOptions, Is.Empty);
            Assert.That(secondStatus.BoundEndpoints, Is.Empty);
            Assert.That(secondEvent.Peer.Host, Is.Empty);
            Assert.That(secondEvent.NegotiatedOptions, Is.Empty);
        });
    }
}
