using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRfc2231DecodedParameter"/>.
/// </summary>
public interface IEmailRfc2231DecodedParameterContract
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    string GetName();
    /// <summary>
    /// Gets the decoded bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current decoded bytes.</returns>
    byte[] GetDecodedBytes();
    /// <summary>
    /// Gets the decoded byte count.
    /// </summary>
    /// <returns>The current decoded byte count.</returns>
    long GetDecodedByteCount();
    /// <summary>
    /// Determines whether the charset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the charset is available; otherwise, <c>false</c>.</returns>
    bool HasCharset();
    /// <summary>
    /// Gets the charset.
    /// </summary>
    /// <returns>The current charset.</returns>
    string? GetCharset();
    /// <summary>
    /// Determines whether the language is present or available.
    /// </summary>
    /// <returns><c>true</c> when the language is available; otherwise, <c>false</c>.</returns>
    bool HasLanguage();
    /// <summary>
    /// Gets the language.
    /// </summary>
    /// <returns>The current language.</returns>
    string? GetLanguage();
    /// <summary>
    /// Determines whether the current value or state is extended.
    /// </summary>
    /// <returns><c>true</c> when the value or state is extended; otherwise, <c>false</c>.</returns>
    bool IsExtended();
    /// <summary>
    /// Determines whether the current value or state is continued.
    /// </summary>
    /// <returns><c>true</c> when the value or state is continued; otherwise, <c>false</c>.</returns>
    bool IsContinued();
    /// <summary>
    /// Gets the segment count.
    /// </summary>
    /// <returns>The current segment count.</returns>
    int GetSegmentCount();
    /// <summary>
    /// Gets the source parameter count.
    /// </summary>
    /// <returns>The current source parameter count.</returns>
    int GetSourceParameterCount();
    /// <summary>
    /// Gets the source parameter.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current source parameter.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailRfc2231Parameter GetSourceParameter(int index);
    /// <summary>
    /// Gets the source parameters.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current source parameters.</returns>
    List<EmailRfc2231Parameter> GetSourceParameters();
}
