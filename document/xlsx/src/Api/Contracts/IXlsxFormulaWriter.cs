using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxFormulaWriter"/>.
/// </summary>
public interface IXlsxFormulaWriter
{
    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxFormulaWriteResult WriteString(string formula);
    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "parseResult">The parse result value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxFormulaWriteResult WriteParseResult(XlsxFormulaParseResult parseResult);
}
