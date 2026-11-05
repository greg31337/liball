using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailOpenPgpSigningResult"/>.
/// </summary>
public class EmailOpenPgpSigningResultOperations : IEmailOpenPgpSigningResultOperationsContract
{
    /// <summary>
    /// Copies the issues to a separate destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The issue list produced by the operation.</returns>
    public EmailIssueList CopyIssues(EmailIssueList value)
    {
        return EmailOpenPgpSigningResult.CopyIssuesImplementation(value);
    }
}
