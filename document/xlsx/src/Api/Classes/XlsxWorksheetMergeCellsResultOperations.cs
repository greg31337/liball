using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxWorksheetMergeCellsResult"/> data.
/// </summary>
public class XlsxWorksheetMergeCellsResultOperations : IXlsxWorksheetMergeCellsResultOperations
{
    /// <summary>
    /// Initializes a new xlsx worksheet merge cells result operations instance.
    /// </summary>
    public XlsxWorksheetMergeCellsResultOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetMergeCellsResult"/>.SetRange through the SetRange operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet merge cells result data to operate on.</param>
    /// <param name="firstRow">The first row value used by the operation.</param>
    /// <param name="firstColumn">The first column value used by the operation.</param>
    /// <param name="lastRow">The last row value used by the operation.</param>
    /// <param name="lastColumn">The last column value used by the operation.</param>
    public void SetRange(XlsxWorksheetMergeCellsResult data, int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        data.SetRange(firstRow, firstColumn, lastRow, lastColumn);
    }
}
