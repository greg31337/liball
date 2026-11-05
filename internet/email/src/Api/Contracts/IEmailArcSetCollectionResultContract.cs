using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcSetCollectionResult"/>.
/// </summary>
public interface IEmailArcSetCollectionResultContract
{
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    EmailArcSetCollectionStatus GetStatus();
    /// <summary>
    /// Determines whether the ARC headers is present or available.
    /// </summary>
    /// <returns><c>true</c> when the ARC headers is available; otherwise, <c>false</c>.</returns>
    bool HasArcHeaders();
    /// <summary>
    /// Determines whether the current value or state is structurally valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is structurally valid; otherwise, <c>false</c>.</returns>
    bool IsStructurallyValid();
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
    EmailArcSet GetSet(int index);
    /// <summary>
    /// Gets the set by instance.
    /// </summary>
    /// <param name = "instance">The instance used by the operation.</param>
    /// <returns>The current set by instance.</returns>
    EmailArcSet? GetSetByInstance(int instance);
    /// <summary>
    /// Gets the sets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current sets.</returns>
    List<EmailArcSet> GetSets();
    /// <summary>
    /// Gets the field count.
    /// </summary>
    /// <returns>The current field count.</returns>
    int GetFieldCount();
    /// <summary>
    /// Gets the field.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current field.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailArcHeaderField GetField(int index);
    /// <summary>
    /// Gets the fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current fields.</returns>
    List<EmailArcHeaderField> GetFields();
    /// <summary>
    /// Gets the unassigned field count.
    /// </summary>
    /// <returns>The current unassigned field count.</returns>
    int GetUnassignedFieldCount();
    /// <summary>
    /// Gets the unassigned fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current unassigned fields.</returns>
    List<EmailArcHeaderField> GetUnassignedFields();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the continuous instances is present or available.
    /// </summary>
    /// <returns><c>true</c> when the continuous instances is available; otherwise, <c>false</c>.</returns>
    bool HasContinuousInstances();
    /// <summary>
    /// Determines whether the complete sets is present or available.
    /// </summary>
    /// <returns><c>true</c> when the complete sets is available; otherwise, <c>false</c>.</returns>
    bool HasCompleteSets();
    /// <summary>
    /// Determines whether the valid chain statuses is present or available.
    /// </summary>
    /// <returns><c>true</c> when the valid chain statuses is available; otherwise, <c>false</c>.</returns>
    bool HasValidChainStatuses();
    /// <summary>
    /// Gets the maximum instance.
    /// </summary>
    /// <returns>The current maximum instance.</returns>
    int GetMaximumInstance();
    /// <summary>
    /// Gets the next instance.
    /// </summary>
    /// <returns>The current next instance.</returns>
    int GetNextInstance();
}
