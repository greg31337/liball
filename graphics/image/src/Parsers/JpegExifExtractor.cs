using System.Collections.Generic;
using System.IO;

namespace image;

internal class JpegExifExtractor
{
    internal const int App1Marker = 225;
    internal const int ExifIdentifierLength = 6;

    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, List<JpegMarkerSegment> segments, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || segments == null)
        {
            return;
        }

        foreach (JpegMarkerSegment segment in segments)
        {
            if (segment != null && segment.Marker == App1Marker)
            {
                ExtractSegment(document, reader, segment, options);
            }
        }
    }

    internal static void ExtractSegment(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment, ImageMetadataReaderOptions options)
    {
        byte[] payload = ReadPayload(document, reader, segment);

        if (HasIdentifier(payload, "Exif\0\0"))
        {
            ExtractExif(document, segment, payload, options);
            return;
        }

        if (HasPartialIdentifier(payload, "Exif\0\0"))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.exif.identifier.truncated", "JPEG APP1 EXIF identifier was truncated.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
        }
    }

    internal static byte[] ReadPayload(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment)
    {
        if (document == null || reader == null || segment == null || segment.PayloadLength <= 0)
        {
            return new byte[0];
        }

        if (segment.PayloadLength > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.app1.payload.large", "JPEG APP1 payload was too large to buffer.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        if (!reader.SeekAbsolute(segment.PayloadOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jpeg.app1.seek", "JPEG APP1 payload could not be reached.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        byte[] payload = reader.ReadBytes((int)segment.PayloadLength);
        if (payload.Length < segment.PayloadLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.app1.truncated", "JPEG APP1 payload ended early.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
        }

        return payload;
    }

    internal static void ExtractExif(ImageMetadataDocument document, JpegMarkerSegment segment, byte[] payload, ImageMetadataReaderOptions options)
    {
        long tiffOffset = segment.PayloadOffset + ExifIdentifierLength;
        int tiffLength = payload.Length - ExifIdentifierLength;
        byte[] tiffPayload = CopyRange(payload, ExifIdentifierLength, tiffLength);
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Exif;
        profile.Family = MetadataFamily.Exif;
        profile.Key = "Exif.APP1";
        profile.Name = "EXIF";
        profile.MimeType = "image/tiff";
        profile.Offset = tiffOffset;
        profile.Length = tiffLength;

        if (tiffLength <= GetMaxProfileBytes(options))
        {
            profile.Data = tiffPayload;
        }
        else
        {
            profile.Data = new byte[0];
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.exif.profile.limit", "JPEG EXIF TIFF payload exceeded the configured profile limit.", "APP1", tiffOffset, tiffLength);
        }

        document.AddProfile(profile);
        AddExifDirectory(document, segment, tiffOffset, tiffLength, tiffPayload);
        ParseEmbeddedTiff(document, tiffPayload, tiffOffset, options);
    }

    internal static void ParseEmbeddedTiff(ImageMetadataDocument document, byte[] tiffPayload, long tiffOffset, ImageMetadataReaderOptions options)
    {
        if (!HasTiffHeader(tiffPayload))
        {
            return;
        }

        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        using (MemoryStream stream = new MemoryStream(tiffPayload))
        {
            BoundedStreamReader reader = new BoundedStreamReader(stream, stream.Length, stream.Length, diagnostics);
            ImageFormatKind kind = IsBigTiffHeader(tiffPayload) ? ImageFormatKind.BigTiff : ImageFormatKind.Tiff;
            ImageMetadataDocument embedded = TiffMetadataParser.Parse(reader, ImageFormatMapping.CreateInfo(kind), options);
            RawTiffProfileParser.RelocateDocument(embedded, tiffOffset);
            PrefixDirectoryPaths(embedded, "JPEG/APP1/EXIF");
            MergeEmbeddedMetadata(document, embedded);
        }
    }

    internal static void PrefixDirectoryPaths(ImageMetadataDocument document, string prefix)
    {
        foreach (MetadataDirectory directory in document.Directories)
        {
            PrefixDirectoryPath(directory, prefix);
        }
    }

    internal static void PrefixDirectoryPath(MetadataDirectory directory, string prefix)
    {
        directory.Path = prefix + "/" + directory.Path;
        foreach (MetadataDirectory child in directory.Children)
        {
            PrefixDirectoryPath(child, prefix);
        }
    }

    internal static void MergeEmbeddedMetadata(ImageMetadataDocument document, ImageMetadataDocument embedded)
    {
        foreach (MetadataDirectory directory in embedded.Directories)
        {
            document.AddDirectory(directory);
        }
        foreach (MetadataProfile profile in embedded.Profiles)
        {
            document.AddProfile(profile);
        }
        foreach (MetadataRawBlock block in embedded.RawBlocks)
        {
            document.AddRawBlock(block);
        }
        foreach (MetadataDiagnostic diagnostic in embedded.Diagnostics)
        {
            document.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddExifDirectory(ImageMetadataDocument document, JpegMarkerSegment segment, long tiffOffset, int tiffLength, byte[] tiffPayload)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Exif, "APP1", "EXIF");
        directory.Path = "JPEG/APP1/EXIF";
        directory.Offset = segment.MarkerOffset;
        directory.Length = segment.SegmentLength;
        directory.AddTagWithValue("Exif.APP1.Identifier", "Identifier", MetadataValue.FromString("Exif"));
        directory.AddTagWithValue("Exif.APP1.TiffPayloadOffset", "TIFF Payload Offset", MetadataValue.FromInteger(tiffOffset));
        directory.AddTagWithValue("Exif.APP1.TiffByteLength", "TIFF Byte Length", MetadataValue.FromInteger(tiffLength));
        directory.AddTagWithValue("Exif.APP1.TiffByteOrder", "TIFF Byte Order", MetadataValue.FromString(TiffByteOrderName(tiffPayload)));
        directory.AddTagWithValue("Exif.APP1.RoutedToTiffParser", "Routed To TIFF Parser", MetadataValue.FromBoolean(true));

        if (!HasTiffHeader(tiffPayload))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jpeg.exif.tiff.header.invalid", "JPEG EXIF payload did not begin with a recognized TIFF header.", "APP1", tiffOffset, tiffLength);
        }
    }

    internal static byte[] CopyRange(byte[] bytes, int offset, int count)
    {
        if (bytes == null || offset < 0 || count <= 0 || offset > bytes.Length)
        {
            return new byte[0];
        }

        int actualCount = count;
        if (actualCount > bytes.Length - offset)
        {
            actualCount = bytes.Length - offset;
        }

        byte[] copy = new byte[actualCount];
        int index = 0;
        while (index < actualCount)
        {
            copy[index] = bytes[offset + index];
            index = index + 1;
        }

        return copy;
    }

    internal static bool HasIdentifier(byte[] payload, string identifier)
    {
        if (payload == null || identifier == null)
        {
            return false;
        }

        if (payload.Length < identifier.Length)
        {
            return false;
        }

        int index = 0;
        while (index < identifier.Length)
        {
            if (payload[index] != identifier[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool HasPartialIdentifier(byte[] payload, string identifier)
    {
        if (payload == null || identifier == null)
        {
            return false;
        }

        if (payload.Length == 0 || payload.Length >= identifier.Length)
        {
            return false;
        }

        int index = 0;
        while (index < payload.Length)
        {
            if (payload[index] != identifier[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool HasTiffHeader(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
        {
            return false;
        }

        if (bytes[0] == 73 && bytes[1] == 73 && bytes[2] == 42 && bytes[3] == 0)
        {
            return true;
        }

        if (bytes[0] == 77 && bytes[1] == 77 && bytes[2] == 0 && bytes[3] == 42)
        {
            return true;
        }

        if (bytes[0] == 73 && bytes[1] == 73 && bytes[2] == 43 && bytes[3] == 0)
        {
            return true;
        }

        if (bytes[0] == 77 && bytes[1] == 77 && bytes[2] == 0 && bytes[3] == 43)
        {
            return true;
        }

        return false;
    }

    internal static bool IsBigTiffHeader(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4)
        {
            return false;
        }

        return bytes[0] == 73 && bytes[1] == 73 && bytes[2] == 43 && bytes[3] == 0
            || bytes[0] == 77 && bytes[1] == 77 && bytes[2] == 0 && bytes[3] == 43;
    }

    internal static string TiffByteOrderName(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 2)
        {
            return "Unknown";
        }

        if (bytes[0] == 73 && bytes[1] == 73)
        {
            return "LittleEndian";
        }

        if (bytes[0] == 77 && bytes[1] == 77)
        {
            return "BigEndian";
        }

        return "Unknown";
    }

    internal static long GetMaxProfileBytes(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxProfileBytes;
        }

        return options.MaxProfileBytes;
    }
}
