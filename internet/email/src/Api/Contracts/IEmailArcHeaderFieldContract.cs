using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcHeaderField"/>.
/// </summary>
public interface IEmailArcHeaderFieldContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailSignatureHeaderKind GetKind();
    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <returns>The current header.</returns>
    EmailHeader GetHeader();
    /// <summary>
    /// Gets the source index.
    /// </summary>
    /// <returns>The current source index.</returns>
    int GetSourceIndex();
    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    int GetInstance();
    /// <summary>
    /// Determines whether the instance is present or available.
    /// </summary>
    /// <returns><c>true</c> when the instance is available; otherwise, <c>false</c>.</returns>
    bool HasInstance();
    /// <summary>
    /// Determines whether the current value or state is parse success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is parse success; otherwise, <c>false</c>.</returns>
    bool IsParseSuccess();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the message signature is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message signature is available; otherwise, <c>false</c>.</returns>
    bool HasMessageSignature();
    /// <summary>
    /// Gets the message signature.
    /// </summary>
    /// <returns>The current message signature.</returns>
    EmailDkimSignature? GetMessageSignature();
    /// <summary>
    /// Determines whether the seal is present or available.
    /// </summary>
    /// <returns><c>true</c> when the seal is available; otherwise, <c>false</c>.</returns>
    bool HasSeal();
    /// <summary>
    /// Gets the seal.
    /// </summary>
    /// <returns>The current seal.</returns>
    EmailArcSeal? GetSeal();
    /// <summary>
    /// Determines whether the ARC authentication results is present or available.
    /// </summary>
    /// <returns><c>true</c> when the ARC authentication results is available; otherwise, <c>false</c>.</returns>
    bool HasArcAuthenticationResults();
    /// <summary>
    /// Gets the ARC authentication results.
    /// </summary>
    /// <returns>The current ARC authentication results.</returns>
    EmailArcAuthenticationResults? GetArcAuthenticationResults();
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
