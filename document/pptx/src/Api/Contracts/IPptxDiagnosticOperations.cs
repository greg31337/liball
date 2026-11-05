using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxDiagnosticOperations"/>.
/// </summary>
public interface IPptxDiagnosticOperations
{
    /// <summary>
    /// Reports whether the diagnostic is error.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsError(PptxDiagnostic target);
    /// <summary>
    /// Reports whether the diagnostic contains or enables location.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasLocation(PptxDiagnostic target);
    /// <summary>
    /// Formats the diagnostic into a human-readable string.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string Format(PptxDiagnostic target);
    /// <summary>
    /// Creates a new diagnostic using the supplied values.
    /// </summary>
    /// <param name="code">The code value to use.</param>
    /// <param name="message">The message value to use.</param>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="xmlPath">The xml path value to use.</param>
    /// <returns>The resulting diagnostic value.</returns>
    PptxDiagnostic CreateInfo(string code, string message, string partName, string relationshipId, string xmlPath);
    /// <summary>
    /// Creates a new diagnostic using the supplied values.
    /// </summary>
    /// <param name="code">The code value to use.</param>
    /// <param name="message">The message value to use.</param>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="xmlPath">The xml path value to use.</param>
    /// <returns>The resulting diagnostic value.</returns>
    PptxDiagnostic CreateWarning(string code, string message, string partName, string relationshipId, string xmlPath);
    /// <summary>
    /// Creates a new diagnostic using the supplied values.
    /// </summary>
    /// <param name="code">The code value to use.</param>
    /// <param name="message">The message value to use.</param>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="xmlPath">The xml path value to use.</param>
    /// <returns>The resulting diagnostic value.</returns>
    PptxDiagnostic CreateError(string code, string message, string partName, string relationshipId, string xmlPath);
}
