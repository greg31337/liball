using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxDiagnostic values.
/// </summary>
public class PptxDiagnosticOperations : IPptxDiagnosticOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxDiagnosticOperations()
    {
    }

    /// <summary>
    /// Reports whether the diagnostic is error.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsError(PptxDiagnostic target)
    {
        return target.IsError();
    }

    /// <summary>
    /// Reports whether the diagnostic contains or enables location.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasLocation(PptxDiagnostic target)
    {
        return target.HasLocation();
    }

    /// <summary>
    /// Formats the diagnostic into a human-readable string.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string Format(PptxDiagnostic target)
    {
        return target.Format();
    }

    /// <summary>
    /// Creates a new diagnostic using the supplied values.
    /// </summary>
    /// <param name="code">The code value to use.</param>
    /// <param name="message">The message value to use.</param>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="xmlPath">The xml path value to use.</param>
    /// <returns>The resulting diagnostic value.</returns>
    public PptxDiagnostic CreateInfo(string code, string message, string partName, string relationshipId, string xmlPath)
    {
        return PptxDiagnostic.CreateInfo(code, message, partName, relationshipId, xmlPath);
    }

    /// <summary>
    /// Creates a new diagnostic using the supplied values.
    /// </summary>
    /// <param name="code">The code value to use.</param>
    /// <param name="message">The message value to use.</param>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="xmlPath">The xml path value to use.</param>
    /// <returns>The resulting diagnostic value.</returns>
    public PptxDiagnostic CreateWarning(string code, string message, string partName, string relationshipId, string xmlPath)
    {
        return PptxDiagnostic.CreateWarning(code, message, partName, relationshipId, xmlPath);
    }

    /// <summary>
    /// Creates a new diagnostic using the supplied values.
    /// </summary>
    /// <param name="code">The code value to use.</param>
    /// <param name="message">The message value to use.</param>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="xmlPath">The xml path value to use.</param>
    /// <returns>The resulting diagnostic value.</returns>
    public PptxDiagnostic CreateError(string code, string message, string partName, string relationshipId, string xmlPath)
    {
        return PptxDiagnostic.CreateError(code, message, partName, relationshipId, xmlPath);
    }
}
