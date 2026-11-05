using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcSetVerificationResult"/>.
/// </summary>
public interface IEmailArcSetVerificationResultContract
{
    /// <summary>
    /// Gets the set.
    /// </summary>
    /// <returns>The current set.</returns>
    EmailArcSet GetSet();
    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    int GetInstance();
    /// <summary>
    /// Determines whether the message signature result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message signature result is available; otherwise, <c>false</c>.</returns>
    bool HasMessageSignatureResult();
    /// <summary>
    /// Gets the message signature result.
    /// </summary>
    /// <returns>The current message signature result.</returns>
    EmailArcSignatureVerificationResult GetMessageSignatureResult();
    /// <summary>
    /// Determines whether the seal result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the seal result is available; otherwise, <c>false</c>.</returns>
    bool HasSealResult();
    /// <summary>
    /// Gets the seal result.
    /// </summary>
    /// <returns>The current seal result.</returns>
    EmailArcSignatureVerificationResult GetSealResult();
    /// <summary>
    /// Determines whether the authentication results is present or available.
    /// </summary>
    /// <returns><c>true</c> when the authentication results is available; otherwise, <c>false</c>.</returns>
    bool HasAuthenticationResults();
    /// <summary>
    /// Gets the authentication results.
    /// </summary>
    /// <returns>The current authentication results.</returns>
    EmailAuthenticationResults? GetAuthenticationResults();
}
