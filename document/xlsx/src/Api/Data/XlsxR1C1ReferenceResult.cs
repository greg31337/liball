using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a r1 c1 reference operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxR1C1ReferenceResult
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
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public XlsxR1C1ReferenceValue Reference;
    /// <summary>
    /// Stores the row value used by this model.
    /// </summary>
    public int Row;
    /// <summary>
    /// Stores the column value used by this model.
    /// </summary>
    public int Column;
    /// <summary>
    /// Indicates whether row is present and should be written.
    /// </summary>
    public bool HasRow;
    /// <summary>
    /// Indicates whether column is present and should be written.
    /// </summary>
    public bool HasColumn;
    /// <summary>
    /// Indicates whether relative row.
    /// </summary>
    public bool IsRelativeRow;
    /// <summary>
    /// Indicates whether relative column.
    /// </summary>
    public bool IsRelativeColumn;
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
    /// Initializes a new r1 c1 reference result with its default state.
    /// </summary>
    public XlsxR1C1ReferenceResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Reference = new XlsxR1C1ReferenceValue();
        Row = 0;
        Column = 0;
        HasRow = false;
        HasColumn = false;
        IsRelativeRow = true;
        IsRelativeColumn = true;
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
