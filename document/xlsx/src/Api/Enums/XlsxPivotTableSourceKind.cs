using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists the supported pivot table source kind.
/// </summary>
public enum XlsxPivotTableSourceKind
{
    /// <summary>
    /// Selects range for pivot table source kind operations.
    /// </summary>
    Range = 0,
    /// <summary>
    /// Selects table for pivot table source kind operations.
    /// </summary>
    Table = 1
}
