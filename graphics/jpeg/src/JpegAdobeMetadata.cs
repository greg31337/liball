namespace jpeg;

internal class JpegAdobeMetadataParser
{
    internal bool IsAdobe(byte[]? payload)
    {
        if (payload == null)
        {
            return false;
        }

        if (payload.Length < 5)
        {
            return false;
        }

        return payload[0] == 0x41
            && payload[1] == 0x64
            && payload[2] == 0x6F
            && payload[3] == 0x62
            && payload[4] == 0x65;
    }

    internal JpegAdobeMetadata Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (!IsAdobe(payload))
        {
            throw JpegInvalidDataException.Create("APP14 payload is not an Adobe segment.");
        }

        if (payload.Length != 12)
        {
            throw JpegInvalidDataException.Create("Adobe APP14 payload must contain exactly 12 bytes.");
        }

        JpegAdobeMetadata metadata = new JpegAdobeMetadata();
        metadata.IsPresent = true;
        metadata.Version = JpegByteReader.ReadUInt16(payload, 5);
        metadata.Flags0 = JpegByteReader.ReadUInt16(payload, 7);
        metadata.Flags1 = JpegByteReader.ReadUInt16(payload, 9);
        metadata.Transform = payload[11];
        Validate(metadata);
        ApplyTransformFlags(metadata);
        return metadata;
    }

    internal void Validate(JpegAdobeMetadata metadata)
    {
        if (metadata.Transform == JpegConstants.AdobeTransformUnknown)
        {
            return;
        }

        if (metadata.Transform == JpegConstants.AdobeTransformYCbCr)
        {
            return;
        }

        if (metadata.Transform == JpegConstants.AdobeTransformYCCK)
        {
            return;
        }

        throw JpegInvalidDataException.Create("Adobe APP14 transform must be 0, 1, or 2.");
    }

    internal void ApplyTransformFlags(JpegAdobeMetadata metadata)
    {
        metadata.IsUnknownTransform = metadata.Transform == JpegConstants.AdobeTransformUnknown;
        metadata.IsYCbCrTransform = metadata.Transform == JpegConstants.AdobeTransformYCbCr;
        metadata.IsYCCKTransform = metadata.Transform == JpegConstants.AdobeTransformYCCK;
        metadata.TransformColorSpace = InterpretTransformColorSpace(metadata.Transform);
    }

    internal JpegColorSpace InterpretTransformColorSpace(int transform)
    {
        if (transform == JpegConstants.AdobeTransformYCbCr)
        {
            return JpegColorSpace.YCbCr;
        }

        if (transform == JpegConstants.AdobeTransformYCCK)
        {
            return JpegColorSpace.YCCK;
        }

        return JpegColorSpace.Unknown;
    }

    internal void ApplyComponentInterpretation(JpegAdobeMetadata metadata, int componentCount)
    {
        if (metadata.Transform == JpegConstants.AdobeTransformYCbCr)
        {
            metadata.ColorSpace = JpegColorSpace.YCbCr;
            metadata.InvertsCmyk = false;
            return;
        }

        if (metadata.Transform == JpegConstants.AdobeTransformYCCK)
        {
            metadata.ColorSpace = JpegColorSpace.YCCK;
            metadata.InvertsCmyk = true;
            return;
        }

        metadata.ColorSpace = InterpretUnknownTransformColorSpace(componentCount);
        metadata.InvertsCmyk = metadata.ColorSpace == JpegColorSpace.CMYK;
    }

    internal JpegColorSpace InterpretUnknownTransformColorSpace(int componentCount)
    {
        if (componentCount == 1)
        {
            return JpegColorSpace.Grayscale;
        }

        if (componentCount == 3)
        {
            return JpegColorSpace.RGB;
        }

        if (componentCount == 4)
        {
            return JpegColorSpace.CMYK;
        }

        return JpegColorSpace.Unknown;
    }
}

internal class JpegAdobeMetadataWriter
{
    internal byte[] WritePayload(JpegAdobeMetadata metadata)
    {
        Validate(metadata);
        byte[] payload = new byte[12];
        payload[0] = 0x41;
        payload[1] = 0x64;
        payload[2] = 0x6F;
        payload[3] = 0x62;
        payload[4] = 0x65;
        JpegByteWriter.WriteUInt16(payload, 5, metadata.Version);
        JpegByteWriter.WriteUInt16(payload, 7, metadata.Flags0);
        JpegByteWriter.WriteUInt16(payload, 9, metadata.Flags1);
        payload[11] = (byte)metadata.Transform;
        return payload;
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, JpegAdobeMetadata metadata)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        byte[] payload = WritePayload(metadata);
        segmentWriter.WriteMarkerSegment(JpegMarker.App14, payload);
    }

    internal void Validate(JpegAdobeMetadata metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        if (metadata.Version < 0 || metadata.Version > 65535)
        {
            throw JpegWriteValidationException.Create("Adobe APP14 version must fit in two bytes.");
        }

        if (metadata.Flags0 < 0 || metadata.Flags0 > 65535)
        {
            throw JpegWriteValidationException.Create("Adobe APP14 flags0 must fit in two bytes.");
        }

        if (metadata.Flags1 < 0 || metadata.Flags1 > 65535)
        {
            throw JpegWriteValidationException.Create("Adobe APP14 flags1 must fit in two bytes.");
        }

        ValidateTransform(metadata);
    }

    internal void ValidateTransform(JpegAdobeMetadata metadata)
    {
        if (metadata.Transform == JpegConstants.AdobeTransformUnknown)
        {
            return;
        }

        if (metadata.Transform == JpegConstants.AdobeTransformYCbCr)
        {
            return;
        }

        if (metadata.Transform == JpegConstants.AdobeTransformYCCK)
        {
            return;
        }

        throw JpegWriteValidationException.Create("Adobe APP14 transform must be 0, 1, or 2.");
    }
}
