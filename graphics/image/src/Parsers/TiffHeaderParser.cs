namespace image;

internal class TiffHeaderParser
{
    internal const int ClassicTiffMagic = 42;
    internal const int BigTiffMagic = 43;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);

        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "tiff.reader.null", "TIFF parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;

        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tiff.header.seek", "TIFF header could not be reached.", "TIFF", 0, 0);
            CopyReaderDiagnostics(document, reader);
            return document;
        }

        byte[] order = reader.ReadBytes(2);
        if (order.Length < 2)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tiff.header.truncated", "TIFF header byte order was truncated.", "TIFF", 0, 2);
            CopyReaderDiagnostics(document, reader);
            return document;
        }

        bool littleEndian = IsLittleEndian(order);
        bool bigEndian = IsBigEndian(order);

        if (!littleEndian && !bigEndian)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tiff.byte.order.invalid", "TIFF byte order marker was invalid.", "TIFF", 0, 2);
            CopyReaderDiagnostics(document, reader);
            return document;
        }

        EndianBinaryReader endian = new EndianBinaryReader(reader, littleEndian);
        ushort magic = endian.ReadUnsigned16();
        if (!endian.LastReadSucceeded)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tiff.magic.truncated", "TIFF magic number was truncated.", "TIFF", 2, 2);
            CopyReaderDiagnostics(document, reader);
            return document;
        }

        if (magic == ClassicTiffMagic || RawTiffProfileParser.IsAlternateClassicMagic(document.Format.Kind, littleEndian, magic))
        {
            ParseClassicHeader(document, reader, endian, littleEndian, magic, options);
        }
        else if (magic == BigTiffMagic)
        {
            ParseBigTiffHeader(document, reader, endian, littleEndian, magic, options);
        }
        else
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tiff.magic.invalid", "TIFF magic number was invalid.", "TIFF", 2, 2);
        }

        CopyReaderDiagnostics(document, reader);
        ImageMetadataNormalizerCore.BuildSummary(document);
        return document;
    }

    internal static void ParseClassicHeader(ImageMetadataDocument document, BoundedStreamReader reader, EndianBinaryReader endian, bool littleEndian, ushort magic, ImageMetadataReaderOptions options)
    {
        uint firstIfdOffset = endian.ReadUnsigned32();
        if (!endian.LastReadSucceeded)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tiff.ifd.offset.truncated", "TIFF first IFD offset was truncated.", "TIFF", 4, 4);
            return;
        }

        document.Format = ImageFormatMapping.CreateInfo(ImageFormatKind.Tiff);
        AddHeaderDirectory(document, "TIFF Header", littleEndian, magic, false, 4, 0, firstIfdOffset, 8);
        ValidateFirstIfdOffset(document, firstIfdOffset, 8);

        if (CanTraverseFirstIfdOffset(document, firstIfdOffset, 8))
        {
            TiffIfdTraverser.Traverse(document, reader, littleEndian, false, firstIfdOffset, options);
        }
    }

    internal static void ParseBigTiffHeader(ImageMetadataDocument document, BoundedStreamReader reader, EndianBinaryReader endian, bool littleEndian, ushort magic, ImageMetadataReaderOptions options)
    {
        ushort offsetSize = endian.ReadUnsigned16();
        bool offsetSizeSucceeded = endian.LastReadSucceeded;
        ushort reserved = endian.ReadUnsigned16();
        bool reservedSucceeded = endian.LastReadSucceeded;
        ulong firstIfdOffset = endian.ReadUnsigned64();

        if (!offsetSizeSucceeded || !reservedSucceeded || !endian.LastReadSucceeded)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bigtiff.header.truncated", "BigTIFF header fields were truncated.", "BigTIFF", 4, 12);
            return;
        }

        document.Format = ImageFormatMapping.CreateInfo(ImageFormatKind.BigTiff);
        AddHeaderDirectory(document, "BigTIFF Header", littleEndian, magic, true, offsetSize, reserved, firstIfdOffset, 16);

        if (offsetSize != 8)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bigtiff.offset.size.invalid", "BigTIFF offset size was not 8 bytes.", "BigTIFF", 4, 2);
        }

        if (reserved != 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bigtiff.reserved.invalid", "BigTIFF reserved field was not zero.", "BigTIFF", 6, 2);
        }

        ValidateFirstIfdOffset(document, firstIfdOffset, 16);

        if (CanTraverseFirstIfdOffset(document, firstIfdOffset, 16))
        {
            TiffIfdTraverser.Traverse(document, reader, littleEndian, true, firstIfdOffset, options);
        }
    }

    internal static void AddHeaderDirectory(ImageMetadataDocument document, string name, bool littleEndian, ushort magic, bool bigTiff, ushort offsetSize, ushort reserved, ulong firstIfdOffset, long headerLength)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Tiff, "Header", name);
        directory.Path = "TIFF/Header";
        directory.Offset = 0;
        directory.Length = headerLength;
        directory.AddTagWithValue("Tiff.Header.ByteOrder", "Byte Order", MetadataValue.FromString(ByteOrderName(littleEndian)));
        directory.AddTagWithValue("Tiff.Header.Magic", "Magic", MetadataValue.FromInteger(magic));
        directory.AddTagWithValue("Tiff.Header.IsBigTiff", "Is BigTIFF", MetadataValue.FromBoolean(bigTiff));
        directory.AddTagWithValue("Tiff.Header.OffsetSize", "Offset Size", MetadataValue.FromInteger(offsetSize));
        directory.AddTagWithValue("Tiff.Header.FirstIfdOffset", "First IFD Offset", MetadataValue.FromUnsignedInteger(firstIfdOffset));

        if (bigTiff)
        {
            directory.AddTagWithValue("Tiff.Header.Reserved", "Reserved", MetadataValue.FromInteger(reserved));
        }
    }

    internal static void ValidateFirstIfdOffset(ImageMetadataDocument document, ulong firstIfdOffset, long headerLength)
    {
        if (firstIfdOffset == 0)
        {
            return;
        }

        if (firstIfdOffset < (ulong)headerLength)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "tiff.ifd.offset.inside.header", "TIFF first IFD offset pointed inside the header.", "TIFF", ClampOffset(firstIfdOffset), headerLength);
            return;
        }

        if (firstIfdOffset > (ulong)document.ByteLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "tiff.ifd.offset.outside", "TIFF first IFD offset exceeded the stream length.", "TIFF", ClampOffset(firstIfdOffset), 0);
        }
    }

    internal static bool CanTraverseFirstIfdOffset(ImageMetadataDocument document, ulong firstIfdOffset, long headerLength)
    {
        if (document == null || firstIfdOffset == 0)
        {
            return false;
        }

        if (firstIfdOffset < (ulong)headerLength)
        {
            return false;
        }

        return firstIfdOffset < (ulong)document.ByteLength;
    }

    internal static bool IsLittleEndian(byte[] order)
    {
        return order != null && order.Length >= 2 && order[0] == 73 && order[1] == 73;
    }

    internal static bool IsBigEndian(byte[] order)
    {
        return order != null && order.Length >= 2 && order[0] == 77 && order[1] == 77;
    }

    internal static string ByteOrderName(bool littleEndian)
    {
        if (littleEndian)
        {
            return "LittleEndian";
        }

        return "BigEndian";
    }

    internal static long ClampOffset(ulong offset)
    {
        if (offset > 9223372036854775807)
        {
            return 9223372036854775807;
        }

        return (long)offset;
    }

    internal static void CopyReaderDiagnostics(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (document == null || reader == null || reader.Diagnostics == null)
        {
            return;
        }

        foreach (MetadataDiagnostic diagnostic in reader.Diagnostics)
        {
            document.Diagnostics.Add(diagnostic);
        }
    }
}
