using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxWorkbookRelationshipResolver"/>.
/// </summary>
public interface IXlsxWorkbookRelationshipResolver
{
    /// <summary>
    /// Performs the resolve operation using the current model state.
    /// </summary>
    /// <param name = "workbook">The workbook model used by the operation.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxWorkbookRelationshipResolutionResult Resolve(XlsxWorkbookPart workbook, XlsxRelationshipsPart relationships);
}
