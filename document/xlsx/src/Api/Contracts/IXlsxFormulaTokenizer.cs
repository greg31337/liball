using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxFormulaTokenizer"/>.
/// </summary>
public interface IXlsxFormulaTokenizer
{
    /// <summary>
    /// Converts kenize to the requested OOXML-compatible representation.
    /// </summary>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxFormulaTokenizeResult Tokenize(string? formula);
}
