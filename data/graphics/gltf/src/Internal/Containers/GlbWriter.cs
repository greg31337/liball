using System;
using System.IO;

namespace gltf;

internal class GlbWriter
{
    internal byte[] WriteBytes(GltfContainerData container, bool preserveUnknownChunks)
    {
        int outputByteLength = GetOutputByteLength(container, preserveUnknownChunks);
        using (MemoryStream stream = new MemoryStream(outputByteLength))
        {
            WritePreparedStream(container, stream, preserveUnknownChunks, outputByteLength);
            return stream.ToArray();
        }
    }

    internal int WriteStream(GltfContainerData container, Stream stream, bool preserveUnknownChunks)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (!stream.CanWrite)
        {
            throw new ArgumentException("The output stream must be writable.", "stream");
        }

        int outputByteLength = GetOutputByteLength(container, preserveUnknownChunks);
        WritePreparedStream(container, stream, preserveUnknownChunks, outputByteLength);
        return outputByteLength;
    }

    internal int GetOutputByteLength(GltfContainerData container, bool preserveUnknownChunks)
    {
        ValidateContainer(container, preserveUnknownChunks);

        int jsonByteLength = GetAlignedByteLength(container.JsonBytes.Length, "JSON chunk");
        int outputByteLength = AddOutputByteLength(
            GltfConstants.GlbHeaderByteLength,
            GltfConstants.GlbChunkHeaderByteLength,
            "GLB JSON chunk header");
        outputByteLength = AddOutputByteLength(outputByteLength, jsonByteLength, "GLB JSON chunk");

        if (container.HasBinChunk)
        {
            int binByteLength = GetAlignedByteLength(container.BinBytes.Length, "BIN chunk");
            outputByteLength = AddOutputByteLength(
                outputByteLength,
                GltfConstants.GlbChunkHeaderByteLength,
                "GLB BIN chunk header");
            outputByteLength = AddOutputByteLength(outputByteLength, binByteLength, "GLB BIN chunk");
        }

        if (preserveUnknownChunks)
        {
            int index = 0;
            while (index < container.UnknownChunks.Count)
            {
                GltfGlbChunk chunk = container.UnknownChunks[index];
                outputByteLength = AddOutputByteLength(
                    outputByteLength,
                    GltfConstants.GlbChunkHeaderByteLength,
                    $"GLB unknown chunk {index} header");
                outputByteLength = AddOutputByteLength(
                    outputByteLength,
                    chunk.Data.Length,
                    $"GLB unknown chunk {index}");
                index++;
            }
        }

        ConvertByteLengthToUInt32(outputByteLength, "GLB total");
        return outputByteLength;
    }

    internal uint ConvertByteLengthToUInt32(int byteLength, string subject)
    {
        if (byteLength < 0)
        {
            throw new GltfWriteException($"{subject} byte length {byteLength} cannot be represented as an unsigned 32-bit length.");
        }

        return (uint)byteLength;
    }

    internal void WriteUInt32LittleEndian(Stream stream, uint value)
    {
        byte[] bytes = new byte[4];
        bytes[0] = (byte)(value & 0xFFU);
        bytes[1] = (byte)((value >> 8) & 0xFFU);
        bytes[2] = (byte)((value >> 16) & 0xFFU);
        bytes[3] = (byte)((value >> 24) & 0xFFU);
        stream.Write(bytes, 0, bytes.Length);
    }

    internal int GetAlignedByteLength(int byteLength, string subject)
    {
        int paddingByteLength = FourByteAlignment.GetPadding(byteLength);
        return AddOutputByteLength(byteLength, paddingByteLength, subject);
    }

    internal int AddOutputByteLength(int currentByteLength, int addedByteLength, string subject)
    {
        if (currentByteLength < 0 || addedByteLength < 0)
        {
            throw new GltfWriteException($"{subject} has a negative byte length.");
        }

        long result = (long)currentByteLength + (long)addedByteLength;
        if (result > int.MaxValue)
        {
            throw new GltfWriteException($"{subject} makes the GLB output exceed the supported signed byte length.");
        }

        return (int)result;
    }

    internal void ValidateContainer(GltfContainerData container, bool preserveUnknownChunks)
    {
        if (container == null)
        {
            throw new ArgumentNullException("container");
        }

        if (container.JsonBytes == null)
        {
            throw new GltfWriteException("The GLB JSON chunk bytes are null.");
        }

        if (container.JsonBytes.Length == 0)
        {
            throw new GltfWriteException("The GLB JSON chunk must not be empty.");
        }

        if (container.BinBytes == null)
        {
            throw new GltfWriteException("The GLB BIN chunk bytes are null.");
        }

        if (container.UnknownChunks == null)
        {
            throw new GltfWriteException("The GLB unknown chunk collection is null.");
        }

        if (!preserveUnknownChunks)
        {
            return;
        }

        int index = 0;
        while (index < container.UnknownChunks.Count)
        {
            GltfGlbChunk chunk = container.UnknownChunks[index];
            if (chunk == null)
            {
                throw new GltfWriteException($"GLB unknown chunk {index} is null.");
            }

            if (chunk.Type == GltfConstants.GlbJsonChunkType || chunk.Type == GltfConstants.GlbBinChunkType)
            {
                throw new GltfWriteException(
                    $"GLB unknown chunk {index} uses reserved chunk type 0x{chunk.Type:X8}.");
            }

            if (chunk.Data == null)
            {
                throw new GltfWriteException($"GLB unknown chunk {index} data is null.");
            }

            if (!FourByteAlignment.IsAligned(chunk.Data.Length))
            {
                throw new GltfWriteException(
                    $"GLB unknown chunk {index} byte length {chunk.Data.Length} is not aligned to 4 bytes.");
            }

            ConvertByteLengthToUInt32(chunk.Data.Length, $"GLB unknown chunk {index}");
            index++;
        }
    }

    internal void WritePreparedStream(
        GltfContainerData container,
        Stream stream,
        bool preserveUnknownChunks,
        int outputByteLength)
    {
        WriteUInt32LittleEndian(stream, GltfConstants.GlbMagic);
        WriteUInt32LittleEndian(stream, (uint)GltfConstants.GlbVersion);
        WriteUInt32LittleEndian(stream, ConvertByteLengthToUInt32(outputByteLength, "GLB total"));
        WriteChunk(stream, GltfConstants.GlbJsonChunkType, container.JsonBytes, GltfConstants.GlbJsonPaddingByte);

        if (container.HasBinChunk)
        {
            WriteChunk(stream, GltfConstants.GlbBinChunkType, container.BinBytes, GltfConstants.GlbBinPaddingByte);
        }

        if (preserveUnknownChunks)
        {
            int index = 0;
            while (index < container.UnknownChunks.Count)
            {
                GltfGlbChunk chunk = container.UnknownChunks[index];
                WriteChunk(stream, chunk.Type, chunk.Data, GltfConstants.GlbBinPaddingByte);
                index++;
            }
        }
    }

    internal void WriteChunk(Stream stream, uint chunkType, byte[] data, byte paddingByte)
    {
        int alignedByteLength = GetAlignedByteLength(data.Length, "GLB chunk");
        WriteUInt32LittleEndian(stream, ConvertByteLengthToUInt32(alignedByteLength, "GLB chunk"));
        WriteUInt32LittleEndian(stream, chunkType);
        stream.Write(data, 0, data.Length);

        int paddingByteLength = alignedByteLength - data.Length;
        int index = 0;
        while (index < paddingByteLength)
        {
            stream.WriteByte(paddingByte);
            index++;
        }
    }
}
