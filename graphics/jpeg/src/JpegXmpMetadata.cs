using System.Collections.Generic;

namespace jpeg;

internal class JpegXmpMetadataParser
{
    internal bool IsXmp(byte[]? payload)
    {
        if (payload == null)
        {
            return false;
        }

        if (payload.Length < JpegConstants.XmpIdentifierLength)
        {
            return false;
        }

        byte[] identifier = GetIdentifier();
        int index = 0;
        while (index < identifier.Length)
        {
            if (payload[index] != identifier[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal JpegXmpMetadata Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (!IsXmp(payload))
        {
            throw JpegInvalidDataException.Create("APP1 payload is not an XMP segment.");
        }

        JpegXmpMetadata metadata = new JpegXmpMetadata();
        metadata.IsPresent = true;
        metadata.RawPayload = CopyBytes(payload, 0, payload.Length);
        metadata.PacketData = CopyBytes(payload, JpegConstants.XmpIdentifierLength, payload.Length - JpegConstants.XmpIdentifierLength);
        metadata.Packet = System.Text.Encoding.UTF8.GetString(metadata.PacketData);
        return metadata;
    }

    internal byte[] GetIdentifier()
    {
        return new byte[]
        {
            0x68, 0x74, 0x74, 0x70, 0x3A, 0x2F, 0x2F, 0x6E,
            0x73, 0x2E, 0x61, 0x64, 0x6F, 0x62, 0x65, 0x2E,
            0x63, 0x6F, 0x6D, 0x2F, 0x78, 0x61, 0x70, 0x2F,
            0x31, 0x2E, 0x30, 0x2F, 0
        };
    }

    internal byte[] CopyBytes(byte[] data, int offset, int length)
    {
        byte[] copy = new byte[length];
        if (length > 0)
        {
            System.Array.Copy(data, offset, copy, 0, length);
        }

        return copy;
    }
}

internal class JpegExtendedXmpChunkParser
{
    internal bool IsExtendedXmp(byte[]? payload)
    {
        if (payload == null)
        {
            return false;
        }

        if (payload.Length < JpegConstants.ExtendedXmpChunkHeaderLength)
        {
            return false;
        }

        byte[] identifier = GetIdentifier();
        int index = 0;
        while (index < identifier.Length)
        {
            if (payload[index] != identifier[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal JpegExtendedXmpChunk Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (!IsExtendedXmp(payload))
        {
            throw JpegInvalidDataException.Create("APP1 payload is not an Extended XMP chunk.");
        }

        JpegExtendedXmpChunk chunk = new JpegExtendedXmpChunk();
        chunk.IsPresent = true;
        chunk.Guid = System.Text.Encoding.ASCII.GetString(payload, JpegConstants.ExtendedXmpIdentifierLength, 32);
        chunk.FullLength = JpegByteReader.ReadUInt32(payload, JpegConstants.ExtendedXmpIdentifierLength + 32);
        chunk.Offset = JpegByteReader.ReadUInt32(payload, JpegConstants.ExtendedXmpIdentifierLength + 36);
        int dataOffset = JpegConstants.ExtendedXmpChunkHeaderLength;
        int dataLength = payload.Length - dataOffset;
        ValidateChunk(chunk, dataLength);
        chunk.RawPayload = CopyBytes(payload, 0, payload.Length);
        chunk.Data = CopyBytes(payload, dataOffset, dataLength);
        return chunk;
    }

    internal void ValidateChunk(JpegExtendedXmpChunk chunk, int dataLength)
    {
        if (!IsValidGuid(chunk.Guid))
        {
            throw JpegInvalidDataException.Create("Extended XMP GUID must contain 32 hexadecimal ASCII characters.");
        }

        if (chunk.FullLength <= 0 || chunk.FullLength > 2147483647L)
        {
            throw JpegInvalidDataException.Create("Extended XMP full length is invalid.");
        }

        if (chunk.Offset < 0 || chunk.Offset > chunk.FullLength)
        {
            throw JpegInvalidDataException.Create("Extended XMP chunk offset is invalid.");
        }

        if (dataLength > chunk.FullLength - chunk.Offset)
        {
            throw JpegInvalidDataException.Create("Extended XMP chunk data exceeds the full packet length.");
        }
    }

    internal bool IsValidGuid(string guid)
    {
        if (guid == null || guid.Length != 32)
        {
            return false;
        }

        int index = 0;
        while (index < guid.Length)
        {
            char value = guid[index];
            bool digit = value >= '0' && value <= '9';
            bool upper = value >= 'A' && value <= 'F';
            bool lower = value >= 'a' && value <= 'f';
            if (!digit && !upper && !lower)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal byte[] GetIdentifier()
    {
        return new byte[]
        {
            0x68, 0x74, 0x74, 0x70, 0x3A, 0x2F, 0x2F, 0x6E,
            0x73, 0x2E, 0x61, 0x64, 0x6F, 0x62, 0x65, 0x2E,
            0x63, 0x6F, 0x6D, 0x2F, 0x78, 0x6D, 0x70, 0x2F,
            0x65, 0x78, 0x74, 0x65, 0x6E, 0x73, 0x69, 0x6F,
            0x6E, 0x2F, 0
        };
    }

    internal byte[] CopyBytes(byte[] data, int offset, int length)
    {
        byte[] copy = new byte[length];
        if (length > 0)
        {
            System.Array.Copy(data, offset, copy, 0, length);
        }

        return copy;
    }
}

internal class JpegExtendedXmpAssembler
{
    internal byte[] Reassemble(List<JpegExtendedXmpChunk> chunks)
    {
        if (chunks == null)
        {
            throw new System.ArgumentNullException("chunks");
        }

        JpegExtendedXmpMetadata metadata = Inspect(chunks);
        if (!metadata.IsPresent)
        {
            return new byte[0];
        }

        if (!metadata.IsComplete)
        {
            throw JpegInvalidDataException.Create(metadata.Diagnostics[0].Message);
        }

        return metadata.PacketData;
    }

    internal JpegExtendedXmpMetadata Inspect(List<JpegExtendedXmpChunk> chunks)
    {
        if (chunks == null)
        {
            throw new System.ArgumentNullException("chunks");
        }

        JpegExtendedXmpMetadata metadata = new JpegExtendedXmpMetadata();
        if (chunks.Count == 0)
        {
            return metadata;
        }

        metadata.IsPresent = true;
        foreach (JpegExtendedXmpChunk source in chunks)
        {
            metadata.Chunks.Add(source);
        }

        JpegExtendedXmpChunk first = chunks[0];
        metadata.Guid = first.Guid;
        metadata.FullLength = first.FullLength;
        JpegExtendedXmpChunkParser parser = new JpegExtendedXmpChunkParser();
        int chunkIndex = 0;
        while (chunkIndex < chunks.Count)
        {
            JpegExtendedXmpChunk chunk = chunks[chunkIndex];
            if (!parser.IsValidGuid(chunk.Guid))
            {
                AddDiagnostic(metadata, JpegXmpReassemblyDiagnosticCode.InvalidGuid, "Extended XMP GUID must contain 32 hexadecimal ASCII characters.", chunkIndex, -1, 0);
            }
            else if (chunk.Guid != first.Guid)
            {
                AddDiagnostic(metadata, JpegXmpReassemblyDiagnosticCode.GuidMismatch, "Extended XMP chunks must share the same GUID.", chunkIndex, -1, 0);
            }

            if (chunk.FullLength <= 0 || chunk.FullLength > 2147483647L)
            {
                AddDiagnostic(metadata, JpegXmpReassemblyDiagnosticCode.InvalidFullLength, "Extended XMP full length is invalid.", chunkIndex, -1, chunk.FullLength);
            }
            else if (chunk.FullLength != first.FullLength)
            {
                AddDiagnostic(metadata, JpegXmpReassemblyDiagnosticCode.FullLengthMismatch, "Extended XMP chunks must share the same full length.", chunkIndex, -1, chunk.FullLength);
            }

            if (chunk.Data == null)
            {
                AddDiagnostic(metadata, JpegXmpReassemblyDiagnosticCode.InvalidOffset, "Extended XMP chunk data cannot be null.", chunkIndex, chunk.Offset, 0);
            }
            else if (first.FullLength <= 0 || first.FullLength > 2147483647L || chunk.Offset < 0 || chunk.Offset > first.FullLength || chunk.Data.Length > first.FullLength - chunk.Offset)
            {
                AddDiagnostic(metadata, JpegXmpReassemblyDiagnosticCode.InvalidOffset, "Extended XMP chunk data exceeds the full packet length.", chunkIndex, chunk.Offset, chunk.Data.Length);
            }

            chunkIndex = chunkIndex + 1;
        }

        if (metadata.Diagnostics.Count > 0)
        {
            return metadata;
        }

        int fullLength = (int)first.FullLength;
        byte[] packet = new byte[fullLength];
        bool[] seen = new bool[fullLength];
        chunkIndex = 0;
        while (chunkIndex < chunks.Count)
        {
            JpegExtendedXmpChunk chunk = chunks[chunkIndex];
            bool overlap = false;
            int dataIndex = 0;
            while (dataIndex < chunk.Data.Length)
            {
                int destination = (int)chunk.Offset + dataIndex;
                if (seen[destination])
                {
                    overlap = true;
                }
                else
                {
                    packet[destination] = chunk.Data[dataIndex];
                    seen[destination] = true;
                }

                dataIndex = dataIndex + 1;
            }

            if (overlap)
            {
                AddDiagnostic(metadata, JpegXmpReassemblyDiagnosticCode.Overlap, "Extended XMP chunks overlap.", chunkIndex, chunk.Offset, chunk.Data.Length);
            }

            chunkIndex = chunkIndex + 1;
        }

        int gapOffset = FindFirstGap(seen);
        if (gapOffset >= 0)
        {
            AddDiagnostic(metadata, JpegXmpReassemblyDiagnosticCode.Gap, "Extended XMP chunks do not cover the full packet.", -1, gapOffset, CountGap(seen, gapOffset));
        }

        if (metadata.Diagnostics.Count == 0)
        {
            metadata.IsComplete = true;
            metadata.PacketData = packet;
            metadata.Packet = System.Text.Encoding.UTF8.GetString(packet);
        }

        return metadata;
    }

    internal int FindFirstGap(bool[] seen)
    {
        int index = 0;
        while (index < seen.Length)
        {
            if (!seen[index])
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int CountGap(bool[] seen, int offset)
    {
        int index = offset;
        while (index < seen.Length && !seen[index])
        {
            index = index + 1;
        }

        return index - offset;
    }

    internal void AddDiagnostic(JpegExtendedXmpMetadata metadata, JpegXmpReassemblyDiagnosticCode code, string message, int chunkIndex, long offset, long length)
    {
        JpegXmpReassemblyDiagnostic diagnostic = new JpegXmpReassemblyDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.ChunkIndex = chunkIndex;
        diagnostic.Offset = offset;
        diagnostic.Length = length;
        metadata.Diagnostics.Add(diagnostic);
    }
}

internal class JpegExtendedXmpChunkWriter
{
    internal List<JpegExtendedXmpChunk> CreateChunks(byte[] packet, string guid)
    {
        if (packet == null)
        {
            throw new System.ArgumentNullException("packet");
        }

        ValidateGuid(guid);
        if (packet.Length == 0)
        {
            throw JpegWriteValidationException.Create("Extended XMP packet cannot be empty.");
        }

        List<JpegExtendedXmpChunk> chunks = new List<JpegExtendedXmpChunk>();
        int offset = 0;
        while (offset < packet.Length)
        {
            int chunkLength = packet.Length - offset;
            if (chunkLength > JpegConstants.MaximumExtendedXmpChunkDataLength)
            {
                chunkLength = JpegConstants.MaximumExtendedXmpChunkDataLength;
            }

            JpegExtendedXmpChunk chunk = new JpegExtendedXmpChunk();
            chunk.IsPresent = true;
            chunk.Guid = guid;
            chunk.FullLength = packet.Length;
            chunk.Offset = offset;
            chunk.Data = new byte[chunkLength];
            System.Array.Copy(packet, offset, chunk.Data, 0, chunkLength);
            chunks.Add(chunk);
            offset = offset + chunkLength;
        }

        return chunks;
    }

    internal byte[] WritePayload(JpegExtendedXmpChunk chunk)
    {
        ValidateChunk(chunk);
        JpegExtendedXmpChunkParser parser = new JpegExtendedXmpChunkParser();
        byte[] identifier = parser.GetIdentifier();
        byte[] payload = new byte[JpegConstants.ExtendedXmpChunkHeaderLength + chunk.Data.Length];
        System.Array.Copy(identifier, 0, payload, 0, identifier.Length);
        byte[] guidBytes = System.Text.Encoding.ASCII.GetBytes(chunk.Guid);
        System.Array.Copy(guidBytes, 0, payload, JpegConstants.ExtendedXmpIdentifierLength, 32);
        WriteUInt32(payload, JpegConstants.ExtendedXmpIdentifierLength + 32, chunk.FullLength);
        WriteUInt32(payload, JpegConstants.ExtendedXmpIdentifierLength + 36, chunk.Offset);
        System.Array.Copy(chunk.Data, 0, payload, JpegConstants.ExtendedXmpChunkHeaderLength, chunk.Data.Length);
        return payload;
    }

    internal void WriteMarkerSegments(JpegMarkerSegmentWriter segmentWriter, byte[] packet, string guid)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        List<JpegExtendedXmpChunk> chunks = CreateChunks(packet, guid);
        foreach (JpegExtendedXmpChunk chunk in chunks)
        {
            segmentWriter.WriteMarkerSegment(JpegMarker.App1, WritePayload(chunk));
        }
    }

    internal void ValidateChunk(JpegExtendedXmpChunk chunk)
    {
        if (chunk == null)
        {
            throw new System.ArgumentNullException("chunk");
        }

        ValidateGuid(chunk.Guid);
        if (chunk.Data == null)
        {
            throw JpegWriteValidationException.Create("Extended XMP chunk data cannot be null.");
        }

        if (chunk.FullLength <= 0 || chunk.FullLength > 2147483647L)
        {
            throw JpegWriteValidationException.Create("Extended XMP full length is invalid.");
        }

        if (chunk.Offset < 0 || chunk.Offset > chunk.FullLength || chunk.Data.Length > chunk.FullLength - chunk.Offset)
        {
            throw JpegWriteValidationException.Create("Extended XMP chunk data exceeds the full packet length.");
        }

        if (chunk.Data.Length > JpegConstants.MaximumExtendedXmpChunkDataLength)
        {
            throw JpegWriteValidationException.Create("Extended XMP chunk data exceeds the maximum APP1 payload length.");
        }
    }

    internal void ValidateGuid(string guid)
    {
        if (guid == null)
        {
            throw new System.ArgumentNullException("guid");
        }

        JpegExtendedXmpChunkParser parser = new JpegExtendedXmpChunkParser();
        if (!parser.IsValidGuid(guid))
        {
            throw JpegWriteValidationException.Create("Extended XMP GUID must contain 32 hexadecimal ASCII characters.");
        }
    }

    internal void WriteUInt32(byte[] data, int offset, long value)
    {
        JpegByteWriter.WriteUInt32(data, offset, value);
    }
}

internal class JpegXmpMetadataWriter
{
    internal byte[] WritePayload(JpegXmpMetadata metadata)
    {
        Validate(metadata);
        byte[] payload = new byte[metadata.RawPayload.Length];
        if (payload.Length > 0)
        {
            System.Array.Copy(metadata.RawPayload, 0, payload, 0, metadata.RawPayload.Length);
        }

        return payload;
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, JpegXmpMetadata metadata)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        byte[] payload = WritePayload(metadata);
        segmentWriter.WriteMarkerSegment(JpegMarker.App1, payload);
    }

    internal void Validate(JpegXmpMetadata metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        JpegXmpMetadataParser parser = new JpegXmpMetadataParser();
        if (!parser.IsXmp(metadata.RawPayload))
        {
            throw JpegWriteValidationException.Create("XMP metadata must contain an APP1 XMP payload.");
        }

        if (metadata.RawPayload.Length > JpegConstants.MaximumMarkerPayloadLength)
        {
            throw JpegWriteValidationException.Create("XMP APP1 payload exceeds the maximum marker payload length.");
        }
    }
}
