using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpSigningResult"/>.
/// </summary>
public interface IEmailOpenPgpSigningResultOperationsContract
{
    /// <summary>
    /// Copies the issues to a separate destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The issue list produced by the operation.</returns>
    EmailIssueList CopyIssues(EmailIssueList value);
}
