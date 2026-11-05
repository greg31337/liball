namespace image;

internal class BmpHeaderParser
{
    internal const int FileHeaderSize = 14;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);

        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "bmp.reader.null", "BMP parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmp.header.seek", "BMP file header could not be reached.", "FileHeader", 0, FileHeaderSize);
            Finish(document, reader);
            return document;
        }

        BmpFileHeader? fileHeader = ReadFileHeader(document, reader, options);
        if (fileHeader == null)
        {
            Finish(document, reader);
            return document;
        }

        BmpDibHeader? dibHeader = ReadDibHeader(document, reader, options);
        if (dibHeader != null)
        {
            ValidatePixelDataOffset(document, fileHeader, dibHeader);
            BmpMetadataExtractor.Extract(document, reader, fileHeader, dibHeader, options);
        }

        Finish(document, reader);
        return document;
    }

    internal static BmpFileHeader? ReadFileHeader(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options)
    {
        if (reader.Remaining() < FileHeaderSize)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmp.file-header.truncated", "BMP file header was truncated.", "FileHeader", 0, FileHeaderSize);
            return null;
        }

        byte[] data = reader.ReadBytes(FileHeaderSize);
        BmpFileHeader header = new BmpFileHeader();
        header.Signature = new string(new char[] { (char)data[0], (char)data[1] });
        header.FileSize = ReadUnsigned32(data, 2);
        header.Reserved1 = ReadUnsigned16(data, 6);
        header.Reserved2 = ReadUnsigned16(data, 8);
        header.PixelDataOffset = ReadUnsigned32(data, 10);
        header.Data = data;

        if (header.Signature != "BM")
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmp.signature.invalid", "BMP file signature was not BM.", "FileHeader", 0, 2);
            return null;
        }

        AddFileHeaderDirectory(document, header);
        AddRawBlock(document, options, "FileHeader", "BMP File Header", 0, data);
        ValidateFileHeader(document, header);
        return header;
    }

    internal static BmpDibHeader? ReadDibHeader(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options)
    {
        long offset = reader.Position;
        if (reader.Remaining() < 4)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmp.dib-size.truncated", "BMP DIB header size was truncated.", "DIBHeader", offset, 4);
            return null;
        }

        byte[] sizeBytes = reader.ReadBytes(4);
        uint size = ReadUnsigned32(sizeBytes, 0);
        string headerType = HeaderType(size);
        if (headerType.Length == 0)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "bmp.dib-size.unsupported", "BMP DIB header size was not a supported common version.", "DIBHeader");
            return null;
        }

        if ((long)size > MaxSegmentBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmp.dib-header.limit", "BMP DIB header exceeded the configured segment limit.", "DIBHeader", offset, size);
            return null;
        }

        long remainingSize = (long)size - 4;
        if (reader.Remaining() < remainingSize)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmp.dib-header.truncated", "BMP DIB header was truncated.", "DIBHeader", offset, size);
            return null;
        }

        byte[] data = new byte[(int)size];
        CopyRange(sizeBytes, 0, data, 0, 4);
        byte[] remainder = reader.ReadBytes((int)remainingSize);
        CopyRange(remainder, 0, data, 4, remainder.Length);

        BmpDibHeader header = DecodeDibHeader(data, headerType);
        AddDibHeaderDirectory(document, header, offset);
        AddRawBlock(document, options, "DIBHeader", header.HeaderType, offset, data);
        return header;
    }

    internal static BmpDibHeader DecodeDibHeader(byte[] data, string headerType)
    {
        BmpDibHeader header = new BmpDibHeader();
        header.HeaderSize = ReadUnsigned32(data, 0);
        header.HeaderType = headerType;
        header.Data = data;

        if (header.HeaderSize == 12)
        {
            header.Width = ReadUnsigned16(data, 4);
            header.Height = ReadUnsigned16(data, 6);
            header.Planes = ReadUnsigned16(data, 8);
            header.BitCount = ReadUnsigned16(data, 10);
            return header;
        }

        header.Width = ReadSigned32(data, 4);
        header.Height = ReadSigned32(data, 8);
        header.Planes = ReadUnsigned16(data, 12);
        header.BitCount = ReadUnsigned16(data, 14);

        if (header.HeaderSize == 16)
        {
            return header;
        }

        header.Compression = ReadUnsigned32(data, 16);
        header.ImageSize = ReadUnsigned32(data, 20);
        header.XPelsPerMeter = ReadSigned32(data, 24);
        header.YPelsPerMeter = ReadSigned32(data, 28);
        header.ColorsUsed = ReadUnsigned32(data, 32);
        header.ColorsImportant = ReadUnsigned32(data, 36);

        if (header.HeaderSize == 64)
        {
            header.Units = ReadUnsigned16(data, 40);
            header.Reserved = ReadUnsigned16(data, 42);
            header.Recording = ReadUnsigned16(data, 44);
            header.Rendering = ReadUnsigned16(data, 46);
            header.Size1 = ReadUnsigned32(data, 48);
            header.Size2 = ReadUnsigned32(data, 52);
            header.ColorEncoding = ReadUnsigned32(data, 56);
            header.Identifier = ReadUnsigned32(data, 60);
            return header;
        }

        if (header.HeaderSize >= 52)
        {
            header.RedMask = ReadUnsigned32(data, 40);
            header.GreenMask = ReadUnsigned32(data, 44);
            header.BlueMask = ReadUnsigned32(data, 48);
        }

        if (header.HeaderSize >= 56)
        {
            header.AlphaMask = ReadUnsigned32(data, 52);
        }

        if (header.HeaderSize >= 108)
        {
            header.ColorSpaceType = ReadUnsigned32(data, 56);
            header.ColorSpaceEndpoints = CopyBytes(data, 60, 36);
            header.GammaRed = ReadUnsigned32(data, 96);
            header.GammaGreen = ReadUnsigned32(data, 100);
            header.GammaBlue = ReadUnsigned32(data, 104);
        }

        if (header.HeaderSize >= 124)
        {
            header.Intent = ReadUnsigned32(data, 108);
            header.ProfileDataOffset = ReadUnsigned32(data, 112);
            header.ProfileSize = ReadUnsigned32(data, 116);
            header.Reserved = ReadUnsigned32(data, 120);
        }

        return header;
    }

    internal static string HeaderType(uint size)
    {
        switch (size)
        {
            case 12:
                return "BITMAPCOREHEADER";
            case 16:
                return "OS22XBITMAPHEADER (Short)";
            case 40:
                return "BITMAPINFOHEADER";
            case 52:
                return "BITMAPV2INFOHEADER";
            case 56:
                return "BITMAPV3INFOHEADER";
            case 64:
                return "OS22XBITMAPHEADER";
            case 108:
                return "BITMAPV4HEADER";
            case 124:
                return "BITMAPV5HEADER";
            default:
                return "";
        }
    }

    internal static void AddFileHeaderDirectory(ImageMetadataDocument document, BmpFileHeader header)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Bmp, "FileHeader", "BMP File Header");
        directory.Path = "BMP/FileHeader";
        directory.Offset = 0;
        directory.Length = FileHeaderSize;
        AddTag(directory, "Bmp.FileHeader.Signature", "Signature", MetadataValue.FromString(header.Signature), 0, 2, header.Signature);
        AddTag(directory, "Bmp.FileHeader.FileSize", "File Size", MetadataValue.FromUnsignedInteger(header.FileSize), 2, 4, "");
        AddTag(directory, "Bmp.FileHeader.Reserved1", "Reserved 1", MetadataValue.FromUnsignedInteger(header.Reserved1), 6, 2, "");
        AddTag(directory, "Bmp.FileHeader.Reserved2", "Reserved 2", MetadataValue.FromUnsignedInteger(header.Reserved2), 8, 2, "");
        AddTag(directory, "Bmp.FileHeader.PixelDataOffset", "Pixel Data Offset", MetadataValue.FromUnsignedInteger(header.PixelDataOffset), 10, 4, "");
    }

    internal static void AddDibHeaderDirectory(ImageMetadataDocument document, BmpDibHeader header, long offset)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Bmp, "DIBHeader", "BMP DIB Header");
        directory.Path = "BMP/DIBHeader";
        directory.Offset = offset;
        directory.Length = header.HeaderSize;
        AddTag(directory, "Bmp.DIBHeader.HeaderSize", "Header Size", MetadataValue.FromUnsignedInteger(header.HeaderSize), offset, 4, "");
        AddTag(directory, "Bmp.DIBHeader.HeaderType", "Header Type", MetadataValue.FromString(header.HeaderType), offset, 4, header.HeaderType);
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string display)
    {
        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = display;
        tag.Confidence = 100;
        return tag;
    }

    internal static void ValidateFileHeader(ImageMetadataDocument document, BmpFileHeader header)
    {
        if (header.Reserved1 != 0 || header.Reserved2 != 0)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "bmp.reserved.nonzero", "BMP reserved file header fields were not zero.", "FileHeader", 6, 4);
        }

        if ((ulong)header.FileSize > (ulong)document.ByteLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmp.file-size.truncated", "BMP declared file size exceeded the stream length.", "FileHeader", 2, 4);
        }
        else if ((ulong)header.FileSize < (ulong)document.ByteLength)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "bmp.file-size.mismatch", "BMP declared file size was smaller than the stream length.", "FileHeader", 2, 4);
        }

        if ((ulong)header.PixelDataOffset > (ulong)document.ByteLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmp.pixel-offset.outside", "BMP pixel data offset exceeded the stream length.", "FileHeader", 10, 4);
        }
    }

    internal static void ValidatePixelDataOffset(ImageMetadataDocument document, BmpFileHeader fileHeader, BmpDibHeader dibHeader)
    {
        ulong minimumOffset = (ulong)FileHeaderSize + dibHeader.HeaderSize;
        if ((ulong)fileHeader.PixelDataOffset < minimumOffset)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmp.pixel-offset.inside-header", "BMP pixel data offset pointed inside the headers.", "FileHeader", 10, 4);
        }
    }

    internal static void AddRawBlock(ImageMetadataDocument document, ImageMetadataReaderOptions options, string blockType, string name, long offset, byte[] data)
    {
        if (!CaptureRawBlocks(options))
        {
            return;
        }

        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Bmp, blockType, name, offset, data.Length);
        if (CaptureRawBlockData(options))
        {
            if ((long)data.Length > MaxRawBlockBytes(options))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmp.raw-block.limit", "BMP header exceeded the configured raw block limit.", blockType, offset, data.Length);
            }
            else
            {
                block.Data = CopyBytes(data, 0, data.Length);
            }
        }
    }

    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().CaptureRawBlocks;
        }

        return options.CaptureRawBlocks;
    }

    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().CaptureRawBlockData;
        }

        return options.CaptureRawBlockData;
    }

    internal static long MaxRawBlockBytes(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxRawBlockBytes;
        }

        return options.MaxRawBlockBytes;
    }

    internal static long MaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxSegmentBytes;
        }

        return options.MaxSegmentBytes;
    }

    internal static ushort ReadUnsigned16(byte[] data, int offset)
    {
        return (ushort)((uint)data[offset] | ((uint)data[offset + 1] << 8));
    }

    internal static uint ReadUnsigned32(byte[] data, int offset)
    {
        return (uint)data[offset]
            | ((uint)data[offset + 1] << 8)
            | ((uint)data[offset + 2] << 16)
            | ((uint)data[offset + 3] << 24);
    }

    internal static int ReadSigned32(byte[] data, int offset)
    {
        return IntegerBitPattern.ToSigned32(ReadUnsigned32(data, offset));
    }

    internal static byte[] CopyBytes(byte[] source, int offset, int length)
    {
        byte[] copy = new byte[length];
        CopyRange(source, offset, copy, 0, length);
        return copy;
    }

    internal static void CopyRange(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index = index + 1;
        }
    }

    internal static void Finish(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        TiffHeaderParser.CopyReaderDiagnostics(document, reader);
        ImageMetadataNormalizerCore.BuildSummary(document);
    }
}
