using System.Collections.Generic;
using System.Text;

namespace image;

internal class Crc32
{
    internal const uint InitialValue = 4294967295;
    internal const uint Polynomial = 3988292384;

    internal static uint Begin()
    {
        return InitialValue;
    }

    internal static uint Finish(uint crc)
    {
        return crc ^ InitialValue;
    }

    internal static uint Compute(byte[]? bytes)
    {
        return ComputeRange(bytes, 0, ByteLength(bytes), null, "");
    }

    internal static uint ComputeRange(byte[]? bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        uint crc = Begin();
        crc = Update(crc, bytes, offset, count, diagnostics, blockType);
        return Finish(crc);
    }

    internal static uint Update(uint crc, byte[]? bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        if (!ValidateRange(bytes, offset, count, diagnostics, blockType))
        {
            return crc;
        }

        if (bytes == null)
        {
            return crc;
        }

        int index = 0;
        while (index < count)
        {
            crc = UpdateByte(crc, bytes[offset + index]);
            index = index + 1;
        }

        return crc;
    }

    internal static uint UpdateByte(uint crc, byte value)
    {
        crc = crc ^ value;
        int bit = 0;

        while (bit < 8)
        {
            if ((crc & 1) == 1)
            {
                crc = (crc >> 1) ^ Polynomial;
            }
            else
            {
                crc = crc >> 1;
            }

            bit = bit + 1;
        }

        return crc;
    }

    internal static uint ComputePngChunk(string? chunkType, byte[]? data, List<MetadataDiagnostic>? diagnostics)
    {
        if (chunkType == null)
        {
            ValidateChunkType(chunkType, diagnostics);
            return 0;
        }

        if (!ValidateChunkType(chunkType, diagnostics))
        {
            return 0;
        }

        byte[] typeBytes = Encoding.ASCII.GetBytes(chunkType);
        uint crc = Begin();
        crc = Update(crc, typeBytes, 0, typeBytes.Length, diagnostics, chunkType);
        crc = Update(crc, data, 0, ByteLength(data), diagnostics, chunkType);
        return Finish(crc);
    }

    internal static bool ValidatePngChunkCrc(string? chunkType, byte[]? data, uint expectedCrc, long chunkOffset, List<MetadataDiagnostic>? diagnostics)
    {
        if (chunkType == null)
        {
            ValidateChunkType(chunkType, diagnostics);
            return false;
        }

        if (!ValidateChunkType(chunkType, diagnostics))
        {
            return false;
        }

        uint actualCrc = ComputePngChunk(chunkType, data, diagnostics);

        if (actualCrc != expectedCrc)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "png.crc.mismatch", "PNG chunk CRC did not match the computed CRC.", chunkType, chunkOffset, ByteLength(data));
            return false;
        }

        return true;
    }

    internal static bool ValidateChunkType(string? chunkType, List<MetadataDiagnostic>? diagnostics)
    {
        if (chunkType == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "png.chunk.type.null", "PNG chunk type was null.", "PNG", 0, 0);
            return false;
        }

        if (chunkType.Length != 4)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "png.chunk.type.length", "PNG chunk type length was not four bytes.", chunkType, 0, chunkType.Length);
            return false;
        }

        int index = 0;
        while (index < chunkType.Length)
        {
            char value = chunkType[index];

            if (!IsAsciiLetter(value))
            {
                MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "png.chunk.type.invalid", "PNG chunk type contained a non-letter byte.", chunkType, index, 1);
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsAsciiLetter(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return true;
        }

        if (value >= 'a' && value <= 'z')
        {
            return true;
        }

        return false;
    }

    internal static bool ValidateRange(byte[]? bytes, int offset, int count, List<MetadataDiagnostic>? diagnostics, string blockType)
    {
        if (bytes == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "crc.bytes.null", "CRC byte array was null.", blockType, offset, count);
            return false;
        }

        if (offset < 0 || count < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "crc.range.negative", "CRC range offset or count was negative.", blockType, offset, count);
            return false;
        }

        if (offset > bytes.Length)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "crc.offset.outside", "CRC range offset exceeded byte array length.", blockType, offset, count);
            return false;
        }

        if (count > bytes.Length - offset)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "crc.range.truncated", "CRC range exceeded byte array length.", blockType, offset, count);
            return false;
        }

        return true;
    }

    internal static int ByteLength(byte[]? bytes)
    {
        if (bytes == null)
        {
            return 0;
        }

        return bytes.Length;
    }
}
