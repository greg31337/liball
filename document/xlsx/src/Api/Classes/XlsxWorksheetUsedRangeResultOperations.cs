using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxWorksheetUsedRangeResult"/> data.
/// </summary>
public class XlsxWorksheetUsedRangeResultOperations : IXlsxWorksheetUsedRangeResultOperations
{
    /// <summary>
    /// Initializes a new xlsx worksheet used range result operations instance.
    /// </summary>
    public XlsxWorksheetUsedRangeResultOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetUsedRangeResult"/>.SetBounds through the SetBounds operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet used range result data to operate on.</param>
    /// <param name="rowIndex">The row index value used by the operation.</param>
    /// <param name="columnIndex">The column index value used by the operation.</param>
    public void SetBounds(XlsxWorksheetUsedRangeResult data, int rowIndex, int columnIndex)
    {
        data.SetBounds(rowIndex, columnIndex);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetUsedRangeResult"/>.UpdateRangeReference through the UpdateRangeReference operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet used range result data to operate on.</param>
    public void UpdateRangeReference(XlsxWorksheetUsedRangeResult data)
    {
        data.UpdateRangeReference();
    }
}
