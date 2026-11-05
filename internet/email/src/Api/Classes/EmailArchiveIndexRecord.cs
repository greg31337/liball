using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents an archive index record.
/// </summary>
public class EmailArchiveIndexRecord : IEmailArchiveIndexRecordContract
{
    EmailArchiveSourceKind sourceKind;
    string? sourcePath;
    bool indexPresent;
    int index;
    EmailRange? archiveRange;
    EmailRange? messageRange;
    long messageByteCount;
    EmailArchiveParseStatus parseStatus;
    List<EmailArchiveIndexHeader> selectedHeaders;
    EmailHashResult? exactHash;
    EmailHashResult? semanticHash;
    EmailMessageSummary? summary;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new archive index record.
    /// </summary>
    /// <param name = "sourceKind">The source kind used by the operation.</param>
    /// <param name = "sourcePath">The file-system or message path to use.</param>
    /// <param name = "indexPresent">Whether to enable the index present behavior.</param>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "archiveRange">The archive range used by the operation.</param>
    /// <param name = "messageRange">The message range used by the operation.</param>
    /// <param name = "messageByteCount">The number of items or bytes to process.</param>
    /// <param name = "parseStatus">The parse status used by the operation.</param>
    /// <param name = "selectedHeaders">The selected headers used by the operation.</param>
    /// <param name = "exactHash">The exact hash used by the operation.</param>
    /// <param name = "semanticHash">The semantic hash used by the operation.</param>
    /// <param name = "summary">The summary used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArchiveIndexRecord(EmailArchiveSourceKind sourceKind, string? sourcePath, bool indexPresent, int index, EmailRange? archiveRange, EmailRange? messageRange, long messageByteCount, EmailArchiveParseStatus parseStatus, List<EmailArchiveIndexHeader> selectedHeaders, EmailHashResult? exactHash, EmailHashResult? semanticHash, EmailMessageSummary? summary, EmailIssueList issues)
    {
        if (sourceKind < EmailArchiveSourceKind.StandaloneMessage || sourceKind > EmailArchiveSourceKind.Maildir)
            throw new ArgumentOutOfRangeException("sourceKind");
        if (indexPresent && index < 0)
            throw new ArgumentOutOfRangeException("index");
        if (messageByteCount < 0)
            throw new ArgumentOutOfRangeException("messageByteCount");
        if (parseStatus < EmailArchiveParseStatus.Success || parseStatus > EmailArchiveParseStatus.Failed)
            throw new ArgumentOutOfRangeException("parseStatus");
        if (selectedHeaders == null || issues == null)
            throw new ArgumentNullException("selectedHeaders");
        this.sourceKind = sourceKind;
        this.sourcePath = sourcePath;
        this.indexPresent = indexPresent;
        this.index = index;
        this.archiveRange = archiveRange;
        this.messageRange = messageRange;
        this.messageByteCount = messageByteCount;
        this.parseStatus = parseStatus;
        this.selectedHeaders = CopyHeaders(selectedHeaders);
        this.exactHash = exactHash;
        this.semanticHash = semanticHash;
        this.summary = summary;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Gets the source kind.
    /// </summary>
    /// <returns>The current source kind.</returns>
    public EmailArchiveSourceKind GetSourceKind()
    {
        return sourceKind;
    }

    /// <summary>
    /// Determines whether the source path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source path is available; otherwise, <c>false</c>.</returns>
    public bool HasSourcePath()
    {
        return sourcePath != null;
    }

    /// <summary>
    /// Gets the source path.
    /// </summary>
    /// <returns>The current source path.</returns>
    public string? GetSourcePath()
    {
        return sourcePath;
    }

    /// <summary>
    /// Determines whether the index is present or available.
    /// </summary>
    /// <returns><c>true</c> when the index is available; otherwise, <c>false</c>.</returns>
    public bool HasIndex()
    {
        return indexPresent;
    }

    /// <summary>
    /// Gets the index.
    /// </summary>
    /// <returns>The current index.</returns>
    public int GetIndex()
    {
        return index;
    }

    /// <summary>
    /// Determines whether the archive range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the archive range is available; otherwise, <c>false</c>.</returns>
    public bool HasArchiveRange()
    {
        return archiveRange != null;
    }

    /// <summary>
    /// Gets the archive range.
    /// </summary>
    /// <returns>The current archive range.</returns>
    public EmailRange? GetArchiveRange()
    {
        return archiveRange;
    }

    /// <summary>
    /// Determines whether the message range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message range is available; otherwise, <c>false</c>.</returns>
    public bool HasMessageRange()
    {
        return messageRange != null;
    }

    /// <summary>
    /// Gets the message range.
    /// </summary>
    /// <returns>The current message range.</returns>
    public EmailRange? GetMessageRange()
    {
        return messageRange;
    }

    /// <summary>
    /// Gets the message byte count.
    /// </summary>
    /// <returns>The current message byte count.</returns>
    public long GetMessageByteCount()
    {
        return messageByteCount;
    }

    /// <summary>
    /// Gets the parse status.
    /// </summary>
    /// <returns>The current parse status.</returns>
    public EmailArchiveParseStatus GetParseStatus()
    {
        return parseStatus;
    }

    /// <summary>
    /// Gets the selected header count.
    /// </summary>
    /// <returns>The current selected header count.</returns>
    public int GetSelectedHeaderCount()
    {
        return selectedHeaders.Count;
    }

    /// <summary>
    /// Gets the selected header at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested selected header.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArchiveIndexHeader GetSelectedHeaderAt(int index)
    {
        if (index < 0 || index >= selectedHeaders.Count)
            throw new ArgumentOutOfRangeException("index");
        return selectedHeaders[index];
    }

    /// <summary>
    /// Gets the selected headers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current selected headers.</returns>
    public List<EmailArchiveIndexHeader> GetSelectedHeaders()
    {
        return new List<EmailArchiveIndexHeader>(selectedHeaders);
    }

    /// <summary>
    /// Gets the first selected header value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current first selected header value.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string? GetFirstSelectedHeaderValue(string name)
    {
        if (name == null)
            throw new ArgumentNullException("name");
        int index = 0;
        while (index < selectedHeaders.Count)
        {
            if (string.Equals(selectedHeaders[index].GetName(), name, StringComparison.OrdinalIgnoreCase))
                return selectedHeaders[index].GetValue();
            index++;
        }

        return null;
    }

    /// <summary>
    /// Determines whether the exact hash is present or available.
    /// </summary>
    /// <returns><c>true</c> when the exact hash is available; otherwise, <c>false</c>.</returns>
    public bool HasExactHash()
    {
        return exactHash != null;
    }

    /// <summary>
    /// Gets the exact hash.
    /// </summary>
    /// <returns>The current exact hash.</returns>
    public EmailHashResult? GetExactHash()
    {
        return exactHash;
    }

    /// <summary>
    /// Determines whether the semantic hash is present or available.
    /// </summary>
    /// <returns><c>true</c> when the semantic hash is available; otherwise, <c>false</c>.</returns>
    public bool HasSemanticHash()
    {
        return semanticHash != null;
    }

    /// <summary>
    /// Gets the semantic hash.
    /// </summary>
    /// <returns>The current semantic hash.</returns>
    public EmailHashResult? GetSemanticHash()
    {
        return semanticHash;
    }

    /// <summary>
    /// Determines whether the summary is present or available.
    /// </summary>
    /// <returns><c>true</c> when the summary is available; otherwise, <c>false</c>.</returns>
    public bool HasSummary()
    {
        return summary != null;
    }

    /// <summary>
    /// Gets the summary.
    /// </summary>
    /// <returns>The current summary.</returns>
    public EmailMessageSummary? GetSummary()
    {
        return summary;
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
    /// Determines whether the current value or state is parse success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is parse success; otherwise, <c>false</c>.</returns>
    public bool IsParseSuccess()
    {
        return parseStatus == EmailArchiveParseStatus.Success;
    }

    static List<EmailArchiveIndexHeader> CopyHeaders(List<EmailArchiveIndexHeader> values)
    {
        List<EmailArchiveIndexHeader> copy = new List<EmailArchiveIndexHeader>();
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == null)
                throw new ArgumentException("Selected index headers cannot contain null entries.", "values");
            copy.Add(values[index]);
            index++;
        }

        return copy;
    }
}
