using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a DKIM signature verification operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailDkimSignatureVerificationResult : IEmailDkimSignatureVerificationResultContract
{
    EmailHeader header;
    EmailDkimVerificationStatus status;
    EmailDkimVerificationPolicyStatus policyStatus;
    string reason;
    EmailDkimSignatureParseResult parseResult;
    EmailDkimVerificationKey? verificationKey;
    EmailDkimBodyCanonicalizationResult? bodyCanonicalization;
    EmailDkimHeaderCanonicalizationResult? headerCanonicalization;
    bool bodyHashValid;
    bool signatureValid;
    bool testingKey;
    bool strictIdentity;
    bool unsignedBodyContent;
    /// <summary>
    /// Creates a new DKIM signature verification result.
    /// </summary>
    /// <param name = "header">The header used by the operation.</param>
    /// <param name = "status">The status used by the operation.</param>
    /// <param name = "policyStatus">The policy status used by the operation.</param>
    /// <param name = "reason">The reason used by the operation.</param>
    /// <param name = "parseResult">The parse result used by the operation.</param>
    /// <param name = "verificationKey">The verification key used by the operation.</param>
    /// <param name = "bodyCanonicalization">The body canonicalization used by the operation.</param>
    /// <param name = "headerCanonicalization">The header canonicalization used by the operation.</param>
    /// <param name = "bodyHashValid">Whether to enable the body hash valid behavior.</param>
    /// <param name = "signatureValid">Whether to enable the signature valid behavior.</param>
    /// <param name = "testingKey">Whether to enable the testing key behavior.</param>
    /// <param name = "strictIdentity">Whether to enable the strict identity behavior.</param>
    /// <param name = "unsignedBodyContent">Whether to enable the unsigned body content behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDkimSignatureVerificationResult(EmailHeader header, EmailDkimVerificationStatus status, EmailDkimVerificationPolicyStatus policyStatus, string reason, EmailDkimSignatureParseResult parseResult, EmailDkimVerificationKey? verificationKey, EmailDkimBodyCanonicalizationResult? bodyCanonicalization, EmailDkimHeaderCanonicalizationResult? headerCanonicalization, bool bodyHashValid, bool signatureValid, bool testingKey, bool strictIdentity, bool unsignedBodyContent)
    {
        if (header == null || reason == null || parseResult == null)
            throw new ArgumentNullException("header");
        if (status < EmailDkimVerificationStatus.Pass || status > EmailDkimVerificationStatus.PolicyRejected)
            throw new ArgumentOutOfRangeException("status");
        if (policyStatus < EmailDkimVerificationPolicyStatus.NotEvaluated || policyStatus > EmailDkimVerificationPolicyStatus.Rejected)
            throw new ArgumentOutOfRangeException("policyStatus");
        this.header = header.Clone();
        this.status = status;
        this.policyStatus = policyStatus;
        this.reason = reason;
        this.parseResult = parseResult;
        this.verificationKey = verificationKey;
        this.bodyCanonicalization = bodyCanonicalization;
        this.headerCanonicalization = headerCanonicalization;
        this.bodyHashValid = bodyHashValid;
        this.signatureValid = signatureValid;
        this.testingKey = testingKey;
        this.strictIdentity = strictIdentity;
        this.unsignedBodyContent = unsignedBodyContent;
    }

    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <returns>The current header.</returns>
    public EmailHeader GetHeader()
    {
        return header.Clone();
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailDkimVerificationStatus GetStatus()
    {
        return status;
    }

    /// <summary>
    /// Gets the policy status.
    /// </summary>
    /// <returns>The current policy status.</returns>
    public EmailDkimVerificationPolicyStatus GetPolicyStatus()
    {
        return policyStatus;
    }

    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    public string GetReason()
    {
        return reason;
    }

    /// <summary>
    /// Gets the parse result.
    /// </summary>
    /// <returns>The current parse result.</returns>
    public EmailDkimSignatureParseResult GetParseResult()
    {
        return parseResult;
    }

    /// <summary>
    /// Determines whether the signature is present or available.
    /// </summary>
    /// <returns><c>true</c> when the signature is available; otherwise, <c>false</c>.</returns>
    public bool HasSignature()
    {
        return parseResult.HasValue();
    }

    /// <summary>
    /// Gets the signature.
    /// </summary>
    /// <returns>The current signature.</returns>
    public EmailDkimSignature? GetSignature()
    {
        return parseResult.GetValue();
    }

    /// <summary>
    /// Determines whether the verification key is present or available.
    /// </summary>
    /// <returns><c>true</c> when the verification key is available; otherwise, <c>false</c>.</returns>
    public bool HasVerificationKey()
    {
        return verificationKey != null;
    }

    /// <summary>
    /// Gets the verification key.
    /// </summary>
    /// <returns>The current verification key.</returns>
    public EmailDkimVerificationKey? GetVerificationKey()
    {
        return verificationKey;
    }

    /// <summary>
    /// Determines whether the body canonicalization is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body canonicalization is available; otherwise, <c>false</c>.</returns>
    public bool HasBodyCanonicalization()
    {
        return bodyCanonicalization != null;
    }

    /// <summary>
    /// Gets the body canonicalization.
    /// </summary>
    /// <returns>The current body canonicalization.</returns>
    public EmailDkimBodyCanonicalizationResult? GetBodyCanonicalization()
    {
        return bodyCanonicalization;
    }

    /// <summary>
    /// Determines whether the header canonicalization is present or available.
    /// </summary>
    /// <returns><c>true</c> when the header canonicalization is available; otherwise, <c>false</c>.</returns>
    public bool HasHeaderCanonicalization()
    {
        return headerCanonicalization != null;
    }

    /// <summary>
    /// Gets the header canonicalization.
    /// </summary>
    /// <returns>The current header canonicalization.</returns>
    public EmailDkimHeaderCanonicalizationResult? GetHeaderCanonicalization()
    {
        return headerCanonicalization;
    }

    /// <summary>
    /// Determines whether the current value or state is body hash valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is body hash valid; otherwise, <c>false</c>.</returns>
    public bool IsBodyHashValid()
    {
        return bodyHashValid;
    }

    /// <summary>
    /// Determines whether the current value or state is signature valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signature valid; otherwise, <c>false</c>.</returns>
    public bool IsSignatureValid()
    {
        return signatureValid;
    }

    /// <summary>
    /// Determines whether the current value or state is testing key.
    /// </summary>
    /// <returns><c>true</c> when the value or state is testing key; otherwise, <c>false</c>.</returns>
    public bool IsTestingKey()
    {
        return testingKey;
    }

    /// <summary>
    /// Determines whether the current value or state is legacy algorithm.
    /// </summary>
    /// <returns><c>true</c> when the value or state is legacy algorithm; otherwise, <c>false</c>.</returns>
    public bool IsLegacyAlgorithm()
    {
        return HasSignature() && (GetSignature() ?? throw new InvalidOperationException()).GetAlgorithmKind() == EmailDkimAlgorithmKind.RsaSha1;
    }

    /// <summary>
    /// Determines whether the current policy requires strict identity.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool RequiresStrictIdentity()
    {
        return strictIdentity;
    }

    /// <summary>
    /// Determines whether the unsigned body content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the unsigned body content is available; otherwise, <c>false</c>.</returns>
    public bool HasUnsignedBodyContent()
    {
        return unsignedBodyContent;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return status == EmailDkimVerificationStatus.Pass;
    }

    /// <summary>
    /// Gets the authentication result kind.
    /// </summary>
    /// <returns>The current authentication result kind.</returns>
    public EmailAuthenticationResultKind GetAuthenticationResultKind()
    {
        if (status == EmailDkimVerificationStatus.Pass)
            return EmailAuthenticationResultKind.Pass;
        if (status == EmailDkimVerificationStatus.PolicyRejected)
            return EmailAuthenticationResultKind.Policy;
        if (status == EmailDkimVerificationStatus.BodyHashMismatch || status == EmailDkimVerificationStatus.SignatureMismatch || status == EmailDkimVerificationStatus.KeyRevoked)
            return EmailAuthenticationResultKind.Fail;
        return EmailAuthenticationResultKind.PermError;
    }
}
