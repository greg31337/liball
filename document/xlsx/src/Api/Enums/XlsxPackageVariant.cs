using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Lists the supported package variant.
/// </summary>
public enum XlsxPackageVariant
{
    /// <summary>
    /// Selects the standard macro-free workbook package format.
    /// </summary>
    Xlsx = 0,
    /// <summary>
    /// Selects the macro-enabled workbook package format.
    /// </summary>
    Xlsm = 1,
    /// <summary>
    /// Selects the macro-free workbook template package format.
    /// </summary>
    Xltx = 2,
    /// <summary>
    /// Selects the macro-enabled workbook template package format.
    /// </summary>
    Xltm = 3
}
