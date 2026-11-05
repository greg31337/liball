using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a date serial conversion operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxDateSerialConversionResult
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
    /// Stores the date time value value used by this model.
    /// </summary>
    public System.DateTime DateTimeValue;
    /// <summary>
    /// Stores the serial value value used by this model.
    /// </summary>
    public double SerialValue;
    /// <summary>
    /// Indicates whether 1904 date system.
    /// </summary>
    public bool Is1904DateSystem;
    /// <summary>
    /// Indicates whether excel1900 leap day.
    /// </summary>
    public bool IsExcel1900LeapDay;
    /// <summary>
    /// Indicates whether time fraction is present and should be written.
    /// </summary>
    public bool HasTimeFraction;
    /// <summary>
    /// Initializes a new date serial conversion result with its default state.
    /// </summary>
    public XlsxDateSerialConversionResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        DateTimeValue = default(System.DateTime);
        SerialValue = 0;
        Is1904DateSystem = false;
        IsExcel1900LeapDay = false;
        HasTimeFraction = false;
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
