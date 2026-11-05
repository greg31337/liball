using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailOpenPgpArmorResult"/>.
/// </summary>
public class EmailOpenPgpArmorResultOperations : IEmailOpenPgpArmorResultOperationsContract
{
    /// <summary>
    /// Copies the value to a separate destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] Copy(byte[] value)
    {
        return EmailOpenPgpArmorResult.CopyImplementation(value);
    }

    /// <summary>
    /// Copies the issues to a separate destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The issue list produced by the operation.</returns>
    public EmailIssueList CopyIssues(EmailIssueList value)
    {
        return EmailOpenPgpArmorResult.CopyIssuesImplementation(value);
    }
}
