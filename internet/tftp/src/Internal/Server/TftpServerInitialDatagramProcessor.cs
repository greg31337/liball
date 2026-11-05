using System;

namespace tftp;

internal static class TftpServerInitialDatagramProcessor
{
    internal static TftpServerInitialDatagramResult ClassifyDatagram(
        byte[] buffer,
        UdpDatagramReceiveResult datagram,
        int maximumRequestLength)
    {
        if (buffer == null || datagram == null)
        {
            throw new TftpProtocolException(
                "The listener datagram inputs cannot be null.",
                false,
                0);
        }

        if (maximumRequestLength < 5 ||
            maximumRequestLength > TftpProtocolValues.MaximumRequestPacketLength)
        {
            throw new TftpProtocolException(
                "The listener request length limit is invalid.",
                false,
                0);
        }

        if (!datagram.HasDatagram || datagram.Length < 0 ||
            datagram.Length > buffer.Length ||
            datagram.RemoteEndPoint.Port < 1 ||
            TftpListenerDatagramFilter.ShouldSuppressDestination(
                datagram.DestinationAddress))
        {
            return new TftpServerInitialDatagramResult();
        }

        if (datagram.ExceededMaximumLength ||
            datagram.Length > maximumRequestLength)
        {
            return CreateInitialError(
                "The initial request exceeds the configured packet length limit.",
                maximumRequestLength);
        }

        TftpPacketCodec codec = new TftpPacketCodec();
        TftpPacketDecodeResult decoded = codec.TryDecode(
            buffer.AsSpan(0, datagram.Length),
            0);
        if (!decoded.Success)
        {
            return CreateInitialError(
                decoded.FailureMessage,
                maximumRequestLength);
        }

        TftpPacket packet = decoded.Packet;
        if (packet.Opcode == TftpOpcode.Error)
        {
            return new TftpServerInitialDatagramResult();
        }

        if (packet.Opcode != TftpOpcode.ReadRequest &&
            packet.Opcode != TftpOpcode.WriteRequest)
        {
            return CreateInitialError(
                "The listener accepts only RRQ and WRQ packets.",
                maximumRequestLength);
        }

        TftpServerInitialDatagramResult result =
            new TftpServerInitialDatagramResult();
        result.Action = TftpServerInitialDatagramAction.AcceptRequest;
        result.RequestPacket = packet;
        return result;
    }

    internal static TftpServerInitialDatagramResult CreateInitialError(
        string message,
        int maximumPacketLength)
    {
        TftpServerInitialDatagramResult result =
            new TftpServerInitialDatagramResult();
        result.Action = TftpServerInitialDatagramAction.SendError;
        result.ErrorPacket = TftpServerErrorMapper.CreateErrorPacket(
            TftpErrorCode.IllegalOperation,
            message,
            maximumPacketLength);
        result.FailureMessage = result.ErrorPacket.ErrorMessage;
        return result;
    }
}
