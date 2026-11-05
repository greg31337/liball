namespace jpeg;

internal class JpegJfifMetadataParser
{
    internal bool IsJfif(byte[]? payload)
    {
        if (payload == null)
        {
            return false;
        }

        if (payload.Length < 5)
        {
            return false;
        }

        return payload[0] == 0x4A
            && payload[1] == 0x46
            && payload[2] == 0x49
            && payload[3] == 0x46
            && payload[4] == 0;
    }

    internal JpegJfifMetadata Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (!IsJfif(payload))
        {
            throw JpegInvalidDataException.Create("APP0 payload is not a JFIF segment.");
        }

        if (payload.Length < 14)
        {
            throw JpegInvalidDataException.Create("JFIF APP0 payload is too short.");
        }

        JpegJfifMetadata metadata = new JpegJfifMetadata();
        metadata.IsPresent = true;
        metadata.MajorVersion = payload[5];
        metadata.MinorVersion = payload[6];
        metadata.DensityUnit = payload[7];
        metadata.XDensity = JpegByteReader.ReadUInt16(payload, 8);
        metadata.YDensity = JpegByteReader.ReadUInt16(payload, 10);
        metadata.ThumbnailWidth = payload[12];
        metadata.ThumbnailHeight = payload[13];
        Validate(metadata, payload.Length - 14);
        CopyThumbnail(metadata, payload);
        JpegJfifDensityMapper mapper = new JpegJfifDensityMapper();
        mapper.ApplyDpi(metadata);
        return metadata;
    }

    internal void Validate(JpegJfifMetadata metadata, int availableThumbnailBytes)
    {
        if (metadata.DensityUnit < 0 || metadata.DensityUnit > 2)
        {
            throw JpegInvalidDataException.Create("JFIF density unit must be 0, 1, or 2.");
        }

        int thumbnailBytes = metadata.ThumbnailWidth * metadata.ThumbnailHeight * 3;
        if (thumbnailBytes != availableThumbnailBytes)
        {
            throw JpegInvalidDataException.Create("JFIF thumbnail byte count does not match thumbnail dimensions.");
        }
    }

    internal void CopyThumbnail(JpegJfifMetadata metadata, byte[] payload)
    {
        int thumbnailBytes = metadata.ThumbnailWidth * metadata.ThumbnailHeight * 3;
        metadata.ThumbnailData = new byte[thumbnailBytes];
        metadata.RgbThumbnailPixels = new byte[thumbnailBytes];
        metadata.HasRgbThumbnail = thumbnailBytes > 0;
        if (thumbnailBytes > 0)
        {
            System.Array.Copy(payload, 14, metadata.ThumbnailData, 0, thumbnailBytes);
            System.Array.Copy(payload, 14, metadata.RgbThumbnailPixels, 0, thumbnailBytes);
        }
    }
}

internal class JpegJfifMetadataWriter
{
    internal byte[] WritePayload(JpegJfifMetadata metadata)
    {
        Validate(metadata);
        int thumbnailBytes = metadata.ThumbnailWidth * metadata.ThumbnailHeight * 3;
        byte[] thumbnailData = GetThumbnailBytes(metadata);
        JpegByteWriter writer = JpegByteWriter.WithCapacity(14 + thumbnailBytes);
        writer.WriteByte(0x4A);
        writer.WriteByte(0x46);
        writer.WriteByte(0x49);
        writer.WriteByte(0x46);
        writer.WriteByte(0);
        writer.WriteByte(metadata.MajorVersion);
        writer.WriteByte(metadata.MinorVersion);
        writer.WriteByte(metadata.DensityUnit);
        writer.WriteUInt16(metadata.XDensity);
        writer.WriteUInt16(metadata.YDensity);
        writer.WriteByte(metadata.ThumbnailWidth);
        writer.WriteByte(metadata.ThumbnailHeight);
        if (thumbnailBytes > 0)
        {
            writer.WriteBytesRange(thumbnailData, 0, thumbnailBytes);
        }

        return writer.ToArray();
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, JpegJfifMetadata metadata)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        byte[] payload = WritePayload(metadata);
        segmentWriter.WriteMarkerSegment(JpegMarker.App0, payload);
    }

    internal void Validate(JpegJfifMetadata metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        if (metadata.MajorVersion < 0 || metadata.MajorVersion > 255)
        {
            throw JpegWriteValidationException.Create("JFIF major version must fit in one byte.");
        }

        if (metadata.MinorVersion < 0 || metadata.MinorVersion > 255)
        {
            throw JpegWriteValidationException.Create("JFIF minor version must fit in one byte.");
        }

        if (metadata.DensityUnit < 0 || metadata.DensityUnit > 2)
        {
            throw JpegWriteValidationException.Create("JFIF density unit must be 0, 1, or 2.");
        }

        if (metadata.XDensity < 0 || metadata.XDensity > 65535)
        {
            throw JpegWriteValidationException.Create("JFIF X density must fit in two bytes.");
        }

        if (metadata.YDensity < 0 || metadata.YDensity > 65535)
        {
            throw JpegWriteValidationException.Create("JFIF Y density must fit in two bytes.");
        }

        if (metadata.ThumbnailWidth < 0 || metadata.ThumbnailWidth > 255)
        {
            throw JpegWriteValidationException.Create("JFIF thumbnail width must fit in one byte.");
        }

        if (metadata.ThumbnailHeight < 0 || metadata.ThumbnailHeight > 255)
        {
            throw JpegWriteValidationException.Create("JFIF thumbnail height must fit in one byte.");
        }

        if (metadata.ThumbnailData == null)
        {
            throw JpegWriteValidationException.Create("JFIF thumbnail data cannot be null.");
        }

        if (metadata.RgbThumbnailPixels == null)
        {
            throw JpegWriteValidationException.Create("JFIF RGB thumbnail pixels cannot be null.");
        }

        int thumbnailBytes = metadata.ThumbnailWidth * metadata.ThumbnailHeight * 3;
        if (thumbnailBytes > JpegConstants.MaximumMarkerPayloadLength - 14)
        {
            throw JpegWriteValidationException.Create("JFIF RGB thumbnail exceeds the APP0 marker payload limit.");
        }

        if (metadata.HasRgbThumbnail && thumbnailBytes == 0)
        {
            throw JpegWriteValidationException.Create("JFIF RGB thumbnail presence requires nonzero dimensions.");
        }

        byte[] thumbnailData = metadata.ThumbnailData;
        if (metadata.HasRgbThumbnail)
        {
            thumbnailData = metadata.RgbThumbnailPixels;
        }

        if (thumbnailData.Length != thumbnailBytes)
        {
            throw JpegWriteValidationException.Create("JFIF RGB thumbnail data must exactly match its dimensions.");
        }
    }

    internal byte[] GetThumbnailBytes(JpegJfifMetadata metadata)
    {
        if (metadata.HasRgbThumbnail)
        {
            return metadata.RgbThumbnailPixels;
        }

        return metadata.ThumbnailData;
    }
}

internal class JpegJfifDensityMapper
{
    internal bool HasAbsoluteDensity(JpegJfifMetadata metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        return metadata.DensityUnit == 1 || metadata.DensityUnit == 2;
    }

    internal double ToDpi(int density, int densityUnit)
    {
        if (densityUnit == 0)
        {
            return 0.0;
        }

        if (densityUnit == 1)
        {
            return (double)density;
        }

        if (densityUnit == 2)
        {
            return (double)density * 2.54;
        }

        throw JpegInvalidDataException.Create("JFIF density unit must be 0, 1, or 2.");
    }

    internal void ApplyDpi(JpegJfifMetadata metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        metadata.HasDensityDpi = HasAbsoluteDensity(metadata);
        metadata.XDpi = ToDpi(metadata.XDensity, metadata.DensityUnit);
        metadata.YDpi = ToDpi(metadata.YDensity, metadata.DensityUnit);
    }

    internal void SetDpi(JpegJfifMetadata metadata, double xDpi, double yDpi)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        metadata.DensityUnit = 1;
        metadata.XDensity = FromDpi(xDpi, 1);
        metadata.YDensity = FromDpi(yDpi, 1);
        ApplyDpi(metadata);
    }

    internal int FromDpi(double dpi, int densityUnit)
    {
        if (densityUnit == 0)
        {
            return 0;
        }

        double density = dpi;
        if (densityUnit == 2)
        {
            density = dpi / 2.54;
        }
        else
        {
            if (densityUnit != 1)
            {
                throw JpegWriteValidationException.Create("JFIF density unit must be 0, 1, or 2.");
            }
        }

        if (density < 0.0 || density > 65535.0)
        {
            throw JpegWriteValidationException.Create("JFIF density converted from DPI must fit in two bytes.");
        }

        return (int)System.Math.Round(density);
    }
}
