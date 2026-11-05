using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxVbaProject values.
/// </summary>
public class PptxVbaProjectOperations : IPptxVbaProjectOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxVbaProjectOperations()
    {
    }

    /// <summary>
    /// Returns the bytes without changing the vba project.
    /// </summary>
    /// <returns>The serialized presentation data.</returns>
    /// <param name="target">The data value to operate on.</param>
    public byte[] GetBytes(PptxVbaProject target)
    {
        return target.GetBytes();
    }

    /// <summary>
    /// Replaces bytes while preserving unrelated presentation data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void ReplaceBytes(PptxVbaProject target, byte[] bytes)
    {
        target.ReplaceBytes(bytes);
    }

    /// <summary>
    /// Scans the vba project for potentially unsafe embedded or linked content.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxResult ScanSecurity(PptxVbaProject target)
    {
        return target.ScanSecurity();
    }
}
