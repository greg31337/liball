using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents one node in a MIME entity tree, including its headers, content, child entities, and source metadata.
/// </summary>
public class MimeEntity : IMimeEntityContract
{
    MimeEntityKind kind;
    EmailHeaders? headers;
    EmailMediaType? mediaType;
    EmailDisposition? disposition;
    string? transferEncodingName;
    EmailMessageId? contentId;
    string? contentLocation;
    EmailContentLanguage? contentLanguage;
    string? contentDescription;
    EmailPartialMessageMetadata? partialMessageMetadata;
    EmailExternalBodyMetadata? externalBodyMetadata;
    EmailBodyIngestMetadata? bodyIngestMetadata;
    EmailContent? rawContent;
    EmailContent? decodedContent;
    List<MimeEntity>? children;
    EmailMessage? nestedMessage;
    EmailContent? preamble;
    EmailContent? epilogue;
    EmailContent? originalEntityContent;
    bool originalEntityContentInvalidated;
    EmailRange? sourceRange;
    EmailIssueList? ownIssues;
    MimeEntity? parent;
    List<EmailMessageHeaderSnapshot>? headerSnapshots;
    List<MimeEntity>? childSnapshots;
    EmailContent? rawContentSnapshot;
    EmailContent? decodedContentSnapshot;
    EmailContent? preambleSnapshot;
    EmailContent? epilogueSnapshot;
    EmailMessage? nestedMessageSnapshot;
    EmailMediaType? mediaTypeSnapshot;
    bool forcedExactDirty;
    bool forcedSemanticDirty;
    bool signatureMutationGuardEnabled;
    internal static MimeEntity CreateImplementationWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind kind, EmailHeaders headers, EmailMediaType mediaType, EmailContent? content, List<MimeEntity> children, EmailMessage? nestedMessage)
    {
        return new MimeEntity(kind, headers, mediaType, null, null, null, null, null, null, null, null, null, content, null, children, nestedMessage, null, null, null, new EmailIssueList());
    }

    internal static MimeEntity CreateImplementationWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind kind, EmailHeaders headers, EmailMediaType? mediaType, EmailDisposition? disposition, string? transferEncodingName, EmailMessageId? contentId, string? contentLocation, EmailContentLanguage? contentLanguage, string? contentDescription, EmailContent? rawContent, EmailContent? decodedContent, List<MimeEntity> children, EmailMessage? nestedMessage, EmailContent? preamble, EmailContent? epilogue, EmailRange? sourceRange, EmailIssueList issues)
    {
        return new MimeEntity(kind, headers, mediaType, disposition, transferEncodingName, contentId, contentLocation, contentLanguage, contentDescription, null, null, null, rawContent, decodedContent, children, nestedMessage, preamble, epilogue, sourceRange, issues);
    }

    internal static MimeEntity CreateImplementationWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndPartialMessageMetadataAndExternalBodyMetadataAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind kind, EmailHeaders headers, EmailMediaType mediaType, EmailDisposition disposition, string transferEncodingName, EmailMessageId contentId, string contentLocation, EmailContentLanguage contentLanguage, string contentDescription, EmailPartialMessageMetadata partialMessageMetadata, EmailExternalBodyMetadata externalBodyMetadata, EmailContent rawContent, EmailContent decodedContent, List<MimeEntity> children, EmailMessage nestedMessage, EmailContent preamble, EmailContent epilogue, EmailRange sourceRange, EmailIssueList issues)
    {
        return new MimeEntity(kind, headers, mediaType, disposition, transferEncodingName, contentId, contentLocation, contentLanguage, contentDescription, partialMessageMetadata, externalBodyMetadata, null, rawContent, decodedContent, children, nestedMessage, preamble, epilogue, sourceRange, issues);
    }

    /// <summary>
    /// Creates a new MIME entity.
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
    /// <param name = "bodyIngestMetadata">The body ingest metadata used by the operation.</param>
    /// <param name = "rawContent">The raw content used by the operation.</param>
    /// <param name = "decodedContent">The decoded content used by the operation.</param>
    /// <param name = "children">The children used by the operation.</param>
    /// <param name = "nestedMessage">The nested message used by the operation.</param>
    /// <param name = "preamble">The preamble used by the operation.</param>
    /// <param name = "epilogue">The epilogue used by the operation.</param>
    /// <param name = "sourceRange">The source range used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    public MimeEntity(MimeEntityKind kind, EmailHeaders headers, EmailMediaType? mediaType, EmailDisposition? disposition, string? transferEncodingName, EmailMessageId? contentId, string? contentLocation, EmailContentLanguage? contentLanguage, string? contentDescription, EmailPartialMessageMetadata? partialMessageMetadata, EmailExternalBodyMetadata? externalBodyMetadata, EmailBodyIngestMetadata? bodyIngestMetadata, EmailContent? rawContent, EmailContent? decodedContent, List<MimeEntity> children, EmailMessage? nestedMessage, EmailContent? preamble, EmailContent? epilogue, EmailRange? sourceRange, EmailIssueList? issues)
    {
        Initialize(kind, headers, mediaType, disposition, transferEncodingName, contentId, contentLocation, contentLanguage, contentDescription, partialMessageMetadata, externalBodyMetadata, bodyIngestMetadata, rawContent, decodedContent, children, nestedMessage, preamble, epilogue, sourceRange, issues);
    }

    internal static MimeEntity CreateImplementationWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndPartialMessageMetadataAndExternalBodyMetadataAndBodyIngestMetadataAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind kind, EmailHeaders headers, EmailMediaType? mediaType, EmailDisposition? disposition, string? transferEncodingName, EmailMessageId? contentId, string? contentLocation, EmailContentLanguage? contentLanguage, string? contentDescription, EmailPartialMessageMetadata? partialMessageMetadata, EmailExternalBodyMetadata? externalBodyMetadata, EmailBodyIngestMetadata? bodyIngestMetadata, EmailContent? rawContent, EmailContent? decodedContent, List<MimeEntity> children, EmailMessage? nestedMessage, EmailContent? preamble, EmailContent? epilogue, EmailRange? sourceRange, EmailIssueList? issues)
    {
        return new MimeEntity(kind, headers, mediaType, disposition, transferEncodingName, contentId, contentLocation, contentLanguage, contentDescription, partialMessageMetadata, externalBodyMetadata, bodyIngestMetadata, rawContent, decodedContent, children, nestedMessage, preamble, epilogue, sourceRange, issues);
    }

    void Initialize(MimeEntityKind suppliedKind, EmailHeaders suppliedHeaders, EmailMediaType? suppliedMediaType, EmailDisposition? suppliedDisposition, string? suppliedTransferEncodingName, EmailMessageId? suppliedContentId, string? suppliedContentLocation, EmailContentLanguage? suppliedContentLanguage, string? suppliedContentDescription, EmailPartialMessageMetadata? suppliedPartialMessageMetadata, EmailExternalBodyMetadata? suppliedExternalBodyMetadata, EmailBodyIngestMetadata? suppliedBodyIngestMetadata, EmailContent? suppliedRawContent, EmailContent? suppliedDecodedContent, List<MimeEntity> suppliedChildren, EmailMessage? suppliedNestedMessage, EmailContent? suppliedPreamble, EmailContent? suppliedEpilogue, EmailRange? suppliedSourceRange, EmailIssueList? suppliedIssues)
    {
        ValidateKind(suppliedKind);
        if (suppliedHeaders == null)
        {
            throw new ArgumentNullException("suppliedHeaders");
        }

        if (suppliedMediaType == null)
        {
            throw new ArgumentNullException("suppliedMediaType");
        }

        if (suppliedChildren == null)
        {
            throw new ArgumentNullException("suppliedChildren");
        }

        if (suppliedIssues == null)
        {
            throw new ArgumentNullException("suppliedIssues");
        }

        ValidateOptionalText(suppliedContentLocation, "suppliedContentLocation");
        ValidateOptionalText(suppliedContentDescription, "suppliedContentDescription");
        ValidateTransferEncoding(suppliedTransferEncodingName);
        ValidateMediaType(suppliedKind, suppliedMediaType);
        ValidateSpecializedMetadata(suppliedKind, suppliedPartialMessageMetadata, suppliedExternalBodyMetadata);
        ValidateShape(suppliedKind, suppliedBodyIngestMetadata, suppliedRawContent, suppliedDecodedContent, suppliedChildren, suppliedNestedMessage, suppliedPreamble, suppliedEpilogue);
        ValidateChildren(suppliedChildren);
        kind = suppliedKind;
        headers = suppliedHeaders;
        mediaType = suppliedMediaType;
        disposition = suppliedDisposition;
        transferEncodingName = suppliedTransferEncodingName;
        contentId = suppliedContentId;
        contentLocation = suppliedContentLocation;
        contentLanguage = suppliedContentLanguage;
        contentDescription = suppliedContentDescription;
        partialMessageMetadata = suppliedPartialMessageMetadata;
        externalBodyMetadata = suppliedExternalBodyMetadata;
        bodyIngestMetadata = suppliedBodyIngestMetadata;
        rawContent = suppliedRawContent;
        decodedContent = suppliedDecodedContent;
        children = new List<MimeEntity>(suppliedChildren);
        nestedMessage = suppliedNestedMessage;
        preamble = suppliedPreamble;
        epilogue = suppliedEpilogue;
        originalEntityContent = null;
        originalEntityContentInvalidated = false;
        sourceRange = suppliedSourceRange;
        ownIssues = new EmailIssueList();
        ownIssues.AddRange(suppliedIssues);
        parent = null;
        AttachChildren();
        forcedExactDirty = false;
        forcedSemanticDirty = false;
        signatureMutationGuardEnabled = false;
        CaptureSnapshots();
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public MimeEntityKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the headers.
    /// </summary>
    /// <returns>The current headers.</returns>
    public EmailHeaders? GetHeaders()
    {
        return headers;
    }

    /// <summary>
    /// Gets the media type.
    /// </summary>
    /// <returns>The current media type.</returns>
    public EmailMediaType? GetMediaType()
    {
        return mediaType;
    }

    /// <summary>
    /// Sets the media type used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetMediaType(EmailMediaType value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        ValidateMediaType(kind, value);
        if (!object.ReferenceEquals(mediaType, value))
        {
            EnsureSignatureMutationAllowed();
            mediaType = value;
            MarkDirty();
        }
    }

    /// <summary>
    /// Determines whether the disposition is present or available.
    /// </summary>
    /// <returns><c>true</c> when the disposition is available; otherwise, <c>false</c>.</returns>
    public bool HasDisposition()
    {
        return disposition != null;
    }

    /// <summary>
    /// Gets the disposition.
    /// </summary>
    /// <returns>The current disposition.</returns>
    public EmailDisposition? GetDisposition()
    {
        return disposition;
    }

    /// <summary>
    /// Determines whether the transfer encoding is present or available.
    /// </summary>
    /// <returns><c>true</c> when the transfer encoding is available; otherwise, <c>false</c>.</returns>
    public bool HasTransferEncoding()
    {
        return transferEncodingName != null;
    }

    /// <summary>
    /// Gets the transfer encoding name.
    /// </summary>
    /// <returns>The current transfer encoding name.</returns>
    public string? GetTransferEncodingName()
    {
        return transferEncodingName;
    }

    /// <summary>
    /// Determines whether the operation or source value was transfer encoding defaulted.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasTransferEncodingDefaulted()
    {
        return transferEncodingName == null;
    }

    /// <summary>
    /// Gets the effective transfer encoding name.
    /// </summary>
    /// <returns>The current effective transfer encoding name.</returns>
    public string GetEffectiveTransferEncodingName()
    {
        return transferEncodingName == null ? new EmailMimeDefaults().GetDefaultTransferEncodingName() : transferEncodingName;
    }

    /// <summary>
    /// Determines whether the known transfer encoding is present or available.
    /// </summary>
    /// <returns><c>true</c> when the known transfer encoding is available; otherwise, <c>false</c>.</returns>
    public bool HasKnownTransferEncoding()
    {
        return IsKnownTransferEncoding(transferEncodingName);
    }

    /// <summary>
    /// Gets the transfer encoding kind.
    /// </summary>
    /// <returns>The current transfer encoding kind.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public EmailTransferEncodingKind GetTransferEncodingKind()
    {
        if (!HasKnownTransferEncoding())
        {
            throw new InvalidOperationException("The entity does not have a known content-transfer-encoding.");
        }

        if (string.Equals(transferEncodingName, "7bit", StringComparison.OrdinalIgnoreCase))
        {
            return EmailTransferEncodingKind.SevenBit;
        }

        if (string.Equals(transferEncodingName, "8bit", StringComparison.OrdinalIgnoreCase))
        {
            return EmailTransferEncodingKind.EightBit;
        }

        if (string.Equals(transferEncodingName, "binary", StringComparison.OrdinalIgnoreCase))
        {
            return EmailTransferEncodingKind.Binary;
        }

        if (string.Equals(transferEncodingName, "quoted-printable", StringComparison.OrdinalIgnoreCase))
        {
            return EmailTransferEncodingKind.QuotedPrintable;
        }

        return EmailTransferEncodingKind.Base64;
    }

    /// <summary>
    /// Gets the effective transfer encoding kind.
    /// </summary>
    /// <returns>The current effective transfer encoding kind.</returns>
    public EmailTransferEncodingKind GetEffectiveTransferEncodingKind()
    {
        return transferEncodingName == null ? new EmailMimeDefaults().GetDefaultTransferEncodingKind() : GetTransferEncodingKind();
    }

    /// <summary>
    /// Determines whether the content ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content ID is available; otherwise, <c>false</c>.</returns>
    public bool HasContentId()
    {
        return contentId != null;
    }

    /// <summary>
    /// Gets the content ID.
    /// </summary>
    /// <returns>The current content ID.</returns>
    public EmailMessageId? GetContentId()
    {
        return contentId;
    }

    /// <summary>
    /// Determines whether the content location is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content location is available; otherwise, <c>false</c>.</returns>
    public bool HasContentLocation()
    {
        return contentLocation != null;
    }

    /// <summary>
    /// Gets the content location.
    /// </summary>
    /// <returns>The current content location.</returns>
    public string? GetContentLocation()
    {
        return contentLocation;
    }

    /// <summary>
    /// Determines whether the content language is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content language is available; otherwise, <c>false</c>.</returns>
    public bool HasContentLanguage()
    {
        return contentLanguage != null;
    }

    /// <summary>
    /// Gets the content language.
    /// </summary>
    /// <returns>The current content language.</returns>
    public EmailContentLanguage? GetContentLanguage()
    {
        return contentLanguage;
    }

    /// <summary>
    /// Determines whether the content description is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content description is available; otherwise, <c>false</c>.</returns>
    public bool HasContentDescription()
    {
        return contentDescription != null;
    }

    /// <summary>
    /// Gets the content description.
    /// </summary>
    /// <returns>The current content description.</returns>
    public string? GetContentDescription()
    {
        return contentDescription;
    }

    /// <summary>
    /// Refreshes derived MIME metadata from the entity's current content headers using tolerant bounded defaults.
    /// </summary>
    /// <returns>The metadata refresh result.</returns>
    public EmailMimeMetadataRefreshResult RefreshMetadata()
    {
        return RefreshMetadataWithOptions(new EmailParseOptions());
    }

    /// <summary>
    /// Refreshes derived MIME metadata from the entity's current content headers.
    /// </summary>
    /// <param name = "options">The parsing and resource-limit policy.</param>
    /// <returns>The metadata refresh result.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when options is null.</exception>
    public EmailMimeMetadataRefreshResult RefreshMetadataWithOptions(EmailParseOptions options)
    {
        if (options == null) { throw new ArgumentNullException("options"); }
        MimeEntityKind previousKind = kind;
        EmailMimeMetadataCandidate candidate = new EmailMimeMetadataRefresher(headers ?? throw new InvalidOperationException(), parent == null ? null : parent.mediaType, options.Clone()).Parse();
        EmailIssueList refreshIssues = new EmailIssueList();
        refreshIssues.AddRange(candidate.GetIssues());
        bool conflicts = candidate.HasConflicts();
        bool mediaAllowed = true;
        if (!CanAdoptKind(candidate.GetKind()))
        {
            conflicts = true;
            mediaAllowed = false;
            AddRefreshConflict(refreshIssues, "Content-Type implies an entity kind that is incompatible with the existing body shape.", "The current entity kind and derived media type were retained.", "Content-Type");
        }

        EmailPartialMessageMetadata? refreshedPartial = partialMessageMetadata;
        EmailExternalBodyMetadata? refreshedExternal = externalBodyMetadata;
        bool specializedNeedsRefresh = mediaAllowed && (!MediaTypeEqual(mediaType, candidate.GetMediaType()) || kind != candidate.GetKind());
        if (specializedNeedsRefresh && candidate.GetKind() == MimeEntityKind.PartialMessage)
        {
            refreshedPartial = EmailSpecializedMessageMetadataParser.ParsePartial(candidate.GetMediaType(), candidate.GetTransferEncodingName(), options, refreshIssues);
            refreshedExternal = null;
        }
        else if (specializedNeedsRefresh && candidate.GetKind() == MimeEntityKind.ExternalBody)
        {
            if (rawContent == null || !rawContent.IsRepeatable() || !rawContent.HasKnownLength())
            {
                conflicts = true;
                mediaAllowed = false;
                AddRefreshConflict(refreshIssues, "message/external-body metadata cannot be refreshed because repeatable raw body content is unavailable.", "The current entity kind and derived media type were retained.", "Content-Type");
            }
            else
            {
                refreshedExternal = EmailSpecializedMessageMetadataParser.ParseExternal(candidate.GetMediaType(), candidate.GetTransferEncodingName(), rawContent, new EmailRange(0, rawContent.GetLength()), options, refreshIssues);
                refreshedPartial = null;
            }
        }
        else if (specializedNeedsRefresh)
        {
            refreshedPartial = null;
            refreshedExternal = null;
        }

        string? refreshedTransfer = candidate.GetTransferEncodingName();
        bool transferAllowed = true;
        if (!TransferEncodingEquivalent(transferEncodingName, refreshedTransfer) && rawContent == null && decodedContent != null)
        {
            conflicts = true;
            transferAllowed = false;
            AddRefreshConflict(refreshIssues, "Content-Transfer-Encoding changed but only decoded body content is available.", "The previous transfer encoding was retained to avoid losing the only body representation.", "Content-Transfer-Encoding");
        }

        int changed = CountRefreshChanges(candidate, mediaAllowed, transferAllowed, refreshedPartial, refreshedExternal);
        if (changed > 0)
        {
            EnsureSignatureMutationAllowed();
            ApplyRefresh(candidate, mediaAllowed, transferAllowed, refreshedPartial, refreshedExternal);
        }

        return new EmailMimeMetadataRefreshResult(previousKind, kind, changed, conflicts, refreshIssues);
    }

    /// <summary>
    /// Determines whether the partial message metadata is present or available.
    /// </summary>
    /// <returns><c>true</c> when the partial message metadata is available; otherwise, <c>false</c>.</returns>
    public bool HasPartialMessageMetadata()
    {
        return partialMessageMetadata != null;
    }

    /// <summary>
    /// Gets the partial message metadata.
    /// </summary>
    /// <returns>The current partial message metadata.</returns>
    public EmailPartialMessageMetadata? GetPartialMessageMetadata()
    {
        return partialMessageMetadata;
    }

    /// <summary>
    /// Determines whether the external body metadata is present or available.
    /// </summary>
    /// <returns><c>true</c> when the external body metadata is available; otherwise, <c>false</c>.</returns>
    public bool HasExternalBodyMetadata()
    {
        return externalBodyMetadata != null;
    }

    /// <summary>
    /// Gets the external body metadata.
    /// </summary>
    /// <returns>The current external body metadata.</returns>
    public EmailExternalBodyMetadata? GetExternalBodyMetadata()
    {
        return externalBodyMetadata;
    }

    /// <summary>
    /// Determines whether the body ingest metadata is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body ingest metadata is available; otherwise, <c>false</c>.</returns>
    public bool HasBodyIngestMetadata()
    {
        return bodyIngestMetadata != null;
    }

    /// <summary>
    /// Gets the body ingest metadata.
    /// </summary>
    /// <returns>The current body ingest metadata.</returns>
    public EmailBodyIngestMetadata? GetBodyIngestMetadata()
    {
        return bodyIngestMetadata;
    }

    /// <summary>
    /// Determines whether the content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content is available; otherwise, <c>false</c>.</returns>
    public bool HasContent()
    {
        return rawContent != null || decodedContent != null;
    }

    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <returns>The current content.</returns>
    public EmailContent? GetContent()
    {
        return rawContent == null ? decodedContent : rawContent;
    }

    /// <summary>
    /// Determines whether the raw content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the raw content is available; otherwise, <c>false</c>.</returns>
    public bool HasRawContent()
    {
        return rawContent != null;
    }

    /// <summary>
    /// Gets the raw content.
    /// </summary>
    /// <returns>The current raw content.</returns>
    public EmailContent? GetRawContent()
    {
        return rawContent;
    }

    /// <summary>
    /// Determines whether the decoded content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the decoded content is available; otherwise, <c>false</c>.</returns>
    public bool HasDecodedContent()
    {
        return decodedContent != null;
    }

    /// <summary>
    /// Gets the decoded content.
    /// </summary>
    /// <returns>The current decoded content.</returns>
    public EmailContent? GetDecodedContent()
    {
        return decodedContent;
    }

    /// <summary>
    /// Determines whether the nested message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the nested message is available; otherwise, <c>false</c>.</returns>
    public bool HasNestedMessage()
    {
        return nestedMessage != null;
    }

    /// <summary>
    /// Gets the nested message.
    /// </summary>
    /// <returns>The current nested message.</returns>
    public EmailMessage? GetNestedMessage()
    {
        return nestedMessage;
    }

    /// <summary>
    /// Determines whether the preamble is present or available.
    /// </summary>
    /// <returns><c>true</c> when the preamble is available; otherwise, <c>false</c>.</returns>
    public bool HasPreamble()
    {
        return preamble != null;
    }

    /// <summary>
    /// Gets the preamble.
    /// </summary>
    /// <returns>The current preamble.</returns>
    public EmailContent? GetPreamble()
    {
        return preamble;
    }

    /// <summary>
    /// Determines whether the epilogue is present or available.
    /// </summary>
    /// <returns><c>true</c> when the epilogue is available; otherwise, <c>false</c>.</returns>
    public bool HasEpilogue()
    {
        return epilogue != null;
    }

    /// <summary>
    /// Gets the epilogue.
    /// </summary>
    /// <returns>The current epilogue.</returns>
    public EmailContent? GetEpilogue()
    {
        return epilogue;
    }

    /// <summary>
    /// Determines whether the source range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source range is available; otherwise, <c>false</c>.</returns>
    public bool HasSourceRange()
    {
        return sourceRange != null;
    }

    /// <summary>
    /// Gets the source range.
    /// </summary>
    /// <returns>The current source range.</returns>
    public EmailRange? GetSourceRange()
    {
        return sourceRange;
    }

    /// <summary>
    /// Determines whether the original entity content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original entity content is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalEntityContent()
    {
        return originalEntityContent != null;
    }

    /// <summary>
    /// Gets the original entity content.
    /// </summary>
    /// <returns>The current original entity content.</returns>
    public EmailContent? GetOriginalEntityContent()
    {
        return originalEntityContent;
    }

    /// <summary>
    /// Determines whether the current original entity content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the current original entity content is available; otherwise, <c>false</c>.</returns>
    public bool HasCurrentOriginalEntityContent()
    {
        return originalEntityContent != null && !HasMutationAgainstOriginalEntityContent();
    }

    internal void RetainOriginalEntityContent(EmailContent content)
    {
        if (content == null)
        {
            throw new ArgumentNullException("content");
        }

        if (content.IsClosed())
        {
            throw new ObjectDisposedException("content");
        }

        if (!content.IsRepeatable() || !content.HasKnownLength())
        {
            throw new ArgumentException("Original MIME entity content must be repeatable and have a known length.", "content");
        }

        if (originalEntityContent != null)
        {
            throw new InvalidOperationException("Original MIME entity content has already been retained.");
        }

        if (sourceRange != null && content.GetLength() != sourceRange.GetLength())
        {
            throw new ArgumentException("Original MIME entity content must match the entity source range length.", "content");
        }

        originalEntityContent = content;
    }

    /// <summary>
    /// Determines whether the parent is present or available.
    /// </summary>
    /// <returns><c>true</c> when the parent is available; otherwise, <c>false</c>.</returns>
    public bool HasParent()
    {
        return parent != null;
    }

    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <returns>The current parent.</returns>
    public MimeEntity? GetParent()
    {
        return parent;
    }

    /// <summary>
    /// Gets the dirty state.
    /// </summary>
    /// <returns>The current dirty state.</returns>
    public EmailDirtyState GetDirtyState()
    {
        bool exact = IsExactDirty();
        bool semantic = IsSemanticDirty();
        if (exact && semantic)
        {
            return EmailDirtyState.ExactAndSemantic;
        }

        if (exact)
        {
            return EmailDirtyState.ExactOnly;
        }

        return semantic ? EmailDirtyState.SemanticOnly : EmailDirtyState.Clean;
    }

    /// <summary>
    /// Determines whether the current value or state is dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is dirty; otherwise, <c>false</c>.</returns>
    public bool IsDirty()
    {
        return IsExactDirty() || IsSemanticDirty();
    }

    /// <summary>
    /// Determines whether the current value or state is signature mutation guard enabled.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signature mutation guard enabled; otherwise, <c>false</c>.</returns>
    public bool IsSignatureMutationGuardEnabled()
    {
        return signatureMutationGuardEnabled;
    }

    /// <summary>
    /// Enables the signature mutation guard.
    /// </summary>
    public void EnableSignatureMutationGuard()
    {
        ConfigureSignatureMutationGuard(false);
    }

    /// <summary>
    /// Disables the signature mutation guard.
    /// </summary>
    public void DisableSignatureMutationGuard()
    {
        signatureMutationGuardEnabled = false;
        (headers ?? throw new InvalidOperationException()).SetMutationGuard(null);
        int index = 0;
        while (index < (children ?? throw new InvalidOperationException()).Count)
        {
            children[index].DisableSignatureMutationGuard();
            index++;
        }

        if (nestedMessage != null)
            nestedMessage.DisableSignatureMutationGuard();
    }

    /// <summary>
    /// Determines whether the current value or state is exact dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is exact dirty; otherwise, <c>false</c>.</returns>
    public bool IsExactDirty()
    {
        if (HasLocalExactChange())
        {
            return true;
        }

        int index = 0;
        while (index < (children ?? throw new InvalidOperationException()).Count)
        {
            if (children[index].IsExactDirty())
            {
                return true;
            }

            index++;
        }

        return nestedMessage != null && nestedMessage.IsExactDirty();
    }

    /// <summary>
    /// Determines whether the current value or state is semantic dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is semantic dirty; otherwise, <c>false</c>.</returns>
    public bool IsSemanticDirty()
    {
        if (HasLocalSemanticChange())
        {
            return true;
        }

        int index = 0;
        while (index < (children ?? throw new InvalidOperationException()).Count)
        {
            if (children[index].IsSemanticDirty())
            {
                return true;
            }

            index++;
        }

        return nestedMessage != null && nestedMessage.IsSemanticDirty();
    }

    /// <summary>
    /// Marks the exact dirty.
    /// </summary>
    public void MarkExactDirty()
    {
        EnsureSignatureMutationAllowed();
        forcedExactDirty = true;
        originalEntityContentInvalidated = true;
    }

    /// <summary>
    /// Marks the semantic dirty.
    /// </summary>
    public void MarkSemanticDirty()
    {
        EnsureSignatureMutationAllowed();
        forcedSemanticDirty = true;
        originalEntityContentInvalidated = true;
    }

    /// <summary>
    /// Marks the dirty.
    /// </summary>
    public void MarkDirty()
    {
        EnsureSignatureMutationAllowed();
        forcedExactDirty = true;
        forcedSemanticDirty = true;
        originalEntityContentInvalidated = true;
    }

    /// <summary>
    /// Gets the dirty source ranges.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current dirty source ranges.</returns>
    public List<EmailRange> GetDirtySourceRanges()
    {
        List<EmailRange> ranges = new List<EmailRange>();
        if (!IsExactDirty() || sourceRange == null)
        {
            return ranges;
        }

        if (HasLocalExactChange() || nestedMessage != null && nestedMessage.IsExactDirty())
        {
            ranges.Add(sourceRange);
            return ranges;
        }

        int index = 0;
        while (index < (children ?? throw new InvalidOperationException()).Count)
        {
            if (children[index].IsExactDirty())
            {
                List<EmailRange> childRanges = children[index].GetDirtySourceRanges();
                if (childRanges.Count == 0)
                {
                    ranges.Clear();
                    ranges.Add(sourceRange);
                    return ranges;
                }

                int rangeIndex = 0;
                while (rangeIndex < childRanges.Count)
                {
                    ranges.Add(childRanges[rangeIndex]);
                    rangeIndex++;
                }
            }

            index++;
        }

        return ranges;
    }

    /// <summary>
    /// Determines whether the current value or state is container.
    /// </summary>
    /// <returns><c>true</c> when the value or state is container; otherwise, <c>false</c>.</returns>
    public bool IsContainer()
    {
        return IsContainerKind(kind);
    }

    /// <summary>
    /// Gets the child count.
    /// </summary>
    /// <returns>The current child count.</returns>
    public int GetChildCount()
    {
        return (children ?? throw new InvalidOperationException()).Count;
    }

    /// <summary>
    /// Gets the child at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested child.</returns>
    public MimeEntity GetChildAt(int index)
    {
        ValidateChildIndex(index);
        return (children ?? throw new InvalidOperationException())[index];
    }

    /// <summary>
    /// Gets the children.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current children.</returns>
    public List<MimeEntity> GetChildren()
    {
        return new List<MimeEntity>((children ?? throw new InvalidOperationException()));
    }

    /// <summary>
    /// Adds a child to the current collection or model.
    /// </summary>
    /// <param name = "child">The child used by the operation.</param>
    public void AddChild(MimeEntity child)
    {
        InsertChild((children ?? throw new InvalidOperationException()).Count, child);
    }

    /// <summary>
    /// Inserts a child at the requested position.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "child">The child used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void InsertChild(int index, MimeEntity child)
    {
        if (index < 0 || index > (children ?? throw new InvalidOperationException()).Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        ValidateNewChild(child);
        EnsureSignatureMutationAllowed();
        List<MimeEntity> proposed = new List<MimeEntity>(children);
        proposed.Insert(index, child);
        ValidateMutableChildren(proposed);
        children.Insert(index, child);
        child.parent = this;
    }

    /// <summary>
    /// Replaces the matching child at value or values.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "replacement">The replacement used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity ReplaceChildAt(int index, MimeEntity replacement)
    {
        ValidateChildIndex(index);
        if (object.ReferenceEquals((children ?? throw new InvalidOperationException())[index], replacement))
        {
            return children[index];
        }

        ValidateNewChild(replacement);
        EnsureSignatureMutationAllowed();
        List<MimeEntity> proposed = new List<MimeEntity>(children);
        proposed[index] = replacement;
        ValidateMutableChildren(proposed);
        MimeEntity previous = children[index];
        previous.parent = null;
        children[index] = replacement;
        replacement.parent = this;
        return previous;
    }

    /// <summary>
    /// Removes the matching child at value or values.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    public MimeEntity RemoveChildAt(int index)
    {
        ValidateChildIndex(index);
        EnsureSignatureMutationAllowed();
        List<MimeEntity> proposed = new List<MimeEntity>((children ?? throw new InvalidOperationException()));
        proposed.RemoveAt(index);
        ValidateMutableChildren(proposed);
        MimeEntity removed = children[index];
        children.RemoveAt(index);
        removed.parent = null;
        return removed;
    }

    /// <summary>
    /// Moves the child.
    /// </summary>
    /// <param name = "fromIndex">The zero-based from index.</param>
    /// <param name = "toIndex">The zero-based to index.</param>
    public void MoveChild(int fromIndex, int toIndex)
    {
        ValidateChildIndex(fromIndex);
        ValidateChildIndex(toIndex);
        if (fromIndex == toIndex)
        {
            return;
        }

        EnsureSignatureMutationAllowed();
        List<MimeEntity> proposed = new List<MimeEntity>((children ?? throw new InvalidOperationException()));
        MimeEntity moved = proposed[fromIndex];
        proposed.RemoveAt(fromIndex);
        proposed.Insert(toIndex, moved);
        ValidateMutableChildren(proposed);
        children = proposed;
    }

    /// <summary>
    /// Gets the own issues.
    /// </summary>
    /// <returns>The current own issues.</returns>
    public EmailIssueList GetOwnIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(ownIssues);
        return copy;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList result = GetOwnIssues();
        int headerIndex = 0;
        while (headerIndex < (headers ?? throw new InvalidOperationException()).GetCount())
        {
            result.AddRange(headers.GetAt(headerIndex).GetIssues());
            headerIndex++;
        }

        int childIndex = 0;
        while (childIndex < (children ?? throw new InvalidOperationException()).Count)
        {
            result.AddRange(children[childIndex].GetIssues());
            childIndex++;
        }

        if (nestedMessage != null)
        {
            result.AddRange(nestedMessage.GetIssues());
        }

        return result;
    }

    /// <summary>
    /// Adds an issue to the current collection or model.
    /// </summary>
    /// <param name = "issue">The issue used by the operation.</param>
    public void AddIssue(EmailIssue issue)
    {
        (ownIssues ?? throw new InvalidOperationException()).Add(issue);
    }

    /// <summary>
    /// Clears the stored own issues value or values.
    /// </summary>
    public void ClearOwnIssues()
    {
        (ownIssues ?? throw new InvalidOperationException()).Clear();
    }

    /// <summary>
    /// Materializes the content.
    /// </summary>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The content produced by the operation.</returns>
    public EmailContent MaterializeContent(long maximumBytes)
    {
        if (decodedContent != null)
        {
            return MaterializeDecodedContent(maximumBytes);
        }

        return MaterializeRawContent(maximumBytes);
    }

    /// <summary>
    /// Materializes the raw content.
    /// </summary>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The content produced by the operation.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public EmailContent MaterializeRawContent(long maximumBytes)
    {
        if (rawContent == null)
        {
            throw new InvalidOperationException("The entity has no raw content to materialize.");
        }

        bool baseline = object.ReferenceEquals(rawContent, rawContentSnapshot);
        rawContent = Materialize(rawContent, maximumBytes);
        if (baseline)
        {
            rawContentSnapshot = rawContent;
        }

        return rawContent;
    }

    /// <summary>
    /// Materializes the decoded content.
    /// </summary>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The content produced by the operation.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public EmailContent MaterializeDecodedContent(long maximumBytes)
    {
        if (decodedContent == null)
        {
            throw new InvalidOperationException("The entity has no decoded content to materialize.");
        }

        bool baseline = object.ReferenceEquals(decodedContent, decodedContentSnapshot);
        decodedContent = Materialize(decodedContent, maximumBytes);
        if (baseline)
        {
            decodedContentSnapshot = decodedContent;
        }

        return decodedContent;
    }

    /// <summary>
    /// Replaces the matching content value or values.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    public void ReplaceContent(EmailContent content)
    {
        ValidateContentReplacement(content);
        EnsureSignatureMutationAllowed();
        rawContent = null;
        decodedContent = content;
        bodyIngestMetadata = null;
    }

    /// <summary>
    /// Replaces the matching raw content value or values.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    public void ReplaceRawContent(EmailContent content)
    {
        ValidateContentReplacement(content);
        EnsureSignatureMutationAllowed();
        rawContent = content;
        decodedContent = null;
        bodyIngestMetadata = null;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public MimeEntity Clone()
    {
        List<MimeEntity> childCopies = new List<MimeEntity>();
        int index = 0;
        while (index < (children ?? throw new InvalidOperationException()).Count)
        {
            childCopies.Add(children[index].Clone());
            index++;
        }

        EmailMessage? messageCopy = nestedMessage == null ? null : nestedMessage.Clone();
        MimeEntity clone = new MimeEntity(kind, (headers ?? throw new InvalidOperationException()).Clone(), mediaType, disposition, transferEncodingName, contentId, contentLocation, contentLanguage, contentDescription, partialMessageMetadata, externalBodyMetadata, bodyIngestMetadata, rawContent, decodedContent, childCopies, messageCopy, preamble, epilogue, sourceRange, ownIssues);
        if (originalEntityContent != null)
        {
            clone.RetainOriginalEntityContent(originalEntityContent);
            clone.originalEntityContentInvalidated = originalEntityContentInvalidated;
        }

        if (HasLocalExactChange())
        {
            clone.forcedExactDirty = true;
        }

        if (HasLocalSemanticChange())
        {
            clone.forcedSemanticDirty = true;
        }

        return clone;
    }

    /// <summary>
    /// Detaches the source.
    /// </summary>
    /// <param name = "maximumContentBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void DetachSource(long maximumContentBytes)
    {
        if (maximumContentBytes < 0)
        {
            throw new ArgumentOutOfRangeException("maximumContentBytes");
        }

        bool originalContentWasCurrent = originalEntityContent != null && !HasMutationAgainstOriginalEntityContent();
        EmailContent? detachedRawContent = rawContent == null ? null : MaterializeDetached(rawContent, maximumContentBytes);
        EmailContent? detachedDecodedContent = decodedContent == null ? null : MaterializeDetached(decodedContent, maximumContentBytes);
        EmailContent? detachedPreamble = preamble == null ? null : MaterializeDetached(preamble, maximumContentBytes);
        EmailContent? detachedEpilogue = epilogue == null ? null : MaterializeDetached(epilogue, maximumContentBytes);
        EmailContent? detachedOriginalEntityContent = originalEntityContent == null ? null : MaterializeDetached(originalEntityContent, maximumContentBytes);
        rawContent = detachedRawContent;
        decodedContent = detachedDecodedContent;
        preamble = detachedPreamble;
        epilogue = detachedEpilogue;
        originalEntityContent = detachedOriginalEntityContent;
        int headerIndex = 0;
        while (headerIndex < (headers ?? throw new InvalidOperationException()).GetCount())
        {
            headers.GetAt(headerIndex).DetachSource();
            headerIndex++;
        }

        int childIndex = 0;
        while (childIndex < (children ?? throw new InvalidOperationException()).Count)
        {
            children[childIndex].DetachSource(maximumContentBytes);
            childIndex++;
        }

        if (nestedMessage != null)
        {
            nestedMessage.DetachSource(maximumContentBytes);
        }

        sourceRange = null;
        forcedExactDirty = false;
        forcedSemanticDirty = false;
        CaptureSnapshots();
        if (!originalContentWasCurrent)
        {
            originalEntityContentInvalidated = true;
        }

        forcedExactDirty = true;
    }

    void AttachChildren()
    {
        int index = 0;
        while (index < (children ?? throw new InvalidOperationException()).Count)
        {
            children[index].parent = this;
            index++;
        }
    }

    internal void EnableAllSignatureMutationGuard()
    {
        ConfigureSignatureMutationGuard(true);
    }

    void ConfigureSignatureMutationGuard(bool protectedByAncestor)
    {
        bool protect = protectedByAncestor || kind == MimeEntityKind.Signed || IsOpaqueSignedSmime();
        if (protect)
        {
            signatureMutationGuardEnabled = true;
            (headers ?? throw new InvalidOperationException()).SetMutationGuard(EnsureSignatureMutationAllowed);
        }

        int index = 0;
        while (index < (children ?? throw new InvalidOperationException()).Count)
        {
            children[index].ConfigureSignatureMutationGuard(protect);
            index++;
        }

        if (nestedMessage != null)
        {
            if (protect)
                nestedMessage.EnableAllSignatureMutationGuard();
            else
                nestedMessage.EnableSignatureMutationGuard();
        }
    }

    void EnsureSignatureMutationAllowed()
    {
        if (signatureMutationGuardEnabled)
            throw new EmailSignatureMutationException();
    }

    bool IsOpaqueSignedSmime()
    {
        if (!(mediaType ?? throw new InvalidOperationException()).IsType("application", "pkcs7-mime") && !mediaType.IsType("application", "x-pkcs7-mime"))
            return false;
        EmailParameter? parameter = mediaType.GetParameters().GetFirst("smime-type");
        if (parameter == null)
            return false;
        return string.Equals(System.Text.Encoding.ASCII.GetString(parameter.GetValueBytes()), "signed-data", StringComparison.OrdinalIgnoreCase);
    }

    void CaptureSnapshots()
    {
        headerSnapshots = new List<EmailMessageHeaderSnapshot>();
        int headerIndex = 0;
        while (headerIndex < (headers ?? throw new InvalidOperationException()).GetCount())
        {
            headerSnapshots.Add(new EmailMessageHeaderSnapshot(headers.GetAt(headerIndex)));
            headerIndex++;
        }

        childSnapshots = new List<MimeEntity>((children ?? throw new InvalidOperationException()));
        rawContentSnapshot = rawContent;
        decodedContentSnapshot = decodedContent;
        preambleSnapshot = preamble;
        epilogueSnapshot = epilogue;
        nestedMessageSnapshot = nestedMessage;
        mediaTypeSnapshot = mediaType;
    }

    int CountRefreshChanges(EmailMimeMetadataCandidate candidate, bool mediaAllowed, bool transferAllowed, EmailPartialMessageMetadata? refreshedPartial, EmailExternalBodyMetadata? refreshedExternal)
    {
        int count = 0;
        if (mediaAllowed && !MediaTypeEqual(mediaType, candidate.GetMediaType())) { count++; }
        if (mediaAllowed && kind != candidate.GetKind()) { count++; }
        if (!DispositionEqual(disposition, candidate.GetDisposition())) { count++; }
        if (transferAllowed && !string.Equals(transferEncodingName, candidate.GetTransferEncodingName(), StringComparison.Ordinal)) { count++; }
        if (!MessageIdEqual(contentId, candidate.GetContentId())) { count++; }
        if (!string.Equals(contentLocation, candidate.GetContentLocation(), StringComparison.Ordinal)) { count++; }
        if (!ContentLanguageEqual(contentLanguage, candidate.GetContentLanguage())) { count++; }
        if (!string.Equals(contentDescription, candidate.GetContentDescription(), StringComparison.Ordinal)) { count++; }
        if (!object.ReferenceEquals(partialMessageMetadata, refreshedPartial)) { count++; }
        if (!object.ReferenceEquals(externalBodyMetadata, refreshedExternal)) { count++; }
        return count;
    }

    void ApplyRefresh(EmailMimeMetadataCandidate candidate, bool mediaAllowed, bool transferAllowed, EmailPartialMessageMetadata? refreshedPartial, EmailExternalBodyMetadata? refreshedExternal)
    {
        bool transferSemanticsChanged = transferAllowed && !TransferEncodingEquivalent(transferEncodingName, candidate.GetTransferEncodingName());
        if (mediaAllowed)
        {
            if (!MediaTypeEqual(mediaType, candidate.GetMediaType())) { mediaType = candidate.GetMediaType(); }
            kind = candidate.GetKind();
            partialMessageMetadata = refreshedPartial;
            externalBodyMetadata = refreshedExternal;
        }
        if (!DispositionEqual(disposition, candidate.GetDisposition())) { disposition = candidate.GetDisposition(); }
        if (transferAllowed) { transferEncodingName = candidate.GetTransferEncodingName(); }
        if (!MessageIdEqual(contentId, candidate.GetContentId())) { contentId = candidate.GetContentId(); }
        contentLocation = candidate.GetContentLocation();
        if (!ContentLanguageEqual(contentLanguage, candidate.GetContentLanguage())) { contentLanguage = candidate.GetContentLanguage(); }
        contentDescription = candidate.GetContentDescription();
        if (transferSemanticsChanged && rawContent != null)
        {
            decodedContent = null;
            bodyIngestMetadata = null;
        }
    }

    bool CanAdoptKind(MimeEntityKind proposed)
    {
        if (proposed == kind) { return true; }
        bool proposedContainer = IsContainerKind(proposed);
        if (proposedContainer)
        {
            if (rawContent != null || decodedContent != null || nestedMessage != null) { return false; }
            if ((proposed == MimeEntityKind.Signed || proposed == MimeEntityKind.Encrypted) && (children ?? throw new InvalidOperationException()).Count != 2) { return false; }
            return true;
        }
        if (proposed == MimeEntityKind.Message)
        {
            return nestedMessage != null && (children ?? throw new InvalidOperationException()).Count == 0 && rawContent == null && decodedContent == null;
        }
        return (children ?? throw new InvalidOperationException()).Count == 0 && nestedMessage == null && preamble == null && epilogue == null;
    }

    void AddRefreshConflict(EmailIssueList issues, string description, string recovery, string fieldName)
    {
        EmailHeader? header = (headers ?? throw new InvalidOperationException()).GetFirst(fieldName);
        EmailSourcePosition? position = header == null ? null : header.GetValueSourcePosition();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.MimeMetadataConflict, description, position, null, fieldName, recovery));
    }

    static bool MediaTypeEqual(EmailMediaType? left, EmailMediaType right)
    {
        if (left == null) { return false; }
        return string.Equals(left.GetTopLevelType(), right.GetTopLevelType(), StringComparison.OrdinalIgnoreCase) && string.Equals(left.GetSubtype(), right.GetSubtype(), StringComparison.OrdinalIgnoreCase) && left.WasDefaulted() == right.WasDefaulted() && BytesEqual(left.GetRawBytes(), right.GetRawBytes());
    }

    static bool DispositionEqual(EmailDisposition? left, EmailDisposition? right)
    {
        if (left == null || right == null) { return left == null && right == null; }
        return BytesEqual(left.GetRawBytes(), right.GetRawBytes());
    }

    static bool MessageIdEqual(EmailMessageId? left, EmailMessageId? right)
    {
        if (left == null || right == null) { return left == null && right == null; }
        return BytesEqual(left.GetRawBytes(), right.GetRawBytes());
    }

    static bool ContentLanguageEqual(EmailContentLanguage? left, EmailContentLanguage? right)
    {
        if (left == null || right == null) { return left == null && right == null; }
        return BytesEqual(left.GetRawBytes(), right.GetRawBytes());
    }

    static bool TransferEncodingEquivalent(string? left, string? right)
    {
        string leftValue = left == null ? "7bit" : left;
        string rightValue = right == null ? "7bit" : right;
        return string.Equals(leftValue, rightValue, StringComparison.OrdinalIgnoreCase);
    }

    static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length) { return false; }
        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index]) { return false; }
            index++;
        }
        return true;
    }

    bool HasLocalExactChange()
    {
        if (forcedExactDirty || !object.ReferenceEquals(mediaType, mediaTypeSnapshot) || HasHeaderStructureChange() || HasChildStructureChange() || !object.ReferenceEquals(rawContent, rawContentSnapshot) || !object.ReferenceEquals(decodedContent, decodedContentSnapshot) || !object.ReferenceEquals(preamble, preambleSnapshot) || !object.ReferenceEquals(epilogue, epilogueSnapshot) || !object.ReferenceEquals(nestedMessage, nestedMessageSnapshot))
        {
            return true;
        }

        int index = 0;
        while (index < (headerSnapshots ?? throw new InvalidOperationException()).Count)
        {
            if (!headerSnapshots[index].IsExactMatch((headers ?? throw new InvalidOperationException()).GetAt(index)))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    bool HasMutationAgainstOriginalEntityContent()
    {
        if (originalEntityContentInvalidated || HasHeaderStructureChange() || HasChildStructureChange() || !object.ReferenceEquals(rawContent, rawContentSnapshot) || !object.ReferenceEquals(decodedContent, decodedContentSnapshot) || !object.ReferenceEquals(preamble, preambleSnapshot) || !object.ReferenceEquals(epilogue, epilogueSnapshot) || !object.ReferenceEquals(nestedMessage, nestedMessageSnapshot))
        {
            return true;
        }

        int headerIndex = 0;
        while (headerIndex < (headerSnapshots ?? throw new InvalidOperationException()).Count)
        {
            if (!headerSnapshots[headerIndex].IsExactMatch((headers ?? throw new InvalidOperationException()).GetAt(headerIndex)))
            {
                return true;
            }

            headerIndex++;
        }

        int childIndex = 0;
        while (childIndex < (children ?? throw new InvalidOperationException()).Count)
        {
            MimeEntity child = children[childIndex];
            if (child.originalEntityContent != null)
            {
                if (child.HasMutationAgainstOriginalEntityContent())
                    return true;
            }
            else if (child.IsExactDirty())
            {
                return true;
            }

            childIndex++;
        }

        return nestedMessage != null && nestedMessage.IsExactDirty();
    }

    bool HasLocalSemanticChange()
    {
        if (forcedSemanticDirty || !object.ReferenceEquals(mediaType, mediaTypeSnapshot) || HasHeaderStructureChange() || HasChildStructureChange() || !object.ReferenceEquals(rawContent, rawContentSnapshot) || !object.ReferenceEquals(decodedContent, decodedContentSnapshot) || !object.ReferenceEquals(preamble, preambleSnapshot) || !object.ReferenceEquals(epilogue, epilogueSnapshot) || !object.ReferenceEquals(nestedMessage, nestedMessageSnapshot))
        {
            return true;
        }

        int index = 0;
        while (index < (headerSnapshots ?? throw new InvalidOperationException()).Count)
        {
            if (!headerSnapshots[index].IsSemanticMatch((headers ?? throw new InvalidOperationException()).GetAt(index)))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    bool HasHeaderStructureChange()
    {
        if ((headers ?? throw new InvalidOperationException()).GetCount() != (headerSnapshots ?? throw new InvalidOperationException()).Count)
        {
            return true;
        }

        int index = 0;
        while (index < headerSnapshots.Count)
        {
            if (!object.ReferenceEquals(headers.GetAt(index), headerSnapshots[index].GetHeader()))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    bool HasChildStructureChange()
    {
        if ((children ?? throw new InvalidOperationException()).Count != (childSnapshots ?? throw new InvalidOperationException()).Count)
        {
            return true;
        }

        int index = 0;
        while (index < childSnapshots.Count)
        {
            if (!object.ReferenceEquals(children[index], childSnapshots[index]))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    void ValidateContentReplacement(EmailContent content)
    {
        if (content == null)
        {
            throw new ArgumentNullException("content");
        }

        if (content.IsClosed())
        {
            throw new ObjectDisposedException("content");
        }

        if (IsContainerKind(kind) || kind == MimeEntityKind.Message)
        {
            throw new InvalidOperationException("This MIME entity kind cannot hold direct replacement content.");
        }
    }

    static EmailContent Materialize(EmailContent content, long maximumBytes)
    {
        if (content.GetKind() == EmailContentKind.Memory && content.GetOwnership() == EmailContentOwnership.Owned)
        {
            return content;
        }

        byte[] bytes = content.Materialize(maximumBytes);
        return new EmailContentOperations().CreateMemory(bytes, EmailContentOwnership.Owned, false);
    }

    static EmailContent MaterializeDetached(EmailContent content, long maximumBytes)
    {
        byte[] bytes = content.Materialize(maximumBytes);
        return new EmailContentOperations().CreateMemory(bytes, EmailContentOwnership.Owned, false);
    }

    void ValidateNewChild(MimeEntity child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        if (object.ReferenceEquals(child, this) || IsAncestorOf(child, this))
        {
            throw new ArgumentException("Adding the entity would create a MIME tree cycle.", "child");
        }

        if (child.parent != null)
        {
            throw new ArgumentException("A MIME entity cannot belong to more than one parent.", "child");
        }
    }

    void ValidateMutableChildren(List<MimeEntity> proposed)
    {
        if (!IsContainerKind(kind))
        {
            throw new InvalidOperationException("This MIME entity kind cannot contain child entities.");
        }

        if ((kind == MimeEntityKind.Signed || kind == MimeEntityKind.Encrypted) && proposed.Count != 2)
        {
            throw new InvalidOperationException("Signed and encrypted MIME entities must contain exactly two children.");
        }
    }

    void ValidateChildIndex(int index)
    {
        if (index < 0 || index >= (children ?? throw new InvalidOperationException()).Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }
    }

    static void ValidateKind(MimeEntityKind value)
    {
        if (value < MimeEntityKind.Leaf || value > MimeEntityKind.Encrypted)
        {
            throw new ArgumentOutOfRangeException("value");
        }
    }

    static void ValidateOptionalText(string? value, string parameterName)
    {
        if (value != null && value.Length == 0)
        {
            throw new ArgumentException("Optional MIME metadata cannot be empty when supplied.", parameterName);
        }
    }

    static void ValidateTransferEncoding(string? value)
    {
        if (value == null)
        {
            return;
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("A content-transfer-encoding name cannot be empty.", "value");
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == '(' || current == ')' || current == '<' || current == '>' || current == '@' || current == ',' || current == ';' || current == ':' || current == '\\' || current == '"' || current == '/' || current == '[' || current == ']' || current == '?' || current == '=')
            {
                throw new ArgumentException("A content-transfer-encoding name must be a MIME token.", "value");
            }

            index++;
        }
    }

    static void ValidateMediaType(MimeEntityKind kind, EmailMediaType value)
    {
        bool valid = false;
        if (kind == MimeEntityKind.Leaf)
        {
            valid = IsLeafMediaType(value);
        }
        else if (kind == MimeEntityKind.Multipart)
        {
            valid = string.Equals(value.GetTopLevelType(), "multipart", StringComparison.OrdinalIgnoreCase) && !value.IsType("multipart", "signed") && !value.IsType("multipart", "encrypted");
        }
        else if (kind == MimeEntityKind.Message)
        {
            valid = value.IsType("message", "rfc822") || value.IsType("message", "global");
        }
        else if (kind == MimeEntityKind.PartialMessage)
        {
            valid = value.IsType("message", "partial");
        }
        else if (kind == MimeEntityKind.ExternalBody)
        {
            valid = value.IsType("message", "external-body");
        }
        else if (kind == MimeEntityKind.DeliveryStatus)
        {
            valid = value.IsType("message", "delivery-status") || value.IsType("message", "global-delivery-status");
        }
        else if (kind == MimeEntityKind.DispositionNotification)
        {
            valid = value.IsType("message", "disposition-notification") || value.IsType("message", "global-disposition-notification");
        }
        else if (kind == MimeEntityKind.FeedbackReport)
        {
            valid = value.IsType("message", "feedback-report");
        }
        else if (kind == MimeEntityKind.Calendar)
        {
            valid = value.IsType("text", "calendar");
        }
        else if (kind == MimeEntityKind.Tnef)
        {
            valid = value.IsType("application", "ms-tnef") || value.IsType("application", "vnd.ms-tnef") || value.IsType("application", "x-tnef");
        }
        else if (kind == MimeEntityKind.Signed)
        {
            valid = value.IsType("multipart", "signed");
        }
        else if (kind == MimeEntityKind.Encrypted)
        {
            valid = value.IsType("multipart", "encrypted");
        }

        if (!valid)
        {
            throw new ArgumentException("The media type is not compatible with the MIME entity kind.", "value");
        }
    }

    static bool IsLeafMediaType(EmailMediaType value)
    {
        if (string.Equals(value.GetTopLevelType(), "multipart", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (value.IsType("message", "rfc822") || value.IsType("message", "global") || value.IsType("message", "partial") || value.IsType("message", "external-body") || value.IsType("message", "delivery-status") || value.IsType("message", "global-delivery-status") || value.IsType("message", "disposition-notification") || value.IsType("message", "global-disposition-notification") || value.IsType("message", "feedback-report"))
        {
            return false;
        }

        if (value.IsType("text", "calendar"))
        {
            return false;
        }

        return !value.IsType("application", "ms-tnef") && !value.IsType("application", "vnd.ms-tnef") && !value.IsType("application", "x-tnef");
    }

    static void ValidateSpecializedMetadata(MimeEntityKind kind, EmailPartialMessageMetadata? partialMetadata, EmailExternalBodyMetadata? externalMetadata)
    {
        if (partialMetadata != null && kind != MimeEntityKind.PartialMessage)
        {
            throw new ArgumentException("Partial-message metadata requires a partial-message MIME entity.", "partialMetadata");
        }

        if (externalMetadata != null && kind != MimeEntityKind.ExternalBody)
        {
            throw new ArgumentException("External-body metadata requires an external-body MIME entity.", "externalMetadata");
        }
    }

    static void ValidateShape(MimeEntityKind kind, EmailBodyIngestMetadata? bodyIngestMetadata, EmailContent? rawContent, EmailContent? decodedContent, List<MimeEntity> children, EmailMessage? nestedMessage, EmailContent? preamble, EmailContent? epilogue)
    {
        bool container = IsContainerKind(kind);
        bool message = kind == MimeEntityKind.Message;
        if ((container || message) && bodyIngestMetadata != null)
        {
            throw new ArgumentException("Body-ingest metadata requires a direct-content MIME entity.", "bodyIngestMetadata");
        }

        if (container)
        {
            if (rawContent != null || decodedContent != null || nestedMessage != null)
            {
                throw new ArgumentException("Container MIME entities cannot also hold direct content or a nested message.", "rawContent");
            }

            if ((kind == MimeEntityKind.Signed || kind == MimeEntityKind.Encrypted) && children.Count != 2)
            {
                throw new ArgumentException("Signed and encrypted MIME entities require exactly two children.", "children");
            }
        }
        else if (message)
        {
            if (nestedMessage == null || rawContent != null || decodedContent != null || children.Count != 0 || preamble != null || epilogue != null)
            {
                throw new ArgumentException("A nested-message entity must hold exactly one EmailMessage and no direct content or children.", "nestedMessage");
            }
        }
        else
        {
            bool discarded = bodyIngestMetadata != null && bodyIngestMetadata.GetStorageMode() == EmailBodyStorageMode.Discard;
            if (discarded && (rawContent != null || decodedContent != null))
            {
                throw new ArgumentException("A discarded MIME body cannot retain direct content.", "bodyIngestMetadata");
            }

            if (rawContent == null && decodedContent == null && !discarded)
            {
                throw new ArgumentException("A content MIME entity requires raw or decoded content.", "rawContent");
            }

            if (nestedMessage != null || children.Count != 0 || preamble != null || epilogue != null)
            {
                throw new ArgumentException("A content MIME entity cannot hold children, a nested message, a preamble, or an epilogue.", "children");
            }
        }

        if (!container && (preamble != null || epilogue != null))
        {
            throw new ArgumentException("Only container MIME entities can have a preamble or epilogue.", "preamble");
        }
    }

    static void ValidateChildren(List<MimeEntity> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            MimeEntity child = values[index];
            if (child == null)
            {
                throw new ArgumentException("MIME child collections cannot contain null entries.", "values");
            }

            if (child.parent != null)
            {
                throw new ArgumentException("A MIME entity cannot belong to more than one parent.", "values");
            }

            int duplicateIndex = 0;
            while (duplicateIndex < index)
            {
                if (object.ReferenceEquals(values[duplicateIndex], child))
                {
                    throw new ArgumentException("A MIME entity cannot occur twice under one parent.", "values");
                }

                duplicateIndex++;
            }

            index++;
        }
    }

    static bool IsContainerKind(MimeEntityKind value)
    {
        return value == MimeEntityKind.Multipart || value == MimeEntityKind.Signed || value == MimeEntityKind.Encrypted;
    }

    static bool IsAncestorOf(MimeEntity possibleAncestor, MimeEntity value)
    {
        MimeEntity? current = value;
        while (current != null)
        {
            if (object.ReferenceEquals(current, possibleAncestor))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    static bool IsKnownTransferEncoding(string? value)
    {
        if (value == null)
        {
            return false;
        }

        return string.Equals(value, "7bit", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "8bit", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "binary", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "quoted-printable", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "base64", StringComparison.OrdinalIgnoreCase);
    }
}
