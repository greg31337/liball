using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Contains ordered PDF structure or conformance validation findings.
/// </summary>
public class PdfValidationResult : IPdfValidationResult
{
    internal List<PdfValidationIssue> _issues;
    internal IReadOnlyList<PdfValidationIssue> Issues;

    /// <summary>Initializes an empty validation result.</summary>
    public PdfValidationResult()
    {
        _issues = new List<PdfValidationIssue>();
        Issues = _issues;
    }

    /// <summary>Adds a validation error.</summary>
    /// <param name="code">The stable finding code.</param>
    /// <param name="message">The finding message.</param>
    /// <param name="objectNumber">The related object number, or zero.</param>
    public void AddError(string code, string message, int objectNumber)
    {
        _issues.Add(new PdfValidationIssue(PdfValidationIssueSeverity.Error, code, message, objectNumber));
    }

    /// <summary>Adds a validation warning.</summary>
    /// <param name="code">The stable finding code.</param>
    /// <param name="message">The finding message.</param>
    /// <param name="objectNumber">The related object number, or zero.</param>
    public void AddWarning(string code, string message, int objectNumber)
    {
        _issues.Add(new PdfValidationIssue(PdfValidationIssueSeverity.Warning, code, message, objectNumber));
    }

    /// <summary>Returns findings in validation order.</summary>
    /// <returns>A read-only view of the findings.</returns>
    public IReadOnlyList<PdfValidationIssue> GetIssues()
    {
        return Issues;
    }

    /// <summary>Determines whether errors were recorded.</summary>
    /// <returns>True when at least one error was recorded.</returns>
    public bool HasErrors()
    {
        foreach (PdfValidationIssue issue in _issues)
        {
            if (issue.Severity == PdfValidationIssueSeverity.Error)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether validation succeeded.</summary>
    /// <returns>True when no errors were recorded.</returns>
    public bool IsValid()
    {
        return !HasErrors();
    }

    /// <summary>Determines whether a stable finding code was recorded.</summary>
    /// <param name="code">The finding code.</param>
    /// <returns>True when the code was recorded.</returns>
    public bool HasIssue(string code)
    {
        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        foreach (PdfValidationIssue issue in _issues)
        {
            if (issue.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Counts validation errors.</summary>
    /// <returns>The error count.</returns>
    public int CountErrors()
    {
        int count = 0;
        foreach (PdfValidationIssue issue in _issues)
        {
            if (issue.Severity == PdfValidationIssueSeverity.Error)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Counts validation warnings.</summary>
    /// <returns>The warning count.</returns>
    public int CountWarnings()
    {
        int count = 0;
        foreach (PdfValidationIssue issue in _issues)
        {
            if (issue.Severity == PdfValidationIssueSeverity.Warning)
            {
                count++;
            }
        }

        return count;
    }
}
