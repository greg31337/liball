using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Describes the outcome of a protected header extraction operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailProtectedHeaderExtractionResult : IEmailProtectedHeaderExtractionResultContract
{
    bool recognized;
    EmailProtectedHeaderIntent intent;
    EmailProtectedHeaderProtocol protocol;
    EmailHeaders protectedHeaders;
    EmailHeaders recordedOuterHeaders;
    EmailHeaders displayHeaders;
    List<EmailProtectedHeaderEntry> entries;
    List<EmailProtectedHeaderMismatch> mismatches;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new protected header extraction result.
    /// </summary>
    /// <param name = "recognized">Whether to enable the recognized behavior.</param>
    /// <param name = "intent">The intent used by the operation.</param>
    /// <param name = "protocol">The protocol used by the operation.</param>
    /// <param name = "protectedHeaders">The protected headers used by the operation.</param>
    /// <param name = "recordedOuterHeaders">The recorded outer headers used by the operation.</param>
    /// <param name = "displayHeaders">The display headers used by the operation.</param>
    /// <param name = "entries">The entries used by the operation.</param>
    /// <param name = "mismatches">The mismatches used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    public EmailProtectedHeaderExtractionResult(bool recognized, EmailProtectedHeaderIntent intent, EmailProtectedHeaderProtocol protocol, EmailHeaders protectedHeaders, EmailHeaders recordedOuterHeaders, EmailHeaders displayHeaders, List<EmailProtectedHeaderEntry> entries, List<EmailProtectedHeaderMismatch> mismatches, EmailIssueList issues)
    {
        this.recognized = recognized;
        this.intent = intent;
        this.protocol = protocol;
        this.protectedHeaders = protectedHeaders.Clone();
        this.recordedOuterHeaders = recordedOuterHeaders.Clone();
        this.displayHeaders = displayHeaders.Clone();
        this.entries = new List<EmailProtectedHeaderEntry>(entries);
        this.mismatches = new List<EmailProtectedHeaderMismatch>(mismatches);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Determines whether the current value or state is recognized.
    /// </summary>
    /// <returns><c>true</c> when the value or state is recognized; otherwise, <c>false</c>.</returns>
    public bool IsRecognized()
    {
        return recognized;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return recognized && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Gets the intent.
    /// </summary>
    /// <returns>The current intent.</returns>
    public EmailProtectedHeaderIntent GetIntent()
    {
        return intent;
    }

    /// <summary>
    /// Gets the protocol.
    /// </summary>
    /// <returns>The current protocol.</returns>
    public EmailProtectedHeaderProtocol GetProtocol()
    {
        return protocol;
    }

    /// <summary>
    /// Gets the protected headers.
    /// </summary>
    /// <returns>The current protected headers.</returns>
    public EmailHeaders GetProtectedHeaders()
    {
        return protectedHeaders.Clone();
    }

    /// <summary>
    /// Gets the recorded outer headers.
    /// </summary>
    /// <returns>The current recorded outer headers.</returns>
    public EmailHeaders GetRecordedOuterHeaders()
    {
        return recordedOuterHeaders.Clone();
    }

    /// <summary>
    /// Gets the display headers.
    /// </summary>
    /// <returns>The current display headers.</returns>
    public EmailHeaders GetDisplayHeaders()
    {
        return displayHeaders.Clone();
    }

    /// <summary>
    /// Gets the reply headers.
    /// </summary>
    /// <returns>The current reply headers.</returns>
    public EmailHeaders GetReplyHeaders()
    {
        return protectedHeaders.Clone();
    }

    /// <summary>
    /// Gets the entry count.
    /// </summary>
    /// <returns>The current entry count.</returns>
    public int GetEntryCount()
    {
        return entries.Count;
    }

    /// <summary>
    /// Gets the entry at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested entry.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailProtectedHeaderEntry GetEntryAt(int index)
    {
        if (index < 0 || index >= entries.Count)
            throw new ArgumentOutOfRangeException("index");
        EmailProtectedHeaderEntry entry = entries[index];
        return new EmailProtectedHeaderEntry(entry.GetHeader(), entry.GetState());
    }

    /// <summary>
    /// Gets the entries.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current entries.</returns>
    public List<EmailProtectedHeaderEntry> GetEntries()
    {
        List<EmailProtectedHeaderEntry> copy = new List<EmailProtectedHeaderEntry>();
        int index = 0;
        while (index < entries.Count)
        {
            copy.Add(GetEntryAt(index));
            index++;
        }

        return copy;
    }

    /// <summary>
    /// Gets the mismatch count.
    /// </summary>
    /// <returns>The current mismatch count.</returns>
    public int GetMismatchCount()
    {
        return mismatches.Count;
    }

    /// <summary>
    /// Gets the mismatch at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested mismatch.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailProtectedHeaderMismatch GetMismatchAt(int index)
    {
        if (index < 0 || index >= mismatches.Count)
            throw new ArgumentOutOfRangeException("index");
        return mismatches[index];
    }

    /// <summary>
    /// Gets the mismatches.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current mismatches.</returns>
    public List<EmailProtectedHeaderMismatch> GetMismatches()
    {
        return new List<EmailProtectedHeaderMismatch>(mismatches);
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
}
