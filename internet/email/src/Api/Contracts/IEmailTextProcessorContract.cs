using System;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTextProcessor"/>.
/// </summary>
public interface IEmailTextProcessorContract
{
    /// <summary>
    /// Searches the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "query">The query used by the operation.</param>
    /// <returns>The text search result produced by the operation.</returns>
    EmailTextSearchResult SearchWithRootAndQuery(MimeEntity root, string query);
    /// <summary>
    /// Searches the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "query">The query used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The text search result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailTextSearchResult SearchWithRootAndQueryAndOptions(MimeEntity root, string query, EmailTextSearchOptions options);
    /// <summary>
    /// Searches the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "query">The query used by the operation.</param>
    /// <returns>The text search result produced by the operation.</returns>
    EmailTextSearchResult SearchWithMessageAndQuery(EmailMessage message, string query);
    /// <summary>
    /// Searches the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "query">The query used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The text search result produced by the operation.</returns>
    EmailTextSearchResult SearchWithMessageAndQueryAndOptions(EmailMessage message, string query, EmailTextSearchOptions options);
    /// <summary>
    /// Creates a snippet configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The string produced by the operation.</returns>
    string CreateSnippetWithValue(string value);
    /// <summary>
    /// Creates a snippet configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    string CreateSnippetWithValueAndOptions(string value, EmailTextAnalysisOptions options);
    /// <summary>
    /// Analyzes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The text analysis result produced by the operation.</returns>
    EmailTextAnalysisResult AnalyzeWithValue(string value);
    /// <summary>
    /// Analyzes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The text analysis result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailTextAnalysisResult AnalyzeWithValueAndOptions(string value, EmailTextAnalysisOptions options);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailTextDecodeResult DecodeWithEntity(MimeEntity entity);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailTextDecodeResult DecodeWithEntityAndTextOptions(MimeEntity entity, EmailTextDecodeOptions textOptions);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <param name = "flowedOptions">The flowed options used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailTextDecodeResult DecodeWithEntityAndTextOptionsAndFlowedOptions(MimeEntity entity, EmailTextDecodeOptions textOptions, EmailFlowedTextOptions flowedOptions);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "attachment">The attachment used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailTextDecodeResult DecodeWithAttachment(EmailAttachmentView attachment);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "attachment">The attachment used by the operation.</param>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <param name = "flowedOptions">The flowed options used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailTextDecodeResult DecodeWithAttachmentAndTextOptionsAndFlowedOptions(EmailAttachmentView attachment, EmailTextDecodeOptions textOptions, EmailFlowedTextOptions flowedOptions);
    /// <summary>
    /// Determines whether the current value or state is flowed.
    /// </summary>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns><c>true</c> when the value or state is flowed; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    bool IsFlowed(EmailMediaType mediaType);
    /// <summary>
    /// Determines whether the current value or state uses del sp.
    /// </summary>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns><c>true</c> when the value or state uses del sp; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    bool UsesDelSp(EmailMediaType mediaType);
    /// <summary>
    /// Decodes the flowed.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "deleteSpace">Whether to enable the delete space behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    string DecodeFlowed(string value, bool deleteSpace);
    /// <summary>
    /// Encodes the flowed.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    string EncodeFlowedWithValue(string value);
    /// <summary>
    /// Encodes the flowed.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    string EncodeFlowedWithValueAndOptions(string value, EmailFlowedTextOptions options);
}
