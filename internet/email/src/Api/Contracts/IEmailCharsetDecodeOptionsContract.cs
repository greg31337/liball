using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCharsetDecodeOptions"/>.
/// </summary>
public interface IEmailCharsetDecodeOptionsContract
{
    /// <summary>
    /// Gets the fallback policy.
    /// </summary>
    /// <returns>The current fallback policy.</returns>
    EmailCharsetFallbackPolicy GetFallbackPolicy();
    /// <summary>
    /// Sets the fallback policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetFallbackPolicy(EmailCharsetFallbackPolicy value);
    /// <summary>
    /// Gets the BOM policy.
    /// </summary>
    /// <returns>The current BOM policy.</returns>
    EmailCharsetBomPolicy GetBomPolicy();
    /// <summary>
    /// Sets the BOM policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetBomPolicy(EmailCharsetBomPolicy value);
    /// <summary>
    /// Gets the replacement.
    /// </summary>
    /// <returns>The current replacement.</returns>
    string GetReplacement();
    /// <summary>
    /// Sets the replacement used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetReplacement(string value);
    /// <summary>
    /// Gets the maximum characters.
    /// </summary>
    /// <returns>The current maximum characters.</returns>
    int GetMaximumCharacters();
    /// <summary>
    /// Sets the maximum characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumCharacters(int value);
    /// <summary>
    /// Gets the maximum charset name length.
    /// </summary>
    /// <returns>The current maximum charset name length.</returns>
    int GetMaximumCharsetNameLength();
    /// <summary>
    /// Sets the maximum charset name length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumCharsetNameLength(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailCharsetDecodeOptions Clone();
}
