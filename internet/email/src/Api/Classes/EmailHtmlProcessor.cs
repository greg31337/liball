using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Provides bounded HTML-to-text, sanitization, data-URI, and resource-reference operations for email content.
/// </summary>
public class EmailHtmlProcessor : IEmailHtmlProcessorContract
{
    internal static EmailHtmlDataUriResult ExtractDataUrisImplementationWithHtml(string html)
    {
        return ExtractDataUrisImplementationWithHtmlAndOptions(html, new EmailHtmlDataUriOptions());
    }

    /// <summary>
    /// Extracts the data uris.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <returns>The HTML data URI result produced by the operation.</returns>
    public EmailHtmlDataUriResult ExtractDataUrisWithHtml(string html)
    {
        return EmailHtmlProcessor.ExtractDataUrisImplementationWithHtml(html);
    }

    internal static EmailHtmlDataUriResult ExtractDataUrisImplementationWithHtmlAndOptions(string html, EmailHtmlDataUriOptions options)
    {
        if (html == null)
            throw new ArgumentNullException("html");
        if (options == null)
            throw new ArgumentNullException("options");
        return EmailHtmlDataUriExtractor.Extract(html, options.Clone(), null, new EmailIssueList());
    }

    /// <summary>
    /// Extracts the data uris.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The HTML data URI result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHtmlDataUriResult ExtractDataUrisWithHtmlAndOptions(string html, EmailHtmlDataUriOptions options)
    {
        return EmailHtmlProcessor.ExtractDataUrisImplementationWithHtmlAndOptions(html, options);
    }

    internal static EmailHtmlDataUriResult ExtractDataUrisImplementationWithEntity(MimeEntity entity)
    {
        return ExtractDataUrisImplementationWithEntityAndTextOptionsAndHtmlOptions(entity, new EmailTextDecodeOptions(), new EmailHtmlDataUriOptions());
    }

    /// <summary>
    /// Extracts the data uris.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The HTML data URI result produced by the operation.</returns>
    public EmailHtmlDataUriResult ExtractDataUrisWithEntity(MimeEntity entity)
    {
        return EmailHtmlProcessor.ExtractDataUrisImplementationWithEntity(entity);
    }

    internal static EmailHtmlDataUriResult ExtractDataUrisImplementationWithEntityAndTextOptionsAndHtmlOptions(MimeEntity entity, EmailTextDecodeOptions textOptions, EmailHtmlDataUriOptions htmlOptions)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (textOptions == null)
            throw new ArgumentNullException("textOptions");
        if (htmlOptions == null)
            throw new ArgumentNullException("htmlOptions");
        if (!(entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "html"))
            throw new ArgumentException("Only text/html MIME entities can have data URIs extracted.", "entity");
        EmailTextDecodeResult decoded = new EmailTextDecoder().DecodeWithEntityAndOptions(entity, textOptions);
        return EmailHtmlDataUriExtractor.Extract(decoded.GetText(), htmlOptions.Clone(), decoded, decoded.GetIssues());
    }

    /// <summary>
    /// Extracts the data uris.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <param name = "htmlOptions">The html options used by the operation.</param>
    /// <returns>The HTML data URI result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHtmlDataUriResult ExtractDataUrisWithEntityAndTextOptionsAndHtmlOptions(MimeEntity entity, EmailTextDecodeOptions textOptions, EmailHtmlDataUriOptions htmlOptions)
    {
        return EmailHtmlProcessor.ExtractDataUrisImplementationWithEntityAndTextOptionsAndHtmlOptions(entity, textOptions, htmlOptions);
    }

    internal static EmailHtmlSanitizeResult SanitizeImplementationWithHtml(string html)
    {
        return SanitizeImplementationWithHtmlAndOptions(html, new EmailHtmlSanitizeOptions());
    }

    /// <summary>
    /// Sanitizes the supplied value.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <returns>The HTML sanitize result produced by the operation.</returns>
    public EmailHtmlSanitizeResult SanitizeWithHtml(string html)
    {
        return EmailHtmlProcessor.SanitizeImplementationWithHtml(html);
    }

    internal static EmailHtmlSanitizeResult SanitizeImplementationWithHtmlAndOptions(string html, EmailHtmlSanitizeOptions options)
    {
        if (html == null)
            throw new ArgumentNullException("html");
        if (options == null)
            throw new ArgumentNullException("options");
        return EmailHtmlSanitizerEngine.Sanitize(html, options.Clone(), null, new EmailIssueList());
    }

    /// <summary>
    /// Sanitizes the supplied value.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The HTML sanitize result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHtmlSanitizeResult SanitizeWithHtmlAndOptions(string html, EmailHtmlSanitizeOptions options)
    {
        return EmailHtmlProcessor.SanitizeImplementationWithHtmlAndOptions(html, options);
    }

    internal static EmailHtmlSanitizeResult SanitizeImplementationWithEntity(MimeEntity entity)
    {
        return SanitizeImplementationWithEntityAndTextOptionsAndHtmlOptions(entity, new EmailTextDecodeOptions(), new EmailHtmlSanitizeOptions());
    }

    /// <summary>
    /// Sanitizes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The HTML sanitize result produced by the operation.</returns>
    public EmailHtmlSanitizeResult SanitizeWithEntity(MimeEntity entity)
    {
        return EmailHtmlProcessor.SanitizeImplementationWithEntity(entity);
    }

    internal static EmailHtmlSanitizeResult SanitizeImplementationWithEntityAndTextOptionsAndHtmlOptions(MimeEntity entity, EmailTextDecodeOptions textOptions, EmailHtmlSanitizeOptions htmlOptions)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (textOptions == null)
            throw new ArgumentNullException("textOptions");
        if (htmlOptions == null)
            throw new ArgumentNullException("htmlOptions");
        if (!(entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "html"))
            throw new ArgumentException("Only text/html MIME entities can be sanitized.", "entity");
        EmailTextDecodeResult decoded = new EmailTextDecoder().DecodeWithEntityAndOptions(entity, textOptions);
        return EmailHtmlSanitizerEngine.Sanitize(decoded.GetText(), htmlOptions.Clone(), decoded, decoded.GetIssues());
    }

    /// <summary>
    /// Sanitizes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <param name = "htmlOptions">The html options used by the operation.</param>
    /// <returns>The HTML sanitize result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHtmlSanitizeResult SanitizeWithEntityAndTextOptionsAndHtmlOptions(MimeEntity entity, EmailTextDecodeOptions textOptions, EmailHtmlSanitizeOptions htmlOptions)
    {
        return EmailHtmlProcessor.SanitizeImplementationWithEntityAndTextOptionsAndHtmlOptions(entity, textOptions, htmlOptions);
    }

    internal static EmailHtmlTextResult ToTextImplementationWithHtml(string html)
    {
        return ToTextImplementationWithHtmlAndOptions(html, new EmailHtmlTextOptions());
    }

    /// <summary>
    /// Converts to the text.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <returns>The HTML text result produced by the operation.</returns>
    public EmailHtmlTextResult ToTextWithHtml(string html)
    {
        return EmailHtmlProcessor.ToTextImplementationWithHtml(html);
    }

    internal static EmailHtmlTextResult ToTextImplementationWithHtmlAndOptions(string html, EmailHtmlTextOptions options)
    {
        if (html == null)
            throw new ArgumentNullException("html");
        if (options == null)
            throw new ArgumentNullException("options");
        return Convert(html, options.Clone(), null, new EmailIssueList());
    }

    /// <summary>
    /// Converts to the text.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The HTML text result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHtmlTextResult ToTextWithHtmlAndOptions(string html, EmailHtmlTextOptions options)
    {
        return EmailHtmlProcessor.ToTextImplementationWithHtmlAndOptions(html, options);
    }

    internal static EmailHtmlTextResult ToTextImplementationWithEntity(MimeEntity entity)
    {
        return ToTextImplementationWithEntityAndTextOptionsAndHtmlOptions(entity, new EmailTextDecodeOptions(), new EmailHtmlTextOptions());
    }

    /// <summary>
    /// Converts to the text.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The HTML text result produced by the operation.</returns>
    public EmailHtmlTextResult ToTextWithEntity(MimeEntity entity)
    {
        return EmailHtmlProcessor.ToTextImplementationWithEntity(entity);
    }

    internal static EmailHtmlTextResult ToTextImplementationWithEntityAndTextOptionsAndHtmlOptions(MimeEntity entity, EmailTextDecodeOptions textOptions, EmailHtmlTextOptions htmlOptions)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (textOptions == null)
            throw new ArgumentNullException("textOptions");
        if (htmlOptions == null)
            throw new ArgumentNullException("htmlOptions");
        if (!(entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "html"))
            throw new ArgumentException("Only text/html MIME entities can be converted to plain text.", "entity");
        EmailTextDecodeResult decoded = new EmailTextDecoder().DecodeWithEntityAndOptions(entity, textOptions);
        return Convert(decoded.GetText(), htmlOptions.Clone(), decoded, decoded.GetIssues());
    }

    /// <summary>
    /// Converts to the text.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <param name = "htmlOptions">The html options used by the operation.</param>
    /// <returns>The HTML text result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHtmlTextResult ToTextWithEntityAndTextOptionsAndHtmlOptions(MimeEntity entity, EmailTextDecodeOptions textOptions, EmailHtmlTextOptions htmlOptions)
    {
        return EmailHtmlProcessor.ToTextImplementationWithEntityAndTextOptionsAndHtmlOptions(entity, textOptions, htmlOptions);
    }

    static EmailHtmlTextResult Convert(string html, EmailHtmlTextOptions options, EmailTextDecodeResult? decoded, EmailIssueList initialIssues)
    {
        EmailHtmlLimits limits = options.GetLimits();
        EmailHtmlLimits tokenizerLimits = limits.Clone();
        if (html.Length > tokenizerLimits.GetMaxOutputCharacters())
            tokenizerLimits.SetMaxOutputCharacters(html.Length);
        EmailHtmlTokenizationResult tokenization = new EmailHtmlTokenizer().TokenizeWithHtmlAndLimits(html, tokenizerLimits);
        EmailIssueList issues = new EmailIssueList();
        issues.AddRange(initialIssues);
        issues.AddRange(tokenization.GetIssues());
        EmailHtmlTextConverter converter = new EmailHtmlTextConverter(tokenization, options, limits.GetMaxOutputCharacters());
        string text = converter.Convert();
        return new EmailHtmlTextResult(text, tokenization, issues, converter.GetSkippedHiddenElementCount(), converter.GetAppendedLinkTargetCount(), converter.GetAppendedImageAlternativeCount(), decoded);
    }
}
