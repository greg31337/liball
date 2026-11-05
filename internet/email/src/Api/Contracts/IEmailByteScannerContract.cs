using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailByteScanner"/>.
/// </summary>
public interface IEmailByteScannerContract
{
    /// <summary>
    /// Determines whether the current value or state is ascii.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is ascii; otherwise, <c>false</c>.</returns>
    bool IsAscii(byte value);
    /// <summary>
    /// Determines whether the current value or state is ascii letter.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is ascii letter; otherwise, <c>false</c>.</returns>
    bool IsAsciiLetter(byte value);
    /// <summary>
    /// Determines whether the current value or state is ascii digit.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is ascii digit; otherwise, <c>false</c>.</returns>
    bool IsAsciiDigit(byte value);
    /// <summary>
    /// Determines whether the current value or state is ascii letter or digit.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is ascii letter or digit; otherwise, <c>false</c>.</returns>
    bool IsAsciiLetterOrDigit(byte value);
    /// <summary>
    /// Determines whether the current value or state is wsp.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is wsp; otherwise, <c>false</c>.</returns>
    bool IsWsp(byte value);
    /// <summary>
    /// Determines whether the current value or state is cr.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is cr; otherwise, <c>false</c>.</returns>
    bool IsCr(byte value);
    /// <summary>
    /// Determines whether the current value or state is lf.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is lf; otherwise, <c>false</c>.</returns>
    bool IsLf(byte value);
    /// <summary>
    /// Determines whether the current value or state is control.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is control; otherwise, <c>false</c>.</returns>
    bool IsControl(byte value);
    /// <summary>
    /// Determines whether the current value or state is visible ascii.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is visible ascii; otherwise, <c>false</c>.</returns>
    bool IsVisibleAscii(byte value);
    /// <summary>
    /// Determines whether the current value or state is field name character.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is field name character; otherwise, <c>false</c>.</returns>
    bool IsFieldNameCharacter(byte value);
    /// <summary>
    /// Determines whether the current value or state is atext.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is atext; otherwise, <c>false</c>.</returns>
    bool IsAtext(byte value);
    /// <summary>
    /// Determines whether the current value or state is MIME token character.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is MIME token character; otherwise, <c>false</c>.</returns>
    bool IsMimeTokenCharacter(byte value);
    /// <summary>
    /// Determines whether the current value or state is hex digit.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is hex digit; otherwise, <c>false</c>.</returns>
    bool IsHexDigit(byte value);
    /// <summary>
    /// Converts to the lower ascii.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The byte produced by the operation.</returns>
    byte ToLowerAscii(byte value);
    /// <summary>
    /// Converts to the upper ascii.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The byte produced by the operation.</returns>
    byte ToUpperAscii(byte value);
    /// <summary>
    /// Determines whether this value equals the supplied ascii ignore case.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool EqualsAsciiIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right);
    /// <summary>
    /// Compares the ascii ignore case.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    int CompareAsciiIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right);
    /// <summary>
    /// Starts the s with ascii ignore case.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "prefix">The prefix used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool StartsWithAsciiIgnoreCase(ReadOnlySpan<byte> value, ReadOnlySpan<byte> prefix);
    /// <summary>
    /// Gets the hex value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The current hex value.</returns>
    int GetHexValue(byte value);
    /// <summary>
    /// Decodes the hex pair.
    /// </summary>
    /// <param name = "high">The high used by the operation.</param>
    /// <param name = "low">The low used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    int DecodeHexPair(byte high, byte low);
    /// <summary>
    /// Converts to the upper hex.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The byte produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    byte ToUpperHex(int value);
    /// <summary>
    /// Converts to the lower hex.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The byte produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    byte ToLowerHex(int value);
    /// <summary>
    /// Finds the matching byte.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "delimiter">The delimiter used by the operation.</param>
    /// <param name = "startIndex">The zero-based start index.</param>
    /// <returns>The computed count or size.</returns>
    int FindByte(ReadOnlySpan<byte> value, byte delimiter, int startIndex);
    /// <summary>
    /// Finds the matching sequence.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "delimiter">The delimiter used by the operation.</param>
    /// <param name = "startIndex">The zero-based start index.</param>
    /// <returns>The computed count or size.</returns>
    int FindSequence(ReadOnlySpan<byte> value, ReadOnlySpan<byte> delimiter, int startIndex);
    /// <summary>
    /// Finds the matching sequence ascii ignore case.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "delimiter">The delimiter used by the operation.</param>
    /// <param name = "startIndex">The zero-based start index.</param>
    /// <returns>The computed count or size.</returns>
    int FindSequenceAsciiIgnoreCase(ReadOnlySpan<byte> value, ReadOnlySpan<byte> delimiter, int startIndex);
    /// <summary>
    /// Finds the matching any.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "delimiters">The delimiters used by the operation.</param>
    /// <param name = "startIndex">The zero-based start index.</param>
    /// <returns>The computed count or size.</returns>
    int FindAny(ReadOnlySpan<byte> value, ReadOnlySpan<byte> delimiters, int startIndex);
}
