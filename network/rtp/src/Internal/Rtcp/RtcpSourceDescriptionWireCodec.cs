using System;
using System.Collections.Generic;

namespace rtp;

internal static class RtcpSourceDescriptionWireCodec
{
    internal static string? GetSourceDescriptionValidationError(
        RtcpSourceDescription? description)
    {
        if (description == null)
        {
            return "The RTCP source-description body is required.";
        }

        if (description.Chunks == null)
        {
            return "The RTCP source-description chunk array is required.";
        }

        if (description.Chunks.Length > RtcpProtocolConstants.MaximumSourceCount)
        {
            return "An RTCP source-description packet cannot contain more than 31 chunks.";
        }

        int chunkIndex = 0;
        while (chunkIndex < description.Chunks.Length)
        {
            RtcpSourceDescriptionChunk? chunk = description.Chunks[chunkIndex];
            if (chunk == null)
            {
                return "An RTCP source-description chunk is required.";
            }

            if (chunk.Items == null)
            {
                return "The RTCP source-description item array is required.";
            }

            int itemIndex = 0;
            while (itemIndex < chunk.Items.Length)
            {
                string? itemError = GetSourceDescriptionItemValidationError(
                    chunk.Items[itemIndex]);
                if (itemError != null)
                {
                    return itemError;
                }

                itemIndex = itemIndex + 1;
            }

            chunkIndex = chunkIndex + 1;
        }

        return null;
    }

    internal static string? GetSourceDescriptionItemValidationError(
        RtcpSourceDescriptionItem? item)
    {
        if (item == null)
        {
            return "An RTCP source-description item is required.";
        }

        if (item.Value == null)
        {
            return "The RTCP source-description item value is required.";
        }

        if (item.PrivatePrefix == null)
        {
            return "The RTCP source-description private-prefix array is required.";
        }

        if (item.Type == RtcpSdesItemType.End)
        {
            return "The RTCP source-description END item is written implicitly.";
        }

        if (item.Type == RtcpSdesItemType.Private)
        {
            long privateLength = 1L + item.PrivatePrefix.Length + item.Value.Length;
            if (privateLength > RtcpProtocolConstants.MaximumTextLength)
            {
                return "The RTCP private item prefix and value exceed 255 bytes.";
            }

            if (!RtpKnownHeaderExtensionWireCodec.IsValidUtf8(item.PrivatePrefix))
            {
                return "The RTCP private item prefix is not valid UTF-8.";
            }

            if (!RtpKnownHeaderExtensionWireCodec.IsValidUtf8(item.Value))
            {
                return "The RTCP private item value is not valid UTF-8.";
            }

            return null;
        }

        if (item.PrivatePrefix.Length != 0)
        {
            return "Only an RTCP private item may contain a private prefix.";
        }

        if (item.Value.Length > RtcpProtocolConstants.MaximumTextLength)
        {
            return "An RTCP source-description item value cannot exceed 255 bytes.";
        }

        if (item.Type == RtcpSdesItemType.Unknown)
        {
            if (item.WireType <= (byte)RtcpSdesItemType.Private)
            {
                return "An unknown RTCP source-description item needs an unregistered wire type.";
            }

            return null;
        }

        if (item.Type != RtcpSdesItemType.CanonicalName &&
            item.Type != RtcpSdesItemType.Name &&
            item.Type != RtcpSdesItemType.Email &&
            item.Type != RtcpSdesItemType.Phone &&
            item.Type != RtcpSdesItemType.Location &&
            item.Type != RtcpSdesItemType.Tool &&
            item.Type != RtcpSdesItemType.Note)
        {
            return "The RTCP source-description item discriminator is invalid.";
        }

        if (!RtpKnownHeaderExtensionWireCodec.IsValidUtf8(item.Value))
        {
            return "The RTCP source-description item value is not valid UTF-8.";
        }

        return null;
    }

    internal static string? GetSourceDescriptionDecodeError(
        ReadOnlySpan<byte> body,
        int sourceCount)
    {
        if (sourceCount < 0 || sourceCount > RtcpProtocolConstants.MaximumSourceCount)
        {
            return "An RTCP source-description packet may contain between 0 and 31 chunks.";
        }

        int offset = 0;
        int chunkIndex = 0;
        while (chunkIndex < sourceCount)
        {
            if (body.Length - offset < RtcpProtocolConstants.SourceIdentifierSize)
            {
                return "The RTCP source-description packet is truncated before a chunk source.";
            }

            offset = offset + RtcpProtocolConstants.SourceIdentifierSize;
            bool foundEnd = false;
            while (!foundEnd)
            {
                if (offset >= body.Length)
                {
                    return "The RTCP source-description chunk has no END marker.";
                }

                byte wireType = NetworkBinaryReader.ReadByte(body, offset);
                if (wireType == RtcpProtocolConstants.SourceDescriptionEnd)
                {
                    offset = offset + 1;
                    while (offset % RtcpProtocolConstants.LengthWordSize != 0)
                    {
                        if (offset >= body.Length)
                        {
                            return "The RTCP source-description chunk alignment is truncated.";
                        }

                        if (NetworkBinaryReader.ReadByte(body, offset) !=
                            RtcpProtocolConstants.SourceDescriptionEnd)
                        {
                            return "The RTCP source-description chunk padding must be zero.";
                        }

                        offset = offset + 1;
                    }

                    foundEnd = true;
                }
                else
                {
                    if (body.Length - offset <
                        RtcpProtocolConstants.SourceDescriptionItemHeaderSize)
                    {
                        return "The RTCP source-description item header is truncated.";
                    }

                    int itemLength = NetworkBinaryReader.ReadByte(body, offset + 1);
                    int encodedItemLength =
                        RtcpProtocolConstants.SourceDescriptionItemHeaderSize + itemLength;
                    if (body.Length - offset < encodedItemLength)
                    {
                        return "The RTCP source-description item value is truncated.";
                    }

                    if (wireType == (byte)RtcpSdesItemType.Private)
                    {
                        if (itemLength == 0)
                        {
                            return "An RTCP private item must contain its prefix-length byte.";
                        }

                        int prefixLength = NetworkBinaryReader.ReadByte(body, offset + 2);
                        if (prefixLength > itemLength - 1)
                        {
                            return "The RTCP private item prefix exceeds its item length.";
                        }

                        if (!RtpKnownHeaderExtensionWireCodec.IsValidUtf8(
                            body.Slice(offset + 3, prefixLength)))
                        {
                            return "The RTCP private item prefix is not valid UTF-8.";
                        }

                        int valueLength = itemLength - prefixLength - 1;
                        if (!RtpKnownHeaderExtensionWireCodec.IsValidUtf8(
                            body.Slice(offset + 3 + prefixLength, valueLength)))
                        {
                            return "The RTCP private item value is not valid UTF-8.";
                        }
                    }
                    else if (wireType >= (byte)RtcpSdesItemType.CanonicalName &&
                        wireType <= (byte)RtcpSdesItemType.Note &&
                        !RtpKnownHeaderExtensionWireCodec.IsValidUtf8(
                            body.Slice(
                                offset + RtcpProtocolConstants.SourceDescriptionItemHeaderSize,
                                itemLength)))
                    {
                        return "The RTCP source-description item value is not valid UTF-8.";
                    }

                    offset = offset + encodedItemLength;
                }
            }

            chunkIndex = chunkIndex + 1;
        }

        if (offset != body.Length)
        {
            return "The RTCP source-description length does not match its chunk count.";
        }

        return null;
    }

    internal static long CalculateSourceDescriptionBodySize(
        RtcpSourceDescription description)
    {
        long bodySize = 0;
        int chunkIndex = 0;
        while (chunkIndex < description.Chunks.Length)
        {
            bodySize = bodySize + CalculateSourceDescriptionChunkSize(
                description.Chunks[chunkIndex]);
            chunkIndex = chunkIndex + 1;
        }

        return bodySize;
    }

    internal static long CalculateSourceDescriptionChunkSize(
        RtcpSourceDescriptionChunk chunk)
    {
        long chunkSize = RtcpProtocolConstants.SourceIdentifierSize + 1L;
        int itemIndex = 0;
        while (itemIndex < chunk.Items.Length)
        {
            chunkSize = chunkSize + CalculateSourceDescriptionItemSize(
                chunk.Items[itemIndex]);
            itemIndex = itemIndex + 1;
        }

        long remainder = chunkSize % RtcpProtocolConstants.LengthWordSize;
        if (remainder != 0)
        {
            chunkSize = chunkSize +
                RtcpProtocolConstants.LengthWordSize - remainder;
        }

        return chunkSize;
    }

    internal static int CalculateSourceDescriptionItemSize(RtcpSourceDescriptionItem item)
    {
        int payloadLength = item.Value.Length;
        if (item.Type == RtcpSdesItemType.Private)
        {
            payloadLength = payloadLength + item.PrivatePrefix.Length + 1;
        }

        return RtcpProtocolConstants.SourceDescriptionItemHeaderSize + payloadLength;
    }

    internal static RtcpSourceDescription DecodeSourceDescription(
        ReadOnlySpan<byte> body,
        int sourceCount)
    {
        RtcpSourceDescription description = new RtcpSourceDescription();
        description.Chunks = new RtcpSourceDescriptionChunk[sourceCount];
        int offset = 0;
        int chunkIndex = 0;
        while (chunkIndex < sourceCount)
        {
            RtcpSourceDescriptionChunk chunk = new RtcpSourceDescriptionChunk();
            chunk.SourceIdentifier = NetworkBinaryReader.ReadUInt32(body, offset);
            offset = offset + RtcpProtocolConstants.SourceIdentifierSize;
            List<RtcpSourceDescriptionItem> items =
                new List<RtcpSourceDescriptionItem>();
            bool foundEnd = false;
            while (!foundEnd)
            {
                byte wireType = NetworkBinaryReader.ReadByte(body, offset);
                if (wireType == RtcpProtocolConstants.SourceDescriptionEnd)
                {
                    offset = offset + 1;
                    while (offset % RtcpProtocolConstants.LengthWordSize != 0)
                    {
                        offset = offset + 1;
                    }

                    foundEnd = true;
                }
                else
                {
                    int itemLength = NetworkBinaryReader.ReadByte(body, offset + 1);
                    RtcpSourceDescriptionItem item = new RtcpSourceDescriptionItem();
                    item.Type = GetSourceDescriptionItemType(wireType);
                    item.WireType = wireType;
                    if (wireType == (byte)RtcpSdesItemType.Private)
                    {
                        int prefixLength = NetworkBinaryReader.ReadByte(body, offset + 2);
                        item.PrivatePrefix = body.Slice(offset + 3, prefixLength).ToArray();
                        item.Value = body.Slice(
                            offset + 3 + prefixLength,
                            itemLength - prefixLength - 1).ToArray();
                    }
                    else
                    {
                        item.Value = body.Slice(
                            offset + RtcpProtocolConstants.SourceDescriptionItemHeaderSize,
                            itemLength).ToArray();
                    }

                    items.Add(item);
                    offset = offset +
                        RtcpProtocolConstants.SourceDescriptionItemHeaderSize + itemLength;
                }
            }

            chunk.Items = items.ToArray();
            description.Chunks[chunkIndex] = chunk;
            chunkIndex = chunkIndex + 1;
        }

        return description;
    }

    internal static void EncodeSourceDescription(
        RtcpSourceDescription description,
        Span<byte> destination)
    {
        int offset = 0;
        int chunkIndex = 0;
        while (chunkIndex < description.Chunks.Length)
        {
            RtcpSourceDescriptionChunk chunk = description.Chunks[chunkIndex];
            NetworkBinaryWriter.WriteUInt32(destination, offset, chunk.SourceIdentifier);
            offset = offset + RtcpProtocolConstants.SourceIdentifierSize;
            int itemIndex = 0;
            while (itemIndex < chunk.Items.Length)
            {
                RtcpSourceDescriptionItem item = chunk.Items[itemIndex];
                int itemLength = CalculateSourceDescriptionItemSize(item) -
                    RtcpProtocolConstants.SourceDescriptionItemHeaderSize;
                NetworkBinaryWriter.WriteByte(
                    destination,
                    offset,
                    GetSourceDescriptionItemWireType(item));
                NetworkBinaryWriter.WriteByte(destination, offset + 1, (byte)itemLength);
                if (item.Type == RtcpSdesItemType.Private)
                {
                    NetworkBinaryWriter.WriteByte(
                        destination,
                        offset + 2,
                        (byte)item.PrivatePrefix.Length);
                    NetworkBinaryWriter.WriteBytes(
                        destination,
                        offset + 3,
                        item.PrivatePrefix);
                    NetworkBinaryWriter.WriteBytes(
                        destination,
                        offset + 3 + item.PrivatePrefix.Length,
                        item.Value);
                }
                else
                {
                    NetworkBinaryWriter.WriteBytes(
                        destination,
                        offset + RtcpProtocolConstants.SourceDescriptionItemHeaderSize,
                        item.Value);
                }

                offset = offset +
                    RtcpProtocolConstants.SourceDescriptionItemHeaderSize + itemLength;
                itemIndex = itemIndex + 1;
            }

            NetworkBinaryWriter.WriteByte(
                destination,
                offset,
                RtcpProtocolConstants.SourceDescriptionEnd);
            offset = offset + 1;
            while (offset % RtcpProtocolConstants.LengthWordSize != 0)
            {
                NetworkBinaryWriter.WriteByte(
                    destination,
                    offset,
                    RtcpProtocolConstants.SourceDescriptionEnd);
                offset = offset + 1;
            }

            chunkIndex = chunkIndex + 1;
        }
    }

    internal static RtcpSdesItemType GetSourceDescriptionItemType(byte wireType)
    {
        if (wireType >= (byte)RtcpSdesItemType.CanonicalName &&
            wireType <= (byte)RtcpSdesItemType.Private)
        {
            return (RtcpSdesItemType)wireType;
        }

        return RtcpSdesItemType.Unknown;
    }

    internal static byte GetSourceDescriptionItemWireType(
        RtcpSourceDescriptionItem item)
    {
        if (item.Type == RtcpSdesItemType.Unknown)
        {
            return item.WireType;
        }

        return (byte)item.Type;
    }
}
