using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines operations exposed by <see cref="PdfValidationResult"/>.
/// </summary>
public interface IPdfValidationResult
{
    /// <summary>Adds a validation error.</summary>
    /// <param name="code">The stable finding code.</param>
    /// <param name="message">The finding message.</param>
    /// <param name="objectNumber">The related object number, or zero.</param>
    void AddError(string code, string message, int objectNumber);

    /// <summary>Adds a validation warning.</summary>
    /// <param name="code">The stable finding code.</param>
    /// <param name="message">The finding message.</param>
    /// <param name="objectNumber">The related object number, or zero.</param>
    void AddWarning(string code, string message, int objectNumber);

    /// <summary>Returns findings in validation order.</summary>
    /// <returns>A read-only view of the findings.</returns>
    IReadOnlyList<PdfValidationIssue> GetIssues();

    /// <summary>Determines whether errors were recorded.</summary>
    /// <returns>True when at least one error was recorded.</returns>
    bool HasErrors();

    /// <summary>Determines whether validation succeeded.</summary>
    /// <returns>True when no errors were recorded.</returns>
    bool IsValid();

    /// <summary>Determines whether a stable finding code was recorded.</summary>
    /// <param name="code">The finding code.</param>
    /// <returns>True when the code was recorded.</returns>
    bool HasIssue(string code);

    /// <summary>Counts validation errors.</summary>
    /// <returns>The error count.</returns>
    int CountErrors();

    /// <summary>Counts validation warnings.</summary>
    /// <returns>The warning count.</returns>
    int CountWarnings();
}
