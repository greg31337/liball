using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcVerificationResult"/>.
/// </summary>
public interface IEmailArcVerificationResultContract
{
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    EmailArcVerificationStatus GetStatus();
    /// <summary>
    /// Gets the chain status.
    /// </summary>
    /// <returns>The current chain status.</returns>
    EmailArcChainStatus GetChainStatus();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Gets the failure kind.
    /// </summary>
    /// <returns>The current failure kind.</returns>
    EmailArcVerificationFailureKind GetFailureKind();
    /// <summary>
    /// Gets the failure instance.
    /// </summary>
    /// <returns>The current failure instance.</returns>
    int GetFailureInstance();
    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    string GetReason();
    /// <summary>
    /// Gets the collection.
    /// </summary>
    /// <returns>The current collection.</returns>
    EmailArcSetCollectionResult GetCollection();
    /// <summary>
    /// Gets the set count.
    /// </summary>
    /// <returns>The current set count.</returns>
    int GetSetCount();
    /// <summary>
    /// Gets the set.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current set.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailArcSetVerificationResult GetSet(int index);
    /// <summary>
    /// Gets the sets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current sets.</returns>
    List<EmailArcSetVerificationResult> GetSets();
    /// <summary>
    /// Determines whether the oldest pass is present or available.
    /// </summary>
    /// <returns><c>true</c> when the oldest pass is available; otherwise, <c>false</c>.</returns>
    bool HasOldestPass();
    /// <summary>
    /// Gets the oldest pass.
    /// </summary>
    /// <returns>The current oldest pass.</returns>
    int GetOldestPass();
    /// <summary>
    /// Gets the authentication result kind.
    /// </summary>
    /// <returns>The current authentication result kind.</returns>
    EmailAuthenticationResultKind GetAuthenticationResultKind();
}
