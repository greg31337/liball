using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a xml value parse operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxXmlValueParseResult
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
    /// Stores the boolean value value used by this model.
    /// </summary>
    public bool BooleanValue;
    /// <summary>
    /// Stores the int value value used by this model.
    /// </summary>
    public int IntValue;
    /// <summary>
    /// Stores the unsigned int value value used by this model.
    /// </summary>
    public uint UnsignedIntValue;
    /// <summary>
    /// Stores the double value value used by this model.
    /// </summary>
    public double DoubleValue;
    /// <summary>
    /// Stores the date time value value used by this model.
    /// </summary>
    public System.DateTime DateTimeValue;
    /// <summary>
    /// Stores the color value value used by this model.
    /// </summary>
    public XlsxSpreadsheetColor ColorValue;
    /// <summary>
    /// Stores the cell reference value value used by this model.
    /// </summary>
    public XlsxCellReferenceValue CellReferenceValue;
    /// <summary>
    /// Stores the text value value used by this model.
    /// </summary>
    public string TextValue;
    /// <summary>
    /// Initializes a new xml value parse result with its default state.
    /// </summary>
    public XlsxXmlValueParseResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        BooleanValue = false;
        IntValue = 0;
        UnsignedIntValue = 0;
        DoubleValue = 0;
        DateTimeValue = default(System.DateTime);
        ColorValue = new XlsxSpreadsheetColor();
        CellReferenceValue = new XlsxCellReferenceValue();
        TextValue = "";
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
