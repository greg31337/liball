using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxSpreadsheetColorResolutionResult"/> data.
/// </summary>
public class XlsxSpreadsheetColorResolutionResultOperations : IXlsxSpreadsheetColorResolutionResultOperations
{
    /// <summary>
    /// Initializes a new xlsx spreadsheet color resolution result operations instance.
    /// </summary>
    public XlsxSpreadsheetColorResolutionResultOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxSpreadsheetColorResolutionResult"/>.AddError through the AddError operation.
    /// </summary>
    /// <param name="data">The xlsx spreadsheet color resolution result data to operate on.</param>
    /// <param name="code">The code value used by the operation.</param>
    /// <param name="message">The message value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxDiagnostic AddError(XlsxSpreadsheetColorResolutionResult data, string code, string message)
    {
        return data.AddError(code, message);
    }
}
