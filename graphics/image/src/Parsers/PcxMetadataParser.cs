namespace image;

internal class PcxHeader
{
    internal int Manufacturer;
    internal int Version;
    internal int Encoding;
    internal int BitsPerPlane;
    internal int XMinimum;
    internal int YMinimum;
    internal int XMaximum;
    internal int YMaximum;
    internal int HorizontalDpi;
    internal int VerticalDpi;
    internal byte[] HeaderPalette;
    internal int Reserved;
    internal int PlaneCount;
    internal int BytesPerLine;
    internal int PaletteType;
    internal int HorizontalScreenSize;
    internal int VerticalScreenSize;

    internal PcxHeader()
    {
        HeaderPalette = new byte[0];
    }
}

internal class PcxMetadataParser
{
    internal const int HeaderLength = 128;
    internal const int HeaderPaletteOffset = 16;
    internal const int HeaderPaletteLength = 48;
    internal const int ManufacturerValue = 10;
    internal const int RleEncoding = 1;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "pcx.reader.null", "PCX parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "pcx.header.seek", "PCX header could not be reached.", "PCX", 0, HeaderLength);
            return Finish(document, reader);
        }
        if (reader.Remaining() < HeaderLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "pcx.header.truncated", "PCX header was truncated.", "PCX", 0, HeaderLength);
            return Finish(document, reader);
        }

        byte[] bytes = reader.ReadBytes(HeaderLength);
        PcxHeader header = DecodeHeader(bytes);
        if (!ValidateHeader(document, header))
        {
            return Finish(document, reader);
        }

        AddHeaderDirectory(document, header);
        AddPaletteDirectory(document, header);
        AddFrame(document, header);
        CaptureHeader(document, options, bytes);
        ImageMetadataNormalizerCore.BuildSummary(document);
        return Finish(document, reader);
    }

    internal static PcxHeader DecodeHeader(byte[] bytes)
    {
        PcxHeader header = new PcxHeader();
        header.Manufacturer = bytes[0];
        header.Version = bytes[1];
        header.Encoding = bytes[2];
        header.BitsPerPlane = bytes[3];
        header.XMinimum = ReadUnsigned16(bytes, 4);
        header.YMinimum = ReadUnsigned16(bytes, 6);
        header.XMaximum = ReadUnsigned16(bytes, 8);
        header.YMaximum = ReadUnsigned16(bytes, 10);
        header.HorizontalDpi = ReadUnsigned16(bytes, 12);
        header.VerticalDpi = ReadUnsigned16(bytes, 14);
        header.HeaderPalette = CopyBytes(bytes, HeaderPaletteOffset, HeaderPaletteLength);
        header.Reserved = bytes[64];
        header.PlaneCount = bytes[65];
        header.BytesPerLine = ReadUnsigned16(bytes, 66);
        header.PaletteType = ReadUnsigned16(bytes, 68);
        header.HorizontalScreenSize = ReadUnsigned16(bytes, 70);
        header.VerticalScreenSize = ReadUnsigned16(bytes, 72);
        return header;
    }

    internal static bool ValidateHeader(ImageMetadataDocument document, PcxHeader header)
    {
        bool valid = true;
        if (header.Manufacturer != ManufacturerValue)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "pcx.manufacturer.invalid", "PCX manufacturer byte was not 0x0A.", "PCX", 0, 1);
            valid = false;
        }
        if (!IsVersionSupported(header.Version))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "pcx.version.invalid", "PCX version was not a recognized value.", "PCX", 1, 1);
            valid = false;
        }
        if (header.Encoding != RleEncoding)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "pcx.encoding.invalid", "PCX encoding was not the defined RLE encoding.", "PCX", 2, 1);
            valid = false;
        }
        if (!IsBitsPerPlaneSupported(header.BitsPerPlane))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "pcx.bits-per-plane.invalid", "PCX bits per plane was not 1, 2, 4, or 8.", "PCX", 3, 1);
            valid = false;
        }
        if (header.PlaneCount <= 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "pcx.plane-count.invalid", "PCX plane count was zero.", "PCX", 65, 1);
            valid = false;
        }
        if (header.XMaximum < header.XMinimum || header.YMaximum < header.YMinimum)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "pcx.bounds.invalid", "PCX maximum coordinates preceded the minimum coordinates.", "PCX", 4, 8);
            valid = false;
        }
        if (header.BytesPerLine <= 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "pcx.bytes-per-line.invalid", "PCX bytes per line was zero.", "PCX", 66, 2);
            valid = false;
        }
        if (!valid)
        {
            return false;
        }

        int width = Width(header);
        long minimumBytesPerLine = ((long)width * header.BitsPerPlane + 7) / 8;
        if (header.BytesPerLine < minimumBytesPerLine)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "pcx.bytes-per-line.short", "PCX bytes per line was too small for the declared width and plane depth.", "PCX", 66, 2);
            return false;
        }
        if ((header.BytesPerLine & 1) != 0)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "pcx.bytes-per-line.odd", "PCX bytes per line was not even as recommended by the format.", "PCX", 66, 2);
        }
        if (header.Reserved != 0)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "pcx.reserved.nonzero", "PCX reserved header byte was nonzero.", "PCX", 64, 1);
        }
        if (header.PaletteType != 0 && header.PaletteType != 1 && header.PaletteType != 2)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "pcx.palette-type.unknown", "PCX palette interpretation value was unknown.", "PCX", 68, 2);
        }
        return true;
    }

    internal static void AddHeaderDirectory(ImageMetadataDocument document, PcxHeader header)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "Header", "PCX Header");
        directory.Path = "PCX/Header";
        directory.Offset = 0;
        directory.Length = HeaderLength;
        AddTag(directory, "Manufacturer", "Manufacturer", MetadataValue.FromUnsignedInteger((ulong)header.Manufacturer), 0, 1, "ZSoft PC Paintbrush");
        AddTag(directory, "Version", "Version", MetadataValue.FromUnsignedInteger((ulong)header.Version), 1, 1, VersionName(header.Version));
        AddTag(directory, "Encoding", "Encoding", MetadataValue.FromUnsignedInteger((ulong)header.Encoding), 2, 1, "Run-length encoding");
        AddTag(directory, "BitsPerPlane", "Bits Per Plane", MetadataValue.FromInteger(header.BitsPerPlane), 3, 1, "");
        AddTag(directory, "XMinimum", "X Minimum", MetadataValue.FromInteger(header.XMinimum), 4, 2, "");
        AddTag(directory, "YMinimum", "Y Minimum", MetadataValue.FromInteger(header.YMinimum), 6, 2, "");
        AddTag(directory, "XMaximum", "X Maximum", MetadataValue.FromInteger(header.XMaximum), 8, 2, "");
        AddTag(directory, "YMaximum", "Y Maximum", MetadataValue.FromInteger(header.YMaximum), 10, 2, "");
        AddTag(directory, "Width", "Width", MetadataValue.FromInteger(Width(header)), 4, 6, "");
        AddTag(directory, "Height", "Height", MetadataValue.FromInteger(Height(header)), 6, 6, "");
        AddTag(directory, "HorizontalDpi", "Horizontal DPI", MetadataValue.FromInteger(header.HorizontalDpi), 12, 2, "");
        AddTag(directory, "VerticalDpi", "Vertical DPI", MetadataValue.FromInteger(header.VerticalDpi), 14, 2, "");
        AddTag(directory, "ResolutionUnit", "Resolution Unit", MetadataValue.FromString("pixels/inch"), 12, 4, "pixels/inch");
        AddTag(directory, "PlaneCount", "Plane Count", MetadataValue.FromInteger(header.PlaneCount), 65, 1, "");
        AddTag(directory, "TotalBitsPerPixel", "Total Bits Per Pixel", MetadataValue.FromInteger(header.BitsPerPlane * header.PlaneCount), -1, 0, "");
        AddTag(directory, "BytesPerLine", "Bytes Per Line", MetadataValue.FromInteger(header.BytesPerLine), 66, 2, "per plane");
        AddTag(directory, "DecodedRowBytes", "Decoded Row Bytes", MetadataValue.FromInteger(header.BytesPerLine * header.PlaneCount), -1, 0, "");
        AddTag(directory, "PaletteType", "Palette Type", MetadataValue.FromInteger(header.PaletteType), 68, 2, PaletteTypeName(header.PaletteType));
        AddTag(directory, "HorizontalScreenSize", "Horizontal Screen Size", MetadataValue.FromInteger(header.HorizontalScreenSize), 70, 2, "pixels");
        AddTag(directory, "VerticalScreenSize", "Vertical Screen Size", MetadataValue.FromInteger(header.VerticalScreenSize), 72, 2, "pixels");
        AddTag(directory, "ImageDataOffset", "Image Data Offset", MetadataValue.FromInteger(HeaderLength), 0, HeaderLength, "");
    }

    internal static void AddPaletteDirectory(ImageMetadataDocument document, PcxHeader header)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "Palette", "PCX Header Palette");
        directory.Path = "PCX/Palette";
        directory.Offset = HeaderPaletteOffset;
        directory.Length = HeaderPaletteLength;
        AddTag(directory, "Palette.EntryCount", "Palette Entry Count", MetadataValue.FromInteger(16), HeaderPaletteOffset, HeaderPaletteLength, "");
        AddTag(directory, "Palette.EntrySize", "Palette Entry Size", MetadataValue.FromInteger(3), HeaderPaletteOffset, HeaderPaletteLength, "RGB bytes");
        AddTag(directory, "Palette.Entries", "Palette Entries", MetadataValue.FromBytes(CopyBytes(header.HeaderPalette, 0, header.HeaderPalette.Length)), HeaderPaletteOffset, HeaderPaletteLength, "");
    }

    internal static void AddFrame(ImageMetadataDocument document, PcxHeader header)
    {
        ImageMetadataFrame frame = document.AddFrameWithDimensions(Width(header), Height(header));
        frame.BitDepth = header.BitsPerPlane;
        frame.ChannelCount = header.PlaneCount;
        frame.Compression = ImageCompressionKind.Rle;
        frame.ColorSpace = ColorSpace(header);
        frame.SourceColorType = frame.ColorSpace.ToString();
        frame.HorizontalResolution = header.HorizontalDpi;
        frame.VerticalResolution = header.VerticalDpi;
        frame.ResolutionUnit = "pixels/inch";
    }

    internal static ImageColorSpaceKind ColorSpace(PcxHeader header)
    {
        if (header.BitsPerPlane == 8 && header.PlaneCount >= 3)
        {
            return ImageColorSpaceKind.Rgb;
        }
        if (header.BitsPerPlane == 8 && header.PlaneCount == 1 && header.PaletteType == 2)
        {
            return ImageColorSpaceKind.Gray;
        }
        return ImageColorSpaceKind.Indexed;
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string keyName, string name, MetadataValue value, long offset, long length, string display)
    {
        MetadataTag tag = directory.AddTagWithValue("Pcx." + keyName, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = display;
        tag.Confidence = 100;
        return tag;
    }

    internal static void CaptureHeader(ImageMetadataDocument document, ImageMetadataReaderOptions options, byte[] bytes)
    {
        if (!CaptureRawBlocks(options))
        {
            return;
        }
        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Format, "Header", "PCX Header", 0, HeaderLength);
        if (!CaptureRawBlockData(options))
        {
            return;
        }
        if (HeaderLength > MaxRawBlockBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "pcx.raw.block.limit", "PCX header exceeded the configured raw block limit.", "Header", 0, HeaderLength);
            return;
        }
        block.Data = CopyBytes(bytes, 0, bytes.Length);
    }

    internal static int Width(PcxHeader header)
    {
        return header.XMaximum - header.XMinimum + 1;
    }

    internal static int Height(PcxHeader header)
    {
        return header.YMaximum - header.YMinimum + 1;
    }

    internal static bool IsVersionSupported(int version)
    {
        return version == 0 || version == 2 || version == 3 || version == 4 || version == 5;
    }

    internal static bool IsBitsPerPlaneSupported(int value)
    {
        return value == 1 || value == 2 || value == 4 || value == 8;
    }

    internal static string VersionName(int version)
    {
        switch (version)
        {
            case 0: return "PC Paintbrush 2.5";
            case 2: return "PC Paintbrush 2.8 with palette";
            case 3: return "PC Paintbrush 2.8 without palette";
            case 4: return "PC Paintbrush for Windows";
            case 5: return "PC Paintbrush 3.0 or later";
            default: return "Unknown";
        }
    }

    internal static string PaletteTypeName(int value)
    {
        if (value == 1) return "Color or black and white";
        if (value == 2) return "Grayscale";
        return value == 0 ? "Unspecified" : "Unknown";
    }

    internal static int ReadUnsigned16(byte[] bytes, int offset)
    {
        return bytes[offset] | bytes[offset + 1] << 8;
    }

    internal static byte[] CopyBytes(byte[] bytes, int offset, int length)
    {
        if (bytes == null || offset < 0 || length <= 0 || offset > bytes.Length)
        {
            return new byte[0];
        }
        int actualLength = length;
        if (actualLength > bytes.Length - offset)
        {
            actualLength = bytes.Length - offset;
        }
        byte[] copy = new byte[actualLength];
        int index = 0;
        while (index < actualLength)
        {
            copy[index] = bytes[offset + index];
            index = index + 1;
        }
        return copy;
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
