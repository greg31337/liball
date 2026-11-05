using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a spreadsheet color resolution operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxSpreadsheetColorResolutionResult
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
    /// Indicates whether color is present and should be written.
    /// </summary>
    public bool HasColor;
    /// <summary>
    /// Stores the argb value used by this model.
    /// </summary>
    public string Argb;
    /// <summary>
    /// Stores the rgb value used by this model.
    /// </summary>
    public string Rgb;
    /// <summary>
    /// Stores the alpha value used by this model.
    /// </summary>
    public int Alpha;
    /// <summary>
    /// Stores the red value used by this model.
    /// </summary>
    public int Red;
    /// <summary>
    /// Stores the green value used by this model.
    /// </summary>
    public int Green;
    /// <summary>
    /// Stores the blue value used by this model.
    /// </summary>
    public int Blue;
    /// <summary>
    /// Stores the source value used by this model.
    /// </summary>
    public string Source;
    /// <summary>
    /// Initializes a new spreadsheet color resolution result with its default state.
    /// </summary>
    public XlsxSpreadsheetColorResolutionResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        HasColor = false;
        Argb = "";
        Rgb = "";
        Alpha = 255;
        Red = 0;
        Green = 0;
        Blue = 0;
        Source = "";
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Creates and appends an error diagnostic, which marks this result invalid.
    /// </summary>
    /// <param name = "code">The stable diagnostic code.</param>
    /// <param name = "message">The human-readable explanation of the error.</param>
    /// <returns>The error diagnostic that was added.</returns>
    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        AddDiagnostic(diagnostic);
        return diagnostic;
    }
}
