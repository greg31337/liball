using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailMessage"/>.
/// </summary>
public class EmailMessageOperations : IEmailMessageOperationsContract
{
    /// <summary>Creates an empty message.</summary>
    /// <returns>The configured message.</returns>
    public EmailMessage CreateWith()
    {
        return EmailMessage.CreateImplementationWith();
    }

    /// <summary>
    /// Creates a new message.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>The configured message.</returns>
    public EmailMessage CreateWithHeaders(EmailHeaders headers)
    {
        return EmailMessage.CreateImplementationWithHeaders(headers);
    }

    /// <summary>
    /// Creates a message with source and envelope metadata.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "envelope">The envelope used by the operation.</param>
    /// <param name = "sourceInfo">The source info used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <returns>The configured message.</returns>
    public EmailMessage CreateWithHeadersAndEnvelopeAndSourceInfoAndIssues(EmailHeaders headers, EmailEnvelope envelope, EmailSourceInfo sourceInfo, EmailIssueList issues)
    {
        return EmailMessage.CreateImplementationWithHeadersAndEnvelopeAndSourceInfoAndIssues(headers, envelope, sourceInfo, issues);
    }

    /// <summary>
    /// Creates a message with its complete model and source metadata.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "envelope">The envelope used by the operation.</param>
    /// <param name = "sourceInfo">The source info used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <returns>The configured message.</returns>
    public EmailMessage CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(EmailHeaders? headers, MimeEntity? body, EmailEnvelope envelope, EmailSourceInfo? sourceInfo, EmailIssueList? issues)
    {
        return EmailMessage.CreateImplementationWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(headers, body, envelope, sourceInfo, issues);
    }
}
