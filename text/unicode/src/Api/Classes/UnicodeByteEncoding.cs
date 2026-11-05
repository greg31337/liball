namespace unicode;

/// <summary>
/// Converts between .NET UTF-16 strings and standard Unicode byte encodings.
/// </summary>
/// <remarks>
/// Convenience methods use replacement behavior. Policy methods can instead reject malformed input. Encoders do not emit byte-order marks; decoders preserve an input byte-order mark as U+FEFF.
/// </remarks>
public class UnicodeByteEncoding : IUnicodeByteEncoding
{
    internal static readonly UnicodeByteEncoding Instance = new UnicodeByteEncoding();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodeByteEncoding()
    {
    }

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
    public byte[] EncodeWithPolicy(
        string text,
        UnicodeByteEncodingKind encoding,
        UnicodeMalformedInputPolicy policy)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        System.Text.Encoding selectedEncoding = GetEncoding(encoding, policy);
        return selectedEncoding.GetBytes(text);
    }

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
    public string DecodeWithPolicy(
        byte[] bytes,
        UnicodeByteEncodingKind encoding,
        UnicodeMalformedInputPolicy policy)
    {
        System.ArgumentNullException.ThrowIfNull(bytes);

        System.Text.Encoding selectedEncoding = GetEncoding(encoding, policy);
        return selectedEncoding.GetString(bytes);
    }

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
    public UnicodeBufferResult EncodeBuffer(
        System.ReadOnlySpan<char> text,
        System.Span<byte> destination,
        UnicodeByteEncodingKind encoding,
        UnicodeMalformedInputPolicy policy)
    {
        System.Text.Encoding selectedEncoding = GetEncoding(encoding, policy);
        int requiredLength = selectedEncoding.GetByteCount(text);
        if (destination.Length < requiredLength)
        {
            return new UnicodeBufferResult(false, requiredLength, 0);
        }

        int writtenLength = selectedEncoding.GetBytes(text, destination);
        return new UnicodeBufferResult(true, requiredLength, writtenLength);
    }

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
    public UnicodeBufferResult DecodeBuffer(
        System.ReadOnlySpan<byte> bytes,
        System.Span<char> destination,
        UnicodeByteEncodingKind encoding,
        UnicodeMalformedInputPolicy policy)
    {
        System.Text.Encoding selectedEncoding = GetEncoding(encoding, policy);
        int requiredLength = selectedEncoding.GetCharCount(bytes);
        if (destination.Length < requiredLength)
        {
            return new UnicodeBufferResult(false, requiredLength, 0);
        }

        int writtenLength = selectedEncoding.GetChars(bytes, destination);
        return new UnicodeBufferResult(true, requiredLength, writtenLength);
    }
    /// <summary>
    /// Encodes UTF-8 from a .NET UTF-16 string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public byte[] EncodeUtf8(string text)
    {
        return EncodeWithPolicy(text, UnicodeByteEncodingKind.Utf8, UnicodeMalformedInputPolicy.Replace);
    }

    /// <summary>
    /// Decodes UTF-8 into a .NET UTF-16 string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    public string DecodeUtf8(byte[] bytes)
    {
        return DecodeWithPolicy(bytes, UnicodeByteEncodingKind.Utf8, UnicodeMalformedInputPolicy.Replace);
    }

    /// <summary>
    /// Encodes UTF-16 Le from a .NET UTF-16 string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public byte[] EncodeUtf16Le(string text)
    {
        return EncodeWithPolicy(text, UnicodeByteEncodingKind.Utf16Le, UnicodeMalformedInputPolicy.Replace);
    }

    /// <summary>
    /// Decodes UTF-16 Le into a .NET UTF-16 string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    public string DecodeUtf16Le(byte[] bytes)
    {
        return DecodeWithPolicy(bytes, UnicodeByteEncodingKind.Utf16Le, UnicodeMalformedInputPolicy.Replace);
    }

    /// <summary>
    /// Encodes UTF-16 Be from a .NET UTF-16 string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public byte[] EncodeUtf16Be(string text)
    {
        return EncodeWithPolicy(text, UnicodeByteEncodingKind.Utf16Be, UnicodeMalformedInputPolicy.Replace);
    }

    /// <summary>
    /// Decodes UTF-16 Be into a .NET UTF-16 string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    public string DecodeUtf16Be(byte[] bytes)
    {
        return DecodeWithPolicy(bytes, UnicodeByteEncodingKind.Utf16Be, UnicodeMalformedInputPolicy.Replace);
    }

    /// <summary>
    /// Encodes UTF-32 from a .NET UTF-16 string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public byte[] EncodeUtf32(string text)
    {
        return EncodeUtf32Le(text);
    }

    /// <summary>
    /// Decodes UTF-32 into a .NET UTF-16 string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    public string DecodeUtf32(byte[] bytes)
    {
        return DecodeUtf32Le(bytes);
    }

    /// <summary>
    /// Encodes UTF-32 Le from a .NET UTF-16 string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public byte[] EncodeUtf32Le(string text)
    {
        return EncodeWithPolicy(text, UnicodeByteEncodingKind.Utf32Le, UnicodeMalformedInputPolicy.Replace);
    }

    /// <summary>
    /// Decodes UTF-32 Le into a .NET UTF-16 string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    public string DecodeUtf32Le(byte[] bytes)
    {
        return DecodeWithPolicy(bytes, UnicodeByteEncodingKind.Utf32Le, UnicodeMalformedInputPolicy.Replace);
    }

    /// <summary>
    /// Encodes UTF-32 Be from a .NET UTF-16 string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new byte array containing the encoded value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public byte[] EncodeUtf32Be(string text)
    {
        return EncodeWithPolicy(text, UnicodeByteEncodingKind.Utf32Be, UnicodeMalformedInputPolicy.Replace);
    }

    /// <summary>
    /// Decodes UTF-32 Be into a .NET UTF-16 string.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    public string DecodeUtf32Be(byte[] bytes)
    {
        return DecodeWithPolicy(bytes, UnicodeByteEncodingKind.Utf32Be, UnicodeMalformedInputPolicy.Replace);
    }

    internal static System.Text.Encoding GetEncoding(
        UnicodeByteEncodingKind encoding,
        UnicodeMalformedInputPolicy policy)
    {
        UnicodeScalarReader.ValidatePolicy(policy);
        bool strict = policy == UnicodeMalformedInputPolicy.Strict;

        if (encoding == UnicodeByteEncodingKind.Utf8)
        {
            return new System.Text.UTF8Encoding(false, strict);
        }

        if (encoding == UnicodeByteEncodingKind.Utf16Le)
        {
            return new System.Text.UnicodeEncoding(false, false, strict);
        }

        if (encoding == UnicodeByteEncodingKind.Utf16Be)
        {
            return new System.Text.UnicodeEncoding(true, false, strict);
        }

        if (encoding == UnicodeByteEncodingKind.Utf32Le)
        {
            return new System.Text.UTF32Encoding(false, false, strict);
        }

        if (encoding == UnicodeByteEncodingKind.Utf32Be)
        {
            return new System.Text.UTF32Encoding(true, false, strict);
        }

        throw new System.ArgumentOutOfRangeException("encoding");
    }
}
