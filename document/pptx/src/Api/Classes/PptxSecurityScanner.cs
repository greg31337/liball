using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxSecurityScanner values.
/// </summary>
public class PptxSecurityScanner : IPptxSecurityScanner
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxSecurityScanner()
    {
    }

    /// <summary>
    /// Scans the security scanner for potentially unsafe embedded or linked content.
    /// </summary>
    /// <param name="package">The package value to use.</param>
    /// <returns>The resulting result value.</returns>
    public PptxResult Scan(PptxPackage package)
    {
        return PptxSecurityScannerCore.Scan(package);
    }
}
