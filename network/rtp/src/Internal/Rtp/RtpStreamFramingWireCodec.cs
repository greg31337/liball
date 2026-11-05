using System;

namespace rtp;

internal static class RtpStreamFramingWireCodec
{
    internal static int GetEncodedSize(ReadOnlySpan<byte> datagram)
    {
        string? validationError = GetDatagramValidationError(datagram);
        if (validationError != null)
        {
            throw new RtpPacketFormatException(validationError);
        }

        return RtpProtocolConstants.StreamFrameLengthSize + datagram.Length;
    }

    internal static int EncodeFrame(
        ReadOnlySpan<byte> datagram,
        Span<byte> destination)
    {
        int encodedSize = GetEncodedSize(datagram);
        if (destination.Length < encodedSize)
        {
            throw new RtpBufferCapacityException(
                "The destination is too small for the encoded RTP stream frame.");
        }

        EncodeValidatedFrame(
            datagram,
            destination.Slice(0, encodedSize));
        return encodedSize;
    }

    internal static RtpCodecResult TryEncodeFrame(
        ReadOnlySpan<byte> datagram,
        Span<byte> destination)
    {
        string? validationError = GetDatagramValidationError(datagram);
        if (validationError != null)
        {
            return CreateFailure(validationError);
        }

        int encodedSize = RtpProtocolConstants.StreamFrameLengthSize + datagram.Length;
        if (destination.Length < encodedSize)
        {
            return CreateFailure(
                "The destination is too small for the encoded RTP stream frame.");
        }

        EncodeValidatedFrame(
            datagram,
            destination.Slice(0, encodedSize));
        RtpCodecResult result = new RtpCodecResult();
        result.Success = true;
        result.WrittenCount = encodedSize;
        return result;
    }

    internal static byte[] DecodeFrame(ReadOnlySpan<byte> source)
    {
        RtpCodecResult result = TryDecodeFrame(source);
        if (!result.Success)
        {
            string errorMessage = "The RTP stream frame could not be decoded.";
            if (result.ErrorMessage != null)
            {
                errorMessage = result.ErrorMessage;
            }

            throw new RtpPacketFormatException(errorMessage);
        }

        return result.Data;
    }

    internal static RtpCodecResult TryDecodeFrame(ReadOnlySpan<byte> source)
    {
        if (source.Length < RtpProtocolConstants.StreamFrameLengthSize)
        {
            return CreateFailure(
                "The RTP stream frame length prefix is incomplete.");
        }

        int datagramSize = NetworkBinaryReader.ReadUInt16(source, 0);
        int encodedSize = RtpProtocolConstants.StreamFrameLengthSize + datagramSize;
        if (source.Length < encodedSize)
        {
            return CreateFailure("The RTP stream frame payload is incomplete.");
        }

        RtpCodecResult result = new RtpCodecResult();
        result.Success = true;
        result.ConsumedCount = encodedSize;
        result.Data = source.Slice(
            RtpProtocolConstants.StreamFrameLengthSize,
            datagramSize).ToArray();
        return result;
    }

    internal static void EncodeValidatedFrame(
        ReadOnlySpan<byte> datagram,
        Span<byte> destination)
    {
        datagram.CopyTo(destination.Slice(
            RtpProtocolConstants.StreamFrameLengthSize,
            datagram.Length));
        NetworkBinaryWriter.WriteUInt16(
            destination,
            0,
            (ushort)datagram.Length);
    }

    internal static string? GetDatagramValidationError(ReadOnlySpan<byte> datagram)
    {
        if (datagram.Length > RtpProtocolConstants.MaximumStreamFramePayloadSize)
        {
            return "The RTP stream frame payload exceeds its 16-bit length field.";
        }

        return null;
    }

    internal static RtpCodecResult CreateFailure(string errorMessage)
    {
        RtpCodecResult result = new RtpCodecResult();
        result.ErrorMessage = errorMessage;
        return result;
    }
}
