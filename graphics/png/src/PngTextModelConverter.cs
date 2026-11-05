namespace png;

internal class PngTextModelConverter
{
    internal static PngTextEntry CreateLatin1Entry(PngText source, int originalChunkIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        ValidateOriginalChunkIndex(originalChunkIndex);

        PngTextEntry result = CreateEntry(PngTextKind.Latin1, originalChunkIndex, source.Keyword, source.Text);
        return result;
    }

    internal static PngTextEntry CreateCompressedLatin1Entry(PngCompressedText source, int originalChunkIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(source.CompressedText);
        ValidateOriginalChunkIndex(originalChunkIndex);

        PngTextEntry result = CreateEntry(PngTextKind.CompressedLatin1, originalChunkIndex, source.Keyword, source.Text);
        result.CompressionFlag = 1;
        result.CompressionMethod = source.CompressionMethod;
        result.CompressedText = CopyBytes(source.CompressedText);
        return result;
    }

    internal static PngTextEntry CreateInternationalEntry(PngInternationalText source, int originalChunkIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(source.CompressedText);
        ValidateOriginalChunkIndex(originalChunkIndex);

        PngTextEntry result = CreateEntry(PngTextKind.InternationalUtf8, originalChunkIndex, source.Keyword, source.Text);
        result.CompressionFlag = source.CompressionFlag;
        result.CompressionMethod = source.CompressionMethod;
        result.LanguageTag = source.LanguageTag;
        result.TranslatedKeyword = source.TranslatedKeyword;
        result.CompressedText = CopyBytes(source.CompressedText);
        return result;
    }

    internal static PngTextEntry CreateEntry(PngTextKind kind, int originalChunkIndex, string keyword, string text)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        ArgumentNullException.ThrowIfNull(text);

        PngTextEntry result = new PngTextEntry();
        result.Kind = kind;
        result.OriginalChunkIndex = originalChunkIndex;
        result.Keyword = keyword;
        result.Text = text;
        return result;
    }

    internal static void ValidateOriginalChunkIndex(int originalChunkIndex)
    {
        if (originalChunkIndex < 0)
        {
            throw new ArgumentOutOfRangeException("originalChunkIndex");
        }
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index += 1;
        }

        return result;
    }
}
