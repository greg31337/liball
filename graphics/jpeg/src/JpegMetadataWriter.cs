using System.Collections.Generic;

namespace jpeg;

internal class JpegMetadataWriter
{
    internal const int CategoryJfif = 1;
    internal const int CategoryJfxx = 2;
    internal const int CategoryIcc = 3;
    internal const int CategoryExif = 4;
    internal const int CategoryXmp = 5;
    internal const int CategoryAdobe = 6;
    internal const int CategoryComment = 7;
    internal const int CategoryUnknown = 8;

    internal void WriteMarkerSegments(JpegMarkerSegmentWriter segmentWriter, JpegMetadata metadata)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        if (metadata.RawMarkersAreExplicit || metadata.RawMarkers.Count > 0)
        {
            WriteRawMarkerSegments(segmentWriter, metadata.RawMarkers);
            return;
        }

        WriteStructuredMarkerSegments(segmentWriter, metadata);
    }

    internal void WriteMarkerSegmentsWithPolicy(JpegMarkerSegmentWriter segmentWriter, JpegMetadata metadata, JpegMetadataPolicy policy)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        JpegMetadataPolicyFilter filter = new JpegMetadataPolicyFilter();
        JpegMetadata filtered = filter.CreateMetadataForPolicy(metadata, policy);
        WriteMarkerSegments(segmentWriter, filtered);
    }

    internal void WriteRawMarkerSegments(JpegMarkerSegmentWriter segmentWriter, List<JpegRawMarker> markers)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        if (markers == null)
        {
            throw new System.ArgumentNullException("markers");
        }

        foreach (JpegRawMarker marker in markers)
        {
            WriteRawMarker(segmentWriter, marker);
        }
    }

    internal void WriteRawMarkersInCategory(JpegMarkerSegmentWriter segmentWriter, List<JpegRawMarker> markers, int category)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        if (markers == null)
        {
            throw new System.ArgumentNullException("markers");
        }

        foreach (JpegRawMarker marker in markers)
        {
            if (GetRawMarkerCategory(marker) == category)
            {
                WriteRawMarker(segmentWriter, marker);
            }
        }
    }

    internal void WriteRawMarker(JpegMarkerSegmentWriter segmentWriter, JpegRawMarker marker)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        if (marker == null)
        {
            throw new System.ArgumentNullException("marker");
        }

        segmentWriter.WriteMarkerSegmentValue(marker.Value, marker.Payload);
    }

    internal int GetRawMarkerCategory(JpegRawMarker marker)
    {
        if (marker == null)
        {
            throw new System.ArgumentNullException("marker");
        }

        return GetPayloadCategory(marker.Value, marker.Payload);
    }

    internal int GetPayloadCategory(int markerValue, byte[] payload)
    {
        if (markerValue == JpegConstants.MarkerApp0)
        {
            JpegJfifMetadataParser jfifParser = new JpegJfifMetadataParser();
            if (jfifParser.IsJfif(payload))
            {
                return CategoryJfif;
            }

            JpegJfxxMetadataParser jfxxParser = new JpegJfxxMetadataParser();
            if (jfxxParser.IsJfxx(payload))
            {
                return CategoryJfxx;
            }
        }

        if (markerValue == JpegConstants.MarkerApp2)
        {
            JpegIccProfileChunkParser iccParser = new JpegIccProfileChunkParser();
            if (iccParser.IsIccProfile(payload))
            {
                return CategoryIcc;
            }
        }

        if (markerValue == JpegConstants.MarkerApp1)
        {
            JpegExifMetadataParser exifParser = new JpegExifMetadataParser();
            if (exifParser.IsExif(payload))
            {
                return CategoryExif;
            }

            if (IsXmpPayload(payload))
            {
                return CategoryXmp;
            }
        }

        if (markerValue == JpegConstants.MarkerApp14)
        {
            JpegAdobeMetadataParser adobeParser = new JpegAdobeMetadataParser();
            if (adobeParser.IsAdobe(payload))
            {
                return CategoryAdobe;
            }
        }

        if (markerValue == JpegConstants.MarkerComment)
        {
            return CategoryComment;
        }

        return CategoryUnknown;
    }

    internal bool IsXmpPayload(byte[] payload)
    {
        JpegXmpMetadataParser xmpParser = new JpegXmpMetadataParser();
        if (xmpParser.IsXmp(payload))
        {
            return true;
        }

        JpegExtendedXmpChunkParser extendedParser = new JpegExtendedXmpChunkParser();
        if (extendedParser.IsExtendedXmp(payload))
        {
            return true;
        }

        return false;
    }

    internal void WriteStructuredMarkerSegments(JpegMarkerSegmentWriter segmentWriter, JpegMetadata metadata)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        WriteStructuredJfif(segmentWriter, metadata);
        WriteStructuredJfxx(segmentWriter, metadata);
        WriteStructuredIcc(segmentWriter, metadata);
        WriteStructuredMpf(segmentWriter, metadata);
        WriteStructuredExif(segmentWriter, metadata);
        WriteStructuredXmp(segmentWriter, metadata);
        WriteStructuredPhotoshop(segmentWriter, metadata);
        WriteStructuredAdobe(segmentWriter, metadata);
        WriteStructuredComments(segmentWriter, metadata);
        WriteMetadataSegments(segmentWriter, metadata);
    }

    internal void WriteStructuredJfif(JpegMarkerSegmentWriter segmentWriter, JpegMetadata metadata)
    {
        if (metadata.HasJfif)
        {
            JpegJfifMetadataWriter writer = new JpegJfifMetadataWriter();
            writer.WriteMarkerSegment(segmentWriter, metadata.Jfif);
        }
    }

    internal void WriteStructuredJfxx(JpegMarkerSegmentWriter segmentWriter, JpegMetadata metadata)
    {
        if (!metadata.HasJfxx)
        {
            return;
        }

        JpegJfxxMetadataWriter writer = new JpegJfxxMetadataWriter();
        foreach (JpegJfxxMetadata jfxx in metadata.Jfxx)
        {
            writer.WriteMarkerSegment(segmentWriter, jfxx);
        }
    }

    internal void WriteStructuredIcc(JpegMarkerSegmentWriter segmentWriter, JpegMetadata metadata)
    {
        JpegIccProfileChunkWriter writer = new JpegIccProfileChunkWriter();
        if (metadata.HasIccProfile)
        {
            writer.WriteMarkerSegments(segmentWriter, metadata.IccProfile);
            return;
        }

        if (!metadata.HasIccProfileChunks)
        {
            return;
        }

        foreach (JpegIccProfileChunk chunk in metadata.IccProfileChunks)
        {
            segmentWriter.WriteMarkerSegment(JpegMarker.App2, writer.WritePayload(chunk));
        }
    }

    internal void WriteStructuredExif(JpegMarkerSegmentWriter segmentWriter, JpegMetadata metadata)
    {
        if (metadata.HasExif)
        {
            JpegExifMetadataWriter writer = new JpegExifMetadataWriter();
            writer.WriteMarkerSegment(segmentWriter, metadata.ExifMetadata);
        }
    }

    internal void WriteStructuredMpf(JpegMarkerSegmentWriter segmentWriter, JpegMetadata metadata)
    {
        if (metadata.HasMpf)
        {
            JpegMpfMetadataWriter writer = new JpegMpfMetadataWriter();
            writer.WriteMarkerSegment(segmentWriter, metadata.MpfMetadata);
        }
    }

    internal void WriteStructuredXmp(JpegMarkerSegmentWriter segmentWriter, JpegMetadata metadata)
    {
        if (metadata.HasXmp)
        {
            JpegXmpMetadataWriter writer = new JpegXmpMetadataWriter();
            writer.WriteMarkerSegment(segmentWriter, metadata.XmpMetadata);
        }

        if (!metadata.HasExtendedXmpChunks)
        {
            return;
        }

        JpegExtendedXmpChunkWriter extendedWriter = new JpegExtendedXmpChunkWriter();
        foreach (JpegExtendedXmpChunk chunk in metadata.ExtendedXmpChunks)
        {
            segmentWriter.WriteMarkerSegment(JpegMarker.App1, extendedWriter.WritePayload(chunk));
        }
    }

    internal void WriteStructuredAdobe(JpegMarkerSegmentWriter segmentWriter, JpegMetadata metadata)
    {
        if (metadata.HasAdobe)
        {
            JpegAdobeMetadataWriter writer = new JpegAdobeMetadataWriter();
            writer.WriteMarkerSegment(segmentWriter, metadata.Adobe);
        }
    }

    internal void WriteStructuredPhotoshop(JpegMarkerSegmentWriter segmentWriter, JpegMetadata metadata)
    {
        if (metadata.HasPhotoshop)
        {
            JpegPhotoshopMetadataWriter writer = new JpegPhotoshopMetadataWriter();
            writer.WriteMarkerSegment(segmentWriter, metadata.Photoshop);
        }
    }

    internal void WriteStructuredComments(JpegMarkerSegmentWriter segmentWriter, JpegMetadata metadata)
    {
        JpegCommentWriter writer = new JpegCommentWriter();
        writer.WriteMarkerSegments(segmentWriter, metadata.Comments);
    }

    internal void WriteMetadataSegments(JpegMarkerSegmentWriter segmentWriter, JpegMetadata metadata)
    {
        foreach (JpegMetadataSegment segment in metadata.Segments)
        {
            if (segment.Data == null)
            {
                throw JpegWriteValidationException.Create("Metadata segment payload cannot be null.");
            }

            segmentWriter.WriteMarkerSegment(segment.Marker, segment.Data);
        }
    }
}
