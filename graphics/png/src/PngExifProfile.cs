using System.IO;

namespace png;

internal class PngExifProfileParser
{
    internal const int MinimumDataLength = 4;

    internal static PngExifProfile Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngExifProfile Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateProfileHeader(data, offset, length);

        PngExifProfile profile = new PngExifProfile();
        profile.Profile = CopyRange(data, offset, length);
        return profile;
    }

    internal static PngExifProfile Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkExif)
        {
            throw new InvalidDataException("Chunk type must be eXIf.");
        }
    }

    internal static void ValidateRange(byte[] data, int offset, int length)
    {
        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length < 0 || length > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException("length");
        }
    }

    internal static void ValidateProfileHeader(byte[] data, int offset, int length)
    {
        if (length < MinimumDataLength)
        {
            throw new InvalidDataException("eXIf profile must contain at least a TIFF byte order marker and fixed 42 value.");
        }

        if (!HasLittleEndianTiffHeader(data, offset) && !HasBigEndianTiffHeader(data, offset))
        {
            throw new InvalidDataException("eXIf profile must begin with a valid TIFF header.");
        }
    }

    internal static bool HasLittleEndianTiffHeader(byte[] data, int offset)
    {
        return data[offset] == 0x49
            && data[offset + 1] == 0x49
            && data[offset + 2] == 0x2A
            && data[offset + 3] == 0x00;
    }

    internal static bool HasBigEndianTiffHeader(byte[] data, int offset)
    {
        return data[offset] == 0x4D
            && data[offset + 1] == 0x4D
            && data[offset + 2] == 0x00
            && data[offset + 3] == 0x2A;
    }

    internal static byte[] CopyRange(byte[] data, int offset, int length)
    {
        byte[] copy = new byte[length];
        CopyBytes(data, offset, copy, 0, length);
        return copy;
    }

    internal static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}

internal class PngExifProfileWriter
{
    internal static byte[] WriteData(PngExifProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(profile.Profile);

        ValidateProfileHeader(profile.Profile, 0, profile.Profile.Length);
        return CopyRange(profile.Profile, 0, profile.Profile.Length);
    }

    internal static void WriteData(byte[] data, int offset, PngExifProfile profile)
    {
        ArgumentNullException.ThrowIfNull(data);

        byte[] payload = WriteData(profile);
        ValidateWriteRange(data, offset, payload.Length);
        CopyBytes(payload, 0, data, offset, payload.Length);
    }

    internal static PngChunk WriteChunk(PngExifProfile profile)
    {
        return CreateChunk(WriteData(profile));
    }

    internal static void ValidateProfileHeader(byte[] data, int offset, int length)
    {
        if (length < PngExifProfileParser.MinimumDataLength)
        {
            throw new ArgumentOutOfRangeException("Profile");
        }

        if (!HasLittleEndianTiffHeader(data, offset) && !HasBigEndianTiffHeader(data, offset))
        {
            throw new ArgumentOutOfRangeException("Profile");
        }
    }

    internal static bool HasLittleEndianTiffHeader(byte[] data, int offset)
    {
        return data[offset] == 0x49
            && data[offset + 1] == 0x49
            && data[offset + 2] == 0x2A
            && data[offset + 3] == 0x00;
    }

    internal static bool HasBigEndianTiffHeader(byte[] data, int offset)
    {
        return data[offset] == 0x4D
            && data[offset + 1] == 0x4D
            && data[offset + 2] == 0x00
            && data[offset + 3] == 0x2A;
    }

    internal static void ValidateWriteRange(byte[] data, int offset, int length)
    {
        if (offset < 0 || offset > data.Length - length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }
    }

    internal static PngChunk CreateChunk(byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkExif;
        chunk.Data = data;
        chunk.Length = (uint)chunk.Data.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = false;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = true;
        return chunk;
    }

    internal static byte[] CopyRange(byte[] data, int offset, int length)
    {
        byte[] copy = new byte[length];
        CopyBytes(data, offset, copy, 0, length);
        return copy;
    }

    internal static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
