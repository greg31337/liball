namespace jpeg;

internal class JpegPhotoshopMetadataParser
{
    internal bool IsPhotoshop(byte[]? payload)
    {
        if (payload == null)
        {
            return false;
        }

        if (payload.Length < JpegConstants.PhotoshopIdentifierLength)
        {
            return false;
        }

        byte[] identifier = GetIdentifier();
        int index = 0;
        while (index < identifier.Length)
        {
            if (payload[index] != identifier[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal JpegPhotoshopMetadata Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (!IsPhotoshop(payload))
        {
            throw JpegInvalidDataException.Create("APP13 payload is not a Photoshop segment.");
        }

        JpegPhotoshopMetadata metadata = new JpegPhotoshopMetadata();
        metadata.IsPresent = true;
        metadata.RawPayload = CopyBytes(payload, 0, payload.Length);
        ParseResourceBlocks(metadata, payload);
        return metadata;
    }

    internal void ParseResourceBlocks(JpegPhotoshopMetadata metadata, byte[] payload)
    {
        int offset = JpegConstants.PhotoshopIdentifierLength;
        while (offset < payload.Length)
        {
            JpegPhotoshopResourceBlock block = ParseResourceBlock(payload, offset);
            metadata.ResourceBlocks.Add(block);
            if (block.IsIptc)
            {
                metadata.HasIptc = true;
                metadata.IptcData = CopyBytes(block.Data, 0, block.Data.Length);
            }

            offset = GetNextResourceOffset(block);
        }
    }

    internal JpegPhotoshopResourceBlock ParseResourceBlock(byte[] payload, int offset)
    {
        JpegByteReader.ValidateByteArrayRange(payload, offset, 7);
        JpegPhotoshopResourceBlock block = new JpegPhotoshopResourceBlock();
        block.IsPresent = true;
        block.Offset = offset;
        block.Signature = System.Text.Encoding.ASCII.GetString(payload, offset, 4);
        if (block.Signature != "8BIM" && block.Signature != "8B64")
        {
            throw JpegInvalidDataException.Create("Photoshop resource block signature is invalid.");
        }

        block.ResourceId = JpegByteReader.ReadUInt16(payload, offset + 4);
        int nameLength = payload[offset + 6];
        int nameStart = offset + 7;
        JpegByteReader.ValidateByteArrayRange(payload, nameStart, nameLength);
        block.Name = System.Text.Encoding.ASCII.GetString(payload, nameStart, nameLength);
        int nameStorageLength = 1 + nameLength;
        if (nameStorageLength % 2 != 0)
        {
            nameStorageLength = nameStorageLength + 1;
        }

        int sizeOffset = offset + 6 + nameStorageLength;
        JpegByteReader.ValidateByteArrayRange(payload, sizeOffset, 4);
        block.Size = JpegByteReader.ReadUInt32(payload, sizeOffset);
        block.DataOffset = sizeOffset + 4;
        if (block.Size > 2147483647L)
        {
            throw JpegInvalidDataException.Create("Photoshop resource block data is too large.");
        }

        JpegByteReader.ValidateByteArrayRange(payload, (int)block.DataOffset, (int)block.Size);
        block.Data = CopyBytes(payload, (int)block.DataOffset, (int)block.Size);
        block.IsIptc = block.ResourceId == JpegConstants.PhotoshopIptcResourceId;
        return block;
    }

    internal int GetNextResourceOffset(JpegPhotoshopResourceBlock block)
    {
        long next = block.DataOffset + block.Size;
        if (block.Size % 2 != 0)
        {
            next = next + 1;
        }

        if (next > 2147483647L)
        {
            throw JpegInvalidDataException.Create("Photoshop resource block offset is too large.");
        }

        return (int)next;
    }

    internal byte[] GetIdentifier()
    {
        return new byte[]
        {
            0x50, 0x68, 0x6F, 0x74, 0x6F, 0x73, 0x68,
            0x6F, 0x70, 0x20, 0x33, 0x2E, 0x30, 0
        };
    }

    internal byte[] CopyBytes(byte[] data, int offset, int length)
    {
        byte[] copy = new byte[length];
        if (length > 0)
        {
            System.Array.Copy(data, offset, copy, 0, length);
        }

        return copy;
    }
}

internal class JpegPhotoshopMetadataWriter
{
    internal byte[] WritePayload(JpegPhotoshopMetadata metadata)
    {
        Validate(metadata);
        byte[] payload = new byte[metadata.RawPayload.Length];
        if (payload.Length > 0)
        {
            System.Array.Copy(metadata.RawPayload, 0, payload, 0, metadata.RawPayload.Length);
        }

        return payload;
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, JpegPhotoshopMetadata metadata)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        byte[] payload = WritePayload(metadata);
        segmentWriter.WriteMarkerSegment(JpegMarker.App13, payload);
    }

    internal void Validate(JpegPhotoshopMetadata metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        JpegPhotoshopMetadataParser parser = new JpegPhotoshopMetadataParser();
        if (!parser.IsPhotoshop(metadata.RawPayload))
        {
            throw JpegWriteValidationException.Create("Photoshop metadata must contain an APP13 Photoshop payload.");
        }

        if (metadata.RawPayload.Length > JpegConstants.MaximumMarkerPayloadLength)
        {
            throw JpegWriteValidationException.Create("Photoshop APP13 payload exceeds the maximum marker payload length.");
        }
    }
}
