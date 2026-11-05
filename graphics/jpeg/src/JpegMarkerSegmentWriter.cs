namespace jpeg;

internal class JpegMarkerSegmentWriter
{
    internal JpegByteWriter Writer = new JpegByteWriter();
    internal int Order;

    internal JpegMarkerSegmentWriter()
    {
        Writer = new JpegByteWriter();
        Order = 0;
    }

    internal static JpegMarkerSegmentWriter WithCapacity(int capacity)
    {
        JpegMarkerSegmentWriter writer = new JpegMarkerSegmentWriter();
        writer.Writer = JpegByteWriter.WithCapacity(capacity);
        return writer;
    }

    internal static JpegMarkerSegmentWriter FromStream(System.IO.Stream stream)
    {
        JpegMarkerSegmentWriter writer = new JpegMarkerSegmentWriter();
        writer.Writer = JpegByteWriter.FromStream(stream);
        return writer;
    }

    internal void WriteStartOfImage()
    {
        WriteStandaloneMarkerValue(JpegConstants.MarkerStartOfImage);
    }

    internal void WriteEndOfImage()
    {
        WriteStandaloneMarkerValue(JpegConstants.MarkerEndOfImage);
    }

    internal void WriteStandaloneMarker(JpegMarker marker)
    {
        WriteStandaloneMarkerValue((int)marker);
    }

    internal void WriteStandaloneMarkerValue(int markerValue)
    {
        ValidateMarkerValue(markerValue);
        if (MarkerHasSegmentLength(markerValue))
        {
            throw JpegWriteValidationException.Create("Marker requires a length-bearing segment.");
        }

        Writer.WriteUInt16(markerValue);
        Order = Order + 1;
    }

    internal void WriteMarkerSegment(JpegMarker marker, byte[] payload)
    {
        WriteMarkerSegmentValue((int)marker, payload);
    }

    internal void WriteMarkerSegmentValue(int markerValue, byte[] payload)
    {
        ValidateMarkerValue(markerValue);
        ValidatePayload(payload);
        if (!MarkerHasSegmentLength(markerValue))
        {
            throw JpegWriteValidationException.Create("Standalone marker cannot be written as a length-bearing segment.");
        }

        int segmentLength = payload.Length + JpegConstants.MarkerSegmentLengthBytes;
        Writer.WriteUInt16(markerValue);
        Writer.WriteUInt16(segmentLength);
        Writer.WriteBytes(payload);
        Order = Order + 1;
    }

    internal void WriteSegment(JpegMarkerSegment segment)
    {
        if (segment == null)
        {
            throw new System.ArgumentNullException("segment");
        }

        if (segment.HasLength)
        {
            WriteMarkerSegmentValue(segment.Value, segment.Payload);
            return;
        }

        WriteStandaloneMarkerValue(segment.Value);
    }

    internal byte[] ToArray()
    {
        return Writer.ToArray();
    }

    internal void ValidatePayload(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (payload.Length > JpegConstants.MaximumMarkerPayloadLength)
        {
            throw JpegWriteValidationException.Create("JPEG marker payload exceeds the maximum marker payload length.");
        }
    }

    internal void ValidateMarkerValue(int markerValue)
    {
        if (markerValue < 0xFF00 || markerValue > 0xFFFF)
        {
            throw JpegWriteValidationException.Create("JPEG marker value must include the 0xFF marker prefix.");
        }

        int code = markerValue & 255;
        if (code == JpegConstants.MarkerStuffedByte)
        {
            throw JpegWriteValidationException.Create("Stuffed entropy bytes are not marker values.");
        }

        if (code == JpegConstants.MarkerPrefix)
        {
            throw JpegWriteValidationException.Create("Fill bytes are not marker values.");
        }
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
}
