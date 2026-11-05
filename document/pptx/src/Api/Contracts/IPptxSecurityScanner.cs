using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxSecurityScanner"/>.
/// </summary>
public interface IPptxSecurityScanner
{
    /// <summary>
    /// Scans the security scanner for potentially unsafe embedded or linked content.
    /// </summary>
    /// <param name="package">The package value to use.</param>
    /// <returns>The resulting result value.</returns>
    PptxResult Scan(PptxPackage package);
}
