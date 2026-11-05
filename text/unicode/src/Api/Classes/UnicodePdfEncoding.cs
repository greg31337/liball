namespace unicode;

/// <summary>
/// Encodes and decodes PDF text strings using PDFDocEncoding or UTF-16BE.
/// </summary>
public class UnicodePdfEncoding : IUnicodePdfEncoding
{
    internal static readonly UnicodePdfEncoding Instance = new UnicodePdfEncoding();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodePdfEncoding()
    {
    }
    /// <summary>
    /// Uses PDFDocEncoding when every scalar maps exactly and the result does not begin with the UTF-16BE marker; otherwise emits UTF-16BE with a BOM.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public byte[] EncodeTextString(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        int pdfDocEncodingLength = GetPdfTextStringEncodingLength(text);
        int requiredLength = pdfDocEncodingLength;
        if (requiredLength < 0)
        {
            requiredLength = GetUtf16BeWithBomLength(text);
        }

        byte[] bytes = new byte[requiredLength];
        EncodeTextStringBuffer(text, bytes);
        return bytes;
    }

    /// <summary>
    /// Encodes a PDF text string into a caller-provided byte destination without partial writes.
    /// </summary>
    /// <param name="text">The UTF-16 code units to process.</param>
    /// <param name="destination">The destination for the encoded PDF text string.</param>
    /// <returns>The write result, including the required byte length.</returns>
    public UnicodeBufferResult EncodeTextStringBuffer(
        System.ReadOnlySpan<char> text,
        System.Span<byte> destination)
    {
        int pdfDocEncodingLength = GetPdfTextStringEncodingLength(text);
        if (pdfDocEncodingLength >= 0)
        {
            if (destination.Length < pdfDocEncodingLength)
            {
                return new UnicodeBufferResult(false, pdfDocEncodingLength, 0);
            }

            WritePdfDocEncoding(text, destination);
            return new UnicodeBufferResult(true, pdfDocEncodingLength, pdfDocEncodingLength);
        }

        int requiredLength = GetUtf16BeWithBomLength(text);
        if (destination.Length < requiredLength)
        {
            return new UnicodeBufferResult(false, requiredLength, 0);
        }

        WriteUtf16BeWithBom(text, destination);
        return new UnicodeBufferResult(true, requiredLength, requiredLength);
    }

    /// <summary>
    /// Decodes a PDF text string as UTF-16BE when it has a BOM, otherwise as PDFDocEncoding.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    public string DecodeTextString(byte[] bytes)
    {
        System.ArgumentNullException.ThrowIfNull(bytes);

        if (HasUtf16BeBom(bytes))
        {
            return DecodeUtf16BePayload(bytes, 2);
        }

        return DecodePdfDocEncoding(bytes);
    }

    /// <summary>
    /// Selects PDFDocEncoding when lossless and unambiguous, and UTF-16BE with BOM otherwise.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting classification or status.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodePdfTextEncoding GetTextStringEncoding(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (GetPdfTextStringEncodingLength(text) >= 0)
        {
            return UnicodePdfTextEncoding.PdfDocEncoding;
        }

        return UnicodePdfTextEncoding.Utf16BeWithBom;
    }

    /// <summary>
    /// Encodes text as UTF-16BE and prefixes the two-byte PDF byte-order mark.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public byte[] EncodeUtf16BeWithBom(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        byte[] bytes = new byte[GetUtf16BeWithBomLength(text)];
        EncodeUtf16BeWithBomBuffer(text, bytes);
        return bytes;
    }

    /// <summary>
    /// Encodes UTF-16BE with a PDF byte-order mark into a caller-provided byte destination without partial writes.
    /// </summary>
    /// <param name="text">The UTF-16 code units to process.</param>
    /// <param name="destination">The destination for the byte-order mark and encoded bytes.</param>
    /// <returns>The write result, including the required byte length.</returns>
    public UnicodeBufferResult EncodeUtf16BeWithBomBuffer(
        System.ReadOnlySpan<char> text,
        System.Span<byte> destination)
    {
        int requiredLength = GetUtf16BeWithBomLength(text);
        if (destination.Length < requiredLength)
        {
            return new UnicodeBufferResult(false, requiredLength, 0);
        }

        WriteUtf16BeWithBom(text, destination);
        return new UnicodeBufferResult(true, requiredLength, requiredLength);
    }

    internal static byte[] EncodeUtf16BePayload(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        string scalarSafe = UnicodeScalarReader.Instance.ReplaceInvalid(text);
        byte[] bytes = new byte[scalarSafe.Length * 2];
        int index = 0;
        while (index < scalarSafe.Length)
        {
            WriteUtf16BeCodeUnit(bytes, index * 2, scalarSafe[index]);
            index = index + 1;
        }

        return bytes;
    }

    internal static bool HasUtf16BeBom(byte[] bytes)
    {
        System.ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length < 2)
        {
            return false;
        }

        if (bytes[0] != 0xFE)
        {
            return false;
        }

        return bytes[1] == 0xFF;
    }

    internal static string DecodeUtf16BePayload(byte[] bytes, int offset)
    {
        System.ArgumentNullException.ThrowIfNull(bytes);

        if (offset < 0)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (offset > bytes.Length)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder((bytes.Length - offset) / 2);
        int index = offset;
        while (bytes.Length - index >= 2)
        {
            int codeUnit = ReadUtf16BeCodeUnit(bytes, index);
            builder.Append((char)codeUnit);
            index = index + 2;
        }

        if (index < bytes.Length)
        {
            builder.Append(UnicodeScalar.Instance.ToUtf16String(UnicodeScalar.ReplacementCharacter));
        }

        return UnicodeScalarReader.Instance.ReplaceInvalid(builder.ToString());
    }

    internal static int ReadUtf16BeCodeUnit(byte[] bytes, int offset)
    {
        System.ArgumentNullException.ThrowIfNull(bytes);

        if (offset < 0)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (offset >= bytes.Length)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (bytes.Length - offset < 2)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        return (bytes[offset] << 8) | bytes[offset + 1];
    }

    internal static void WriteUtf16BeCodeUnit(byte[] bytes, int offset, int codeUnit)
    {
        System.ArgumentNullException.ThrowIfNull(bytes);

        if (offset < 0)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (offset >= bytes.Length)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (bytes.Length - offset < 2)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (codeUnit < 0)
        {
            throw new System.ArgumentOutOfRangeException("codeUnit");
        }

        if (codeUnit > 0xFFFF)
        {
            throw new System.ArgumentOutOfRangeException("codeUnit");
        }

        bytes[offset] = (byte)(codeUnit >> 8);
        bytes[offset + 1] = (byte)(codeUnit & 0xFF);
    }

    /// <summary>
    /// Reports whether every scalar in the text has an exact PDFDocEncoding byte mapping.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public bool CanEncodePdfDocEncoding(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return GetPdfDocEncodingLength(text) >= 0;
    }

    /// <summary>
    /// Encodes text with the single-byte PDFDocEncoding mapping and rejects unmappable scalars.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values do not form a valid input.</exception>
    public byte[] EncodePdfDocEncoding(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        int requiredLength = GetPdfDocEncodingLength(text);
        if (requiredLength < 0)
        {
            throw new System.ArgumentException("Text contains invalid or unmappable input for PDFDocEncoding.", "text");
        }

        byte[] bytes = new byte[requiredLength];
        EncodePdfDocEncodingBuffer(text, bytes);
        return bytes;
    }

    /// <summary>
    /// Encodes PDFDocEncoding into a caller-provided byte destination without partial writes.
    /// </summary>
    /// <param name="text">The UTF-16 code units to process.</param>
    /// <param name="destination">The destination for encoded bytes.</param>
    /// <returns>The write result, including the required byte length.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values do not form a valid input.</exception>
    public UnicodeBufferResult EncodePdfDocEncodingBuffer(
        System.ReadOnlySpan<char> text,
        System.Span<byte> destination)
    {
        int requiredLength = GetPdfDocEncodingLength(text);
        if (requiredLength < 0)
        {
            throw new System.ArgumentException("Text contains invalid or unmappable input for PDFDocEncoding.", "text");
        }

        if (destination.Length < requiredLength)
        {
            return new UnicodeBufferResult(false, requiredLength, 0);
        }

        WritePdfDocEncoding(text, destination);
        return new UnicodeBufferResult(true, requiredLength, requiredLength);
    }

    /// <summary>
    /// Maps each PDFDocEncoding byte to its Unicode scalar and returns a .NET string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    public string DecodePdfDocEncoding(byte[] bytes)
    {
        System.ArgumentNullException.ThrowIfNull(bytes);

        int[] table = UnicodePdfEncodingTables.PdfDocEncodingToUnicode();
        System.Text.StringBuilder builder = new System.Text.StringBuilder(bytes.Length);
        int index = 0;
        while (index < bytes.Length)
        {
            builder.Append(UnicodeScalar.Instance.ToUtf16String(table[bytes[index]]));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static int FindPdfDocEncodingByte(int codePoint)
    {
        UnicodeRange[] identityRanges = UnicodePdfEncodingTables.PdfDocEncodingIdentityRanges();
        if (UnicodeRangeSet.ContainsRanges(identityRanges, codePoint))
        {
            return codePoint;
        }

        return UnicodeTableLookup.LookupRangeValue(
            UnicodePdfEncodingTables.PdfDocEncodingUnicodeToByte(),
            codePoint,
            -1);
    }

    internal static int GetPdfDocEncodingLength(System.ReadOnlySpan<char> text)
    {
        int index = 0;
        while (index < text.Length)
        {
            int codePoint = text[index];
            if (codePoint >= 0xD800)
            {
                if (codePoint <= 0xDFFF)
                {
                    return -1;
                }
            }

            if (FindPdfDocEncodingByte(codePoint) < 0)
            {
                return -1;
            }

            index = index + 1;
        }

        return text.Length;
    }

    internal static int GetPdfTextStringEncodingLength(System.ReadOnlySpan<char> text)
    {
        int requiredLength = GetPdfDocEncodingLength(text);
        if (requiredLength < 2)
        {
            return requiredLength;
        }

        if (FindPdfDocEncodingByte(text[0]) != 0xFE)
        {
            return requiredLength;
        }

        if (FindPdfDocEncodingByte(text[1]) == 0xFF)
        {
            return -1;
        }

        return requiredLength;
    }

    internal static int GetUtf16BeWithBomLength(System.ReadOnlySpan<char> text)
    {
        if (text.Length > (int.MaxValue - 2) / 2)
        {
            throw new System.ArgumentException("Text is too long to encode.", "text");
        }

        return (text.Length * 2) + 2;
    }

    internal static void WritePdfDocEncoding(
        System.ReadOnlySpan<char> text,
        System.Span<byte> destination)
    {
        int index = 0;
        while (index < text.Length)
        {
            destination[index] = (byte)FindPdfDocEncodingByte(text[index]);
            index = index + 1;
        }
    }

    internal static void WriteUtf16BeWithBom(
        System.ReadOnlySpan<char> text,
        System.Span<byte> destination)
    {
        destination[0] = 0xFE;
        destination[1] = 0xFF;

        int sourceIndex = 0;
        int destinationIndex = 2;
        while (sourceIndex < text.Length)
        {
            int codeUnit = text[sourceIndex];
            if (codeUnit >= 0xD800 && codeUnit <= 0xDBFF)
            {
                if (sourceIndex + 1 < text.Length)
                {
                    int lowSurrogate = text[sourceIndex + 1];
                    if (lowSurrogate >= 0xDC00 && lowSurrogate <= 0xDFFF)
                    {
                        WriteUtf16BeSpanCodeUnit(destination, destinationIndex, codeUnit);
                        WriteUtf16BeSpanCodeUnit(destination, destinationIndex + 2, lowSurrogate);
                        sourceIndex = sourceIndex + 2;
                        destinationIndex = destinationIndex + 4;
                        continue;
                    }
                }

                codeUnit = UnicodeScalar.ReplacementCharacter;
            }
            else if (codeUnit >= 0xDC00 && codeUnit <= 0xDFFF)
            {
                codeUnit = UnicodeScalar.ReplacementCharacter;
            }

            WriteUtf16BeSpanCodeUnit(destination, destinationIndex, codeUnit);
            sourceIndex = sourceIndex + 1;
            destinationIndex = destinationIndex + 2;
        }
    }

    internal static void WriteUtf16BeSpanCodeUnit(
        System.Span<byte> destination,
        int offset,
        int codeUnit)
    {
        destination[offset] = (byte)(codeUnit >> 8);
        destination[offset + 1] = (byte)(codeUnit & 0xFF);
    }
}
