using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxSpreadsheetColorResolver"/>.
/// </summary>
public interface IXlsxSpreadsheetColorResolver
{
    /// <summary>
    /// Performs the resolve operation using the current model state.
    /// </summary>
    /// <param name = "color">The color value used by the operation.</param>
    /// <param name = "theme">The theme value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxSpreadsheetColorResolutionResult Resolve(XlsxSpreadsheetColor color, XlsxThemePart theme);
}
