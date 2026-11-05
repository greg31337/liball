using System.IO;

namespace png;

internal class PngChunkDatastreamWriter
{
    internal static byte[] WriteToBytes(PngChunk[] chunks)
    {
        return WriteToBytes(chunks, new PngSecurityLimits());
    }

    internal static byte[] WriteToBytes(PngChunk[] chunks, PngSecurityLimits limits)
    {
        MemoryStream stream = new MemoryStream();
        Write(stream, chunks, limits);
        return stream.ToArray();
    }

    internal static byte[] WriteToBytesWithBailout(PngChunk[] chunks, PngSecurityLimits limits, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        MemoryStream stream = new MemoryStream();
        WriteWithBailout(stream, chunks, limits, options);
        if (options.WasEarlyBailedOut)
        {
            stream.Dispose();
            return new byte[0];
        }

        byte[] result = stream.ToArray();
        stream.Dispose();
        return result;
    }

    internal static void WriteWithBailout(Stream stream, PngChunk[] chunks, PngSecurityLimits limits, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(options);
        ValidateChunks(chunks, limits);
        if (PngEncodingBailout.RecordBytes(options, PngConstants.SignatureLength))
        {
            return;
        }

        WriteSignature(stream);
        int index = 0;
        while (index < chunks.Length)
        {
            long framedLength = chunks[index].Data.Length + 12;
            if (PngEncodingBailout.RecordBytes(options, framedLength))
            {
                return;
            }

            PngRawChunkWriter.Write(stream, chunks[index], limits);
            index += 1;
        }
    }

    internal static void Write(Stream stream, PngChunk[] chunks)
    {
        Write(stream, chunks, new PngSecurityLimits());
    }

    internal static void Write(Stream stream, PngChunk[] chunks, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ValidateChunks(chunks, limits);

        WriteSignature(stream);
        int index = 0;
        while (index < chunks.Length)
        {
            PngRawChunkWriter.Write(stream, chunks[index], limits);
            index += 1;
        }
    }

    internal static void ValidateChunks(PngChunk[] chunks, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(limits);

        if (chunks.Length == 0)
        {
            throw new InvalidDataException("PNG datastream must contain an IEND chunk.");
        }

        PngSecurityLimitEnforcer.EnforceDatastream(chunks, limits);

        int index = 0;
        while (index < chunks.Length)
        {
            ArgumentNullException.ThrowIfNull(chunks[index]);
            if (index < chunks.Length - 1 && chunks[index].Type == PngConstants.ChunkIEND)
            {
                throw new InvalidDataException("PNG datastream cannot contain chunks after IEND.");
            }

            index += 1;
        }

        if (chunks[chunks.Length - 1].Type != PngConstants.ChunkIEND)
        {
            throw new InvalidDataException("PNG datastream must end with IEND.");
        }
    }

    internal static void WriteSignature(Stream stream)
    {
        stream.WriteByte(PngConstants.SignatureByte0);
        stream.WriteByte(PngConstants.SignatureByte1);
        stream.WriteByte(PngConstants.SignatureByte2);
        stream.WriteByte(PngConstants.SignatureByte3);
        stream.WriteByte(PngConstants.SignatureByte4);
        stream.WriteByte(PngConstants.SignatureByte5);
        stream.WriteByte(PngConstants.SignatureByte6);
        stream.WriteByte(PngConstants.SignatureByte7);
    }
}
