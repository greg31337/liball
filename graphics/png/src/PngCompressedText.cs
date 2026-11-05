using System.IO;

namespace png;

internal class PngCompressedTextParser
{
    internal const int MinimumKeywordLength = PngTextKeywordValidator.MinimumKeywordLength;
    internal const int MaximumKeywordLength = PngTextKeywordValidator.MaximumKeywordLength;
    internal const byte CompressionMethodDeflate = 0;

    internal static PngCompressedText Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length, new PngSecurityLimits());
    }

    internal static PngCompressedText Parse(byte[] data, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length, limits);
    }

    internal static PngCompressedText Parse(byte[] data, int offset, int length)
    {
        return Parse(data, offset, length, new PngSecurityLimits());
    }

    internal static PngCompressedText Parse(byte[] data, int offset, int length, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(limits);

        ValidateRange(data, offset, length);
        int separatorOffset = FindSeparator(data, offset, length);
        int keywordLength = separatorOffset - offset;
        ValidateKeywordLength(keywordLength);
        ValidateKeyword(data, offset, keywordLength);

        int compressionMethodOffset = separatorOffset + 1;
        if (compressionMethodOffset >= offset + length)
        {
            throw new InvalidDataException("zTXt data must include a compression method byte.");
        }

        byte compressionMethod = data[compressionMethodOffset];
        ValidateCompressionMethod(compressionMethod);

        int compressedOffset = compressionMethodOffset + 1;
        int compressedLength = offset + length - compressedOffset;

        byte[] decompressed = PngZlibDecompressor.Decompress(data, compressedOffset, compressedLength, limits);
        ValidateNoNullBytes(decompressed, 0, decompressed.Length, "zTXt text string must not contain a null character.");

        PngCompressedText text = new PngCompressedText();
        text.Keyword = DecodeLatin1(data, offset, keywordLength);
        text.CompressionMethod = compressionMethod;
        text.CompressedText = CopyRange(data, compressedOffset, compressedLength);
        text.Text = DecodeLatin1(decompressed, 0, decompressed.Length);
        return text;
    }

    internal static PngCompressedText Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static PngCompressedText Parse(PngChunk chunk, PngSecurityLimits limits)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data, limits);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkCompressedText)
        {
            throw new InvalidDataException("Chunk type must be zTXt.");
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

    internal static int FindSeparator(byte[] data, int offset, int length)
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

        throw new InvalidDataException("zTXt data must include a keyword separator.");
    }

    internal static void ValidateKeywordLength(int length)
    {
        if (length < MinimumKeywordLength || length > MaximumKeywordLength)
        {
            throw new InvalidDataException("zTXt keyword must contain 1 through 79 bytes.");
        }
    }

    internal static void ValidateKeyword(byte[] data, int offset, int length)
    {
        if (!PngTextKeywordValidator.IsValidBytes(data, offset, length))
        {
            throw new InvalidDataException("zTXt keyword contains invalid characters or spacing.");
        }
    }

    internal static void ValidateCompressionMethod(byte compressionMethod)
    {
        if (compressionMethod != CompressionMethodDeflate)
        {
            throw new InvalidDataException("zTXt compression method must be 0.");
        }
    }

    internal static void ValidateNoNullBytes(byte[] data, int offset, int length, string message)
    {
        int index = 0;
        while (index < length)
        {
            if (data[offset + index] == 0)
            {
                throw new InvalidDataException(message);
            }

            index += 1;
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

internal class PngCompressedTextWriter
{
    internal static byte[] WriteData(PngCompressedText text)
    {
        return WriteData(text, new PngSecurityLimits());
    }

    internal static byte[] WriteData(PngCompressedText text, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(text.Keyword);
        ArgumentNullException.ThrowIfNull(text.Text);
        ArgumentNullException.ThrowIfNull(limits);

        ValidateCompressionMethod(text.CompressionMethod);
        byte[] keyword = EncodeKeyword(text.Keyword);
        byte[] value = EncodeText(text.Text);
        byte[] compressed = PngZlibCompressor.Compress(value, limits);
        return CreateData(keyword, text.CompressionMethod, compressed);
    }

    internal static void WriteData(byte[] data, int offset, PngCompressedText text)
    {
        WriteData(data, offset, text, new PngSecurityLimits());
    }

    internal static void WriteData(byte[] data, int offset, PngCompressedText text, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);

        byte[] payload = WriteData(text, limits);
        ValidateWriteRange(data, offset, payload.Length);
        CopyBytes(payload, 0, data, offset, payload.Length);
    }

    internal static PngChunk WriteChunk(PngCompressedText text)
    {
        return CreateChunk(WriteData(text));
    }

    internal static PngChunk WriteChunk(PngCompressedText text, PngSecurityLimits limits)
    {
        return CreateChunk(WriteData(text, limits));
    }

    internal static void ValidateCompressionMethod(byte compressionMethod)
    {
        if (compressionMethod != PngCompressedTextParser.CompressionMethodDeflate)
        {
            throw new ArgumentOutOfRangeException("CompressionMethod");
        }
    }

    internal static byte[] EncodeKeyword(string keyword)
    {
        PngTextKeywordValidator.Validate(keyword);
        byte[] data = EncodeLatin1(keyword, "Keyword");
        if (data.Length < PngCompressedTextParser.MinimumKeywordLength || data.Length > PngCompressedTextParser.MaximumKeywordLength)
        {
            throw new ArgumentOutOfRangeException("Keyword");
        }

        return data;
    }

    internal static byte[] EncodeText(string text)
    {
        return EncodeLatin1(text, "Text");
    }

    internal static byte[] EncodeLatin1(string value, string name)
    {
        byte[] data = new byte[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\0' || character > 255)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            data[index] = (byte)character;
            index += 1;
        }

        return data;
    }

    internal static byte[] CreateData(byte[] keyword, byte compressionMethod, byte[] compressed)
    {
        int length = keyword.Length + 2 + compressed.Length;
        byte[] data = new byte[length];
        CopyBytes(keyword, 0, data, 0, keyword.Length);
        data[keyword.Length] = 0;
        data[keyword.Length + 1] = compressionMethod;
        CopyBytes(compressed, 0, data, keyword.Length + 2, compressed.Length);
        return data;
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
        chunk.Type = PngConstants.ChunkCompressedText;
        chunk.Data = data;
        chunk.Length = (uint)chunk.Data.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = false;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = true;
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
