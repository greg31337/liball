namespace unicode;

/// <summary>
/// Provides UTF-16 scalar reading, validation, and replacement operations.
/// </summary>
public interface IUnicodeScalarReader
{
    /// <summary>
    /// Decodes UTF-16 into scalar records, replacing each malformed surrogate sequence with U+FFFD.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeScalarRecord[] ReadAll(string text);

    /// <summary>
    /// Decodes UTF-16 under an explicit malformed-input policy.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="policy">How malformed surrogate subsequences are handled.</param>
    /// <returns>A new array containing the resulting scalar records.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>policy</c> is unsupported.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">Thrown for malformed UTF-16 when <c>policy</c> is strict.</exception>
    UnicodeScalarRecord[] ReadAllWithPolicy(string text, UnicodeMalformedInputPolicy policy);

    /// <summary>
    /// Decodes a UTF-16 span under an explicit malformed-input policy.
    /// </summary>
    /// <param name="text">The UTF-16 code units to process.</param>
    /// <param name="policy">How malformed surrogate subsequences are handled.</param>
    /// <returns>A new array containing the resulting scalar records.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>policy</c> is unsupported.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">Thrown for malformed UTF-16 when <c>policy</c> is strict.</exception>
    UnicodeScalarRecord[] ReadAllSpan(System.ReadOnlySpan<char> text, UnicodeMalformedInputPolicy policy);

    /// <summary>
    /// Decodes UTF-16 scalar records into a caller-provided destination without partial writes.
    /// </summary>
    /// <param name="text">The UTF-16 code units to process.</param>
    /// <param name="destination">The destination for scalar records.</param>
    /// <param name="policy">How malformed surrogate subsequences are handled.</param>
    /// <returns>The write result, including the required destination length.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>policy</c> is unsupported.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">Thrown for malformed UTF-16 when <c>policy</c> is strict.</exception>
    UnicodeBufferResult ReadIntoBuffer(
        System.ReadOnlySpan<char> text,
        System.Span<UnicodeScalarRecord> destination,
        UnicodeMalformedInputPolicy policy);

    /// <summary>
    /// Validates text under the selected profile and returns the prepared value and diagnostics.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeValidationResult Validate(string text);

    /// <summary>
    /// Returns text with malformed UTF-16 sequences replaced by U+FFFD.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ReplaceInvalid(string text);
}
