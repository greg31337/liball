using System.Text;

namespace image;

internal class TgaHeader
{
    internal int IdLength;
    internal int ColorMapType;
    internal int ImageType;
    internal int ColorMapFirstEntry;
    internal int ColorMapLength;
    internal int ColorMapEntryBits;
    internal int XOrigin;
    internal int YOrigin;
    internal int Width;
    internal int Height;
    internal int PixelDepth;
    internal int Descriptor;
    internal long ImageIdOffset;
    internal long ColorMapOffset;
    internal long ColorMapByteLength;
    internal long ImageDataOffset;
}

internal class TgaMetadataParser
{
    internal const int HeaderLength = 18;
    internal const int FooterLength = 26;
    internal const int ExtensionLength = 495;
    internal const int DeveloperRecordLength = 10;
    internal const string FooterSignature = "TRUEVISION-XFILE.";

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "tga.reader.null", "TGA parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        byte[] headerBytes = ReadRange(reader, 0, HeaderLength);
        if (headerBytes.Length != HeaderLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tga.header.truncated", "TGA header was truncated.", "TGA Header", 0, HeaderLength);
            return Finish(document, reader);
        }

        TgaHeader header = DecodeHeader(headerBytes);
        AddHeaderDirectory(document, header);
        CaptureBlock(document, reader, options, "Header", "TGA Header", 0, HeaderLength);
        bool validHeader = ValidateHeader(document, reader, header);
        ParseImageId(document, reader, options, header);
        ParseColorMap(document, reader, options, header);
        if (validHeader && header.ImageType != 0)
        {
            AddFrame(document, header);
        }

        ParseFooter(document, reader, options);
        ImageMetadataNormalizerCore.BuildSummary(document);
        return Finish(document, reader);
    }

    internal static TgaHeader DecodeHeader(byte[] bytes)
    {
        TgaHeader header = new TgaHeader();
        header.IdLength = bytes[0];
        header.ColorMapType = bytes[1];
        header.ImageType = bytes[2];
        header.ColorMapFirstEntry = ReadUnsigned16(bytes, 3);
        header.ColorMapLength = ReadUnsigned16(bytes, 5);
        header.ColorMapEntryBits = bytes[7];
        header.XOrigin = ReadUnsigned16(bytes, 8);
        header.YOrigin = ReadUnsigned16(bytes, 10);
        header.Width = ReadUnsigned16(bytes, 12);
        header.Height = ReadUnsigned16(bytes, 14);
        header.PixelDepth = bytes[16];
        header.Descriptor = bytes[17];
        header.ImageIdOffset = HeaderLength;
        header.ColorMapOffset = HeaderLength + header.IdLength;
        header.ColorMapByteLength = ((long)header.ColorMapEntryBits + 7) / 8 * header.ColorMapLength;
        header.ImageDataOffset = header.ColorMapOffset + header.ColorMapByteLength;
        return header;
    }

    internal static bool ValidateHeader(ImageMetadataDocument document, BoundedStreamReader reader, TgaHeader header)
    {
        bool valid = true;
        if (header.ColorMapType != 0 && header.ColorMapType != 1)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tga.color-map-type.invalid", "TGA color map type was not zero or one.", "TGA Header", 1, 1);
            valid = false;
        }
        if (!IsImageTypeSupported(header.ImageType))
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "tga.image-type.unsupported", "TGA image type was not a supported uncompressed or run-length encoded type.", "TGA Header");
            valid = false;
        }
        if (header.ImageType != 0 && (header.Width == 0 || header.Height == 0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tga.dimensions.invalid", "TGA image dimensions must be nonzero.", "TGA Header", 12, 4);
            valid = false;
        }
        if (!IsPixelDepthSupported(header.ImageType, header.PixelDepth))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tga.pixel-depth.invalid", "TGA pixel depth was incompatible with the image type.", "TGA Header", 16, 1);
            valid = false;
        }
        if (IsColorMapped(header.ImageType) && header.ColorMapType != 1)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tga.color-map.missing", "Color-mapped TGA image did not declare a color map.", "TGA Header", 1, 2);
            valid = false;
        }
        if (header.ColorMapType == 1 && (header.ColorMapLength == 0 || !IsColorMapDepthSupported(header.ColorMapEntryBits)))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tga.color-map.invalid", "TGA color map length or entry depth was invalid.", "TGA Header", 5, 3);
            valid = false;
        }
        if ((header.Descriptor & 192) != 0)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "tga.descriptor.reserved", "TGA image descriptor reserved bits were nonzero.", "TGA Header", 17, 1);
        }
        if (!RangeFits(header.ImageIdOffset, header.IdLength, reader.AccessibleLength()))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tga.image-id.truncated", "TGA image identification field exceeded the source.", "TGA Image ID", header.ImageIdOffset, header.IdLength);
            valid = false;
        }
        if (header.ColorMapType == 1 && !RangeFits(header.ColorMapOffset, header.ColorMapByteLength, reader.AccessibleLength()))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tga.color-map.truncated", "TGA color map exceeded the source.", "TGA Color Map", header.ColorMapOffset, header.ColorMapByteLength);
            valid = false;
        }
        return valid;
    }

    internal static void AddHeaderDirectory(ImageMetadataDocument document, TgaHeader header)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "Header", "TGA Header");
        directory.Path = "TGA/Header";
        directory.Offset = 0;
        directory.Length = HeaderLength;
        AddTag(directory, "Header.IdLength", "Image ID Length", MetadataValue.FromInteger(header.IdLength), 0, 1, "bytes");
        AddTag(directory, "Header.ColorMapType", "Color Map Type", MetadataValue.FromInteger(header.ColorMapType), 1, 1, ColorMapTypeName(header.ColorMapType));
        AddTag(directory, "Header.ImageType", "Image Type", MetadataValue.FromInteger(header.ImageType), 2, 1, ImageTypeName(header.ImageType));
        AddTag(directory, "Header.ColorMapFirstEntry", "Color Map First Entry", MetadataValue.FromInteger(header.ColorMapFirstEntry), 3, 2, "");
        AddTag(directory, "Header.ColorMapLength", "Color Map Length", MetadataValue.FromInteger(header.ColorMapLength), 5, 2, "entries");
        AddTag(directory, "Header.ColorMapEntryBits", "Color Map Entry Depth", MetadataValue.FromInteger(header.ColorMapEntryBits), 7, 1, "bits");
        AddTag(directory, "Header.XOrigin", "X Origin", MetadataValue.FromInteger(header.XOrigin), 8, 2, "pixels");
        AddTag(directory, "Header.YOrigin", "Y Origin", MetadataValue.FromInteger(header.YOrigin), 10, 2, "pixels");
        AddTag(directory, "Header.Width", "Width", MetadataValue.FromInteger(header.Width), 12, 2, "pixels");
        AddTag(directory, "Header.Height", "Height", MetadataValue.FromInteger(header.Height), 14, 2, "pixels");
        AddTag(directory, "Header.PixelDepth", "Pixel Depth", MetadataValue.FromInteger(header.PixelDepth), 16, 1, "bits");
        AddTag(directory, "Header.AttributeBits", "Attribute Bits", MetadataValue.FromInteger(header.Descriptor & 15), 17, 1, "bits");
        AddTag(directory, "Header.RightToLeft", "Right-to-left Origin", MetadataValue.FromBoolean((header.Descriptor & 16) != 0), 17, 1, "");
        AddTag(directory, "Header.TopToBottom", "Top-to-bottom Origin", MetadataValue.FromBoolean((header.Descriptor & 32) != 0), 17, 1, "");
        AddTag(directory, "Header.ImageDataOffset", "Image Data Offset", MetadataValue.FromInteger(header.ImageDataOffset), 0, HeaderLength, "bytes");
    }

    internal static void ParseImageId(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, TgaHeader header)
    {
        if (header.IdLength == 0 || !RangeFits(header.ImageIdOffset, header.IdLength, reader.AccessibleLength()))
        {
            return;
        }
        byte[] bytes = ReadRange(reader, header.ImageIdOffset, header.IdLength);
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "ImageId", "TGA Image Identification");
        directory.Path = "TGA/ImageId";
        directory.Offset = header.ImageIdOffset;
        directory.Length = header.IdLength;
        AddTag(directory, "ImageId", "Image Identification", MetadataValue.FromString(DecodeText(bytes, 0, bytes.Length)), header.ImageIdOffset, header.IdLength, "");
        CaptureBlock(document, reader, options, "ImageId", "TGA Image Identification", header.ImageIdOffset, header.IdLength);
    }

    internal static void ParseColorMap(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, TgaHeader header)
    {
        if (header.ColorMapType != 1 || header.ColorMapLength == 0 || header.ColorMapByteLength == 0)
        {
            return;
        }
        if (!RangeFits(header.ColorMapOffset, header.ColorMapByteLength, reader.AccessibleLength()))
        {
            return;
        }
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "ColorMap", "TGA Color Map");
        directory.Path = "TGA/ColorMap";
        directory.Offset = header.ColorMapOffset;
        directory.Length = header.ColorMapByteLength;
        AddTag(directory, "ColorMap.FirstEntry", "First Entry Index", MetadataValue.FromInteger(header.ColorMapFirstEntry), header.ColorMapOffset, header.ColorMapByteLength, "");
        AddTag(directory, "ColorMap.EntryCount", "Entry Count", MetadataValue.FromInteger(header.ColorMapLength), header.ColorMapOffset, header.ColorMapByteLength, "");
        AddTag(directory, "ColorMap.EntryBits", "Entry Depth", MetadataValue.FromInteger(header.ColorMapEntryBits), header.ColorMapOffset, header.ColorMapByteLength, "bits");
        AddTag(directory, "ColorMap.ByteLength", "Byte Length", MetadataValue.FromInteger(header.ColorMapByteLength), header.ColorMapOffset, header.ColorMapByteLength, "bytes");
        CaptureBlock(document, reader, options, "ColorMap", "TGA Color Map", header.ColorMapOffset, header.ColorMapByteLength);
    }

    internal static void AddFrame(ImageMetadataDocument document, TgaHeader header)
    {
        ImageMetadataFrame frame = document.AddFrameWithDimensions(header.Width, header.Height);
        frame.BitDepth = SampleBitDepth(header);
        frame.ChannelCount = ChannelCount(header);
        frame.HasAlpha = HasAlpha(header);
        frame.ColorSpace = ColorSpace(header);
        frame.Compression = IsRunLengthEncoded(header.ImageType) ? ImageCompressionKind.Rle : ImageCompressionKind.None;
        frame.Orientation = Orientation(header.Descriptor);
    }

    internal static void ParseFooter(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options)
    {
        long footerOffset = reader.AccessibleLength() - FooterLength;
        if (footerOffset < HeaderLength)
        {
            return;
        }
        byte[] footer = ReadRange(reader, footerOffset, FooterLength);
        if (footer.Length != FooterLength || !HasFooterSignature(footer))
        {
            return;
        }

        long extensionOffset = ReadUnsigned32(footer, 0);
        long developerOffset = ReadUnsigned32(footer, 4);
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "Footer", "TGA 2.0 Footer");
        directory.Path = "TGA/Footer";
        directory.Offset = footerOffset;
        directory.Length = FooterLength;
        AddTag(directory, "Footer.ExtensionOffset", "Extension Area Offset", MetadataValue.FromInteger(extensionOffset), footerOffset, 4, "bytes");
        AddTag(directory, "Footer.DeveloperOffset", "Developer Directory Offset", MetadataValue.FromInteger(developerOffset), footerOffset + 4, 4, "bytes");
        AddTag(directory, "Footer.Signature", "Signature", MetadataValue.FromString(FooterSignature), footerOffset + 8, 18, FooterSignature);
        CaptureBlock(document, reader, options, "Footer", "TGA 2.0 Footer", footerOffset, FooterLength);
        if (extensionOffset != 0)
        {
            ParseExtension(document, reader, options, extensionOffset, footerOffset);
        }
        if (developerOffset != 0)
        {
            ParseDeveloperDirectory(document, reader, options, developerOffset, footerOffset);
        }
    }

    internal static void ParseExtension(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, long offset, long footerOffset)
    {
        if (!RangeFits(offset, 2, footerOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tga.extension.offset.invalid", "TGA extension area offset did not identify data before the footer.", "TGA Extension", offset, 2);
            return;
        }
        byte[] sizeBytes = ReadRange(reader, offset, 2);
        if (sizeBytes.Length != 2)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tga.extension.truncated", "TGA extension area size field was truncated.", "TGA Extension", offset, 2);
            return;
        }
        int declaredSize = ReadUnsigned16(sizeBytes, 0);
        if (declaredSize < ExtensionLength)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tga.extension.size.invalid", "TGA extension area was smaller than the defined 495-byte structure.", "TGA Extension", offset, declaredSize);
            return;
        }
        if (declaredSize > MaxSegmentBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tga.extension.segment.limit", "TGA extension area exceeded the configured segment limit.", "TGA Extension", offset, declaredSize);
            return;
        }
        if (!RangeFits(offset, declaredSize, footerOffset))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tga.extension.truncated", "TGA extension area exceeded the data preceding the footer.", "TGA Extension", offset, declaredSize);
            return;
        }
        byte[] bytes = ReadRange(reader, offset, ExtensionLength);
        if (bytes.Length != ExtensionLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tga.extension.truncated", "TGA extension area was truncated.", "TGA Extension", offset, ExtensionLength);
            return;
        }

        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "Extension", "TGA Extension Area");
        directory.Path = "TGA/Extension";
        directory.Offset = offset;
        directory.Length = declaredSize;
        AddTag(directory, "Extension.Size", "Extension Size", MetadataValue.FromInteger(declaredSize), offset, 2, "bytes");
        AddTextTag(directory, "Extension.AuthorName", "Author Name", bytes, 2, 41, offset);
        AddTextTag(directory, "Extension.AuthorComments", "Author Comments", DecodeCommentLines(bytes, 43), 43, 324, offset);
        AddTimestampTags(document, directory, bytes, offset);
        AddTextTag(directory, "Extension.JobName", "Job Name", bytes, 379, 41, offset);
        AddTag(directory, "Extension.JobHours", "Job Hours", MetadataValue.FromInteger(ReadUnsigned16(bytes, 420)), offset + 420, 2, "hours");
        AddTag(directory, "Extension.JobMinutes", "Job Minutes", MetadataValue.FromInteger(ReadUnsigned16(bytes, 422)), offset + 422, 2, "minutes");
        AddTag(directory, "Extension.JobSeconds", "Job Seconds", MetadataValue.FromInteger(ReadUnsigned16(bytes, 424)), offset + 424, 2, "seconds");
        AddTextTag(directory, "Extension.SoftwareId", "Software ID", bytes, 426, 41, offset);
        int versionNumber = ReadUnsigned16(bytes, 467);
        string versionLetter = DecodeText(bytes, 469, 1);
        AddTag(directory, "Extension.SoftwareVersionNumber", "Software Version Number", MetadataValue.FromInteger(versionNumber), offset + 467, 2, FormatSoftwareVersion(versionNumber, versionLetter));
        AddTag(directory, "Extension.SoftwareVersionLetter", "Software Version Letter", MetadataValue.FromString(versionLetter), offset + 469, 1, versionLetter);
        AddTag(directory, "Extension.KeyColor", "Key Color", MetadataValue.FromBytes(CopyBytes(bytes, 470, 4)), offset + 470, 4, "BGRA");
        AddTag(directory, "Extension.PixelAspectRatio", "Pixel Aspect Ratio", MetadataValue.FromRational(ReadUnsigned16(bytes, 474), ReadUnsigned16(bytes, 476)), offset + 474, 4, "");
        AddTag(directory, "Extension.Gamma", "Gamma", MetadataValue.FromRational(ReadUnsigned16(bytes, 478), ReadUnsigned16(bytes, 480)), offset + 478, 4, "");
        AddTag(directory, "Extension.ColorCorrectionOffset", "Color Correction Table Offset", MetadataValue.FromInteger(ReadUnsigned32(bytes, 482)), offset + 482, 4, "bytes");
        AddTag(directory, "Extension.PostageStampOffset", "Postage Stamp Offset", MetadataValue.FromInteger(ReadUnsigned32(bytes, 486)), offset + 486, 4, "bytes");
        AddTag(directory, "Extension.ScanLineOffset", "Scan Line Table Offset", MetadataValue.FromInteger(ReadUnsigned32(bytes, 490)), offset + 490, 4, "bytes");
        AddTag(directory, "Extension.AttributesType", "Attributes Type", MetadataValue.FromInteger(bytes[494]), offset + 494, 1, AttributesTypeName(bytes[494]));
        CaptureBlock(document, reader, options, "Extension", "TGA Extension Area", offset, declaredSize);
    }

    internal static void AddTimestampTags(ImageMetadataDocument document, MetadataDirectory directory, byte[] bytes, long extensionOffset)
    {
        int month = ReadUnsigned16(bytes, 367);
        int day = ReadUnsigned16(bytes, 369);
        int year = ReadUnsigned16(bytes, 371);
        int hour = ReadUnsigned16(bytes, 373);
        int minute = ReadUnsigned16(bytes, 375);
        int second = ReadUnsigned16(bytes, 377);
        AddTag(directory, "Extension.TimestampMonth", "Timestamp Month", MetadataValue.FromInteger(month), extensionOffset + 367, 2, "");
        AddTag(directory, "Extension.TimestampDay", "Timestamp Day", MetadataValue.FromInteger(day), extensionOffset + 369, 2, "");
        AddTag(directory, "Extension.TimestampYear", "Timestamp Year", MetadataValue.FromInteger(year), extensionOffset + 371, 2, "");
        AddTag(directory, "Extension.TimestampHour", "Timestamp Hour", MetadataValue.FromInteger(hour), extensionOffset + 373, 2, "");
        AddTag(directory, "Extension.TimestampMinute", "Timestamp Minute", MetadataValue.FromInteger(minute), extensionOffset + 375, 2, "");
        AddTag(directory, "Extension.TimestampSecond", "Timestamp Second", MetadataValue.FromInteger(second), extensionOffset + 377, 2, "");
        if (month == 0 && day == 0 && year == 0 && hour == 0 && minute == 0 && second == 0)
        {
            return;
        }
        if (!ValidTimestamp(month, day, year, hour, minute, second))
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "tga.extension.timestamp.invalid", "TGA extension timestamp contained an invalid date or time component.", "TGA Extension", extensionOffset + 367, 12);
            return;
        }
        string timestamp = year.ToString("D4") + ":" + month.ToString("D2") + ":" + day.ToString("D2") + " " + hour.ToString("D2") + ":" + minute.ToString("D2") + ":" + second.ToString("D2");
        AddTag(directory, "Extension.Timestamp", "Timestamp", MetadataValue.FromDateText(timestamp), extensionOffset + 367, 12, timestamp);
    }

    internal static void ParseDeveloperDirectory(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, long offset, long footerOffset)
    {
        if (!RangeFits(offset, 2, footerOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tga.developer.offset.invalid", "TGA developer directory offset did not identify data before the footer.", "TGA Developer Directory", offset, 2);
            return;
        }
        byte[] countBytes = ReadRange(reader, offset, 2);
        if (countBytes.Length != 2)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tga.developer.truncated", "TGA developer directory count was truncated.", "TGA Developer Directory", offset, 2);
            return;
        }
        int count = ReadUnsigned16(countBytes, 0);
        int maximum = MaxTagCount(options);
        if (maximum < 0 || count > maximum)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tga.developer.count.limit", "TGA developer directory exceeded the configured tag limit.", "TGA Developer Directory", offset, count);
            return;
        }
        long directoryLength = 2 + (long)count * DeveloperRecordLength;
        if (directoryLength > MaxSegmentBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tga.developer.segment.limit", "TGA developer directory exceeded the configured segment limit.", "TGA Developer Directory", offset, directoryLength);
            return;
        }
        if (!RangeFits(offset, directoryLength, footerOffset))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tga.developer.truncated", "TGA developer directory was truncated.", "TGA Developer Directory", offset, directoryLength);
            return;
        }

        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "Developer", "TGA Developer Directory");
        directory.Path = "TGA/Developer";
        directory.Offset = offset;
        directory.Length = directoryLength;
        AddTag(directory, "Developer.EntryCount", "Developer Entry Count", MetadataValue.FromInteger(count), offset, 2, "entries");
        int index = 0;
        while (index < count)
        {
            long recordOffset = offset + 2 + (long)index * DeveloperRecordLength;
            byte[] record = ReadRange(reader, recordOffset, DeveloperRecordLength);
            if (record.Length != DeveloperRecordLength)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "tga.developer.truncated", "TGA developer directory record was truncated.", "TGA Developer Directory", recordOffset, DeveloperRecordLength);
                return;
            }
            int tag = ReadUnsigned16(record, 0);
            long dataOffset = ReadUnsigned32(record, 2);
            long dataSize = ReadUnsigned32(record, 6);
            MetadataDirectory child = directory.AddChildWithName(MetadataFamily.Format, "DeveloperEntry", "TGA Developer Entry");
            child.Path = "TGA/Developer/Entry" + index.ToString();
            child.Offset = recordOffset;
            child.Length = DeveloperRecordLength;
            child.RepeatIndex = index;
            AddTag(child, "Developer.Tag", "Developer Tag", MetadataValue.FromInteger(tag), recordOffset, 2, "");
            AddTag(child, "Developer.DataOffset", "Developer Data Offset", MetadataValue.FromInteger(dataOffset), recordOffset + 2, 4, "bytes");
            AddTag(child, "Developer.DataSize", "Developer Data Size", MetadataValue.FromInteger(dataSize), recordOffset + 6, 4, "bytes");
            ParseDeveloperPayload(document, reader, options, tag, index, dataOffset, dataSize, footerOffset);
            index = index + 1;
        }
        CaptureBlock(document, reader, options, "DeveloperDirectory", "TGA Developer Directory", offset, directoryLength);
    }

    internal static void ParseDeveloperPayload(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, int tag, int index, long offset, long length, long footerOffset)
    {
        if (length == 0)
        {
            return;
        }
        if (length > MaxSegmentBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tga.developer.data.segment.limit", "TGA developer entry data exceeded the configured segment limit.", "TGA Developer Data", offset, length);
            return;
        }
        if (!RangeFits(offset, length, footerOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tga.developer.data.range", "TGA developer entry data range was outside data preceding the footer.", "TGA Developer Data", offset, length);
            return;
        }
        CaptureBlock(document, reader, options, "DeveloperData." + tag.ToString(), "TGA Developer Data " + index.ToString(), offset, length);
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string keyName, string name, MetadataValue value, long offset, long length, string display)
    {
        MetadataTag tag = directory.AddTagWithValue("Tga." + keyName, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = display;
        tag.Confidence = 100;
        return tag;
    }

    internal static void AddTextTag(MetadataDirectory directory, string key, string name, byte[] bytes, int offset, int length, long baseOffset)
    {
        AddTextTag(directory, key, name, DecodeText(bytes, offset, length), offset, length, baseOffset);
    }

    internal static void AddTextTag(MetadataDirectory directory, string key, string name, string value, int offset, int length, long baseOffset)
    {
        AddTag(directory, key, name, MetadataValue.FromString(value), baseOffset + offset, length, value);
    }

    internal static void CaptureBlock(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, string blockType, string name, long offset, long length)
    {
        if (!CaptureRawBlocks(options))
        {
            return;
        }
        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Format, blockType, name, offset, length);
        if (!CaptureRawBlockData(options))
        {
            return;
        }
        if (length > MaxRawBlockBytes(options) || length > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "tga.raw.block.limit", "TGA raw block exceeded the configured raw block limit.", blockType, offset, length);
            return;
        }
        block.Data = ReadRange(reader, offset, (int)length);
    }

    internal static byte[] ReadRange(BoundedStreamReader reader, long offset, int length)
    {
        if (reader == null || length <= 0 || !RangeFits(offset, length, reader.AccessibleLength()))
        {
            return new byte[0];
        }
        if (!reader.SeekAbsolute(offset))
        {
            return new byte[0];
        }
        return reader.ReadBytes(length);
    }

    internal static bool RangeFits(long offset, long length, long boundary)
    {
        if (offset < 0 || length < 0 || boundary < 0 || offset > boundary)
        {
            return false;
        }
        return length <= boundary - offset;
    }

    internal static bool HasFooterSignature(byte[] footer)
    {
        int index = 0;
        while (index < FooterSignature.Length)
        {
            if (footer[8 + index] != (byte)FooterSignature[index])
            {
                return false;
            }
            index = index + 1;
        }
        return footer[25] == 0;
    }

    internal static bool IsImageTypeSupported(int imageType)
    {
        return imageType == 0 || imageType == 1 || imageType == 2 || imageType == 3 || imageType == 9 || imageType == 10 || imageType == 11;
    }

    internal static bool IsColorMapped(int imageType)
    {
        return imageType == 1 || imageType == 9;
    }

    internal static bool IsTrueColor(int imageType)
    {
        return imageType == 2 || imageType == 10;
    }

    internal static bool IsGray(int imageType)
    {
        return imageType == 3 || imageType == 11;
    }

    internal static bool IsRunLengthEncoded(int imageType)
    {
        return imageType == 9 || imageType == 10 || imageType == 11;
    }

    internal static bool IsPixelDepthSupported(int imageType, int depth)
    {
        if (imageType == 0)
        {
            return depth == 0;
        }
        if (IsColorMapped(imageType))
        {
            return depth == 8 || depth == 16;
        }
        if (IsTrueColor(imageType))
        {
            return depth == 15 || depth == 16 || depth == 24 || depth == 32;
        }
        if (IsGray(imageType))
        {
            return depth == 8 || depth == 16;
        }
        return false;
    }

    internal static bool IsColorMapDepthSupported(int depth)
    {
        return depth == 15 || depth == 16 || depth == 24 || depth == 32;
    }

    internal static int SampleBitDepth(TgaHeader header)
    {
        if (IsColorMapped(header.ImageType))
        {
            return header.PixelDepth;
        }
        if (IsGray(header.ImageType))
        {
            return header.PixelDepth == 16 ? 8 : header.PixelDepth;
        }
        if (header.PixelDepth == 15 || header.PixelDepth == 16)
        {
            return 5;
        }
        return 8;
    }

    internal static int ChannelCount(TgaHeader header)
    {
        if (IsColorMapped(header.ImageType))
        {
            return 1;
        }
        if (IsGray(header.ImageType))
        {
            return header.PixelDepth == 16 ? 2 : 1;
        }
        return header.PixelDepth == 32 ? 4 : 3;
    }

    internal static bool HasAlpha(TgaHeader header)
    {
        int attributes = header.Descriptor & 15;
        return attributes > 0 && (header.PixelDepth == 16 || header.PixelDepth == 32);
    }

    internal static ImageColorSpaceKind ColorSpace(TgaHeader header)
    {
        if (IsColorMapped(header.ImageType)) return ImageColorSpaceKind.Indexed;
        if (IsGray(header.ImageType)) return ImageColorSpaceKind.Gray;
        return ImageColorSpaceKind.Rgb;
    }

    internal static ImageOrientation Orientation(int descriptor)
    {
        bool right = (descriptor & 16) != 0;
        bool top = (descriptor & 32) != 0;
        if (top && !right) return ImageOrientation.Normal;
        if (top && right) return ImageOrientation.MirrorHorizontal;
        if (!top && !right) return ImageOrientation.MirrorVertical;
        return ImageOrientation.Rotate180;
    }

    internal static string ImageTypeName(int imageType)
    {
        switch (imageType)
        {
            case 0: return "No image data";
            case 1: return "Uncompressed color-mapped";
            case 2: return "Uncompressed true-color";
            case 3: return "Uncompressed grayscale";
            case 9: return "Run-length encoded color-mapped";
            case 10: return "Run-length encoded true-color";
            case 11: return "Run-length encoded grayscale";
            default: return "Unknown";
        }
    }

    internal static string ColorMapTypeName(int colorMapType)
    {
        if (colorMapType == 0) return "No color map";
        if (colorMapType == 1) return "Color map present";
        return "Unknown";
    }

    internal static string AttributesTypeName(int type)
    {
        switch (type)
        {
            case 0: return "No alpha data";
            case 1: return "Undefined alpha data, ignored";
            case 2: return "Undefined alpha data, retained";
            case 3: return "Useful alpha data";
            case 4: return "Premultiplied alpha data";
            default: return "Unknown";
        }
    }

    internal static string DecodeCommentLines(byte[] bytes, int offset)
    {
        string result = "";
        int index = 0;
        while (index < 4)
        {
            string line = DecodeText(bytes, offset + index * 81, 81);
            if (line.Length > 0)
            {
                if (result.Length > 0)
                {
                    result = result + "\n";
                }
                result = result + line;
            }
            index = index + 1;
        }
        return result;
    }

    internal static string DecodeText(byte[] bytes, int offset, int length)
    {
        if (bytes == null || offset < 0 || length <= 0 || offset > bytes.Length)
        {
            return "";
        }
        int end = offset + length;
        if (end > bytes.Length)
        {
            end = bytes.Length;
        }
        int terminator = offset;
        while (terminator < end && bytes[terminator] != 0)
        {
            terminator = terminator + 1;
        }
        return Encoding.ASCII.GetString(bytes, offset, terminator - offset).TrimEnd(' ', '\t', '\r', '\n');
    }

    internal static string FormatSoftwareVersion(int number, string letter)
    {
        int major = number / 100;
        int minor = number % 100;
        return major.ToString() + "." + minor.ToString("D2") + letter;
    }

    internal static bool ValidTimestamp(int month, int day, int year, int hour, int minute, int second)
    {
        if (year == 0 || month < 1 || month > 12 || day < 1 || hour > 23 || minute > 59 || second > 59)
        {
            return false;
        }
        int maximumDay = 31;
        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            maximumDay = 30;
        }
        if (month == 2)
        {
            maximumDay = IsLeapYear(year) ? 29 : 28;
        }
        return day <= maximumDay;
    }

    internal static bool IsLeapYear(int year)
    {
        return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
    }

    internal static int ReadUnsigned16(byte[] bytes, int offset)
    {
        return bytes[offset] | bytes[offset + 1] << 8;
    }

    internal static long ReadUnsigned32(byte[] bytes, int offset)
    {
        return (long)bytes[offset] | (long)bytes[offset + 1] << 8 | (long)bytes[offset + 2] << 16 | (long)bytes[offset + 3] << 24;
    }

    internal static byte[] CopyBytes(byte[] bytes, int offset, int length)
    {
        if (bytes == null || offset < 0 || length <= 0 || offset > bytes.Length)
        {
            return new byte[0];
        }
        int actual = length;
        if (actual > bytes.Length - offset)
        {
            actual = bytes.Length - offset;
        }
        byte[] copy = new byte[actual];
        int index = 0;
        while (index < actual)
        {
            copy[index] = bytes[offset + index];
            index = index + 1;
        }
        return copy;
    }

    internal static int MaxTagCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxTagCount : options.MaxTagCount;
    }

    internal static long MaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
    }

    internal static long MaxRawBlockBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxRawBlockBytes : options.MaxRawBlockBytes;
    }

    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions options)
    {
        return options == null || options.CaptureRawBlocks;
    }

    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions options)
    {
        return options != null && options.CaptureRawBlockData;
    }

    internal static ImageMetadataDocument Finish(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (reader != null && reader.Diagnostics != null)
        {
            foreach (MetadataDiagnostic diagnostic in reader.Diagnostics)
            {
                document.AddDiagnostic(diagnostic);
            }
            reader.SeekAbsolute(0);
        }
        return document;
    }
}
