using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a ARC set verification operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailArcSetVerificationResult : IEmailArcSetVerificationResultContract
{
    EmailArcSet set;
    EmailArcSignatureVerificationResult messageSignature;
    EmailArcSignatureVerificationResult seal;
    /// <summary>
    /// Creates a new ARC set verification result.
    /// </summary>
    /// <param name = "set">The set used by the operation.</param>
    /// <param name = "messageSignature">The message signature used by the operation.</param>
    /// <param name = "seal">The seal used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailArcSetVerificationResult(EmailArcSet set, EmailArcSignatureVerificationResult messageSignature, EmailArcSignatureVerificationResult seal)
    {
        if (set == null)
            throw new ArgumentNullException("set");
        this.set = set;
        this.messageSignature = messageSignature;
        this.seal = seal;
    }

    /// <summary>
    /// Gets the set.
    /// </summary>
    /// <returns>The current set.</returns>
    public EmailArcSet GetSet()
    {
        return set;
    }

    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    public int GetInstance()
    {
        return set.GetInstance();
    }

    /// <summary>
    /// Determines whether the message signature result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message signature result is available; otherwise, <c>false</c>.</returns>
    public bool HasMessageSignatureResult()
    {
        return messageSignature != null;
    }

    /// <summary>
    /// Gets the message signature result.
    /// </summary>
    /// <returns>The current message signature result.</returns>
    public EmailArcSignatureVerificationResult GetMessageSignatureResult()
    {
        return messageSignature;
    }

    /// <summary>
    /// Determines whether the seal result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the seal result is available; otherwise, <c>false</c>.</returns>
    public bool HasSealResult()
    {
        return seal != null;
    }

    /// <summary>
    /// Gets the seal result.
    /// </summary>
    /// <returns>The current seal result.</returns>
    public EmailArcSignatureVerificationResult GetSealResult()
    {
        return seal;
    }

    /// <summary>
    /// Determines whether the authentication results is present or available.
    /// </summary>
    /// <returns><c>true</c> when the authentication results is available; otherwise, <c>false</c>.</returns>
    public bool HasAuthenticationResults()
    {
        return set.HasAuthenticationResultsLink();
    }

    /// <summary>
    /// Gets the authentication results.
    /// </summary>
    /// <returns>The current authentication results.</returns>
    public EmailAuthenticationResults? GetAuthenticationResults()
    {
        return set.GetAuthenticationResults();
    }
}
