using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcSealer"/>.
/// </summary>
public interface IEmailArcSealerContract
{
    /// <summary>
    /// Seals the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "authenticationResults">The authentication results used by the operation.</param>
    /// <param name = "chainStatus">The chain status used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The ARC sealing result produced by the operation.</returns>
    EmailArcSealingResult SealWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, EmailAuthenticationResults authenticationResults, EmailArcChainStatus chainStatus, RSA privateKey, EmailArcSealingOptions options);
    /// <summary>
    /// Seals the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "authenticationResults">The authentication results used by the operation.</param>
    /// <param name = "chainStatus">The chain status used by the operation.</param>
    /// <param name = "messageSignaturePrivateKey">The message signature private key used by the operation.</param>
    /// <param name = "sealPrivateKey">The seal private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The ARC sealing result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailArcSealingResult SealWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, EmailAuthenticationResults authenticationResults, EmailArcChainStatus chainStatus, RSA messageSignaturePrivateKey, RSA sealPrivateKey, EmailArcSealingOptions options);
    /// <summary>
    /// Seals the and insert.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "authenticationResults">The authentication results used by the operation.</param>
    /// <param name = "chainStatus">The chain status used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The ARC sealing result produced by the operation.</returns>
    EmailArcSealingResult SealAndInsertWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, EmailAuthenticationResults authenticationResults, EmailArcChainStatus chainStatus, RSA privateKey, EmailArcSealingOptions options);
    /// <summary>
    /// Seals the and insert.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "authenticationResults">The authentication results used by the operation.</param>
    /// <param name = "chainStatus">The chain status used by the operation.</param>
    /// <param name = "messageSignaturePrivateKey">The message signature private key used by the operation.</param>
    /// <param name = "sealPrivateKey">The seal private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The ARC sealing result produced by the operation.</returns>
    EmailArcSealingResult SealAndInsertWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, EmailAuthenticationResults authenticationResults, EmailArcChainStatus chainStatus, RSA messageSignaturePrivateKey, RSA sealPrivateKey, EmailArcSealingOptions options);
}
