using System;

namespace gltf;

internal class GlbReader
{
    internal GlbHeader ReadHeader(ReadOnlySpan<byte> bytes, int maximumInputByteLength)
    {
        if (maximumInputByteLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumInputByteLength");
        }

        if (bytes.Length > maximumInputByteLength)
        {
            throw new GltfParseException(
                $"GLB input length {bytes.Length} exceeds the configured maximum {maximumInputByteLength} at byte {maximumInputByteLength}.");
        }

        if (bytes.Length < GltfConstants.GlbHeaderByteLength)
        {
            throw new GltfParseException(
                $"GLB header is truncated at byte {bytes.Length}; {GltfConstants.GlbHeaderByteLength} bytes are required.");
        }

        uint magic = ReadUInt32LittleEndian(bytes, 0);
        if (magic != GltfConstants.GlbMagic)
        {
            throw new GltfParseException("Invalid GLB magic at byte 0.");
        }

        uint version = ReadUInt32LittleEndian(bytes, 4);
        if (version != (uint)GltfConstants.GlbVersion)
        {
            throw new GltfParseException(
                $"Unsupported GLB version {version} at byte 4; expected version {GltfConstants.GlbVersion}.");
        }

        uint unsignedDeclaredByteLength = ReadUInt32LittleEndian(bytes, 8);
        if (unsignedDeclaredByteLength > (uint)int.MaxValue)
        {
            throw new GltfParseException(
                $"GLB declared length {unsignedDeclaredByteLength} at byte 8 exceeds the supported signed length.");
        }

        int declaredByteLength = (int)unsignedDeclaredByteLength;
        if (declaredByteLength < GltfConstants.MinimumGlbByteLength)
        {
            throw new GltfParseException(
                $"GLB declared length {declaredByteLength} at byte 8 is smaller than the minimum {GltfConstants.MinimumGlbByteLength}.");
        }

        if (declaredByteLength > maximumInputByteLength)
        {
            throw new GltfParseException(
                $"GLB declared length {declaredByteLength} at byte 8 exceeds the configured maximum {maximumInputByteLength}.");
        }

        if (bytes.Length < declaredByteLength)
        {
            throw new GltfParseException(
                $"GLB is truncated at byte {bytes.Length}; declared length is {declaredByteLength}.");
        }

        GlbHeader header = new GlbHeader();
        header.Version = (int)version;
        header.DeclaredByteLength = declaredByteLength;
        return header;
    }

    internal GltfContainerData ReadChunks(ReadOnlySpan<byte> bytes, GlbHeader header, bool preserveUnknownChunks)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (header.DeclaredByteLength < GltfConstants.GlbHeaderByteLength
            || header.DeclaredByteLength > bytes.Length)
        {
            throw new ArgumentOutOfRangeException("header");
        }

        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Glb;
        container.OriginalByteLength = bytes.Length;

        bool hasJsonChunk = false;
        bool hasBinChunk = false;
        int chunkIndex = 0;
        int offset = GltfConstants.GlbHeaderByteLength;
        while (offset < header.DeclaredByteLength)
        {
            int remainingByteLength = header.DeclaredByteLength - offset;
            if (remainingByteLength < GltfConstants.GlbChunkHeaderByteLength)
            {
                throw new GltfParseException(
                    $"GLB chunk header is truncated at byte {offset}; {remainingByteLength} bytes remain.");
            }

            uint unsignedChunkByteLength = ReadUInt32LittleEndian(bytes, offset);
            if (unsignedChunkByteLength > (uint)int.MaxValue)
            {
                throw new GltfParseException(
                    $"GLB chunk length {unsignedChunkByteLength} at byte {offset} exceeds the supported signed length.");
            }

            int chunkByteLength = (int)unsignedChunkByteLength;
            if (chunkByteLength % GltfConstants.AlignmentByteLength != 0)
            {
                throw new GltfParseException(
                    $"GLB chunk length {chunkByteLength} at byte {offset} is not aligned to {GltfConstants.AlignmentByteLength} bytes.");
            }

            uint chunkType = ReadUInt32LittleEndian(bytes, offset + 4);
            if (chunkIndex == 0 && chunkType != GltfConstants.GlbJsonChunkType)
            {
                throw new GltfParseException(
                    $"The first GLB chunk is not JSON at byte {offset + 4}.");
            }

            int dataOffset = offset + GltfConstants.GlbChunkHeaderByteLength;
            int availableDataByteLength = header.DeclaredByteLength - dataOffset;
            if (chunkByteLength > availableDataByteLength)
            {
                throw new GltfParseException(
                    $"GLB chunk at byte {offset} declares {chunkByteLength} data bytes beyond container length {header.DeclaredByteLength}.");
            }

            ReadOnlySpan<byte> chunkData = bytes.Slice(dataOffset, chunkByteLength);
            if (chunkType == GltfConstants.GlbJsonChunkType)
            {
                if (hasJsonChunk)
                {
                    throw new GltfParseException($"GLB contains a duplicate JSON chunk at byte {offset + 4}.");
                }

                if (chunkByteLength == 0)
                {
                    throw new GltfParseException($"GLB JSON chunk is empty at byte {dataOffset}.");
                }

                ValidateJsonPadding(chunkData, dataOffset);
                container.JsonBytes = OwnedBytes.CopySpan(chunkData);
                hasJsonChunk = true;
            }
            else if (chunkType == GltfConstants.GlbBinChunkType)
            {
                if (hasBinChunk)
                {
                    throw new GltfParseException($"GLB contains a duplicate BIN chunk at byte {offset + 4}.");
                }

                if (chunkIndex != 1)
                {
                    throw new GltfParseException(
                        $"The GLB BIN chunk is not the second chunk at byte {offset + 4}.");
                }

                container.HasBinChunk = true;
                container.BinBytes = OwnedBytes.CopySpan(chunkData);
                hasBinChunk = true;
            }
            else if (preserveUnknownChunks)
            {
                GltfGlbChunk unknownChunk = new GltfGlbChunk();
                unknownChunk.Type = chunkType;
                unknownChunk.Data = OwnedBytes.CopySpan(chunkData);
                container.UnknownChunks.Add(unknownChunk);
            }

            offset = dataOffset + chunkByteLength;
            chunkIndex++;
        }

        if (!hasJsonChunk)
        {
            throw new GltfParseException($"GLB does not contain a JSON chunk at byte {GltfConstants.GlbHeaderByteLength}.");
        }

        int trailingByteLength = bytes.Length - header.DeclaredByteLength;
        if (trailingByteLength > 0)
        {
            container.TrailingBytes = OwnedBytes.CopySpan(
                bytes.Slice(header.DeclaredByteLength, trailingByteLength));
        }

        return container;
    }

    internal uint ReadUInt32LittleEndian(ReadOnlySpan<byte> bytes, int offset)
    {
        return (uint)bytes[offset]
            | ((uint)bytes[offset + 1] << 8)
            | ((uint)bytes[offset + 2] << 16)
            | ((uint)bytes[offset + 3] << 24);
    }

    internal void ValidateJsonPadding(ReadOnlySpan<byte> jsonBytes, int dataOffset)
    {
        int closingObjectOffset = jsonBytes.Length - 1;
        while (closingObjectOffset >= 0 && jsonBytes[closingObjectOffset] != 0x7D)
        {
            closingObjectOffset--;
        }

        if (closingObjectOffset < 0)
        {
            return;
        }

        int offset = closingObjectOffset + 1;
        while (offset < jsonBytes.Length)
        {
            if (jsonBytes[offset] != GltfConstants.GlbJsonPaddingByte)
            {
                throw new GltfParseException(
                    $"Invalid GLB JSON padding byte at byte {dataOffset + offset}; expected 0x20.");
            }

            offset++;
        }
    }
}
