using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxUriOperations"/>.
/// </summary>
public interface IPptxUriOperations
{
    /// <summary>
    /// Normalizes a package part name to an absolute, forward-slash-separated form.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting text.</returns>
    string NormalizePartName(string partName);
    /// <summary>
    /// Reports whether the uri is valid part name.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool IsValidPartName(string partName);
    /// <summary>
    /// Validates the uri and returns all diagnostics found during the check.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting result value.</returns>
    PptxResult ValidatePartName(string partName);
    /// <summary>
    /// Converts a normalized package part name to its ZIP entry name.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting text.</returns>
    string ToZipEntryName(string partName);
    /// <summary>
    /// Reports whether the uri is external target.
    /// </summary>
    /// <param name="target">The relationship target path or external URI.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool IsExternalTarget(string target);
    /// <summary>
    /// Reports whether the uri is valid internal relationship target.
    /// </summary>
    /// <param name="sourcePartName">The source part name value to use.</param>
    /// <param name="target">The relationship target path or external URI.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool IsValidInternalRelationshipTarget(string sourcePartName, string target);
    /// <summary>
    /// Validates the uri and returns all diagnostics found during the check.
    /// </summary>
    /// <param name="sourcePartName">The source part name value to use.</param>
    /// <param name="target">The relationship target path or external URI.</param>
    /// <returns>The resulting result value.</returns>
    PptxResult ValidateInternalRelationshipTarget(string sourcePartName, string target);
    /// <summary>
    /// Builds the relative relationship target needed to reference an internal package part.
    /// </summary>
    /// <param name="sourcePartName">The source part name value to use.</param>
    /// <param name="target">The relationship target path or external URI.</param>
    /// <returns>The resulting text.</returns>
    string BuildInternalRelationshipTarget(string sourcePartName, string target);
}
