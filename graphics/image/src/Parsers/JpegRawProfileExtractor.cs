using System.Collections.Generic;

namespace image;

internal class JpegRawProfileExtractor
{
    internal const int App2Marker = 226;
    internal const int App11Marker = 235;
    internal const int App12Marker = 236;
    internal const string MpfIdentifier = "MPF\0";
    internal const string FlashPixIdentifier = "FPXR\0";
    internal const string DuckyIdentifier = "Ducky\0";
    internal const string JumbfIdentifier = "JUMBF\0";
    internal const string C2paIdentifier = "C2PA\0";

    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, List<JpegMarkerSegment> segments, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || segments == null)
        {
            return;
        }

        foreach (JpegMarkerSegment segment in segments)
        {
            if (segment != null)
            {
                ExtractSegment(document, reader, segment, options);
            }
        }
    }

    internal static void ExtractSegment(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment, ImageMetadataReaderOptions options)
    {
        if (segment.Marker != App2Marker && segment.Marker != App11Marker && segment.Marker != App12Marker)
        {
            return;
        }

        byte[] payload = ReadPayload(document, reader, segment);

        if (segment.Marker == App2Marker)
        {
            if (JpegExifExtractor.HasIdentifier(payload, MpfIdentifier))
            {
                AddRawProfile(document, segment, payload, MpfIdentifier, MetadataProfileKind.Unknown, MetadataFamily.Jpeg, "Jpeg.APP2.MPF", "MPF", options);
                return;
            }

            if (JpegExifExtractor.HasIdentifier(payload, FlashPixIdentifier))
            {
                AddRawProfile(document, segment, payload, FlashPixIdentifier, MetadataProfileKind.Unknown, MetadataFamily.Jpeg, "Jpeg.APP2.FlashPix", "FlashPix", options);
                return;
            }
        }

        if (segment.Marker == App12Marker && JpegExifExtractor.HasIdentifier(payload, DuckyIdentifier))
        {
            AddRawProfile(document, segment, payload, DuckyIdentifier, MetadataProfileKind.Unknown, MetadataFamily.Jpeg, "Jpeg.APP12.Ducky", "Ducky", options);
            return;
        }

        if (segment.Marker == App11Marker)
        {
            if (JpegExifExtractor.HasIdentifier(payload, JumbfIdentifier))
            {
                AddRawProfile(document, segment, payload, JumbfIdentifier, MetadataProfileKind.Jumbf, MetadataFamily.Container, "Jumbf.APP11", "JUMBF", options);
                return;
            }

            if (JpegExifExtractor.HasIdentifier(payload, C2paIdentifier))
            {
                AddRawProfile(document, segment, payload, C2paIdentifier, MetadataProfileKind.C2pa, MetadataFamily.Container, "C2pa.APP11", "C2PA", options);
            }
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
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.raw.profile.payload.large", "JPEG raw profile payload was too large to buffer.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        if (!reader.SeekAbsolute(segment.PayloadOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jpeg.raw.profile.seek", "JPEG raw profile payload could not be reached.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        byte[] payload = reader.ReadBytes((int)segment.PayloadLength);
        if (payload.Length < segment.PayloadLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.raw.profile.truncated", "JPEG raw profile payload ended early.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
        }

        return payload;
    }

    internal static void AddRawProfile(ImageMetadataDocument document, JpegMarkerSegment segment, byte[] payload, string identifier, MetadataProfileKind kind, MetadataFamily family, string key, string name, ImageMetadataReaderOptions options)
    {
        int dataOffset = IdentifierLength(identifier);
        int dataLength = ByteLength(payload) - dataOffset;
        byte[] data = JpegExifExtractor.CopyRange(payload, dataOffset, dataLength);
        long absoluteDataOffset = segment.PayloadOffset + dataOffset;

        MetadataProfile profile = new MetadataProfile();
        profile.Kind = kind;
        profile.Family = family;
        profile.Key = key;
        profile.Name = name;
        profile.MimeType = "application/octet-stream";
        profile.Offset = absoluteDataOffset;
        profile.Length = dataLength;

        if (dataLength <= GetMaxProfileBytes(options))
        {
            profile.Data = data;
        }
        else
        {
            profile.Data = new byte[0];
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.raw.profile.limit", "JPEG raw profile exceeded the configured profile limit.", segment.Name, absoluteDataOffset, dataLength);
        }

        document.AddProfile(profile);
        AddRawProfileDirectory(document, segment, identifier, family, key, name, absoluteDataOffset, dataLength);
    }

    internal static void AddRawProfileDirectory(ImageMetadataDocument document, JpegMarkerSegment segment, string identifier, MetadataFamily family, string key, string name, long dataOffset, int dataLength)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(family, segment.Name, name);
        directory.Path = "JPEG/" + segment.Name + "/" + name;
        directory.Offset = segment.MarkerOffset;
        directory.Length = segment.SegmentLength;
        directory.AddTagWithValue(key + ".Identifier", "Identifier", MetadataValue.FromString(TrimNullIdentifier(identifier)));
        directory.AddTagWithValue(key + ".DataOffset", "Data Offset", MetadataValue.FromInteger(dataOffset));
        directory.AddTagWithValue(key + ".DataByteLength", "Data Byte Length", MetadataValue.FromInteger(dataLength));
    }

    internal static string TrimNullIdentifier(string identifier)
    {
        if (identifier == null)
        {
            return "";
        }

        int length = identifier.Length;
        if (length > 0 && identifier[length - 1] == 0)
        {
            length = length - 1;
        }

        return identifier.Substring(0, length);
    }

    internal static int IdentifierLength(string identifier)
    {
        if (identifier == null)
        {
            return 0;
        }

        return identifier.Length;
    }

    internal static int ByteLength(byte[] bytes)
    {
        if (bytes == null)
        {
            return 0;
        }

        return bytes.Length;
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
