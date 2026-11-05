using System;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with text processor.
/// </summary>
public class EmailTextProcessor : IEmailTextProcessorContract
{
    internal static EmailTextSearchResult SearchImplementationWithRootAndQuery(MimeEntity root, string query)
    {
        return SearchImplementationWithRootAndQueryAndOptions(root, query, new EmailTextSearchOptions());
    }

    /// <summary>
    /// Searches the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "query">The query used by the operation.</param>
    /// <returns>The text search result produced by the operation.</returns>
    public EmailTextSearchResult SearchWithRootAndQuery(MimeEntity root, string query)
    {
        return EmailTextProcessor.SearchImplementationWithRootAndQuery(root, query);
    }

    internal static EmailTextSearchResult SearchImplementationWithRootAndQueryAndOptions(MimeEntity root, string query, EmailTextSearchOptions options)
    {
        if (root == null)
            throw new ArgumentNullException("root");
        ValidateSearch(query, options);
        return new EmailTextSearcher(query, options.Clone()).Search(root);
    }

    /// <summary>
    /// Searches the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "query">The query used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The text search result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTextSearchResult SearchWithRootAndQueryAndOptions(MimeEntity root, string query, EmailTextSearchOptions options)
    {
        return EmailTextProcessor.SearchImplementationWithRootAndQueryAndOptions(root, query, options);
    }

    internal static EmailTextSearchResult SearchImplementationWithMessageAndQuery(EmailMessage message, string query)
    {
        return SearchImplementationWithRootAndQueryAndOptions(GetSearchRoot(message), query, new EmailTextSearchOptions());
    }

    /// <summary>
    /// Searches the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "query">The query used by the operation.</param>
    /// <returns>The text search result produced by the operation.</returns>
    public EmailTextSearchResult SearchWithMessageAndQuery(EmailMessage message, string query)
    {
        return EmailTextProcessor.SearchImplementationWithMessageAndQuery(message, query);
    }

    internal static EmailTextSearchResult SearchImplementationWithMessageAndQueryAndOptions(EmailMessage message, string query, EmailTextSearchOptions options)
    {
        return SearchImplementationWithRootAndQueryAndOptions(GetSearchRoot(message), query, options);
    }

    /// <summary>
    /// Searches the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "query">The query used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The text search result produced by the operation.</returns>
    public EmailTextSearchResult SearchWithMessageAndQueryAndOptions(EmailMessage message, string query, EmailTextSearchOptions options)
    {
        return EmailTextProcessor.SearchImplementationWithMessageAndQueryAndOptions(message, query, options);
    }

    internal static string CreateSnippetImplementationWithValue(string value)
    {
        return AnalyzeImplementationWithValue(value).GetSnippet();
    }

    /// <summary>
    /// Creates a snippet configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The string produced by the operation.</returns>
    public string CreateSnippetWithValue(string value)
    {
        return EmailTextProcessor.CreateSnippetImplementationWithValue(value);
    }

    internal static string CreateSnippetImplementationWithValueAndOptions(string value, EmailTextAnalysisOptions options)
    {
        return AnalyzeImplementationWithValueAndOptions(value, options).GetSnippet();
    }

    /// <summary>
    /// Creates a snippet configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public string CreateSnippetWithValueAndOptions(string value, EmailTextAnalysisOptions options)
    {
        return EmailTextProcessor.CreateSnippetImplementationWithValueAndOptions(value, options);
    }

    internal static EmailTextAnalysisResult AnalyzeImplementationWithValue(string value)
    {
        return AnalyzeImplementationWithValueAndOptions(value, new EmailTextAnalysisOptions());
    }

    /// <summary>
    /// Analyzes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The text analysis result produced by the operation.</returns>
    public EmailTextAnalysisResult AnalyzeWithValue(string value)
    {
        return EmailTextProcessor.AnalyzeImplementationWithValue(value);
    }

    internal static EmailTextAnalysisResult AnalyzeImplementationWithValueAndOptions(string value, EmailTextAnalysisOptions options)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        if (options == null)
            throw new ArgumentNullException("options");
        return EmailTextAnalyzer.Analyze(value, options.Clone());
    }

    /// <summary>
    /// Analyzes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The text analysis result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTextAnalysisResult AnalyzeWithValueAndOptions(string value, EmailTextAnalysisOptions options)
    {
        return EmailTextProcessor.AnalyzeImplementationWithValueAndOptions(value, options);
    }

    internal static EmailTextDecodeResult DecodeImplementationWithEntity(MimeEntity entity)
    {
        return DecodeImplementationWithEntityAndTextOptionsAndFlowedOptions(entity, new EmailTextDecodeOptions(), new EmailFlowedTextOptions());
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTextDecodeResult DecodeWithEntity(MimeEntity entity)
    {
        return EmailTextProcessor.DecodeImplementationWithEntity(entity);
    }

    internal static EmailTextDecodeResult DecodeImplementationWithEntityAndTextOptions(MimeEntity entity, EmailTextDecodeOptions textOptions)
    {
        return DecodeImplementationWithEntityAndTextOptionsAndFlowedOptions(entity, textOptions, new EmailFlowedTextOptions());
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTextDecodeResult DecodeWithEntityAndTextOptions(MimeEntity entity, EmailTextDecodeOptions textOptions)
    {
        return EmailTextProcessor.DecodeImplementationWithEntityAndTextOptions(entity, textOptions);
    }

    internal static EmailTextDecodeResult DecodeImplementationWithEntityAndTextOptionsAndFlowedOptions(MimeEntity entity, EmailTextDecodeOptions textOptions, EmailFlowedTextOptions flowedOptions)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (textOptions == null)
            throw new ArgumentNullException("textOptions");
        if (flowedOptions == null)
            throw new ArgumentNullException("flowedOptions");
        EmailTextDecodeResult decoded = new EmailTextDecoder().DecodeWithEntityAndOptions(entity, textOptions);
        if (!IsFlowedImplementation(entity.GetMediaType() ?? throw new InvalidOperationException()))
            return decoded;
        EmailFlowedTextOptions effective = flowedOptions.Clone();
        effective.SetDeleteSpace(UsesDelSpImplementation(entity.GetMediaType() ?? throw new InvalidOperationException()));
        return decoded.WithText(new EmailFlowedText().DecodeWithValueAndOptions(decoded.GetText(), effective));
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <param name = "flowedOptions">The flowed options used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTextDecodeResult DecodeWithEntityAndTextOptionsAndFlowedOptions(MimeEntity entity, EmailTextDecodeOptions textOptions, EmailFlowedTextOptions flowedOptions)
    {
        return EmailTextProcessor.DecodeImplementationWithEntityAndTextOptionsAndFlowedOptions(entity, textOptions, flowedOptions);
    }

    internal static EmailTextDecodeResult DecodeImplementationWithAttachment(EmailAttachmentView attachment)
    {
        return DecodeImplementationWithAttachmentAndTextOptionsAndFlowedOptions(attachment, new EmailTextDecodeOptions(), new EmailFlowedTextOptions());
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "attachment">The attachment used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTextDecodeResult DecodeWithAttachment(EmailAttachmentView attachment)
    {
        return EmailTextProcessor.DecodeImplementationWithAttachment(attachment);
    }

    internal static EmailTextDecodeResult DecodeImplementationWithAttachmentAndTextOptionsAndFlowedOptions(EmailAttachmentView attachment, EmailTextDecodeOptions textOptions, EmailFlowedTextOptions flowedOptions)
    {
        if (attachment == null)
            throw new ArgumentNullException("attachment");
        if (textOptions == null)
            throw new ArgumentNullException("textOptions");
        if (flowedOptions == null)
            throw new ArgumentNullException("flowedOptions");
        EmailTextDecodeResult decoded = new EmailTextDecoder().DecodeWithAttachmentAndOptions(attachment, textOptions);
        if (!IsFlowedImplementation(attachment.GetMediaType() ?? throw new InvalidOperationException()))
            return decoded;
        EmailFlowedTextOptions effective = flowedOptions.Clone();
        effective.SetDeleteSpace(UsesDelSpImplementation(attachment.GetMediaType() ?? throw new InvalidOperationException()));
        return decoded.WithText(new EmailFlowedText().DecodeWithValueAndOptions(decoded.GetText(), effective));
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "attachment">The attachment used by the operation.</param>
    /// <param name = "textOptions">The text options used by the operation.</param>
    /// <param name = "flowedOptions">The flowed options used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTextDecodeResult DecodeWithAttachmentAndTextOptionsAndFlowedOptions(EmailAttachmentView attachment, EmailTextDecodeOptions textOptions, EmailFlowedTextOptions flowedOptions)
    {
        return EmailTextProcessor.DecodeImplementationWithAttachmentAndTextOptionsAndFlowedOptions(attachment, textOptions, flowedOptions);
    }

    internal static bool IsFlowedImplementation(EmailMediaType mediaType)
    {
        if (mediaType == null)
            throw new ArgumentNullException("mediaType");
        if (!mediaType.IsType("text", "plain"))
            return false;
        return string.Equals(GetParameter(mediaType, "format"), "flowed", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the current value or state is flowed.
    /// </summary>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns><c>true</c> when the value or state is flowed; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public bool IsFlowed(EmailMediaType mediaType)
    {
        return EmailTextProcessor.IsFlowedImplementation(mediaType);
    }

    internal static bool UsesDelSpImplementation(EmailMediaType mediaType)
    {
        if (mediaType == null)
            throw new ArgumentNullException("mediaType");
        if (!IsFlowedImplementation(mediaType))
            return false;
        return string.Equals(GetParameter(mediaType, "delsp"), "yes", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the current value or state uses del sp.
    /// </summary>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns><c>true</c> when the value or state uses del sp; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public bool UsesDelSp(EmailMediaType mediaType)
    {
        return EmailTextProcessor.UsesDelSpImplementation(mediaType);
    }

    internal static string DecodeFlowedImplementation(string value, bool deleteSpace)
    {
        return new EmailFlowedText().DecodeWithValueAndDeleteSpace(value, deleteSpace);
    }

    /// <summary>
    /// Decodes the flowed.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "deleteSpace">Whether to enable the delete space behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public string DecodeFlowed(string value, bool deleteSpace)
    {
        return EmailTextProcessor.DecodeFlowedImplementation(value, deleteSpace);
    }

    internal static string EncodeFlowedImplementationWithValue(string value)
    {
        return new EmailFlowedText().EncodeWithValueUsingString(value);
    }

    /// <summary>
    /// Encodes the flowed.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public string EncodeFlowedWithValue(string value)
    {
        return EmailTextProcessor.EncodeFlowedImplementationWithValue(value);
    }

    internal static string EncodeFlowedImplementationWithValueAndOptions(string value, EmailFlowedTextOptions options)
    {
        return new EmailFlowedText().EncodeWithValueAndOptionsUsingStringAndEmailFlowedTextOptions(value, options);
    }

    /// <summary>
    /// Encodes the flowed.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public string EncodeFlowedWithValueAndOptions(string value, EmailFlowedTextOptions options)
    {
        return EmailTextProcessor.EncodeFlowedImplementationWithValueAndOptions(value, options);
    }

    static string? GetParameter(EmailMediaType mediaType, string name)
    {
        EmailRfc2231DecodedParameter? decoded = mediaType.GetParameters().GetDecoded(name);
        if (decoded != null)
            return Encoding.ASCII.GetString(decoded.GetDecodedBytes()).Trim();
        EmailParameter? physical = mediaType.GetParameters().GetFirst(name);
        if (physical != null)
            return Encoding.ASCII.GetString(physical.GetValueBytes()).Trim();
        return null;
    }

    static void ValidateSearch(string query, EmailTextSearchOptions options)
    {
        if (query == null)
            throw new ArgumentNullException("query");
        if (query.Length == 0)
            throw new ArgumentException("A decoded-text search query cannot be empty.", "query");
        if (options == null)
            throw new ArgumentNullException("options");
        if (query.Length > options.GetMaximumQueryCharacters())
            throw new EmailLimitException("MaxTextSearchQueryCharacters", options.GetMaximumQueryCharacters(), query.Length);
    }

    static MimeEntity GetSearchRoot(EmailMessage message)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        if (!message.HasBody())
            throw new ArgumentException("A message without a MIME body has no decoded text to search.", "message");
        return message.GetBody() ?? throw new InvalidOperationException();
    }
}
