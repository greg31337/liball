namespace jpeg;

internal class JpegMpfMetadataParser
{
    internal bool IsMpf(byte[]? payload)
    {
        if (payload == null)
        {
            return false;
        }

        if (payload.Length < JpegConstants.MpfIdentifierLength)
        {
            return false;
        }

        return payload[0] == 0x4D
            && payload[1] == 0x50
            && payload[2] == 0x46
            && payload[3] == 0;
    }

    internal JpegMpfMetadata Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (!IsMpf(payload))
        {
            throw JpegInvalidDataException.Create("APP2 payload is not an MPF segment.");
        }

        JpegMpfMetadata metadata = new JpegMpfMetadata();
        metadata.IsPresent = true;
        metadata.RawPayload = CopyBytes(payload, 0, payload.Length);
        metadata.TiffData = CopyBytes(payload, JpegConstants.MpfIdentifierLength, payload.Length - JpegConstants.MpfIdentifierLength);
        return metadata;
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

internal class JpegMpfMetadataWriter
{
    internal byte[] WritePayload(JpegMpfMetadata metadata)
    {
        Validate(metadata);
        byte[] payload = new byte[metadata.RawPayload.Length];
        if (payload.Length > 0)
        {
            System.Array.Copy(metadata.RawPayload, 0, payload, 0, metadata.RawPayload.Length);
        }

        return payload;
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, JpegMpfMetadata metadata)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        byte[] payload = WritePayload(metadata);
        segmentWriter.WriteMarkerSegment(JpegMarker.App2, payload);
    }

    internal void Validate(JpegMpfMetadata metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        JpegMpfMetadataParser parser = new JpegMpfMetadataParser();
        if (!parser.IsMpf(metadata.RawPayload))
        {
            throw JpegWriteValidationException.Create("MPF metadata must contain an APP2 MPF payload.");
        }

        if (metadata.RawPayload.Length > JpegConstants.MaximumMarkerPayloadLength)
        {
            throw JpegWriteValidationException.Create("MPF APP2 payload exceeds the maximum marker payload length.");
        }
    }
}
