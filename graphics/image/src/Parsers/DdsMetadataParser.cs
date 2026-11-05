using System.Text;

namespace image;

internal class DdsHeader
{
    internal long Size;
    internal long Flags;
    internal long Height;
    internal long Width;
    internal long PitchOrLinearSize;
    internal long Depth;
    internal long MipMapCount;
    internal long PixelFormatSize;
    internal long PixelFormatFlags;
    internal string FourCc;
    internal long FourCcValue;
    internal long RgbBitCount;
    internal long RedMask;
    internal long GreenMask;
    internal long BlueMask;
    internal long AlphaMask;
    internal long Caps;
    internal long Caps2;
    internal long Caps3;
    internal long Caps4;
    internal long Reserved2;

    internal DdsHeader()
    {
        FourCc = "";
    }
}

internal class DdsDx10Header
{
    internal long DxgiFormat;
    internal long ResourceDimension;
    internal long MiscFlag;
    internal long ArraySize;
    internal long MiscFlags2;
}

internal class DdsMetadataParser
{
    internal const int MagicLength = 4;
    internal const int HeaderLength = 124;
    internal const int FileHeaderLength = 128;
    internal const int PixelFormatLength = 32;
    internal const int Dx10HeaderLength = 20;
    internal const long HeaderSizeValue = 124;
    internal const long PixelFormatSizeValue = 32;
    internal const long PixelFormatAlphaPixels = 1;
    internal const long PixelFormatAlpha = 2;
    internal const long PixelFormatFourCc = 4;
    internal const long PixelFormatRgb = 64;
    internal const long PixelFormatYuv = 512;
    internal const long PixelFormatLuminance = 131072;
    internal const long HeaderFlagPitch = 8;
    internal const long HeaderFlagMipMapCount = 131072;
    internal const long HeaderFlagLinearSize = 524288;
    internal const long HeaderFlagDepth = 8388608;
    internal const long CapsComplex = 8;
    internal const long CapsTexture = 4096;
    internal const long CapsMipMap = 4194304;
    internal const long Caps2CubeMap = 512;
    internal const long Caps2CubeMapPositiveX = 1024;
    internal const long Caps2CubeMapNegativeX = 2048;
    internal const long Caps2CubeMapPositiveY = 4096;
    internal const long Caps2CubeMapNegativeY = 8192;
    internal const long Caps2CubeMapPositiveZ = 16384;
    internal const long Caps2CubeMapNegativeZ = 32768;
    internal const long Caps2Volume = 2097152;
    internal const long Dx10MiscTextureCube = 4;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "dds.reader.null", "DDS parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        byte[] bytes = ReadRange(reader, 0, FileHeaderLength);
        if (bytes.Length != FileHeaderLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "dds.header.truncated", "DDS magic and header were truncated.", "DDS Header", 0, FileHeaderLength);
            return Finish(document, reader);
        }
        if (!HasMagic(bytes))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "dds.magic.invalid", "DDS magic was not present.", "DDS Header", 0, MagicLength);
            return Finish(document, reader);
        }

        DdsHeader header = DecodeHeader(bytes);
        AddHeaderDirectory(document, header);
        AddPixelFormatDirectory(document, header);
        CaptureBlock(document, reader, options, "Header", "DDS Header", 0, FileHeaderLength);
        bool valid = ValidateHeader(document, header);
        DdsDx10Header? dx10 = null;
        if (header.FourCc == "DX10")
        {
            dx10 = ParseDx10Header(document, reader, options, header);
        }
        if (valid)
        {
            AddFrame(document, header, dx10);
        }
        ImageMetadataNormalizerCore.BuildSummary(document);
        return Finish(document, reader);
    }

    internal static DdsHeader DecodeHeader(byte[] bytes)
    {
        DdsHeader header = new DdsHeader();
        header.Size = ReadUnsigned32(bytes, 4);
        header.Flags = ReadUnsigned32(bytes, 8);
        header.Height = ReadUnsigned32(bytes, 12);
        header.Width = ReadUnsigned32(bytes, 16);
        header.PitchOrLinearSize = ReadUnsigned32(bytes, 20);
        header.Depth = ReadUnsigned32(bytes, 24);
        header.MipMapCount = ReadUnsigned32(bytes, 28);
        header.PixelFormatSize = ReadUnsigned32(bytes, 76);
        header.PixelFormatFlags = ReadUnsigned32(bytes, 80);
        header.FourCc = DecodeFourCc(bytes, 84);
        header.FourCcValue = ReadUnsigned32(bytes, 84);
        header.RgbBitCount = ReadUnsigned32(bytes, 88);
        header.RedMask = ReadUnsigned32(bytes, 92);
        header.GreenMask = ReadUnsigned32(bytes, 96);
        header.BlueMask = ReadUnsigned32(bytes, 100);
        header.AlphaMask = ReadUnsigned32(bytes, 104);
        header.Caps = ReadUnsigned32(bytes, 108);
        header.Caps2 = ReadUnsigned32(bytes, 112);
        header.Caps3 = ReadUnsigned32(bytes, 116);
        header.Caps4 = ReadUnsigned32(bytes, 120);
        header.Reserved2 = ReadUnsigned32(bytes, 124);
        return header;
    }

    internal static bool ValidateHeader(ImageMetadataDocument document, DdsHeader header)
    {
        bool valid = true;
        if (header.Size != HeaderSizeValue)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "dds.header.size.invalid", "DDS header size was not 124 bytes.", "DDS Header", 4, 4);
            valid = false;
        }
        if (header.PixelFormatSize != PixelFormatSizeValue)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "dds.pixel-format.size.invalid", "DDS pixel format size was not 32 bytes.", "DDS Pixel Format", 76, 4);
            valid = false;
        }
        if (header.Width == 0 || header.Height == 0 || header.Width > 2147483647 || header.Height > 2147483647)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "dds.dimensions.invalid", "DDS dimensions were zero or exceeded the supported range.", "DDS Header", 12, 8);
            valid = false;
        }
        if ((header.Flags & HeaderFlagPitch) != 0 && (header.Flags & HeaderFlagLinearSize) != 0)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "dds.pitch-linear-size.conflict", "DDS header declared both pitch and linear size.", "DDS Header", 8, 16);
        }
        if ((header.Flags & HeaderFlagMipMapCount) != 0 && header.MipMapCount == 0)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "dds.mipmap-count.zero", "DDS header declared mipmaps with a zero mipmap count.", "DDS Header", 28, 4);
        }
        if ((header.Caps2 & Caps2Volume) != 0 && header.Depth == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "dds.volume.depth.invalid", "DDS volume texture had zero depth.", "DDS Header", 24, 4);
            valid = false;
        }
        if ((header.Caps2 & Caps2CubeMap) != 0 && CubeFaceCount(header.Caps2) == 0)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "dds.cubemap.faces.missing", "DDS cube map did not declare any faces.", "DDS Header", 112, 4);
        }
        if (!HasRecognizedPixelFormat(header))
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "dds.pixel-format.unknown", "DDS pixel format flags did not identify a recognized pixel representation.", "DDS Pixel Format", 80, PixelFormatLength - 4);
        }
        return valid;
    }

    internal static void AddHeaderDirectory(ImageMetadataDocument document, DdsHeader header)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "Header", "DDS Header");
        directory.Path = "DDS/Header";
        directory.Offset = 0;
        directory.Length = FileHeaderLength;
        AddTag(directory, "Header.Magic", "Magic", MetadataValue.FromString("DDS "), 0, 4, "DDS");
        AddTag(directory, "Header.Size", "Header Size", MetadataValue.FromUnsignedInteger((ulong)header.Size), 4, 4, "bytes");
        AddTag(directory, "Header.Flags", "Header Flags", MetadataValue.FromUnsignedInteger((ulong)header.Flags), 8, 4, HeaderFlagsText(header.Flags));
        AddTag(directory, "Header.Height", "Height", MetadataValue.FromUnsignedInteger((ulong)header.Height), 12, 4, "pixels");
        AddTag(directory, "Header.Width", "Width", MetadataValue.FromUnsignedInteger((ulong)header.Width), 16, 4, "pixels");
        AddTag(directory, "Header.PitchOrLinearSize", "Pitch or Linear Size", MetadataValue.FromUnsignedInteger((ulong)header.PitchOrLinearSize), 20, 4, PitchOrLinearSizeName(header.Flags));
        AddTag(directory, "Header.Depth", "Depth", MetadataValue.FromUnsignedInteger((ulong)header.Depth), 24, 4, "pixels");
        AddTag(directory, "Header.MipMapCount", "Mipmap Count", MetadataValue.FromUnsignedInteger((ulong)header.MipMapCount), 28, 4, "levels");
        AddTag(directory, "Header.Caps", "Surface Capabilities", MetadataValue.FromUnsignedInteger((ulong)header.Caps), 108, 4, CapsText(header.Caps));
        AddTag(directory, "Header.Caps2", "Additional Capabilities", MetadataValue.FromUnsignedInteger((ulong)header.Caps2), 112, 4, Caps2Text(header.Caps2));
        AddTag(directory, "Header.Caps3", "Capabilities 3", MetadataValue.FromUnsignedInteger((ulong)header.Caps3), 116, 4, "");
        AddTag(directory, "Header.Caps4", "Capabilities 4", MetadataValue.FromUnsignedInteger((ulong)header.Caps4), 120, 4, "");
        AddTag(directory, "Header.Reserved2", "Reserved", MetadataValue.FromUnsignedInteger((ulong)header.Reserved2), 124, 4, "");
        AddTag(directory, "Header.IsCubeMap", "Cube Map", MetadataValue.FromBoolean((header.Caps2 & Caps2CubeMap) != 0), 112, 4, "");
        AddTag(directory, "Header.CubeFaceCount", "Cube Face Count", MetadataValue.FromInteger(CubeFaceCount(header.Caps2)), 112, 4, "faces");
        AddTag(directory, "Header.IsVolume", "Volume Texture", MetadataValue.FromBoolean((header.Caps2 & Caps2Volume) != 0), 112, 4, "");
        AddTag(directory, "Header.DataOffset", "Texture Data Offset", MetadataValue.FromInteger(header.FourCc == "DX10" ? FileHeaderLength + Dx10HeaderLength : FileHeaderLength), 0, FileHeaderLength, "bytes");
    }

    internal static void AddPixelFormatDirectory(ImageMetadataDocument document, DdsHeader header)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "PixelFormat", "DDS Pixel Format");
        directory.Path = "DDS/PixelFormat";
        directory.Offset = 76;
        directory.Length = PixelFormatLength;
        AddTag(directory, "PixelFormat.Size", "Pixel Format Size", MetadataValue.FromUnsignedInteger((ulong)header.PixelFormatSize), 76, 4, "bytes");
        AddTag(directory, "PixelFormat.Flags", "Pixel Format Flags", MetadataValue.FromUnsignedInteger((ulong)header.PixelFormatFlags), 80, 4, PixelFormatFlagsText(header.PixelFormatFlags));
        AddTag(directory, "PixelFormat.FourCC", "FourCC", MetadataValue.FromString(header.FourCc), 84, 4, FourCcName(header.FourCc, header.FourCcValue));
        AddTag(directory, "PixelFormat.FourCCValue", "FourCC Value", MetadataValue.FromUnsignedInteger((ulong)header.FourCcValue), 84, 4, "");
        AddTag(directory, "PixelFormat.RgbBitCount", "RGB Bit Count", MetadataValue.FromUnsignedInteger((ulong)header.RgbBitCount), 88, 4, "bits");
        AddTag(directory, "PixelFormat.RedMask", "Red Mask", MetadataValue.FromUnsignedInteger((ulong)header.RedMask), 92, 4, Hex32(header.RedMask));
        AddTag(directory, "PixelFormat.GreenMask", "Green Mask", MetadataValue.FromUnsignedInteger((ulong)header.GreenMask), 96, 4, Hex32(header.GreenMask));
        AddTag(directory, "PixelFormat.BlueMask", "Blue Mask", MetadataValue.FromUnsignedInteger((ulong)header.BlueMask), 100, 4, Hex32(header.BlueMask));
        AddTag(directory, "PixelFormat.AlphaMask", "Alpha Mask", MetadataValue.FromUnsignedInteger((ulong)header.AlphaMask), 104, 4, Hex32(header.AlphaMask));
        AddTag(directory, "PixelFormat.Name", "Pixel Format", MetadataValue.FromString(PixelFormatName(header)), 80, PixelFormatLength - 4, PixelFormatName(header));
        AddTag(directory, "PixelFormat.IsBlockCompressed", "Block Compressed", MetadataValue.FromBoolean(IsBlockCompressed(header.FourCc)), 84, 4, "");
    }

    internal static DdsDx10Header? ParseDx10Header(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, DdsHeader header)
    {
        byte[] bytes = ReadRange(reader, FileHeaderLength, Dx10HeaderLength);
        if (bytes.Length != Dx10HeaderLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "dds.dx10.truncated", "DDS DX10 extended header was truncated.", "DDS DX10 Header", FileHeaderLength, Dx10HeaderLength);
            return null;
        }
        DdsDx10Header dx10 = new DdsDx10Header();
        dx10.DxgiFormat = ReadUnsigned32(bytes, 0);
        dx10.ResourceDimension = ReadUnsigned32(bytes, 4);
        dx10.MiscFlag = ReadUnsigned32(bytes, 8);
        dx10.ArraySize = ReadUnsigned32(bytes, 12);
        dx10.MiscFlags2 = ReadUnsigned32(bytes, 16);
        AddDx10Directory(document, dx10);
        ValidateDx10Header(document, header, dx10);
        CaptureBlock(document, reader, options, "DX10Header", "DDS DX10 Extended Header", FileHeaderLength, Dx10HeaderLength);
        return dx10;
    }

    internal static void AddDx10Directory(ImageMetadataDocument document, DdsDx10Header dx10)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "DX10", "DDS DX10 Extended Header");
        directory.Path = "DDS/DX10";
        directory.Offset = FileHeaderLength;
        directory.Length = Dx10HeaderLength;
        AddTag(directory, "DX10.DxgiFormat", "DXGI Format", MetadataValue.FromUnsignedInteger((ulong)dx10.DxgiFormat), 128, 4, DxgiFormatName(dx10.DxgiFormat));
        AddTag(directory, "DX10.ResourceDimension", "Resource Dimension", MetadataValue.FromUnsignedInteger((ulong)dx10.ResourceDimension), 132, 4, ResourceDimensionName(dx10.ResourceDimension));
        AddTag(directory, "DX10.MiscFlag", "Miscellaneous Flags", MetadataValue.FromUnsignedInteger((ulong)dx10.MiscFlag), 136, 4, (dx10.MiscFlag & Dx10MiscTextureCube) != 0 ? "Texture cube" : "");
        AddTag(directory, "DX10.ArraySize", "Array Size", MetadataValue.FromUnsignedInteger((ulong)dx10.ArraySize), 140, 4, "elements");
        AddTag(directory, "DX10.MiscFlags2", "Alpha Mode Flags", MetadataValue.FromUnsignedInteger((ulong)dx10.MiscFlags2), 144, 4, AlphaModeName(dx10.MiscFlags2 & 7));
        AddTag(directory, "DX10.AlphaMode", "Alpha Mode", MetadataValue.FromUnsignedInteger((ulong)(dx10.MiscFlags2 & 7)), 144, 4, AlphaModeName(dx10.MiscFlags2 & 7));
        AddTag(directory, "DX10.IsCubeMap", "Cube Map", MetadataValue.FromBoolean((dx10.MiscFlag & Dx10MiscTextureCube) != 0), 136, 4, "");
        long surfaceCount = dx10.ArraySize;
        if ((dx10.MiscFlag & Dx10MiscTextureCube) != 0)
        {
            surfaceCount = surfaceCount * 6;
        }
        AddTag(directory, "DX10.SurfaceCount", "Surface Count", MetadataValue.FromUnsignedInteger((ulong)surfaceCount), 136, 8, "surfaces");
    }

    internal static void ValidateDx10Header(ImageMetadataDocument document, DdsHeader header, DdsDx10Header dx10)
    {
        if (dx10.ResourceDimension != 2 && dx10.ResourceDimension != 3 && dx10.ResourceDimension != 4)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "dds.dx10.dimension.invalid", "DDS DX10 resource dimension was not a texture dimension.", "DDS DX10 Header", 132, 4);
        }
        if (dx10.ArraySize == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "dds.dx10.array-size.invalid", "DDS DX10 array size was zero.", "DDS DX10 Header", 140, 4);
        }
        if (dx10.DxgiFormat == 0)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "dds.dx10.format.unknown", "DDS DX10 pixel format was unknown.", "DDS DX10 Header", 128, 4);
        }
        if (dx10.ResourceDimension == 2 && header.Height != 1)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "dds.dx10.texture1d.height", "DDS DX10 1D texture height was not one.", "DDS Header", 12, 4);
        }
        if (dx10.ResourceDimension == 4 && (header.Depth == 0 || dx10.ArraySize != 1))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "dds.dx10.texture3d.invalid", "DDS DX10 3D texture required nonzero depth and an array size of one.", "DDS DX10 Header", 140, 4);
        }
        if ((dx10.MiscFlag & Dx10MiscTextureCube) != 0 && dx10.ResourceDimension != 3)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "dds.dx10.cubemap.dimension", "DDS DX10 cube map was not declared as a 2D texture.", "DDS DX10 Header", 132, 8);
        }
        if ((dx10.MiscFlags2 & 7) > 4)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "dds.dx10.alpha-mode.unknown", "DDS DX10 alpha mode was unknown.", "DDS DX10 Header", 144, 4);
        }
        if ((dx10.MiscFlags2 & 4294967288L) != 0)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "dds.dx10.misc-flags2.reserved", "DDS DX10 alpha mode field had reserved bits set.", "DDS DX10 Header", 144, 4);
        }
    }

    internal static void AddFrame(ImageMetadataDocument document, DdsHeader header, DdsDx10Header? dx10)
    {
        ImageMetadataFrame frame = document.AddFrameWithDimensions((int)header.Width, (int)header.Height);
        frame.BitDepth = PixelBitDepth(header, dx10);
        frame.ChannelCount = ChannelCount(header, dx10);
        frame.HasAlpha = HasAlpha(header, dx10);
        frame.ColorSpace = ColorSpace(header, dx10);
        frame.Compression = IsCompressed(header, dx10) ? ImageCompressionKind.Unknown : ImageCompressionKind.None;
        frame.Orientation = ImageOrientation.Normal;
    }

    internal static int PixelBitDepth(DdsHeader header, DdsDx10Header? dx10)
    {
        if (dx10 != null)
        {
            return DxgiBitDepth(dx10.DxgiFormat);
        }
        if ((header.PixelFormatFlags & PixelFormatRgb) != 0)
        {
            int maximum = CountMaskBits(header.RedMask);
            int green = CountMaskBits(header.GreenMask);
            int blue = CountMaskBits(header.BlueMask);
            int alpha = CountMaskBits(header.AlphaMask);
            if (green > maximum) maximum = green;
            if (blue > maximum) maximum = blue;
            if (alpha > maximum) maximum = alpha;
            if (maximum > 0) return maximum;
        }
        if ((header.PixelFormatFlags & PixelFormatLuminance) != 0 && header.RgbBitCount > 0 && header.RgbBitCount <= 32)
        {
            int channels = header.AlphaMask == 0 ? 1 : 2;
            return (int)header.RgbBitCount / channels;
        }
        if ((header.PixelFormatFlags & PixelFormatAlpha) != 0 && header.RgbBitCount > 0 && header.RgbBitCount <= 32)
        {
            return (int)header.RgbBitCount;
        }
        if ((header.PixelFormatFlags & PixelFormatYuv) != 0)
        {
            int maximum = CountMaskBits(header.RedMask);
            int green = CountMaskBits(header.GreenMask);
            int blue = CountMaskBits(header.BlueMask);
            if (green > maximum) maximum = green;
            if (blue > maximum) maximum = blue;
            if (maximum > 0) return maximum;
            if (header.RgbBitCount > 0 && header.RgbBitCount <= 96) return (int)header.RgbBitCount / 3;
        }
        return FourCcBitDepth(header.FourCc, header.FourCcValue);
    }

    internal static int ChannelCount(DdsHeader header, DdsDx10Header? dx10)
    {
        if (dx10 != null)
        {
            return DxgiChannelCount(dx10.DxgiFormat);
        }
        if ((header.PixelFormatFlags & PixelFormatRgb) != 0)
        {
            int channels = 0;
            if (header.RedMask != 0) channels = channels + 1;
            if (header.GreenMask != 0) channels = channels + 1;
            if (header.BlueMask != 0) channels = channels + 1;
            if (header.AlphaMask != 0) channels = channels + 1;
            return channels;
        }
        if ((header.PixelFormatFlags & PixelFormatLuminance) != 0)
        {
            return header.AlphaMask == 0 ? 1 : 2;
        }
        if ((header.PixelFormatFlags & PixelFormatAlpha) != 0)
        {
            return 1;
        }
        if ((header.PixelFormatFlags & PixelFormatYuv) != 0)
        {
            return 3;
        }
        return FourCcChannelCount(header.FourCc, header.FourCcValue);
    }

    internal static bool HasAlpha(DdsHeader header, DdsDx10Header? dx10)
    {
        if (dx10 != null)
        {
            long alphaMode = dx10.MiscFlags2 & 7;
            if (alphaMode == 3 || alphaMode == 4)
            {
                return false;
            }
            return DxgiHasAlpha(dx10.DxgiFormat);
        }
        if ((header.PixelFormatFlags & PixelFormatAlpha) != 0 || (header.PixelFormatFlags & PixelFormatAlphaPixels) != 0 || header.AlphaMask != 0)
        {
            return true;
        }
        if (header.FourCc == "DXT1" || header.FourCc == "DXT2" || header.FourCc == "DXT3" || header.FourCc == "DXT4" || header.FourCc == "DXT5") return true;
        return header.FourCcValue == 36 || header.FourCcValue == 113 || header.FourCcValue == 116;
    }

    internal static ImageColorSpaceKind ColorSpace(DdsHeader header, DdsDx10Header? dx10)
    {
        if (dx10 != null)
        {
            int channels = DxgiChannelCount(dx10.DxgiFormat);
            if (channels == 1 && !DxgiHasAlpha(dx10.DxgiFormat)) return ImageColorSpaceKind.Gray;
            if (channels >= 3) return ImageColorSpaceKind.Rgb;
            return ImageColorSpaceKind.Unknown;
        }
        if ((header.PixelFormatFlags & PixelFormatLuminance) != 0) return ImageColorSpaceKind.Gray;
        if ((header.PixelFormatFlags & PixelFormatRgb) != 0) return ImageColorSpaceKind.Rgb;
        if ((header.PixelFormatFlags & PixelFormatYuv) != 0 || header.FourCc == "YUY2" || header.FourCc == "UYVY") return ImageColorSpaceKind.YCbCr;
        if (FourCcChannelCount(header.FourCc, header.FourCcValue) >= 3) return ImageColorSpaceKind.Rgb;
        if (FourCcChannelCount(header.FourCc, header.FourCcValue) == 1) return ImageColorSpaceKind.Gray;
        return ImageColorSpaceKind.Unknown;
    }

    internal static bool IsCompressed(DdsHeader header, DdsDx10Header? dx10)
    {
        if (dx10 != null)
        {
            return IsDxgiBlockCompressed(dx10.DxgiFormat);
        }
        return IsBlockCompressed(header.FourCc);
    }

    internal static bool HasRecognizedPixelFormat(DdsHeader header)
    {
        return (header.PixelFormatFlags & (PixelFormatFourCc | PixelFormatRgb | PixelFormatYuv | PixelFormatLuminance | PixelFormatAlpha)) != 0;
    }

    internal static bool IsBlockCompressed(string fourCc)
    {
        return fourCc == "DXT1" || fourCc == "DXT2" || fourCc == "DXT3" || fourCc == "DXT4" || fourCc == "DXT5" || fourCc == "ATI1" || fourCc == "ATI2" || fourCc == "BC4U" || fourCc == "BC4S" || fourCc == "BC5U" || fourCc == "BC5S";
    }

    internal static bool IsDxgiBlockCompressed(long format)
    {
        return (format >= 70 && format <= 84) || (format >= 94 && format <= 99);
    }

    internal static int FourCcBitDepth(string fourCc, long value)
    {
        if (fourCc == "DXT1" || fourCc == "DXT2" || fourCc == "DXT3" || fourCc == "DXT4" || fourCc == "DXT5") return 8;
        if (fourCc == "ATI1" || fourCc == "ATI2" || fourCc == "BC4U" || fourCc == "BC4S" || fourCc == "BC5U" || fourCc == "BC5S") return 8;
        if (fourCc == "YUY2" || fourCc == "UYVY" || fourCc == "RGBG" || fourCc == "GRGB") return 8;
        if (value == 36 || value == 110 || value == 111 || value == 112 || value == 113) return 16;
        if (value == 114 || value == 115 || value == 116) return 32;
        return 0;
    }

    internal static int FourCcChannelCount(string fourCc, long value)
    {
        if (fourCc == "DXT1" || fourCc == "DXT2" || fourCc == "DXT3" || fourCc == "DXT4" || fourCc == "DXT5") return 4;
        if (fourCc == "ATI1" || fourCc == "BC4U" || fourCc == "BC4S") return 1;
        if (fourCc == "ATI2" || fourCc == "BC5U" || fourCc == "BC5S") return 2;
        if (fourCc == "YUY2" || fourCc == "UYVY" || fourCc == "RGBG" || fourCc == "GRGB") return 3;
        if (value == 111 || value == 114) return 1;
        if (value == 112 || value == 115) return 2;
        if (value == 36 || value == 110 || value == 113 || value == 116) return 4;
        return 0;
    }

    internal static string PixelFormatName(DdsHeader header)
    {
        if (header.FourCc == "DX10") return "DX10 extended format";
        if ((header.PixelFormatFlags & PixelFormatFourCc) != 0) return FourCcName(header.FourCc, header.FourCcValue);
        if ((header.PixelFormatFlags & PixelFormatRgb) != 0) return header.AlphaMask == 0 ? "RGB" : "RGBA";
        if ((header.PixelFormatFlags & PixelFormatLuminance) != 0) return header.AlphaMask == 0 ? "Luminance" : "Luminance with alpha";
        if ((header.PixelFormatFlags & PixelFormatAlpha) != 0) return "Alpha";
        if ((header.PixelFormatFlags & PixelFormatYuv) != 0) return "YUV";
        return "Unknown";
    }

    internal static string FourCcName(string fourCc, long value)
    {
        switch (fourCc)
        {
            case "DXT1": return "BC1 / DXT1";
            case "DXT2": return "BC2 / DXT2 premultiplied alpha";
            case "DXT3": return "BC2 / DXT3";
            case "DXT4": return "BC3 / DXT4 premultiplied alpha";
            case "DXT5": return "BC3 / DXT5";
            case "ATI1": return "BC4 unsigned";
            case "BC4U": return "BC4 unsigned";
            case "BC4S": return "BC4 signed";
            case "ATI2": return "BC5 unsigned";
            case "BC5U": return "BC5 unsigned";
            case "BC5S": return "BC5 signed";
            case "DX10": return "DX10 extended header";
        }
        switch (value)
        {
            case 36: return "A16B16G16R16";
            case 110: return "Q16W16V16U16";
            case 111: return "R16F";
            case 112: return "G16R16F";
            case 113: return "A16B16G16R16F";
            case 114: return "R32F";
            case 115: return "G32R32F";
            case 116: return "A32B32G32R32F";
        }
        return fourCc.Length == 0 ? "None" : fourCc;
    }

    internal static string DxgiFormatName(long format)
    {
        switch (format)
        {
            case 0: return "DXGI_FORMAT_UNKNOWN";
            case 2: return "DXGI_FORMAT_R32G32B32A32_FLOAT";
            case 10: return "DXGI_FORMAT_R16G16B16A16_FLOAT";
            case 11: return "DXGI_FORMAT_R16G16B16A16_UNORM";
            case 24: return "DXGI_FORMAT_R10G10B10A2_UNORM";
            case 26: return "DXGI_FORMAT_R11G11B10_FLOAT";
            case 28: return "DXGI_FORMAT_R8G8B8A8_UNORM";
            case 29: return "DXGI_FORMAT_R8G8B8A8_UNORM_SRGB";
            case 35: return "DXGI_FORMAT_R16G16_UNORM";
            case 49: return "DXGI_FORMAT_R8G8_UNORM";
            case 54: return "DXGI_FORMAT_R16_FLOAT";
            case 56: return "DXGI_FORMAT_R16_UNORM";
            case 61: return "DXGI_FORMAT_R8_UNORM";
            case 65: return "DXGI_FORMAT_A8_UNORM";
            case 71: return "DXGI_FORMAT_BC1_UNORM";
            case 72: return "DXGI_FORMAT_BC1_UNORM_SRGB";
            case 74: return "DXGI_FORMAT_BC2_UNORM";
            case 75: return "DXGI_FORMAT_BC2_UNORM_SRGB";
            case 77: return "DXGI_FORMAT_BC3_UNORM";
            case 78: return "DXGI_FORMAT_BC3_UNORM_SRGB";
            case 80: return "DXGI_FORMAT_BC4_UNORM";
            case 81: return "DXGI_FORMAT_BC4_SNORM";
            case 83: return "DXGI_FORMAT_BC5_UNORM";
            case 84: return "DXGI_FORMAT_BC5_SNORM";
            case 85: return "DXGI_FORMAT_B5G6R5_UNORM";
            case 86: return "DXGI_FORMAT_B5G5R5A1_UNORM";
            case 87: return "DXGI_FORMAT_B8G8R8A8_UNORM";
            case 88: return "DXGI_FORMAT_B8G8R8X8_UNORM";
            case 91: return "DXGI_FORMAT_B8G8R8A8_UNORM_SRGB";
            case 93: return "DXGI_FORMAT_B8G8R8X8_UNORM_SRGB";
            case 95: return "DXGI_FORMAT_BC6H_UF16";
            case 96: return "DXGI_FORMAT_BC6H_SF16";
            case 98: return "DXGI_FORMAT_BC7_UNORM";
            case 99: return "DXGI_FORMAT_BC7_UNORM_SRGB";
        }
        return "DXGI format " + format.ToString();
    }

    internal static int DxgiBitDepth(long format)
    {
        if ((format >= 1 && format <= 8) || (format >= 15 && format <= 18) || (format >= 39 && format <= 43)) return 32;
        if ((format >= 9 && format <= 14) || (format >= 33 && format <= 38) || (format >= 53 && format <= 59) || format == 95 || format == 96) return 16;
        if (format >= 23 && format <= 25) return 10;
        if (format == 26) return 11;
        if (format == 85) return 6;
        if (format == 86) return 5;
        if ((format >= 27 && format <= 32) || (format >= 48 && format <= 52) || (format >= 60 && format <= 65) || (format >= 70 && format <= 84) || (format >= 87 && format <= 93) || (format >= 97 && format <= 99)) return 8;
        return 0;
    }

    internal static int DxgiChannelCount(long format)
    {
        if ((format >= 39 && format <= 43) || (format >= 53 && format <= 66) || (format >= 79 && format <= 81)) return 1;
        if ((format >= 15 && format <= 22) || (format >= 33 && format <= 38) || (format >= 44 && format <= 52) || (format >= 82 && format <= 84)) return 2;
        if ((format >= 5 && format <= 8) || format == 26 || format == 67 || format == 85 || format == 88 || format == 93 || (format >= 94 && format <= 96)) return 3;
        if ((format >= 1 && format <= 4) || (format >= 9 && format <= 14) || (format >= 23 && format <= 25) || (format >= 27 && format <= 32) || (format >= 70 && format <= 78) || format == 86 || format == 87 || format == 91 || (format >= 97 && format <= 99)) return 4;
        return 0;
    }

    internal static bool DxgiHasAlpha(long format)
    {
        if (format == 65) return true;
        if (format == 2 || (format >= 10 && format <= 14) || format == 24) return true;
        if (format == 28 || format == 29 || format == 71 || format == 72 || format == 74 || format == 75 || format == 77 || format == 78) return true;
        return format == 86 || format == 87 || format == 91 || format == 98 || format == 99;
    }

    internal static string ResourceDimensionName(long dimension)
    {
        if (dimension == 2) return "Texture 1D";
        if (dimension == 3) return "Texture 2D";
        if (dimension == 4) return "Texture 3D";
        return "Unknown";
    }

    internal static string AlphaModeName(long alphaMode)
    {
        if (alphaMode == 0) return "Unknown";
        if (alphaMode == 1) return "Straight";
        if (alphaMode == 2) return "Premultiplied";
        if (alphaMode == 3) return "Opaque";
        if (alphaMode == 4) return "Custom";
        return "Reserved";
    }

    internal static int CubeFaceCount(long caps2)
    {
        int count = 0;
        if ((caps2 & Caps2CubeMapPositiveX) != 0) count = count + 1;
        if ((caps2 & Caps2CubeMapNegativeX) != 0) count = count + 1;
        if ((caps2 & Caps2CubeMapPositiveY) != 0) count = count + 1;
        if ((caps2 & Caps2CubeMapNegativeY) != 0) count = count + 1;
        if ((caps2 & Caps2CubeMapPositiveZ) != 0) count = count + 1;
        if ((caps2 & Caps2CubeMapNegativeZ) != 0) count = count + 1;
        return count;
    }

    internal static int CountMaskBits(long mask)
    {
        int count = 0;
        long remaining = mask;
        while (remaining != 0)
        {
            if ((remaining & 1) != 0) count = count + 1;
            remaining = remaining >> 1;
        }
        return count;
    }

    internal static string HeaderFlagsText(long flags)
    {
        string text = "";
        text = AddFlagText(text, flags, 1, "Caps");
        text = AddFlagText(text, flags, 2, "Height");
        text = AddFlagText(text, flags, 4, "Width");
        text = AddFlagText(text, flags, HeaderFlagPitch, "Pitch");
        text = AddFlagText(text, flags, 4096, "PixelFormat");
        text = AddFlagText(text, flags, HeaderFlagMipMapCount, "MipMapCount");
        text = AddFlagText(text, flags, HeaderFlagLinearSize, "LinearSize");
        return AddFlagText(text, flags, HeaderFlagDepth, "Depth");
    }

    internal static string PixelFormatFlagsText(long flags)
    {
        string text = "";
        text = AddFlagText(text, flags, PixelFormatAlphaPixels, "AlphaPixels");
        text = AddFlagText(text, flags, PixelFormatAlpha, "Alpha");
        text = AddFlagText(text, flags, PixelFormatFourCc, "FourCC");
        text = AddFlagText(text, flags, PixelFormatRgb, "RGB");
        text = AddFlagText(text, flags, PixelFormatYuv, "YUV");
        return AddFlagText(text, flags, PixelFormatLuminance, "Luminance");
    }

    internal static string CapsText(long caps)
    {
        string text = "";
        text = AddFlagText(text, caps, CapsComplex, "Complex");
        text = AddFlagText(text, caps, CapsTexture, "Texture");
        return AddFlagText(text, caps, CapsMipMap, "MipMap");
    }

    internal static string Caps2Text(long caps)
    {
        string text = "";
        text = AddFlagText(text, caps, Caps2CubeMap, "CubeMap");
        text = AddFlagText(text, caps, Caps2CubeMapPositiveX, "+X");
        text = AddFlagText(text, caps, Caps2CubeMapNegativeX, "-X");
        text = AddFlagText(text, caps, Caps2CubeMapPositiveY, "+Y");
        text = AddFlagText(text, caps, Caps2CubeMapNegativeY, "-Y");
        text = AddFlagText(text, caps, Caps2CubeMapPositiveZ, "+Z");
        text = AddFlagText(text, caps, Caps2CubeMapNegativeZ, "-Z");
        return AddFlagText(text, caps, Caps2Volume, "Volume");
    }

    internal static string AddFlagText(string text, long flags, long flag, string name)
    {
        if ((flags & flag) == 0)
        {
            return text;
        }
        if (text.Length == 0)
        {
            return name;
        }
        return text + ", " + name;
    }

    internal static string PitchOrLinearSizeName(long flags)
    {
        if ((flags & HeaderFlagPitch) != 0) return "row pitch in bytes";
        if ((flags & HeaderFlagLinearSize) != 0) return "top-level byte length";
        return "unspecified";
    }

    internal static string Hex32(long value)
    {
        return "0x" + value.ToString("X8");
    }

    internal static string DecodeFourCc(byte[] bytes, int offset)
    {
        bool printable = true;
        int index = 0;
        while (index < 4)
        {
            if (bytes[offset + index] < 32 || bytes[offset + index] > 126)
            {
                printable = false;
            }
            index = index + 1;
        }
        if (!printable)
        {
            return "";
        }
        return Encoding.ASCII.GetString(bytes, offset, 4);
    }

    internal static bool HasMagic(byte[] bytes)
    {
        return bytes != null && bytes.Length >= 4 && bytes[0] == 68 && bytes[1] == 68 && bytes[2] == 83 && bytes[3] == 32;
    }

    internal static long ReadUnsigned32(byte[] bytes, int offset)
    {
        return (long)bytes[offset] | (long)bytes[offset + 1] << 8 | (long)bytes[offset + 2] << 16 | (long)bytes[offset + 3] << 24;
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string keyName, string name, MetadataValue value, long offset, long length, string display)
    {
        MetadataTag tag = directory.AddTagWithValue("Dds." + keyName, name, value);
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
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "dds.raw.block.limit", "DDS raw block exceeded the configured raw block limit.", blockType, offset, length);
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
