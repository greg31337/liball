using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Contains the outcome of accessibility preparation.
/// </summary>
public class PdfAccessibilityPreparationResult : IPdfAccessibilityPreparationResult
{
    internal List<PdfAccessibilityPreparationIssue> _issues;
    /// <summary>
    /// Exposes the issues currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfAccessibilityPreparationIssue> Issues;

    /// <summary>
    /// Initializes a new accessibility preparation result.
    /// </summary>
    public PdfAccessibilityPreparationResult()
    {
        _issues = new List<PdfAccessibilityPreparationIssue>();
        Issues = _issues;
    }

    /// <summary>
    /// Adds an error to this accessibility preparation result.
    /// </summary>
    /// <param name="code">The code to use for this operation.</param>
    /// <param name="message">The message to use for this operation.</param>
    public void AddError(string code, string message)
    {
        _issues.Add(new PdfAccessibilityPreparationIssue(PdfAccessibilityPreparationIssueSeverity.Error, code, message));
    }

    /// <summary>
    /// Adds a warning to this accessibility preparation result.
    /// </summary>
    /// <param name="code">The code to use for this operation.</param>
    /// <param name="message">The message to use for this operation.</param>
    public void AddWarning(string code, string message)
    {
        _issues.Add(new PdfAccessibilityPreparationIssue(PdfAccessibilityPreparationIssueSeverity.Warning, code, message));
    }

    /// <summary>
    /// Returns the issues in the order in which they were added.
    /// </summary>
    /// <returns>A read-only view of the recorded issues.</returns>
    public IReadOnlyList<PdfAccessibilityPreparationIssue> GetIssues()
    {
        return Issues;
    }

    /// <summary>
    /// Determines whether the current object has errors.
    /// </summary>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    public bool HasErrors()
    {
        foreach (PdfAccessibilityPreparationIssue issue in _issues)
        {
            if (issue.Severity == PdfAccessibilityPreparationIssueSeverity.Error)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the current object is ready.
    /// </summary>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    public bool IsReady()
    {
        return !HasErrors();
    }

    /// <summary>
    /// Determines whether the current object has issue.
    /// </summary>
    /// <param name="code">The code to use for this operation.</param>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    public bool HasIssue(string code)
    {
        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        foreach (PdfAccessibilityPreparationIssue issue in _issues)
        {
            if (issue.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Counts the errors currently recorded in this result.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    public int CountErrors()
    {
        int count = 0;
        foreach (PdfAccessibilityPreparationIssue issue in _issues)
        {
            if (issue.Severity == PdfAccessibilityPreparationIssueSeverity.Error)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Counts the warnings currently recorded in this result.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    public int CountWarnings()
    {
        int count = 0;
        foreach (PdfAccessibilityPreparationIssue issue in _issues)
        {
            if (issue.Severity == PdfAccessibilityPreparationIssueSeverity.Warning)
            {
                count++;
            }
        }

        return count;
    }
}
