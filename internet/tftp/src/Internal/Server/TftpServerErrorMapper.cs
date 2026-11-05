using System;
using System.Text;

namespace tftp;

internal static class TftpServerErrorMapper
{
    internal static TftpPacket CreateErrorPacket(
        TftpErrorCode errorCode,
        string message,
        int maximumPacketLength)
    {
        if (maximumPacketLength < 5 ||
            maximumPacketLength > TftpProtocolValues.MaximumRequestPacketLength)
        {
            throw new TftpProtocolException(
                "The maximum server ERROR packet length is invalid.",
                false,
                0);
        }

        TftpErrorCode normalizedCode = NormalizeErrorCode(errorCode);
        string availableMessage = message;
        if (availableMessage == null || availableMessage.Length == 0)
        {
            availableMessage = DefaultErrorMessage(normalizedCode);
        }

        int maximumMessageLength = maximumPacketLength - 5;
        StringBuilder boundedMessage = new StringBuilder();
        int characterIndex = 0;
        while (characterIndex < availableMessage.Length &&
            boundedMessage.Length < maximumMessageLength)
        {
            char character = availableMessage[characterIndex];
            if (character == 0 || character > 127)
            {
                boundedMessage.Append('?');
            }
            else
            {
                boundedMessage.Append(character);
            }

            characterIndex += 1;
        }

        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = normalizedCode;
        packet.ErrorMessage = boundedMessage.ToString();
        return packet;
    }

    internal static byte[] EncodeErrorPacket(TftpPacket errorPacket)
    {
        if (errorPacket == null || errorPacket.Opcode != TftpOpcode.Error)
        {
            throw new TftpProtocolException(
                "An ERROR packet is required for server error encoding.",
                false,
                0);
        }

        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(errorPacket)];
        codec.Encode(errorPacket, encoded.AsSpan());
        return encoded;
    }

    internal static TftpErrorCode NormalizeErrorCode(TftpErrorCode errorCode)
    {
        int numericCode = (int)errorCode;
        if (numericCode < (int)TftpErrorCode.Undefined ||
            numericCode > (int)TftpErrorCode.OptionNegotiationFailed)
        {
            return TftpErrorCode.Undefined;
        }

        return errorCode;
    }

    internal static string DefaultErrorMessage(TftpErrorCode errorCode)
    {
        switch (errorCode)
        {
            case TftpErrorCode.FileNotFound:
                return "The requested file was not found.";
            case TftpErrorCode.AccessViolation:
                return "Access to the requested file was denied.";
            case TftpErrorCode.StorageExhausted:
                return "The server has insufficient storage capacity.";
            case TftpErrorCode.IllegalOperation:
                return "The TFTP operation is illegal.";
            case TftpErrorCode.UnknownTransferId:
                return "Unknown transfer ID.";
            case TftpErrorCode.FileAlreadyExists:
                return "The destination file already exists.";
            case TftpErrorCode.NoSuchUser:
                return "The requested user does not exist.";
            case TftpErrorCode.OptionNegotiationFailed:
                return "TFTP option negotiation failed.";
            default:
                return "The server transfer failed.";
        }
    }
}
