using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxFormulaParser"/>.
/// </summary>
public interface IXlsxFormulaParser
{
    /// <summary>
    /// Parses the supplied text into the corresponding typed model without using the current culture.
    /// </summary>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxFormulaParseResult Parse(string formula);
}
