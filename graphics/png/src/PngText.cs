using System.IO;

namespace png;

internal class PngTextParser
{
    internal const int MinimumKeywordLength = PngTextKeywordValidator.MinimumKeywordLength;
    internal const int MaximumKeywordLength = PngTextKeywordValidator.MaximumKeywordLength;

    internal static PngText Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngText Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        int separatorOffset = FindSeparator(data, offset, length);
        int keywordLength = separatorOffset - offset;
        ValidateKeywordLength(keywordLength);
        ValidateKeyword(data, offset, keywordLength);

        int textOffset = separatorOffset + 1;
        int textLength = offset + length - textOffset;
        ValidateNoNullBytes(data, textOffset, textLength, "tEXt text string must not contain a null character.");

        PngText text = new PngText();
        text.Keyword = DecodeLatin1(data, offset, keywordLength);
        text.Text = DecodeLatin1(data, textOffset, textLength);
        return text;
    }

    internal static PngText Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkText)
        {
            throw new InvalidDataException("Chunk type must be tEXt.");
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

        throw new InvalidDataException("tEXt data must include a keyword separator.");
    }

    internal static void ValidateKeywordLength(int length)
    {
        if (length < MinimumKeywordLength || length > MaximumKeywordLength)
        {
            throw new InvalidDataException("tEXt keyword must contain 1 through 79 bytes.");
        }
    }

    internal static void ValidateKeyword(byte[] data, int offset, int length)
    {
        if (!PngTextKeywordValidator.IsValidBytes(data, offset, length))
        {
            throw new InvalidDataException("tEXt keyword contains invalid characters or spacing.");
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
}

internal class PngTextWriter
{
    internal static byte[] WriteData(PngText text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(text.Keyword);
        ArgumentNullException.ThrowIfNull(text.Text);

        byte[] keyword = EncodeKeyword(text.Keyword);
        byte[] value = EncodeText(text.Text);
        return CreateData(keyword, value);
    }

    internal static void WriteData(byte[] data, int offset, PngText text)
    {
        ArgumentNullException.ThrowIfNull(data);

        byte[] payload = WriteData(text);
        ValidateWriteRange(data, offset, payload.Length);
        CopyBytes(payload, 0, data, offset, payload.Length);
    }

    internal static PngChunk WriteChunk(PngText text)
    {
        return CreateChunk(WriteData(text));
    }

    internal static byte[] EncodeKeyword(string keyword)
    {
        PngTextKeywordValidator.Validate(keyword);
        byte[] data = EncodeLatin1(keyword, "Keyword");
        if (data.Length < PngTextParser.MinimumKeywordLength || data.Length > PngTextParser.MaximumKeywordLength)
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

    internal static byte[] CreateData(byte[] keyword, byte[] text)
    {
        int length = keyword.Length + 1 + text.Length;
        byte[] data = new byte[length];
        CopyBytes(keyword, 0, data, 0, keyword.Length);
        data[keyword.Length] = 0;
        CopyBytes(text, 0, data, keyword.Length + 1, text.Length);
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
        chunk.Type = PngConstants.ChunkText;
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
