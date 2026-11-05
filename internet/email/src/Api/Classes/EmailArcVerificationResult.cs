using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a ARC verification operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailArcVerificationResult : IEmailArcVerificationResultContract
{
    EmailArcVerificationStatus status;
    EmailArcVerificationFailureKind failureKind;
    int failureInstance;
    string reason;
    EmailArcSetCollectionResult collection;
    List<EmailArcSetVerificationResult> sets;
    bool hasOldestPass;
    int oldestPass;
    /// <summary>
    /// Creates a new ARC verification result.
    /// </summary>
    /// <param name = "status">The status used by the operation.</param>
    /// <param name = "failureKind">The failure kind used by the operation.</param>
    /// <param name = "failureInstance">The failure instance used by the operation.</param>
    /// <param name = "reason">The reason used by the operation.</param>
    /// <param name = "collection">The collection used by the operation.</param>
    /// <param name = "sets">The sets used by the operation.</param>
    /// <param name = "hasOldestPass">Whether to enable the has oldest pass behavior.</param>
    /// <param name = "oldestPass">The oldest pass used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcVerificationResult(EmailArcVerificationStatus status, EmailArcVerificationFailureKind failureKind, int failureInstance, string reason, EmailArcSetCollectionResult collection, List<EmailArcSetVerificationResult> sets, bool hasOldestPass, int oldestPass)
    {
        if (status < EmailArcVerificationStatus.None || status > EmailArcVerificationStatus.Fail)
            throw new ArgumentOutOfRangeException("status");
        if (failureKind < EmailArcVerificationFailureKind.None || failureKind > EmailArcVerificationFailureKind.Seal)
            throw new ArgumentOutOfRangeException("failureKind");
        if (failureInstance < 0 || failureInstance > 50 || oldestPass < 0 || oldestPass > 50)
            throw new ArgumentOutOfRangeException("failureInstance");
        if (reason == null || collection == null || sets == null)
            throw new ArgumentNullException("reason");
        this.status = status;
        this.failureKind = failureKind;
        this.failureInstance = failureInstance;
        this.reason = reason;
        this.collection = collection;
        this.sets = new List<EmailArcSetVerificationResult>(sets);
        this.hasOldestPass = hasOldestPass;
        this.oldestPass = oldestPass;
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailArcVerificationStatus GetStatus()
    {
        return status;
    }

    /// <summary>
    /// Gets the chain status.
    /// </summary>
    /// <returns>The current chain status.</returns>
    public EmailArcChainStatus GetChainStatus()
    {
        return status == EmailArcVerificationStatus.None ? EmailArcChainStatus.None : status == EmailArcVerificationStatus.Pass ? EmailArcChainStatus.Pass : EmailArcChainStatus.Fail;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return status == EmailArcVerificationStatus.Pass;
    }

    /// <summary>
    /// Gets the failure kind.
    /// </summary>
    /// <returns>The current failure kind.</returns>
    public EmailArcVerificationFailureKind GetFailureKind()
    {
        return failureKind;
    }

    /// <summary>
    /// Gets the failure instance.
    /// </summary>
    /// <returns>The current failure instance.</returns>
    public int GetFailureInstance()
    {
        return failureInstance;
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
    /// Gets the collection.
    /// </summary>
    /// <returns>The current collection.</returns>
    public EmailArcSetCollectionResult GetCollection()
    {
        return collection;
    }

    /// <summary>
    /// Gets the set count.
    /// </summary>
    /// <returns>The current set count.</returns>
    public int GetSetCount()
    {
        return sets.Count;
    }

    /// <summary>
    /// Gets the set.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current set.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcSetVerificationResult GetSet(int index)
    {
        if (index < 0 || index >= sets.Count)
            throw new ArgumentOutOfRangeException("index");
        return sets[index];
    }

    /// <summary>
    /// Gets the sets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current sets.</returns>
    public List<EmailArcSetVerificationResult> GetSets()
    {
        return new List<EmailArcSetVerificationResult>(sets);
    }

    /// <summary>
    /// Determines whether the oldest pass is present or available.
    /// </summary>
    /// <returns><c>true</c> when the oldest pass is available; otherwise, <c>false</c>.</returns>
    public bool HasOldestPass()
    {
        return hasOldestPass;
    }

    /// <summary>
    /// Gets the oldest pass.
    /// </summary>
    /// <returns>The current oldest pass.</returns>
    public int GetOldestPass()
    {
        return oldestPass;
    }

    /// <summary>
    /// Gets the authentication result kind.
    /// </summary>
    /// <returns>The current authentication result kind.</returns>
    public EmailAuthenticationResultKind GetAuthenticationResultKind()
    {
        if (status == EmailArcVerificationStatus.Pass)
            return EmailAuthenticationResultKind.Pass;
        if (status == EmailArcVerificationStatus.Fail)
            return EmailAuthenticationResultKind.Fail;
        return EmailAuthenticationResultKind.None;
    }
}
