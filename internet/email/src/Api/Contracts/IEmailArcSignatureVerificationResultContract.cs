using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcSignatureVerificationResult"/>.
/// </summary>
public interface IEmailArcSignatureVerificationResultContract
{
    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    int GetInstance();
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailSignatureHeaderKind GetKind();
    /// <summary>
    /// Gets the field.
    /// </summary>
    /// <returns>The current field.</returns>
    EmailArcHeaderField GetField();
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    EmailArcSignatureVerificationStatus GetStatus();
    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    string GetReason();
    /// <summary>
    /// Determines whether the operation or source value was evaluated.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasEvaluated();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
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
    /// Determines whether the message header canonicalization is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message header canonicalization is available; otherwise, <c>false</c>.</returns>
    bool HasMessageHeaderCanonicalization();
    /// <summary>
    /// Gets the message header canonicalization.
    /// </summary>
    /// <returns>The current message header canonicalization.</returns>
    EmailDkimHeaderCanonicalizationResult? GetMessageHeaderCanonicalization();
    /// <summary>
    /// Determines whether the seal canonicalization is present or available.
    /// </summary>
    /// <returns><c>true</c> when the seal canonicalization is available; otherwise, <c>false</c>.</returns>
    bool HasSealCanonicalization();
    /// <summary>
    /// Gets the seal canonicalization.
    /// </summary>
    /// <returns>The current seal canonicalization.</returns>
    EmailArcSealCanonicalizationResult? GetSealCanonicalization();
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
}
