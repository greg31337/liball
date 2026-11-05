using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcSet"/>.
/// </summary>
public interface IEmailArcSetContract
{
    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    int GetInstance();
    /// <summary>
    /// Gets the authentication results field count.
    /// </summary>
    /// <returns>The current authentication results field count.</returns>
    int GetAuthenticationResultsFieldCount();
    /// <summary>
    /// Gets the message signature field count.
    /// </summary>
    /// <returns>The current message signature field count.</returns>
    int GetMessageSignatureFieldCount();
    /// <summary>
    /// Gets the seal field count.
    /// </summary>
    /// <returns>The current seal field count.</returns>
    int GetSealFieldCount();
    /// <summary>
    /// Gets the authentication results field.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current authentication results field.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailArcHeaderField GetAuthenticationResultsField(int index);
    /// <summary>
    /// Gets the message signature field.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current message signature field.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailArcHeaderField GetMessageSignatureField(int index);
    /// <summary>
    /// Gets the seal field.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current seal field.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailArcHeaderField GetSealField(int index);
    /// <summary>
    /// Gets the authentication results fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current authentication results fields.</returns>
    List<EmailArcHeaderField> GetAuthenticationResultsFields();
    /// <summary>
    /// Gets the message signature fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current message signature fields.</returns>
    List<EmailArcHeaderField> GetMessageSignatureFields();
    /// <summary>
    /// Gets the seal fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current seal fields.</returns>
    List<EmailArcHeaderField> GetSealFields();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Determines whether the duplicate fields is present or available.
    /// </summary>
    /// <returns><c>true</c> when the duplicate fields is available; otherwise, <c>false</c>.</returns>
    bool HasDuplicateFields();
    /// <summary>
    /// Determines whether the valid fields is present or available.
    /// </summary>
    /// <returns><c>true</c> when the valid fields is available; otherwise, <c>false</c>.</returns>
    bool HasValidFields();
    /// <summary>
    /// Determines whether the authentication results link is present or available.
    /// </summary>
    /// <returns><c>true</c> when the authentication results link is available; otherwise, <c>false</c>.</returns>
    bool HasAuthenticationResultsLink();
    /// <summary>
    /// Gets the authentication results.
    /// </summary>
    /// <returns>The current authentication results.</returns>
    EmailAuthenticationResults? GetAuthenticationResults();
    /// <summary>
    /// Gets the declared chain status.
    /// </summary>
    /// <returns>The current declared chain status.</returns>
    EmailArcChainStatus GetDeclaredChainStatus();
    /// <summary>
    /// Gets the message signature algorithm kind.
    /// </summary>
    /// <returns>The current message signature algorithm kind.</returns>
    EmailDkimAlgorithmKind GetMessageSignatureAlgorithmKind();
    /// <summary>
    /// Gets the seal algorithm kind.
    /// </summary>
    /// <returns>The current seal algorithm kind.</returns>
    EmailDkimAlgorithmKind GetSealAlgorithmKind();
    /// <summary>
    /// Determines whether the matching algorithms is present or available.
    /// </summary>
    /// <returns><c>true</c> when the matching algorithms is available; otherwise, <c>false</c>.</returns>
    bool HasMatchingAlgorithms();
}
