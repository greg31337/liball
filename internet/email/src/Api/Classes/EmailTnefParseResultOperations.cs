using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailTnefParseResult"/>.
/// </summary>
public class EmailTnefParseResultOperations : IEmailTnefParseResultOperationsContract
{
    /// <summary>Creates a TNEF parse result without a decoded message model.</summary>
    /// <param name = "container">The parsed container, when available.</param>
    /// <param name = "issues">The parse diagnostics.</param>
    /// <param name = "complete">Whether parsing completed.</param>
    /// <returns>The configured parse result.</returns>
    public EmailTnefParseResult CreateWithContainerAndIssuesAndComplete(EmailTnefContainer? container, EmailIssueList issues, bool complete)
    {
        return EmailTnefParseResult.CreateImplementationWithContainerAndIssuesAndComplete(container, issues, complete);
    }

    /// <summary>
    /// Creates a new TNEF parse result.
    /// </summary>
    /// <param name = "container">The container used by the operation.</param>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "complete">Whether to enable the complete behavior.</param>
    /// <returns>The configured parse result.</returns>
    public EmailTnefParseResult CreateWithContainerAndMessageAndIssuesAndComplete(EmailTnefContainer container, EmailTnefMessage message, EmailIssueList issues, bool complete)
    {
        return EmailTnefParseResult.CreateImplementationWithContainerAndMessageAndIssuesAndComplete(container, message, issues, complete);
    }
}
