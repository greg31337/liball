namespace unicode;

/// <summary>
/// Provides PDF text-string encoding and decoding operations.
/// </summary>
public interface IUnicodePdfEncoding
{
    /// <summary>
    /// Uses PDFDocEncoding when every scalar maps exactly and the result does not begin with the UTF-16BE marker; otherwise emits UTF-16BE with a BOM.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    byte[] EncodeTextString(string text);

    /// <summary>
    /// Encodes a PDF text string into a caller-provided byte destination without partial writes.
    /// </summary>
    /// <param name="text">The UTF-16 code units to process.</param>
    /// <param name="destination">The destination for the encoded PDF text string.</param>
    /// <returns>The write result, including the required byte length.</returns>
    UnicodeBufferResult EncodeTextStringBuffer(
        System.ReadOnlySpan<char> text,
        System.Span<byte> destination);

    /// <summary>
    /// Decodes a PDF text string as UTF-16BE when it has a BOM, otherwise as PDFDocEncoding.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    string DecodeTextString(byte[] bytes);

    /// <summary>
    /// Selects PDFDocEncoding when lossless and unambiguous, and UTF-16BE with BOM otherwise.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting classification or status.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodePdfTextEncoding GetTextStringEncoding(string text);

    /// <summary>
    /// Encodes text as UTF-16BE and prefixes the two-byte PDF byte-order mark.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    byte[] EncodeUtf16BeWithBom(string text);

    /// <summary>
    /// Encodes UTF-16BE with a PDF byte-order mark into a caller-provided byte destination without partial writes.
    /// </summary>
    /// <param name="text">The UTF-16 code units to process.</param>
    /// <param name="destination">The destination for the byte-order mark and encoded bytes.</param>
    /// <returns>The write result, including the required byte length.</returns>
    UnicodeBufferResult EncodeUtf16BeWithBomBuffer(
        System.ReadOnlySpan<char> text,
        System.Span<byte> destination);

    /// <summary>
    /// Reports whether every scalar in the text has an exact PDFDocEncoding byte mapping.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    bool CanEncodePdfDocEncoding(string text);

    /// <summary>
    /// Encodes text with the single-byte PDFDocEncoding mapping and rejects unmappable scalars.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values do not form a valid input.</exception>
    byte[] EncodePdfDocEncoding(string text);

    /// <summary>
    /// Encodes PDFDocEncoding into a caller-provided byte destination without partial writes.
    /// </summary>
    /// <param name="text">The UTF-16 code units to process.</param>
    /// <param name="destination">The destination for encoded bytes.</param>
    /// <returns>The write result, including the required byte length.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values do not form a valid input.</exception>
    UnicodeBufferResult EncodePdfDocEncodingBuffer(
        System.ReadOnlySpan<char> text,
        System.Span<byte> destination);

    /// <summary>
    /// Maps each PDFDocEncoding byte to its Unicode scalar and returns a .NET string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    string DecodePdfDocEncoding(byte[] bytes);
}
