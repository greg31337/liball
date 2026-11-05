using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimSignatureVerificationResult"/>.
/// </summary>
public interface IEmailDkimSignatureVerificationResultContract
{
    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <returns>The current header.</returns>
    EmailHeader GetHeader();
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    EmailDkimVerificationStatus GetStatus();
    /// <summary>
    /// Gets the policy status.
    /// </summary>
    /// <returns>The current policy status.</returns>
    EmailDkimVerificationPolicyStatus GetPolicyStatus();
    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    string GetReason();
    /// <summary>
    /// Gets the parse result.
    /// </summary>
    /// <returns>The current parse result.</returns>
    EmailDkimSignatureParseResult GetParseResult();
    /// <summary>
    /// Determines whether the signature is present or available.
    /// </summary>
    /// <returns><c>true</c> when the signature is available; otherwise, <c>false</c>.</returns>
    bool HasSignature();
    /// <summary>
    /// Gets the signature.
    /// </summary>
    /// <returns>The current signature.</returns>
    EmailDkimSignature? GetSignature();
    /// <summary>
    /// Determines whether the verification key is present or available.
    /// </summary>
    /// <returns><c>true</c> when the verification key is available; otherwise, <c>false</c>.</returns>
    bool HasVerificationKey();
    /// <summary>
    /// Gets the verification key.
    /// </summary>
    /// <returns>The current verification key.</returns>
    EmailDkimVerificationKey? GetVerificationKey();
    /// <summary>
    /// Determines whether the body canonicalization is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body canonicalization is available; otherwise, <c>false</c>.</returns>
    bool HasBodyCanonicalization();
    /// <summary>
    /// Gets the body canonicalization.
    /// </summary>
    /// <returns>The current body canonicalization.</returns>
    EmailDkimBodyCanonicalizationResult? GetBodyCanonicalization();
    /// <summary>
    /// Determines whether the header canonicalization is present or available.
    /// </summary>
    /// <returns><c>true</c> when the header canonicalization is available; otherwise, <c>false</c>.</returns>
    bool HasHeaderCanonicalization();
    /// <summary>
    /// Gets the header canonicalization.
    /// </summary>
    /// <returns>The current header canonicalization.</returns>
    EmailDkimHeaderCanonicalizationResult? GetHeaderCanonicalization();
    /// <summary>
    /// Determines whether the current value or state is body hash valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is body hash valid; otherwise, <c>false</c>.</returns>
    bool IsBodyHashValid();
    /// <summary>
    /// Determines whether the current value or state is signature valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signature valid; otherwise, <c>false</c>.</returns>
    bool IsSignatureValid();
    /// <summary>
    /// Determines whether the current value or state is testing key.
    /// </summary>
    /// <returns><c>true</c> when the value or state is testing key; otherwise, <c>false</c>.</returns>
    bool IsTestingKey();
    /// <summary>
    /// Determines whether the current value or state is legacy algorithm.
    /// </summary>
    /// <returns><c>true</c> when the value or state is legacy algorithm; otherwise, <c>false</c>.</returns>
    bool IsLegacyAlgorithm();
    /// <summary>
    /// Determines whether the current policy requires strict identity.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool RequiresStrictIdentity();
    /// <summary>
    /// Determines whether the unsigned body content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the unsigned body content is available; otherwise, <c>false</c>.</returns>
    bool HasUnsignedBodyContent();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Gets the authentication result kind.
    /// </summary>
    /// <returns>The current authentication result kind.</returns>
    EmailAuthenticationResultKind GetAuthenticationResultKind();
}
