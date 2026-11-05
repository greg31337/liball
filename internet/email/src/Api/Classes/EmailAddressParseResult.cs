using System;

namespace email;
/// <summary>
/// Describes the outcome of a address parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailAddressParseResult : IEmailAddressParseResultContract
{
    EmailAddressList addressList;
    EmailIssueList issues;
    int consumedByteCount;
    int sourceByteCount;
    /// <summary>
    /// Creates a new address parse result.
    /// </summary>
    /// <param name = "addressList">The address list used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "consumedByteCount">The number of items or bytes to process.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAddressParseResult(EmailAddressList addressList, EmailIssueList issues, int consumedByteCount, int sourceByteCount)
    {
        if (addressList == null)
        {
            throw new ArgumentNullException("addressList");
        }

        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        if (consumedByteCount < 0 || sourceByteCount < 0 || consumedByteCount > sourceByteCount)
        {
            throw new ArgumentOutOfRangeException("consumedByteCount");
        }

        this.addressList = addressList;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.consumedByteCount = consumedByteCount;
        this.sourceByteCount = sourceByteCount;
    }

    /// <summary>
    /// Gets the address list.
    /// </summary>
    /// <returns>The current address list.</returns>
    public EmailAddressList GetAddressList()
    {
        return addressList;
    }

    /// <summary>
    /// Determines whether the mailbox is present or available.
    /// </summary>
    /// <returns><c>true</c> when the mailbox is available; otherwise, <c>false</c>.</returns>
    public bool HasMailbox()
    {
        return addressList.GetCount() == 1 && addressList.GetAt(0).IsMailbox();
    }

    /// <summary>
    /// Gets the mailbox.
    /// </summary>
    /// <returns>The current mailbox.</returns>
    public EmailAddress? GetMailbox()
    {
        if (!HasMailbox())
        {
            return null;
        }

        return addressList.GetAt(0).GetMailbox();
    }

    /// <summary>
    /// Determines whether the group is present or available.
    /// </summary>
    /// <returns><c>true</c> when the group is available; otherwise, <c>false</c>.</returns>
    public bool HasGroup()
    {
        return addressList.GetCount() == 1 && addressList.GetAt(0).IsGroup();
    }

    /// <summary>
    /// Gets the group.
    /// </summary>
    /// <returns>The current group.</returns>
    public EmailAddressGroup? GetGroup()
    {
        if (!HasGroup())
        {
            return null;
        }

        return addressList.GetAt(0).GetGroup();
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
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    public int GetConsumedByteCount()
    {
        return consumedByteCount;
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public int GetSourceByteCount()
    {
        return sourceByteCount;
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return consumedByteCount == sourceByteCount;
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
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    public bool HasFatalError()
    {
        return issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal);
    }
}
