using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxWorkbookRepairResult"/> data.
/// </summary>
public interface IXlsxWorkbookRepairResultOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxWorkbookRepairResult"/>.AddRepair through the AddRepair operation.
    /// </summary>
    /// <param name="data">The xlsx workbook repair result data to operate on.</param>
    /// <param name="code">The code value used by the operation.</param>
    /// <param name="message">The message value used by the operation.</param>
    void AddRepair(XlsxWorkbookRepairResult data, string code, string message);
}
