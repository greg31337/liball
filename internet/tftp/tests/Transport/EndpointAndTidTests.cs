using System.Net;
using System.Net.Sockets;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class EndpointAndTidTests
{
    [Test]
    public void UnlockedTrackerAcceptsOnlyCandidatesFromExpectedAddress()
    {
        TftpPeerTracker tracker = new TftpPeerTracker(
            IPAddress.Loopback,
            0,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(tracker.ClassifyPeer(new IPEndPoint(IPAddress.Loopback, 40000)),
                Is.EqualTo(TftpPeerValidation.Candidate));
            Assert.That(tracker.ClassifyPeer(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 40000)),
                Is.EqualTo(TftpPeerValidation.UnexpectedAddress));
            Assert.That(tracker.ClassifyPeer(new IPEndPoint(IPAddress.Loopback, 0)),
                Is.EqualTo(TftpPeerValidation.UnknownTransferId));
            Assert.That(tracker.ClassifyPeer(null!),
                Is.EqualTo(TftpPeerValidation.UnexpectedAddress));
            Assert.That(tracker.IsTransferPortLocked, Is.False);
        });
    }

    [Test]
    public void LocksFirstValidatedTransferEndpoint()
    {
        TftpPeerTracker tracker = new TftpPeerTracker(
            IPAddress.Loopback,
            0,
            false);
        IPEndPoint selected = new IPEndPoint(IPAddress.Loopback, 40000);

        bool locked = tracker.LockTransferEndpoint(selected);

        Assert.Multiple(() =>
        {
            Assert.That(locked, Is.True);
            Assert.That(tracker.IsTransferPortLocked, Is.True);
            Assert.That(tracker.TransferPort, Is.EqualTo(40000));
            Assert.That(tracker.ClassifyPeer(selected),
                Is.EqualTo(TftpPeerValidation.Expected));
            Assert.That(tracker.ClassifyPeer(new IPEndPoint(IPAddress.Loopback, 40001)),
                Is.EqualTo(TftpPeerValidation.UnknownTransferId));
            Assert.That(tracker.ClassifyPeer(
                new IPEndPoint(IPAddress.Parse("127.0.0.2"), 40000)),
                Is.EqualTo(TftpPeerValidation.UnexpectedAddress));
        });
    }

    [Test]
    public void LockingIsIdempotentAndCannotChangePeer()
    {
        TftpPeerTracker tracker = new TftpPeerTracker(
            IPAddress.Loopback,
            40000,
            true);

        bool repeated = tracker.LockTransferEndpoint(
            new IPEndPoint(IPAddress.Loopback, 40000));
        bool changedPort = tracker.LockTransferEndpoint(
            new IPEndPoint(IPAddress.Loopback, 40001));
        bool changedAddress = tracker.LockTransferEndpoint(
            new IPEndPoint(IPAddress.Parse("127.0.0.2"), 40000));

        Assert.Multiple(() =>
        {
            Assert.That(repeated, Is.True);
            Assert.That(changedPort, Is.False);
            Assert.That(changedAddress, Is.False);
            Assert.That(tracker.TransferPort, Is.EqualTo(40000));
        });
    }

    [Test]
    public void FailedCandidateLockLeavesTrackerUnlocked()
    {
        TftpPeerTracker tracker = new TftpPeerTracker(
            IPAddress.Loopback,
            0,
            false);

        bool locked = tracker.LockTransferEndpoint(
            new IPEndPoint(IPAddress.Parse("127.0.0.2"), 40000));
        bool zeroPortLocked = tracker.LockTransferEndpoint(
            new IPEndPoint(IPAddress.Loopback, 0));

        Assert.Multiple(() =>
        {
            Assert.That(locked, Is.False);
            Assert.That(zeroPortLocked, Is.False);
            Assert.That(tracker.IsTransferPortLocked, Is.False);
            Assert.That(tracker.TransferPort, Is.Zero);
        });
    }

    [TestCase(0)]
    [TestCase(65536)]
    public void RejectsInvalidLockedTransferPort(int port)
    {
        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => new TftpPeerTracker(IPAddress.Loopback, port, true));

        Assert.That(exception.Message, Does.Contain("locked transfer port"));
    }

    [Test]
    public void RejectsMissingExpectedPeerAddress()
    {
        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => new TftpPeerTracker(null!, 0, false));

        Assert.That(exception.Message, Does.Contain("address cannot be null"));
    }

    [TestCase("127.0.0.1", false)]
    [TestCase("223.255.255.255", false)]
    [TestCase("224.0.0.0", true)]
    [TestCase("239.255.255.255", true)]
    [TestCase("240.0.0.0", false)]
    [TestCase("255.255.255.255", true)]
    [TestCase("0.0.0.0", false)]
    [TestCase("::1", false)]
    [TestCase("::", false)]
    [TestCase("ff02::1", true)]
    public void ListenerFilterSuppressesBroadcastAndMulticastDestinations(
        string addressText,
        bool expected)
    {
        bool result = TftpListenerDatagramFilter.ShouldSuppressDestination(
            IPAddress.Parse(addressText));

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ListenerFilterSuppressesMissingDestination()
    {
        Assert.That(TftpListenerDatagramFilter.ShouldSuppressDestination(null!), Is.True);
    }

    [Test]
    public void ReceivesBoundedDatagramWithPeerAndDestinationInformation()
    {
        UdpDatagramSocket? receiver = null;
        Socket? sender = null;
        try
        {
            receiver = UdpDatagramSocket.BindLocalSocket(
                IPAddress.Loopback,
                0,
                CancellationToken.None);
            sender = CreateBoundIpv4Socket();
            byte[] payload = { 1, 2, 3, 4 };
            sender.SendTo(payload, receiver.LocalEndPoint);
            byte[] destination = new byte[4];

            UdpDatagramReceiveResult result = receiver.ReceiveBoundedDatagram(
                destination,
                destination.Length,
                CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.Length, Is.EqualTo(payload.Length));
                Assert.That(result.ExceededMaximumLength, Is.False);
                Assert.That(destination, Is.EqualTo(payload));
                Assert.That(result.RemoteEndPoint,
                    Is.EqualTo((IPEndPoint)sender.LocalEndPoint!));
                Assert.That(result.DestinationAddress, Is.EqualTo(IPAddress.Loopback));
                Assert.That(TftpListenerDatagramFilter.ShouldSuppressDestination(
                    result.DestinationAddress), Is.False);
            });
        }
        finally
        {
            sender?.Close();
            receiver?.CloseSocket();
        }
    }

    [Test]
    public void ReportsDatagramThatExceedsMaximumLength()
    {
        UdpDatagramSocket? receiver = null;
        Socket? sender = null;
        try
        {
            receiver = UdpDatagramSocket.BindLocalSocket(
                IPAddress.Loopback,
                0,
                CancellationToken.None);
            sender = CreateBoundIpv4Socket();
            sender.SendTo(new byte[] { 1, 2, 3, 4, 5, 6 }, receiver.LocalEndPoint);
            byte[] destination = new byte[4];

            UdpDatagramReceiveResult result = receiver.ReceiveBoundedDatagram(
                destination,
                destination.Length,
                CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.Length, Is.EqualTo(destination.Length));
                Assert.That(result.ExceededMaximumLength, Is.True);
                Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            });
        }
        finally
        {
            sender?.Close();
            receiver?.CloseSocket();
        }
    }

    [Test]
    public void ReceivesIpv6DatagramWithIpv6PeerAndDestination()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 is not supported on this host.");
        }

        UdpDatagramSocket? receiver = null;
        Socket? sender = null;
        try
        {
            receiver = UdpDatagramSocket.BindLocalSocket(
                IPAddress.IPv6Loopback,
                0,
                CancellationToken.None);
            sender = new Socket(
                AddressFamily.InterNetworkV6,
                SocketType.Dgram,
                ProtocolType.Udp);
            sender.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
            sender.SendTo(new byte[] { 7 }, receiver.LocalEndPoint);
            byte[] destination = new byte[1];

            UdpDatagramReceiveResult result = receiver.ReceiveBoundedDatagram(
                destination,
                1,
                CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.Length, Is.EqualTo(1));
                Assert.That(destination[0], Is.EqualTo(7));
                Assert.That(result.RemoteEndPoint.AddressFamily,
                    Is.EqualTo(AddressFamily.InterNetworkV6));
                Assert.That(result.DestinationAddress, Is.EqualTo(IPAddress.IPv6Loopback));
            });
        }
        finally
        {
            sender?.Close();
            receiver?.CloseSocket();
        }
    }

    [Test]
    public void ReceiveDoesNotWritePastConfiguredMaximum()
    {
        UdpDatagramSocket? receiver = null;
        Socket? sender = null;
        try
        {
            receiver = UdpDatagramSocket.BindLocalSocket(
                IPAddress.Loopback,
                0,
                CancellationToken.None);
            sender = CreateBoundIpv4Socket();
            sender.SendTo(new byte[] { 1, 2, 3 }, receiver.LocalEndPoint);
            byte[] destination = { 99, 99, 99, 99, 99, 99 };

            UdpDatagramReceiveResult result = receiver.ReceiveBoundedDatagram(
                destination,
                4,
                CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.Length, Is.EqualTo(3));
                Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 99, 99, 99 }));
            });
        }
        finally
        {
            sender?.Close();
            receiver?.CloseSocket();
        }
    }

    [Test]
    public void ReceiveValidatesDestinationAndMaximumLength()
    {
        UdpDatagramSocket receiver = UdpDatagramSocket.BindLocalSocket(
            IPAddress.Loopback,
            0,
            CancellationToken.None);
        try
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<TftpProtocolException>(() => receiver.ReceiveBoundedDatagram(
                    null!,
                    1,
                    CancellationToken.None));
                Assert.Throws<TftpProtocolException>(() => receiver.ReceiveBoundedDatagram(
                    new byte[1],
                    0,
                    CancellationToken.None));
                Assert.Throws<TftpProtocolException>(() => receiver.ReceiveBoundedDatagram(
                    new byte[1],
                    2,
                    CancellationToken.None));
            });
        }
        finally
        {
            receiver.CloseSocket();
        }
    }

    [Test]
    public void ReceiveHonorsCancellationWhileWaiting()
    {
        UdpDatagramSocket receiver = UdpDatagramSocket.BindLocalSocket(
            IPAddress.Loopback,
            0,
            CancellationToken.None);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.CancelAfter(50);
        try
        {
            Assert.Throws<OperationCanceledException>(
                () => receiver.ReceiveBoundedDatagram(
                    new byte[4],
                    4,
                    cancellation.Token));
        }
        finally
        {
            receiver.CloseSocket();
        }
    }

    [Test]
    public void WrongTransferIdReceivesErrorFiveFromTransferSocket()
    {
        UdpDatagramSocket? transferSocket = null;
        Socket? expectedPeer = null;
        Socket? wrongPeer = null;
        try
        {
            transferSocket = UdpDatagramSocket.BindLocalSocket(
                IPAddress.Loopback,
                0,
                CancellationToken.None);
            expectedPeer = CreateBoundIpv4Socket();
            wrongPeer = CreateBoundIpv4Socket();
            wrongPeer.ReceiveTimeout = 1000;
            wrongPeer.Connect(transferSocket.LocalEndPoint);
            IPEndPoint expectedEndPoint = (IPEndPoint)expectedPeer.LocalEndPoint!;
            IPEndPoint wrongEndPoint = (IPEndPoint)wrongPeer.LocalEndPoint!;
            TftpPeerTracker tracker = new TftpPeerTracker(
                expectedEndPoint.Address,
                expectedEndPoint.Port,
                true);

            TftpPeerValidation validation = transferSocket.ValidatePeerAndReply(
                tracker,
                wrongEndPoint,
                CancellationToken.None);
            byte[] encodedError = new byte[128];
            int receivedLength = wrongPeer.Receive(encodedError);
            TftpPacket error = new TftpPacketCodec().Decode(
                encodedError.AsSpan(0, receivedLength),
                0);

            Assert.Multiple(() =>
            {
                Assert.That(validation, Is.EqualTo(TftpPeerValidation.UnknownTransferId));
                Assert.That(error.Opcode, Is.EqualTo(TftpOpcode.Error));
                Assert.That(error.ErrorCode, Is.EqualTo(TftpErrorCode.UnknownTransferId));
                Assert.That(error.ErrorMessage, Does.Contain("Unknown transfer ID"));
            });
        }
        finally
        {
            wrongPeer?.Close();
            expectedPeer?.Close();
            transferSocket?.CloseSocket();
        }
    }

    [Test]
    public void PeerValidationDoesNotReplyToExpectedCandidateOrUnrelatedAddress()
    {
        UdpDatagramSocket transferSocket = UdpDatagramSocket.BindLocalSocket(
            IPAddress.Loopback,
            0,
            CancellationToken.None);
        try
        {
            TftpPeerTracker tracker = new TftpPeerTracker(
                IPAddress.Loopback,
                0,
                false);

            TftpPeerValidation candidate = transferSocket.ValidatePeerAndReply(
                tracker,
                new IPEndPoint(IPAddress.Loopback, 40000),
                CancellationToken.None);
            TftpPeerValidation unrelated = transferSocket.ValidatePeerAndReply(
                tracker,
                new IPEndPoint(IPAddress.Parse("127.0.0.2"), 40000),
                CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(candidate, Is.EqualTo(TftpPeerValidation.Candidate));
                Assert.That(unrelated, Is.EqualTo(TftpPeerValidation.UnexpectedAddress));
            });
        }
        finally
        {
            transferSocket.CloseSocket();
        }
    }

    [Test]
    public void ErrorReplyAndPeerValidationHonorInvalidInputsAndCancellation()
    {
        UdpDatagramSocket socket = UdpDatagramSocket.BindLocalSocket(
            IPAddress.Loopback,
            0,
            CancellationToken.None);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        try
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<TftpProtocolException>(() => socket.ValidatePeerAndReply(
                    null!,
                    new IPEndPoint(IPAddress.Loopback, 40000),
                    CancellationToken.None));
                Assert.Throws<TftpProtocolException>(() => socket.SendUnknownTransferIdError(
                    null!,
                    TftpProtocolValues.MaximumRequestPacketLength,
                    CancellationToken.None));
                Assert.Throws<OperationCanceledException>(
                    () => socket.SendUnknownTransferIdError(
                        new IPEndPoint(IPAddress.Loopback, 40000),
                        TftpProtocolValues.MaximumRequestPacketLength,
                        cancellation.Token));
            });
        }
        finally
        {
            socket.CloseSocket();
        }
    }

    private static Socket CreateBoundIpv4Socket()
    {
        Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return socket;
    }
}
