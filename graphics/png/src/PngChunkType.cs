namespace png;

internal class PngChunkType
{
    internal string Name = "";
    internal byte Byte0;
    internal byte Byte1;
    internal byte Byte2;
    internal byte Byte3;
}

internal class PngChunkTypeParser
{
    internal static PngChunkType Parse(byte[] data)
    {
        return Parse(data, 0);
    }

    internal static PngChunkType Parse(byte[] data, int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset > data.Length - 4)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        return Parse(data[offset], data[offset + 1], data[offset + 2], data[offset + 3]);
    }

    internal static PngChunkType Parse(byte byte0, byte byte1, byte byte2, byte byte3)
    {
        PngChunkType chunkType = new PngChunkType();
        chunkType.Byte0 = byte0;
        chunkType.Byte1 = byte1;
        chunkType.Byte2 = byte2;
        chunkType.Byte3 = byte3;
        chunkType.Name = new string(new char[]
        {
            (char)byte0,
            (char)byte1,
            (char)byte2,
            (char)byte3
        });

        return chunkType;
    }
}

internal class PngChunkTypeFlags
{
    internal static bool IsCritical(PngChunkType chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        return !HasLowercaseBit(chunkType.Byte0);
    }

    internal static bool IsCritical(string chunkType)
    {
        return !HasLowercaseBit(ReadChunkTypeByte(chunkType, 0));
    }

    internal static bool IsAncillary(PngChunkType chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        return HasLowercaseBit(chunkType.Byte0);
    }

    internal static bool IsAncillary(string chunkType)
    {
        return HasLowercaseBit(ReadChunkTypeByte(chunkType, 0));
    }

    internal static bool IsPublic(PngChunkType chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        return !HasLowercaseBit(chunkType.Byte1);
    }

    internal static bool IsPublic(string chunkType)
    {
        return !HasLowercaseBit(ReadChunkTypeByte(chunkType, 1));
    }

    internal static bool IsPrivate(PngChunkType chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        return HasLowercaseBit(chunkType.Byte1);
    }

    internal static bool IsPrivate(string chunkType)
    {
        return HasLowercaseBit(ReadChunkTypeByte(chunkType, 1));
    }

    internal static bool IsReservedBitValid(PngChunkType chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        return !HasLowercaseBit(chunkType.Byte2);
    }

    internal static bool IsReservedBitValid(string chunkType)
    {
        return !HasLowercaseBit(ReadChunkTypeByte(chunkType, 2));
    }

    internal static bool IsSafeToCopy(PngChunkType chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        return HasLowercaseBit(chunkType.Byte3);
    }

    internal static bool IsSafeToCopy(string chunkType)
    {
        return HasLowercaseBit(ReadChunkTypeByte(chunkType, 3));
    }

    internal static bool HasLowercaseBit(byte value)
    {
        return (value & 32) != 0;
    }

    internal static byte ReadChunkTypeByte(string chunkType, int index)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        if (chunkType.Length != 4)
        {
            throw new ArgumentException("PNG chunk type must contain exactly 4 characters.");
        }

        char character = chunkType[index];
        if (character > 255)
        {
            throw new ArgumentException("PNG chunk type characters must fit in one byte.");
        }

        return (byte)character;
    }
}

internal class PngChunkTypeValidator
{
    internal static bool IsValid(string chunkType)
    {
        return Validate(chunkType).IsValid;
    }

    internal static bool IsValid(PngChunkType chunkType)
    {
        return Validate(chunkType).IsValid;
    }

    internal static PngValidationResult Validate(string chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        if (chunkType.Length != 4)
        {
            return CreateInvalidResult("PNG_CHUNK_TYPE_LENGTH", "PNG chunk type must contain exactly 4 characters.", chunkType, 0);
        }

        int index = 0;
        while (index < 4)
        {
            char character = chunkType[index];
            if (character > 127 || !IsAsciiLetter((byte)character))
            {
                return CreateInvalidResult("PNG_CHUNK_TYPE_CHARACTER", "PNG chunk type characters must be ASCII letters.", chunkType, index);
            }

            index += 1;
        }

        if (!PngChunkTypeFlags.IsReservedBitValid(chunkType))
        {
            return CreateInvalidResult("PNG_CHUNK_TYPE_RESERVED_BIT", "PNG chunk type third character must be uppercase.", chunkType, 2);
        }

        return CreateValidResult();
    }

    internal static PngValidationResult Validate(PngChunkType chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        if (!IsAsciiLetter(chunkType.Byte0))
        {
            return CreateInvalidResult("PNG_CHUNK_TYPE_CHARACTER", "PNG chunk type characters must be ASCII letters.", chunkType.Name, 0);
        }

        if (!IsAsciiLetter(chunkType.Byte1))
        {
            return CreateInvalidResult("PNG_CHUNK_TYPE_CHARACTER", "PNG chunk type characters must be ASCII letters.", chunkType.Name, 1);
        }

        if (!IsAsciiLetter(chunkType.Byte2))
        {
            return CreateInvalidResult("PNG_CHUNK_TYPE_CHARACTER", "PNG chunk type characters must be ASCII letters.", chunkType.Name, 2);
        }

        if (!IsAsciiLetter(chunkType.Byte3))
        {
            return CreateInvalidResult("PNG_CHUNK_TYPE_CHARACTER", "PNG chunk type characters must be ASCII letters.", chunkType.Name, 3);
        }

        if (!PngChunkTypeFlags.IsReservedBitValid(chunkType))
        {
            return CreateInvalidResult("PNG_CHUNK_TYPE_RESERVED_BIT", "PNG chunk type third character must be uppercase.", chunkType.Name, 2);
        }

        return CreateValidResult();
    }

    internal static bool IsAsciiLetter(byte value)
    {
        if (value >= 65 && value <= 90)
        {
            return true;
        }

        if (value >= 97 && value <= 122)
        {
            return true;
        }

        return false;
    }

    internal static PngValidationResult CreateValidResult()
    {
        PngValidationResult result = new PngValidationResult();
        result.IsValid = true;
        return result;
    }

    internal static PngValidationResult CreateInvalidResult(string code, string message, string chunkType, long offset)
    {
        PngValidationResult result = new PngValidationResult();
        PngError error = new PngError();
        result.IsValid = false;
        error.Code = code;
        error.Message = message;
        error.ChunkType = chunkType;
        error.Offset = offset;
        error.IsFatal = true;
        result.Errors = new PngError[] { error };
        return result;
    }
}
