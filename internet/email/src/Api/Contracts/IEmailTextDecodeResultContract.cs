using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTextDecodeResult"/>.
/// </summary>
public interface IEmailTextDecodeResultContract
{
    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    string GetText();
    /// <summary>
    /// Determines whether the declared charset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the declared charset is available; otherwise, <c>false</c>.</returns>
    bool HasDeclaredCharset();
    /// <summary>
    /// Gets the declared charset.
    /// </summary>
    /// <returns>The current declared charset.</returns>
    string? GetDeclaredCharset();
    /// <summary>
    /// Determines whether the selected charset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the selected charset is available; otherwise, <c>false</c>.</returns>
    bool HasSelectedCharset();
    /// <summary>
    /// Gets the selected charset.
    /// </summary>
    /// <returns>The current selected charset.</returns>
    string? GetSelectedCharset();
    /// <summary>
    /// Gets the charset source.
    /// </summary>
    /// <returns>The current charset source.</returns>
    EmailTextCharsetSource GetCharsetSource();
    /// <summary>
    /// Determines whether the BOM is present or available.
    /// </summary>
    /// <returns><c>true</c> when the BOM is available; otherwise, <c>false</c>.</returns>
    bool HasBom();
    /// <summary>
    /// Gets the BOM charset.
    /// </summary>
    /// <returns>The current BOM charset.</returns>
    string? GetBomCharset();
    /// <summary>
    /// Gets the BOM byte count.
    /// </summary>
    /// <returns>The current BOM byte count.</returns>
    int GetBomByteCount();
    /// <summary>
    /// Gets the undecodable bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current undecodable bytes.</returns>
    byte[] GetUndecodableBytes();
    /// <summary>
    /// Determines whether the operation used replacement.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool UsedReplacement();
    /// <summary>
    /// Determines whether the operation or source value was newline normalized.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasNewlineNormalized();
    /// <summary>
    /// Determines whether the operation or source value was unicode normalized.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasUnicodeNormalized();
    /// <summary>
    /// Gets the control character count.
    /// </summary>
    /// <returns>The current control character count.</returns>
    int GetControlCharacterCount();
    /// <summary>
    /// Gets the bidi control count.
    /// </summary>
    /// <returns>The current bidi control count.</returns>
    int GetBidiControlCount();
    /// <summary>
    /// Determines whether the source values were control characters removed.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WereControlCharactersRemoved();
    /// <summary>
    /// Determines whether the source values were bidi controls removed.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WereBidiControlsRemoved();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    bool HasFatalError();
    /// <summary>
    /// Creates a copy of this result with replacement text and corresponding diagnostics.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The text decode result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailTextDecodeResult WithText(string value);
}
