using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Contains the outcome of a typed drawing object authoring operation.
/// </summary>
public class XlsxDrawingObjectOperationResult
{
    /// <summary>Indicates whether the operation completed without errors.</summary>
    public bool IsValid;
    /// <summary>Contains validation diagnostics produced by the operation.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Stores the authored drawing anchor.</summary>
    public XlsxDrawingRawAnchor? Anchor;
    /// <summary>Indicates whether an anchor was produced.</summary>
    public bool HasAnchor;

    /// <summary>Initializes an empty successful result.</summary>
    public XlsxDrawingObjectOperationResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Anchor = null;
        HasAnchor = false;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    internal void AddError(string code, string message)
    {
        AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message));
    }
}
