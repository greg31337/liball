using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines workbook model repair that can be replaced by applications and tests.
/// </summary>
/// <remarks>
/// Implementations should describe applied repairs and expected failures through the returned result.
/// </remarks>
public interface IXlsxWorkbookRepairer
{
    /// <summary>
    /// Repairs a workbook model using the supplied repair options.
    /// </summary>
    /// <param name = "workbook">The workbook model used by the operation.</param>
    /// <param name = "options">The options that control repair behavior.</param>
    /// <returns>A result containing the repaired workbook, repairs, validity flag, and diagnostics.</returns>
    XlsxWorkbookRepairResult RepairWorkbookWithOptions(XlsxWorkbook workbook, XlsxWorkbookRepairOptions options);
}
