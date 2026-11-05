using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists the supported sheet type.
/// </summary>
public enum XlsxSheetType
{
    /// <summary>
    /// Selects unknown for sheet type operations.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Selects worksheet for sheet type operations.
    /// </summary>
    Worksheet = 1,
    /// <summary>
    /// Selects chartsheet for sheet type operations.
    /// </summary>
    Chartsheet = 2,
    /// <summary>
    /// Selects macrosheet for sheet type operations.
    /// </summary>
    Macrosheet = 3,
    /// <summary>
    /// Selects dialogsheet for sheet type operations.
    /// </summary>
    Dialogsheet = 4
}
