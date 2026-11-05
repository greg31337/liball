using System.Collections.Generic;

namespace image;

internal class JpegPhotoshopExtractor
{
    internal const int App13Marker = 237;
    internal const int IptcResourceId = 1028;
    internal const string PhotoshopIdentifier = "Photoshop 3.0\0";

    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, List<JpegMarkerSegment> segments, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || segments == null)
        {
            return;
        }

        foreach (JpegMarkerSegment segment in segments)
        {
            if (segment != null && segment.Marker == App13Marker)
            {
                ExtractSegment(document, reader, segment, options);
            }
        }
    }

    internal static void ExtractSegment(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment, ImageMetadataReaderOptions options)
    {
        byte[] payload = ReadPayload(document, reader, segment);

        if (!JpegExifExtractor.HasIdentifier(payload, PhotoshopIdentifier))
        {
            if (JpegExifExtractor.HasPartialIdentifier(payload, PhotoshopIdentifier))
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.photoshop.identifier.truncated", "JPEG APP13 Photoshop identifier was truncated.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            }

            return;
        }

        AddPhotoshopProfile(document, segment, payload, options);
        long resourcesOffset = segment.PayloadOffset + PhotoshopIdentifier.Length;
        long resourcesLength = payload.Length - PhotoshopIdentifier.Length;
        List<PhotoshopImageResource> resources = PhotoshopImageResourceParser.Parse(document, reader, resourcesOffset, resourcesLength, options, "jpeg.photoshop.irb", segment.Name);
        AddApp13Directory(document, segment, resources);
        ExtractIptcResources(document, resources, options);
    }

    internal static byte[] ReadPayload(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment)
    {
        if (document == null || reader == null || segment == null || segment.PayloadLength <= 0)
        {
            return new byte[0];
        }

        if (segment.PayloadLength > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.app13.payload.large", "JPEG APP13 payload was too large to buffer.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        if (!reader.SeekAbsolute(segment.PayloadOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jpeg.app13.seek", "JPEG APP13 payload could not be reached.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        byte[] payload = reader.ReadBytes((int)segment.PayloadLength);
        if (payload.Length < segment.PayloadLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.app13.truncated", "JPEG APP13 payload ended early.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
        }

        return payload;
    }

    internal static void AddPhotoshopProfile(ImageMetadataDocument document, JpegMarkerSegment segment, byte[] payload, ImageMetadataReaderOptions options)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.PhotoshopImageResource;
        profile.Family = MetadataFamily.Photoshop;
        profile.Key = "Photoshop.APP13";
        profile.Name = "Photoshop Image Resources";
        profile.MimeType = "application/octet-stream";
        profile.Offset = segment.PayloadOffset;
        profile.Length = ByteLength(payload);

        if (ByteLength(payload) <= GetMaxProfileBytes(options))
        {
            profile.Data = payload;
        }
        else
        {
            profile.Data = new byte[0];
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.photoshop.profile.limit", "JPEG Photoshop IRB payload exceeded the configured profile limit.", segment.Name, segment.PayloadOffset, ByteLength(payload));
        }

        document.AddProfile(profile);
    }

    internal static void AddApp13Directory(ImageMetadataDocument document, JpegMarkerSegment segment, List<PhotoshopImageResource> resources)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Photoshop, "APP13", "Photoshop Image Resources");
        directory.Path = "JPEG/APP13/Photoshop";
        directory.Offset = segment.MarkerOffset;
        directory.Length = segment.SegmentLength;
        directory.AddTagWithValue("Photoshop.APP13.Identifier", "Identifier", MetadataValue.FromString("Photoshop 3.0"));
        directory.AddTagWithValue("Photoshop.APP13.ResourceCount", "Resource Count", MetadataValue.FromInteger(ResourceCount(resources)));

        foreach (PhotoshopImageResource resource in resources)
        {
            AddResourceDirectory(document, resource);
        }
    }

    internal static void AddResourceDirectory(ImageMetadataDocument document, PhotoshopImageResource resource)
    {
        if (resource == null)
        {
            return;
        }

        PhotoshopImageResourceParser.AddDirectory(document, null, resource, "JPEG/APP13/Photoshop/" + resource.ResourceId.ToString());
    }

    internal static void ExtractIptcResources(ImageMetadataDocument document, List<PhotoshopImageResource> resources, ImageMetadataReaderOptions options)
    {
        if (document == null || resources == null)
        {
            return;
        }

        foreach (PhotoshopImageResource resource in resources)
        {
            if (resource != null && resource.ResourceId == IptcResourceId)
            {
                AddIptcProfile(document, resource, options);
                IptcIimParser.Parse(document, resource.Data, resource.DataOffset, "JPEG/APP13/IPTC", "jpeg.iptc", "APP13", options);
            }
        }
    }

    internal static void AddIptcProfile(ImageMetadataDocument document, PhotoshopImageResource resource, ImageMetadataReaderOptions options)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Iptc;
        profile.Family = MetadataFamily.Iptc;
        profile.Key = "Iptc.APP13";
        profile.Name = "IPTC IIM";
        profile.MimeType = "application/octet-stream";
        profile.Offset = resource.DataOffset;
        profile.Length = ByteLength(resource.Data);

        if (ByteLength(resource.Data) <= GetMaxProfileBytes(options))
        {
            profile.Data = resource.Data;
        }
        else
        {
            profile.Data = new byte[0];
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.iptc.profile.limit", "JPEG IPTC data exceeded the configured profile limit.", "APP13", resource.DataOffset, ByteLength(resource.Data));
        }

        document.AddProfile(profile);
    }

    internal static int ResourceCount(List<PhotoshopImageResource> resources)
    {
        if (resources == null)
        {
            return 0;
        }

        return resources.Count;
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
