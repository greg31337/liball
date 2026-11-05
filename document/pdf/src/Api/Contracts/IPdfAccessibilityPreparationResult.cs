using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfAccessibilityPreparationResult"/>.
/// </summary>
public interface IPdfAccessibilityPreparationResult
{
    /// <summary>
    /// Adds an error to this accessibility preparation result.
    /// </summary>
    /// <param name="code">The code to use for this operation.</param>
    /// <param name="message">The message to use for this operation.</param>
    void AddError(string code, string message);

    /// <summary>
    /// Adds a warning to this accessibility preparation result.
    /// </summary>
    /// <param name="code">The code to use for this operation.</param>
    /// <param name="message">The message to use for this operation.</param>
    void AddWarning(string code, string message);

    /// <summary>
    /// Returns the issues in the order in which they were added.
    /// </summary>
    /// <returns>A read-only view of the recorded issues.</returns>
    IReadOnlyList<PdfAccessibilityPreparationIssue> GetIssues();

    /// <summary>
    /// Determines whether the current object has errors.
    /// </summary>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    bool HasErrors();

    /// <summary>
    /// Determines whether the current object is ready.
    /// </summary>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    bool IsReady();

    /// <summary>
    /// Determines whether the current object has issue.
    /// </summary>
    /// <param name="code">The code to use for this operation.</param>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    bool HasIssue(string code);

    /// <summary>
    /// Counts the errors currently recorded in this result.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    int CountErrors();

    /// <summary>
    /// Counts the warnings currently recorded in this result.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    int CountWarnings();
}
