using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a ARC set collection operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailArcSetCollectionResult : IEmailArcSetCollectionResultContract
{
    EmailArcSetCollectionStatus status;
    List<EmailArcSet> sets;
    List<EmailArcHeaderField> fields;
    List<EmailArcHeaderField> unassignedFields;
    EmailIssueList issues;
    bool continuousInstances;
    bool completeSets;
    bool validChainStatuses;
    int maximumInstance;
    /// <summary>
    /// Creates a new ARC set collection result.
    /// </summary>
    /// <param name = "status">The status used by the operation.</param>
    /// <param name = "sets">The sets used by the operation.</param>
    /// <param name = "fields">The fields used by the operation.</param>
    /// <param name = "unassignedFields">The unassigned fields used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "continuousInstances">Whether to enable the continuous instances behavior.</param>
    /// <param name = "completeSets">Whether to enable the complete sets behavior.</param>
    /// <param name = "validChainStatuses">Whether to enable the valid chain statuses behavior.</param>
    /// <param name = "maximumInstance">The maximum instance used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcSetCollectionResult(EmailArcSetCollectionStatus status, List<EmailArcSet> sets, List<EmailArcHeaderField> fields, List<EmailArcHeaderField> unassignedFields, EmailIssueList issues, bool continuousInstances, bool completeSets, bool validChainStatuses, int maximumInstance)
    {
        if (status < EmailArcSetCollectionStatus.None || status > EmailArcSetCollectionStatus.Invalid)
            throw new ArgumentOutOfRangeException("status");
        if (sets == null || fields == null || unassignedFields == null || issues == null)
            throw new ArgumentNullException("sets");
        if (maximumInstance < 0 || maximumInstance > 50)
            throw new ArgumentOutOfRangeException("maximumInstance");
        this.status = status;
        this.sets = new List<EmailArcSet>(sets);
        this.fields = new List<EmailArcHeaderField>(fields);
        this.unassignedFields = new List<EmailArcHeaderField>(unassignedFields);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.continuousInstances = continuousInstances;
        this.completeSets = completeSets;
        this.validChainStatuses = validChainStatuses;
        this.maximumInstance = maximumInstance;
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailArcSetCollectionStatus GetStatus()
    {
        return status;
    }

    /// <summary>
    /// Determines whether the ARC headers is present or available.
    /// </summary>
    /// <returns><c>true</c> when the ARC headers is available; otherwise, <c>false</c>.</returns>
    public bool HasArcHeaders()
    {
        return fields.Count > 0;
    }

    /// <summary>
    /// Determines whether the current value or state is structurally valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is structurally valid; otherwise, <c>false</c>.</returns>
    public bool IsStructurallyValid()
    {
        return status == EmailArcSetCollectionStatus.Valid;
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
    public EmailArcSet GetSet(int index)
    {
        if (index < 0 || index >= sets.Count)
            throw new ArgumentOutOfRangeException("index");
        return sets[index];
    }

    /// <summary>
    /// Gets the set by instance.
    /// </summary>
    /// <param name = "instance">The instance used by the operation.</param>
    /// <returns>The current set by instance.</returns>
    public EmailArcSet? GetSetByInstance(int instance)
    {
        int index = 0;
        while (index < sets.Count)
        {
            if (sets[index].GetInstance() == instance)
                return sets[index];
            index++;
        }

        return null;
    }

    /// <summary>
    /// Gets the sets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current sets.</returns>
    public List<EmailArcSet> GetSets()
    {
        return new List<EmailArcSet>(sets);
    }

    /// <summary>
    /// Gets the field count.
    /// </summary>
    /// <returns>The current field count.</returns>
    public int GetFieldCount()
    {
        return fields.Count;
    }

    /// <summary>
    /// Gets the field.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current field.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcHeaderField GetField(int index)
    {
        if (index < 0 || index >= fields.Count)
            throw new ArgumentOutOfRangeException("index");
        return fields[index];
    }

    /// <summary>
    /// Gets the fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current fields.</returns>
    public List<EmailArcHeaderField> GetFields()
    {
        return new List<EmailArcHeaderField>(fields);
    }

    /// <summary>
    /// Gets the unassigned field count.
    /// </summary>
    /// <returns>The current unassigned field count.</returns>
    public int GetUnassignedFieldCount()
    {
        return unassignedFields.Count;
    }

    /// <summary>
    /// Gets the unassigned fields.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current unassigned fields.</returns>
    public List<EmailArcHeaderField> GetUnassignedFields()
    {
        return new List<EmailArcHeaderField>(unassignedFields);
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
    /// Determines whether the continuous instances is present or available.
    /// </summary>
    /// <returns><c>true</c> when the continuous instances is available; otherwise, <c>false</c>.</returns>
    public bool HasContinuousInstances()
    {
        return continuousInstances;
    }

    /// <summary>
    /// Determines whether the complete sets is present or available.
    /// </summary>
    /// <returns><c>true</c> when the complete sets is available; otherwise, <c>false</c>.</returns>
    public bool HasCompleteSets()
    {
        return completeSets;
    }

    /// <summary>
    /// Determines whether the valid chain statuses is present or available.
    /// </summary>
    /// <returns><c>true</c> when the valid chain statuses is available; otherwise, <c>false</c>.</returns>
    public bool HasValidChainStatuses()
    {
        return validChainStatuses;
    }

    /// <summary>
    /// Gets the maximum instance.
    /// </summary>
    /// <returns>The current maximum instance.</returns>
    public int GetMaximumInstance()
    {
        return maximumInstance;
    }

    /// <summary>
    /// Gets the next instance.
    /// </summary>
    /// <returns>The current next instance.</returns>
    public int GetNextInstance()
    {
        if (status == EmailArcSetCollectionStatus.None)
            return 1;
        if (status != EmailArcSetCollectionStatus.Valid || maximumInstance >= 50)
            return 0;
        return maximumInstance + 1;
    }
}
