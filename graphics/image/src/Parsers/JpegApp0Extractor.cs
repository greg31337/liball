using System.Globalization;
using System.Collections.Generic;

namespace image;

internal class JpegApp0Extractor
{
    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, List<JpegMarkerSegment> segments)
    {
        if (document == null || reader == null || segments == null)
        {
            return;
        }

        foreach (JpegMarkerSegment segment in segments)
        {
            if (segment != null && segment.Marker == 224)
            {
                ExtractSegment(document, reader, segment);
            }
        }
    }

    internal static void ExtractSegment(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment)
    {
        byte[] payload = ReadPayload(document, reader, segment);

        if (HasIdentifier(payload, "JFIF\0"))
        {
            ExtractJfif(document, segment, payload);
            return;
        }

        if (HasIdentifier(payload, "JFXX\0"))
        {
            ExtractJfxx(document, segment, payload);
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
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.app0.payload.large", "JPEG APP0 payload was too large to buffer.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        if (!reader.SeekAbsolute(segment.PayloadOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jpeg.app0.seek", "JPEG APP0 payload could not be reached.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        byte[] payload = reader.ReadBytes((int)segment.PayloadLength);
        if (payload.Length < segment.PayloadLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.app0.truncated", "JPEG APP0 payload ended early.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
        }

        return payload;
    }

    internal static void ExtractJfif(ImageMetadataDocument document, JpegMarkerSegment segment, byte[] payload)
    {
        MetadataDirectory directory = AddDirectory(document, segment, "JFIF");

        if (payload == null || payload.Length < 14)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.jfif.truncated", "JPEG JFIF APP0 payload was too short.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return;
        }

        int versionMajor = payload[5];
        int versionMinor = payload[6];
        int units = payload[7];
        int xDensity = ReadUInt16(payload, 8);
        int yDensity = ReadUInt16(payload, 10);
        int xThumbnail = payload[12];
        int yThumbnail = payload[13];
        int thumbnailBytes = payload.Length - 14;

        directory.AddTagWithValue("Jfif.APP0.Identifier", "Identifier", MetadataValue.FromString("JFIF"));
        directory.AddTagWithValue("Jfif.APP0.VersionMajor", "Version Major", MetadataValue.FromInteger(versionMajor));
        directory.AddTagWithValue("Jfif.APP0.VersionMinor", "Version Minor", MetadataValue.FromInteger(versionMinor));
        directory.AddTagWithValue("Jfif.APP0.Version", "Version", MetadataValue.FromString(versionMajor.ToString(CultureInfo.InvariantCulture) + "." + versionMinor.ToString("00", CultureInfo.InvariantCulture)));
        MetadataTag unitsTag = directory.AddTagWithValue("Jfif.APP0.DensityUnits", "Density Units", MetadataValue.FromInteger(units));
        unitsTag.DisplayValue = DensityUnitName(units);
        directory.AddTagWithValue("Jfif.APP0.XDensity", "X Density", MetadataValue.FromInteger(xDensity));
        directory.AddTagWithValue("Jfif.APP0.YDensity", "Y Density", MetadataValue.FromInteger(yDensity));
        directory.AddTagWithValue("Jfif.APP0.XThumbnail", "X Thumbnail", MetadataValue.FromInteger(xThumbnail));
        directory.AddTagWithValue("Jfif.APP0.YThumbnail", "Y Thumbnail", MetadataValue.FromInteger(yThumbnail));
        directory.AddTagWithValue("Jfif.APP0.ThumbnailByteLength", "Thumbnail Byte Length", MetadataValue.FromInteger(thumbnailBytes));
    }

    internal static void ExtractJfxx(ImageMetadataDocument document, JpegMarkerSegment segment, byte[] payload)
    {
        MetadataDirectory directory = AddDirectory(document, segment, "JFXX");

        if (payload == null || payload.Length < 6)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.jfxx.truncated", "JPEG JFXX APP0 payload was too short.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return;
        }

        int extensionCode = payload[5];
        int thumbnailBytes = payload.Length - 6;

        directory.AddTagWithValue("Jfif.APP0.JFXX.Identifier", "Identifier", MetadataValue.FromString("JFXX"));
        directory.AddTagWithValue("Jfif.APP0.JFXX.ExtensionCode", "Extension Code", MetadataValue.FromInteger(extensionCode));
        directory.AddTagWithValue("Jfif.APP0.JFXX.ExtensionName", "Extension Name", MetadataValue.FromString(JfxxExtensionName(extensionCode)));
        directory.AddTagWithValue("Jfif.APP0.JFXX.ThumbnailByteLength", "Thumbnail Byte Length", MetadataValue.FromInteger(thumbnailBytes));
    }

    internal static MetadataDirectory AddDirectory(ImageMetadataDocument document, JpegMarkerSegment segment, string name)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Jfif, "APP0", name);
        directory.Path = "JPEG/APP0/" + name;
        directory.Offset = segment.MarkerOffset;
        directory.Length = segment.SegmentLength;
        return directory;
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

    internal static int ReadUInt16(byte[] bytes, int offset)
    {
        if (bytes == null || offset < 0 || offset + 1 >= bytes.Length)
        {
            return 0;
        }

        return (bytes[offset] * 256) + bytes[offset + 1];
    }

    internal static string DensityUnitName(int units)
    {
        switch (units)
        {
            case 0:
                return "No units";
            case 1:
                return "Pixels per inch";
            case 2:
                return "Pixels per centimeter";
            default:
                return "Unknown";
        }
    }

    internal static string JfxxExtensionName(int extensionCode)
    {
        switch (extensionCode)
        {
            case 16:
                return "JPEG thumbnail";
            case 17:
                return "Palette thumbnail";
            case 19:
                return "RGB thumbnail";
            default:
                return "Unknown";
        }
    }
}
