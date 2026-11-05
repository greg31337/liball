using System.Collections.Generic;

namespace jpeg;

internal class JpegMetadataContainerEditor
{
    internal byte[] EditMarkers(byte[] data, List<JpegRawMarker> markers, JpegMetadataPolicy policy)
    {
        ValidateData(data);
        ValidateMarkers(markers);
        ValidatePolicy(policy);
        JpegMarkerSegment[] segments = JpegInspector.ReadMarkers(data);
        List<JpegRawMarker> filtered = FilterReplacementMarkers(markers, policy);
        return ReplaceMetadataRanges(data, segments, filtered);
    }

    internal byte[] ApplyPolicy(byte[] data, JpegMetadataPolicy policy)
    {
        ValidateData(data);
        ValidatePolicy(policy);
        JpegMarkerSegment[] segments = JpegInspector.ReadMarkers(data);
        return FilterMetadataRanges(data, segments, policy);
    }

    internal byte[] StripMetadata(byte[] data)
    {
        return ApplyPolicy(data, JpegMetadataPolicy.StripAll);
    }

    internal List<JpegRawMarker> ReadMetadataMarkers(byte[] data)
    {
        ValidateData(data);
        JpegMarkerSegment[] segments = JpegInspector.ReadMarkers(data);
        List<JpegRawMarker> markers = new List<JpegRawMarker>();
        JpegRawMarkerCollection rawMarkers = new JpegRawMarkerCollection();
        foreach (JpegMarkerSegment segment in segments)
        {
            if (rawMarkers.ShouldPreserve(segment))
            {
                markers.Add(rawMarkers.CreateRawMarker(segment));
            }
        }
        return markers;
    }

    internal byte[] ReplaceMetadataRanges(byte[] data, JpegMarkerSegment[] segments, List<JpegRawMarker> markers)
    {
        if (segments.Length == 0 || segments[0].Value != JpegConstants.MarkerStartOfImage)
        {
            throw JpegInvalidDataException.Create("JPEG stream must start with the SOI marker.");
        }

        JpegByteWriter writer = JpegByteWriter.WithCapacity(data.Length);
        int cursor = (int)segments[0].EndOffset;
        writer.WriteBytesRange(data, 0, cursor);
        WriteReplacementMarkers(writer, markers);
        int index = 1;
        while (index < segments.Length)
        {
            JpegMarkerSegment segment = segments[index];
            if (IsMetadataSegment(segment))
            {
                int markerOffset = (int)segment.MarkerOffset;
                if (markerOffset > cursor)
                {
                    writer.WriteBytesRange(data, cursor, markerOffset - cursor);
                }
                cursor = (int)segment.EndOffset;
            }
            index = index + 1;
        }
        if (cursor < data.Length)
        {
            writer.WriteBytesRange(data, cursor, data.Length - cursor);
        }
        return writer.ToArray();
    }

    internal byte[] FilterMetadataRanges(byte[] data, JpegMarkerSegment[] segments, JpegMetadataPolicy policy)
    {
        JpegByteWriter writer = JpegByteWriter.WithCapacity(data.Length);
        JpegMetadataPolicyFilter filter = new JpegMetadataPolicyFilter();
        int cursor = 0;
        foreach (JpegMarkerSegment segment in segments)
        {
            if (IsMetadataSegment(segment) && !filter.ShouldKeepPayload(segment.Value, segment.Payload, policy))
            {
                int markerOffset = (int)segment.MarkerOffset;
                if (markerOffset > cursor)
                {
                    writer.WriteBytesRange(data, cursor, markerOffset - cursor);
                }
                cursor = (int)segment.EndOffset;
            }
        }
        if (cursor < data.Length)
        {
            writer.WriteBytesRange(data, cursor, data.Length - cursor);
        }
        return writer.ToArray();
    }

    internal List<JpegRawMarker> FilterReplacementMarkers(List<JpegRawMarker> markers, JpegMetadataPolicy policy)
    {
        List<JpegRawMarker> filtered = new List<JpegRawMarker>();
        JpegMetadataPolicyFilter filter = new JpegMetadataPolicyFilter();
        foreach (JpegRawMarker marker in markers)
        {
            if (policy == JpegMetadataPolicy.ReplaceSelected || filter.ShouldKeepRawMarker(marker, policy))
            {
                filtered.Add(marker);
            }
        }
        return filtered;
    }

    internal void WriteReplacementMarkers(JpegByteWriter writer, List<JpegRawMarker> markers)
    {
        JpegMarkerSegmentWriter markerWriter = new JpegMarkerSegmentWriter();
        JpegMetadataWriter metadataWriter = new JpegMetadataWriter();
        metadataWriter.WriteRawMarkerSegments(markerWriter, markers);
        writer.WriteBytes(markerWriter.ToArray());
    }

    internal bool IsMetadataSegment(JpegMarkerSegment segment)
    {
        return new JpegRawMarkerCollection().ShouldPreserve(segment);
    }

    internal void ValidateData(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }
    }

    internal void ValidateMarkers(List<JpegRawMarker> markers)
    {
        if (markers == null)
        {
            throw new System.ArgumentNullException("markers");
        }

        JpegRawMarkerCollection rawMarkers = new JpegRawMarkerCollection();
        JpegMarkerSegmentWriter writer = new JpegMarkerSegmentWriter();
        foreach (JpegRawMarker marker in markers)
        {
            if (marker == null)
            {
                throw new System.ArgumentException("Marker collection cannot contain null entries.", "markers");
            }
            if (!rawMarkers.IsApplicationMarker(marker.Value) && !rawMarkers.IsCommentMarker(marker.Value) && !rawMarkers.IsUnknownMarker(marker.Value))
            {
                throw JpegWriteValidationException.Create("Generic metadata editing accepts only APPn, COM, and unknown marker values.");
            }
            writer.WriteMarkerSegmentValue(marker.Value, marker.Payload);
        }
    }

    internal void ValidatePolicy(JpegMetadataPolicy policy)
    {
        if (policy != JpegMetadataPolicy.PreserveAll
            && policy != JpegMetadataPolicy.PreserveKnown
            && policy != JpegMetadataPolicy.StripAll
            && policy != JpegMetadataPolicy.StripUnsafe
            && policy != JpegMetadataPolicy.ReplaceSelected)
        {
            throw new System.ArgumentOutOfRangeException("policy");
        }
    }

    internal byte[] ReadStreamBytes(System.IO.Stream stream)
    {
        return new JpegIo().ReadStream(stream, default(System.Threading.CancellationToken));
    }

    internal void WriteStreamBytes(System.IO.Stream stream, byte[] data)
    {
        new JpegIo().WriteStream(stream, data, default(System.Threading.CancellationToken));
    }

    internal void ValidateOutputPath(string path)
    {
        Jpeg.ValidateFilePath(path);
    }
}
