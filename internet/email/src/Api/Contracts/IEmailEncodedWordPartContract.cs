using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailEncodedWordPart"/>.
/// </summary>
public interface IEmailEncodedWordPartContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailEncodedWordPartKind GetKind();
    /// <summary>
    /// Determines whether the current value or state is encoded word.
    /// </summary>
    /// <returns><c>true</c> when the value or state is encoded word; otherwise, <c>false</c>.</returns>
    bool IsEncodedWord();
    /// <summary>
    /// Gets the bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current bytes.</returns>
    byte[] GetBytes();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    long GetByteCount();
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    long GetSourceByteCount();
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
    /// Gets the encoding.
    /// </summary>
    /// <returns>The current encoding.</returns>
    EmailEncodedWordEncoding GetEncoding();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
}
