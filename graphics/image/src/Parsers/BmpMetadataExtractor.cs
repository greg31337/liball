using System.Globalization;

namespace image;

internal class BmpMetadataExtractor
{
    internal const uint BiRgb = 0;
    internal const uint BiRle8 = 1;
    internal const uint BiRle4 = 2;
    internal const uint BiBitfields = 3;
    internal const uint BiJpeg = 4;
    internal const uint BiPng = 5;
    internal const uint BiAlphaBitfields = 6;
    internal const uint LcsCalibratedRgb = 0;
    internal const uint LcsSrgb = 1934772034;
    internal const uint LcsWindowsColorSpace = 1466527264;
    internal const uint ProfileLinked = 1279872587;
    internal const uint ProfileEmbedded = 1296188740;

    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, BmpFileHeader fileHeader, BmpDibHeader header, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || fileHeader == null || header == null)
        {
            return;
        }

        MetadataDirectory directory = FindDibDirectory(document);
        AddCoreTags(directory, header);
        ValidateCoreFields(document, header);
        AddFrame(document, header, options);

        if (header.HeaderSize >= 40)
        {
            AddInfoTags(directory, header);
        }

        ReadExternalMasks(document, reader, fileHeader, header, directory);
        AddEmbeddedMasks(directory, header);
        ExtractPalette(document, reader, fileHeader, header, options);
        AddColorSpaceMetadata(document, directory, header);
        ExtractV5Profile(document, reader, header, options);
    }

    internal static void AddCoreTags(MetadataDirectory directory, BmpDibHeader header)
    {
        long offset = 14;
        if (header.HeaderSize == 12)
        {
            BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.Width", "Width", MetadataValue.FromInteger(header.Width), offset + 4, 2, "");
            BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.Height", "Height", MetadataValue.FromInteger(header.Height), offset + 6, 2, "");
            BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.Planes", "Planes", MetadataValue.FromInteger(header.Planes), offset + 8, 2, "");
            BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.BitCount", "Bits Per Pixel", MetadataValue.FromInteger(header.BitCount), offset + 10, 2, "");
            BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.TopDown", "Top-down", MetadataValue.FromBoolean(false), offset + 6, 2, "Bottom-up");
            return;
        }

        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.Width", "Width", MetadataValue.FromInteger(header.Width), offset + 4, 4, "");
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.Height", "Height", MetadataValue.FromInteger(header.Height), offset + 8, 4, "");
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.Planes", "Planes", MetadataValue.FromInteger(header.Planes), offset + 12, 2, "");
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.BitCount", "Bits Per Pixel", MetadataValue.FromInteger(header.BitCount), offset + 14, 2, "");
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.TopDown", "Top-down", MetadataValue.FromBoolean(header.Height < 0), offset + 8, 4, header.Height < 0 ? "Top-down" : "Bottom-up");
    }

    internal static void AddInfoTags(MetadataDirectory directory, BmpDibHeader header)
    {
        long offset = 14;
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.Compression", "Compression", MetadataValue.FromUnsignedInteger(header.Compression), offset + 16, 4, CompressionName(header.Compression, header.HeaderSize));
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.ImageSize", "Image Byte Length", MetadataValue.FromUnsignedInteger(header.ImageSize), offset + 20, 4, "");
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.XPixelsPerMeter", "Horizontal Pixels Per Meter", MetadataValue.FromInteger(header.XPelsPerMeter), offset + 24, 4, "");
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.YPixelsPerMeter", "Vertical Pixels Per Meter", MetadataValue.FromInteger(header.YPelsPerMeter), offset + 28, 4, "");
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.ResolutionUnit", "Resolution Unit", MetadataValue.FromString("pixels/meter"), offset + 24, 8, "pixels/meter");
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.ColorsUsed", "Colors Used", MetadataValue.FromUnsignedInteger(header.ColorsUsed), offset + 32, 4, "");
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.ColorsImportant", "Important Colors", MetadataValue.FromUnsignedInteger(header.ColorsImportant), offset + 36, 4, "");
    }

    internal static void AddFrame(ImageMetadataDocument document, BmpDibHeader header, ImageMetadataReaderOptions options)
    {
        if (document.Frames.Count >= MaxFrameCount(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmp.frame.limit", "BMP frame count exceeded the configured frame limit.", "DIBHeader", 14, header.HeaderSize);
            return;
        }

        ImageMetadataFrame frame = new ImageMetadataFrame();
        frame.Width = PositiveDimension(header.Width);
        frame.Height = AbsoluteDimension(header.Height);
        frame.CanvasWidth = frame.Width;
        frame.CanvasHeight = frame.Height;
        frame.BitDepth = header.BitCount;
        frame.ChannelCount = ChannelCount(header);
        frame.HasAlpha = header.AlphaMask != 0 || header.Compression == BiAlphaBitfields;
        frame.Compression = CompressionKind(header.Compression, header.HeaderSize);
        if (header.BitCount == 0)
        {
            frame.ColorSpace = ImageColorSpaceKind.Unknown;
        }
        else
        {
            frame.ColorSpace = header.BitCount <= 8 ? ImageColorSpaceKind.Indexed : ImageColorSpaceKind.Rgb;
        }
        frame.Orientation = ImageOrientation.Normal;
        frame.SourceColorType = frame.ColorSpace.ToString();
        frame.HorizontalResolution = header.XPelsPerMeter;
        frame.VerticalResolution = header.YPelsPerMeter;
        frame.ResolutionUnit = "pixels/meter";
        document.AddFrame(frame);
    }

    internal static void ValidateCoreFields(ImageMetadataDocument document, BmpDibHeader header)
    {
        if (header.Width <= 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmp.width.invalid", "BMP width must be greater than zero.", "DIBHeader", 18, header.HeaderSize == 12 ? 2 : 4);
        }

        if (header.Height == 0 || header.Height == -2147483648)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmp.height.invalid", "BMP height was not a usable nonzero value.", "DIBHeader", header.HeaderSize == 12 ? 20 : 22, header.HeaderSize == 12 ? 2 : 4);
        }

        if (header.Planes != 1)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmp.planes.invalid", "BMP plane count must be one.", "DIBHeader", header.HeaderSize == 12 ? 22 : 26, 2);
        }

        if (!ValidBitCount(header.BitCount, header.Compression))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmp.bit-count.invalid", "BMP bits per pixel was not valid for the declared compression.", "DIBHeader", header.HeaderSize == 12 ? 24 : 28, 2);
        }
    }

    internal static bool ValidBitCount(ushort bitCount, uint compression)
    {
        if (bitCount == 0)
        {
            return compression == BiJpeg || compression == BiPng;
        }

        return bitCount == 1 || bitCount == 4 || bitCount == 8 || bitCount == 16 || bitCount == 24 || bitCount == 32;
    }

    internal static void ReadExternalMasks(ImageMetadataDocument document, BoundedStreamReader reader, BmpFileHeader fileHeader, BmpDibHeader header, MetadataDirectory directory)
    {
        if (header.HeaderSize != 40 || (header.Compression != BiBitfields && header.Compression != BiAlphaBitfields))
        {
            return;
        }

        int maskCount = header.Compression == BiAlphaBitfields ? 4 : 3;
        int byteLength = maskCount * 4;
        long offset = 14 + header.HeaderSize;
        if ((ulong)fileHeader.PixelDataOffset < (ulong)(offset + byteLength) || reader.Remaining() < byteLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmp.masks.truncated", "BMP external color masks were truncated.", "ColorMasks", offset, byteLength);
            return;
        }

        byte[] data = reader.ReadBytes(byteLength);
        header.RedMask = BmpHeaderParser.ReadUnsigned32(data, 0);
        header.GreenMask = BmpHeaderParser.ReadUnsigned32(data, 4);
        header.BlueMask = BmpHeaderParser.ReadUnsigned32(data, 8);
        if (maskCount == 4)
        {
            header.AlphaMask = BmpHeaderParser.ReadUnsigned32(data, 12);
        }

        AddMaskTags(directory, header, offset, maskCount == 4);
    }

    internal static void AddEmbeddedMasks(MetadataDirectory directory, BmpDibHeader header)
    {
        if (header.HeaderSize < 52 || header.HeaderSize == 64)
        {
            return;
        }

        AddMaskTags(directory, header, 54, header.HeaderSize >= 56);
    }

    internal static void AddMaskTags(MetadataDirectory directory, BmpDibHeader header, long offset, bool includeAlpha)
    {
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.RedMask", "Red Mask", MetadataValue.FromUnsignedInteger(header.RedMask), offset, 4, Hex32(header.RedMask));
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.GreenMask", "Green Mask", MetadataValue.FromUnsignedInteger(header.GreenMask), offset + 4, 4, Hex32(header.GreenMask));
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.BlueMask", "Blue Mask", MetadataValue.FromUnsignedInteger(header.BlueMask), offset + 8, 4, Hex32(header.BlueMask));
        if (includeAlpha)
        {
            BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.AlphaMask", "Alpha Mask", MetadataValue.FromUnsignedInteger(header.AlphaMask), offset + 12, 4, Hex32(header.AlphaMask));
        }
    }

    internal static void ExtractPalette(ImageMetadataDocument document, BoundedStreamReader reader, BmpFileHeader fileHeader, BmpDibHeader header, ImageMetadataReaderOptions options)
    {
        ulong entryCount = PaletteEntryCount(header);
        if (entryCount == 0)
        {
            return;
        }

        int entrySize = header.HeaderSize == 12 ? 3 : 4;
        ulong byteLength = entryCount * (ulong)entrySize;
        long offset = reader.Position;
        long maximum = MaxSegmentBytes(options);
        if (maximum < 0 || byteLength > (ulong)maximum || byteLength > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmp.palette.limit", "BMP palette exceeded the configured segment limit.", "Palette", offset, ClampLength(byteLength));
            return;
        }

        ulong availableBeforePixels = 0;
        if ((ulong)fileHeader.PixelDataOffset >= (ulong)offset)
        {
            availableBeforePixels = (ulong)fileHeader.PixelDataOffset - (ulong)offset;
        }

        if (byteLength > availableBeforePixels || byteLength > (ulong)reader.Remaining())
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmp.palette.truncated", "BMP palette was truncated.", "Palette", offset, ClampLength(byteLength));
            return;
        }

        byte[] data = reader.ReadBytes((int)byteLength);
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Bmp, "Palette", "BMP Color Palette");
        directory.Path = "BMP/Palette";
        directory.Offset = offset;
        directory.Length = data.Length;
        BmpHeaderParser.AddTag(directory, "Bmp.Palette.EntryCount", "Palette Entry Count", MetadataValue.FromUnsignedInteger(entryCount), offset, data.Length, "");
        BmpHeaderParser.AddTag(directory, "Bmp.Palette.EntrySize", "Palette Entry Size", MetadataValue.FromInteger(entrySize), offset, data.Length, entrySize.ToString() + " bytes");
        MetadataTag entries = BmpHeaderParser.AddTag(directory, "Bmp.Palette.Entries", "Palette Entries", MetadataValue.FromBytes(BmpHeaderParser.CopyBytes(data, 0, data.Length)), offset, data.Length, "");
        entries.RawValue = BmpHeaderParser.CopyBytes(data, 0, data.Length);
        BmpHeaderParser.AddRawBlock(document, options, "Palette", "BMP Color Palette", offset, data);
    }

    internal static ulong PaletteEntryCount(BmpDibHeader header)
    {
        if (header.BitCount == 0)
        {
            return 0;
        }

        if (header.HeaderSize == 12 || header.HeaderSize == 16)
        {
            if (header.BitCount <= 8)
            {
                return (ulong)1 << header.BitCount;
            }

            return 0;
        }

        if (header.ColorsUsed != 0)
        {
            return header.ColorsUsed;
        }

        if (header.BitCount <= 8)
        {
            return (ulong)1 << header.BitCount;
        }

        return 0;
    }

    internal static void AddColorSpaceMetadata(ImageMetadataDocument document, MetadataDirectory directory, BmpDibHeader header)
    {
        if (header.HeaderSize < 108)
        {
            return;
        }

        long offset = 14;
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.ColorSpaceType", "Color Space Type", MetadataValue.FromUnsignedInteger(header.ColorSpaceType), offset + 56, 4, ColorSpaceName(header.ColorSpaceType));
        MetadataTag endpoints = BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.ColorSpaceEndpoints", "Color Space Endpoints", MetadataValue.FromBytes(BmpHeaderParser.CopyBytes(header.ColorSpaceEndpoints, 0, header.ColorSpaceEndpoints.Length)), offset + 60, 36, "");
        endpoints.RawValue = BmpHeaderParser.CopyBytes(header.ColorSpaceEndpoints, 0, header.ColorSpaceEndpoints.Length);
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.GammaRed", "Red Gamma", MetadataValue.FromUnsignedInteger(header.GammaRed), offset + 96, 4, Fixed16Dot16(header.GammaRed));
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.GammaGreen", "Green Gamma", MetadataValue.FromUnsignedInteger(header.GammaGreen), offset + 100, 4, Fixed16Dot16(header.GammaGreen));
        BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.GammaBlue", "Blue Gamma", MetadataValue.FromUnsignedInteger(header.GammaBlue), offset + 104, 4, Fixed16Dot16(header.GammaBlue));

        if (document.Frames.Count > 0 && (header.ColorSpaceType == LcsSrgb || header.ColorSpaceType == LcsWindowsColorSpace))
        {
            document.Frames[0].ColorSpace = ImageColorSpaceKind.Rgb;
        }

        if (header.HeaderSize >= 124)
        {
            BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.RenderingIntent", "Rendering Intent", MetadataValue.FromUnsignedInteger(header.Intent), offset + 108, 4, RenderingIntentName(header.Intent));
            BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.ProfileDataOffset", "Profile Data Offset", MetadataValue.FromUnsignedInteger(header.ProfileDataOffset), offset + 112, 4, "Relative to DIB header");
            BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.ProfileSize", "Profile Byte Length", MetadataValue.FromUnsignedInteger(header.ProfileSize), offset + 116, 4, "");
            BmpHeaderParser.AddTag(directory, "Bmp.DIBHeader.Reserved", "Reserved", MetadataValue.FromUnsignedInteger(header.Reserved), offset + 120, 4, "");
        }
    }

    internal static void ExtractV5Profile(ImageMetadataDocument document, BoundedStreamReader reader, BmpDibHeader header, ImageMetadataReaderOptions options)
    {
        if (header.HeaderSize < 124 || (header.ColorSpaceType != ProfileEmbedded && header.ColorSpaceType != ProfileLinked))
        {
            return;
        }

        ulong offset = 14 + (ulong)header.ProfileDataOffset;
        ulong end = offset + (ulong)header.ProfileSize;
        if (header.ProfileSize == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmp.profile.size.invalid", "BMP V5 color profile size was zero.", "ColorProfile", ClampOffset(offset), 0);
            return;
        }

        if (end < offset || end > (ulong)document.ByteLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmp.profile.truncated", "BMP V5 color profile exceeded the stream length.", "ColorProfile", ClampOffset(offset), header.ProfileSize);
            return;
        }

        MetadataDirectory directory = AddProfileDirectory(document, header, (long)offset);
        MetadataProfile? profile = null;
        if (header.ColorSpaceType == ProfileEmbedded && ShouldParseIcc(options))
        {
            profile = CreateIccProfile((long)offset, header.ProfileSize);
        }

        if ((long)header.ProfileSize > MaxProfileBytes(options) || header.ProfileSize > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmp.profile.limit", "BMP V5 color profile exceeded the configured profile limit.", "ColorProfile", ClampOffset(offset), header.ProfileSize);
            if (profile != null)
            {
                document.AddProfile(profile);
            }

            return;
        }

        if (header.ColorSpaceType == ProfileEmbedded && profile == null)
        {
            return;
        }

        if (!reader.SeekAbsolute((long)offset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmp.profile.seek", "BMP V5 color profile could not be reached.", "ColorProfile", ClampOffset(offset), header.ProfileSize);
            return;
        }

        byte[] data = reader.ReadBytes((int)header.ProfileSize);
        if (header.ColorSpaceType == ProfileLinked)
        {
            string linkedName = NullTerminatedAscii(data);
            BmpHeaderParser.AddTag(directory, "Bmp.ColorProfile.LinkedName", "Linked Profile Name", MetadataValue.FromString(linkedName), (long)offset, data.Length, linkedName);
            return;
        }

        if (profile == null)
        {
            return;
        }

        profile.Data = BmpHeaderParser.CopyBytes(data, 0, data.Length);
        document.AddProfile(profile);
        IccProfileParser.ParseProfile(document, profile, options, "BMP/ColorProfile/ICC", "ColorProfile");
    }

    internal static MetadataDirectory AddProfileDirectory(ImageMetadataDocument document, BmpDibHeader header, long offset)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(header.ColorSpaceType == ProfileEmbedded ? MetadataFamily.Icc : MetadataFamily.Bmp, "ColorProfile", "BMP V5 Color Profile");
        directory.Path = "BMP/ColorProfile";
        directory.Offset = offset;
        directory.Length = header.ProfileSize;
        BmpHeaderParser.AddTag(directory, "Bmp.ColorProfile.Type", "Profile Type", MetadataValue.FromString(ColorSpaceName(header.ColorSpaceType)), offset, header.ProfileSize, ColorSpaceName(header.ColorSpaceType));
        BmpHeaderParser.AddTag(directory, "Bmp.ColorProfile.ByteLength", "Profile Byte Length", MetadataValue.FromUnsignedInteger(header.ProfileSize), offset, header.ProfileSize, "");
        return directory;
    }

    internal static MetadataProfile CreateIccProfile(long offset, uint length)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Icc;
        profile.Family = MetadataFamily.Icc;
        profile.Key = "Icc.BMP.V5";
        profile.Name = "BMP V5 ICC Profile";
        profile.MimeType = "application/vnd.iccprofile";
        profile.Offset = offset;
        profile.Length = length;
        return profile;
    }

    internal static MetadataDirectory FindDibDirectory(ImageMetadataDocument document)
    {
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory != null && directory.Group == "DIBHeader")
            {
                return directory;
            }
        }

        return document.AddDirectoryWithName(MetadataFamily.Bmp, "DIBHeader", "BMP DIB Header");
    }

    internal static string CompressionName(uint compression, uint headerSize)
    {
        if (headerSize == 64)
        {
            switch (compression)
            {
                case 0:
                    return "RGB";
                case 1:
                    return "RLE8";
                case 2:
                    return "RLE4";
                case 3:
                    return "Huffman 1D";
                case 4:
                    return "RLE24";
                default:
                    return "Unknown";
            }
        }

        switch (compression)
        {
            case BiRgb:
                return "RGB";
            case BiRle8:
                return "RLE8";
            case BiRle4:
                return "RLE4";
            case BiBitfields:
                return "Bitfields";
            case BiJpeg:
                return "JPEG";
            case BiPng:
                return "PNG";
            case BiAlphaBitfields:
                return "Alpha bitfields";
            default:
                return "Unknown";
        }
    }

    internal static ImageCompressionKind CompressionKind(uint compression, uint headerSize)
    {
        if (headerSize == 12 || headerSize == 16 || compression == BiRgb || compression == BiBitfields || compression == BiAlphaBitfields)
        {
            return ImageCompressionKind.None;
        }

        if (compression == BiRle8 || compression == BiRle4 || (headerSize == 64 && compression == 4))
        {
            return ImageCompressionKind.Rle;
        }

        if (compression == BiJpeg && headerSize != 64)
        {
            return ImageCompressionKind.Jpeg;
        }

        if (compression == BiPng && headerSize != 64)
        {
            return ImageCompressionKind.Deflate;
        }

        return ImageCompressionKind.Unknown;
    }

    internal static int ChannelCount(BmpDibHeader header)
    {
        if (header.BitCount == 0)
        {
            return 0;
        }

        if (header.BitCount <= 8)
        {
            return 1;
        }

        if (header.AlphaMask != 0 || header.Compression == BiAlphaBitfields)
        {
            return 4;
        }

        return 3;
    }

    internal static int PositiveDimension(int value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return value;
    }

    internal static int AbsoluteDimension(int value)
    {
        if (value == -2147483648)
        {
            return 0;
        }

        return value < 0 ? -value : value;
    }

    internal static string ColorSpaceName(uint value)
    {
        switch (value)
        {
            case LcsCalibratedRgb:
                return "Calibrated RGB";
            case LcsSrgb:
                return "sRGB";
            case LcsWindowsColorSpace:
                return "Windows color space";
            case ProfileLinked:
                return "Linked profile";
            case ProfileEmbedded:
                return "Embedded profile";
            default:
                return "Unknown";
        }
    }

    internal static string RenderingIntentName(uint value)
    {
        switch (value)
        {
            case 1:
                return "Business";
            case 2:
                return "Graphics";
            case 4:
                return "Images";
            case 8:
                return "Absolute colorimetric";
            default:
                return "Unknown";
        }
    }

    internal static string Fixed16Dot16(uint value)
    {
        return ((double)value / 65536.0).ToString("0.####", CultureInfo.InvariantCulture);
    }

    internal static string Hex32(uint value)
    {
        return "0x" + value.ToString("X8", CultureInfo.InvariantCulture);
    }

    internal static string NullTerminatedAscii(byte[] data)
    {
        int length = 0;
        while (length < data.Length && data[length] != 0)
        {
            length = length + 1;
        }

        return MetadataStringReader.DecodeAsciiRange(data, 0, length, null, "ColorProfile");
    }

    internal static bool ShouldParseIcc(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseIcc;
    }

    internal static long MaxProfileBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxProfileBytes : options.MaxProfileBytes;
    }

    internal static long MaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
    }

    internal static int MaxFrameCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxFrameCount : options.MaxFrameCount;
    }

    internal static long ClampLength(ulong value)
    {
        if (value > 9223372036854775807)
        {
            return 9223372036854775807;
        }

        return (long)value;
    }

    internal static long ClampOffset(ulong value)
    {
        return ClampLength(value);
    }
}
