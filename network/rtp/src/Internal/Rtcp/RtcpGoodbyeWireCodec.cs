using System;

namespace rtp;

internal static class RtcpGoodbyeWireCodec
{
    internal static string? GetGoodbyeValidationError(RtcpGoodbye? goodbye)
    {
        if (goodbye == null)
        {
            return "The RTCP goodbye body is required.";
        }

        if (goodbye.Sources == null)
        {
            return "The RTCP goodbye source array is required.";
        }

        if (goodbye.Sources.Length > RtcpProtocolConstants.MaximumSourceCount)
        {
            return "An RTCP goodbye packet cannot contain more than 31 sources.";
        }

        if (goodbye.Reason == null)
        {
            return "The RTCP goodbye reason array is required.";
        }

        if (!goodbye.HasReason)
        {
            if (goodbye.Reason.Length != 0)
            {
                return "RTCP goodbye reason bytes require the reason-length field.";
            }

            return null;
        }

        if (goodbye.Reason.Length > RtcpProtocolConstants.MaximumTextLength)
        {
            return "An RTCP goodbye reason cannot exceed 255 bytes.";
        }

        if (!RtpKnownHeaderExtensionWireCodec.IsValidUtf8(goodbye.Reason))
        {
            return "The RTCP goodbye reason is not valid UTF-8.";
        }

        return null;
    }

    internal static string? GetGoodbyeDecodeError(
        ReadOnlySpan<byte> body,
        int sourceCount)
    {
        if (sourceCount < 0 || sourceCount > RtcpProtocolConstants.MaximumSourceCount)
        {
            return "An RTCP goodbye packet may contain between 0 and 31 sources.";
        }

        int sourcesSize = sourceCount * RtcpProtocolConstants.SourceIdentifierSize;
        if (body.Length < sourcesSize)
        {
            return "The RTCP goodbye packet is truncated before its declared sources.";
        }

        if (body.Length == sourcesSize)
        {
            return null;
        }

        int reasonLength = NetworkBinaryReader.ReadByte(body, sourcesSize);
        int reasonSectionSize = CalculateGoodbyeReasonSectionSize(reasonLength);
        int expectedBodySize = sourcesSize + reasonSectionSize;
        if (body.Length != expectedBodySize)
        {
            return "The RTCP goodbye reason length does not match the packet length.";
        }

        int reasonOffset = sourcesSize + 1;
        if (!RtpKnownHeaderExtensionWireCodec.IsValidUtf8(
            body.Slice(reasonOffset, reasonLength)))
        {
            return "The RTCP goodbye reason is not valid UTF-8.";
        }

        int paddingOffset = reasonOffset + reasonLength;
        while (paddingOffset < body.Length)
        {
            if (NetworkBinaryReader.ReadByte(body, paddingOffset) != 0)
            {
                return "The RTCP goodbye reason padding must be zero.";
            }

            paddingOffset = paddingOffset + 1;
        }

        return null;
    }

    internal static int CalculateGoodbyeReasonSectionSize(int reasonLength)
    {
        int sectionSize = 1 + reasonLength;
        int remainder = sectionSize % RtcpProtocolConstants.LengthWordSize;
        if (remainder != 0)
        {
            sectionSize = sectionSize +
                RtcpProtocolConstants.LengthWordSize - remainder;
        }

        return sectionSize;
    }

    internal static long CalculateGoodbyeBodySize(RtcpGoodbye goodbye)
    {
        long bodySize = (long)goodbye.Sources.Length *
            RtcpProtocolConstants.SourceIdentifierSize;
        if (goodbye.HasReason)
        {
            bodySize = bodySize + CalculateGoodbyeReasonSectionSize(
                goodbye.Reason.Length);
        }

        return bodySize;
    }

    internal static RtcpGoodbye DecodeGoodbye(
        ReadOnlySpan<byte> body,
        int sourceCount)
    {
        RtcpGoodbye goodbye = new RtcpGoodbye();
        goodbye.Sources = new uint[sourceCount];
        int sourceIndex = 0;
        while (sourceIndex < sourceCount)
        {
            goodbye.Sources[sourceIndex] = NetworkBinaryReader.ReadUInt32(
                body,
                sourceIndex * RtcpProtocolConstants.SourceIdentifierSize);
            sourceIndex = sourceIndex + 1;
        }

        int sourcesSize = sourceCount * RtcpProtocolConstants.SourceIdentifierSize;
        if (body.Length > sourcesSize)
        {
            goodbye.HasReason = true;
            int reasonLength = NetworkBinaryReader.ReadByte(body, sourcesSize);
            goodbye.Reason = body.Slice(sourcesSize + 1, reasonLength).ToArray();
        }

        return goodbye;
    }

    internal static void EncodeGoodbye(RtcpGoodbye goodbye, Span<byte> destination)
    {
        int sourceIndex = 0;
        while (sourceIndex < goodbye.Sources.Length)
        {
            NetworkBinaryWriter.WriteUInt32(
                destination,
                sourceIndex * RtcpProtocolConstants.SourceIdentifierSize,
                goodbye.Sources[sourceIndex]);
            sourceIndex = sourceIndex + 1;
        }

        if (goodbye.HasReason)
        {
            int offset = goodbye.Sources.Length *
                RtcpProtocolConstants.SourceIdentifierSize;
            NetworkBinaryWriter.WriteByte(
                destination,
                offset,
                (byte)goodbye.Reason.Length);
            offset = offset + 1;
            NetworkBinaryWriter.WriteBytes(destination, offset, goodbye.Reason);
            offset = offset + goodbye.Reason.Length;
            int bodySize = (int)CalculateGoodbyeBodySize(goodbye);
            while (offset < bodySize)
            {
                NetworkBinaryWriter.WriteByte(destination, offset, 0);
                offset = offset + 1;
            }
        }
    }
}
