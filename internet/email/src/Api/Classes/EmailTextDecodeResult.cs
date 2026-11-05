using System;

namespace email;
/// <summary>
/// Describes the outcome of a text decode operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailTextDecodeResult : IEmailTextDecodeResultContract
{
    string text;
    string? declaredCharset;
    string? selectedCharset;
    EmailTextCharsetSource charsetSource;
    string? bomCharset;
    int bomByteCount;
    byte[] undecodableBytes;
    bool usedReplacement;
    bool newlineNormalized;
    bool unicodeNormalized;
    int controlCharacterCount;
    int bidiControlCount;
    bool controlCharactersRemoved;
    bool bidiControlsRemoved;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new text decode result.
    /// </summary>
    /// <param name = "text">The text used by the operation.</param>
    /// <param name = "declaredCharset">The declared charset used by the operation.</param>
    /// <param name = "selectedCharset">The selected charset used by the operation.</param>
    /// <param name = "charsetSource">The charset source used by the operation.</param>
    /// <param name = "bomCharset">The bom charset used by the operation.</param>
    /// <param name = "bomByteCount">The number of items or bytes to process.</param>
    /// <param name = "undecodableBytes">The source bytes or data to process.</param>
    /// <param name = "usedReplacement">Whether to enable the used replacement behavior.</param>
    /// <param name = "newlineNormalized">Whether to enable the newline normalized behavior.</param>
    /// <param name = "unicodeNormalized">Whether to enable the unicode normalized behavior.</param>
    /// <param name = "controlCharacterCount">The number of items or bytes to process.</param>
    /// <param name = "bidiControlCount">The number of items or bytes to process.</param>
    /// <param name = "controlCharactersRemoved">Whether to enable the control characters removed behavior.</param>
    /// <param name = "bidiControlsRemoved">Whether to enable the bidi controls removed behavior.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailTextDecodeResult(string text, string? declaredCharset, string? selectedCharset, EmailTextCharsetSource charsetSource, string? bomCharset, int bomByteCount, byte[] undecodableBytes, bool usedReplacement, bool newlineNormalized, bool unicodeNormalized, int controlCharacterCount, int bidiControlCount, bool controlCharactersRemoved, bool bidiControlsRemoved, EmailIssueList issues)
    {
        if (text == null)
            throw new ArgumentNullException("text");
        if (charsetSource < EmailTextCharsetSource.None || charsetSource > EmailTextCharsetSource.Fallback)
            throw new ArgumentOutOfRangeException("charsetSource");
        if (selectedCharset == null && charsetSource != EmailTextCharsetSource.None)
            throw new ArgumentException("A charset source requires a selected charset.", "charsetSource");
        if (selectedCharset != null && charsetSource == EmailTextCharsetSource.None)
            throw new ArgumentException("A selected charset requires a charset source.", "selectedCharset");
        if (bomByteCount < 0)
            throw new ArgumentOutOfRangeException("bomByteCount");
        if (undecodableBytes == null)
            throw new ArgumentNullException("undecodableBytes");
        if (controlCharacterCount < 0)
            throw new ArgumentOutOfRangeException("controlCharacterCount");
        if (bidiControlCount < 0)
            throw new ArgumentOutOfRangeException("bidiControlCount");
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.text = text;
        this.declaredCharset = declaredCharset;
        this.selectedCharset = selectedCharset;
        this.charsetSource = charsetSource;
        this.bomCharset = bomCharset;
        this.bomByteCount = bomByteCount;
        this.undecodableBytes = Copy(undecodableBytes);
        this.usedReplacement = usedReplacement;
        this.newlineNormalized = newlineNormalized;
        this.unicodeNormalized = unicodeNormalized;
        this.controlCharacterCount = controlCharacterCount;
        this.bidiControlCount = bidiControlCount;
        this.controlCharactersRemoved = controlCharactersRemoved;
        this.bidiControlsRemoved = bidiControlsRemoved;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    public string GetText()
    {
        return text;
    }

    /// <summary>
    /// Determines whether the declared charset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the declared charset is available; otherwise, <c>false</c>.</returns>
    public bool HasDeclaredCharset()
    {
        return declaredCharset != null;
    }

    /// <summary>
    /// Gets the declared charset.
    /// </summary>
    /// <returns>The current declared charset.</returns>
    public string? GetDeclaredCharset()
    {
        return declaredCharset;
    }

    /// <summary>
    /// Determines whether the selected charset is present or available.
    /// </summary>
    /// <returns><c>true</c> when the selected charset is available; otherwise, <c>false</c>.</returns>
    public bool HasSelectedCharset()
    {
        return selectedCharset != null;
    }

    /// <summary>
    /// Gets the selected charset.
    /// </summary>
    /// <returns>The current selected charset.</returns>
    public string? GetSelectedCharset()
    {
        return selectedCharset;
    }

    /// <summary>
    /// Gets the charset source.
    /// </summary>
    /// <returns>The current charset source.</returns>
    public EmailTextCharsetSource GetCharsetSource()
    {
        return charsetSource;
    }

    /// <summary>
    /// Determines whether the BOM is present or available.
    /// </summary>
    /// <returns><c>true</c> when the BOM is available; otherwise, <c>false</c>.</returns>
    public bool HasBom()
    {
        return bomCharset != null;
    }

    /// <summary>
    /// Gets the BOM charset.
    /// </summary>
    /// <returns>The current BOM charset.</returns>
    public string? GetBomCharset()
    {
        return bomCharset;
    }

    /// <summary>
    /// Gets the BOM byte count.
    /// </summary>
    /// <returns>The current BOM byte count.</returns>
    public int GetBomByteCount()
    {
        return bomByteCount;
    }

    /// <summary>
    /// Gets the undecodable bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current undecodable bytes.</returns>
    public byte[] GetUndecodableBytes()
    {
        return Copy(undecodableBytes);
    }

    /// <summary>
    /// Determines whether the operation used replacement.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool UsedReplacement()
    {
        return usedReplacement;
    }

    /// <summary>
    /// Determines whether the operation or source value was newline normalized.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasNewlineNormalized()
    {
        return newlineNormalized;
    }

    /// <summary>
    /// Determines whether the operation or source value was unicode normalized.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasUnicodeNormalized()
    {
        return unicodeNormalized;
    }

    /// <summary>
    /// Gets the control character count.
    /// </summary>
    /// <returns>The current control character count.</returns>
    public int GetControlCharacterCount()
    {
        return controlCharacterCount;
    }

    /// <summary>
    /// Gets the bidi control count.
    /// </summary>
    /// <returns>The current bidi control count.</returns>
    public int GetBidiControlCount()
    {
        return bidiControlCount;
    }

    /// <summary>
    /// Determines whether the source values were control characters removed.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WereControlCharactersRemoved()
    {
        return controlCharactersRemoved;
    }

    /// <summary>
    /// Determines whether the source values were bidi controls removed.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WereBidiControlsRemoved()
    {
        return bidiControlsRemoved;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    public bool HasFatalError()
    {
        return issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal);
    }

    /// <summary>
    /// Creates a copy of this result with replacement text and corresponding diagnostics.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The text decode result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTextDecodeResult WithText(string value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        return new EmailTextDecodeResult(value, declaredCharset, selectedCharset, charsetSource, bomCharset, bomByteCount, undecodableBytes, usedReplacement, newlineNormalized, unicodeNormalized, controlCharacterCount, bidiControlCount, controlCharactersRemoved, bidiControlsRemoved, issues);
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
