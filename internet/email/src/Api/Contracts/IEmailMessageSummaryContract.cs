using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageSummary"/>.
/// </summary>
public interface IEmailMessageSummaryContract
{
    /// <summary>
    /// Gets the header count.
    /// </summary>
    /// <returns>The current header count.</returns>
    int GetHeaderCount();
    /// <summary>
    /// Determines whether the raw message byte count is present or available.
    /// </summary>
    /// <returns><c>true</c> when the raw message byte count is available; otherwise, <c>false</c>.</returns>
    bool HasRawMessageByteCount();
    /// <summary>
    /// Gets the raw message byte count.
    /// </summary>
    /// <returns>The current raw message byte count.</returns>
    long GetRawMessageByteCount();
    /// <summary>
    /// Gets the raw content byte count.
    /// </summary>
    /// <returns>The current raw content byte count.</returns>
    long GetRawContentByteCount();
    /// <summary>
    /// Determines whether the current value or state is raw content byte count complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is raw content byte count complete; otherwise, <c>false</c>.</returns>
    bool IsRawContentByteCountComplete();
    /// <summary>
    /// Gets the decoded content byte count.
    /// </summary>
    /// <returns>The current decoded content byte count.</returns>
    long GetDecodedContentByteCount();
    /// <summary>
    /// Determines whether the current value or state is decoded content byte count complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is decoded content byte count complete; otherwise, <c>false</c>.</returns>
    bool IsDecodedContentByteCountComplete();
    /// <summary>
    /// Gets the part count.
    /// </summary>
    /// <returns>The current part count.</returns>
    int GetPartCount();
    /// <summary>
    /// Gets the leaf part count.
    /// </summary>
    /// <returns>The current leaf part count.</returns>
    int GetLeafPartCount();
    /// <summary>
    /// Gets the container part count.
    /// </summary>
    /// <returns>The current container part count.</returns>
    int GetContainerPartCount();
    /// <summary>
    /// Gets the maximum depth.
    /// </summary>
    /// <returns>The current maximum depth.</returns>
    int GetMaximumDepth();
    /// <summary>
    /// Gets the distinct media type count.
    /// </summary>
    /// <returns>The current distinct media type count.</returns>
    int GetDistinctMediaTypeCount();
    /// <summary>
    /// Gets the media type at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested media type.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailMediaTypeStatistic GetMediaTypeAt(int index);
    /// <summary>
    /// Gets the media types.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current media types.</returns>
    List<EmailMediaTypeStatistic> GetMediaTypes();
    /// <summary>
    /// Gets the media type entity count.
    /// </summary>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The current media type entity count.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    int GetMediaTypeEntityCount(string topLevelType, string subtype);
    /// <summary>
    /// Gets the to recipient count.
    /// </summary>
    /// <returns>The current to recipient count.</returns>
    int GetToRecipientCount();
    /// <summary>
    /// Gets the cc recipient count.
    /// </summary>
    /// <returns>The current cc recipient count.</returns>
    int GetCcRecipientCount();
    /// <summary>
    /// Gets the bcc recipient count.
    /// </summary>
    /// <returns>The current bcc recipient count.</returns>
    int GetBccRecipientCount();
    /// <summary>
    /// Gets the header recipient count.
    /// </summary>
    /// <returns>The current header recipient count.</returns>
    int GetHeaderRecipientCount();
    /// <summary>
    /// Gets the resent recipient count.
    /// </summary>
    /// <returns>The current resent recipient count.</returns>
    int GetResentRecipientCount();
    /// <summary>
    /// Gets the envelope recipient count.
    /// </summary>
    /// <returns>The current envelope recipient count.</returns>
    int GetEnvelopeRecipientCount();
    /// <summary>
    /// Determines whether the date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the date is available; otherwise, <c>false</c>.</returns>
    bool HasDate();
    /// <summary>
    /// Gets the date.
    /// </summary>
    /// <returns>The current date.</returns>
    EmailDateTime? GetDate();
    /// <summary>
    /// Gets the resent date count.
    /// </summary>
    /// <returns>The current resent date count.</returns>
    int GetResentDateCount();
    /// <summary>
    /// Gets the resent date at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested resent date.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailDateTime GetResentDateAt(int index);
    /// <summary>
    /// Gets the resent dates.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current resent dates.</returns>
    List<EmailDateTime> GetResentDates();
    /// <summary>
    /// Gets the received date count.
    /// </summary>
    /// <returns>The current received date count.</returns>
    int GetReceivedDateCount();
    /// <summary>
    /// Gets the received date at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested received date.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailDateTime GetReceivedDateAt(int index);
    /// <summary>
    /// Gets the received dates.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current received dates.</returns>
    List<EmailDateTime> GetReceivedDates();
    /// <summary>
    /// Gets the attachment count.
    /// </summary>
    /// <returns>The current attachment count.</returns>
    int GetAttachmentCount();
    /// <summary>
    /// Gets the inline resource count.
    /// </summary>
    /// <returns>The current inline resource count.</returns>
    int GetInlineResourceCount();
    /// <summary>
    /// Gets the nested message count.
    /// </summary>
    /// <returns>The current nested message count.</returns>
    int GetNestedMessageCount();
    /// <summary>
    /// Gets the attachment raw byte count.
    /// </summary>
    /// <returns>The current attachment raw byte count.</returns>
    long GetAttachmentRawByteCount();
    /// <summary>
    /// Determines whether the current value or state is attachment raw byte count complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is attachment raw byte count complete; otherwise, <c>false</c>.</returns>
    bool IsAttachmentRawByteCountComplete();
    /// <summary>
    /// Gets the attachment decoded byte count.
    /// </summary>
    /// <returns>The current attachment decoded byte count.</returns>
    long GetAttachmentDecodedByteCount();
    /// <summary>
    /// Determines whether the current value or state is attachment decoded byte count complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is attachment decoded byte count complete; otherwise, <c>false</c>.</returns>
    bool IsAttachmentDecodedByteCountComplete();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the issue count.
    /// </summary>
    /// <returns>The current issue count.</returns>
    int GetIssueCount();
    /// <summary>
    /// Gets the information issue count.
    /// </summary>
    /// <returns>The current information issue count.</returns>
    int GetInformationIssueCount();
    /// <summary>
    /// Gets the warning issue count.
    /// </summary>
    /// <returns>The current warning issue count.</returns>
    int GetWarningIssueCount();
    /// <summary>
    /// Gets the error issue count.
    /// </summary>
    /// <returns>The current error issue count.</returns>
    int GetErrorIssueCount();
    /// <summary>
    /// Gets the fatal issue count.
    /// </summary>
    /// <returns>The current fatal issue count.</returns>
    int GetFatalIssueCount();
    /// <summary>
    /// Gets the security summary.
    /// </summary>
    /// <returns>The current security summary.</returns>
    EmailSecuritySummary GetSecuritySummary();
}
