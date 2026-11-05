using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailEnvelope"/>.
/// </summary>
public class EmailEnvelopeOperations : IEmailEnvelopeOperationsContract
{
    /// <summary>Creates an empty envelope.</summary>
    /// <returns>The configured envelope.</returns>
    public EmailEnvelope CreateWith()
    {
        return EmailEnvelope.CreateImplementationWith();
    }

    /// <summary>
    /// Creates a new envelope.
    /// </summary>
    /// <param name = "sender">The sender used by the operation.</param>
    /// <param name = "recipients">The recipients used by the operation.</param>
    /// <param name = "mboxFromLine">The mbox from line used by the operation.</param>
    /// <param name = "mboxFromLineRange">The mbox from line range used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured envelope.</returns>
    public EmailEnvelope CreateWithSenderAndRecipientsAndMboxFromLineAndMboxFromLineRange(EmailAddress? sender, List<EmailAddress> recipients, byte[]? mboxFromLine, EmailRange? mboxFromLineRange)
    {
        return EmailEnvelope.CreateImplementationWithSenderAndRecipientsAndMboxFromLineAndMboxFromLineRange(sender, recipients, mboxFromLine, mboxFromLineRange);
    }

    /// <summary>
    /// Creates a recipient projection configured for the requested behavior.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>The envelope produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailEnvelope CreateRecipientProjection(EmailHeaders headers)
    {
        return EmailEnvelope.CreateRecipientProjectionImplementation(headers);
    }
}
