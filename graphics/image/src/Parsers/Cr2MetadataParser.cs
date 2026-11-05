using System.Collections.Generic;

namespace image;

internal class Cr2MetadataParser
{
    internal const int HeaderLength = 16;
    internal const int MajorVersion = 2;
    internal const int MinorVersion = 0;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "tiff.reader.null", "CR2 parser received a null TIFF reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "cr2.header.seek", "CR2 header could not be reached.", "CR2", 0, HeaderLength);
            return Finish(document, reader);
        }

        byte[] header = reader.ReadBytes(HeaderLength);
        if (header.Length < HeaderLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "cr2.header.truncated", "CR2 header was truncated.", "CR2", 0, HeaderLength);
            return Finish(document, reader);
        }

        bool littleEndian = header[0] == 73 && header[1] == 73;
        bool bigEndian = header[0] == 77 && header[1] == 77;
        if (!littleEndian && !bigEndian)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "cr2.byte-order.invalid", "CR2 byte order marker was invalid.", "CR2", 0, 2);
            return Finish(document, reader);
        }

        ushort magic = TiffValueDecoder.ReadUnsigned16(header, 2, littleEndian);
        uint firstIfdOffset = TiffValueDecoder.ReadUnsigned32(header, 4, littleEndian);
        uint rawIfdOffset = TiffValueDecoder.ReadUnsigned32(header, 12, littleEndian);
        bool signatureValid = header[8] == 67 && header[9] == 82;
        bool versionValid = header[10] == MajorVersion && header[11] == MinorVersion;

        TiffHeaderParser.AddHeaderDirectory(document, "CR2 TIFF Header", littleEndian, magic, false, 4, 0, firstIfdOffset, HeaderLength);
        AddProfileDirectory(document, firstIfdOffset, rawIfdOffset, SignatureText(header), header[10], header[11]);
        RawTiffProfileParser.CaptureRawBlock(document, reader, options, 0, HeaderLength, "Header", "CR2 Header");

        if (magic != TiffHeaderParser.ClassicTiffMagic)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "cr2.magic.invalid", "CR2 TIFF magic number was invalid.", "CR2", 2, 2);
        }

        if (!signatureValid)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "cr2.signature.invalid", "CR2 signature was invalid.", "CR2", 8, 2);
        }

        if (!versionValid)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "cr2.version.unexpected", "CR2 header version was not 2.0.", "CR2", 10, 2);
        }

        TiffHeaderParser.ValidateFirstIfdOffset(document, firstIfdOffset, HeaderLength);
        if (magic == TiffHeaderParser.ClassicTiffMagic && TiffHeaderParser.CanTraverseFirstIfdOffset(document, firstIfdOffset, HeaderLength))
        {
            TiffIfdTraverser.Traverse(document, reader, littleEndian, false, firstIfdOffset, options);
        }

        if (magic == TiffHeaderParser.ClassicTiffMagic && signatureValid && versionValid)
        {
            TraverseRawIfd(document, reader, littleEndian, rawIfdOffset, options);
        }

        ImageMetadataNormalizerCore.BuildSummary(document);
        return Finish(document, reader);
    }

    internal static void TraverseRawIfd(ImageMetadataDocument document, BoundedStreamReader reader, bool littleEndian, uint rawIfdOffset, ImageMetadataReaderOptions options)
    {
        if (rawIfdOffset == 0)
        {
            return;
        }

        if (rawIfdOffset < HeaderLength)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "cr2.raw-ifd.offset.inside-header", "CR2 RAW IFD offset pointed inside the header.", "CR2", rawIfdOffset, 0);
            return;
        }

        if ((ulong)rawIfdOffset >= (ulong)document.ByteLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "cr2.raw-ifd.offset.outside", "CR2 RAW IFD offset exceeded the stream length.", "CR2", rawIfdOffset, 0);
            return;
        }

        if (HasDirectoryAtOffset(document, rawIfdOffset))
        {
            return;
        }

        List<ulong> visitedOffsets = ExistingDirectoryOffsets(document);
        if (visitedOffsets.Count >= TiffIfdTraverser.GetMaxIfdCount(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "cr2.raw-ifd.count.limit", "CR2 RAW IFD exceeded the configured IFD count limit.", "CR2", rawIfdOffset, visitedOffsets.Count);
            return;
        }

        visitedOffsets.Add(rawIfdOffset);
        TiffIfdTraverser.ReadIfd(document, reader, littleEndian, false, rawIfdOffset, visitedOffsets.Count - 1, options, visitedOffsets, "CR2RawIFD", "Canon RAW Image File Directory", MetadataFamily.Raw);
    }

    internal static void AddProfileDirectory(ImageMetadataDocument document, uint firstIfdOffset, uint rawIfdOffset, string signature, byte majorVersion, byte minorVersion)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Raw, "Profile", "CR2 RAW Profile");
        directory.Path = "Raw/Profile";
        directory.Offset = 0;
        directory.Length = HeaderLength;
        directory.AddTagWithValue("Raw.Profile.Format", "RAW Format", MetadataValue.FromString("CR2"));
        directory.AddTagWithValue("Raw.Profile.Container", "Container", MetadataValue.FromString("TIFF"));
        directory.AddTagWithValue("Raw.Profile.HeaderVariant", "Header Variant", MetadataValue.FromString("CR2"));
        directory.AddTagWithValue("Raw.Profile.Signature", "Signature", MetadataValue.FromString(signature));
        directory.AddTagWithValue("Raw.Profile.MajorVersion", "Major Version", MetadataValue.FromInteger(majorVersion));
        directory.AddTagWithValue("Raw.Profile.MinorVersion", "Minor Version", MetadataValue.FromInteger(minorVersion));
        directory.AddTagWithValue("Raw.Profile.FirstIfdOffset", "First IFD Offset", MetadataValue.FromUnsignedInteger(firstIfdOffset));
        directory.AddTagWithValue("Raw.Profile.RawIfdOffset", "RAW IFD Offset", MetadataValue.FromUnsignedInteger(rawIfdOffset));
    }

    internal static bool HasDirectoryAtOffset(ImageMetadataDocument document, ulong offset)
    {
        if (document == null)
        {
            return false;
        }

        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory != null && directory.Group != "Header" && directory.Group != "Profile" && directory.Offset >= 0 && (ulong)directory.Offset == offset)
            {
                return true;
            }
        }

        return false;
    }

    internal static string SignatureText(byte[] header)
    {
        if (header == null || header.Length < 10)
        {
            return "";
        }

        return ((char)header[8]).ToString() + ((char)header[9]).ToString();
    }

    internal static List<ulong> ExistingDirectoryOffsets(ImageMetadataDocument document)
    {
        List<ulong> offsets = new List<ulong>();
        if (document == null)
        {
            return offsets;
        }

        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory != null && directory.Group != "Header" && directory.Group != "Profile" && directory.Offset >= 0)
            {
                offsets.Add((ulong)directory.Offset);
            }
        }

        return offsets;
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

    internal static ImageMetadataDocument Finish(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        CopyReaderDiagnostics(document, reader);
        if (reader != null)
        {
            reader.SeekAbsolute(0);
        }
        return document;
    }
}
