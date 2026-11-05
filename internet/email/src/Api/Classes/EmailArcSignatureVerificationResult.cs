using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a ARC signature verification operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailArcSignatureVerificationResult : IEmailArcSignatureVerificationResultContract
{
    EmailArcHeaderField field;
    EmailArcSignatureVerificationStatus status;
    string reason;
    EmailDkimVerificationKey? verificationKey;
    EmailDkimBodyCanonicalizationResult? bodyCanonicalization;
    EmailDkimHeaderCanonicalizationResult? messageHeaderCanonicalization;
    EmailArcSealCanonicalizationResult? sealCanonicalization;
    bool bodyHashValid;
    bool signatureValid;
    /// <summary>
    /// Creates a new ARC signature verification result.
    /// </summary>
    /// <param name = "field">The field used by the operation.</param>
    /// <param name = "status">The status used by the operation.</param>
    /// <param name = "reason">The reason used by the operation.</param>
    /// <param name = "verificationKey">The verification key used by the operation.</param>
    /// <param name = "bodyCanonicalization">The body canonicalization used by the operation.</param>
    /// <param name = "messageHeaderCanonicalization">The message header canonicalization used by the operation.</param>
    /// <param name = "sealCanonicalization">The seal canonicalization used by the operation.</param>
    /// <param name = "bodyHashValid">Whether to enable the body hash valid behavior.</param>
    /// <param name = "signatureValid">Whether to enable the signature valid behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcSignatureVerificationResult(EmailArcHeaderField field, EmailArcSignatureVerificationStatus status, string reason, EmailDkimVerificationKey? verificationKey, EmailDkimBodyCanonicalizationResult? bodyCanonicalization, EmailDkimHeaderCanonicalizationResult? messageHeaderCanonicalization, EmailArcSealCanonicalizationResult? sealCanonicalization, bool bodyHashValid, bool signatureValid)
    {
        if (field == null || reason == null)
            throw new ArgumentNullException("field");
        if (field.GetKind() != EmailSignatureHeaderKind.ArcMessageSignature && field.GetKind() != EmailSignatureHeaderKind.ArcSeal)
            throw new ArgumentException("An ARC signature result requires an AMS or AS field.", "field");
        if (status < EmailArcSignatureVerificationStatus.NotEvaluated || status > EmailArcSignatureVerificationStatus.SignatureMismatch)
            throw new ArgumentOutOfRangeException("status");
        this.field = field;
        this.status = status;
        this.reason = reason;
        this.verificationKey = verificationKey;
        this.bodyCanonicalization = bodyCanonicalization;
        this.messageHeaderCanonicalization = messageHeaderCanonicalization;
        this.sealCanonicalization = sealCanonicalization;
        this.bodyHashValid = bodyHashValid;
        this.signatureValid = signatureValid;
    }

    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    public int GetInstance()
    {
        return field.GetInstance();
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailSignatureHeaderKind GetKind()
    {
        return field.GetKind();
    }

    /// <summary>
    /// Gets the field.
    /// </summary>
    /// <returns>The current field.</returns>
    public EmailArcHeaderField GetField()
    {
        return field;
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailArcSignatureVerificationStatus GetStatus()
    {
        return status;
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
    /// Determines whether the operation or source value was evaluated.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasEvaluated()
    {
        return status != EmailArcSignatureVerificationStatus.NotEvaluated;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return status == EmailArcSignatureVerificationStatus.Pass;
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
    /// Determines whether the message header canonicalization is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message header canonicalization is available; otherwise, <c>false</c>.</returns>
    public bool HasMessageHeaderCanonicalization()
    {
        return messageHeaderCanonicalization != null;
    }

    /// <summary>
    /// Gets the message header canonicalization.
    /// </summary>
    /// <returns>The current message header canonicalization.</returns>
    public EmailDkimHeaderCanonicalizationResult? GetMessageHeaderCanonicalization()
    {
        return messageHeaderCanonicalization;
    }

    /// <summary>
    /// Determines whether the seal canonicalization is present or available.
    /// </summary>
    /// <returns><c>true</c> when the seal canonicalization is available; otherwise, <c>false</c>.</returns>
    public bool HasSealCanonicalization()
    {
        return sealCanonicalization != null;
    }

    /// <summary>
    /// Gets the seal canonicalization.
    /// </summary>
    /// <returns>The current seal canonicalization.</returns>
    public EmailArcSealCanonicalizationResult? GetSealCanonicalization()
    {
        return sealCanonicalization;
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
}
