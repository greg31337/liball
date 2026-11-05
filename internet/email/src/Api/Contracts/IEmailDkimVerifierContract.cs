using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimVerifier"/>.
/// </summary>
public interface IEmailDkimVerifierContract
{
    /// <summary>
    /// Verifies the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <returns>The DKIM verification result produced by the operation.</returns>
    EmailDkimVerificationResult VerifyWithHeadersAndBodyAndKeys(EmailHeaders headers, ReadOnlySpan<byte> body, List<EmailDkimVerificationKey> keys);
    /// <summary>
    /// Verifies the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailDkimVerificationResult VerifyWithHeadersAndBodyAndKeysAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, List<EmailDkimVerificationKey> keys, EmailDkimVerificationOptions options);
    /// <summary>
    /// Verifies the supplied value using keys resolved on demand.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "keyResolver">The caller-supplied key resolver used by the operation.</param>
    /// <returns>The DKIM verification result produced by the operation.</returns>
    EmailDkimVerificationResult VerifyWithHeadersAndBodyAndKeyResolver(EmailHeaders headers, ReadOnlySpan<byte> body, IEmailDkimKeyResolver keyResolver);
    /// <summary>
    /// Verifies the supplied value using keys resolved on demand.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "keyResolver">The caller-supplied key resolver used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailDkimVerificationResult VerifyWithHeadersAndBodyAndKeyResolverAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, IEmailDkimKeyResolver keyResolver, EmailDkimVerificationOptions options);
}
