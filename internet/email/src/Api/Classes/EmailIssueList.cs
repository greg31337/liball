using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Stores an ordered collection of diagnostics without treating warnings as operation failure.
/// </summary>
public class EmailIssueList : IEmailIssueListContract
{
    List<EmailIssue> issues;
    /// <summary>
    /// Creates a new issue list.
    /// </summary>
    public EmailIssueList()
    {
        issues = new List<EmailIssue>();
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    public int GetCount()
    {
        return issues.Count;
    }

    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailIssue GetAt(int index)
    {
        if (index < 0 || index >= issues.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return issues[index];
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    public List<EmailIssue> GetAll()
    {
        return new List<EmailIssue>(issues);
    }

    /// <summary>
    /// Adds a value to the current collection or model.
    /// </summary>
    /// <param name = "issue">The issue used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void Add(EmailIssue issue)
    {
        if (issue == null)
        {
            throw new ArgumentNullException("issue");
        }

        issues.Add(issue);
    }

    /// <summary>
    /// Adds a range to the current collection or model.
    /// </summary>
    /// <param name = "other">The other used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void AddRange(EmailIssueList? other)
    {
        if (other == null)
        {
            throw new ArgumentNullException("other");
        }

        List<EmailIssue> additions = other.GetAll();
        int index = 0;
        while (index < additions.Count)
        {
            issues.Add(additions[index]);
            index++;
        }
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <param name = "severity">The severity used by the operation.</param>
    /// <returns>The current count.</returns>
    public int GetCountBySeverity(EmailIssueSeverity severity)
    {
        ValidateSeverity(severity);
        int count = 0;
        int index = 0;
        while (index < issues.Count)
        {
            if (issues[index].GetSeverity() == severity)
            {
                count++;
            }

            index++;
        }

        return count;
    }

    /// <summary>
    /// Determines whether the severity at least is present or available.
    /// </summary>
    /// <param name = "severity">The severity used by the operation.</param>
    /// <returns><c>true</c> when the severity at least is available; otherwise, <c>false</c>.</returns>
    public bool HasSeverityAtLeast(EmailIssueSeverity severity)
    {
        ValidateSeverity(severity);
        int index = 0;
        while (index < issues.Count)
        {
            if (issues[index].GetSeverity() >= severity)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the collection contains code.
    /// </summary>
    /// <param name = "code">The code used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool ContainsCode(EmailIssueCode code)
    {
        int index = 0;
        while (index < issues.Count)
        {
            if (issues[index].GetCode() == code)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    /// <summary>
    /// Clears all stored items or state.
    /// </summary>
    public void Clear()
    {
        issues.Clear();
    }

    static void ValidateSeverity(EmailIssueSeverity severity)
    {
        if (severity < EmailIssueSeverity.Information || severity > EmailIssueSeverity.Fatal)
        {
            throw new ArgumentOutOfRangeException("severity");
        }
    }
}
