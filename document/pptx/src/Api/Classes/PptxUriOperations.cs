using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxUri values.
/// </summary>
public class PptxUriOperations : IPptxUriOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxUriOperations()
    {
    }

    /// <summary>
    /// Normalizes a package part name to an absolute, forward-slash-separated form.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting text.</returns>
    public string NormalizePartName(string partName)
    {
        return PptxUriCore.NormalizePartName(partName);
    }

    /// <summary>
    /// Reports whether the uri is valid part name.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool IsValidPartName(string partName)
    {
        return PptxUriCore.IsValidPartName(partName);
    }

    /// <summary>
    /// Validates the uri and returns all diagnostics found during the check.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting result value.</returns>
    public PptxResult ValidatePartName(string partName)
    {
        return PptxUriCore.ValidatePartName(partName);
    }

    /// <summary>
    /// Converts a normalized package part name to its ZIP entry name.
    /// </summary>
    /// <param name="partName">The normalized package part name, beginning with <c>/</c>.</param>
    /// <returns>The resulting text.</returns>
    public string ToZipEntryName(string partName)
    {
        return PptxUriCore.ToZipEntryName(partName);
    }

    /// <summary>
    /// Reports whether the uri is external target.
    /// </summary>
    /// <param name="target">The relationship target path or external URI.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool IsExternalTarget(string target)
    {
        return PptxUriCore.IsExternalTarget(target);
    }

    /// <summary>
    /// Reports whether the uri is valid internal relationship target.
    /// </summary>
    /// <param name="sourcePartName">The source part name value to use.</param>
    /// <param name="target">The relationship target path or external URI.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool IsValidInternalRelationshipTarget(string sourcePartName, string target)
    {
        return PptxUriCore.IsValidInternalRelationshipTarget(sourcePartName, target);
    }

    /// <summary>
    /// Validates the uri and returns all diagnostics found during the check.
    /// </summary>
    /// <param name="sourcePartName">The source part name value to use.</param>
    /// <param name="target">The relationship target path or external URI.</param>
    /// <returns>The resulting result value.</returns>
    public PptxResult ValidateInternalRelationshipTarget(string sourcePartName, string target)
    {
        return PptxUriCore.ValidateInternalRelationshipTarget(sourcePartName, target);
    }

    /// <summary>
    /// Builds the relative relationship target needed to reference an internal package part.
    /// </summary>
    /// <param name="sourcePartName">The source part name value to use.</param>
    /// <param name="target">The relationship target path or external URI.</param>
    /// <returns>The resulting text.</returns>
    public string BuildInternalRelationshipTarget(string sourcePartName, string target)
    {
        return PptxUriCore.BuildInternalRelationshipTarget(sourcePartName, target);
    }
}
