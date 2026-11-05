namespace jpeg;

internal class JpegNumberOfLinesParser
{
    internal int Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (payload.Length != 2)
        {
            throw JpegInvalidDataException.Create("DNL payload must contain exactly one two-byte line count.");
        }

        int lineCount = JpegByteReader.ReadUInt16(payload, 0);
        if (lineCount == 0)
        {
            throw JpegInvalidDataException.Create("DNL line count must be greater than zero.");
        }

        return lineCount;
    }
}
