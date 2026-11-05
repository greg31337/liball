using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxPackageCleanupResult values.
/// </summary>
public class PptxPackageCleanupResultOperations : IPptxPackageCleanupResultOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxPackageCleanupResultOperations()
    {
    }

    /// <summary>
    /// Returns <c>true</c> when the operation has no error diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Succeeded(PptxPackageCleanupResult target)
    {
        return target.Succeeded();
    }

    /// <summary>
    /// Returns <c>true</c> when the operation contains at least one error diagnostic.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Failed(PptxPackageCleanupResult target)
    {
        return target.Failed();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ChangeCount(PptxPackageCleanupResult target)
    {
        return target.ChangeCount();
    }
}
