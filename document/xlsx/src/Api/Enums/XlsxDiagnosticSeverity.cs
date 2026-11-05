using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists the supported diagnostic severity.
/// </summary>
public enum XlsxDiagnosticSeverity
{
    /// <summary>
    /// Identifies an informational diagnostic that does not invalidate the operation.
    /// </summary>
    Info = 0,
    /// <summary>
    /// Identifies a warning that preserves a valid result but may require caller attention.
    /// </summary>
    Warning = 1,
    /// <summary>
    /// Identifies an error that makes the containing validation result invalid.
    /// </summary>
    Error = 2
}
