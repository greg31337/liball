using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Contains errors and warnings found while checking whether a document is ready for later PDF/A conversion.
/// </summary>
public class PdfAPreparationResult : IPdfAPreparationResult
{
    internal List<PdfAPreparationIssue> _issues;
    /// <summary>
    /// Exposes the issues currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfAPreparationIssue> Issues;

    /// <summary>
    /// Initializes an empty PDF/A preparation result.
    /// </summary>
    public PdfAPreparationResult()
    {
        _issues = new List<PdfAPreparationIssue>();
        Issues = _issues;
    }

    /// <summary>
    /// Adds a condition that prevents the document from being considered ready for PDF/A conversion.
    /// </summary>
    /// <param name="code">The code to use for this operation.</param>
    /// <param name="message">The message to use for this operation.</param>
    public void AddError(string code, string message)
    {
        _issues.Add(new PdfAPreparationIssue(PdfAPreparationIssueSeverity.Error, code, message));
    }

    /// <summary>
    /// Adds a condition that should be reviewed but does not by itself prevent PDF/A preparation.
    /// </summary>
    /// <param name="code">The code to use for this operation.</param>
    /// <param name="message">The message to use for this operation.</param>
    public void AddWarning(string code, string message)
    {
        _issues.Add(new PdfAPreparationIssue(PdfAPreparationIssueSeverity.Warning, code, message));
    }

    /// <summary>
    /// Returns the issues in the order in which they were added.
    /// </summary>
    /// <returns>A read-only view of the recorded issues.</returns>
    public IReadOnlyList<PdfAPreparationIssue> GetIssues()
    {
        return Issues;
    }

    /// <summary>
    /// Determines whether the current object has errors.
    /// </summary>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    public bool HasErrors()
    {
        foreach (PdfAPreparationIssue issue in _issues)
        {
            if (issue.Severity == PdfAPreparationIssueSeverity.Error)
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
    /// Determines whether an issue with the supplied stable code has been recorded.
    /// </summary>
    /// <param name="code">The code to use for this operation.</param>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    public bool HasIssue(string code)
    {
        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        foreach (PdfAPreparationIssue issue in _issues)
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
        foreach (PdfAPreparationIssue issue in _issues)
        {
            if (issue.Severity == PdfAPreparationIssueSeverity.Error)
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
        foreach (PdfAPreparationIssue issue in _issues)
        {
            if (issue.Severity == PdfAPreparationIssueSeverity.Warning)
            {
                count++;
            }
        }

        return count;
    }
}
