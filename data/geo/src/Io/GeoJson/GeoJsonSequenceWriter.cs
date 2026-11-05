using System.IO;

namespace geo;

internal class GeoJsonSequenceWriter
{
    internal GeoJsonWriteOptions Options;

    internal GeoJsonSequenceWriter(GeoJsonWriteOptions options)
    {
        Options = options;
    }

    internal void WriteDocuments(GeoJsonDocument[] documents, Stream stream)
    {
        if (documents == null) throw new GeoJsonException("GeoJSON sequence document array is null.");
        if (stream == null) throw new GeoJsonException("GeoJSON sequence output stream is null.");
        if (!stream.CanWrite) throw new GeoJsonException("GeoJSON sequence output stream is not writable.");

        int index = 0;
        while (index < documents.Length)
        {
            if (documents[index] == null)
            {
                throw new GeoJsonException("GeoJSON sequence document at index " + index + " is null.");
            }
            byte[] payload = new GeoJsonFormatter(documents[index], Options).FormatUtf8Bytes();
            stream.WriteByte(GeoJsonSequenceReader.Separator);
            stream.Write(payload, 0, payload.Length);
            stream.WriteByte(GeoJsonSequenceReader.Terminator);
            index++;
        }
    }
}
