using System.Collections.Generic;

namespace image;

internal class JpegAdobeExtractor
{
    internal const int App14Marker = 238;
    internal const string AdobeIdentifier = "Adobe";

    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, List<JpegMarkerSegment> segments)
    {
        if (document == null || reader == null || segments == null)
        {
            return;
        }

        foreach (JpegMarkerSegment segment in segments)
        {
            if (segment != null && segment.Marker == App14Marker)
            {
                ExtractSegment(document, reader, segment);
            }
        }
    }

    internal static void ExtractSegment(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment)
    {
        byte[] payload = ReadPayload(document, reader, segment);

        if (!JpegExifExtractor.HasIdentifier(payload, AdobeIdentifier))
        {
            if (JpegExifExtractor.HasPartialIdentifier(payload, AdobeIdentifier))
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.adobe.identifier.truncated", "JPEG APP14 Adobe identifier was truncated.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            }

            return;
        }

        AddAdobeDirectory(document, segment, payload);
    }

    internal static byte[] ReadPayload(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment)
    {
        if (document == null || reader == null || segment == null || segment.PayloadLength <= 0)
        {
            return new byte[0];
        }

        if (segment.PayloadLength > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.app14.payload.large", "JPEG APP14 payload was too large to buffer.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        if (!reader.SeekAbsolute(segment.PayloadOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jpeg.app14.seek", "JPEG APP14 payload could not be reached.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        byte[] payload = reader.ReadBytes((int)segment.PayloadLength);
        if (payload.Length < segment.PayloadLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.app14.truncated", "JPEG APP14 payload ended early.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
        }

        return payload;
    }

    internal static void AddAdobeDirectory(ImageMetadataDocument document, JpegMarkerSegment segment, byte[] payload)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Jpeg, "APP14", "Adobe");
        directory.Path = "JPEG/APP14/Adobe";
        directory.Offset = segment.MarkerOffset;
        directory.Length = segment.SegmentLength;
        directory.AddTagWithValue("Jpeg.APP14.Identifier", "Identifier", MetadataValue.FromString("Adobe"));

        if (payload == null || payload.Length < 12)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.adobe.truncated", "JPEG APP14 Adobe payload was too short.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return;
        }

        int version = ReadUInt16(payload, 5);
        int flags0 = ReadUInt16(payload, 7);
        int flags1 = ReadUInt16(payload, 9);
        int colorTransform = payload[11];

        directory.AddTagWithValue("Jpeg.APP14.Version", "Version", MetadataValue.FromInteger(version));
        directory.AddTagWithValue("Jpeg.APP14.Flags0", "Flags 0", MetadataValue.FromInteger(flags0));
        directory.AddTagWithValue("Jpeg.APP14.Flags1", "Flags 1", MetadataValue.FromInteger(flags1));
        directory.AddTagWithValue("Jpeg.APP14.ColorTransform", "Color Transform", MetadataValue.FromInteger(colorTransform));
        directory.AddTagWithValue("Jpeg.APP14.ColorTransformName", "Color Transform Name", MetadataValue.FromString(ColorTransformName(colorTransform)));
    }

    internal static int ReadUInt16(byte[] bytes, int offset)
    {
        if (bytes == null || offset < 0 || offset + 1 >= bytes.Length)
        {
            return 0;
        }

        return (bytes[offset] * 256) + bytes[offset + 1];
    }

    internal static string ColorTransformName(int colorTransform)
    {
        switch (colorTransform)
        {
            case 0:
                return "Unknown or RGB/CMYK";
            case 1:
                return "YCbCr";
            case 2:
                return "YCCK";
            default:
                return "Unknown";
        }
    }
}
