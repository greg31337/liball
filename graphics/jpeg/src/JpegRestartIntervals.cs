namespace jpeg;

internal class JpegRestartIntervalParser
{
    internal int Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (payload.Length != 2)
        {
            throw JpegInvalidDataException.Create("DRI payload must contain exactly one two-byte restart interval.");
        }

        return JpegByteReader.ReadUInt16(payload, 0);
    }
}

internal class JpegRestartIntervalWriter
{
    internal byte[] WritePayload(int restartInterval)
    {
        ValidateInterval(restartInterval);
        JpegByteWriter writer = JpegByteWriter.WithCapacity(2);
        writer.WriteUInt16(restartInterval);
        return writer.ToArray();
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, int restartInterval)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        byte[] payload = WritePayload(restartInterval);
        segmentWriter.WriteMarkerSegment(JpegMarker.DefineRestartInterval, payload);
    }

    internal void ValidateInterval(int restartInterval)
    {
        if (restartInterval < 0 || restartInterval > 65535)
        {
            throw JpegWriteValidationException.Create("DRI restart interval must fit in two bytes.");
        }
    }
}
