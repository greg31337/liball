using System.IO;

namespace png;

internal class PngIccProfileParser
{
    internal const int MinimumNameLength = 1;
    internal const int MaximumNameLength = 79;
    internal const byte CompressionMethodDeflate = 0;

    internal static PngIccProfile Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length, new PngSecurityLimits());
    }

    internal static PngIccProfile Parse(byte[] data, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length, limits);
    }

    internal static PngIccProfile Parse(byte[] data, int offset, int length)
    {
        return Parse(data, offset, length, new PngSecurityLimits());
    }

    internal static PngIccProfile Parse(byte[] data, int offset, int length, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(limits);

        ValidateRange(data, offset, length);
        int separatorOffset = FindNameSeparator(data, offset, length);
        int nameLength = separatorOffset - offset;
        ValidateNameLength(nameLength);

        int compressionMethodOffset = separatorOffset + 1;
        if (compressionMethodOffset >= offset + length)
        {
            throw new InvalidDataException("iCCP data must include a compression method byte.");
        }

        byte compressionMethod = data[compressionMethodOffset];
        ValidateCompressionMethod(compressionMethod);

        int compressedOffset = compressionMethodOffset + 1;
        int compressedLength = offset + length - compressedOffset;
        if (compressedLength <= 0)
        {
            throw new InvalidDataException("iCCP compressed profile data must not be empty.");
        }

        PngIccProfile profile = new PngIccProfile();
        profile.Name = DecodeLatin1(data, offset, nameLength);
        profile.CompressionMethod = compressionMethod;
        profile.CompressedProfile = CopyRange(data, compressedOffset, compressedLength);
        profile.Profile = PngZlibDecompressor.Decompress(data, compressedOffset, compressedLength, limits);
        return profile;
    }

    internal static PngIccProfile Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static PngIccProfile Parse(PngChunk chunk, PngSecurityLimits limits)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data, limits);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkIccProfile)
        {
            throw new InvalidDataException("Chunk type must be iCCP.");
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

    internal static int FindNameSeparator(byte[] data, int offset, int length)
    {
        int index = offset;
        int end = offset + length;
        while (index < end)
        {
            if (data[index] == 0)
            {
                return index;
            }

            index += 1;
        }

        throw new InvalidDataException("iCCP data must include a profile name separator.");
    }

    internal static void ValidateNameLength(int length)
    {
        if (length < MinimumNameLength || length > MaximumNameLength)
        {
            throw new InvalidDataException("iCCP profile name must contain 1 through 79 bytes.");
        }
    }

    internal static void ValidateCompressionMethod(byte compressionMethod)
    {
        if (compressionMethod != CompressionMethodDeflate)
        {
            throw new InvalidDataException("iCCP compression method must be 0.");
        }
    }

    internal static string DecodeLatin1(byte[] data, int offset, int length)
    {
        char[] chars = new char[length];
        int index = 0;
        while (index < length)
        {
            chars[index] = (char)data[offset + index];
            index += 1;
        }

        return new string(chars);
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

internal class PngIccProfileWriter
{
    internal static byte[] WriteData(PngIccProfile profile)
    {
        return WriteData(profile, new PngSecurityLimits());
    }

    internal static byte[] WriteData(PngIccProfile profile, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(profile.Name);
        ArgumentNullException.ThrowIfNull(profile.Profile);
        ArgumentNullException.ThrowIfNull(limits);

        ValidateCompressionMethod(profile.CompressionMethod);
        byte[] name = EncodeLatin1(profile.Name);
        byte[] compressed = PngZlibCompressor.Compress(profile.Profile, limits);
        return CreateData(name, compressed);
    }

    internal static void WriteData(byte[] data, int offset, PngIccProfile profile)
    {
        ArgumentNullException.ThrowIfNull(data);

        byte[] payload = WriteData(profile);
        ValidateWriteRange(data, offset, payload.Length);
        CopyBytes(payload, 0, data, offset, payload.Length);
    }

    internal static PngChunk WriteChunk(PngIccProfile profile)
    {
        return CreateChunk(WriteData(profile));
    }

    internal static byte[] CreateData(byte[] name, byte[] compressed)
    {
        int length = name.Length + 2 + compressed.Length;
        byte[] data = new byte[length];
        CopyBytes(name, 0, data, 0, name.Length);
        data[name.Length] = 0;
        data[name.Length + 1] = PngIccProfileParser.CompressionMethodDeflate;
        CopyBytes(compressed, 0, data, name.Length + 2, compressed.Length);
        return data;
    }

    internal static byte[] EncodeLatin1(string name)
    {
        if (name.Length < PngIccProfileParser.MinimumNameLength || name.Length > PngIccProfileParser.MaximumNameLength)
        {
            throw new ArgumentOutOfRangeException("Name");
        }

        byte[] data = new byte[name.Length];
        int index = 0;
        while (index < name.Length)
        {
            char value = name[index];
            if (value == '\0' || value > 255)
            {
                throw new ArgumentOutOfRangeException("Name");
            }

            data[index] = (byte)value;
            index += 1;
        }

        return data;
    }

    internal static void ValidateCompressionMethod(byte compressionMethod)
    {
        if (compressionMethod != PngIccProfileParser.CompressionMethodDeflate)
        {
            throw new ArgumentOutOfRangeException("CompressionMethod");
        }
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
        chunk.Type = PngConstants.ChunkIccProfile;
        chunk.Data = data;
        chunk.Length = (uint)chunk.Data.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = false;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
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
