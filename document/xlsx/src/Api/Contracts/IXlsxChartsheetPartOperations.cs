using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxChartsheetPart"/> data.
/// </summary>
public interface IXlsxChartsheetPartOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxChartsheetPart"/>.ResolveDrawingRelationship through the ResolveDrawingRelationship operation.
    /// </summary>
    /// <param name="data">The xlsx chartsheet part data to operate on.</param>
    /// <param name="relationships">The relationships value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxSheetDrawingRelationshipResult ResolveDrawingRelationship(XlsxChartsheetPart data, XlsxRelationshipsPart relationships);
}
