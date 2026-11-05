using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents an ARC header field.
/// </summary>
public class EmailArcHeaderField : IEmailArcHeaderFieldContract
{
    EmailSignatureHeaderKind kind;
    EmailHeader header;
    int sourceIndex;
    int instance;
    bool parseSuccess;
    EmailIssueList issues;
    EmailDkimSignature? messageSignature;
    EmailArcSeal? seal;
    EmailArcAuthenticationResults? arcAuthenticationResults;
    /// <summary>
    /// Creates a new ARC header field.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "header">The header used by the operation.</param>
    /// <param name = "sourceIndex">The zero-based source index.</param>
    /// <param name = "instance">The instance used by the operation.</param>
    /// <param name = "parseSuccess">Whether to enable the parse success behavior.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "messageSignature">The message signature used by the operation.</param>
    /// <param name = "seal">The seal used by the operation.</param>
    /// <param name = "arcAuthenticationResults">The arc authentication results used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcHeaderField(EmailSignatureHeaderKind kind, EmailHeader header, int sourceIndex, int instance, bool parseSuccess, EmailIssueList issues, EmailDkimSignature? messageSignature, EmailArcSeal? seal, EmailArcAuthenticationResults? arcAuthenticationResults)
    {
        if (kind < EmailSignatureHeaderKind.ArcMessageSignature || kind > EmailSignatureHeaderKind.ArcAuthenticationResults)
            throw new ArgumentOutOfRangeException("kind");
        if (header == null || issues == null)
            throw new ArgumentNullException("header");
        if (sourceIndex < 0 || instance < 0 || instance > 50)
            throw new ArgumentOutOfRangeException("sourceIndex");
        int valueCount = 0;
        if (messageSignature != null)
            valueCount++;
        if (seal != null)
            valueCount++;
        if (arcAuthenticationResults != null)
            valueCount++;
        if (valueCount > 1)
            throw new ArgumentException("An ARC header field cannot contain multiple parsed value kinds.", "messageSignature");
        if (messageSignature != null && kind != EmailSignatureHeaderKind.ArcMessageSignature || seal != null && kind != EmailSignatureHeaderKind.ArcSeal || arcAuthenticationResults != null && kind != EmailSignatureHeaderKind.ArcAuthenticationResults)
            throw new ArgumentException("The parsed ARC value does not match the header kind.", "kind");
        this.kind = kind;
        this.header = header.Clone();
        this.sourceIndex = sourceIndex;
        this.instance = instance;
        this.parseSuccess = parseSuccess;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.messageSignature = messageSignature;
        this.seal = seal;
        this.arcAuthenticationResults = arcAuthenticationResults;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailSignatureHeaderKind GetKind()
    {
        return kind;
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
    /// Gets the source index.
    /// </summary>
    /// <returns>The current source index.</returns>
    public int GetSourceIndex()
    {
        return sourceIndex;
    }

    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    public int GetInstance()
    {
        return instance;
    }

    /// <summary>
    /// Determines whether the instance is present or available.
    /// </summary>
    /// <returns><c>true</c> when the instance is available; otherwise, <c>false</c>.</returns>
    public bool HasInstance()
    {
        return instance >= 1;
    }

    /// <summary>
    /// Determines whether the current value or state is parse success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is parse success; otherwise, <c>false</c>.</returns>
    public bool IsParseSuccess()
    {
        return parseSuccess;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    /// <summary>
    /// Determines whether the message signature is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message signature is available; otherwise, <c>false</c>.</returns>
    public bool HasMessageSignature()
    {
        return messageSignature != null;
    }

    /// <summary>
    /// Gets the message signature.
    /// </summary>
    /// <returns>The current message signature.</returns>
    public EmailDkimSignature? GetMessageSignature()
    {
        return messageSignature;
    }

    /// <summary>
    /// Determines whether the seal is present or available.
    /// </summary>
    /// <returns><c>true</c> when the seal is available; otherwise, <c>false</c>.</returns>
    public bool HasSeal()
    {
        return seal != null;
    }

    /// <summary>
    /// Gets the seal.
    /// </summary>
    /// <returns>The current seal.</returns>
    public EmailArcSeal? GetSeal()
    {
        return seal;
    }

    /// <summary>
    /// Determines whether the ARC authentication results is present or available.
    /// </summary>
    /// <returns><c>true</c> when the ARC authentication results is available; otherwise, <c>false</c>.</returns>
    public bool HasArcAuthenticationResults()
    {
        return arcAuthenticationResults != null;
    }

    /// <summary>
    /// Gets the ARC authentication results.
    /// </summary>
    /// <returns>The current ARC authentication results.</returns>
    public EmailArcAuthenticationResults? GetArcAuthenticationResults()
    {
        return arcAuthenticationResults;
    }

    /// <summary>
    /// Determines whether the authentication results is present or available.
    /// </summary>
    /// <returns><c>true</c> when the authentication results is available; otherwise, <c>false</c>.</returns>
    public bool HasAuthenticationResults()
    {
        return arcAuthenticationResults != null;
    }

    /// <summary>
    /// Gets the authentication results.
    /// </summary>
    /// <returns>The current authentication results.</returns>
    public EmailAuthenticationResults? GetAuthenticationResults()
    {
        return arcAuthenticationResults == null ? null : arcAuthenticationResults.GetAuthenticationResults();
    }
}
