using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines workbook model validation that can be replaced by applications and tests.
/// </summary>
/// <remarks>
/// Implementations should report expected validation failures through the returned diagnostics rather than throwing.
/// </remarks>
public interface IXlsxWorkbookValidator
{
    /// <summary>
    /// Checks a workbook model using the supplied validation options.
    /// </summary>
    /// <param name = "workbook">The workbook model used by the operation.</param>
    /// <param name = "options">The options that control validation behavior and limits.</param>
    /// <returns>A result containing the validity flag and diagnostics.</returns>
    XlsxValidationResult ValidateWorkbookWithOptions(XlsxWorkbook workbook, XlsxWorkbookValidationOptions options);
}
