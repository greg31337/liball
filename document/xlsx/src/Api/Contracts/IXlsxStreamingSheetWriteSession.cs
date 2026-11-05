using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines the public instance operations implemented by <see cref = "XlsxStreamingSheetWriteSession"/>.
/// </summary>
public interface IXlsxStreamingSheetWriteSession
{
    /// <summary>
    /// Validates and writes text row in the requested representation.
    /// </summary>
    /// <param name = "values">The values value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingSheetWriteResult WriteTextRow(List<string> values);
    /// <summary>
    /// Validates and writes row in the requested representation.
    /// </summary>
    /// <param name = "values">The values value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingSheetWriteResult WriteRow(List<XlsxCellValue> values);
    /// <summary>
    /// Completes or closes the streaming operation and releases the reader or writer owned by the session.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxStreamingSheetWriteResult Close();
}
