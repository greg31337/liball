using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxVbaProjectOperations"/>.
/// </summary>
public interface IPptxVbaProjectOperations
{
    /// <summary>
    /// Returns the bytes without changing the vba project.
    /// </summary>
    /// <returns>The serialized presentation data.</returns>
    /// <param name="target">The data value to operate on.</param>
    byte[] GetBytes(PptxVbaProject target);
    /// <summary>
    /// Replaces bytes while preserving unrelated presentation data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void ReplaceBytes(PptxVbaProject target, byte[] bytes);
    /// <summary>
    /// Scans the vba project for potentially unsafe embedded or linked content.
    /// </summary>
    /// <returns>The resulting result value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxResult ScanSecurity(PptxVbaProject target);
}
