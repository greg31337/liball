using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailEnvelope"/>.
/// </summary>
public interface IEmailEnvelopeOperationsContract
{
    /// <summary>Creates an empty envelope.</summary>
    /// <returns>The configured envelope.</returns>
    EmailEnvelope CreateWith();
    /// <summary>
    /// Creates a new envelope.
    /// </summary>
    /// <param name = "sender">The sender used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <param name = "mboxFromLine">The mbox from line used by the operation.</param>
    /// <param name = "mboxFromLineRange">The mbox from line range used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured envelope.</returns>
    EmailEnvelope CreateWithSenderAndRecipientsAndMboxFromLineAndMboxFromLineRange(EmailAddress? sender, List<EmailAddress> recipients, byte[]? mboxFromLine, EmailRange? mboxFromLineRange);
    /// <summary>
    /// Creates a recipient projection configured for the requested behavior.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>The envelope produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailEnvelope CreateRecipientProjection(EmailHeaders headers);
}
