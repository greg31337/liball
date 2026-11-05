namespace uri;

/// <summary>
/// Validates, encodes, decodes, and normalizes percent-encoded data.
/// </summary>
public interface IPercentCodec
{
    /// <summary>Validates every percent triplet in text.</summary>
    /// <param name="text">The text to validate.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The first diagnostic, or <c>null</c> when all triplets are valid.</returns>
    UriDiagnostic? ValidatePercentEncoding(string text, PercentCodecOptions options);

    /// <summary>Encodes Unicode text as strict UTF-8 percent encoding.</summary>
    /// <param name="text">The Unicode text to encode.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The encoded text.</returns>
    /// <exception cref="PercentEncodingException">Encoding or a configured limit fails.</exception>
    string EncodeText(string text, PercentCodecOptions options);

    /// <summary>Percent-encodes arbitrary bytes without interpreting them as text.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The encoded text.</returns>
    /// <exception cref="PercentEncodingException">Encoding or a configured limit fails.</exception>
    string EncodeBytes(byte[] bytes, PercentCodecOptions options);

    /// <summary>Encodes a span of Unicode text as strict UTF-8 percent encoding.</summary>
    /// <param name="text">The Unicode text span to encode.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The encoded text.</returns>
    /// <exception cref="PercentEncodingException">Encoding or a configured limit fails.</exception>
    string EncodeSpan(System.ReadOnlySpan<char> text, PercentCodecOptions options);

    /// <summary>Decodes percent-encoded data to bytes without choosing a character set.</summary>
    /// <param name="text">The percent-encoded text.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The byte-decoding outcome.</returns>
    PercentDecodeResult DecodeBytes(string text, PercentCodecOptions options);

    /// <summary>Decodes percent-encoded octets to Unicode according to the selected UTF-8 mode.</summary>
    /// <param name="text">The percent-encoded text.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The text-decoding outcome.</returns>
    PercentDecodeResult DecodeText(string text, PercentCodecOptions options);

    /// <summary>Uppercases triplet hex digits and decodes only percent-encoded unreserved characters.</summary>
    /// <param name="text">The percent-encoded text to normalize.</param>
    /// <param name="options">The processing options.</param>
    /// <returns>The normalized text.</returns>
    /// <exception cref="PercentEncodingException">Normalization or a configured limit fails.</exception>
    string NormalizePercentEncoding(string text, PercentCodecOptions options);
}
