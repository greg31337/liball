using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace tftp;

internal class UdpDatagramSocket
{
    internal Socket Socket;
    internal IPEndPoint LocalEndPoint;

    internal UdpDatagramSocket(Socket socket, IPEndPoint localEndPoint)
    {
        Socket = socket;
        LocalEndPoint = localEndPoint;
    }

    internal static UdpDatagramSocket BindLocalSocket(
        IPAddress address,
        int port,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (address == null)
        {
            throw new TftpProtocolException(
                "The local bind address cannot be null.",
                false,
                0);
        }

        if (address.AddressFamily != AddressFamily.InterNetwork &&
            address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            throw new TftpProtocolException(
                "The local bind address must use IPv4 or IPv6.",
                false,
                0);
        }

        if (port < 0 || port > 65535)
        {
            throw new TftpProtocolException(
                "The local UDP port is outside the valid range.",
                false,
                0);
        }

        Socket socket = new Socket(
            address.AddressFamily,
            SocketType.Dgram,
            ProtocolType.Udp);
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            socket.DualMode = false;
            socket.SetSocketOption(
                SocketOptionLevel.IPv6,
                SocketOptionName.PacketInformation,
                true);
        }
        else
        {
            socket.SetSocketOption(
                SocketOptionLevel.IP,
                SocketOptionName.PacketInformation,
                true);
        }

        socket.Bind(new IPEndPoint(address, port));
        EndPoint? boundEndPoint = socket.LocalEndPoint;
        if (boundEndPoint == null)
        {
            socket.Close();
            throw new TftpProtocolException(
                "The UDP socket did not report a bound local endpoint.",
                false,
                0);
        }

        return new UdpDatagramSocket(socket, (IPEndPoint)boundEndPoint);
    }

    internal UdpDatagramReceiveResult ReceiveBoundedDatagram(
        byte[] destination,
        int maximumLength,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (destination == null)
        {
            throw new TftpProtocolException(
                "The datagram destination buffer cannot be null.",
                false,
                0);
        }

        if (maximumLength < 1 || maximumLength > destination.Length)
        {
            throw new TftpProtocolException(
                "The maximum datagram length is outside the destination buffer.",
                false,
                0);
        }

        EndPoint remoteTemplate = new IPEndPoint(IPAddress.Any, 0);
        if (Socket.AddressFamily == AddressFamily.InterNetworkV6)
        {
            remoteTemplate = new IPEndPoint(IPAddress.IPv6Any, 0);
        }

        SocketReceiveMessageFromResult received = Socket.ReceiveMessageFromAsync(
            destination.AsMemory(0, maximumLength),
            SocketFlags.None,
            remoteTemplate,
            cancellationToken).AsTask().GetAwaiter().GetResult();

        UdpDatagramReceiveResult result = new UdpDatagramReceiveResult();
        result.HasDatagram = true;
        result.Length = received.ReceivedBytes;
        result.RemoteEndPoint = (IPEndPoint)received.RemoteEndPoint;
        result.DestinationAddress = received.PacketInformation.Address;
        result.ExceededMaximumLength =
            (received.SocketFlags & SocketFlags.Truncated) != 0;
        return result;
    }

    internal UdpDatagramReceiveResult ReceiveBoundedDatagramOrTimeout(
        byte[] destination,
        int maximumLength,
        long waitMilliseconds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (destination == null)
        {
            throw new TftpProtocolException(
                "The datagram destination buffer cannot be null.",
                false,
                0);
        }

        if (maximumLength < 1 || maximumLength > destination.Length)
        {
            throw new TftpProtocolException(
                "The maximum datagram length is outside the destination buffer.",
                false,
                0);
        }

        if (waitMilliseconds < 0)
        {
            throw new TftpProtocolException(
                "The datagram wait duration cannot be negative.",
                false,
                0);
        }

        long startedMilliseconds = Environment.TickCount64;
        long elapsedMilliseconds = 0;
        while (elapsedMilliseconds < waitMilliseconds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long remainingMilliseconds = waitMilliseconds - elapsedMilliseconds;
            int pollMilliseconds = 50;
            if (remainingMilliseconds < pollMilliseconds)
            {
                pollMilliseconds = (int)remainingMilliseconds;
            }

            if (Socket.Poll(
                pollMilliseconds * 1000,
                SelectMode.SelectRead))
            {
                return ReceiveBoundedDatagram(
                    destination,
                    maximumLength,
                    cancellationToken);
            }

            long nowMilliseconds = Environment.TickCount64;
            if (nowMilliseconds < startedMilliseconds)
            {
                elapsedMilliseconds = 0;
            }
            else
            {
                elapsedMilliseconds = nowMilliseconds - startedMilliseconds;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (Socket.Poll(0, SelectMode.SelectRead))
        {
            return ReceiveBoundedDatagram(
                destination,
                maximumLength,
                cancellationToken);
        }

        return new UdpDatagramReceiveResult();
    }

    internal int SendUnknownTransferIdError(
        IPEndPoint remoteEndPoint,
        int maximumPacketLength,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (remoteEndPoint == null)
        {
            throw new TftpProtocolException(
                "The ERROR destination endpoint cannot be null.",
                false,
                0);
        }

        TftpPacket packet = TftpServerErrorMapper.CreateErrorPacket(
            TftpErrorCode.UnknownTransferId,
            "",
            maximumPacketLength);
        byte[] encoded = TftpServerErrorMapper.EncodeErrorPacket(packet);
        return Socket.SendTo(
            encoded.AsSpan(),
            SocketFlags.None,
            remoteEndPoint);
    }

    internal TftpPeerValidation ValidatePeerAndReply(
        TftpPeerTracker peerTracker,
        IPEndPoint remoteEndPoint,
        CancellationToken cancellationToken)
    {
        if (peerTracker == null)
        {
            throw new TftpProtocolException(
                "The peer tracker cannot be null.",
                false,
                0);
        }

        TftpPeerValidation validation = peerTracker.ClassifyPeer(remoteEndPoint);
        if (validation == TftpPeerValidation.UnknownTransferId)
        {
            SendUnknownTransferIdError(
                remoteEndPoint,
                TftpProtocolValues.MaximumRequestPacketLength,
                cancellationToken);
        }

        return validation;
    }

    internal void CloseSocket()
    {
        Socket.Close();
    }
}
