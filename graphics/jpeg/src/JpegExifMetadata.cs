namespace jpeg;

internal class JpegExifMetadataParser
{
    internal bool IsExif(byte[]? payload)
    {
        if (payload == null)
        {
            return false;
        }

        if (payload.Length < JpegConstants.ExifIdentifierLength)
        {
            return false;
        }

        return payload[0] == 0x45
            && payload[1] == 0x78
            && payload[2] == 0x69
            && payload[3] == 0x66
            && payload[4] == 0
            && payload[5] == 0;
    }

    internal JpegExifMetadata Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (!IsExif(payload))
        {
            throw JpegInvalidDataException.Create("APP1 payload is not an Exif segment.");
        }

        JpegExifMetadata metadata = new JpegExifMetadata();
        metadata.IsPresent = true;
        metadata.TiffOffset = JpegConstants.ExifIdentifierLength;
        metadata.RawPayload = CopyBytes(payload, 0, payload.Length);
        metadata.TiffData = CopyBytes(payload, JpegConstants.ExifIdentifierLength, payload.Length - JpegConstants.ExifIdentifierLength);
        if (metadata.TiffData.Length >= 8)
        {
            JpegExifTiffReader reader = new JpegExifTiffReader();
            metadata.TiffHeader = reader.ParseHeader(metadata.TiffData);
            JpegExifTagReader tagReader = new JpegExifTagReader();
            tagReader.Apply(metadata);
        }

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

internal class JpegExifTagReader
{
    internal void Apply(JpegExifMetadata metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        if (!metadata.TiffHeader.IsPresent)
        {
            return;
        }

        if (metadata.TiffHeader.FirstIfdOffset == 0 || metadata.TiffHeader.FirstIfdOffset >= metadata.TiffData.Length)
        {
            return;
        }

        JpegExifTiffReader reader = new JpegExifTiffReader();
        TraverseIfd(metadata, metadata.TiffHeader.FirstIfdOffset, 0);
        long thumbnailIfdOffset = reader.ReadNextIfdOffset(metadata.TiffData, metadata.TiffHeader.FirstIfdOffset, metadata.TiffHeader.ByteOrder);
        if (thumbnailIfdOffset > 0 && thumbnailIfdOffset < metadata.TiffData.Length)
        {
            metadata.ThumbnailIfdOffset = thumbnailIfdOffset;
            metadata.HasThumbnailIfd = true;
            TraverseIfd(metadata, thumbnailIfdOffset, 3);
            ExtractJpegThumbnail(metadata);
            ExtractUncompressedThumbnail(metadata);
        }

        if (metadata.HasExifIfd && metadata.ExifIfdOffset > 0 && metadata.ExifIfdOffset < metadata.TiffData.Length)
        {
            TraverseIfd(metadata, metadata.ExifIfdOffset, 1);
        }

        if (metadata.HasGpsIfd && metadata.GpsIfdOffset > 0 && metadata.GpsIfdOffset < metadata.TiffData.Length)
        {
            TraverseIfd(metadata, metadata.GpsIfdOffset, 2);
        }
    }

    internal void TraverseIfd(JpegExifMetadata metadata, long ifdOffset, int ifdKind)
    {
        JpegExifTiffReader reader = new JpegExifTiffReader();
        int entryCount = reader.ReadIfdEntryCount(metadata.TiffData, ifdOffset, metadata.TiffHeader.ByteOrder);
        JpegExifIfd directory = new JpegExifIfd();
        directory.Kind = ResolveIfdKind(ifdKind);
        directory.Offset = ifdOffset;
        directory.NextIfdOffset = reader.ReadNextIfdOffset(metadata.TiffData, ifdOffset, metadata.TiffHeader.ByteOrder);
        int index = 0;
        while (index < entryCount)
        {
            JpegExifIfdEntry entry = reader.ReadIfdEntry(metadata.TiffData, ifdOffset, index, metadata.TiffHeader.ByteOrder);
            directory.Tags.Add(CreateGeneralTag(metadata, entry));
            ApplyEntry(metadata, entry, ifdKind);
            index = index + 1;
        }

        metadata.Ifds.Add(directory);
    }

    internal JpegExifIfdKind ResolveIfdKind(int ifdKind)
    {
        if (ifdKind == 0)
        {
            return JpegExifIfdKind.Primary;
        }

        if (ifdKind == 1)
        {
            return JpegExifIfdKind.Exif;
        }

        if (ifdKind == 2)
        {
            return JpegExifIfdKind.Gps;
        }

        if (ifdKind == 3)
        {
            return JpegExifIfdKind.Thumbnail;
        }

        return JpegExifIfdKind.Unknown;
    }

    internal JpegExifTag CreateGeneralTag(JpegExifMetadata metadata, JpegExifIfdEntry entry)
    {
        JpegExifTag tag = new JpegExifTag();
        tag.Identifier = entry.Tag;
        tag.Type = (JpegExifTagType)entry.Type;
        tag.Count = entry.Count;
        tag.EntryOffset = entry.EntryOffset;
        tag.ValueOffset = entry.ValueOffset;
        tag.Value = CreateGeneralTagValue(metadata, entry);
        return tag;
    }

    internal JpegExifTagValue CreateGeneralTagValue(JpegExifMetadata metadata, JpegExifIfdEntry entry)
    {
        int byteCount = GetValueByteCount(entry);
        long offset = GetValueDataOffset(entry);
        JpegExifTiffReader reader = new JpegExifTiffReader();
        reader.ValidateTiffRange(metadata.TiffData, offset, byteCount);
        JpegExifTagValue value = new JpegExifTagValue();
        value.RawBytes = new byte[byteCount];
        if (byteCount > 0)
        {
            System.Array.Copy(metadata.TiffData, (int)offset, value.RawBytes, 0, byteCount);
        }

        DecodeGeneralTagValue(metadata, entry, offset, value);
        return value;
    }

    internal void DecodeGeneralTagValue(JpegExifMetadata metadata, JpegExifIfdEntry entry, long offset, JpegExifTagValue value)
    {
        if (entry.Type == 2)
        {
            value.Text = ReadAscii(metadata, entry);
            return;
        }

        int index = 0;
        JpegExifTiffReader reader = new JpegExifTiffReader();
        while (index < entry.Count)
        {
            if (entry.Type == 1)
            {
                value.UnsignedValues.Add(metadata.TiffData[(int)offset + index]);
            }

            if (entry.Type == 3)
            {
                value.UnsignedValues.Add(reader.ReadUInt16(metadata.TiffData, (int)offset + index * 2, metadata.TiffHeader.ByteOrder));
            }

            if (entry.Type == 4)
            {
                value.UnsignedValues.Add(reader.ReadUInt32(metadata.TiffData, (int)offset + index * 4, metadata.TiffHeader.ByteOrder));
            }

            if (entry.Type == 6)
            {
                int signedByte = metadata.TiffData[(int)offset + index];
                if (signedByte >= 128)
                {
                    signedByte = signedByte - 256;
                }

                value.SignedValues.Add(signedByte);
            }

            if (entry.Type == 8)
            {
                value.SignedValues.Add(reader.ReadInt16(metadata.TiffData, (int)offset + index * 2, metadata.TiffHeader.ByteOrder));
            }

            if (entry.Type == 9)
            {
                value.SignedValues.Add(reader.ReadInt32(metadata.TiffData, (int)offset + index * 4, metadata.TiffHeader.ByteOrder));
            }

            if (entry.Type == 5 || entry.Type == 10)
            {
                long numerator = reader.ReadUInt32(metadata.TiffData, (int)offset + index * 8, metadata.TiffHeader.ByteOrder);
                long denominator = reader.ReadUInt32(metadata.TiffData, (int)offset + index * 8 + 4, metadata.TiffHeader.ByteOrder);
                if (entry.Type == 10)
                {
                    numerator = reader.ReadInt32(metadata.TiffData, (int)offset + index * 8, metadata.TiffHeader.ByteOrder);
                    denominator = reader.ReadInt32(metadata.TiffData, (int)offset + index * 8 + 4, metadata.TiffHeader.ByteOrder);
                }

                value.RationalNumerators.Add(numerator);
                value.RationalDenominators.Add(denominator);
                double rational = 0.0;
                if (denominator != 0)
                {
                    rational = (double)numerator / (double)denominator;
                }

                value.RationalValues.Add(rational);
            }

            index = index + 1;
        }
    }

    internal void ApplyEntry(JpegExifMetadata metadata, JpegExifIfdEntry entry, int ifdKind)
    {
        if (ifdKind == 0)
        {
            ApplyImageEntry(metadata, entry);
        }

        if (ifdKind == 1)
        {
            ApplyExifEntry(metadata, entry);
        }

        if (ifdKind == 2)
        {
            ApplyGpsEntry(metadata, entry);
        }

        if (ifdKind == 3)
        {
            ApplyThumbnailEntry(metadata, entry);
        }
    }

    internal void ApplyImageEntry(JpegExifMetadata metadata, JpegExifIfdEntry entry)
    {
        if (entry.Tag == 0x0100)
        {
            metadata.ImageWidth = ReadUnsignedValue(metadata, entry);
            metadata.HasImageWidth = true;
        }

        if (entry.Tag == 0x0101)
        {
            metadata.ImageHeight = ReadUnsignedValue(metadata, entry);
            metadata.HasImageHeight = true;
        }

        if (entry.Tag == 0x010F)
        {
            metadata.Make = ReadAscii(metadata, entry);
            metadata.HasMake = metadata.Make.Length > 0;
        }

        if (entry.Tag == 0x0110)
        {
            metadata.Model = ReadAscii(metadata, entry);
            metadata.HasModel = metadata.Model.Length > 0;
        }

        if (entry.Tag == 0x0112)
        {
            metadata.Orientation = (int)ReadUnsignedValue(metadata, entry);
            metadata.HasOrientation = true;
        }

        if (entry.Tag == 0x011A)
        {
            metadata.XResolution = ReadRational(metadata, entry);
            metadata.HasXResolution = true;
        }

        if (entry.Tag == 0x011B)
        {
            metadata.YResolution = ReadRational(metadata, entry);
            metadata.HasYResolution = true;
        }

        if (entry.Tag == 0x0128)
        {
            metadata.ResolutionUnit = (int)ReadUnsignedValue(metadata, entry);
            metadata.HasResolutionUnit = true;
        }

        if (entry.Tag == 0x0131)
        {
            metadata.Software = ReadAscii(metadata, entry);
            metadata.HasSoftware = metadata.Software.Length > 0;
        }

        if (entry.Tag == 0x0132)
        {
            metadata.DateTime = ReadAscii(metadata, entry);
            metadata.HasDateTime = metadata.DateTime.Length > 0;
        }

        if (entry.Tag == 0x8769)
        {
            metadata.ExifIfdOffset = ReadUnsignedValue(metadata, entry);
            metadata.HasExifIfd = true;
        }

        if (entry.Tag == 0x8825)
        {
            metadata.GpsIfdOffset = ReadUnsignedValue(metadata, entry);
            metadata.HasGpsIfd = true;
        }
    }

    internal void ApplyExifEntry(JpegExifMetadata metadata, JpegExifIfdEntry entry)
    {
        if (entry.Tag == 0x9003)
        {
            metadata.DateTimeOriginal = ReadAscii(metadata, entry);
            metadata.HasDateTimeOriginal = metadata.DateTimeOriginal.Length > 0;
        }

        if (entry.Tag == 0xA001)
        {
            metadata.ColorSpace = (int)ReadUnsignedValue(metadata, entry);
            metadata.HasColorSpace = true;
        }

        if (entry.Tag == 0xA002)
        {
            metadata.PixelWidth = ReadUnsignedValue(metadata, entry);
            metadata.HasPixelWidth = true;
        }

        if (entry.Tag == 0xA003)
        {
            metadata.PixelHeight = ReadUnsignedValue(metadata, entry);
            metadata.HasPixelHeight = true;
        }

        if (entry.Tag == 0xA434)
        {
            metadata.LensModel = ReadAscii(metadata, entry);
            metadata.HasLensModel = metadata.LensModel.Length > 0;
        }
    }

    internal void ApplyGpsEntry(JpegExifMetadata metadata, JpegExifIfdEntry entry)
    {
        if (entry.Tag == 0x0001)
        {
            metadata.GpsLatitudeRef = ReadAscii(metadata, entry);
        }

        if (entry.Tag == 0x0002)
        {
            metadata.GpsLatitude = ReadGpsCoordinate(metadata, entry, metadata.GpsLatitudeRef);
            metadata.HasGpsLatitude = true;
        }

        if (entry.Tag == 0x0003)
        {
            metadata.GpsLongitudeRef = ReadAscii(metadata, entry);
        }

        if (entry.Tag == 0x0004)
        {
            metadata.GpsLongitude = ReadGpsCoordinate(metadata, entry, metadata.GpsLongitudeRef);
            metadata.HasGpsLongitude = true;
        }
    }

    internal void ApplyThumbnailEntry(JpegExifMetadata metadata, JpegExifIfdEntry entry)
    {
        if (entry.Tag == 0x0100)
        {
            metadata.ThumbnailWidth = ReadUnsignedValue(metadata, entry);
            metadata.HasThumbnailWidth = true;
        }

        if (entry.Tag == 0x0101)
        {
            metadata.ThumbnailHeight = ReadUnsignedValue(metadata, entry);
            metadata.HasThumbnailHeight = true;
        }

        if (entry.Tag == 0x0103)
        {
            metadata.ThumbnailCompression = (int)ReadUnsignedValue(metadata, entry);
            metadata.HasThumbnailCompression = true;
        }

        if (entry.Tag == 0x0106)
        {
            metadata.ThumbnailPhotometricInterpretation = (int)ReadUnsignedValue(metadata, entry);
            metadata.HasThumbnailPhotometricInterpretation = true;
        }

        if (entry.Tag == 0x0111)
        {
            metadata.ThumbnailStripOffset = ReadUnsignedValue(metadata, entry);
            metadata.HasThumbnailStripOffset = true;
        }

        if (entry.Tag == 0x0115)
        {
            metadata.ThumbnailSamplesPerPixel = (int)ReadUnsignedValue(metadata, entry);
            metadata.HasThumbnailSamplesPerPixel = true;
        }

        if (entry.Tag == 0x0117)
        {
            metadata.ThumbnailStripByteCount = ReadUnsignedValue(metadata, entry);
            metadata.HasThumbnailStripByteCount = true;
        }

        if (entry.Tag == 0x0201)
        {
            metadata.JpegThumbnailOffset = ReadUnsignedValue(metadata, entry);
            metadata.HasJpegThumbnailOffset = true;
        }

        if (entry.Tag == 0x0202)
        {
            metadata.JpegThumbnailLength = ReadUnsignedValue(metadata, entry);
            metadata.HasJpegThumbnailLength = true;
        }
    }

    internal void ExtractJpegThumbnail(JpegExifMetadata metadata)
    {
        if (!metadata.HasJpegThumbnailOffset || !metadata.HasJpegThumbnailLength)
        {
            return;
        }

        if (metadata.HasThumbnailCompression && metadata.ThumbnailCompression != 6)
        {
            return;
        }

        if (metadata.JpegThumbnailLength <= 0 || metadata.JpegThumbnailLength > 2147483647L)
        {
            throw JpegInvalidDataException.Create("Exif JPEG thumbnail length is invalid.");
        }

        JpegExifTiffReader reader = new JpegExifTiffReader();
        reader.ValidateTiffRange(metadata.TiffData, metadata.JpegThumbnailOffset, (int)metadata.JpegThumbnailLength);
        metadata.JpegThumbnailData = new byte[(int)metadata.JpegThumbnailLength];
        System.Array.Copy(metadata.TiffData, (int)metadata.JpegThumbnailOffset, metadata.JpegThumbnailData, 0, metadata.JpegThumbnailData.Length);
        metadata.HasJpegThumbnail = true;
    }

    internal void ExtractUncompressedThumbnail(JpegExifMetadata metadata)
    {
        if (!metadata.HasThumbnailCompression || metadata.ThumbnailCompression != 1)
        {
            return;
        }

        if (!metadata.HasThumbnailWidth || !metadata.HasThumbnailHeight)
        {
            return;
        }

        if (!metadata.HasThumbnailStripOffset || !metadata.HasThumbnailStripByteCount)
        {
            return;
        }

        if (!metadata.HasThumbnailPhotometricInterpretation)
        {
            return;
        }

        int samplesPerPixel = GetThumbnailSamplesPerPixel(metadata);
        long expectedLength = metadata.ThumbnailWidth * metadata.ThumbnailHeight * samplesPerPixel;
        if (expectedLength <= 0 || expectedLength > 2147483647L)
        {
            throw JpegInvalidDataException.Create("Exif uncompressed thumbnail dimensions are invalid.");
        }

        if (metadata.ThumbnailStripByteCount < expectedLength || metadata.ThumbnailStripByteCount > 2147483647L)
        {
            throw JpegInvalidDataException.Create("Exif uncompressed thumbnail byte count is invalid.");
        }

        JpegExifTiffReader reader = new JpegExifTiffReader();
        reader.ValidateTiffRange(metadata.TiffData, metadata.ThumbnailStripOffset, (int)metadata.ThumbnailStripByteCount);
        byte[] pixels = new byte[(int)expectedLength];
        System.Array.Copy(metadata.TiffData, (int)metadata.ThumbnailStripOffset, pixels, 0, pixels.Length);
        if (metadata.ThumbnailPhotometricInterpretation == 2)
        {
            metadata.RgbThumbnailPixels = pixels;
            metadata.HasRgbThumbnail = true;
        }

        if (metadata.ThumbnailPhotometricInterpretation == 6)
        {
            metadata.YCbCrThumbnailPixels = pixels;
            metadata.HasYCbCrThumbnail = true;
        }
    }

    internal int GetThumbnailSamplesPerPixel(JpegExifMetadata metadata)
    {
        if (metadata.HasThumbnailSamplesPerPixel)
        {
            return metadata.ThumbnailSamplesPerPixel;
        }

        return 3;
    }

    internal long ReadUnsignedValue(JpegExifMetadata metadata, JpegExifIfdEntry entry)
    {
        JpegExifTiffReader reader = new JpegExifTiffReader();
        if (entry.Count != 1)
        {
            throw JpegInvalidDataException.Create("Exif scalar tag must contain one value.");
        }

        if (entry.Type == 3)
        {
            return reader.ReadUInt16(metadata.TiffData, (int)entry.EntryOffset + 8, metadata.TiffHeader.ByteOrder);
        }

        if (entry.Type == 4)
        {
            return entry.ValueOffset;
        }

        throw JpegInvalidDataException.Create("Exif scalar tag type is not supported.");
    }

    internal string ReadAscii(JpegExifMetadata metadata, JpegExifIfdEntry entry)
    {
        if (entry.Type != 2)
        {
            throw JpegInvalidDataException.Create("Exif ASCII tag type is not supported.");
        }

        int length = GetValueByteCount(entry);
        long offset = GetValueDataOffset(entry);
        JpegExifTiffReader reader = new JpegExifTiffReader();
        reader.ValidateTiffRange(metadata.TiffData, offset, length);
        int trimmedLength = 0;
        while (trimmedLength < length && metadata.TiffData[(int)offset + trimmedLength] != 0)
        {
            trimmedLength = trimmedLength + 1;
        }

        return System.Text.Encoding.ASCII.GetString(metadata.TiffData, (int)offset, trimmedLength);
    }

    internal double ReadRational(JpegExifMetadata metadata, JpegExifIfdEntry entry)
    {
        if (entry.Type != 5 || entry.Count != 1)
        {
            throw JpegInvalidDataException.Create("Exif rational tag type is not supported.");
        }

        JpegExifTiffReader reader = new JpegExifTiffReader();
        reader.ValidateTiffRange(metadata.TiffData, entry.ValueOffset, 8);
        long numerator = reader.ReadUInt32(metadata.TiffData, (int)entry.ValueOffset, metadata.TiffHeader.ByteOrder);
        long denominator = reader.ReadUInt32(metadata.TiffData, (int)entry.ValueOffset + 4, metadata.TiffHeader.ByteOrder);
        if (denominator == 0)
        {
            return 0.0;
        }

        return (double)numerator / (double)denominator;
    }

    internal double ReadGpsCoordinate(JpegExifMetadata metadata, JpegExifIfdEntry entry, string reference)
    {
        if (entry.Type != 5 || entry.Count != 3)
        {
            throw JpegInvalidDataException.Create("Exif GPS coordinate tag type is not supported.");
        }

        double degrees = ReadRationalAt(metadata, entry.ValueOffset);
        double minutes = ReadRationalAt(metadata, entry.ValueOffset + 8);
        double seconds = ReadRationalAt(metadata, entry.ValueOffset + 16);
        double value = degrees + minutes / 60.0 + seconds / 3600.0;
        if (reference == "S" || reference == "W")
        {
            value = 0.0 - value;
        }

        return value;
    }

    internal double ReadRationalAt(JpegExifMetadata metadata, long offset)
    {
        JpegExifTiffReader reader = new JpegExifTiffReader();
        reader.ValidateTiffRange(metadata.TiffData, offset, 8);
        long numerator = reader.ReadUInt32(metadata.TiffData, (int)offset, metadata.TiffHeader.ByteOrder);
        long denominator = reader.ReadUInt32(metadata.TiffData, (int)offset + 4, metadata.TiffHeader.ByteOrder);
        if (denominator == 0)
        {
            return 0.0;
        }

        return (double)numerator / (double)denominator;
    }

    internal long GetValueDataOffset(JpegExifIfdEntry entry)
    {
        int byteCount = GetValueByteCount(entry);
        if (byteCount <= 4)
        {
            return entry.EntryOffset + 8;
        }

        return entry.ValueOffset;
    }

    internal int GetValueByteCount(JpegExifIfdEntry entry)
    {
        int unitSize = GetTypeUnitSize(entry.Type);
        long byteCount = entry.Count * unitSize;
        if (byteCount > 2147483647L)
        {
            throw JpegInvalidDataException.Create("Exif tag value is too large.");
        }

        return (int)byteCount;
    }

    internal int GetTypeUnitSize(int type)
    {
        if (type == 1 || type == 2 || type == 6 || type == 7)
        {
            return 1;
        }

        if (type == 3 || type == 8)
        {
            return 2;
        }

        if (type == 4 || type == 9 || type == 11)
        {
            return 4;
        }

        if (type == 5 || type == 10 || type == 12)
        {
            return 8;
        }

        throw JpegInvalidDataException.Create("Exif tag type is not supported.");
    }
}

internal class JpegExifTiffReader
{
    internal JpegExifTiffHeader ParseHeader(byte[] tiffData)
    {
        if (tiffData == null)
        {
            throw new System.ArgumentNullException("tiffData");
        }

        ValidateTiffRange(tiffData, 0, 8);
        JpegTiffByteOrder order = ReadByteOrder(tiffData);
        int magic = ReadUInt16(tiffData, 2, order);
        if (magic != 42)
        {
            throw JpegInvalidDataException.Create("Exif TIFF header magic must be 42.");
        }

        long firstIfdOffset = ReadUInt32(tiffData, 4, order);
        if (firstIfdOffset > tiffData.Length)
        {
            throw JpegInvalidDataException.Create("Unexpected end of JPEG input.");
        }

        JpegExifTiffHeader header = new JpegExifTiffHeader();
        header.IsPresent = true;
        header.ByteOrder = order;
        header.IsLittleEndian = order == JpegTiffByteOrder.LittleEndian;
        header.IsBigEndian = order == JpegTiffByteOrder.BigEndian;
        header.Magic = magic;
        header.FirstIfdOffset = firstIfdOffset;
        return header;
    }

    internal JpegTiffByteOrder ReadByteOrder(byte[] tiffData)
    {
        ValidateTiffRange(tiffData, 0, 2);
        if (tiffData[0] == 0x49 && tiffData[1] == 0x49)
        {
            return JpegTiffByteOrder.LittleEndian;
        }

        if (tiffData[0] == 0x4D && tiffData[1] == 0x4D)
        {
            return JpegTiffByteOrder.BigEndian;
        }

        throw JpegInvalidDataException.Create("Exif TIFF byte order must be II or MM.");
    }

    internal int ReadUInt16(byte[] data, int offset, JpegTiffByteOrder order)
    {
        ValidateOrder(order);
        ValidateTiffRange(data, offset, 2);
        if (order == JpegTiffByteOrder.LittleEndian)
        {
            return data[offset] | (data[offset + 1] << 8);
        }

        return (data[offset] << 8) | data[offset + 1];
    }

    internal int ReadInt16(byte[] data, int offset, JpegTiffByteOrder order)
    {
        int value = ReadUInt16(data, offset, order);
        if (value >= 32768)
        {
            return value - 65536;
        }

        return value;
    }

    internal long ReadUInt32(byte[] data, int offset, JpegTiffByteOrder order)
    {
        ValidateOrder(order);
        ValidateTiffRange(data, offset, 4);
        if (order == JpegTiffByteOrder.LittleEndian)
        {
            return (long)data[offset]
                | ((long)data[offset + 1] << 8)
                | ((long)data[offset + 2] << 16)
                | ((long)data[offset + 3] << 24);
        }

        return ((long)data[offset] << 24)
            | ((long)data[offset + 1] << 16)
            | ((long)data[offset + 2] << 8)
            | (long)data[offset + 3];
    }

    internal long ReadInt32(byte[] data, int offset, JpegTiffByteOrder order)
    {
        long value = ReadUInt32(data, offset, order);
        if (value >= 2147483648L)
        {
            return value - 4294967296L;
        }

        return value;
    }

    internal int ReadIfdEntryCount(byte[] tiffData, long ifdOffset, JpegTiffByteOrder order)
    {
        ValidateTiffRange(tiffData, ifdOffset, 2);
        return ReadUInt16(tiffData, (int)ifdOffset, order);
    }

    internal JpegExifIfdEntry ReadIfdEntry(byte[] tiffData, long ifdOffset, int entryIndex, JpegTiffByteOrder order)
    {
        if (entryIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("entryIndex");
        }

        int entryCount = ReadIfdEntryCount(tiffData, ifdOffset, order);
        if (entryIndex >= entryCount)
        {
            throw JpegInvalidDataException.Create("Exif IFD entry index exceeds the entry count.");
        }

        long entryOffset = ifdOffset + 2 + entryIndex * 12;
        ValidateTiffRange(tiffData, entryOffset, 12);
        JpegExifIfdEntry entry = new JpegExifIfdEntry();
        entry.EntryOffset = entryOffset;
        entry.Tag = ReadUInt16(tiffData, (int)entryOffset, order);
        entry.Type = ReadUInt16(tiffData, (int)entryOffset + 2, order);
        entry.Count = ReadUInt32(tiffData, (int)entryOffset + 4, order);
        entry.ValueOffset = ReadUInt32(tiffData, (int)entryOffset + 8, order);
        return entry;
    }

    internal long ReadNextIfdOffset(byte[] tiffData, long ifdOffset, JpegTiffByteOrder order)
    {
        int entryCount = ReadIfdEntryCount(tiffData, ifdOffset, order);
        long offset = ifdOffset + 2 + entryCount * 12;
        ValidateTiffRange(tiffData, offset, 4);
        return ReadUInt32(tiffData, (int)offset, order);
    }

    internal void ValidateOrder(JpegTiffByteOrder order)
    {
        if (order != JpegTiffByteOrder.LittleEndian && order != JpegTiffByteOrder.BigEndian)
        {
            throw JpegInvalidDataException.Create("Exif TIFF byte order is unknown.");
        }
    }

    internal void ValidateTiffRange(byte[] data, long offset, int count)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (offset < 0 || offset > 2147483647L)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        JpegByteReader.ValidateByteArrayRange(data, (int)offset, count);
    }
}

internal class JpegExifMetadataWriter
{
    internal byte[] WritePayload(JpegExifMetadata metadata)
    {
        Validate(metadata);
        byte[] payload = new byte[metadata.RawPayload.Length];
        if (payload.Length > 0)
        {
            System.Array.Copy(metadata.RawPayload, 0, payload, 0, metadata.RawPayload.Length);
        }

        ApplySelectedFieldReplacements(metadata, payload);
        return payload;
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, JpegExifMetadata metadata)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        byte[] payload = WritePayload(metadata);
        segmentWriter.WriteMarkerSegment(JpegMarker.App1, payload);
    }

    internal void Validate(JpegExifMetadata metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        JpegExifMetadataParser parser = new JpegExifMetadataParser();
        if (!parser.IsExif(metadata.RawPayload))
        {
            throw JpegWriteValidationException.Create("Exif metadata must contain an APP1 Exif payload.");
        }

        if (metadata.RawPayload.Length > JpegConstants.MaximumMarkerPayloadLength)
        {
            throw JpegWriteValidationException.Create("Exif APP1 payload exceeds the maximum marker payload length.");
        }
    }

    internal void ApplySelectedFieldReplacements(JpegExifMetadata metadata, byte[] payload)
    {
        if (payload.Length < JpegConstants.ExifIdentifierLength + 8)
        {
            return;
        }

        JpegExifMetadataParser parser = new JpegExifMetadataParser();
        byte[] tiffData = parser.CopyBytes(payload, JpegConstants.ExifIdentifierLength, payload.Length - JpegConstants.ExifIdentifierLength);
        JpegExifTiffReader reader = new JpegExifTiffReader();
        JpegExifTiffHeader header = reader.ParseHeader(tiffData);
        if (header.FirstIfdOffset == 0 || header.FirstIfdOffset >= tiffData.Length)
        {
            return;
        }

        ApplyImageFieldReplacements(metadata, payload, tiffData, header);
        JpegExifIfdEntry? exifPointer = FindEntry(tiffData, header.FirstIfdOffset, 0x8769, header.ByteOrder);
        if (exifPointer != null)
        {
            long exifIfdOffset = ReadExistingUnsignedValue(tiffData, exifPointer, header.ByteOrder);
            if (exifIfdOffset > 0 && exifIfdOffset < tiffData.Length)
            {
                ApplyExifFieldReplacements(metadata, payload, tiffData, header, exifIfdOffset);
            }
        }

        JpegExifIfdEntry? gpsPointer = FindEntry(tiffData, header.FirstIfdOffset, 0x8825, header.ByteOrder);
        if (gpsPointer != null)
        {
            long gpsIfdOffset = ReadExistingUnsignedValue(tiffData, gpsPointer, header.ByteOrder);
            if (gpsIfdOffset > 0 && gpsIfdOffset < tiffData.Length)
            {
                ApplyGpsFieldReplacements(metadata, payload, tiffData, header, gpsIfdOffset);
            }
        }

        JpegExifTiffReader tiffReader = new JpegExifTiffReader();
        long thumbnailIfdOffset = tiffReader.ReadNextIfdOffset(tiffData, header.FirstIfdOffset, header.ByteOrder);
        if (thumbnailIfdOffset > 0 && thumbnailIfdOffset < tiffData.Length)
        {
            ApplyThumbnailFieldReplacements(metadata, payload, tiffData, header, thumbnailIfdOffset);
        }
    }

    internal void ApplyImageFieldReplacements(JpegExifMetadata metadata, byte[] payload, byte[] tiffData, JpegExifTiffHeader header)
    {
        if (metadata.HasImageWidth)
        {
            ReplaceUnsignedValue(payload, tiffData, header.FirstIfdOffset, 0x0100, metadata.ImageWidth, header.ByteOrder);
        }

        if (metadata.HasImageHeight)
        {
            ReplaceUnsignedValue(payload, tiffData, header.FirstIfdOffset, 0x0101, metadata.ImageHeight, header.ByteOrder);
        }

        if (metadata.HasMake)
        {
            ReplaceAsciiValue(payload, tiffData, header.FirstIfdOffset, 0x010F, metadata.Make, header.ByteOrder);
        }

        if (metadata.HasModel)
        {
            ReplaceAsciiValue(payload, tiffData, header.FirstIfdOffset, 0x0110, metadata.Model, header.ByteOrder);
        }

        if (metadata.HasOrientation)
        {
            ReplaceUnsignedValue(payload, tiffData, header.FirstIfdOffset, 0x0112, metadata.Orientation, header.ByteOrder);
        }

        if (metadata.HasXResolution)
        {
            ReplaceRationalValue(payload, tiffData, header.FirstIfdOffset, 0x011A, metadata.XResolution, header.ByteOrder);
        }

        if (metadata.HasYResolution)
        {
            ReplaceRationalValue(payload, tiffData, header.FirstIfdOffset, 0x011B, metadata.YResolution, header.ByteOrder);
        }

        if (metadata.HasResolutionUnit)
        {
            ReplaceUnsignedValue(payload, tiffData, header.FirstIfdOffset, 0x0128, metadata.ResolutionUnit, header.ByteOrder);
        }

        if (metadata.HasSoftware)
        {
            ReplaceAsciiValue(payload, tiffData, header.FirstIfdOffset, 0x0131, metadata.Software, header.ByteOrder);
        }

        if (metadata.HasDateTime)
        {
            ReplaceAsciiValue(payload, tiffData, header.FirstIfdOffset, 0x0132, metadata.DateTime, header.ByteOrder);
        }
    }

    internal void ApplyExifFieldReplacements(JpegExifMetadata metadata, byte[] payload, byte[] tiffData, JpegExifTiffHeader header, long ifdOffset)
    {
        if (metadata.HasDateTimeOriginal)
        {
            ReplaceAsciiValue(payload, tiffData, ifdOffset, 0x9003, metadata.DateTimeOriginal, header.ByteOrder);
        }

        if (metadata.HasColorSpace)
        {
            ReplaceUnsignedValue(payload, tiffData, ifdOffset, 0xA001, metadata.ColorSpace, header.ByteOrder);
        }

        if (metadata.HasPixelWidth)
        {
            ReplaceUnsignedValue(payload, tiffData, ifdOffset, 0xA002, metadata.PixelWidth, header.ByteOrder);
        }

        if (metadata.HasPixelHeight)
        {
            ReplaceUnsignedValue(payload, tiffData, ifdOffset, 0xA003, metadata.PixelHeight, header.ByteOrder);
        }

        if (metadata.HasLensModel)
        {
            ReplaceAsciiValue(payload, tiffData, ifdOffset, 0xA434, metadata.LensModel, header.ByteOrder);
        }
    }

    internal void ApplyGpsFieldReplacements(JpegExifMetadata metadata, byte[] payload, byte[] tiffData, JpegExifTiffHeader header, long ifdOffset)
    {
        if (metadata.HasGpsLatitude)
        {
            ReplaceGpsCoordinate(payload, tiffData, ifdOffset, 0x0001, 0x0002, metadata.GpsLatitude, 90.0, "N", "S", header.ByteOrder);
        }

        if (metadata.HasGpsLongitude)
        {
            ReplaceGpsCoordinate(payload, tiffData, ifdOffset, 0x0003, 0x0004, metadata.GpsLongitude, 180.0, "E", "W", header.ByteOrder);
        }
    }

    internal void ApplyThumbnailFieldReplacements(JpegExifMetadata metadata, byte[] payload, byte[] tiffData, JpegExifTiffHeader header, long ifdOffset)
    {
        if (metadata.HasThumbnailWidth)
        {
            ReplaceUnsignedValue(payload, tiffData, ifdOffset, 0x0100, metadata.ThumbnailWidth, header.ByteOrder);
        }

        if (metadata.HasThumbnailHeight)
        {
            ReplaceUnsignedValue(payload, tiffData, ifdOffset, 0x0101, metadata.ThumbnailHeight, header.ByteOrder);
        }

        if (metadata.HasThumbnailCompression)
        {
            ReplaceUnsignedValue(payload, tiffData, ifdOffset, 0x0103, metadata.ThumbnailCompression, header.ByteOrder);
        }

        if (metadata.HasThumbnailPhotometricInterpretation)
        {
            ReplaceUnsignedValue(payload, tiffData, ifdOffset, 0x0106, metadata.ThumbnailPhotometricInterpretation, header.ByteOrder);
        }

        if (metadata.HasThumbnailSamplesPerPixel)
        {
            ReplaceUnsignedValue(payload, tiffData, ifdOffset, 0x0115, metadata.ThumbnailSamplesPerPixel, header.ByteOrder);
        }

        if (metadata.HasJpegThumbnail)
        {
            ValidateJpegThumbnailData(metadata.JpegThumbnailData);
            ReplaceThumbnailData(payload, tiffData, ifdOffset, 0x0201, 0x0202, metadata.JpegThumbnailData, header.ByteOrder);
        }

        if (metadata.HasRgbThumbnail && metadata.HasYCbCrThumbnail)
        {
            throw JpegWriteValidationException.Create("Exif thumbnail replacement cannot select both RGB and YCbCr pixels.");
        }

        byte[] pixels = new byte[0];
        int photometricInterpretation = 0;
        if (metadata.HasRgbThumbnail)
        {
            pixels = metadata.RgbThumbnailPixels;
            photometricInterpretation = 2;
        }

        if (metadata.HasYCbCrThumbnail)
        {
            pixels = metadata.YCbCrThumbnailPixels;
            photometricInterpretation = 6;
        }

        if (photometricInterpretation != 0)
        {
            ValidateUncompressedThumbnailData(metadata, pixels);
            ReplaceUnsignedValue(payload, tiffData, ifdOffset, 0x0103, 1, header.ByteOrder);
            ReplaceUnsignedValue(payload, tiffData, ifdOffset, 0x0106, photometricInterpretation, header.ByteOrder);
            ReplaceUnsignedValue(payload, tiffData, ifdOffset, 0x0115, 3, header.ByteOrder);
            ReplaceThumbnailData(payload, tiffData, ifdOffset, 0x0111, 0x0117, pixels, header.ByteOrder);
        }
    }

    internal void ReplaceRationalValue(byte[] payload, byte[] tiffData, long ifdOffset, int tag, double value, JpegTiffByteOrder order)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
        {
            throw JpegWriteValidationException.Create("Exif unsigned rational replacement value is invalid.");
        }

        JpegExifIfdEntry? entry = FindEntry(tiffData, ifdOffset, tag, order);
        if (entry == null)
        {
            return;
        }

        if (entry.Type != 5 || entry.Count != 1)
        {
            throw JpegWriteValidationException.Create("Exif rational replacement tag type is not supported.");
        }

        WriteRationalComponent(payload, entry.ValueOffset, value, order);
    }

    internal void ReplaceGpsCoordinate(
        byte[] payload,
        byte[] tiffData,
        long ifdOffset,
        int referenceTag,
        int coordinateTag,
        double coordinate,
        double maximum,
        string positiveReference,
        string negativeReference,
        JpegTiffByteOrder order)
    {
        if (double.IsNaN(coordinate) || double.IsInfinity(coordinate) || coordinate < 0.0 - maximum || coordinate > maximum)
        {
            throw JpegWriteValidationException.Create("Exif GPS coordinate replacement is out of range.");
        }

        string reference = positiveReference;
        if (coordinate < 0.0)
        {
            reference = negativeReference;
        }

        ReplaceAsciiValue(payload, tiffData, ifdOffset, referenceTag, reference, order);
        JpegExifIfdEntry? entry = FindEntry(tiffData, ifdOffset, coordinateTag, order);
        if (entry == null)
        {
            return;
        }

        if (entry.Type != 5 || entry.Count != 3)
        {
            throw JpegWriteValidationException.Create("Exif GPS coordinate tag type is not supported.");
        }

        double absolute = System.Math.Abs(coordinate);
        double degrees = System.Math.Floor(absolute);
        double remainingMinutes = (absolute - degrees) * 60.0;
        double minutes = System.Math.Floor(remainingMinutes);
        double seconds = (remainingMinutes - minutes) * 60.0;
        WriteRationalComponent(payload, entry.ValueOffset, degrees, order);
        WriteRationalComponent(payload, entry.ValueOffset + 8, minutes, order);
        WriteRationalComponent(payload, entry.ValueOffset + 16, seconds, order);
    }

    internal void WriteRationalComponent(byte[] payload, long tiffOffset, double value, JpegTiffByteOrder order)
    {
        long denominator = 1;
        long numerator = (long)System.Math.Round(value);
        if (System.Math.Abs(value - (double)numerator) > 0.000000001)
        {
            denominator = 1000000;
            numerator = (long)System.Math.Round(value * (double)denominator);
        }

        if (numerator < 0 || numerator > 4294967295L)
        {
            throw JpegWriteValidationException.Create("Exif rational replacement numerator is out of range.");
        }

        int payloadOffset = JpegConstants.ExifIdentifierLength + (int)tiffOffset;
        WriteUInt32(payload, payloadOffset, numerator, order);
        WriteUInt32(payload, payloadOffset + 4, denominator, order);
    }

    internal void ReplaceThumbnailData(
        byte[] payload,
        byte[] tiffData,
        long ifdOffset,
        int offsetTag,
        int lengthTag,
        byte[] data,
        JpegTiffByteOrder order)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        JpegExifIfdEntry? offsetEntry = FindEntry(tiffData, ifdOffset, offsetTag, order);
        JpegExifIfdEntry? lengthEntry = FindEntry(tiffData, ifdOffset, lengthTag, order);
        if (offsetEntry == null || lengthEntry == null)
        {
            throw JpegWriteValidationException.Create("Exif thumbnail replacement requires existing offset and length tags.");
        }

        long dataOffset = ReadExistingUnsignedValue(tiffData, offsetEntry, order);
        long capacity = ReadExistingUnsignedValue(tiffData, lengthEntry, order);
        if (capacity < 0 || capacity > 2147483647L || data.Length > capacity)
        {
            throw JpegWriteValidationException.Create("Exif thumbnail replacement does not fit the existing thumbnail allocation.");
        }

        JpegExifTiffReader reader = new JpegExifTiffReader();
        reader.ValidateTiffRange(tiffData, dataOffset, (int)capacity);
        int payloadOffset = JpegConstants.ExifIdentifierLength + (int)dataOffset;
        ClearPayloadRange(payload, payloadOffset, (int)capacity);
        if (data.Length > 0)
        {
            System.Array.Copy(data, 0, payload, payloadOffset, data.Length);
        }

        ReplaceUnsignedValue(payload, tiffData, ifdOffset, lengthTag, data.Length, order);
    }

    internal void ValidateJpegThumbnailData(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (data.Length < 4
            || data[0] != 0xFF
            || data[1] != 0xD8
            || data[data.Length - 2] != 0xFF
            || data[data.Length - 1] != 0xD9)
        {
            throw JpegWriteValidationException.Create("Exif JPEG thumbnail replacement must contain a complete JPEG stream.");
        }
    }

    internal void ValidateUncompressedThumbnailData(JpegExifMetadata metadata, byte[] pixels)
    {
        if (pixels == null)
        {
            throw new System.ArgumentNullException("pixels");
        }

        if (!metadata.HasThumbnailWidth || !metadata.HasThumbnailHeight)
        {
            throw JpegWriteValidationException.Create("Exif uncompressed thumbnail replacement requires width and height tags.");
        }

        long expectedLength = metadata.ThumbnailWidth * metadata.ThumbnailHeight * 3;
        if (expectedLength < 0 || expectedLength > 2147483647L || pixels.Length != expectedLength)
        {
            throw JpegWriteValidationException.Create("Exif uncompressed thumbnail replacement pixels must exactly match its dimensions.");
        }
    }

    internal JpegExifIfdEntry? FindEntry(byte[] tiffData, long ifdOffset, int tag, JpegTiffByteOrder order)
    {
        JpegExifTiffReader reader = new JpegExifTiffReader();
        int entryCount = reader.ReadIfdEntryCount(tiffData, ifdOffset, order);
        int index = 0;
        while (index < entryCount)
        {
            JpegExifIfdEntry entry = reader.ReadIfdEntry(tiffData, ifdOffset, index, order);
            if (entry.Tag == tag)
            {
                return entry;
            }

            index = index + 1;
        }

        return null;
    }

    internal void ReplaceUnsignedValue(byte[] payload, byte[] tiffData, long ifdOffset, int tag, long value, JpegTiffByteOrder order)
    {
        JpegExifIfdEntry? entry = FindEntry(tiffData, ifdOffset, tag, order);
        if (entry == null)
        {
            return;
        }

        if (entry.Count != 1)
        {
            throw JpegWriteValidationException.Create("Exif replacement scalar tag must contain one value.");
        }

        if (entry.Type == 3)
        {
            if (value < 0 || value > 65535)
            {
                throw JpegWriteValidationException.Create("Exif SHORT replacement value is out of range.");
            }

            WriteUInt16(payload, JpegConstants.ExifIdentifierLength + (int)entry.EntryOffset + 8, (int)value, order);
            payload[JpegConstants.ExifIdentifierLength + (int)entry.EntryOffset + 10] = 0;
            payload[JpegConstants.ExifIdentifierLength + (int)entry.EntryOffset + 11] = 0;
            return;
        }

        if (entry.Type == 4)
        {
            if (value < 0 || value > 4294967295L)
            {
                throw JpegWriteValidationException.Create("Exif LONG replacement value is out of range.");
            }

            WriteUInt32(payload, JpegConstants.ExifIdentifierLength + (int)entry.EntryOffset + 8, value, order);
            return;
        }

        throw JpegWriteValidationException.Create("Exif replacement scalar tag type is not supported.");
    }

    internal void ReplaceAsciiValue(byte[] payload, byte[] tiffData, long ifdOffset, int tag, string value, JpegTiffByteOrder order)
    {
        JpegExifIfdEntry? entry = FindEntry(tiffData, ifdOffset, tag, order);
        if (entry == null)
        {
            return;
        }

        if (entry.Type != 2)
        {
            throw JpegWriteValidationException.Create("Exif replacement ASCII tag type is not supported.");
        }

        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
        int capacity = GetAsciiCapacity(entry);
        if (bytes.Length + 1 > capacity)
        {
            throw JpegWriteValidationException.Create("Exif replacement ASCII value does not fit the existing tag.");
        }

        long valueOffset = GetValueDataOffset(entry);
        ClearPayloadRange(payload, JpegConstants.ExifIdentifierLength + (int)valueOffset, capacity);
        if (bytes.Length > 0)
        {
            System.Array.Copy(bytes, 0, payload, JpegConstants.ExifIdentifierLength + (int)valueOffset, bytes.Length);
        }
    }

    internal long ReadExistingUnsignedValue(byte[] tiffData, JpegExifIfdEntry entry, JpegTiffByteOrder order)
    {
        JpegExifTiffReader reader = new JpegExifTiffReader();
        if (entry.Type == 3)
        {
            return reader.ReadUInt16(tiffData, (int)entry.EntryOffset + 8, order);
        }

        if (entry.Type == 4)
        {
            return entry.ValueOffset;
        }

        throw JpegWriteValidationException.Create("Exif IFD pointer tag type is not supported.");
    }

    internal int GetAsciiCapacity(JpegExifIfdEntry entry)
    {
        if (entry.Count > 2147483647L)
        {
            throw JpegWriteValidationException.Create("Exif replacement ASCII value is too large.");
        }

        return (int)entry.Count;
    }

    internal long GetValueDataOffset(JpegExifIfdEntry entry)
    {
        if (entry.Count <= 4)
        {
            return entry.EntryOffset + 8;
        }

        return entry.ValueOffset;
    }

    internal void ClearPayloadRange(byte[] payload, int offset, int count)
    {
        JpegByteReader.ValidateByteArrayRange(payload, offset, count);
        int index = 0;
        while (index < count)
        {
            payload[offset + index] = 0;
            index = index + 1;
        }
    }

    internal void WriteUInt16(byte[] data, int offset, int value, JpegTiffByteOrder order)
    {
        JpegByteReader.ValidateByteArrayRange(data, offset, 2);
        if (order == JpegTiffByteOrder.LittleEndian)
        {
            data[offset] = (byte)(value & 255);
            data[offset + 1] = (byte)((value >> 8) & 255);
            return;
        }

        data[offset] = (byte)((value >> 8) & 255);
        data[offset + 1] = (byte)(value & 255);
    }

    internal void WriteUInt32(byte[] data, int offset, long value, JpegTiffByteOrder order)
    {
        JpegByteReader.ValidateByteArrayRange(data, offset, 4);
        if (order == JpegTiffByteOrder.LittleEndian)
        {
            data[offset] = (byte)(value & 255);
            data[offset + 1] = (byte)((value >> 8) & 255);
            data[offset + 2] = (byte)((value >> 16) & 255);
            data[offset + 3] = (byte)((value >> 24) & 255);
            return;
        }

        data[offset] = (byte)((value >> 24) & 255);
        data[offset + 1] = (byte)((value >> 16) & 255);
        data[offset + 2] = (byte)((value >> 8) & 255);
        data[offset + 3] = (byte)(value & 255);
    }
}
