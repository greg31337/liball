using System.Collections.Generic;

namespace image;

internal class RawTiffProfileParser
{
    internal const int RawHeaderLength = 8;
    internal const int MrwBlockHeaderLength = 8;
    internal const int OrfMagicRo = 20306;
    internal const int OrfMagicRs = 21330;
    internal const int Rw2Magic = 85;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        if (format != null && format.Kind == ImageFormatKind.Mrw)
        {
            return ParseMrw(reader, format, options);
        }

        ImageFormatInfo rawFormat = BasicMetadataParser.CopyFormat(format);
        ImageMetadataDocument document = TiffMetadataParser.Parse(reader, rawFormat, options);
        document.Format = rawFormat;
        AddProfileDirectory(document, rawFormat, 0, document.ByteLength, HeaderVariant(rawFormat == null ? ImageFormatKind.Unknown : rawFormat.Kind));
        CaptureRawBlock(document, reader, options, 0, Minimum(document.ByteLength, RawHeaderLength), "Header", "RAW TIFF Header");
        return document;
    }

    internal static ImageMetadataDocument ParseMrw(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = CreateDocument(reader, format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "raw.mrw.reader.null", "MRW parser received a null reader.");
            return document;
        }

        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "raw.mrw.header.seek", "MRW header could not be reached.", "MRW", 0, RawHeaderLength);
            CopyReaderDiagnostics(document, reader);
            return document;
        }

        byte[] header = reader.ReadBytes(RawHeaderLength);
        if (header.Length < RawHeaderLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "raw.mrw.header.truncated", "MRW header was truncated.", "MRW", 0, RawHeaderLength);
            CopyReaderDiagnostics(document, reader);
            return document;
        }

        if (!HasMrwSignature(header))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "raw.mrw.signature.invalid", "MRW signature was invalid.", "MRW", 0, 4);
            CopyReaderDiagnostics(document, reader);
            return document;
        }

        CaptureRawBlock(document, reader, options, 0, RawHeaderLength, "Header", "MRW Header");
        long declaredEnd = ReadUnsigned32BigEndian(header, 4);
        long scanEnd = declaredEnd;
        if (scanEnd > reader.AccessibleLength())
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "raw.mrw.length.outside", "MRW block area exceeded the stream length.", "MRW", 4, 4);
            scanEnd = reader.AccessibleLength();
        }

        if (scanEnd < RawHeaderLength + MrwBlockHeaderLength)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "raw.mrw.length.invalid", "MRW block area was too short to contain a block.", "MRW", 4, 4);
            CopyReaderDiagnostics(document, reader);
            return document;
        }

        long blockOffset = RawHeaderLength;
        while (blockOffset <= scanEnd - MrwBlockHeaderLength)
        {
            if (!reader.SeekAbsolute(blockOffset))
            {
                break;
            }

            byte[] blockHeader = reader.ReadBytes(MrwBlockHeaderLength);
            if (blockHeader.Length < MrwBlockHeaderLength)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "raw.mrw.block.header.truncated", "MRW block header was truncated.", "MRW", blockOffset, MrwBlockHeaderLength);
                break;
            }

            long payloadLength = ReadUnsigned32BigEndian(blockHeader, 4);
            long payloadOffset = blockOffset + MrwBlockHeaderLength;
            if (payloadLength > scanEnd - payloadOffset)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "raw.mrw.block.payload.truncated", "MRW block payload exceeded the declared block area.", "MRW", payloadOffset, payloadLength);
                break;
            }

            if (IsTtwBlock(blockHeader))
            {
                return ParseMrwTiff(document, reader, format, options, payloadOffset, payloadLength, blockOffset);
            }

            blockOffset = payloadOffset + payloadLength;
        }

        MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "raw.mrw.ttw.missing", "MRW did not contain a TTW metadata block.", "MRW", RawHeaderLength, scanEnd - RawHeaderLength);
        CopyReaderDiagnostics(document, reader);
        return document;
    }

    internal static ImageMetadataDocument ParseMrwTiff(ImageMetadataDocument wrapper, BoundedStreamReader reader, ImageFormatInfo format, ImageMetadataReaderOptions options, long payloadOffset, long payloadLength, long blockOffset)
    {
        List<MetadataDiagnostic> embeddedDiagnostics = new List<MetadataDiagnostic>();
        if (!reader.SeekAbsolute(payloadOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(wrapper.Diagnostics, "raw.mrw.ttw.seek", "MRW TTW metadata payload could not be reached.", "TTW", payloadOffset, payloadLength);
            CopyReaderDiagnostics(wrapper, reader);
            return wrapper;
        }

        long embeddedLimit = payloadLength;
        if (reader.RemainingLimit() < embeddedLimit)
        {
            embeddedLimit = reader.RemainingLimit();
        }

        BoundedStreamReader embeddedReader = new BoundedStreamReader(reader.Source, payloadLength, embeddedLimit, embeddedDiagnostics);
        ImageMetadataDocument document = TiffMetadataParser.Parse(embeddedReader, format, options);
        RelocateDocument(document, payloadOffset);
        document.ByteLength = reader.Length;
        document.Format = BasicMetadataParser.CopyFormat(format);
        CopyRawBlocks(document, wrapper);
        AddProfileDirectory(document, document.Format, payloadOffset, payloadLength, "MRW/TTW");
        CaptureRawBlock(document, reader, options, blockOffset, MrwBlockHeaderLength + payloadLength, "TTW", "MRW TIFF Metadata Block");
        CopyDocumentDiagnostics(document, wrapper);
        CopyReaderDiagnostics(document, reader);
        return document;
    }

    internal static ImageFormatInfo? ApplyExtensionHint(ImageFormatInfo? detected, string extension)
    {
        ImageFormatKind kind = KindFromExtension(extension);
        if (kind == ImageFormatKind.Unknown)
        {
            return detected;
        }

        ImageFormatKind detectedKind = detected == null ? ImageFormatKind.Unknown : detected.Kind;
        if (detectedKind == kind)
        {
            return detected;
        }

        if (detectedKind != ImageFormatKind.Unknown && detectedKind != ImageFormatKind.Tiff && detectedKind != ImageFormatKind.BigTiff)
        {
            return detected;
        }

        ImageFormatInfo hinted = ImageFormatMapping.CreateInfo(kind);
        hinted.Confidence = detectedKind == ImageFormatKind.Unknown ? 40 : 75;
        hinted.DetectedByMagic = false;
        return hinted;
    }

    internal static ImageFormatKind KindFromExtension(string extension)
    {
        ImageFormatKind kind = ImageFormatMapping.KindFromExtension(extension);
        if (ImageFormatMapping.IsRawProfile(kind))
        {
            return kind;
        }

        return ImageFormatKind.Unknown;
    }

    internal static bool IsAlternateClassicMagic(ImageFormatKind kind, bool littleEndian, ushort magic)
    {
        if (!littleEndian)
        {
            return false;
        }

        if (kind == ImageFormatKind.Orf)
        {
            return magic == OrfMagicRo || magic == OrfMagicRs;
        }

        return kind == ImageFormatKind.Rw2 && magic == Rw2Magic;
    }

    internal static string HeaderVariant(ImageFormatKind kind)
    {
        if (kind == ImageFormatKind.Orf)
        {
            return "ORF";
        }

        if (kind == ImageFormatKind.Rw2)
        {
            return "RW2";
        }

        return "TIFF";
    }

    internal static void AddProfileDirectory(ImageMetadataDocument document, ImageFormatInfo format, long tiffOffset, long tiffLength, string variant)
    {
        if (document == null)
        {
            return;
        }

        string name = format == null ? "RAW" : format.Name;
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Raw, "Profile", name + " RAW Profile");
        directory.Path = "Raw/Profile";
        directory.Offset = tiffOffset;
        directory.Length = tiffLength;
        directory.AddTagWithValue("Raw.Profile.Format", "RAW Format", MetadataValue.FromString(name));
        directory.AddTagWithValue("Raw.Profile.Container", "Container", MetadataValue.FromString(variant == "MRW/TTW" ? "MRW" : "TIFF"));
        directory.AddTagWithValue("Raw.Profile.HeaderVariant", "Header Variant", MetadataValue.FromString(variant));
        directory.AddTagWithValue("Raw.Profile.TiffOffset", "TIFF Offset", MetadataValue.FromInteger(tiffOffset));
        directory.AddTagWithValue("Raw.Profile.TiffLength", "TIFF Length", MetadataValue.FromInteger(tiffLength));
    }

    internal static ImageMetadataDocument CreateDocument(BoundedStreamReader? reader, ImageFormatInfo format)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader != null)
        {
            document.ByteLength = reader.Length;
        }

        return document;
    }

    internal static bool HasMrwSignature(byte[] bytes)
    {
        return bytes != null && bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 77 && bytes[2] == 82 && bytes[3] == 77;
    }

    internal static bool IsTtwBlock(byte[] bytes)
    {
        return bytes != null && bytes.Length >= 4 && bytes[1] == 84 && bytes[2] == 84 && bytes[3] == 87;
    }

    internal static uint ReadUnsigned32BigEndian(byte[] bytes, int offset)
    {
        if (bytes == null || offset < 0 || offset + 4 > bytes.Length)
        {
            return 0;
        }

        return ((uint)bytes[offset] << 24)
            | ((uint)bytes[offset + 1] << 16)
            | ((uint)bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }

    internal static void RelocateDocument(ImageMetadataDocument document, long offset)
    {
        if (document == null || offset == 0)
        {
            return;
        }

        foreach (MetadataDirectory directory in document.Directories)
        {
            RelocateDirectory(directory, offset);
        }

        foreach (ImageMetadataFrame frame in document.Frames)
        {
            foreach (MetadataDirectory directory in frame.Directories)
            {
                RelocateDirectory(directory, offset);
            }
        }

        foreach (MetadataProfile profile in document.Profiles)
        {
            if (profile.Offset >= 0)
            {
                profile.Offset = profile.Offset + offset;
            }
        }

        foreach (MetadataRawBlock block in document.RawBlocks)
        {
            if (block.Offset >= 0)
            {
                block.Offset = block.Offset + offset;
            }
        }

        foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
        {
            if (diagnostic.Offset >= 0)
            {
                diagnostic.Offset = diagnostic.Offset + offset;
            }
        }
    }

    internal static void RelocateDirectory(MetadataDirectory directory, long offset)
    {
        if (directory == null)
        {
            return;
        }

        if (directory.Offset >= 0)
        {
            directory.Offset = directory.Offset + offset;
        }

        foreach (MetadataTag tag in directory.Tags)
        {
            if (tag.Offset >= 0)
            {
                tag.Offset = tag.Offset + offset;
            }
        }

        foreach (MetadataDirectory child in directory.Children)
        {
            RelocateDirectory(child, offset);
        }
    }

    internal static void CaptureRawBlock(ImageMetadataDocument document, BoundedStreamReader? reader, ImageMetadataReaderOptions options, long offset, long length, string blockType, string name)
    {
        if (document == null || reader == null || !CaptureRawBlocks(options) || length <= 0)
        {
            return;
        }

        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Raw, blockType, name, offset, length);
        if (!CaptureRawBlockData(options))
        {
            return;
        }

        if (length > MaxRawBlockBytes(options) || length > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "raw.block.capture.limit", "RAW metadata block exceeded the configured raw block limit.", blockType, offset, length);
            return;
        }

        long previous = reader.Position;
        if (reader.SeekAbsolute(offset))
        {
            block.Data = reader.ReadBytes((int)length);
        }
        reader.SeekAbsolute(previous);
    }

    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions options)
    {
        return options == null || options.CaptureRawBlocks;
    }

    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions options)
    {
        return options != null && options.CaptureRawBlockData;
    }

    internal static long MaxRawBlockBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxRawBlockBytes : options.MaxRawBlockBytes;
    }

    internal static long Minimum(long left, long right)
    {
        return left < right ? left : right;
    }

    internal static void CopyDocumentDiagnostics(ImageMetadataDocument target, ImageMetadataDocument source)
    {
        if (target == null || source == null)
        {
            return;
        }

        foreach (MetadataDiagnostic diagnostic in source.Diagnostics)
        {
            target.Diagnostics.Add(diagnostic);
        }
    }

    internal static void CopyRawBlocks(ImageMetadataDocument target, ImageMetadataDocument source)
    {
        if (target == null || source == null)
        {
            return;
        }

        foreach (MetadataRawBlock block in source.RawBlocks)
        {
            target.RawBlocks.Add(block);
        }
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
