namespace jpeg;

internal class JpegMarkerSegmentReader
{
    internal JpegMarkerScanner Scanner = new JpegMarkerScanner(new byte[0]);
    internal int Order;
    internal int MaxMarkerPayloadBytes = JpegConstants.DefaultMaxMarkerPayloadBytes;

    internal JpegMarkerSegmentReader(byte[] data)
    {
        Scanner = new JpegMarkerScanner(data);
        Order = 0;
    }

    internal static JpegMarkerSegmentReader FromStream(System.IO.Stream stream)
    {
        JpegMarkerSegmentReader reader = new JpegMarkerSegmentReader(new byte[0]);
        reader.Scanner = JpegMarkerScanner.FromStream(stream);
        return reader;
    }

    internal JpegMarkerSegment ReadStartOfImage()
    {
        JpegScannedMarker marker = Scanner.ReadStartOfImage();
        return ReadSegmentAfterMarker(marker);
    }

    internal JpegMarkerSegment ReadNextSegment()
    {
        JpegScannedMarker marker = Scanner.ReadNextMarker();
        return ReadSegmentAfterMarker(marker);
    }

    internal JpegMarkerSegment ReadEntropyMarkerSegment()
    {
        JpegScannedMarker marker = Scanner.ReadEntropyMarker();
        return ReadSegmentAfterMarker(marker);
    }

    internal JpegMarkerSegment ReadSegmentAfterMarker(JpegScannedMarker marker)
    {
        JpegMarkerSegment segment = CreateSegment(marker);
        if (!MarkerHasSegmentLength(marker.Value))
        {
            segment.HasLength = false;
            segment.LengthOffset = -1;
            segment.PayloadOffset = -1;
            segment.SegmentLength = 0;
            segment.PayloadLength = 0;
            segment.Payload = new byte[0];
            segment.EndOffset = Scanner.Reader.AbsoluteOffset;
            return segment;
        }

        segment.HasLength = true;
        segment.LengthOffset = Scanner.Reader.AbsoluteOffset;
        int segmentLength = Scanner.Reader.ReadUInt16();
        ValidateSegmentLength(segmentLength, marker);

        int payloadLength = segmentLength - JpegConstants.MarkerSegmentLengthBytes;
        if (MaxMarkerPayloadBytes >= 0 && payloadLength > MaxMarkerPayloadBytes)
        {
            throw JpegResourceLimitException.Create("JPEG marker payload exceeds the configured maximum marker payload bytes.");
        }

        segment.SegmentLength = segmentLength;
        segment.PayloadLength = payloadLength;
        segment.PayloadOffset = Scanner.Reader.AbsoluteOffset;
        segment.Payload = Scanner.Reader.ReadBytes(payloadLength);
        segment.EndOffset = Scanner.Reader.AbsoluteOffset;
        return segment;
    }

    internal JpegMarkerSegment CreateSegment(JpegScannedMarker marker)
    {
        JpegMarkerSegment segment = new JpegMarkerSegment();
        segment.Marker = marker.Marker;
        segment.Value = marker.Value;
        segment.Code = marker.Code;
        segment.Order = Order;
        segment.MarkerOffset = marker.Offset;
        segment.LengthOffset = -1;
        segment.PayloadOffset = -1;
        segment.EndOffset = Scanner.Reader.AbsoluteOffset;
        segment.HasLength = false;
        segment.SegmentLength = 0;
        segment.PayloadLength = 0;
        segment.Payload = new byte[0];
        segment.FoundAfterEntropyData = marker.FoundAfterEntropyData;
        segment.EntropyBytesSkipped = marker.EntropyBytesSkipped;
        Order = Order + 1;
        return segment;
    }

    internal bool MarkerHasSegmentLength(int markerValue)
    {
        if (markerValue == JpegConstants.MarkerStartOfImage)
        {
            return false;
        }

        if (markerValue == JpegConstants.MarkerEndOfImage)
        {
            return false;
        }

        if (markerValue == JpegConstants.MarkerTemporary)
        {
            return false;
        }

        if (markerValue >= JpegConstants.MarkerRestart0 && markerValue <= JpegConstants.MarkerRestart7)
        {
            return false;
        }

        return true;
    }

    internal void ValidateSegmentLength(int segmentLength, JpegScannedMarker marker)
    {
        if (segmentLength < JpegConstants.MinimumMarkerSegmentLength)
        {
            throw JpegInvalidDataException.Create("JPEG marker segment length is smaller than the two-byte length field.");
        }

        if (segmentLength > JpegConstants.MaximumMarkerSegmentLength)
        {
            throw JpegInvalidDataException.Create("JPEG marker segment length exceeds the maximum marker segment length.");
        }

        int payloadLength = segmentLength - JpegConstants.MarkerSegmentLengthBytes;
        if (payloadLength > JpegConstants.MaximumMarkerPayloadLength)
        {
            throw JpegInvalidDataException.Create("JPEG marker payload length exceeds the maximum marker payload length.");
        }
    }
}
