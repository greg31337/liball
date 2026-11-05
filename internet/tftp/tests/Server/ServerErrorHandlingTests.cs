using System.Net;
using System.Net.Sockets;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class ServerErrorHandlingTests
{
    [TestCase(TftpErrorCode.Undefined, "server transfer")]
    [TestCase(TftpErrorCode.FileNotFound, "file was not found")]
    [TestCase(TftpErrorCode.AccessViolation, "denied")]
    [TestCase(TftpErrorCode.StorageExhausted, "storage capacity")]
    [TestCase(TftpErrorCode.IllegalOperation, "operation is illegal")]
    [TestCase(TftpErrorCode.UnknownTransferId, "Unknown transfer ID")]
    [TestCase(TftpErrorCode.FileAlreadyExists, "already exists")]
    [TestCase(TftpErrorCode.NoSuchUser, "user does not exist")]
    [TestCase(TftpErrorCode.OptionNegotiationFailed, "negotiation failed")]
    public void MapsEveryStandardErrorCodeToAnEncodableDefault(
        TftpErrorCode errorCode,
        string expectedMessage)
    {
        TftpPacket packet = TftpServerErrorMapper.CreateErrorPacket(
            errorCode,
            "",
            TftpProtocolValues.MaximumRequestPacketLength);
        byte[] encoded = TftpServerErrorMapper.EncodeErrorPacket(packet);
        TftpPacket decoded = new TftpPacketCodec().Decode(encoded, 0);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(decoded.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(decoded.ErrorMessage, Does.Contain(expectedMessage));
            Assert.That(encoded.Length,
                Is.LessThanOrEqualTo(TftpProtocolValues.MaximumRequestPacketLength));
        });
    }

    [Test]
    public void NormalizesPrivateCodesAndBoundsInvalidTextToPacketCapacity()
    {
        TftpPacket packet = TftpServerErrorMapper.CreateErrorPacket(
            (TftpErrorCode)99,
            "abcdef\0éghijklmnopqrstuvwxyz",
            12);
        byte[] encoded = TftpServerErrorMapper.EncodeErrorPacket(packet);
        TftpPacket decoded = new TftpPacketCodec().Decode(encoded, 0);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Has.Length.EqualTo(12));
            Assert.That(decoded.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(decoded.ErrorMessage, Is.EqualTo("abcdef?"));
            Assert.That(decoded.ErrorMessage.All(character => character <= 127),
                Is.True);
        });
    }

    [Test]
    public void ClassifiesInitialRequestsMalformedPacketsAndErrorSilence()
    {
        TftpPacket request = new TftpPacket();
        request.Opcode = TftpOpcode.ReadRequest;
        request.Filename = "file.bin";
        request.Mode = TftpProtocolValues.OctetMode;
        byte[] encodedRequest = EncodePacket(request);
        TftpServerInitialDatagramResult accepted =
            TftpServerInitialDatagramProcessor.ClassifyDatagram(
                encodedRequest,
                CreateDatagram(encodedRequest.Length, IPAddress.Loopback),
                64);

        byte[] malformedBytes = [0, 1, 102];
        TftpServerInitialDatagramResult malformed =
            TftpServerInitialDatagramProcessor.ClassifyDatagram(
                malformedBytes,
                CreateDatagram(malformedBytes.Length, IPAddress.Loopback),
                64);
        byte[] acknowledgement = [0, 4, 0, 0];
        TftpServerInitialDatagramResult unexpected =
            TftpServerInitialDatagramProcessor.ClassifyDatagram(
                acknowledgement,
                CreateDatagram(acknowledgement.Length, IPAddress.Loopback),
                64);

        TftpPacket error = TftpServerErrorMapper.CreateErrorPacket(
            TftpErrorCode.Undefined,
            "peer error",
            64);
        byte[] encodedError = TftpServerErrorMapper.EncodeErrorPacket(error);
        TftpServerInitialDatagramResult ignoredError =
            TftpServerInitialDatagramProcessor.ClassifyDatagram(
                encodedError,
                CreateDatagram(encodedError.Length, IPAddress.Loopback),
                64);
        TftpServerInitialDatagramResult ignoredBroadcast =
            TftpServerInitialDatagramProcessor.ClassifyDatagram(
                encodedRequest,
                CreateDatagram(encodedRequest.Length, IPAddress.Broadcast),
                64);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Action,
                Is.EqualTo(TftpServerInitialDatagramAction.AcceptRequest));
            Assert.That(accepted.RequestPacket!.Filename, Is.EqualTo("file.bin"));
            Assert.That(malformed.Action,
                Is.EqualTo(TftpServerInitialDatagramAction.SendError));
            Assert.That(malformed.ErrorPacket!.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(unexpected.Action,
                Is.EqualTo(TftpServerInitialDatagramAction.SendError));
            Assert.That(unexpected.ErrorPacket!.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(ignoredError.Action,
                Is.EqualTo(TftpServerInitialDatagramAction.Ignore));
            Assert.That(ignoredBroadcast.Action,
                Is.EqualTo(TftpServerInitialDatagramAction.Ignore));
        });
    }

    [Test]
    public void OversizedInitialErrorIsBoundedAndTruncatedDatagramIsRejected()
    {
        byte[] oversized = new byte[21];
        UdpDatagramReceiveResult datagram = CreateDatagram(
            oversized.Length,
            IPAddress.Loopback);
        TftpServerInitialDatagramResult lengthFailure =
            TftpServerInitialDatagramProcessor.ClassifyDatagram(
                oversized,
                datagram,
                20);
        UdpDatagramReceiveResult truncatedDatagram = CreateDatagram(
            4,
            IPAddress.Loopback);
        truncatedDatagram.ExceededMaximumLength = true;
        TftpServerInitialDatagramResult truncatedFailure =
            TftpServerInitialDatagramProcessor.ClassifyDatagram(
                new byte[4],
                truncatedDatagram,
                20);

        byte[] lengthError = TftpServerErrorMapper.EncodeErrorPacket(
            lengthFailure.ErrorPacket!);
        byte[] truncatedError = TftpServerErrorMapper.EncodeErrorPacket(
            truncatedFailure.ErrorPacket!);
        Assert.Multiple(() =>
        {
            Assert.That(lengthFailure.Action,
                Is.EqualTo(TftpServerInitialDatagramAction.SendError));
            Assert.That(truncatedFailure.Action,
                Is.EqualTo(TftpServerInitialDatagramAction.SendError));
            Assert.That(lengthError.Length, Is.LessThanOrEqualTo(20));
            Assert.That(truncatedError.Length, Is.LessThanOrEqualTo(20));
        });
    }

    [Test]
    public void PeerClassifierIgnoresOtherAddressesAndRejectsWrongTidNonfatally()
    {
        using Socket expectedPeer = CreateBoundSocket();
        using Socket wrongPortPeer = CreateBoundSocket();
        wrongPortPeer.ReceiveTimeout = 1000;
        IPEndPoint expectedEndPoint =
            (IPEndPoint)expectedPeer.LocalEndPoint!;
        TftpServerSession session = new TftpServerSession(
            "peer-test",
            expectedEndPoint,
            IPAddress.Loopback,
            CreateRequestPacket(),
            new ControlledSessionHandler([]),
            CancellationToken.None);
        try
        {
            TftpServerPeerDatagramResult otherAddress =
                TftpServerPeerDatagramProcessor.ClassifyDatagram(
                    session,
                    new IPEndPoint(IPAddress.Parse("127.0.0.2"), 48000),
                    [0],
                    1,
                    512,
                    64,
                    CancellationToken.None);
            IPEndPoint wrongPortEndPoint =
                (IPEndPoint)wrongPortPeer.LocalEndPoint!;
            TftpServerPeerDatagramResult wrongTid =
                TftpServerPeerDatagramProcessor.ClassifyDatagram(
                    session,
                    wrongPortEndPoint,
                    [0],
                    1,
                    512,
                    64,
                    CancellationToken.None);
            byte[] errorBuffer = new byte[64];
            int errorLength = wrongPortPeer.Receive(errorBuffer);
            TftpPacket tidError = new TftpPacketCodec().Decode(
                errorBuffer.AsSpan(0, errorLength),
                0);
            byte[] acknowledgement = [0, 4, 0, 1];
            TftpServerPeerDatagramResult accepted =
                TftpServerPeerDatagramProcessor.ClassifyDatagram(
                    session,
                    expectedEndPoint,
                    acknowledgement,
                    acknowledgement.Length,
                    512,
                    64,
                    CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(otherAddress.Action,
                    Is.EqualTo(
                        TftpServerPeerDatagramAction.IgnoreUnexpectedAddress));
                Assert.That(wrongTid.Action,
                    Is.EqualTo(
                        TftpServerPeerDatagramAction.RejectUnknownTransferId));
                Assert.That(tidError.ErrorCode,
                    Is.EqualTo(TftpErrorCode.UnknownTransferId));
                Assert.That(accepted.Action,
                    Is.EqualTo(TftpServerPeerDatagramAction.AcceptPacket));
                Assert.That(accepted.Packet!.Opcode,
                    Is.EqualTo(TftpOpcode.Acknowledgement));
                Assert.That(accepted.Packet.BlockNumber, Is.EqualTo(1));
            });
        }
        finally
        {
            session.CloseResources();
        }
    }

    [Test]
    public void MalformedExpectedPeerPacketProducesOneUnretainedError()
    {
        using Socket peer = CreateBoundSocket();
        peer.ReceiveTimeout = 1000;
        IPEndPoint peerEndPoint = (IPEndPoint)peer.LocalEndPoint!;
        TftpServerSession session = new TftpServerSession(
            "malformed-test",
            peerEndPoint,
            IPAddress.Loopback,
            CreateRequestPacket(),
            new ControlledSessionHandler([]),
            CancellationToken.None);
        try
        {
            byte[] retainedData = [0, 3, 0, 1, 7];
            session.SendAndRetainResponse(
                retainedData,
                CancellationToken.None);
            byte[] received = new byte[64];
            peer.Receive(received);
            TftpServerPeerDatagramResult malformed =
                TftpServerPeerDatagramProcessor.ClassifyDatagram(
                    session,
                    peerEndPoint,
                    [0],
                    1,
                    512,
                    32,
                    CancellationToken.None);
            session.SendErrorResponse(
                malformed.ErrorPacket!,
                CancellationToken.None);
            int errorLength = peer.Receive(received);
            TftpPacket error = new TftpPacketCodec().Decode(
                received.AsSpan(0, errorLength),
                0);
            bool resent = session.RegisterDuplicateAndResend(
                CancellationToken.None);
            int repeatedLength = peer.Receive(received);

            Assert.Multiple(() =>
            {
                Assert.That(malformed.Action,
                    Is.EqualTo(TftpServerPeerDatagramAction.SendError));
                Assert.That(error.ErrorCode,
                    Is.EqualTo(TftpErrorCode.IllegalOperation));
                Assert.That(errorLength, Is.LessThanOrEqualTo(32));
                Assert.That(resent, Is.True);
                Assert.That(repeatedLength, Is.EqualTo(retainedData.Length));
                Assert.That(received.AsSpan(0, repeatedLength).ToArray(),
                    Is.EqualTo(retainedData));
                Assert.That(session.RetainedResponse, Is.EqualTo(retainedData));
            });
        }
        finally
        {
            session.CloseResources();
        }
    }

    [Test]
    public void ServerStateFailuresUseBoundedMapperAndAreNotRetransmitted()
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        TftpPacket request = CreateRequestPacket();
        request.Options.Add(CreateOption("tsize", "1"));
        TftpServerOptions options = new TftpServerOptions();
        options.MaximumRequestPacketLength = 24;
        TftpServerReadStateMachine state = new TftpServerReadStateMachine(
            request,
            new IPEndPoint(IPAddress.Loopback, 49000),
            storage,
            options,
            new ManualTimeSource());

        TftpServerReadResult failure = state.StartRead(CancellationToken.None);
        byte[] encodedError = TftpServerErrorMapper.EncodeErrorPacket(
            failure.Packet!);

        Assert.Multiple(() =>
        {
            Assert.That(failure.Action,
                Is.EqualTo(TftpServerReadAction.SendError));
            Assert.That(failure.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.OptionNegotiationFailed));
            Assert.That(encodedError.Length, Is.LessThanOrEqualTo(24));
            Assert.Throws<TftpProtocolException>(() => state.EvaluateReadTimer(
                CancellationToken.None));
            Assert.That(storage.CallCount, Is.Zero);
        });
    }

    private static byte[] EncodePacket(TftpPacket packet)
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(packet)];
        codec.Encode(packet, encoded);
        return encoded;
    }

    private static UdpDatagramReceiveResult CreateDatagram(
        int length,
        IPAddress destinationAddress)
    {
        UdpDatagramReceiveResult datagram = new UdpDatagramReceiveResult();
        datagram.HasDatagram = true;
        datagram.Length = length;
        datagram.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 49100);
        datagram.DestinationAddress = destinationAddress;
        return datagram;
    }

    private static Socket CreateBoundSocket()
    {
        Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return socket;
    }

    private static TftpPacket CreateRequestPacket()
    {
        TftpPacket request = new TftpPacket();
        request.Opcode = TftpOpcode.ReadRequest;
        request.Filename = "file.bin";
        request.Mode = TftpProtocolValues.OctetMode;
        return request;
    }

    private static TftpOption CreateOption(string name, string value)
    {
        TftpOption option = new TftpOption();
        option.Name = name;
        option.Value = value;
        return option;
    }
}
