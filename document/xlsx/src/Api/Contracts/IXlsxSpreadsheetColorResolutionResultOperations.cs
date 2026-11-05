using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxSpreadsheetColorResolutionResult"/> data.
/// </summary>
public interface IXlsxSpreadsheetColorResolutionResultOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxSpreadsheetColorResolutionResult"/>.AddError through the AddError operation.
    /// </summary>
    /// <param name="data">The xlsx spreadsheet color resolution result data to operate on.</param>
    /// <param name="code">The code value used by the operation.</param>
    /// <param name="message">The message value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxDiagnostic AddError(XlsxSpreadsheetColorResolutionResult data, string code, string message);
}
