using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessage"/>.
/// </summary>
public interface IEmailMessageOperationsContract
{
    /// <summary>Creates an empty message.</summary>
    /// <returns>The configured message.</returns>
    EmailMessage CreateWith();
    /// <summary>
    /// Creates a new message.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>The configured message.</returns>
    EmailMessage CreateWithHeaders(EmailHeaders headers);
    /// <summary>
    /// Creates a message with source and envelope metadata.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "envelope">The envelope used by the operation.</param>
    /// <param name = "sourceInfo">The source info used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <returns>The configured message.</returns>
    EmailMessage CreateWithHeadersAndEnvelopeAndSourceInfoAndIssues(EmailHeaders headers, EmailEnvelope envelope, EmailSourceInfo sourceInfo, EmailIssueList issues);
    /// <summary>
    /// Creates a message with its complete model and source metadata.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "envelope">The envelope used by the operation.</param>
    /// <param name = "sourceInfo">The source info used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <returns>The configured message.</returns>
    EmailMessage CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(EmailHeaders? headers, MimeEntity? body, EmailEnvelope envelope, EmailSourceInfo? sourceInfo, EmailIssueList? issues);
}
