using System.Collections.Generic;

namespace image;

internal class JpegCommentExtractor
{
    internal const int CommentMarker = 254;

    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, List<JpegMarkerSegment> segments)
    {
        if (document == null || reader == null || segments == null)
        {
            return;
        }

        foreach (JpegMarkerSegment segment in segments)
        {
            if (segment != null && segment.Marker == CommentMarker)
            {
                ExtractSegment(document, reader, segment);
            }
        }
    }

    internal static void ExtractSegment(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment)
    {
        byte[] payload = ReadPayload(document, reader, segment);
        string text = MetadataStringReader.DecodeUtf8RangeWithLatin1Fallback(payload, 0, payload.Length, document.Diagnostics, segment.Name);
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Jpeg, "COM", "Comment");
        directory.Path = "JPEG/COM";
        directory.Offset = segment.MarkerOffset;
        directory.Length = segment.SegmentLength;
        MetadataTag comment = directory.AddTagWithValue("Jpeg.COM.Comment", "Comment", MetadataValue.FromString(text));
        comment.Offset = segment.PayloadOffset;
        comment.Length = payload.Length;
        comment.RawValue = payload;
        comment.DisplayValue = text;
        directory.AddTagWithValue("Jpeg.COM.ByteLength", "Byte Length", MetadataValue.FromInteger(payload.Length));
    }

    internal static byte[] ReadPayload(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment)
    {
        if (document == null || reader == null || segment == null || segment.PayloadLength <= 0)
        {
            return new byte[0];
        }

        if (segment.PayloadLength > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.com.payload.large", "JPEG COM payload was too large to buffer.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        if (!reader.SeekAbsolute(segment.PayloadOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jpeg.com.seek", "JPEG COM payload could not be reached.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        byte[] payload = reader.ReadBytes((int)segment.PayloadLength);
        if (payload.Length < segment.PayloadLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.com.truncated", "JPEG COM payload ended early.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
        }

        return payload;
    }
}
