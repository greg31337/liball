using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "MimeEntity"/>.
/// </summary>
public interface IMimeEntityOperationsContract
{
    /// <summary>
    /// Creates a basic MIME entity.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "children">The children used by the operation.</param>
    /// <param name = "nestedMessage">The nested message used by the operation.</param>
    /// <returns>The configured MIME entity.</returns>
    MimeEntity CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind kind, EmailHeaders headers, EmailMediaType mediaType, EmailContent? content, List<MimeEntity> children, EmailMessage? nestedMessage);
    /// <summary>
    /// Creates a MIME entity with parsed content metadata.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <param name = "disposition">The disposition used by the operation.</param>
    /// <param name = "transferEncodingName">The transfer encoding name used by the operation.</param>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <param name = "contentLocation">The content location used by the operation.</param>
    /// <param name = "contentLanguage">The content language used by the operation.</param>
    /// <param name = "contentDescription">The content description used by the operation.</param>
    /// <param name = "rawContent">The raw content used by the operation.</param>
    /// <param name = "decodedContent">The decoded content used by the operation.</param>
    /// <param name = "children">The children used by the operation.</param>
    /// <param name = "nestedMessage">The nested message used by the operation.</param>
    /// <param name = "preamble">The preamble used by the operation.</param>
    /// <param name = "epilogue">The epilogue used by the operation.</param>
    /// <param name = "sourceRange">The source range used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <returns>The configured MIME entity.</returns>
    MimeEntity CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind kind, EmailHeaders headers, EmailMediaType? mediaType, EmailDisposition? disposition, string? transferEncodingName, EmailMessageId? contentId, string? contentLocation, EmailContentLanguage? contentLanguage, string? contentDescription, EmailContent? rawContent, EmailContent? decodedContent, List<MimeEntity> children, EmailMessage? nestedMessage, EmailContent? preamble, EmailContent? epilogue, EmailRange? sourceRange, EmailIssueList issues);
    /// <summary>
    /// Creates a MIME entity with specialized message metadata.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <param name = "disposition">The disposition used by the operation.</param>
    /// <param name = "transferEncodingName">The transfer encoding name used by the operation.</param>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <param name = "contentLocation">The content location used by the operation.</param>
    /// <param name = "contentLanguage">The content language used by the operation.</param>
    /// <param name = "contentDescription">The content description used by the operation.</param>
    /// <param name = "partialMessageMetadata">The partial message metadata used by the operation.</param>
    /// <param name = "externalBodyMetadata">The external body metadata used by the operation.</param>
    /// <param name = "rawContent">The raw content used by the operation.</param>
    /// <param name = "decodedContent">The decoded content used by the operation.</param>
    /// <param name = "children">The children used by the operation.</param>
    /// <param name = "nestedMessage">The nested message used by the operation.</param>
    /// <param name = "preamble">The preamble used by the operation.</param>
    /// <param name = "epilogue">The epilogue used by the operation.</param>
    /// <param name = "sourceRange">The source range used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <returns>The configured MIME entity.</returns>
    MimeEntity CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndPartialMessageMetadataAndExternalBodyMetadataAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind kind, EmailHeaders headers, EmailMediaType mediaType, EmailDisposition disposition, string transferEncodingName, EmailMessageId contentId, string contentLocation, EmailContentLanguage contentLanguage, string contentDescription, EmailPartialMessageMetadata partialMessageMetadata, EmailExternalBodyMetadata externalBodyMetadata, EmailContent rawContent, EmailContent decodedContent, List<MimeEntity> children, EmailMessage nestedMessage, EmailContent preamble, EmailContent epilogue, EmailRange sourceRange, EmailIssueList issues);
    /// <summary>Creates a MIME entity with its complete model and source metadata.</summary>
    /// <param name = "kind">The entity kind.</param>
    /// <param name = "headers">The entity headers.</param>
    /// <param name = "mediaType">The media type.</param>
    /// <param name = "disposition">The content disposition.</param>
    /// <param name = "transferEncodingName">The transfer encoding name.</param>
    /// <param name = "contentId">The content identifier.</param>
    /// <param name = "contentLocation">The content location.</param>
    /// <param name = "contentLanguage">The content language.</param>
    /// <param name = "contentDescription">The content description.</param>
    /// <param name = "partialMessageMetadata">The partial-message metadata.</param>
    /// <param name = "externalBodyMetadata">The external-body metadata.</param>
    /// <param name = "bodyIngestMetadata">The body ingest metadata.</param>
    /// <param name = "rawContent">The raw content.</param>
    /// <param name = "decodedContent">The decoded content.</param>
    /// <param name = "children">The child entities.</param>
    /// <param name = "nestedMessage">The nested message.</param>
    /// <param name = "preamble">The multipart preamble.</param>
    /// <param name = "epilogue">The multipart epilogue.</param>
    /// <param name = "sourceRange">The source range.</param>
    /// <param name = "issues">The entity diagnostics.</param>
    /// <returns>The configured MIME entity.</returns>
    MimeEntity CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndPartialMessageMetadataAndExternalBodyMetadataAndBodyIngestMetadataAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind kind, EmailHeaders headers, EmailMediaType? mediaType, EmailDisposition? disposition, string? transferEncodingName, EmailMessageId? contentId, string? contentLocation, EmailContentLanguage? contentLanguage, string? contentDescription, EmailPartialMessageMetadata? partialMessageMetadata, EmailExternalBodyMetadata? externalBodyMetadata, EmailBodyIngestMetadata? bodyIngestMetadata, EmailContent? rawContent, EmailContent? decodedContent, List<MimeEntity> children, EmailMessage? nestedMessage, EmailContent? preamble, EmailContent? epilogue, EmailRange? sourceRange, EmailIssueList? issues);
}
