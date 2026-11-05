namespace jpeg;

internal class JpegCommentParser
{
    internal string Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        return System.Text.Encoding.Latin1.GetString(payload);
    }
}

internal class JpegCommentWriter
{
    internal byte[] WritePayload(string comment)
    {
        Validate(comment);
        return System.Text.Encoding.Latin1.GetBytes(comment);
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, string comment)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        byte[] payload = WritePayload(comment);
        segmentWriter.WriteMarkerSegment(JpegMarker.Comment, payload);
    }

    internal void WriteMarkerSegments(JpegMarkerSegmentWriter segmentWriter, System.Collections.Generic.List<string> comments)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        if (comments == null)
        {
            throw new System.ArgumentNullException("comments");
        }

        foreach (string comment in comments)
        {
            WriteMarkerSegment(segmentWriter, comment);
        }
    }

    internal void Validate(string comment)
    {
        if (comment == null)
        {
            throw new System.ArgumentNullException("comment");
        }

        if (comment.Length > JpegConstants.MaximumMarkerPayloadLength)
        {
            throw JpegWriteValidationException.Create("JPEG comment payload exceeds the maximum marker payload length.");
        }

        int index = 0;
        while (index < comment.Length)
        {
            if ((int)comment[index] > 255)
            {
                throw JpegWriteValidationException.Create("JPEG comment contains a character outside Latin-1.");
            }

            index = index + 1;
        }
    }
}
