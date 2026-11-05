using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "MimeEntity"/>.
/// </summary>
public interface IMimeEntityContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    MimeEntityKind GetKind();
    /// <summary>
    /// Gets the headers.
    /// </summary>
    /// <returns>The current headers.</returns>
    EmailHeaders? GetHeaders();
    /// <summary>
    /// Gets the media type.
    /// </summary>
    /// <returns>The current media type.</returns>
    EmailMediaType? GetMediaType();
    /// <summary>
    /// Sets the media type used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetMediaType(EmailMediaType value);
    /// <summary>
    /// Determines whether the disposition is present or available.
    /// </summary>
    /// <returns><c>true</c> when the disposition is available; otherwise, <c>false</c>.</returns>
    bool HasDisposition();
    /// <summary>
    /// Gets the disposition.
    /// </summary>
    /// <returns>The current disposition.</returns>
    EmailDisposition? GetDisposition();
    /// <summary>
    /// Determines whether the transfer encoding is present or available.
    /// </summary>
    /// <returns><c>true</c> when the transfer encoding is available; otherwise, <c>false</c>.</returns>
    bool HasTransferEncoding();
    /// <summary>
    /// Gets the transfer encoding name.
    /// </summary>
    /// <returns>The current transfer encoding name.</returns>
    string? GetTransferEncodingName();
    /// <summary>
    /// Determines whether the operation or source value was transfer encoding defaulted.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasTransferEncodingDefaulted();
    /// <summary>
    /// Gets the effective transfer encoding name.
    /// </summary>
    /// <returns>The current effective transfer encoding name.</returns>
    string GetEffectiveTransferEncodingName();
    /// <summary>
    /// Determines whether the known transfer encoding is present or available.
    /// </summary>
    /// <returns><c>true</c> when the known transfer encoding is available; otherwise, <c>false</c>.</returns>
    bool HasKnownTransferEncoding();
    /// <summary>
    /// Gets the transfer encoding kind.
    /// </summary>
    /// <returns>The current transfer encoding kind.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    EmailTransferEncodingKind GetTransferEncodingKind();
    /// <summary>
    /// Gets the effective transfer encoding kind.
    /// </summary>
    /// <returns>The current effective transfer encoding kind.</returns>
    EmailTransferEncodingKind GetEffectiveTransferEncodingKind();
    /// <summary>
    /// Determines whether the content ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content ID is available; otherwise, <c>false</c>.</returns>
    bool HasContentId();
    /// <summary>
    /// Gets the content ID.
    /// </summary>
    /// <returns>The current content ID.</returns>
    EmailMessageId? GetContentId();
    /// <summary>
    /// Determines whether the content location is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content location is available; otherwise, <c>false</c>.</returns>
    bool HasContentLocation();
    /// <summary>
    /// Gets the content location.
    /// </summary>
    /// <returns>The current content location.</returns>
    string? GetContentLocation();
    /// <summary>
    /// Determines whether the content language is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content language is available; otherwise, <c>false</c>.</returns>
    bool HasContentLanguage();
    /// <summary>
    /// Gets the content language.
    /// </summary>
    /// <returns>The current content language.</returns>
    EmailContentLanguage? GetContentLanguage();
    /// <summary>
    /// Determines whether the content description is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content description is available; otherwise, <c>false</c>.</returns>
    bool HasContentDescription();
    /// <summary>
    /// Gets the content description.
    /// </summary>
    /// <returns>The current content description.</returns>
    string? GetContentDescription();
    /// <summary>Refreshes derived MIME metadata from current content headers.</summary>
    /// <returns>The metadata refresh result.</returns>
    EmailMimeMetadataRefreshResult RefreshMetadata();
    /// <summary>Refreshes derived MIME metadata from current content headers with options.</summary>
    /// <param name = "options">The parsing and resource-limit policy.</param>
    /// <returns>The metadata refresh result.</returns>
    EmailMimeMetadataRefreshResult RefreshMetadataWithOptions(EmailParseOptions options);
    /// <summary>
    /// Determines whether the partial message metadata is present or available.
    /// </summary>
    /// <returns><c>true</c> when the partial message metadata is available; otherwise, <c>false</c>.</returns>
    bool HasPartialMessageMetadata();
    /// <summary>
    /// Gets the partial message metadata.
    /// </summary>
    /// <returns>The current partial message metadata.</returns>
    EmailPartialMessageMetadata? GetPartialMessageMetadata();
    /// <summary>
    /// Determines whether the external body metadata is present or available.
    /// </summary>
    /// <returns><c>true</c> when the external body metadata is available; otherwise, <c>false</c>.</returns>
    bool HasExternalBodyMetadata();
    /// <summary>
    /// Gets the external body metadata.
    /// </summary>
    /// <returns>The current external body metadata.</returns>
    EmailExternalBodyMetadata? GetExternalBodyMetadata();
    /// <summary>
    /// Determines whether the body ingest metadata is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body ingest metadata is available; otherwise, <c>false</c>.</returns>
    bool HasBodyIngestMetadata();
    /// <summary>
    /// Gets the body ingest metadata.
    /// </summary>
    /// <returns>The current body ingest metadata.</returns>
    EmailBodyIngestMetadata? GetBodyIngestMetadata();
    /// <summary>
    /// Determines whether the content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content is available; otherwise, <c>false</c>.</returns>
    bool HasContent();
    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <returns>The current content.</returns>
    EmailContent? GetContent();
    /// <summary>
    /// Determines whether the raw content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the raw content is available; otherwise, <c>false</c>.</returns>
    bool HasRawContent();
    /// <summary>
    /// Gets the raw content.
    /// </summary>
    /// <returns>The current raw content.</returns>
    EmailContent? GetRawContent();
    /// <summary>
    /// Determines whether the decoded content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the decoded content is available; otherwise, <c>false</c>.</returns>
    bool HasDecodedContent();
    /// <summary>
    /// Gets the decoded content.
    /// </summary>
    /// <returns>The current decoded content.</returns>
    EmailContent? GetDecodedContent();
    /// <summary>
    /// Determines whether the nested message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the nested message is available; otherwise, <c>false</c>.</returns>
    bool HasNestedMessage();
    /// <summary>
    /// Gets the nested message.
    /// </summary>
    /// <returns>The current nested message.</returns>
    EmailMessage? GetNestedMessage();
    /// <summary>
    /// Determines whether the preamble is present or available.
    /// </summary>
    /// <returns><c>true</c> when the preamble is available; otherwise, <c>false</c>.</returns>
    bool HasPreamble();
    /// <summary>
    /// Gets the preamble.
    /// </summary>
    /// <returns>The current preamble.</returns>
    EmailContent? GetPreamble();
    /// <summary>
    /// Determines whether the epilogue is present or available.
    /// </summary>
    /// <returns><c>true</c> when the epilogue is available; otherwise, <c>false</c>.</returns>
    bool HasEpilogue();
    /// <summary>
    /// Gets the epilogue.
    /// </summary>
    /// <returns>The current epilogue.</returns>
    EmailContent? GetEpilogue();
    /// <summary>
    /// Determines whether the source range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source range is available; otherwise, <c>false</c>.</returns>
    bool HasSourceRange();
    /// <summary>
    /// Gets the source range.
    /// </summary>
    /// <returns>The current source range.</returns>
    EmailRange? GetSourceRange();
    /// <summary>
    /// Determines whether the original entity content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original entity content is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalEntityContent();
    /// <summary>
    /// Gets the original entity content.
    /// </summary>
    /// <returns>The current original entity content.</returns>
    EmailContent? GetOriginalEntityContent();
    /// <summary>
    /// Determines whether the current original entity content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the current original entity content is available; otherwise, <c>false</c>.</returns>
    bool HasCurrentOriginalEntityContent();
    /// <summary>
    /// Determines whether the parent is present or available.
    /// </summary>
    /// <returns><c>true</c> when the parent is available; otherwise, <c>false</c>.</returns>
    bool HasParent();
    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <returns>The current parent.</returns>
    MimeEntity? GetParent();
    /// <summary>
    /// Gets the dirty state.
    /// </summary>
    /// <returns>The current dirty state.</returns>
    EmailDirtyState GetDirtyState();
    /// <summary>
    /// Determines whether the current value or state is dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is dirty; otherwise, <c>false</c>.</returns>
    bool IsDirty();
    /// <summary>
    /// Determines whether the current value or state is signature mutation guard enabled.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signature mutation guard enabled; otherwise, <c>false</c>.</returns>
    bool IsSignatureMutationGuardEnabled();
    /// <summary>
    /// Enables the signature mutation guard.
    /// </summary>
    void EnableSignatureMutationGuard();
    /// <summary>
    /// Disables the signature mutation guard.
    /// </summary>
    void DisableSignatureMutationGuard();
    /// <summary>
    /// Determines whether the current value or state is exact dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is exact dirty; otherwise, <c>false</c>.</returns>
    bool IsExactDirty();
    /// <summary>
    /// Determines whether the current value or state is semantic dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is semantic dirty; otherwise, <c>false</c>.</returns>
    bool IsSemanticDirty();
    /// <summary>
    /// Marks the exact dirty.
    /// </summary>
    void MarkExactDirty();
    /// <summary>
    /// Marks the semantic dirty.
    /// </summary>
    void MarkSemanticDirty();
    /// <summary>
    /// Marks the dirty.
    /// </summary>
    void MarkDirty();
    /// <summary>
    /// Gets the dirty source ranges.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current dirty source ranges.</returns>
    List<EmailRange> GetDirtySourceRanges();
    /// <summary>
    /// Determines whether the current value or state is container.
    /// </summary>
    /// <returns><c>true</c> when the value or state is container; otherwise, <c>false</c>.</returns>
    bool IsContainer();
    /// <summary>
    /// Gets the child count.
    /// </summary>
    /// <returns>The current child count.</returns>
    int GetChildCount();
    /// <summary>
    /// Gets the child at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested child.</returns>
    MimeEntity GetChildAt(int index);
    /// <summary>
    /// Gets the children.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current children.</returns>
    List<MimeEntity> GetChildren();
    /// <summary>
    /// Adds a child to the current collection or model.
    /// </summary>
    /// <param name = "child">The child used by the operation.</param>
    void AddChild(MimeEntity child);
    /// <summary>
    /// Inserts a child at the requested position.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "child">The child used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void InsertChild(int index, MimeEntity child);
    /// <summary>
    /// Replaces the matching child at value or values.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "replacement">The replacement used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity ReplaceChildAt(int index, MimeEntity replacement);
    /// <summary>
    /// Removes the matching child at value or values.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity RemoveChildAt(int index);
    /// <summary>
    /// Moves the child.
    /// </summary>
    /// <param name = "fromIndex">The zero-based from index.</param>
    /// <param name = "toIndex">The zero-based to index.</param>
    void MoveChild(int fromIndex, int toIndex);
    /// <summary>
    /// Gets the own issues.
    /// </summary>
    /// <returns>The current own issues.</returns>
    EmailIssueList GetOwnIssues();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Adds an issue to the current collection or model.
    /// </summary>
    /// <param name = "issue">The issue used by the operation.</param>
    void AddIssue(EmailIssue issue);
    /// <summary>
    /// Clears the stored own issues value or values.
    /// </summary>
    void ClearOwnIssues();
    /// <summary>
    /// Materializes the content.
    /// </summary>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The content produced by the operation.</returns>
    EmailContent MaterializeContent(long maximumBytes);
    /// <summary>
    /// Materializes the raw content.
    /// </summary>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The content produced by the operation.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    EmailContent MaterializeRawContent(long maximumBytes);
    /// <summary>
    /// Materializes the decoded content.
    /// </summary>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The content produced by the operation.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    EmailContent MaterializeDecodedContent(long maximumBytes);
    /// <summary>
    /// Replaces the matching content value or values.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    void ReplaceContent(EmailContent content);
    /// <summary>
    /// Replaces the matching raw content value or values.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    void ReplaceRawContent(EmailContent content);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    MimeEntity Clone();
    /// <summary>
    /// Detaches the source.
    /// </summary>
    /// <param name = "maximumContentBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void DetachSource(long maximumContentBytes);
}
