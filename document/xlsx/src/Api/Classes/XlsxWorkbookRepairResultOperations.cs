using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxWorkbookRepairResult"/> data.
/// </summary>
public class XlsxWorkbookRepairResultOperations : IXlsxWorkbookRepairResultOperations
{
    /// <summary>
    /// Initializes a new xlsx workbook repair result operations instance.
    /// </summary>
    public XlsxWorkbookRepairResultOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookRepairResult"/>.AddRepair through the AddRepair operation.
    /// </summary>
    /// <param name="data">The xlsx workbook repair result data to operate on.</param>
    /// <param name="code">The code value used by the operation.</param>
    /// <param name="message">The message value used by the operation.</param>
    public void AddRepair(XlsxWorkbookRepairResult data, string code, string message)
    {
        data.AddRepair(code, message);
    }
}
