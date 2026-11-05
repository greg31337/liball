using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxResultOperations"/>.
/// </summary>
public interface IPptxResultOperations
{
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int Count(PptxResult target);
    /// <summary>
    /// Returns <c>true</c> when the operation has no error diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool Succeeded(PptxResult target);
    /// <summary>
    /// Returns <c>true</c> when the operation contains at least one error diagnostic.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool Failed(PptxResult target);
    /// <summary>
    /// Reports whether the result contains or enables errors.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasErrors(PptxResult target);
    /// <summary>
    /// Adds an item and returns the created model when applicable.
    /// </summary>
    /// <param name="diagnostic">The diagnostic value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void Add(PptxResult target, PptxDiagnostic diagnostic);
    /// <summary>
    /// Adds the info and returns the created model when applicable.
    /// </summary>
    /// <param name="code">The code value to use.</param>
    /// <param name="message">The message value to use.</param>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="xmlPath">The xml path value to use.</param>
    /// <returns>The resulting diagnostic value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxDiagnostic AddInfo(PptxResult target, string code, string message, string partName, string relationshipId, string xmlPath);
    /// <summary>
    /// Adds the warning and returns the created model when applicable.
    /// </summary>
    /// <param name="code">The code value to use.</param>
    /// <param name="message">The message value to use.</param>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="xmlPath">The xml path value to use.</param>
    /// <returns>The resulting diagnostic value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxDiagnostic AddWarning(PptxResult target, string code, string message, string partName, string relationshipId, string xmlPath);
    /// <summary>
    /// Adds the error and returns the created model when applicable.
    /// </summary>
    /// <param name="code">The code value to use.</param>
    /// <param name="message">The message value to use.</param>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="xmlPath">The xml path value to use.</param>
    /// <returns>The resulting diagnostic value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxDiagnostic AddError(PptxResult target, string code, string message, string partName, string relationshipId, string xmlPath);
    /// <summary>
    /// Formats the result into a human-readable string.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string FormatDiagnostics(PptxResult target);
}
