using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlProcessor"/>.
/// </summary>
public interface IEmailHtmlProcessorContract
{
    /// <summary>
    /// Extracts the data uris.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <returns>The HTML data URI result produced by the operation.</returns>
    EmailHtmlDataUriResult ExtractDataUrisWithHtml(string html);
    /// <summary>
    /// Extracts the data uris.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The HTML data URI result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailHtmlDataUriResult ExtractDataUrisWithHtmlAndOptions(string html, EmailHtmlDataUriOptions options);
    /// <summary>
    /// Extracts the data uris.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The HTML data URI result produced by the operation.</returns>
    EmailHtmlDataUriResult ExtractDataUrisWithEntity(MimeEntity entity);
    /// <summary>
    /// Extracts the data uris.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <param name = "htmlOptions">The html options used by the operation.</param>
    /// <returns>The HTML data URI result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailHtmlDataUriResult ExtractDataUrisWithEntityAndTextOptionsAndHtmlOptions(MimeEntity entity, EmailTextDecodeOptions textOptions, EmailHtmlDataUriOptions htmlOptions);
    /// <summary>
    /// Sanitizes the supplied value.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <returns>The HTML sanitize result produced by the operation.</returns>
    EmailHtmlSanitizeResult SanitizeWithHtml(string html);
    /// <summary>
    /// Sanitizes the supplied value.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The HTML sanitize result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailHtmlSanitizeResult SanitizeWithHtmlAndOptions(string html, EmailHtmlSanitizeOptions options);
    /// <summary>
    /// Sanitizes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The HTML sanitize result produced by the operation.</returns>
    EmailHtmlSanitizeResult SanitizeWithEntity(MimeEntity entity);
    /// <summary>
    /// Sanitizes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <param name = "htmlOptions">The html options used by the operation.</param>
    /// <returns>The HTML sanitize result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailHtmlSanitizeResult SanitizeWithEntityAndTextOptionsAndHtmlOptions(MimeEntity entity, EmailTextDecodeOptions textOptions, EmailHtmlSanitizeOptions htmlOptions);
    /// <summary>
    /// Converts to the text.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <returns>The HTML text result produced by the operation.</returns>
    EmailHtmlTextResult ToTextWithHtml(string html);
    /// <summary>
    /// Converts to the text.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The HTML text result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailHtmlTextResult ToTextWithHtmlAndOptions(string html, EmailHtmlTextOptions options);
    /// <summary>
    /// Converts to the text.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The HTML text result produced by the operation.</returns>
    EmailHtmlTextResult ToTextWithEntity(MimeEntity entity);
    /// <summary>
    /// Converts to the text.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <param name = "htmlOptions">The html options used by the operation.</param>
    /// <returns>The HTML text result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailHtmlTextResult ToTextWithEntityAndTextOptionsAndHtmlOptions(MimeEntity entity, EmailTextDecodeOptions textOptions, EmailHtmlTextOptions htmlOptions);
}
