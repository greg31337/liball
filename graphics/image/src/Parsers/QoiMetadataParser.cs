namespace image;

internal class QoiHeader
{
    internal ulong Width;
    internal ulong Height;
    internal int Channels;
    internal int ColorSpace;
    internal ulong PixelCount;
    internal ulong DecodedByteLength;
}

internal class QoiMetadataParser
{
    internal const int HeaderLength = 14;
    internal const int EndMarkerLength = 8;
    internal const int MinimumFileLength = 23;
    internal const ulong MaximumPixelCount = 400000000;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "qoi.reader.null", "QOI parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        byte[] bytes = ReadRange(reader, 0, HeaderLength);
        if (bytes.Length != HeaderLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "qoi.header.truncated", "QOI header was truncated.", "QOI Header", 0, HeaderLength);
            return Finish(document, reader);
        }
        if (!HasMagic(bytes))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "qoi.magic.invalid", "QOI magic was not present.", "QOI Header", 0, 4);
            return Finish(document, reader);
        }

        QoiHeader header = DecodeHeader(bytes);
        AddHeaderDirectory(document, reader, header);
        CaptureBlock(document, reader, options, "Header", "QOI Header", 0, HeaderLength);
        bool valid = ValidateHeader(document, header);
        ValidateEndMarker(document, reader, options);
        if (valid)
        {
            AddFrame(document, header);
        }
        ImageMetadataNormalizerCore.BuildSummary(document);
        return Finish(document, reader);
    }

    internal static QoiHeader DecodeHeader(byte[] bytes)
    {
        QoiHeader header = new QoiHeader();
        header.Width = ReadUnsigned32BigEndian(bytes, 4);
        header.Height = ReadUnsigned32BigEndian(bytes, 8);
        header.Channels = bytes[12];
        header.ColorSpace = bytes[13];
        header.PixelCount = header.Width * header.Height;
        if (header.Channels > 0 && header.PixelCount > ulong.MaxValue / (ulong)header.Channels)
        {
            header.DecodedByteLength = ulong.MaxValue;
        }
        else
        {
            header.DecodedByteLength = header.PixelCount * (ulong)header.Channels;
        }
        return header;
    }

    internal static bool ValidateHeader(ImageMetadataDocument document, QoiHeader header)
    {
        bool valid = true;
        if (header.Width == 0 || header.Height == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "qoi.dimensions.zero", "QOI width and height must be nonzero.", "QOI Header", 4, 8);
            valid = false;
        }
        if (header.Width > 2147483647 || header.Height > 2147483647)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "qoi.dimensions.range", "QOI dimensions exceeded the supported frame range.", "QOI Header", 4, 8);
            valid = false;
        }
        if (header.PixelCount > MaximumPixelCount)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "qoi.pixel-count.limit", "QOI image exceeded the format's 400 million pixel safety limit.", "QOI Header", 4, 8);
            valid = false;
        }
        if (header.Channels != 3 && header.Channels != 4)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "qoi.channels.invalid", "QOI channel count was not RGB or RGBA.", "QOI Header", 12, 1);
            valid = false;
        }
        if (header.ColorSpace != 0 && header.ColorSpace != 1)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "qoi.colorspace.invalid", "QOI color-space value was not sRGB or linear.", "QOI Header", 13, 1);
            valid = false;
        }
        return valid;
    }

    internal static void AddHeaderDirectory(ImageMetadataDocument document, BoundedStreamReader reader, QoiHeader header)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "Header", "QOI Header");
        directory.Path = "QOI/Header";
        directory.Offset = 0;
        directory.Length = HeaderLength;
        AddTag(directory, "Header.Magic", "Magic", MetadataValue.FromString("qoif"), 0, 4, "qoif");
        AddTag(directory, "Header.Width", "Width", MetadataValue.FromUnsignedInteger((ulong)header.Width), 4, 4, "pixels");
        AddTag(directory, "Header.Height", "Height", MetadataValue.FromUnsignedInteger((ulong)header.Height), 8, 4, "pixels");
        AddTag(directory, "Header.Channels", "Channels", MetadataValue.FromInteger(header.Channels), 12, 1, ChannelName(header.Channels));
        AddTag(directory, "Header.ColorSpace", "Color Space", MetadataValue.FromInteger(header.ColorSpace), 13, 1, ColorSpaceName(header.ColorSpace));
        AddTag(directory, "Header.PixelCount", "Pixel Count", MetadataValue.FromUnsignedInteger(header.PixelCount), 4, 8, "pixels");
        AddTag(directory, "Header.DecodedByteLength", "Decoded Byte Length", MetadataValue.FromUnsignedInteger(header.DecodedByteLength), 4, 10, "bytes");
        AddTag(directory, "Header.DataOffset", "Encoded Data Offset", MetadataValue.FromInteger(HeaderLength), 0, HeaderLength, "bytes");
        long encodedLength = EncodedDataLength(reader);
        AddTag(directory, "Header.EncodedDataLength", "Encoded Data Length", MetadataValue.FromInteger(encodedLength), HeaderLength, encodedLength, "bytes");
        long markerOffset = EndMarkerOffset(reader);
        AddTag(directory, "Header.EndMarkerOffset", "End Marker Offset", MetadataValue.FromInteger(markerOffset), markerOffset, EndMarkerLength, "bytes");
    }

    internal static void AddFrame(ImageMetadataDocument document, QoiHeader header)
    {
        ImageMetadataFrame frame = document.AddFrameWithDimensions((int)header.Width, (int)header.Height);
        frame.BitDepth = 8;
        frame.ChannelCount = header.Channels;
        frame.HasAlpha = header.Channels == 4;
        frame.ColorSpace = ImageColorSpaceKind.Rgb;
        frame.Compression = ImageCompressionKind.Unknown;
        frame.Orientation = ImageOrientation.Normal;
    }

    internal static void ValidateEndMarker(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options)
    {
        long length = reader.AccessibleLength();
        if (length < HeaderLength + EndMarkerLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "qoi.end-marker.truncated", "QOI end marker was truncated or missing.", "QOI End Marker", HeaderLength, EndMarkerLength);
            return;
        }
        long markerOffset = length - EndMarkerLength;
        byte[] marker = ReadRange(reader, markerOffset, EndMarkerLength);
        if (!HasEndMarker(marker))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "qoi.end-marker.invalid", "QOI end marker did not match the defined eight-byte marker.", "QOI End Marker", markerOffset, EndMarkerLength);
            return;
        }
        CaptureBlock(document, reader, options, "EndMarker", "QOI End Marker", markerOffset, EndMarkerLength);
        if (length < MinimumFileLength)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "qoi.data.empty", "QOI file contained no encoded pixel chunks.", "QOI Data", HeaderLength, 0);
        }
    }

    internal static long EncodedDataLength(BoundedStreamReader reader)
    {
        if (reader == null || reader.AccessibleLength() < HeaderLength + EndMarkerLength)
        {
            return 0;
        }
        return reader.AccessibleLength() - HeaderLength - EndMarkerLength;
    }

    internal static long EndMarkerOffset(BoundedStreamReader reader)
    {
        if (reader == null || reader.AccessibleLength() < HeaderLength + EndMarkerLength)
        {
            return -1;
        }
        return reader.AccessibleLength() - EndMarkerLength;
    }

    internal static string ChannelName(int channels)
    {
        if (channels == 3) return "RGB";
        if (channels == 4) return "RGBA";
        return "Unknown";
    }

    internal static string ColorSpaceName(int colorSpace)
    {
        if (colorSpace == 0) return "sRGB with linear alpha";
        if (colorSpace == 1) return "Linear color and alpha";
        return "Unknown";
    }

    internal static bool HasMagic(byte[] bytes)
    {
        return bytes != null && bytes.Length >= 4 && bytes[0] == 113 && bytes[1] == 111 && bytes[2] == 105 && bytes[3] == 102;
    }

    internal static bool HasEndMarker(byte[] bytes)
    {
        if (bytes == null || bytes.Length != EndMarkerLength)
        {
            return false;
        }
        int index = 0;
        while (index < EndMarkerLength - 1)
        {
            if (bytes[index] != 0)
            {
                return false;
            }
            index = index + 1;
        }
        return bytes[EndMarkerLength - 1] == 1;
    }

    internal static ulong ReadUnsigned32BigEndian(byte[] bytes, int offset)
    {
        return (ulong)bytes[offset] << 24 | (ulong)bytes[offset + 1] << 16 | (ulong)bytes[offset + 2] << 8 | bytes[offset + 3];
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string keyName, string name, MetadataValue value, long offset, long length, string display)
    {
        MetadataTag tag = directory.AddTagWithValue("Qoi." + keyName, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = display;
        tag.Confidence = 100;
        return tag;
    }

    internal static void CaptureBlock(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, string blockType, string name, long offset, int length)
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
        if (length > MaxRawBlockBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "qoi.raw.block.limit", "QOI raw block exceeded the configured raw block limit.", blockType, offset, length);
            return;
        }
        block.Data = ReadRange(reader, offset, length);
    }

    internal static byte[] ReadRange(BoundedStreamReader reader, long offset, int length)
    {
        if (reader == null || length <= 0 || offset < 0 || offset > reader.AccessibleLength() || length > reader.AccessibleLength() - offset)
        {
            return new byte[0];
        }
        if (!reader.SeekAbsolute(offset))
        {
            return new byte[0];
        }
        return reader.ReadBytes(length);
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
