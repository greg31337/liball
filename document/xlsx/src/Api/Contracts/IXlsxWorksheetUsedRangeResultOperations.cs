using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxWorksheetUsedRangeResult"/> data.
/// </summary>
public interface IXlsxWorksheetUsedRangeResultOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxWorksheetUsedRangeResult"/>.SetBounds through the SetBounds operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet used range result data to operate on.</param>
    /// <param name="rowIndex">The row index value used by the operation.</param>
    /// <param name="columnIndex">The column index value used by the operation.</param>
    void SetBounds(XlsxWorksheetUsedRangeResult data, int rowIndex, int columnIndex);
    /// <summary>
    /// Invokes <see cref="XlsxWorksheetUsedRangeResult"/>.UpdateRangeReference through the UpdateRangeReference operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet used range result data to operate on.</param>
    void UpdateRangeReference(XlsxWorksheetUsedRangeResult data);
}
