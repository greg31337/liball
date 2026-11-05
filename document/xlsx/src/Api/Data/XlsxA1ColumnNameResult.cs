using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a a1 column name operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxA1ColumnNameResult
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the column value used by this model.
    /// </summary>
    public int Column;
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the characters consumed value used by this model.
    /// </summary>
    public int CharactersConsumed;
    /// <summary>
    /// Stores the characters written value used by this model.
    /// </summary>
    public int CharactersWritten;
    /// <summary>
    /// Stores the buffer too small value used by this model.
    /// </summary>
    public bool BufferTooSmall;
    /// <summary>
    /// Initializes a new a1 column name result with its default state.
    /// </summary>
    public XlsxA1ColumnNameResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Column = 0;
        Text = "";
        CharactersConsumed = 0;
        CharactersWritten = 0;
        BufferTooSmall = false;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        AddDiagnostic(diagnostic);
        return diagnostic;
    }
}
