namespace unicode;

/// <summary>
/// Provides conversions between .NET text and standard Unicode byte encodings.
/// </summary>
public interface IUnicodeByteEncoding
{
    /// <summary>
    /// Encodes text using the selected byte encoding and malformed-input policy.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="encoding">The byte encoding and byte order to use.</param>
    /// <param name="policy">How malformed UTF-16 is handled.</param>
    /// <returns>A new byte array containing the encoded value without an emitted byte-order mark.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an enum value is unsupported.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">Thrown for malformed UTF-16 under strict policy.</exception>
    byte[] EncodeWithPolicy(
        string text,
        UnicodeByteEncodingKind encoding,
        UnicodeMalformedInputPolicy policy);

    /// <summary>
    /// Decodes bytes using the selected byte encoding and malformed-input policy.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <param name="encoding">The byte encoding and byte order to use.</param>
    /// <param name="policy">How malformed bytes are handled.</param>
    /// <returns>The resulting .NET UTF-16 string; an input byte-order mark is decoded as U+FEFF.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an enum value is unsupported.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">Thrown for malformed bytes under strict policy.</exception>
    string DecodeWithPolicy(
        byte[] bytes,
        UnicodeByteEncodingKind encoding,
        UnicodeMalformedInputPolicy policy);

    /// <summary>
    /// Encodes text into a caller-provided byte destination without partial writes.
    /// </summary>
    /// <param name="text">The UTF-16 code units to process.</param>
    /// <param name="destination">The destination for encoded bytes.</param>
    /// <param name="encoding">The byte encoding and byte order to use.</param>
    /// <param name="policy">How malformed UTF-16 is handled.</param>
    /// <returns>The write result, including the required byte length.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an enum value is unsupported.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">Thrown for malformed UTF-16 under strict policy.</exception>
    UnicodeBufferResult EncodeBuffer(
        System.ReadOnlySpan<char> text,
        System.Span<byte> destination,
        UnicodeByteEncodingKind encoding,
        UnicodeMalformedInputPolicy policy);

    /// <summary>
    /// Decodes bytes into a caller-provided UTF-16 destination without partial writes.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <param name="destination">The destination for decoded UTF-16 code units.</param>
    /// <param name="encoding">The byte encoding and byte order to use.</param>
    /// <param name="policy">How malformed bytes are handled.</param>
    /// <returns>The write result, including the required UTF-16 length.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an enum value is unsupported.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">Thrown for malformed bytes under strict policy.</exception>
    UnicodeBufferResult DecodeBuffer(
        System.ReadOnlySpan<byte> bytes,
        System.Span<char> destination,
        UnicodeByteEncodingKind encoding,
        UnicodeMalformedInputPolicy policy);

    /// <summary>
    /// Encodes UTF-8 from a .NET UTF-16 string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    byte[] EncodeUtf8(string text);

    /// <summary>
    /// Decodes UTF-8 into a .NET UTF-16 string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    string DecodeUtf8(byte[] bytes);

    /// <summary>
    /// Encodes UTF-16 Le from a .NET UTF-16 string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    byte[] EncodeUtf16Le(string text);

    /// <summary>
    /// Decodes UTF-16 Le into a .NET UTF-16 string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    string DecodeUtf16Le(byte[] bytes);

    /// <summary>
    /// Encodes UTF-16 Be from a .NET UTF-16 string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    byte[] EncodeUtf16Be(string text);

    /// <summary>
    /// Decodes UTF-16 Be into a .NET UTF-16 string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    string DecodeUtf16Be(byte[] bytes);

    /// <summary>
    /// Encodes UTF-32 from a .NET UTF-16 string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    byte[] EncodeUtf32(string text);

    /// <summary>
    /// Decodes UTF-32 into a .NET UTF-16 string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    string DecodeUtf32(byte[] bytes);

    /// <summary>
    /// Encodes UTF-32 Le from a .NET UTF-16 string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    byte[] EncodeUtf32Le(string text);

    /// <summary>
    /// Decodes UTF-32 Le into a .NET UTF-16 string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    string DecodeUtf32Le(byte[] bytes);

    /// <summary>
    /// Encodes UTF-32 Be from a .NET UTF-16 string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    byte[] EncodeUtf32Be(string text);

    /// <summary>
    /// Decodes UTF-32 Be into a .NET UTF-16 string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    string DecodeUtf32Be(byte[] bytes);
}
