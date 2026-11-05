using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Identifies the SpreadsheetML conformance family declared by an OOXML workbook package.
/// </summary>
public enum XlsxPackageConformance
{
    /// <summary>
    /// Indicates that conformance could not be determined from the workbook part.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Indicates the ECMA-376 Transitional namespace family.
    /// </summary>
    Transitional = 1,
    /// <summary>
    /// Indicates the ISO/IEC 29500 Strict namespace family.
    /// </summary>
    Strict = 2
}
