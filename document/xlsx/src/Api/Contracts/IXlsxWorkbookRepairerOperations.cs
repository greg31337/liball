using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines the additional public operations implemented by <see cref="XlsxWorkbookRepairer"/>.
/// </summary>
public interface IXlsxWorkbookRepairerOperations
{
    /// <summary>
    /// Repairs workbook bytes with default options.
    /// </summary>
    /// <param name="bytes">The complete OOXML package bytes to repair.</param>
    /// <returns>A result containing the repaired workbook, repairs, validity flag, and diagnostics.</returns>
    XlsxWorkbookRepairResult RepairBytes(byte[] bytes);
    /// <summary>
    /// Repairs workbook bytes using the supplied options.
    /// </summary>
    /// <param name="bytes">The complete OOXML package bytes to repair.</param>
    /// <param name="options">The options that control repair behavior.</param>
    /// <returns>A result containing the repaired workbook, repairs, validity flag, and diagnostics.</returns>
    XlsxWorkbookRepairResult RepairBytesWithOptions(byte[] bytes, XlsxWorkbookRepairOptions options);
    /// <summary>
    /// Repairs a workbook model with default options.
    /// </summary>
    /// <param name="workbook">The workbook model used by the operation.</param>
    /// <returns>A result containing the repaired workbook, repairs, validity flag, and diagnostics.</returns>
    XlsxWorkbookRepairResult RepairWorkbook(XlsxWorkbook workbook);
    /// <summary>
    /// Repairs a workbook model using its shared-string model and the supplied options.
    /// </summary>
    /// <param name="workbook">The workbook model used by the operation.</param>
    /// <param name="sharedStrings">The shared strings used by the operation.</param>
    /// <param name="options">The options that control repair behavior.</param>
    /// <returns>A result containing the repaired workbook, repairs, validity flag, and diagnostics.</returns>
    XlsxWorkbookRepairResult RepairWorkbookWithSharedStrings(XlsxWorkbook workbook, XlsxSharedStringTablePart? sharedStrings, XlsxWorkbookRepairOptions options);
}
