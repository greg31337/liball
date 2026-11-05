using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Describes the outcome of a openPGP armor operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailOpenPgpArmorResult : IEmailOpenPgpArmorResultContract
{
    EmailOpenPgpArmorKind kind;
    byte[] data;
    List<EmailOpenPgpArmorHeader> headers;
    bool checksumPresent;
    bool checksumValid;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new openPGP armor result.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "checksumPresent">Whether to enable the checksum present behavior.</param>
    /// <param name = "checksumValid">Whether to enable the checksum valid behavior.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpArmorResult(EmailOpenPgpArmorKind kind, byte[] data, List<EmailOpenPgpArmorHeader> headers, bool checksumPresent, bool checksumValid, EmailIssueList issues)
    {
        if (data == null || headers == null || issues == null)
            throw new ArgumentNullException("data");
        this.kind = kind;
        this.data = CopyImplementation(data);
        this.headers = new List<EmailOpenPgpArmorHeader>(headers);
        this.checksumPresent = checksumPresent;
        this.checksumValid = checksumValid;
        this.issues = CopyIssuesImplementation(issues);
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailOpenPgpArmorKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current data.</returns>
    public byte[] GetData()
    {
        return CopyImplementation(data);
    }

    /// <summary>
    /// Gets the headers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current headers.</returns>
    public List<EmailOpenPgpArmorHeader> GetHeaders()
    {
        return new List<EmailOpenPgpArmorHeader>(headers);
    }

    /// <summary>
    /// Determines whether the checksum is present or available.
    /// </summary>
    /// <returns><c>true</c> when the checksum is available; otherwise, <c>false</c>.</returns>
    public bool HasChecksum()
    {
        return checksumPresent;
    }

    /// <summary>
    /// Determines whether the current value or state is checksum valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is checksum valid; otherwise, <c>false</c>.</returns>
    public bool IsChecksumValid()
    {
        return checksumPresent && checksumValid;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        return CopyIssuesImplementation(issues);
    }

    internal static byte[] CopyImplementation(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }

    internal static EmailIssueList CopyIssuesImplementation(EmailIssueList value)
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(value);
        return copy;
    }
}
