using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailIssueList"/>.
/// </summary>
public interface IEmailIssueListContract
{
    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    int GetCount();
    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailIssue GetAt(int index);
    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    List<EmailIssue> GetAll();
    /// <summary>
    /// Adds a value to the current collection or model.
    /// </summary>
    /// <param name = "issue">The issue used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void Add(EmailIssue issue);
    /// <summary>
    /// Adds a range to the current collection or model.
    /// </summary>
    /// <param name = "other">The other used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void AddRange(EmailIssueList? other);
    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <param name = "severity">The severity used by the operation.</param>
    /// <returns>The current count.</returns>
    int GetCountBySeverity(EmailIssueSeverity severity);
    /// <summary>
    /// Determines whether the severity at least is present or available.
    /// </summary>
    /// <param name = "severity">The severity used by the operation.</param>
    /// <returns><c>true</c> when the severity at least is available; otherwise, <c>false</c>.</returns>
    bool HasSeverityAtLeast(EmailIssueSeverity severity);
    /// <summary>
    /// Determines whether the collection contains code.
    /// </summary>
    /// <param name = "code">The code used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool ContainsCode(EmailIssueCode code);
    /// <summary>
    /// Clears all stored items or state.
    /// </summary>
    void Clear();
}
