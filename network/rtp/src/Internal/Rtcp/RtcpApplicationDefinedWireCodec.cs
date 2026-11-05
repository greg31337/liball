using System;

namespace rtp;

internal static class RtcpApplicationDefinedWireCodec
{
    internal static string? GetApplicationDefinedValidationError(
        RtcpApplicationDefined? application)
    {
        if (application == null)
        {
            return "The RTCP application-defined body is required.";
        }

        if (application.Subtype > RtcpProtocolConstants.MaximumCount)
        {
            return "The RTCP application-defined subtype exceeds five bits.";
        }

        if (application.Name == null)
        {
            return "The RTCP application-defined name array is required.";
        }

        if (application.Name.Length != RtcpProtocolConstants.ApplicationNameSize)
        {
            return "The RTCP application-defined name must contain exactly four bytes.";
        }

        int nameIndex = 0;
        while (nameIndex < application.Name.Length)
        {
            if (application.Name[nameIndex] > 127)
            {
                return "The RTCP application-defined name must contain only ASCII bytes.";
            }

            nameIndex = nameIndex + 1;
        }

        if (application.Data == null)
        {
            return "The RTCP application-defined data array is required.";
        }

        if (application.Data.Length % RtcpProtocolConstants.LengthWordSize != 0)
        {
            return "The RTCP application-defined data must be word-aligned.";
        }

        return null;
    }

    internal static string? GetApplicationDefinedDecodeError(ReadOnlySpan<byte> body)
    {
        int minimumBodySize = RtcpProtocolConstants.ApplicationMinimumSize -
            RtcpProtocolConstants.HeaderSize;
        if (body.Length < minimumBodySize)
        {
            return "The RTCP application-defined packet is shorter than its fixed fields.";
        }

        if ((body.Length - minimumBodySize) %
            RtcpProtocolConstants.LengthWordSize != 0)
        {
            return "The RTCP application-defined data must be word-aligned.";
        }

        int nameOffset = RtcpProtocolConstants.SourceIdentifierSize;
        int nameIndex = 0;
        while (nameIndex < RtcpProtocolConstants.ApplicationNameSize)
        {
            if (NetworkBinaryReader.ReadByte(body, nameOffset + nameIndex) > 127)
            {
                return "The RTCP application-defined name must contain only ASCII bytes.";
            }

            nameIndex = nameIndex + 1;
        }

        return null;
    }

    internal static long CalculateApplicationDefinedBodySize(
        RtcpApplicationDefined application)
    {
        return RtcpProtocolConstants.ApplicationMinimumSize -
            RtcpProtocolConstants.HeaderSize + (long)application.Data.Length;
    }

    internal static RtcpApplicationDefined DecodeApplicationDefined(
        ReadOnlySpan<byte> body,
        byte subtype)
    {
        int nameOffset = RtcpProtocolConstants.SourceIdentifierSize;
        int dataOffset = nameOffset + RtcpProtocolConstants.ApplicationNameSize;
        RtcpApplicationDefined application = new RtcpApplicationDefined();
        application.Subtype = subtype;
        application.SourceIdentifier = NetworkBinaryReader.ReadUInt32(body, 0);
        application.Name = body.Slice(
            nameOffset,
            RtcpProtocolConstants.ApplicationNameSize).ToArray();
        application.Data = body.Slice(dataOffset).ToArray();
        return application;
    }

    internal static void EncodeApplicationDefined(
        RtcpApplicationDefined application,
        Span<byte> destination)
    {
        int nameOffset = RtcpProtocolConstants.SourceIdentifierSize;
        int dataOffset = nameOffset + RtcpProtocolConstants.ApplicationNameSize;
        NetworkBinaryWriter.WriteUInt32(
            destination,
            0,
            application.SourceIdentifier);
        NetworkBinaryWriter.WriteBytes(destination, nameOffset, application.Name);
        NetworkBinaryWriter.WriteBytes(destination, dataOffset, application.Data);
    }
}
