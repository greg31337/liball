namespace jpeg;

internal class JpegJfxxMetadataParser
{
    internal bool IsJfxx(byte[]? payload)
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
            && payload[2] == 0x58
            && payload[3] == 0x58
            && payload[4] == 0;
    }

    internal JpegJfxxMetadata Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (!IsJfxx(payload))
        {
            throw JpegInvalidDataException.Create("APP0 payload is not a JFXX segment.");
        }

        if (payload.Length < 6)
        {
            throw JpegInvalidDataException.Create("JFXX APP0 payload is too short.");
        }

        JpegJfxxMetadata metadata = new JpegJfxxMetadata();
        metadata.IsPresent = true;
        metadata.ExtensionCode = payload[5];
        Validate(metadata);
        ApplyExtensionFlags(metadata);
        CopyExtensionData(metadata, payload);
        ParseExtensionData(metadata);
        return metadata;
    }

    internal void Validate(JpegJfxxMetadata metadata)
    {
        if (metadata.ExtensionCode == JpegConstants.JfxxExtensionJpegThumbnail)
        {
            return;
        }

        if (metadata.ExtensionCode == JpegConstants.JfxxExtensionIndexedThumbnail)
        {
            return;
        }

        if (metadata.ExtensionCode == JpegConstants.JfxxExtensionRgbThumbnail)
        {
            return;
        }

        throw JpegInvalidDataException.Create("JFXX extension code must identify a standard thumbnail extension.");
    }

    internal void CopyExtensionData(JpegJfxxMetadata metadata, byte[] payload)
    {
        int extensionDataLength = payload.Length - 6;
        metadata.ExtensionData = new byte[extensionDataLength];
        if (extensionDataLength > 0)
        {
            System.Array.Copy(payload, 6, metadata.ExtensionData, 0, extensionDataLength);
        }
    }

    internal void ApplyExtensionFlags(JpegJfxxMetadata metadata)
    {
        metadata.IsJpegThumbnail = metadata.ExtensionCode == JpegConstants.JfxxExtensionJpegThumbnail;
        metadata.IsIndexedThumbnail = metadata.ExtensionCode == JpegConstants.JfxxExtensionIndexedThumbnail;
        metadata.IsRgbThumbnail = metadata.ExtensionCode == JpegConstants.JfxxExtensionRgbThumbnail;
    }

    internal void ParseExtensionData(JpegJfxxMetadata metadata)
    {
        if (metadata.IsRgbThumbnail)
        {
            ParseRgbThumbnail(metadata);
        }

        if (metadata.IsIndexedThumbnail)
        {
            ParseIndexedThumbnail(metadata);
        }

        if (metadata.IsJpegThumbnail)
        {
            ParseJpegThumbnail(metadata);
        }
    }

    internal void ParseJpegThumbnail(JpegJfxxMetadata metadata)
    {
        if (!IsJpegThumbnailData(metadata.ExtensionData))
        {
            throw JpegInvalidDataException.Create("JFXX JPEG thumbnail data must contain a complete JPEG stream.");
        }

        metadata.JpegThumbnailData = new byte[metadata.ExtensionData.Length];
        System.Array.Copy(metadata.ExtensionData, 0, metadata.JpegThumbnailData, 0, metadata.ExtensionData.Length);
    }

    internal void ParseRgbThumbnail(JpegJfxxMetadata metadata)
    {
        if (metadata.ExtensionData.Length < 2)
        {
            throw JpegInvalidDataException.Create("JFXX RGB thumbnail payload is too short.");
        }

        metadata.ThumbnailWidth = metadata.ExtensionData[0];
        metadata.ThumbnailHeight = metadata.ExtensionData[1];
        int thumbnailBytes = metadata.ThumbnailWidth * metadata.ThumbnailHeight * 3;
        if (metadata.ExtensionData.Length != 2 + thumbnailBytes)
        {
            throw JpegInvalidDataException.Create("JFXX RGB thumbnail byte count does not match thumbnail dimensions.");
        }

        metadata.RgbThumbnailPixels = new byte[thumbnailBytes];
        if (thumbnailBytes > 0)
        {
            System.Array.Copy(metadata.ExtensionData, 2, metadata.RgbThumbnailPixels, 0, thumbnailBytes);
        }
    }

    internal void ParseIndexedThumbnail(JpegJfxxMetadata metadata)
    {
        if (metadata.ExtensionData.Length < 770)
        {
            throw JpegInvalidDataException.Create("JFXX indexed thumbnail payload is too short.");
        }

        metadata.ThumbnailWidth = metadata.ExtensionData[0];
        metadata.ThumbnailHeight = metadata.ExtensionData[1];
        int thumbnailBytes = metadata.ThumbnailWidth * metadata.ThumbnailHeight;
        if (metadata.ExtensionData.Length != 770 + thumbnailBytes)
        {
            throw JpegInvalidDataException.Create("JFXX indexed thumbnail byte count does not match thumbnail dimensions.");
        }

        metadata.IndexedThumbnailPalette = new byte[768];
        System.Array.Copy(metadata.ExtensionData, 2, metadata.IndexedThumbnailPalette, 0, 768);
        metadata.IndexedThumbnailPixels = new byte[thumbnailBytes];
        if (thumbnailBytes > 0)
        {
            System.Array.Copy(metadata.ExtensionData, 770, metadata.IndexedThumbnailPixels, 0, thumbnailBytes);
        }
    }

    internal bool IsJpegThumbnailData(byte[]? data)
    {
        if (data == null)
        {
            return false;
        }

        if (data.Length < 4)
        {
            return false;
        }

        if (data[0] != 0xFF || data[1] != 0xD8)
        {
            return false;
        }

        return data[data.Length - 2] == 0xFF && data[data.Length - 1] == 0xD9;
    }
}

internal class JpegJfxxMetadataWriter
{
    internal byte[] WritePayload(JpegJfxxMetadata metadata)
    {
        Validate(metadata);
        byte[] extensionData = GetExtensionData(metadata);
        JpegByteWriter writer = JpegByteWriter.WithCapacity(6 + extensionData.Length);
        writer.WriteByte(0x4A);
        writer.WriteByte(0x46);
        writer.WriteByte(0x58);
        writer.WriteByte(0x58);
        writer.WriteByte(0);
        writer.WriteByte(metadata.ExtensionCode);
        writer.WriteBytesRange(extensionData, 0, extensionData.Length);
        return writer.ToArray();
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, JpegJfxxMetadata metadata)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        byte[] payload = WritePayload(metadata);
        segmentWriter.WriteMarkerSegment(JpegMarker.App0, payload);
    }

    internal void Validate(JpegJfxxMetadata metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        ValidateExtensionCode(metadata);

        if (metadata.ExtensionData == null)
        {
            throw JpegWriteValidationException.Create("JFXX extension data cannot be null.");
        }

        if (metadata.JpegThumbnailData == null)
        {
            throw JpegWriteValidationException.Create("JFXX JPEG thumbnail data cannot be null.");
        }

        if (metadata.RgbThumbnailPixels == null)
        {
            throw JpegWriteValidationException.Create("JFXX RGB thumbnail pixels cannot be null.");
        }

        if (metadata.IndexedThumbnailPalette == null)
        {
            throw JpegWriteValidationException.Create("JFXX indexed thumbnail palette cannot be null.");
        }

        if (metadata.IndexedThumbnailPixels == null)
        {
            throw JpegWriteValidationException.Create("JFXX indexed thumbnail pixels cannot be null.");
        }

        if (metadata.ExtensionCode == JpegConstants.JfxxExtensionRgbThumbnail)
        {
            ValidateRgbThumbnail(metadata);
        }

        if (metadata.ExtensionCode == JpegConstants.JfxxExtensionIndexedThumbnail)
        {
            ValidateIndexedThumbnail(metadata);
        }

        if (metadata.ExtensionCode == JpegConstants.JfxxExtensionJpegThumbnail)
        {
            ValidateJpegThumbnail(metadata);
        }
    }

    internal void ValidateExtensionCode(JpegJfxxMetadata metadata)
    {
        if (metadata.ExtensionCode == JpegConstants.JfxxExtensionJpegThumbnail)
        {
            return;
        }

        if (metadata.ExtensionCode == JpegConstants.JfxxExtensionIndexedThumbnail)
        {
            return;
        }

        if (metadata.ExtensionCode == JpegConstants.JfxxExtensionRgbThumbnail)
        {
            return;
        }

        throw JpegWriteValidationException.Create("JFXX extension code must identify a standard thumbnail extension.");
    }

    internal void ValidateJpegThumbnail(JpegJfxxMetadata metadata)
    {
        byte[] thumbnailData = metadata.JpegThumbnailData;
        if (thumbnailData.Length == 0)
        {
            thumbnailData = metadata.ExtensionData;
        }

        JpegJfxxMetadataParser parser = new JpegJfxxMetadataParser();
        if (!parser.IsJpegThumbnailData(thumbnailData))
        {
            throw JpegWriteValidationException.Create("JFXX JPEG thumbnail data must contain a complete JPEG stream.");
        }

        if (thumbnailData.Length > JpegConstants.MaximumMarkerPayloadLength - 6)
        {
            throw JpegWriteValidationException.Create("JFXX JPEG thumbnail exceeds the APP0 marker payload limit.");
        }
    }

    internal void ValidateRgbThumbnail(JpegJfxxMetadata metadata)
    {
        if (metadata.ThumbnailWidth < 0 || metadata.ThumbnailWidth > 255)
        {
            throw JpegWriteValidationException.Create("JFXX RGB thumbnail width must fit in one byte.");
        }

        if (metadata.ThumbnailHeight < 0 || metadata.ThumbnailHeight > 255)
        {
            throw JpegWriteValidationException.Create("JFXX RGB thumbnail height must fit in one byte.");
        }

        int thumbnailBytes = metadata.ThumbnailWidth * metadata.ThumbnailHeight * 3;
        if (thumbnailBytes > JpegConstants.MaximumMarkerPayloadLength - 8)
        {
            throw JpegWriteValidationException.Create("JFXX RGB thumbnail exceeds the APP0 marker payload limit.");
        }

        if (metadata.RgbThumbnailPixels.Length > 0)
        {
            if (metadata.RgbThumbnailPixels.Length != thumbnailBytes)
            {
                throw JpegWriteValidationException.Create("JFXX RGB thumbnail pixels must exactly match its dimensions.");
            }

            return;
        }

        if (HasCompleteRgbExtensionData(metadata, thumbnailBytes))
        {
            return;
        }

        if (thumbnailBytes > 0)
        {
            throw JpegWriteValidationException.Create("JFXX RGB thumbnail pixels must exactly match its dimensions.");
        }
    }

    internal void ValidateIndexedThumbnail(JpegJfxxMetadata metadata)
    {
        if (metadata.ThumbnailWidth < 0 || metadata.ThumbnailWidth > 255)
        {
            throw JpegWriteValidationException.Create("JFXX indexed thumbnail width must fit in one byte.");
        }

        if (metadata.ThumbnailHeight < 0 || metadata.ThumbnailHeight > 255)
        {
            throw JpegWriteValidationException.Create("JFXX indexed thumbnail height must fit in one byte.");
        }

        int thumbnailBytes = metadata.ThumbnailWidth * metadata.ThumbnailHeight;
        if (thumbnailBytes > JpegConstants.MaximumMarkerPayloadLength - 776)
        {
            throw JpegWriteValidationException.Create("JFXX indexed thumbnail exceeds the APP0 marker payload limit.");
        }

        if (metadata.IndexedThumbnailPalette.Length > 0 || metadata.IndexedThumbnailPixels.Length > 0)
        {
            if (metadata.IndexedThumbnailPalette.Length != 768)
            {
                throw JpegWriteValidationException.Create("JFXX indexed thumbnail palette must contain exactly 256 RGB entries.");
            }

            if (metadata.IndexedThumbnailPixels.Length != thumbnailBytes)
            {
                throw JpegWriteValidationException.Create("JFXX indexed thumbnail pixels must exactly match its dimensions.");
            }

            return;
        }

        if (HasCompleteIndexedExtensionData(metadata, thumbnailBytes))
        {
            return;
        }

        throw JpegWriteValidationException.Create("JFXX indexed thumbnail data must contain a palette and every thumbnail pixel.");
    }

    internal byte[] GetExtensionData(JpegJfxxMetadata metadata)
    {
        if (metadata.ExtensionCode == JpegConstants.JfxxExtensionJpegThumbnail)
        {
            return GetJpegExtensionData(metadata);
        }

        if (metadata.ExtensionCode == JpegConstants.JfxxExtensionRgbThumbnail)
        {
            return GetRgbExtensionData(metadata);
        }

        if (metadata.ExtensionCode == JpegConstants.JfxxExtensionIndexedThumbnail)
        {
            return GetIndexedExtensionData(metadata);
        }

        return metadata.ExtensionData;
    }

    internal byte[] GetJpegExtensionData(JpegJfxxMetadata metadata)
    {
        if (metadata.JpegThumbnailData.Length > 0)
        {
            return metadata.JpegThumbnailData;
        }

        return metadata.ExtensionData;
    }

    internal byte[] GetRgbExtensionData(JpegJfxxMetadata metadata)
    {
        int thumbnailBytes = metadata.ThumbnailWidth * metadata.ThumbnailHeight * 3;
        if (metadata.RgbThumbnailPixels.Length == 0 && HasCompleteRgbExtensionData(metadata, thumbnailBytes))
        {
            return metadata.ExtensionData;
        }

        byte[] extensionData = new byte[2 + thumbnailBytes];
        extensionData[0] = (byte)metadata.ThumbnailWidth;
        extensionData[1] = (byte)metadata.ThumbnailHeight;
        if (thumbnailBytes > 0)
        {
            System.Array.Copy(metadata.RgbThumbnailPixels, 0, extensionData, 2, thumbnailBytes);
        }

        return extensionData;
    }

    internal bool HasCompleteRgbExtensionData(JpegJfxxMetadata metadata, int thumbnailBytes)
    {
        if (metadata.ExtensionData.Length != 2 + thumbnailBytes)
        {
            return false;
        }

        return metadata.ExtensionData[0] == metadata.ThumbnailWidth
            && metadata.ExtensionData[1] == metadata.ThumbnailHeight;
    }

    internal byte[] GetIndexedExtensionData(JpegJfxxMetadata metadata)
    {
        int thumbnailBytes = metadata.ThumbnailWidth * metadata.ThumbnailHeight;
        if (metadata.IndexedThumbnailPalette.Length == 0
            && metadata.IndexedThumbnailPixels.Length == 0
            && HasCompleteIndexedExtensionData(metadata, thumbnailBytes))
        {
            return metadata.ExtensionData;
        }

        byte[] extensionData = new byte[770 + thumbnailBytes];
        extensionData[0] = (byte)metadata.ThumbnailWidth;
        extensionData[1] = (byte)metadata.ThumbnailHeight;
        System.Array.Copy(metadata.IndexedThumbnailPalette, 0, extensionData, 2, 768);
        if (thumbnailBytes > 0)
        {
            System.Array.Copy(metadata.IndexedThumbnailPixels, 0, extensionData, 770, thumbnailBytes);
        }

        return extensionData;
    }

    internal bool HasCompleteIndexedExtensionData(JpegJfxxMetadata metadata, int thumbnailBytes)
    {
        if (metadata.ExtensionData.Length != 770 + thumbnailBytes)
        {
            return false;
        }

        return metadata.ExtensionData[0] == metadata.ThumbnailWidth
            && metadata.ExtensionData[1] == metadata.ThumbnailHeight;
    }
}
