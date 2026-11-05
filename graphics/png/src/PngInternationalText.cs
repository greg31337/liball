using System.IO;
using System.Text;

namespace png;

internal class PngInternationalTextParser
{
    internal const int MinimumKeywordLength = PngTextKeywordValidator.MinimumKeywordLength;
    internal const int MaximumKeywordLength = PngTextKeywordValidator.MaximumKeywordLength;
    internal const byte CompressionFlagUncompressed = 0;
    internal const byte CompressionFlagCompressed = 1;
    internal const byte CompressionMethodDeflate = 0;

    internal static PngInternationalText Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length, new PngSecurityLimits());
    }

    internal static PngInternationalText Parse(byte[] data, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length, limits);
    }

    internal static PngInternationalText Parse(byte[] data, int offset, int length)
    {
        return Parse(data, offset, length, new PngSecurityLimits());
    }

    internal static PngInternationalText Parse(byte[] data, int offset, int length, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(limits);

        ValidateRange(data, offset, length);
        int end = offset + length;
        int keywordSeparatorOffset = FindSeparator(data, offset, end, "iTXt data must include a keyword separator.");
        int keywordLength = keywordSeparatorOffset - offset;
        ValidateKeywordLength(keywordLength);
        ValidateKeyword(data, offset, keywordLength);

        int compressionFlagOffset = keywordSeparatorOffset + 1;
        if (compressionFlagOffset >= end)
        {
            throw new InvalidDataException("iTXt data must include a compression flag byte.");
        }

        int compressionMethodOffset = compressionFlagOffset + 1;
        if (compressionMethodOffset >= end)
        {
            throw new InvalidDataException("iTXt data must include a compression method byte.");
        }

        byte compressionFlag = data[compressionFlagOffset];
        byte compressionMethod = data[compressionMethodOffset];
        ValidateCompressionFlag(compressionFlag);
        ValidateCompressionMethod(compressionFlag, compressionMethod);

        int languageTagOffset = compressionMethodOffset + 1;
        int languageTagSeparatorOffset = FindSeparator(data, languageTagOffset, end, "iTXt data must include a language tag separator.");
        int languageTagLength = languageTagSeparatorOffset - languageTagOffset;
        ValidateAsciiBytes(data, languageTagOffset, languageTagLength, "iTXt language tag must contain ASCII bytes.");

        int translatedKeywordOffset = languageTagSeparatorOffset + 1;
        int translatedKeywordSeparatorOffset = FindSeparator(data, translatedKeywordOffset, end, "iTXt data must include a translated keyword separator.");
        int translatedKeywordLength = translatedKeywordSeparatorOffset - translatedKeywordOffset;

        int textOffset = translatedKeywordSeparatorOffset + 1;
        int textLength = end - textOffset;
        byte[] textBytes = DecodeTextBytes(data, textOffset, textLength, compressionFlag, limits);
        ValidateNoNullBytes(data, translatedKeywordOffset, translatedKeywordLength, "iTXt translated keyword must not contain a null character.");
        ValidateNoNullBytes(textBytes, 0, textBytes.Length, "iTXt text must not contain a null character.");

        PngInternationalText text = new PngInternationalText();
        text.Keyword = DecodeLatin1(data, offset, keywordLength);
        text.CompressionFlag = compressionFlag;
        text.CompressionMethod = compressionMethod;
        text.LanguageTag = DecodeLatin1(data, languageTagOffset, languageTagLength);
        text.TranslatedKeyword = DecodeUtf8(data, translatedKeywordOffset, translatedKeywordLength);
        text.Text = DecodeUtf8(textBytes, 0, textBytes.Length);
        if (compressionFlag == CompressionFlagCompressed)
        {
            text.CompressedText = CopyRange(data, textOffset, textLength);
        }

        return text;
    }

    internal static PngInternationalText Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static PngInternationalText Parse(PngChunk chunk, PngSecurityLimits limits)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data, limits);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkInternationalText)
        {
            throw new InvalidDataException("Chunk type must be iTXt.");
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

    internal static int FindSeparator(byte[] data, int offset, int end, string message)
    {
        int index = offset;
        while (index < end)
        {
            if (data[index] == 0)
            {
                return index;
            }

            index += 1;
        }

        throw new InvalidDataException(message);
    }

    internal static void ValidateKeywordLength(int length)
    {
        if (length < MinimumKeywordLength || length > MaximumKeywordLength)
        {
            throw new InvalidDataException("iTXt keyword must contain 1 through 79 bytes.");
        }
    }

    internal static void ValidateKeyword(byte[] data, int offset, int length)
    {
        if (!PngTextKeywordValidator.IsValidBytes(data, offset, length))
        {
            throw new InvalidDataException("iTXt keyword contains invalid characters or spacing.");
        }
    }

    internal static void ValidateCompressionFlag(byte compressionFlag)
    {
        if (compressionFlag != CompressionFlagUncompressed && compressionFlag != CompressionFlagCompressed)
        {
            throw new InvalidDataException("iTXt compression flag must be 0 or 1.");
        }
    }

    internal static void ValidateCompressionMethod(byte compressionFlag, byte compressionMethod)
    {
        if (compressionFlag == CompressionFlagCompressed && compressionMethod != CompressionMethodDeflate)
        {
            throw new InvalidDataException("iTXt compression method must be 0 when text is compressed.");
        }
    }

    internal static byte[] DecodeTextBytes(byte[] data, int offset, int length, byte compressionFlag, PngSecurityLimits limits)
    {
        if (compressionFlag == CompressionFlagCompressed)
        {
            return PngZlibDecompressor.Decompress(data, offset, length, limits);
        }

        return CopyRange(data, offset, length);
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

    internal static void ValidateAsciiBytes(byte[] data, int offset, int length, string message)
    {
        int index = 0;
        while (index < length)
        {
            if (data[offset + index] > 127)
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

    internal static string DecodeUtf8(byte[] data, int offset, int length)
    {
        UTF8Encoding encoding = new UTF8Encoding(false, true);
        return encoding.GetString(data, offset, length);
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

internal class PngInternationalTextWriter
{
    internal static byte[] WriteData(PngInternationalText text)
    {
        return WriteData(text, new PngSecurityLimits());
    }

    internal static byte[] WriteData(PngInternationalText text, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(text.Keyword);
        ArgumentNullException.ThrowIfNull(text.LanguageTag);
        ArgumentNullException.ThrowIfNull(text.TranslatedKeyword);
        ArgumentNullException.ThrowIfNull(text.Text);
        ArgumentNullException.ThrowIfNull(limits);

        ValidateCompressionFlag(text.CompressionFlag);
        ValidateCompressionMethod(text.CompressionMethod);
        byte[] keyword = EncodeKeyword(text.Keyword);
        byte[] languageTag = EncodeLanguageTag(text.LanguageTag);
        byte[] translatedKeyword = EncodeUtf8Field(text.TranslatedKeyword, "TranslatedKeyword");
        byte[] value = EncodeUtf8Field(text.Text, "Text");
        byte[] storedText = CreateStoredText(value, text.CompressionFlag, limits);
        return CreateData(keyword, text.CompressionFlag, text.CompressionMethod, languageTag, translatedKeyword, storedText);
    }

    internal static void WriteData(byte[] data, int offset, PngInternationalText text)
    {
        WriteData(data, offset, text, new PngSecurityLimits());
    }

    internal static void WriteData(byte[] data, int offset, PngInternationalText text, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);

        byte[] payload = WriteData(text, limits);
        ValidateWriteRange(data, offset, payload.Length);
        CopyBytes(payload, 0, data, offset, payload.Length);
    }

    internal static PngChunk WriteChunk(PngInternationalText text)
    {
        return CreateChunk(WriteData(text));
    }

    internal static PngChunk WriteChunk(PngInternationalText text, PngSecurityLimits limits)
    {
        return CreateChunk(WriteData(text, limits));
    }

    internal static void ValidateCompressionFlag(byte compressionFlag)
    {
        if (compressionFlag != PngInternationalTextParser.CompressionFlagUncompressed && compressionFlag != PngInternationalTextParser.CompressionFlagCompressed)
        {
            throw new ArgumentOutOfRangeException("CompressionFlag");
        }
    }

    internal static void ValidateCompressionMethod(byte compressionMethod)
    {
        if (compressionMethod != PngInternationalTextParser.CompressionMethodDeflate)
        {
            throw new ArgumentOutOfRangeException("CompressionMethod");
        }
    }

    internal static byte[] EncodeKeyword(string keyword)
    {
        PngTextKeywordValidator.Validate(keyword);
        byte[] data = EncodeLatin1(keyword, "Keyword");
        if (data.Length < PngInternationalTextParser.MinimumKeywordLength || data.Length > PngInternationalTextParser.MaximumKeywordLength)
        {
            throw new ArgumentOutOfRangeException("Keyword");
        }

        return data;
    }

    internal static byte[] EncodeLanguageTag(string languageTag)
    {
        byte[] data = new byte[languageTag.Length];
        int index = 0;
        while (index < languageTag.Length)
        {
            char character = languageTag[index];
            if (character == '\0' || character > 127)
            {
                throw new ArgumentOutOfRangeException("LanguageTag");
            }

            data[index] = (byte)character;
            index += 1;
        }

        return data;
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

    internal static byte[] EncodeUtf8Field(string value, string name)
    {
        ValidateNoNullCharacters(value, name);
        UTF8Encoding encoding = new UTF8Encoding(false, true);
        return encoding.GetBytes(value);
    }

    internal static void ValidateNoNullCharacters(string value, string name)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\0')
            {
                throw new ArgumentOutOfRangeException(name);
            }

            index += 1;
        }
    }

    internal static byte[] CreateStoredText(byte[] value, byte compressionFlag, PngSecurityLimits limits)
    {
        if (compressionFlag == PngInternationalTextParser.CompressionFlagCompressed)
        {
            return PngZlibCompressor.Compress(value, limits);
        }

        return value;
    }

    internal static byte[] CreateData(byte[] keyword, byte compressionFlag, byte compressionMethod, byte[] languageTag, byte[] translatedKeyword, byte[] text)
    {
        int length = keyword.Length + 5 + languageTag.Length + translatedKeyword.Length + text.Length;
        byte[] data = new byte[length];
        int offset = 0;
        CopyBytes(keyword, 0, data, offset, keyword.Length);
        offset += keyword.Length;
        data[offset] = 0;
        offset += 1;
        data[offset] = compressionFlag;
        offset += 1;
        data[offset] = compressionMethod;
        offset += 1;
        CopyBytes(languageTag, 0, data, offset, languageTag.Length);
        offset += languageTag.Length;
        data[offset] = 0;
        offset += 1;
        CopyBytes(translatedKeyword, 0, data, offset, translatedKeyword.Length);
        offset += translatedKeyword.Length;
        data[offset] = 0;
        offset += 1;
        CopyBytes(text, 0, data, offset, text.Length);
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
        chunk.Type = PngConstants.ChunkInternationalText;
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
