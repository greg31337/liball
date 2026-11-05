using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessage"/>.
/// </summary>
public interface IEmailMessageContract
{
    /// <summary>
    /// Gets the headers.
    /// </summary>
    /// <returns>The current headers.</returns>
    EmailHeaders? GetHeaders();
    /// <summary>
    /// Determines whether the body is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body is available; otherwise, <c>false</c>.</returns>
    bool HasBody();
    /// <summary>
    /// Gets the body.
    /// </summary>
    /// <returns>The current body.</returns>
    MimeEntity? GetBody();
    /// <summary>
    /// Sets the body used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetBody(MimeEntity value);
    /// <summary>
    /// Gets the envelope.
    /// </summary>
    /// <returns>The current envelope.</returns>
    EmailEnvelope? GetEnvelope();
    /// <summary>
    /// Determines whether the source info is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source info is available; otherwise, <c>false</c>.</returns>
    bool HasSourceInfo();
    /// <summary>
    /// Gets the source info.
    /// </summary>
    /// <returns>The current source info.</returns>
    EmailSourceInfo? GetSourceInfo();
    /// <summary>
    /// Gets the own issues.
    /// </summary>
    /// <returns>The current own issues.</returns>
    EmailIssueList GetOwnIssues();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Adds an issue to the current collection or model.
    /// </summary>
    /// <param name = "issue">The issue used by the operation.</param>
    void AddIssue(EmailIssue issue);
    /// <summary>
    /// Clears the stored own issues value or values.
    /// </summary>
    void ClearOwnIssues();
    /// <summary>
    /// Gets the dirty state.
    /// </summary>
    /// <returns>The current dirty state.</returns>
    EmailDirtyState GetDirtyState();
    /// <summary>
    /// Determines whether the current value or state is exact dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is exact dirty; otherwise, <c>false</c>.</returns>
    bool IsExactDirty();
    /// <summary>
    /// Determines whether the current value or state is semantic dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is semantic dirty; otherwise, <c>false</c>.</returns>
    bool IsSemanticDirty();
    /// <summary>
    /// Determines whether the current value or state is signature mutation guard enabled.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signature mutation guard enabled; otherwise, <c>false</c>.</returns>
    bool IsSignatureMutationGuardEnabled();
    /// <summary>
    /// Enables the signature mutation guard.
    /// </summary>
    void EnableSignatureMutationGuard();
    /// <summary>
    /// Disables the signature mutation guard.
    /// </summary>
    void DisableSignatureMutationGuard();
    /// <summary>
    /// Marks the exact dirty.
    /// </summary>
    void MarkExactDirty();
    /// <summary>
    /// Marks the semantic dirty.
    /// </summary>
    void MarkSemanticDirty();
    /// <summary>
    /// Marks the dirty.
    /// </summary>
    void MarkDirty();
    /// <summary>
    /// Marks the range dirty.
    /// </summary>
    /// <param name = "range">The range used by the operation.</param>
    /// <param name = "semanticChange">Whether to enable the semantic change behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    void MarkRangeDirty(EmailRange range, bool semanticChange);
    /// <summary>
    /// Gets the clean ranges.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current clean ranges.</returns>
    List<EmailRange> GetCleanRanges();
    /// <summary>
    /// Determines whether the current value or state is range clean.
    /// </summary>
    /// <param name = "range">The range used by the operation.</param>
    /// <returns><c>true</c> when the value or state is range clean; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    bool IsRangeClean(EmailRange? range);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailMessage Clone();
    /// <summary>
    /// Detaches the source.
    /// </summary>
    /// <param name = "maximumContentBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void DetachSource(long maximumContentBytes);
}
