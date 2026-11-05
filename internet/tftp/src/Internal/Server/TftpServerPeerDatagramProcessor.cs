using System;
using System.Net;
using System.Threading;

namespace tftp;

internal static class TftpServerPeerDatagramProcessor
{
    internal static TftpServerPeerDatagramResult ClassifyDatagram(
        TftpServerSession session,
        IPEndPoint remoteEndPoint,
        byte[] buffer,
        int datagramLength,
        int maximumDataLength,
        int maximumErrorPacketLength,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (session == null || remoteEndPoint == null || buffer == null)
        {
            throw new TftpProtocolException(
                "The server peer datagram inputs cannot be null.",
                false,
                0);
        }

        if (maximumDataLength < 0 ||
            maximumDataLength > TftpProtocolValues.MaximumBlockSize)
        {
            throw new TftpProtocolException(
                "The server peer DATA length limit is invalid.",
                false,
                0);
        }

        if (maximumErrorPacketLength < 5 ||
            maximumErrorPacketLength >
                TftpProtocolValues.MaximumRequestPacketLength)
        {
            throw new TftpProtocolException(
                "The server peer ERROR length limit is invalid.",
                false,
                0);
        }

        if (!session.ClientEndPoint.Address.Equals(remoteEndPoint.Address))
        {
            return new TftpServerPeerDatagramResult();
        }

        if (session.ClientEndPoint.Port != remoteEndPoint.Port)
        {
            session.TransferSocket.SendUnknownTransferIdError(
                remoteEndPoint,
                maximumErrorPacketLength,
                cancellationToken);
            TftpServerPeerDatagramResult unknownTransferResult =
                new TftpServerPeerDatagramResult();
            unknownTransferResult.Action =
                TftpServerPeerDatagramAction.RejectUnknownTransferId;
            return unknownTransferResult;
        }

        if (datagramLength < 0 || datagramLength > buffer.Length)
        {
            throw new TftpProtocolException(
                "The server peer datagram length is invalid.",
                false,
                0);
        }

        TftpPacketCodec codec = new TftpPacketCodec();
        TftpPacketDecodeResult decoded = codec.TryDecode(
            buffer.AsSpan(0, datagramLength),
            maximumDataLength);
        if (!decoded.Success)
        {
            TftpServerPeerDatagramResult malformedResult =
                new TftpServerPeerDatagramResult();
            malformedResult.Action = TftpServerPeerDatagramAction.SendError;
            malformedResult.ErrorPacket =
                TftpServerErrorMapper.CreateErrorPacket(
                    TftpErrorCode.IllegalOperation,
                    decoded.FailureMessage,
                    maximumErrorPacketLength);
            return malformedResult;
        }

        TftpServerPeerDatagramResult acceptedResult =
            new TftpServerPeerDatagramResult();
        acceptedResult.Action = TftpServerPeerDatagramAction.AcceptPacket;
        acceptedResult.Packet = decoded.Packet;
        return acceptedResult;
    }
}
