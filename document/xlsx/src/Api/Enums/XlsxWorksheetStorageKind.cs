using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists the supported worksheet storage kind.
/// </summary>
public enum XlsxWorksheetStorageKind
{
    /// <summary>
    /// Selects unknown for worksheet storage kind operations.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Selects sparse for worksheet storage kind operations.
    /// </summary>
    Sparse = 1,
    /// <summary>
    /// Selects dense for worksheet storage kind operations.
    /// </summary>
    Dense = 2,
    /// <summary>
    /// Selects streaming for worksheet storage kind operations.
    /// </summary>
    Streaming = 3
}
