using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxWorkbookSanitizeResult"/> data.
/// </summary>
public class XlsxWorkbookSanitizeResultOperations : IXlsxWorkbookSanitizeResultOperations
{
    /// <summary>
    /// Initializes a new xlsx workbook sanitize result operations instance.
    /// </summary>
    public XlsxWorkbookSanitizeResultOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorkbookSanitizeResult"/>.AddWarning through the AddWarning operation.
    /// </summary>
    /// <param name="data">The xlsx workbook sanitize result data to operate on.</param>
    /// <param name="code">The code value used by the operation.</param>
    /// <param name="message">The message value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDiagnostic AddWarning(XlsxWorkbookSanitizeResult data, string code, string message)
    {
        return data.AddWarning(code, message);
    }
}
