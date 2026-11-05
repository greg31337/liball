using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents an ARC set.
/// </summary>
public class EmailArcSet : IEmailArcSetContract
{
    int instance;
    List<EmailArcHeaderField> authenticationResultsFields;
    List<EmailArcHeaderField> messageSignatureFields;
    List<EmailArcHeaderField> sealFields;
    /// <summary>
    /// Creates a new ARC set.
    /// </summary>
    /// <param name = "instance">The instance used by the operation.</param>
    /// <param name = "authenticationResultsFields">The authentication results fields used by the operation.</param>
    /// <param name = "messageSignatureFields">The message signature fields used by the operation.</param>
    /// <param name = "sealFields">The seal fields used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcSet(int instance, List<EmailArcHeaderField> authenticationResultsFields, List<EmailArcHeaderField> messageSignatureFields, List<EmailArcHeaderField> sealFields)
    {
        if (instance < 1 || instance > 50)
            throw new ArgumentOutOfRangeException("instance");
        if (authenticationResultsFields == null || messageSignatureFields == null || sealFields == null)
            throw new ArgumentNullException("authenticationResultsFields");
        this.instance = instance;
        this.authenticationResultsFields = new List<EmailArcHeaderField>(authenticationResultsFields);
        this.messageSignatureFields = new List<EmailArcHeaderField>(messageSignatureFields);
        this.sealFields = new List<EmailArcHeaderField>(sealFields);
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
    /// Gets the authentication results field count.
    /// </summary>
    /// <returns>The current authentication results field count.</returns>
    public int GetAuthenticationResultsFieldCount()
    {
        return authenticationResultsFields.Count;
    }

    /// <summary>
    /// Gets the message signature field count.
    /// </summary>
    /// <returns>The current message signature field count.</returns>
    public int GetMessageSignatureFieldCount()
    {
        return messageSignatureFields.Count;
    }

    /// <summary>
    /// Gets the seal field count.
    /// </summary>
    /// <returns>The current seal field count.</returns>
    public int GetSealFieldCount()
    {
        return sealFields.Count;
    }

    /// <summary>
    /// Gets the authentication results field.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current authentication results field.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcHeaderField GetAuthenticationResultsField(int index)
    {
        if (index < 0 || index >= authenticationResultsFields.Count)
            throw new ArgumentOutOfRangeException("index");
        return authenticationResultsFields[index];
    }

    /// <summary>
    /// Gets the message signature field.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current message signature field.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcHeaderField GetMessageSignatureField(int index)
    {
        if (index < 0 || index >= messageSignatureFields.Count)
            throw new ArgumentOutOfRangeException("index");
        return messageSignatureFields[index];
    }

    /// <summary>
    /// Gets the seal field.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current seal field.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcHeaderField GetSealField(int index)
    {
        if (index < 0 || index >= sealFields.Count)
            throw new ArgumentOutOfRangeException("index");
        return sealFields[index];
    }

    /// <summary>
    /// Gets the authentication results fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current authentication results fields.</returns>
    public List<EmailArcHeaderField> GetAuthenticationResultsFields()
    {
        return new List<EmailArcHeaderField>(authenticationResultsFields);
    }

    /// <summary>
    /// Gets the message signature fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current message signature fields.</returns>
    public List<EmailArcHeaderField> GetMessageSignatureFields()
    {
        return new List<EmailArcHeaderField>(messageSignatureFields);
    }

    /// <summary>
    /// Gets the seal fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current seal fields.</returns>
    public List<EmailArcHeaderField> GetSealFields()
    {
        return new List<EmailArcHeaderField>(sealFields);
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return authenticationResultsFields.Count == 1 && messageSignatureFields.Count == 1 && sealFields.Count == 1;
    }

    /// <summary>
    /// Determines whether the duplicate fields is present or available.
    /// </summary>
    /// <returns><c>true</c> when the duplicate fields is available; otherwise, <c>false</c>.</returns>
    public bool HasDuplicateFields()
    {
        return authenticationResultsFields.Count > 1 || messageSignatureFields.Count > 1 || sealFields.Count > 1;
    }

    /// <summary>
    /// Determines whether the valid fields is present or available.
    /// </summary>
    /// <returns><c>true</c> when the valid fields is available; otherwise, <c>false</c>.</returns>
    public bool HasValidFields()
    {
        return IsSingleValid(authenticationResultsFields) && IsSingleValid(messageSignatureFields) && IsSingleValid(sealFields);
    }

    /// <summary>
    /// Determines whether the authentication results link is present or available.
    /// </summary>
    /// <returns><c>true</c> when the authentication results link is available; otherwise, <c>false</c>.</returns>
    public bool HasAuthenticationResultsLink()
    {
        return authenticationResultsFields.Count == 1 && authenticationResultsFields[0].HasAuthenticationResults();
    }

    /// <summary>
    /// Gets the authentication results.
    /// </summary>
    /// <returns>The current authentication results.</returns>
    public EmailAuthenticationResults? GetAuthenticationResults()
    {
        return HasAuthenticationResultsLink() ? authenticationResultsFields[0].GetAuthenticationResults() : null;
    }

    /// <summary>
    /// Gets the declared chain status.
    /// </summary>
    /// <returns>The current declared chain status.</returns>
    public EmailArcChainStatus GetDeclaredChainStatus()
    {
        if (sealFields.Count != 1 || !sealFields[0].HasSeal())
            return EmailArcChainStatus.Unknown;
        return (sealFields[0].GetSeal() ?? throw new InvalidOperationException()).GetChainStatus();
    }

    /// <summary>
    /// Gets the message signature algorithm kind.
    /// </summary>
    /// <returns>The current message signature algorithm kind.</returns>
    public EmailDkimAlgorithmKind GetMessageSignatureAlgorithmKind()
    {
        if (messageSignatureFields.Count != 1 || !messageSignatureFields[0].HasMessageSignature())
            return EmailDkimAlgorithmKind.Unknown;
        return (messageSignatureFields[0].GetMessageSignature() ?? throw new InvalidOperationException()).GetAlgorithmKind();
    }

    /// <summary>
    /// Gets the seal algorithm kind.
    /// </summary>
    /// <returns>The current seal algorithm kind.</returns>
    public EmailDkimAlgorithmKind GetSealAlgorithmKind()
    {
        if (sealFields.Count != 1 || !sealFields[0].HasSeal())
            return EmailDkimAlgorithmKind.Unknown;
        return (sealFields[0].GetSeal() ?? throw new InvalidOperationException()).GetAlgorithmKind();
    }

    /// <summary>
    /// Determines whether the matching algorithms is present or available.
    /// </summary>
    /// <returns><c>true</c> when the matching algorithms is available; otherwise, <c>false</c>.</returns>
    public bool HasMatchingAlgorithms()
    {
        if (messageSignatureFields.Count != 1 || sealFields.Count != 1 || !messageSignatureFields[0].HasMessageSignature() || !sealFields[0].HasSeal())
            return false;
        return string.Equals((messageSignatureFields[0].GetMessageSignature() ?? throw new InvalidOperationException()).GetAlgorithm(), (sealFields[0].GetSeal() ?? throw new InvalidOperationException()).GetAlgorithm(), StringComparison.OrdinalIgnoreCase);
    }

    static bool IsSingleValid(List<EmailArcHeaderField> fields)
    {
        return fields.Count == 1 && fields[0].IsParseSuccess();
    }
}
