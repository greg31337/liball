using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a message summary.
/// </summary>
public class EmailMessageSummary : IEmailMessageSummaryContract
{
    int headerCount;
    bool rawMessageByteCountPresent;
    long rawMessageByteCount;
    long rawContentByteCount;
    bool rawContentByteCountComplete;
    long decodedContentByteCount;
    bool decodedContentByteCountComplete;
    int partCount;
    int leafPartCount;
    int containerPartCount;
    int maximumDepth;
    List<EmailMediaTypeStatistic> mediaTypes;
    int toRecipientCount;
    int ccRecipientCount;
    int bccRecipientCount;
    int resentRecipientCount;
    int envelopeRecipientCount;
    EmailDateTime? messageDate;
    List<EmailDateTime> resentDates;
    List<EmailDateTime> receivedDates;
    int attachmentCount;
    int inlineResourceCount;
    int nestedMessageCount;
    long attachmentRawByteCount;
    bool attachmentRawByteCountComplete;
    long attachmentDecodedByteCount;
    bool attachmentDecodedByteCountComplete;
    EmailIssueList issues;
    EmailSecuritySummary securitySummary;
    /// <summary>
    /// Creates a new message summary.
    /// </summary>
    /// <param name = "headerCount">The number of items or bytes to process.</param>
    /// <param name = "rawMessageByteCountPresent">Whether to enable the raw message byte count present behavior.</param>
    /// <param name = "rawMessageByteCount">The number of items or bytes to process.</param>
    /// <param name = "rawContentByteCount">The number of items or bytes to process.</param>
    /// <param name = "rawContentByteCountComplete">Whether to enable the raw content byte count complete behavior.</param>
    /// <param name = "decodedContentByteCount">The number of items or bytes to process.</param>
    /// <param name = "decodedContentByteCountComplete">Whether to enable the decoded content byte count complete behavior.</param>
    /// <param name = "partCount">The number of items or bytes to process.</param>
    /// <param name = "leafPartCount">The number of items or bytes to process.</param>
    /// <param name = "containerPartCount">The number of items or bytes to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "mediaTypes">The media types used by the operation.</param>
    /// <param name = "toRecipientCount">The number of items or bytes to process.</param>
    /// <param name = "ccRecipientCount">The number of items or bytes to process.</param>
    /// <param name = "bccRecipientCount">The number of items or bytes to process.</param>
    /// <param name = "resentRecipientCount">The number of items or bytes to process.</param>
    /// <param name = "envelopeRecipientCount">The number of items or bytes to process.</param>
    /// <param name = "messageDate">The message date used by the operation.</param>
    /// <param name = "resentDates">The resent dates used by the operation.</param>
    /// <param name = "receivedDates">The received dates used by the operation.</param>
    /// <param name = "attachmentCount">The number of items or bytes to process.</param>
    /// <param name = "inlineResourceCount">The number of items or bytes to process.</param>
    /// <param name = "nestedMessageCount">The number of items or bytes to process.</param>
    /// <param name = "attachmentRawByteCount">The number of items or bytes to process.</param>
    /// <param name = "attachmentRawByteCountComplete">Whether to enable the attachment raw byte count complete behavior.</param>
    /// <param name = "attachmentDecodedByteCount">The number of items or bytes to process.</param>
    /// <param name = "attachmentDecodedByteCountComplete">Whether to enable the attachment decoded byte count complete behavior.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "securitySummary">The security summary used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMessageSummary(int headerCount, bool rawMessageByteCountPresent, long rawMessageByteCount, long rawContentByteCount, bool rawContentByteCountComplete, long decodedContentByteCount, bool decodedContentByteCountComplete, int partCount, int leafPartCount, int containerPartCount, int maximumDepth, List<EmailMediaTypeStatistic> mediaTypes, int toRecipientCount, int ccRecipientCount, int bccRecipientCount, int resentRecipientCount, int envelopeRecipientCount, EmailDateTime? messageDate, List<EmailDateTime> resentDates, List<EmailDateTime> receivedDates, int attachmentCount, int inlineResourceCount, int nestedMessageCount, long attachmentRawByteCount, bool attachmentRawByteCountComplete, long attachmentDecodedByteCount, bool attachmentDecodedByteCountComplete, EmailIssueList issues, EmailSecuritySummary securitySummary)
    {
        if (mediaTypes == null)
            throw new ArgumentNullException("mediaTypes");
        if (resentDates == null)
            throw new ArgumentNullException("resentDates");
        if (receivedDates == null)
            throw new ArgumentNullException("receivedDates");
        if (issues == null)
            throw new ArgumentNullException("issues");
        if (securitySummary == null)
            throw new ArgumentNullException("securitySummary");
        this.headerCount = headerCount;
        this.rawMessageByteCountPresent = rawMessageByteCountPresent;
        this.rawMessageByteCount = rawMessageByteCount;
        this.rawContentByteCount = rawContentByteCount;
        this.rawContentByteCountComplete = rawContentByteCountComplete;
        this.decodedContentByteCount = decodedContentByteCount;
        this.decodedContentByteCountComplete = decodedContentByteCountComplete;
        this.partCount = partCount;
        this.leafPartCount = leafPartCount;
        this.containerPartCount = containerPartCount;
        this.maximumDepth = maximumDepth;
        this.mediaTypes = new List<EmailMediaTypeStatistic>(mediaTypes);
        this.toRecipientCount = toRecipientCount;
        this.ccRecipientCount = ccRecipientCount;
        this.bccRecipientCount = bccRecipientCount;
        this.resentRecipientCount = resentRecipientCount;
        this.envelopeRecipientCount = envelopeRecipientCount;
        this.messageDate = messageDate;
        this.resentDates = new List<EmailDateTime>(resentDates);
        this.receivedDates = new List<EmailDateTime>(receivedDates);
        this.attachmentCount = attachmentCount;
        this.inlineResourceCount = inlineResourceCount;
        this.nestedMessageCount = nestedMessageCount;
        this.attachmentRawByteCount = attachmentRawByteCount;
        this.attachmentRawByteCountComplete = attachmentRawByteCountComplete;
        this.attachmentDecodedByteCount = attachmentDecodedByteCount;
        this.attachmentDecodedByteCountComplete = attachmentDecodedByteCountComplete;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.securitySummary = securitySummary;
    }

    /// <summary>
    /// Gets the header count.
    /// </summary>
    /// <returns>The current header count.</returns>
    public int GetHeaderCount()
    {
        return headerCount;
    }

    /// <summary>
    /// Determines whether the raw message byte count is present or available.
    /// </summary>
    /// <returns><c>true</c> when the raw message byte count is available; otherwise, <c>false</c>.</returns>
    public bool HasRawMessageByteCount()
    {
        return rawMessageByteCountPresent;
    }

    /// <summary>
    /// Gets the raw message byte count.
    /// </summary>
    /// <returns>The current raw message byte count.</returns>
    public long GetRawMessageByteCount()
    {
        return rawMessageByteCountPresent ? rawMessageByteCount : 0;
    }

    /// <summary>
    /// Gets the raw content byte count.
    /// </summary>
    /// <returns>The current raw content byte count.</returns>
    public long GetRawContentByteCount()
    {
        return rawContentByteCount;
    }

    /// <summary>
    /// Determines whether the current value or state is raw content byte count complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is raw content byte count complete; otherwise, <c>false</c>.</returns>
    public bool IsRawContentByteCountComplete()
    {
        return rawContentByteCountComplete;
    }

    /// <summary>
    /// Gets the decoded content byte count.
    /// </summary>
    /// <returns>The current decoded content byte count.</returns>
    public long GetDecodedContentByteCount()
    {
        return decodedContentByteCount;
    }

    /// <summary>
    /// Determines whether the current value or state is decoded content byte count complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is decoded content byte count complete; otherwise, <c>false</c>.</returns>
    public bool IsDecodedContentByteCountComplete()
    {
        return decodedContentByteCountComplete;
    }

    /// <summary>
    /// Gets the part count.
    /// </summary>
    /// <returns>The current part count.</returns>
    public int GetPartCount()
    {
        return partCount;
    }

    /// <summary>
    /// Gets the leaf part count.
    /// </summary>
    /// <returns>The current leaf part count.</returns>
    public int GetLeafPartCount()
    {
        return leafPartCount;
    }

    /// <summary>
    /// Gets the container part count.
    /// </summary>
    /// <returns>The current container part count.</returns>
    public int GetContainerPartCount()
    {
        return containerPartCount;
    }

    /// <summary>
    /// Gets the maximum depth.
    /// </summary>
    /// <returns>The current maximum depth.</returns>
    public int GetMaximumDepth()
    {
        return maximumDepth;
    }

    /// <summary>
    /// Gets the distinct media type count.
    /// </summary>
    /// <returns>The current distinct media type count.</returns>
    public int GetDistinctMediaTypeCount()
    {
        return mediaTypes.Count;
    }

    /// <summary>
    /// Gets the media type at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested media type.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMediaTypeStatistic GetMediaTypeAt(int index)
    {
        if (index < 0 || index >= mediaTypes.Count)
            throw new ArgumentOutOfRangeException("index");
        return mediaTypes[index];
    }

    /// <summary>
    /// Gets the media types.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current media types.</returns>
    public List<EmailMediaTypeStatistic> GetMediaTypes()
    {
        return new List<EmailMediaTypeStatistic>(mediaTypes);
    }

    /// <summary>
    /// Gets the media type entity count.
    /// </summary>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The current media type entity count.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public int GetMediaTypeEntityCount(string topLevelType, string subtype)
    {
        if (topLevelType == null)
            throw new ArgumentNullException("topLevelType");
        if (subtype == null)
            throw new ArgumentNullException("subtype");
        int index = 0;
        while (index < mediaTypes.Count)
        {
            EmailMediaTypeStatistic statistic = mediaTypes[index];
            if (string.Equals(statistic.GetTopLevelType(), topLevelType, StringComparison.OrdinalIgnoreCase) && string.Equals(statistic.GetSubtype(), subtype, StringComparison.OrdinalIgnoreCase))
            {
                return statistic.GetEntityCount();
            }

            index++;
        }

        return 0;
    }

    /// <summary>
    /// Gets the to recipient count.
    /// </summary>
    /// <returns>The current to recipient count.</returns>
    public int GetToRecipientCount()
    {
        return toRecipientCount;
    }

    /// <summary>
    /// Gets the cc recipient count.
    /// </summary>
    /// <returns>The current cc recipient count.</returns>
    public int GetCcRecipientCount()
    {
        return ccRecipientCount;
    }

    /// <summary>
    /// Gets the bcc recipient count.
    /// </summary>
    /// <returns>The current bcc recipient count.</returns>
    public int GetBccRecipientCount()
    {
        return bccRecipientCount;
    }

    /// <summary>
    /// Gets the header recipient count.
    /// </summary>
    /// <returns>The current header recipient count.</returns>
    public int GetHeaderRecipientCount()
    {
        return toRecipientCount + ccRecipientCount + bccRecipientCount;
    }

    /// <summary>
    /// Gets the resent recipient count.
    /// </summary>
    /// <returns>The current resent recipient count.</returns>
    public int GetResentRecipientCount()
    {
        return resentRecipientCount;
    }

    /// <summary>
    /// Gets the envelope recipient count.
    /// </summary>
    /// <returns>The current envelope recipient count.</returns>
    public int GetEnvelopeRecipientCount()
    {
        return envelopeRecipientCount;
    }

    /// <summary>
    /// Determines whether the date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the date is available; otherwise, <c>false</c>.</returns>
    public bool HasDate()
    {
        return messageDate != null;
    }

    /// <summary>
    /// Gets the date.
    /// </summary>
    /// <returns>The current date.</returns>
    public EmailDateTime? GetDate()
    {
        return messageDate;
    }

    /// <summary>
    /// Gets the resent date count.
    /// </summary>
    /// <returns>The current resent date count.</returns>
    public int GetResentDateCount()
    {
        return resentDates.Count;
    }

    /// <summary>
    /// Gets the resent date at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested resent date.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDateTime GetResentDateAt(int index)
    {
        if (index < 0 || index >= resentDates.Count)
            throw new ArgumentOutOfRangeException("index");
        return resentDates[index];
    }

    /// <summary>
    /// Gets the resent dates.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current resent dates.</returns>
    public List<EmailDateTime> GetResentDates()
    {
        return new List<EmailDateTime>(resentDates);
    }

    /// <summary>
    /// Gets the received date count.
    /// </summary>
    /// <returns>The current received date count.</returns>
    public int GetReceivedDateCount()
    {
        return receivedDates.Count;
    }

    /// <summary>
    /// Gets the received date at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested received date.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDateTime GetReceivedDateAt(int index)
    {
        if (index < 0 || index >= receivedDates.Count)
            throw new ArgumentOutOfRangeException("index");
        return receivedDates[index];
    }

    /// <summary>
    /// Gets the received dates.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current received dates.</returns>
    public List<EmailDateTime> GetReceivedDates()
    {
        return new List<EmailDateTime>(receivedDates);
    }

    /// <summary>
    /// Gets the attachment count.
    /// </summary>
    /// <returns>The current attachment count.</returns>
    public int GetAttachmentCount()
    {
        return attachmentCount;
    }

    /// <summary>
    /// Gets the inline resource count.
    /// </summary>
    /// <returns>The current inline resource count.</returns>
    public int GetInlineResourceCount()
    {
        return inlineResourceCount;
    }

    /// <summary>
    /// Gets the nested message count.
    /// </summary>
    /// <returns>The current nested message count.</returns>
    public int GetNestedMessageCount()
    {
        return nestedMessageCount;
    }

    /// <summary>
    /// Gets the attachment raw byte count.
    /// </summary>
    /// <returns>The current attachment raw byte count.</returns>
    public long GetAttachmentRawByteCount()
    {
        return attachmentRawByteCount;
    }

    /// <summary>
    /// Determines whether the current value or state is attachment raw byte count complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is attachment raw byte count complete; otherwise, <c>false</c>.</returns>
    public bool IsAttachmentRawByteCountComplete()
    {
        return attachmentRawByteCountComplete;
    }

    /// <summary>
    /// Gets the attachment decoded byte count.
    /// </summary>
    /// <returns>The current attachment decoded byte count.</returns>
    public long GetAttachmentDecodedByteCount()
    {
        return attachmentDecodedByteCount;
    }

    /// <summary>
    /// Determines whether the current value or state is attachment decoded byte count complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is attachment decoded byte count complete; otherwise, <c>false</c>.</returns>
    public bool IsAttachmentDecodedByteCountComplete()
    {
        return attachmentDecodedByteCountComplete;
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
    /// Gets the issue count.
    /// </summary>
    /// <returns>The current issue count.</returns>
    public int GetIssueCount()
    {
        return issues.GetCount();
    }

    /// <summary>
    /// Gets the information issue count.
    /// </summary>
    /// <returns>The current information issue count.</returns>
    public int GetInformationIssueCount()
    {
        return issues.GetCountBySeverity(EmailIssueSeverity.Information);
    }

    /// <summary>
    /// Gets the warning issue count.
    /// </summary>
    /// <returns>The current warning issue count.</returns>
    public int GetWarningIssueCount()
    {
        return issues.GetCountBySeverity(EmailIssueSeverity.Warning);
    }

    /// <summary>
    /// Gets the error issue count.
    /// </summary>
    /// <returns>The current error issue count.</returns>
    public int GetErrorIssueCount()
    {
        return issues.GetCountBySeverity(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Gets the fatal issue count.
    /// </summary>
    /// <returns>The current fatal issue count.</returns>
    public int GetFatalIssueCount()
    {
        return issues.GetCountBySeverity(EmailIssueSeverity.Fatal);
    }

    /// <summary>
    /// Gets the security summary.
    /// </summary>
    /// <returns>The current security summary.</returns>
    public EmailSecuritySummary GetSecuritySummary()
    {
        return securitySummary;
    }
}
