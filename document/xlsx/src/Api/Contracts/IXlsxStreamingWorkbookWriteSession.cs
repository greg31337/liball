using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxStreamingWorkbookWriteSession"/>.
/// </summary>
public interface IXlsxStreamingWorkbookWriteSession
{
    /// <summary>
    /// Returns a snapshot of the worksheet metadata accumulated by the session.
    /// </summary>
    /// <returns>Independent worksheet metadata objects in workbook order.</returns>
    List<XlsxStreamingWorkbookSheetInfo> GetSheetInfos();
    /// <summary>
    /// Performs the begin sheet operation using the current model state.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingWorkbookWriteResult BeginSheet(string name);
    /// <summary>
    /// Validates and writes text row in the requested representation.
    /// </summary>
    /// <param name = "values">The values value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingWorkbookWriteResult WriteTextRow(List<string> values);
    /// <summary>
    /// Validates and writes row in the requested representation.
    /// </summary>
    /// <param name = "values">The values value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingWorkbookWriteResult WriteRow(List<XlsxCellValue> values);
    /// <summary>
    /// Performs the end sheet operation using the current model state.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingWorkbookWriteResult EndSheet();
    /// <summary>
    /// Completes or closes the streaming operation and releases the reader or writer owned by the session.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingWorkbookWriteResult Close();
}
