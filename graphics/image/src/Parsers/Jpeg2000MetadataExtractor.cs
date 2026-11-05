using System;
using System.Collections.Generic;

namespace image;

internal class Jpeg2000MetadataExtractor
{
    internal const string ExifUuidText = "JpgTiffExif->JP2";
    internal const string Exif2UuidHex = "0537cdab9d0c4431a72afa561f2a113e";
    internal const string IptcUuidHex = "33c7a4d2b81d4723a0baf1a3e097ad38";
    internal const string Iptc2UuidHex = "09a14e97c0b442e0bebf36df6f0ce36f";
    internal const string XmpUuidHex = "be7acfcb97a942e89c71999491e3afac";
    internal const string GeoJp2UuidHex = "b14bf8bd083d4b43a5ae8cd7d5a6ce03";
    internal const string PhotoshopUuidHex = "2c4c0100850440b9a03e562148d6dfeb";

    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, List<Jpeg2000Box> boxes, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || boxes == null)
        {
            return;
        }

        Jpeg2000MetadataState state = new Jpeg2000MetadataState();
        ExtractBoxes(document, reader, boxes, state, options);
    }

    internal static void ExtractBoxes(ImageMetadataDocument document, BoundedStreamReader reader, List<Jpeg2000Box> boxes, Jpeg2000MetadataState state, ImageMetadataReaderOptions options)
    {
        foreach (Jpeg2000Box box in boxes)
        {
            if (box == null)
            {
                continue;
            }

            if (box.Type == "ihdr")
            {
                DecodeImageHeader(document, reader, box, state, options);
            }
            else if (box.Type == "colr")
            {
                DecodeColorSpecification(document, reader, box, state, options);
            }
            else if (box.Type == "resc" || box.Type == "resd")
            {
                DecodeResolution(document, reader, box, options);
            }
            else if (box.Type == "xml ")
            {
                DecodeXml(document, reader, box, options);
            }
            else if (box.Type == "jp2i")
            {
                DecodeIntellectualProperty(document, reader, box, options);
            }
            else if (box.Type == "uuid")
            {
                DecodeUuid(document, reader, box, options);
            }
            else if (box.Type == "jumb")
            {
                DecodeJumbf(document, reader, box, options);
            }

            ExtractBoxes(document, reader, box.Children, state, options);
        }
    }

    internal static void DecodeImageHeader(ImageMetadataDocument document, BoundedStreamReader reader, Jpeg2000Box box, Jpeg2000MetadataState state, ImageMetadataReaderOptions options)
    {
        byte[] data = ReadBoxData(document, reader, box, 14, "jp2.ihdr.truncated");
        if (data.Length < 14)
        {
            return;
        }

        uint height = ReadUnsigned32(data, 0);
        uint width = ReadUnsigned32(data, 4);
        int componentCount = ReadUnsigned16(data, 8);
        int encodedDepth = data[10];
        int compression = data[11];
        bool colorSpaceUnknown = data[12] != 0;
        bool intellectualProperty = data[13] != 0;
        int bitDepth = encodedDepth == 255 ? 0 : (encodedDepth & 127) + 1;
        bool signedSamples = encodedDepth != 255 && (encodedDepth & 128) != 0;
        MetadataDirectory directory = box.Directory;
        AddTag(directory, "Jpeg2000.ihdr.Height", "Height", MetadataValue.FromUnsignedInteger(height), box.DataOffset, 4, "");
        AddTag(directory, "Jpeg2000.ihdr.Width", "Width", MetadataValue.FromUnsignedInteger(width), box.DataOffset + 4, 4, "");
        AddTag(directory, "Jpeg2000.ihdr.ComponentCount", "Component Count", MetadataValue.FromInteger(componentCount), box.DataOffset + 8, 2, "");
        AddTag(directory, "Jpeg2000.ihdr.BitsPerComponent", "Bits Per Component", MetadataValue.FromInteger(encodedDepth), box.DataOffset + 10, 1, encodedDepth == 255 ? "Variable" : bitDepth.ToString());
        AddTag(directory, "Jpeg2000.ihdr.BitDepth", "Bit Depth", MetadataValue.FromInteger(bitDepth), box.DataOffset + 10, 1, "");
        AddTag(directory, "Jpeg2000.ihdr.SignedSamples", "Signed Samples", MetadataValue.FromBoolean(signedSamples), box.DataOffset + 10, 1, "");
        AddTag(directory, "Jpeg2000.ihdr.CompressionType", "Compression Type", MetadataValue.FromInteger(compression), box.DataOffset + 11, 1, compression == 7 ? "JPEG 2000" : "Unknown");
        AddTag(directory, "Jpeg2000.ihdr.ColorSpaceUnknown", "Color Space Unknown", MetadataValue.FromBoolean(colorSpaceUnknown), box.DataOffset + 12, 1, "");
        AddTag(directory, "Jpeg2000.ihdr.IntellectualProperty", "Intellectual Property", MetadataValue.FromBoolean(intellectualProperty), box.DataOffset + 13, 1, "");

        if (width == 0 || height == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jp2.ihdr.dimensions.invalid", "JPEG 2000 image header contained a zero dimension.", "ihdr", box.DataOffset, 8);
            return;
        }

        if (width > 2147483647 || height > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jp2.ihdr.dimensions.limit", "JPEG 2000 dimensions exceeded the supported frame range.", "ihdr", box.DataOffset, 8);
            return;
        }

        if (document.Frames.Count >= GetMaxFrameCount(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jp2.frame.count.limit", "JPEG 2000 frame count exceeded the configured limit.", "ihdr", box.Offset, Jpeg2000BoxTraverser.ToLong(box.Size));
            return;
        }

        ImageMetadataFrame frame = document.AddFrameWithDimensions((int)width, (int)height);
        frame.BitDepth = bitDepth;
        frame.ChannelCount = componentCount;
        frame.Compression = compression == 7 ? ImageCompressionKind.Jpeg2000 : ImageCompressionKind.Unknown;
        frame.ColorSpace = state.ColorSpace;
        if (compression != 7)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "jp2.ihdr.compression.unsupported", "JPEG 2000 image header used an unknown compression value.", "ihdr");
        }
    }

    internal static void DecodeColorSpecification(ImageMetadataDocument document, BoundedStreamReader reader, Jpeg2000Box box, Jpeg2000MetadataState state, ImageMetadataReaderOptions options)
    {
        byte[] data = ReadBoxData(document, reader, box, 3, "jp2.colr.truncated");
        if (data.Length < 3)
        {
            return;
        }

        int method = data[0];
        int precedence = data[1];
        int approximation = data[2];
        MetadataDirectory directory = box.Directory;
        AddTag(directory, "Jpeg2000.colr.Method", "Method", MetadataValue.FromInteger(method), box.DataOffset, 1, ColorMethodName(method));
        AddTag(directory, "Jpeg2000.colr.Precedence", "Precedence", MetadataValue.FromInteger(precedence), box.DataOffset + 1, 1, "");
        AddTag(directory, "Jpeg2000.colr.Approximation", "Approximation", MetadataValue.FromInteger(approximation), box.DataOffset + 2, 1, ApproximationName(approximation));

        if (method == 1)
        {
            if (box.DataSize < 7)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "jp2.colr.enumerated.truncated", "JPEG 2000 enumerated color space was truncated.", "colr", box.DataOffset, 7);
                return;
            }

            byte[] colorData = ReadRange(document, reader, box.DataOffset, 7, 7, "", "colr");
            if (colorData.Length < 7)
            {
                return;
            }

            uint color = ReadUnsigned32(colorData, 3);
            string name = EnumeratedColorName(color);
            AddTag(directory, "Jpeg2000.colr.EnumeratedColorSpace", "Enumerated Color Space", MetadataValue.FromUnsignedInteger(color), box.DataOffset + 3, 4, name);
            ImageColorSpaceKind colorSpace = EnumeratedColorKind(color);
            if (colorSpace != ImageColorSpaceKind.Unknown)
            {
                state.ColorSpace = colorSpace;
                ApplyColorSpace(document, colorSpace);
            }

            return;
        }

        if (method == 2 || method == 3)
        {
            long profileLength = Jpeg2000BoxTraverser.ToLong(box.DataSize) - 3;
            AddTag(directory, "Jpeg2000.colr.IccProfileByteLength", "ICC Profile Byte Length", MetadataValue.FromInteger(profileLength), box.DataOffset + 3, profileLength, "");
            CaptureRawBox(document, reader, box, "colr", "JPEG 2000 ICC Color Specification", options);
            if (ShouldParseIcc(options))
            {
                MetadataProfile profile = AddProfile(document, reader, MetadataProfileKind.Icc, MetadataFamily.Icc, "Icc.JPEG2000.colr", "ICC Profile", "application/vnd.iccprofile", box.DataOffset + 3, profileLength, GetMaxProfileBytes(options), "jp2.icc.profile.limit", "colr");
                IccProfileParser.ParseProfile(document, profile, options, "JPEG2000/colr/ICC", "colr");
            }

            return;
        }

        if (method == 4)
        {
            long vendorLength = Jpeg2000BoxTraverser.ToLong(box.DataSize) - 3;
            AddTag(directory, "Jpeg2000.colr.VendorDataByteLength", "Vendor Data Byte Length", MetadataValue.FromInteger(vendorLength), box.DataOffset + 3, vendorLength, "");
            return;
        }

        MetadataDiagnostic.AddUnsupported(document.Diagnostics, "jp2.colr.method.unsupported", "JPEG 2000 color specification method was unsupported.", "colr");
    }

    internal static void DecodeResolution(ImageMetadataDocument document, BoundedStreamReader reader, Jpeg2000Box box, ImageMetadataReaderOptions options)
    {
        byte[] data = ReadBoxData(document, reader, box, 10, "jp2." + box.Type.Trim() + ".truncated");
        if (data.Length < 10)
        {
            return;
        }

        int yNumerator = ReadUnsigned16(data, 0);
        int yDenominator = ReadUnsigned16(data, 2);
        int xNumerator = ReadUnsigned16(data, 4);
        int xDenominator = ReadUnsigned16(data, 6);
        int yExponent = SignedByte(data[8]);
        int xExponent = SignedByte(data[9]);
        string prefix = "Jpeg2000." + box.Type.Trim();
        MetadataDirectory directory = box.Directory;
        AddTag(directory, prefix + ".YResolutionBase", "Y Resolution Base", MetadataValue.FromRational(yNumerator, yDenominator), box.DataOffset, 4, "");
        AddTag(directory, prefix + ".XResolutionBase", "X Resolution Base", MetadataValue.FromRational(xNumerator, xDenominator), box.DataOffset + 4, 4, "");
        AddTag(directory, prefix + ".YExponent", "Y Exponent", MetadataValue.FromInteger(yExponent), box.DataOffset + 8, 1, "");
        AddTag(directory, prefix + ".XExponent", "X Exponent", MetadataValue.FromInteger(xExponent), box.DataOffset + 9, 1, "");
        AddTag(directory, prefix + ".ResolutionUnit", "Resolution Unit", MetadataValue.FromString("pixels/m"), box.DataOffset + 8, 2, "pixels/m");
        if (yDenominator == 0 || xDenominator == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jp2.resolution.denominator.invalid", "JPEG 2000 resolution denominator was zero.", box.Type, box.DataOffset, 8);
            return;
        }

        double yResolution = yNumerator / (double)yDenominator * Math.Pow(10, yExponent);
        double xResolution = xNumerator / (double)xDenominator * Math.Pow(10, xExponent);
        AddTag(directory, prefix + ".YResolution", "Y Resolution", MetadataValue.FromDouble(yResolution), box.DataOffset, 9, "");
        AddTag(directory, prefix + ".XResolution", "X Resolution", MetadataValue.FromDouble(xResolution), box.DataOffset + 4, 6, "");
    }

    internal static void DecodeXml(ImageMetadataDocument document, BoundedStreamReader reader, Jpeg2000Box box, ImageMetadataReaderOptions options)
    {
        long length = Jpeg2000BoxTraverser.ToLong(box.DataSize);
        AddTag(box.Directory, "Jpeg2000.XML.ByteLength", "XML Byte Length", MetadataValue.FromInteger(length), box.DataOffset, length, "");
        CaptureRawBox(document, reader, box, box.Type.Trim(), "JPEG 2000 XML", options);
        byte[] data = ReadRange(document, reader, box.DataOffset, length, GetMaxXmlBytes(options), "jp2.xml.limit", box.Type);
        if (data.Length == 0 && length > 0)
        {
            return;
        }

        string text = MetadataXmlReader.DecodeXmlBytes(data, GetMaxXmlBytes(options), document.Diagnostics, box.Type);
        bool xmp = IsXmp(text);
        AddTag(box.Directory, "Jpeg2000.XML.Text", "XML Text", MetadataValue.FromXmlText(text), box.DataOffset, length, text);
        AddTag(box.Directory, "Jpeg2000.XML.IsXmp", "Contains XMP", MetadataValue.FromBoolean(xmp), box.DataOffset, length, "");
        if (xmp && ShouldParseXmp(options))
        {
            AddTextProfile(document, MetadataProfileKind.Xmp, MetadataFamily.Xmp, "Xmp.JPEG2000.XML", "JPEG 2000 XML/XMP", "application/rdf+xml", box.DataOffset, length, data, text, options, "jp2.xmp.profile.limit", box.Type);
        }
    }

    internal static void DecodeIntellectualProperty(ImageMetadataDocument document, BoundedStreamReader reader, Jpeg2000Box box, ImageMetadataReaderOptions options)
    {
        long length = Jpeg2000BoxTraverser.ToLong(box.DataSize);
        AddTag(box.Directory, "Jpeg2000.jp2i.PayloadByteLength", "Payload Byte Length", MetadataValue.FromInteger(length), box.DataOffset, length, "");
        CaptureRawBox(document, reader, box, "jp2i", "JPEG 2000 Intellectual Property", options);
    }

    internal static void DecodeUuid(ImageMetadataDocument document, BoundedStreamReader reader, Jpeg2000Box box, ImageMetadataReaderOptions options)
    {
        string kind = UuidKind(box.Uuid);
        long payloadOffset = box.DataOffset + Jpeg2000BoxTraverser.UuidSize;
        long payloadLength = Jpeg2000BoxTraverser.ToLong(box.DataSize) - Jpeg2000BoxTraverser.UuidSize;
        if (payloadLength < 0)
        {
            payloadLength = 0;
        }

        AddTag(box.Directory, "Jpeg2000.uuid.MetadataKind", "Metadata Kind", MetadataValue.FromString(kind), box.DataOffset, Jpeg2000BoxTraverser.UuidSize, kind);
        AddTag(box.Directory, "Jpeg2000.uuid.PayloadByteLength", "Payload Byte Length", MetadataValue.FromInteger(payloadLength), payloadOffset, payloadLength, "");
        CaptureRawBox(document, reader, box, "uuid", "JPEG 2000 UUID " + kind, options);
        if (kind == "XMP")
        {
            if (!ShouldParseXmp(options))
            {
                return;
            }

            byte[] data = ReadRange(document, reader, payloadOffset, payloadLength, GetMaxXmlBytes(options), "jp2.uuid.xmp.limit", "uuid");
            if (data.Length > 0 || payloadLength == 0)
            {
                if (!MetadataStringReader.IsValidUtf8(data))
                {
                    MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jp2.uuid.xmp.utf8.invalid", "JPEG 2000 UUID XMP payload was not valid UTF-8.", "uuid", payloadOffset, payloadLength);
                }

                string text = MetadataStringReader.DecodeUtf8WithLatin1Fallback(data, document.Diagnostics, "uuid");
                AddTextProfile(document, MetadataProfileKind.Xmp, MetadataFamily.Xmp, "Xmp.JPEG2000.uuid", "JPEG 2000 UUID XMP", "application/rdf+xml", payloadOffset, payloadLength, data, text, options, "jp2.uuid.xmp.profile.limit", "uuid");
            }

            return;
        }

        if (kind == "EXIF" || kind == "EXIF2")
        {
            int skip = 0;
            long prefixLength = payloadLength < 6 ? payloadLength : 6;
            byte[] prefix = ReadRange(document, reader, payloadOffset, prefixLength, 6, "", "uuid");
            if (StartsWith(prefix, new byte[] { 69, 120, 105, 102, 0, 0 }))
            {
                skip = 6;
            }

            AddProfile(document, reader, MetadataProfileKind.Exif, MetadataFamily.Exif, "Exif.JPEG2000.uuid", "EXIF", "image/tiff", payloadOffset + skip, payloadLength - skip, GetMaxProfileBytes(options), "jp2.uuid.exif.profile.limit", "uuid");
            return;
        }

        if (kind == "IPTC" || kind == "IPTC2")
        {
            AddProfile(document, reader, MetadataProfileKind.Iptc, MetadataFamily.Iptc, "Iptc.JPEG2000.uuid", "IPTC", "application/octet-stream", payloadOffset, payloadLength, GetMaxProfileBytes(options), "jp2.uuid.iptc.profile.limit", "uuid");
            byte[] data = ReadRange(document, reader, payloadOffset, payloadLength, GetMaxSegmentBytes(options), "jp2.uuid.iptc.parse.limit", "uuid");
            if (data.Length > 0 || payloadLength == 0)
            {
                IptcIimParser.Parse(document, data, payloadOffset, "JPEG2000/uuid/IPTC", "jp2.iptc", "uuid", options);
            }
            return;
        }

        if (kind == "GeoJP2")
        {
            AddProfile(document, reader, MetadataProfileKind.Exif, MetadataFamily.Exif, "Exif.JPEG2000.GeoJP2", "GeoJP2 TIFF", "image/tiff", payloadOffset, payloadLength, GetMaxProfileBytes(options), "jp2.uuid.geojp2.profile.limit", "uuid");
            return;
        }

        if (kind == "Photoshop")
        {
            AddProfile(document, reader, MetadataProfileKind.PhotoshopImageResource, MetadataFamily.Photoshop, "Photoshop.JPEG2000.uuid", "Photoshop Image Resources", "application/octet-stream", payloadOffset, payloadLength, GetMaxProfileBytes(options), "jp2.uuid.photoshop.profile.limit", "uuid");
        }
    }

    internal static void DecodeJumbf(ImageMetadataDocument document, BoundedStreamReader reader, Jpeg2000Box box, ImageMetadataReaderOptions options)
    {
        long length = Jpeg2000BoxTraverser.ToLong(box.Size);
        AddProfile(document, reader, MetadataProfileKind.Jumbf, MetadataFamily.Container, "Jumbf.JPEG2000.jumb", "JUMBF", "application/jumbf", box.Offset, length, GetMaxProfileBytes(options), "jp2.jumbf.profile.limit", "jumb");
        CaptureRawBox(document, reader, box, "jumb", "JPEG 2000 JUMBF", options);
    }

    internal static byte[] ReadBoxData(ImageMetadataDocument document, BoundedStreamReader reader, Jpeg2000Box box, int required, string code)
    {
        long length = Jpeg2000BoxTraverser.ToLong(box.DataSize);
        if (length < required)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, code, "JPEG 2000 box fields were truncated.", box.Type, box.DataOffset, required);
            return new byte[0];
        }

        return ReadRange(document, reader, box.DataOffset, required, required, "", box.Type);
    }

    internal static byte[] ReadRange(ImageMetadataDocument document, BoundedStreamReader reader, long offset, long length, long limit, string limitCode, string blockType)
    {
        if (length < 0 || length > limit || length > 2147483647)
        {
            if (limitCode.Length > 0)
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, limitCode, "JPEG 2000 metadata payload exceeded the configured limit.", blockType, offset, length);
            }

            return new byte[0];
        }

        if (!reader.SeekAbsolute(offset) || !reader.CanRead(length))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jp2.metadata.read.truncated", "JPEG 2000 metadata payload could not be read.", blockType, offset, length);
            return new byte[0];
        }

        return reader.ReadBytes((int)length);
    }

    internal static void CaptureRawBox(ImageMetadataDocument document, BoundedStreamReader reader, Jpeg2000Box box, string blockType, string name, ImageMetadataReaderOptions options)
    {
        if (!CaptureRawBlocks(options))
        {
            return;
        }

        long length = Jpeg2000BoxTraverser.ToLong(box.Size);
        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Jpeg2000, blockType, name, box.Offset, length);
        if (!CaptureRawBlockData(options))
        {
            return;
        }

        if (length > GetMaxRawBlockBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jp2.raw.block.limit", "JPEG 2000 raw metadata box exceeded the configured raw block limit.", blockType, box.Offset, length);
            return;
        }

        block.Data = ReadRange(document, reader, box.Offset, length, GetMaxRawBlockBytes(options), "jp2.raw.block.limit", blockType);
    }

    internal static MetadataProfile AddProfile(ImageMetadataDocument document, BoundedStreamReader reader, MetadataProfileKind kind, MetadataFamily family, string key, string name, string mimeType, long offset, long length, long limit, string limitCode, string blockType)
    {
        MetadataProfile profile = CreateProfile(kind, family, key, name, mimeType, offset, length);
        profile.Data = ReadRange(document, reader, offset, length, limit, limitCode, blockType);
        document.AddProfile(profile);
        return profile;
    }

    internal static void AddTextProfile(ImageMetadataDocument document, MetadataProfileKind kind, MetadataFamily family, string key, string name, string mimeType, long offset, long length, byte[] data, string text, ImageMetadataReaderOptions options, string limitCode, string blockType)
    {
        MetadataProfile profile = CreateProfile(kind, family, key, name, mimeType, offset, length);
        if (length <= GetMaxProfileBytes(options))
        {
            profile.Data = Copy(data);
            profile.Text = text;
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, limitCode, "JPEG 2000 metadata profile exceeded the configured profile limit.", blockType, offset, length);
        }

        document.AddProfile(profile);
        if (kind == MetadataProfileKind.Xmp)
        {
            XmpPacketParser.ParseProfile(document, profile, options, "JPEG2000/XMP");
        }
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

    internal static void AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string displayValue)
    {
        if (directory == null)
        {
            return;
        }

        Jpeg2000BoxTraverser.AddTag(directory, key, name, value, offset, length, displayValue);
    }

    internal static string UuidKind(byte[] uuid)
    {
        if (uuid == null || uuid.Length != 16)
        {
            return "Unknown";
        }

        string text = MetadataStringReader.DecodeAscii(uuid);
        string hex = HashHelper.ToHex(uuid);
        if (text == ExifUuidText)
        {
            return "EXIF";
        }

        if (hex == Exif2UuidHex)
        {
            return "EXIF2";
        }

        if (hex == IptcUuidHex)
        {
            return "IPTC";
        }

        if (hex == Iptc2UuidHex)
        {
            return "IPTC2";
        }

        if (hex == XmpUuidHex)
        {
            return "XMP";
        }

        if (hex == GeoJp2UuidHex)
        {
            return "GeoJP2";
        }

        if (hex == PhotoshopUuidHex)
        {
            return "Photoshop";
        }

        return "Unknown";
    }

    internal static string ColorMethodName(int method)
    {
        if (method == 1)
        {
            return "Enumerated";
        }

        if (method == 2)
        {
            return "Restricted ICC";
        }

        if (method == 3)
        {
            return "Any ICC";
        }

        if (method == 4)
        {
            return "Vendor Color";
        }

        return "Unknown";
    }

    internal static string ApproximationName(int approximation)
    {
        if (approximation == 0)
        {
            return "Not Specified";
        }

        if (approximation == 1)
        {
            return "Accurate";
        }

        if (approximation == 2)
        {
            return "Exceptional Quality";
        }

        if (approximation == 3)
        {
            return "Reasonable Quality";
        }

        if (approximation == 4)
        {
            return "Poor Quality";
        }

        return "Unknown";
    }

    internal static string EnumeratedColorName(uint value)
    {
        if (value == 0 || value == 15)
        {
            return "Bi-level";
        }

        if (value == 1 || value == 3 || value == 4 || value == 18 || value == 22 || value == 23 || value == 24)
        {
            return "YCbCr";
        }

        if (value == 9)
        {
            return "PhotoYCC";
        }

        if (value == 11)
        {
            return "CMY";
        }

        if (value == 12)
        {
            return "CMYK";
        }

        if (value == 13)
        {
            return "YCCK";
        }

        if (value == 14)
        {
            return "CIELab";
        }

        if (value == 16)
        {
            return "sRGB";
        }

        if (value == 17)
        {
            return "Grayscale";
        }

        if (value == 19)
        {
            return "CIEJab";
        }

        if (value == 20)
        {
            return "e-sRGB";
        }

        if (value == 21)
        {
            return "ROMM-RGB";
        }

        return "Unknown";
    }

    internal static ImageColorSpaceKind EnumeratedColorKind(uint value)
    {
        if (value == 0 || value == 15 || value == 17)
        {
            return ImageColorSpaceKind.Gray;
        }

        if (value == 1 || value == 3 || value == 4 || value == 9 || value == 13 || value == 18 || value == 22 || value == 23 || value == 24)
        {
            return ImageColorSpaceKind.YCbCr;
        }

        if (value == 11 || value == 12)
        {
            return ImageColorSpaceKind.Cmyk;
        }

        if (value == 14 || value == 19)
        {
            return ImageColorSpaceKind.Lab;
        }

        if (value == 16 || value == 20)
        {
            return ImageColorSpaceKind.Rgb;
        }

        if (value == 21)
        {
            return ImageColorSpaceKind.ProPhotoRgb;
        }

        return ImageColorSpaceKind.Unknown;
    }

    internal static void ApplyColorSpace(ImageMetadataDocument document, ImageColorSpaceKind colorSpace)
    {
        foreach (ImageMetadataFrame frame in document.Frames)
        {
            if (frame != null && frame.ColorSpace == ImageColorSpaceKind.Unknown)
            {
                frame.ColorSpace = colorSpace;
            }
        }
    }

    internal static bool IsXmp(string text)
    {
        if (text == null)
        {
            return false;
        }

        return text.IndexOf("<x:xmpmeta", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("<rdf:RDF", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("adobe:ns:meta/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static bool StartsWith(byte[] data, byte[] prefix)
    {
        if (data == null || prefix == null || data.Length < prefix.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (data[index] != prefix[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static byte[] Copy(byte[] data)
    {
        if (data == null)
        {
            return new byte[0];
        }

        byte[] copy = new byte[data.Length];
        int index = 0;
        while (index < data.Length)
        {
            copy[index] = data[index];
            index = index + 1;
        }

        return copy;
    }

    internal static int SignedByte(byte value)
    {
        return value < 128 ? value : value - 256;
    }

    internal static int ReadUnsigned16(byte[] data, int offset)
    {
        return (data[offset] << 8) | data[offset + 1];
    }

    internal static uint ReadUnsigned32(byte[] data, int offset)
    {
        return ((uint)data[offset] << 24)
            | ((uint)data[offset + 1] << 16)
            | ((uint)data[offset + 2] << 8)
            | data[offset + 3];
    }

    internal static int GetMaxFrameCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxFrameCount : options.MaxFrameCount;
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

    internal static bool ShouldParseXmp(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseXmp;
    }

    internal static bool ShouldParseIcc(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseIcc;
    }

    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions options)
    {
        return options == null || options.CaptureRawBlocks;
    }

    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions options)
    {
        return options != null && options.CaptureRawBlockData;
    }
}
