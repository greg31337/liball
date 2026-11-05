using System.Collections.Generic;

namespace image;

internal class JpegXlMetadataExtractor
{
    internal static void ExtractCodestream(ImageMetadataDocument document, JpegXlCodestreamHeader header, ImageMetadataReaderOptions options)
    {
        if (document == null || header == null || !header.SizeValid || !header.MetadataValid)
        {
            return;
        }

        if (header.Width > 2147483647 || header.Height > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jxl.codestream.dimensions.limit", "JPEG XL dimensions exceeded the supported frame range.", "Codestream", header.SourceOffset, header.HeaderByteLength);
            return;
        }

        if (document.Frames.Count >= GetMaxFrameCount(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jxl.frame.count.limit", "JPEG XL frame count exceeded the configured limit.", "Codestream", header.SourceOffset, header.HeaderByteLength);
            return;
        }

        ImageMetadataFrame frame = document.AddFrameWithDimensions((int)header.Width, (int)header.Height);
        frame.BitDepth = (int)header.BitsPerSample;
        frame.ChannelCount = ColorChannelCount(header.ColorSpace) + (int)header.ExtraChannelCount;
        frame.HasAlpha = header.HasAlpha;
        frame.IsAnimated = header.HaveAnimation;
        frame.Compression = ImageCompressionKind.JpegXl;
        frame.ColorSpace = NormalizedColorSpace(header);
        frame.Orientation = (ImageOrientation)header.Orientation;
    }

    internal static void ExtractBoxes(ImageMetadataDocument document, BoundedStreamReader reader, List<JpegXlBox> boxes, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || boxes == null)
        {
            return;
        }

        foreach (JpegXlBox box in boxes)
        {
            if (box == null)
            {
                continue;
            }

            if (box.Type == "Exif")
            {
                ExtractExifBox(document, reader, box, options);
            }
            else if (box.Type == "xml ")
            {
                ExtractXmpBox(document, reader, box, options);
            }
            else if (box.Type == "jumb")
            {
                ExtractJumbfBox(document, reader, box, options);
            }
            else if (box.Type == "brob")
            {
                ExtractCompressedBox(document, reader, box, options);
            }

            ExtractBoxes(document, reader, box.Children, options);
        }
    }

    internal static void ExtractExifBox(ImageMetadataDocument document, BoundedStreamReader reader, JpegXlBox box, ImageMetadataReaderOptions options)
    {
        byte[] data = ReadPayload(document, reader, box, GetMaxProfileBytes(options), "jxl.exif.profile.limit");
        if (data.Length == 0 && box.DataSize > 0)
        {
            return;
        }

        ExtractExifData(document, box.Directory, data, box.DataOffset, box.DataSize, options, false);
    }

    internal static void ExtractExifData(ImageMetadataDocument document, MetadataDirectory directory, byte[] data, long offset, ulong declaredLength, ImageMetadataReaderOptions options, bool compressed)
    {
        AddTag(directory, "JpegXl.Exif.ByteLength", "EXIF Byte Length", MetadataValue.FromUnsignedInteger(declaredLength), offset, JpegXlBoxTraverser.ToLong(declaredLength), "");
        AddTag(directory, "JpegXl.Exif.Compressed", "Compressed", MetadataValue.FromBoolean(compressed), offset, data.Length, "");
        if (data.Length < 4)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.exif.offset.truncated", "JPEG XL EXIF TIFF offset was truncated.", "Exif", offset, 4);
            return;
        }

        uint tiffOffset = JpegXlBoxTraverser.ReadUnsigned32(CopyRange(data, 0, 4));
        ulong startValue = 4 + (ulong)tiffOffset;
        AddTag(directory, "JpegXl.Exif.TiffOffset", "TIFF Offset", MetadataValue.FromUnsignedInteger(tiffOffset), offset, 4, "");
        if (startValue > (ulong)data.Length)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.exif.offset.invalid", "JPEG XL EXIF TIFF offset exceeded the box payload.", "Exif", offset, data.Length);
            return;
        }

        int start = (int)startValue;
        byte[] tiff = CopyRange(data, start, data.Length - start);
        string byteOrder = JpegExifExtractor.TiffByteOrderName(tiff);
        AddTag(directory, "JpegXl.Exif.ByteOrder", "TIFF Byte Order", MetadataValue.FromString(byteOrder), offset + start, PrefixLength(tiff, 2), byteOrder);
        if (!JpegExifExtractor.HasTiffHeader(tiff))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.exif.tiff.header.invalid", "JPEG XL EXIF payload did not contain a recognized TIFF header.", "Exif", offset + start, tiff.Length);
        }

        MetadataProfile profile = CreateProfile(MetadataProfileKind.Exif, MetadataFamily.Exif, "Exif.JPEGXL.Exif", "EXIF", "image/tiff", offset + start, tiff.Length);
        if (tiff.Length <= GetMaxProfileBytes(options))
        {
            profile.Data = tiff;
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jxl.exif.profile.limit", "JPEG XL EXIF profile exceeded the configured profile limit.", "Exif", offset + start, tiff.Length);
        }

        document.AddProfile(profile);
    }

    internal static void ExtractXmpBox(ImageMetadataDocument document, BoundedStreamReader reader, JpegXlBox box, ImageMetadataReaderOptions options)
    {
        AddTag(box.Directory, "JpegXl.Xmp.ByteLength", "XMP Byte Length", MetadataValue.FromUnsignedInteger(box.DataSize), box.DataOffset, JpegXlBoxTraverser.ToLong(box.DataSize), "");
        if (!ShouldParseXmp(options))
        {
            return;
        }

        byte[] data = ReadPayload(document, reader, box, GetMaxXmlBytes(options), "jxl.xmp.profile.limit");
        if (data.Length == 0 && box.DataSize > 0)
        {
            return;
        }

        ExtractXmpData(document, box.Directory, data, box.DataOffset, box.DataSize, options, false);
    }

    internal static void ExtractXmpData(ImageMetadataDocument document, MetadataDirectory directory, byte[] data, long offset, ulong declaredLength, ImageMetadataReaderOptions options, bool compressed)
    {
        AddTag(directory, "JpegXl.Xmp.Compressed", "Compressed", MetadataValue.FromBoolean(compressed), offset, data.Length, "");
        if (!ShouldParseXmp(options))
        {
            return;
        }

        if (data.Length > GetMaxXmlBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jxl.xmp.profile.limit", "JPEG XL XMP packet exceeded the configured XML limit.", "xml ", offset, data.Length);
            return;
        }

        if (!MetadataStringReader.IsValidUtf8(data))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.xmp.utf8.invalid", "JPEG XL XMP payload was not valid UTF-8.", "xml ", offset, data.Length);
        }

        string text = MetadataStringReader.DecodeUtf8WithLatin1Fallback(data, document.Diagnostics, "xml ");
        AddTag(directory, "JpegXl.Xmp.Text", "XMP Text", MetadataValue.FromXmlText(text), offset, data.Length, text);
        MetadataProfile profile = CreateProfile(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "Xmp.JPEGXL.xml", "XMP", "application/rdf+xml", offset, JpegXlBoxTraverser.ToLong(declaredLength));
        if (data.Length <= GetMaxProfileBytes(options))
        {
            profile.Data = CopyRange(data, 0, data.Length);
            profile.Text = text;
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jxl.xmp.profile.limit", "JPEG XL XMP profile exceeded the configured profile limit.", "xml ", offset, data.Length);
        }

        document.AddProfile(profile);
        XmpPacketParser.ParseProfile(document, profile, options, "JPEGXL/xml/XMP");
    }

    internal static void ExtractJumbfBox(ImageMetadataDocument document, BoundedStreamReader reader, JpegXlBox box, ImageMetadataReaderOptions options)
    {
        long length = JpegXlBoxTraverser.ToLong(box.Size);
        byte[] data = ReadRange(document, reader, box.Offset, length, GetMaxProfileBytes(options), "jxl.jumbf.profile.limit", "jumb");
        bool c2pa = ContainsAscii(data, "c2pa");
        AddTag(box.Directory, "JpegXl.Jumbf.ByteLength", "JUMBF Byte Length", MetadataValue.FromInteger(length), box.Offset, length, "");
        AddTag(box.Directory, "JpegXl.Jumbf.IsC2pa", "Contains C2PA", MetadataValue.FromBoolean(c2pa), box.Offset, length, "");
        MetadataProfile profile = CreateProfile(c2pa ? MetadataProfileKind.C2pa : MetadataProfileKind.Jumbf, MetadataFamily.Container, c2pa ? "C2pa.JPEGXL.jumb" : "Jumbf.JPEGXL.jumb", c2pa ? "C2PA" : "JUMBF", "application/jumbf", box.Offset, length);
        profile.Data = data;
        document.AddProfile(profile);
        CaptureRaw(document, reader, box.Offset, length, c2pa ? "C2PA JUMBF" : "JUMBF", options);
    }

    internal static void ExtractCompressedBox(ImageMetadataDocument document, BoundedStreamReader reader, JpegXlBox box, ImageMetadataReaderOptions options)
    {
        if (box.DataSize < 5)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.brob.truncated", "JPEG XL Brotli-compressed box was truncated.", "brob", box.DataOffset, 5);
            return;
        }

        byte[] data = ReadPayload(document, reader, box, GetMaxSegmentBytes(options), "jxl.brob.payload.limit");
        if (data.Length < 5)
        {
            return;
        }

        string type = MetadataStringReader.DecodeLatin1(CopyRange(data, 0, 4));
        AddTag(box.Directory, "JpegXl.brob.DecodedType", "Decoded Box Type", MetadataValue.FromString(type), box.DataOffset, 4, type);
        byte[] compressed = CopyRange(data, 4, data.Length - 4);
        byte[] decoded = BrotliHelper.Decompress(compressed, GetMaxDecompressedBytes(options), document.Diagnostics, "brob");
        if (type == "Exif")
        {
            ExtractExifData(document, box.Directory, decoded, box.DataOffset + 4, (ulong)decoded.Length, options, true);
        }
        else if (type == "xml ")
        {
            ExtractXmpData(document, box.Directory, decoded, box.DataOffset + 4, (ulong)decoded.Length, options, true);
        }
        else if (type == "jumb")
        {
            ExtractCompressedJumbf(document, box, decoded, options);
        }
        else
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "jxl.brob.type.unsupported", "JPEG XL Brotli box contained an unsupported metadata type.", type);
        }
    }

    internal static void ExtractCompressedJumbf(ImageMetadataDocument document, JpegXlBox box, byte[] data, ImageMetadataReaderOptions options)
    {
        bool c2pa = ContainsAscii(data, "c2pa");
        AddTag(box.Directory, "JpegXl.Jumbf.ByteLength", "JUMBF Byte Length", MetadataValue.FromInteger(data.Length), box.DataOffset + 4, data.Length, "");
        AddTag(box.Directory, "JpegXl.Jumbf.IsC2pa", "Contains C2PA", MetadataValue.FromBoolean(c2pa), box.DataOffset + 4, data.Length, "");
        MetadataProfile profile = CreateProfile(c2pa ? MetadataProfileKind.C2pa : MetadataProfileKind.Jumbf, MetadataFamily.Container, c2pa ? "C2pa.JPEGXL.brob" : "Jumbf.JPEGXL.brob", c2pa ? "C2PA" : "JUMBF", "application/jumbf", box.DataOffset + 4, data.Length);
        if (data.Length <= GetMaxProfileBytes(options))
        {
            profile.Data = CopyRange(data, 0, data.Length);
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jxl.jumbf.profile.limit", "JPEG XL JUMBF profile exceeded the configured profile limit.", "brob", box.DataOffset + 4, data.Length);
        }

        document.AddProfile(profile);
    }

    internal static void CaptureRaw(ImageMetadataDocument document, BoundedStreamReader reader, long offset, long length, string name, ImageMetadataReaderOptions options)
    {
        if (!CaptureRawBlocks(options))
        {
            return;
        }

        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.JpegXl, "jumb", name, offset, length);
        if (CaptureRawBlockData(options))
        {
            block.Data = ReadRange(document, reader, offset, length, GetMaxRawBlockBytes(options), "jxl.jumbf.raw.limit", "jumb");
        }
    }

    internal static byte[] ReadPayload(ImageMetadataDocument document, BoundedStreamReader reader, JpegXlBox box, long limit, string code)
    {
        return ReadRange(document, reader, box.DataOffset, JpegXlBoxTraverser.ToLong(box.DataSize), limit, code, box.Type);
    }

    internal static byte[] ReadRange(ImageMetadataDocument document, BoundedStreamReader reader, long offset, long length, long limit, string code, string blockType)
    {
        if (length < 0 || length > limit || length > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, code, "JPEG XL metadata payload exceeded the configured limit.", blockType, offset, length);
            return new byte[0];
        }

        if (!reader.SeekAbsolute(offset) || !reader.CanRead(length))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.metadata.read.truncated", "JPEG XL metadata payload could not be read.", blockType, offset, length);
            return new byte[0];
        }

        return reader.ReadBytes((int)length);
    }

    internal static MetadataProfile CreateProfile(MetadataProfileKind kind, MetadataFamily family, string key, string name, string mimeType, long offset, long length)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = kind;
        profile.Family = family;
        profile.Key = key;
        profile.Name = name;
        profile.MimeType = mimeType;
        profile.Offset = offset;
        profile.Length = length;
        return profile;
    }

    internal static void AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string display)
    {
        if (directory != null)
        {
            JpegXlBoxTraverser.AddTag(directory, key, name, value, offset, length, display);
        }
    }

    internal static bool ContainsAscii(byte[] data, string text)
    {
        if (data == null || text == null || text.Length == 0)
        {
            return false;
        }

        int offset = 0;
        while (offset <= data.Length - text.Length)
        {
            int index = 0;
            while (index < text.Length && ToLowerAscii(data[offset + index]) == ToLowerAscii((byte)text[index]))
            {
                index = index + 1;
            }

            if (index == text.Length)
            {
                return true;
            }

            offset = offset + 1;
        }

        return false;
    }

    internal static byte ToLowerAscii(byte value)
    {
        return value >= 65 && value <= 90 ? (byte)(value + 32) : value;
    }

    internal static byte[] CopyRange(byte[] data, int offset, int length)
    {
        if (data == null || offset < 0 || length < 0 || offset > data.Length || length > data.Length - offset)
        {
            return new byte[0];
        }

        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = data[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal static int PrefixLength(byte[] data, int requested)
    {
        return data.Length < requested ? data.Length : requested;
    }

    internal static int ColorChannelCount(uint colorSpace)
    {
        return colorSpace == 1 ? 1 : 3;
    }

    internal static ImageColorSpaceKind NormalizedColorSpace(JpegXlCodestreamHeader header)
    {
        if (header.ColorSpace == 1)
        {
            return ImageColorSpaceKind.Gray;
        }

        if (header.ColorSpace == 0 || header.ColorSpace == 2)
        {
            if (header.Primaries == 11)
            {
                return ImageColorSpaceKind.DisplayP3;
            }

            if (header.Primaries == 9)
            {
                return ImageColorSpaceKind.WideGamutRgb;
            }

            return ImageColorSpaceKind.Rgb;
        }

        return ImageColorSpaceKind.Unknown;
    }

    internal static bool ShouldParseXmp(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseXmp;
    }

    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions options)
    {
        return options == null || options.CaptureRawBlocks;
    }

    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions options)
    {
        return options != null && options.CaptureRawBlockData;
    }

    internal static long GetMaxProfileBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxProfileBytes : options.MaxProfileBytes;
    }

    internal static long GetMaxXmlBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxXmlBytes : options.MaxXmlBytes;
    }

    internal static long GetMaxRawBlockBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxRawBlockBytes : options.MaxRawBlockBytes;
    }

    internal static long GetMaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
    }

    internal static long GetMaxDecompressedBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxDecompressedBytes : options.MaxDecompressedBytes;
    }

    internal static int GetMaxFrameCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxFrameCount : options.MaxFrameCount;
    }
}
