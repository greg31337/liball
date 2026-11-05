using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines the complete public operations implemented by <see cref="XlsxWorkbookValidator"/>.
/// </summary>
public interface IXlsxWorkbookValidatorOperations
{
    /// <summary>
    /// Checks bytes and returns diagnostics rather than throwing for expected invalid input.
    /// </summary>
    /// <param name="bytes">The complete OOXML package bytes to read.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxValidationResult ValidateBytes(byte[] bytes);
    /// <summary>
    /// Checks bytes using the supplied validation options.
    /// </summary>
    /// <param name="bytes">The complete OOXML package bytes to read.</param>
    /// <param name="options">The options that control validation behavior and limits.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    XlsxValidationResult ValidateBytesWithOptions(byte[] bytes, XlsxWorkbookValidationOptions options);
    /// <summary>
    /// Checks a workbook model with default options.
    /// </summary>
    /// <param name="workbook">The workbook model used by the operation.</param>
    /// <returns>A result containing the validity flag and diagnostics.</returns>
    XlsxValidationResult ValidateWorkbook(XlsxWorkbook workbook);
}
