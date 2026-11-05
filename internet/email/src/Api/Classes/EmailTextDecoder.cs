using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Incrementally decodes text.
/// </summary>
public class EmailTextDecoder : IEmailTextDecoderContract
{
    internal static EmailTextDecodeResult DecodeImplementationWithEntity(MimeEntity entity)
    {
        return DecodeImplementationWithEntityAndOptionsAndPath(entity, new EmailTextDecodeOptions(), null);
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTextDecodeResult DecodeWithEntity(MimeEntity entity)
    {
        return EmailTextDecoder.DecodeImplementationWithEntity(entity);
    }

    internal static EmailTextDecodeResult DecodeImplementationWithEntityAndOptions(MimeEntity entity, EmailTextDecodeOptions options)
    {
        return DecodeImplementationWithEntityAndOptionsAndPath(entity, options, null);
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTextDecodeResult DecodeWithEntityAndOptions(MimeEntity entity, EmailTextDecodeOptions options)
    {
        return EmailTextDecoder.DecodeImplementationWithEntityAndOptions(entity, options);
    }

    internal static EmailTextDecodeResult DecodeImplementationWithEntityAndOptionsAndPath(MimeEntity entity, EmailTextDecodeOptions options, EmailPath? path)
    {
        return DecodeImplementationWithEntityAndOptionsAndPathAndLimitNameAndReportedLimitAndReportedObserved(entity, options, path, "MaxDecodedContentBytes", options == null ? 0 : options.GetMaximumDecodedBytes(), 0);
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTextDecodeResult DecodeWithEntityAndOptionsAndPath(MimeEntity entity, EmailTextDecodeOptions options, EmailPath? path)
    {
        return EmailTextDecoder.DecodeImplementationWithEntityAndOptionsAndPath(entity, options, path);
    }

    internal static EmailTextDecodeResult DecodeImplementationWithEntityAndOptionsAndPathAndLimitNameAndReportedLimitAndReportedObserved(MimeEntity entity, EmailTextDecodeOptions options, EmailPath? path, string limitName, long reportedLimit, long reportedObserved)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (options == null)
            throw new ArgumentNullException("options");
        if (!string.Equals((entity.GetMediaType() ?? throw new InvalidOperationException()).GetTopLevelType(), "text", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only text MIME entities can be decoded as text.", "entity");
        EmailTextDecodeOptions snapshot = options.Clone();
        byte[] bytes = ReadDecodedBytes(entity, snapshot.GetMaximumDecodedBytes(), limitName, reportedLimit, reportedObserved);
        string? declaredCharset = GetDeclaredCharset(entity.GetMediaType());
        return DecodeCore(bytes, declaredCharset, snapshot, path);
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "limitName">The limit name used by the operation.</param>
    /// <param name = "reportedLimit">The reported limit used by the operation.</param>
    /// <param name = "reportedObserved">The reported observed used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTextDecodeResult DecodeWithEntityAndOptionsAndPathAndLimitNameAndReportedLimitAndReportedObserved(MimeEntity entity, EmailTextDecodeOptions options, EmailPath? path, string limitName, long reportedLimit, long reportedObserved)
    {
        return EmailTextDecoder.DecodeImplementationWithEntityAndOptionsAndPathAndLimitNameAndReportedLimitAndReportedObserved(entity, options, path, limitName, reportedLimit, reportedObserved);
    }

    internal static EmailTextDecodeResult DecodeImplementationWithAttachment(EmailAttachmentView attachment)
    {
        return DecodeImplementationWithAttachmentAndOptions(attachment, new EmailTextDecodeOptions());
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "attachment">The attachment used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTextDecodeResult DecodeWithAttachment(EmailAttachmentView attachment)
    {
        return EmailTextDecoder.DecodeImplementationWithAttachment(attachment);
    }

    internal static EmailTextDecodeResult DecodeImplementationWithAttachmentAndOptions(EmailAttachmentView attachment, EmailTextDecodeOptions options)
    {
        if (attachment == null)
            throw new ArgumentNullException("attachment");
        return DecodeImplementationWithEntityAndOptionsAndPath(attachment.GetEntity(), options, attachment.GetPath());
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "attachment">The attachment used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTextDecodeResult DecodeWithAttachmentAndOptions(EmailAttachmentView attachment, EmailTextDecodeOptions options)
    {
        return EmailTextDecoder.DecodeImplementationWithAttachmentAndOptions(attachment, options);
    }

    internal static EmailTextDecodeResult DecodeImplementationWithInputAndDeclaredCharset(ReadOnlySpan<byte> input, string declaredCharset)
    {
        return DecodeImplementationWithInputAndDeclaredCharsetAndOptions(input, declaredCharset, new EmailTextDecodeOptions());
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "declaredCharset">The declared charset used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTextDecodeResult DecodeWithInputAndDeclaredCharset(ReadOnlySpan<byte> input, string declaredCharset)
    {
        return EmailTextDecoder.DecodeImplementationWithInputAndDeclaredCharset(input, declaredCharset);
    }

    internal static EmailTextDecodeResult DecodeImplementationWithInputAndDeclaredCharsetAndOptions(ReadOnlySpan<byte> input, string declaredCharset, EmailTextDecodeOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailTextDecodeOptions snapshot = options.Clone();
        if (input.Length > snapshot.GetMaximumDecodedBytes())
            throw new EmailLimitException("MaxDecodedContentBytes", snapshot.GetMaximumDecodedBytes(), input.Length);
        return DecodeCore(input.ToArray(), NormalizeDeclaredCharset(declaredCharset), snapshot, null);
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "declaredCharset">The declared charset used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailTextDecodeResult DecodeWithInputAndDeclaredCharsetAndOptions(ReadOnlySpan<byte> input, string declaredCharset, EmailTextDecodeOptions options)
    {
        return EmailTextDecoder.DecodeImplementationWithInputAndDeclaredCharsetAndOptions(input, declaredCharset, options);
    }

    static EmailTextDecodeResult DecodeCore(byte[] input, string? declaredCharset, EmailTextDecodeOptions options, EmailPath? path)
    {
        EmailTextCharsetSelection selection = SelectCharset(input, declaredCharset, options);
        EmailCharsetDecodeResult decoded = selection.GetResult();
        EmailIssueList issues = decoded.GetIssues();
        AddFallbackIssue(issues, selection, declaredCharset, path);
        EmailTextTransform newline = NormalizeNewlines(decoded.GetText(), options.GetNewlinePolicy());
        if (newline.WasChanged())
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Information, EmailIssueCode.TextNewlineNormalized, "Text line endings were normalized.", null, path, null, "The decoded result uses the requested newline convention; the MIME entity was not changed."));
        }

        EmailTextTransform unicode = NormalizeUnicode(newline.GetText(), options.GetUnicodeNormalization());
        if (unicode.WasChanged())
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Information, EmailIssueCode.TextUnicodeNormalized, "Decoded text was Unicode-normalized.", null, path, null, "The decoded result uses the requested normalization form; the MIME entity was not changed."));
        }

        EmailTextCharacterScan scanned = ScanCharacters(unicode.GetText(), options.GetRemoveControlCharacters(), options.GetRemoveBidiControls());
        if (scanned.GetControlCharacterCount() > 0)
        {
            string controlDescription = "Decoded text contains " + scanned.GetControlCharacterCount().ToString(CultureInfo.InvariantCulture) + " disallowed control character(s).";
            string controlRecovery = options.GetRemoveControlCharacters() ? "The controls were removed from the decoded result; the MIME entity was not changed." : "The controls were retained and should be considered by display policy.";
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.TextControlCharacter, controlDescription, null, path, null, controlRecovery));
        }

        if (scanned.GetBidiControlCount() > 0)
        {
            string bidiDescription = "Decoded text contains " + scanned.GetBidiControlCount().ToString(CultureInfo.InvariantCulture) + " bidirectional formatting control(s).";
            string bidiRecovery = options.GetRemoveBidiControls() ? "The bidi controls were removed from the decoded result; the MIME entity was not changed." : "The bidi controls were retained and should be surfaced or handled by display policy.";
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.TextBidiControl, bidiDescription, null, path, null, bidiRecovery));
        }

        string finalText = ApplyFinalCharacterLimit(scanned.GetText(), options.GetCharsetOptions().GetMaximumCharacters(), issues, path);
        string? bomCharset = decoded.HasBom() ? decoded.GetBomCharset() : null;
        return new EmailTextDecodeResult(finalText, declaredCharset, decoded.HasResolvedCharset() ? decoded.GetResolvedCharset() : null, selection.GetSource(), bomCharset, decoded.GetBomByteCount(), decoded.GetUndecodableBytes(), decoded.UsedReplacement(), newline.WasChanged(), unicode.WasChanged(), scanned.GetControlCharacterCount(), scanned.GetBidiControlCount(), scanned.WereControlCharactersRemoved(), scanned.WereBidiControlsRemoved(), issues);
    }

    static EmailTextCharsetSelection SelectCharset(byte[] input, string? declaredCharset, EmailTextDecodeOptions options)
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetDecodeOptions charsetOptions = options.GetCharsetOptions();
        EmailCharsetDecodeResult first = registry.Decode(input, declaredCharset, charsetOptions);
        if (first.HasResolvedCharset())
        {
            EmailTextCharsetSource firstSource = first.HasBom() ? EmailTextCharsetSource.ByteOrderMark : declaredCharset == null ? EmailTextCharsetSource.None : EmailTextCharsetSource.Declared;
            return new EmailTextCharsetSelection(first, firstSource, false, false);
        }

        if (first.HasFatalError() || !options.HasFallbackCharset())
        {
            return new EmailTextCharsetSelection(first, EmailTextCharsetSource.None, false, false);
        }

        EmailCharsetDecodeResult fallback = registry.Decode(input, options.GetFallbackCharset(), charsetOptions);
        EmailTextCharsetSource fallbackSource = fallback.HasResolvedCharset() ? fallback.HasBom() ? EmailTextCharsetSource.ByteOrderMark : EmailTextCharsetSource.Fallback : EmailTextCharsetSource.None;
        return new EmailTextCharsetSelection(fallback, fallbackSource, true, declaredCharset != null);
    }

    static void AddFallbackIssue(EmailIssueList issues, EmailTextCharsetSelection selection, string? declaredCharset, EmailPath? path)
    {
        if (!selection.UsedFallback())
            return;
        EmailIssueSeverity severity = selection.ReplacedUnsupportedDeclaration() ? EmailIssueSeverity.Warning : EmailIssueSeverity.Information;
        string description = selection.ReplacedUnsupportedDeclaration() ? "The declared charset " + declaredCharset + " is unsupported; the configured fallback charset was used." : "No charset was declared or detected; the configured fallback charset was used.";
        string recovery = selection.GetResult().HasResolvedCharset() ? "Text was decoded with " + selection.GetResult().GetResolvedCharset() + "." : "The configured fallback charset was also unsupported, so no text was produced.";
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.TextCharsetFallbackUsed, description, null, path, "Content-Type", recovery));
    }

    static byte[] ReadDecodedBytes(MimeEntity entity, long maximumBytes, string limitName, long reportedLimit, long reportedObserved)
    {
        using (MemoryStream output = new MemoryStream())
        {
            new EmailDecodedContent().CopyToWithEntityAndOutputAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(entity, output, maximumBytes, limitName, reportedLimit, reportedObserved);
            return output.ToArray();
        }
    }

    static string? GetDeclaredCharset(EmailMediaType? mediaType)
    {
        EmailRfc2231DecodedParameter? decoded = (mediaType ?? throw new InvalidOperationException()).GetParameters().GetDecoded("charset");
        if (decoded != null)
            return NormalizeDeclaredCharset(Encoding.ASCII.GetString(decoded.GetDecodedBytes()));
        EmailParameter? physical = mediaType.GetParameters().GetFirst("charset");
        if (physical != null)
            return NormalizeDeclaredCharset(Encoding.ASCII.GetString(physical.GetValueBytes()));
        return null;
    }

    static string? NormalizeDeclaredCharset(string value)
    {
        if (value == null)
            return null;
        string normalized = value.Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    static EmailTextTransform NormalizeNewlines(string text, EmailTextNewlinePolicy policy)
    {
        if (policy == EmailTextNewlinePolicy.Preserve)
            return new EmailTextTransform(text, false);
        StringBuilder result = new StringBuilder();
        bool changed = false;
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == '\r')
            {
                bool pair = index + 1 < text.Length && text[index + 1] == '\n';
                if (policy == EmailTextNewlinePolicy.NormalizeToLf)
                {
                    result.Append('\n');
                    changed = true;
                }
                else
                {
                    result.Append("\r\n");
                    if (!pair)
                        changed = true;
                }

                index += pair ? 2 : 1;
            }
            else if (current == '\n')
            {
                if (policy == EmailTextNewlinePolicy.NormalizeToCrlf)
                {
                    result.Append("\r\n");
                    changed = true;
                }
                else
                {
                    result.Append('\n');
                }

                index++;
            }
            else
            {
                result.Append(current);
                index++;
            }
        }

        return new EmailTextTransform(result.ToString(), changed);
    }

    static EmailTextTransform NormalizeUnicode(string text, EmailTextUnicodeNormalization normalization)
    {
        if (normalization == EmailTextUnicodeNormalization.None)
            return new EmailTextTransform(text, false);
        NormalizationForm form = normalization == EmailTextUnicodeNormalization.Nfc ? NormalizationForm.FormC : NormalizationForm.FormKC;
        string normalized = text.Normalize(form);
        return new EmailTextTransform(normalized, !string.Equals(text, normalized, StringComparison.Ordinal));
    }

    static EmailTextCharacterScan ScanCharacters(string text, bool removeControls, bool removeBidi)
    {
        StringBuilder result = new StringBuilder();
        int controls = 0;
        int bidi = 0;
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            bool bidiControl = IsBidiControl(value);
            bool control = IsDisallowedControl(value);
            if (bidiControl)
                bidi++;
            if (control)
                controls++;
            if (!(bidiControl && removeBidi) && !(control && removeControls))
                result.Append(value);
            index++;
        }

        return new EmailTextCharacterScan(result.ToString(), controls, bidi, controls > 0 && removeControls, bidi > 0 && removeBidi);
    }

    static bool IsDisallowedControl(char value)
    {
        if (value == '\t' || value == '\r' || value == '\n')
            return false;
        return char.GetUnicodeCategory(value) == UnicodeCategory.Control;
    }

    static bool IsBidiControl(char value)
    {
        return value == '\u061C' || value == '\u200E' || value == '\u200F' || value >= '\u202A' && value <= '\u202E' || value >= '\u2066' && value <= '\u2069';
    }

    static string ApplyFinalCharacterLimit(string text, int maximumCharacters, EmailIssueList issues, EmailPath? path)
    {
        if (text.Length <= maximumCharacters)
            return text;
        int length = maximumCharacters;
        if (length > 0 && length < text.Length && char.IsHighSurrogate(text[length - 1]) && char.IsLowSurrogate(text[length]))
            length--;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.CharsetCharacterLimitExceeded, "Text normalization exceeds the configured character limit.", null, path, null, "The decoded result was truncated at a Unicode scalar boundary."));
        return text.Substring(0, length);
    }
}
