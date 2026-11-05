using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a worksheet pane operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorksheetPaneResult
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
    /// Stores the view value used by this model.
    /// </summary>
    public XlsxWorksheetView? View;
    /// <summary>
    /// Indicates whether view is present and should be written.
    /// </summary>
    public bool HasView;
    /// <summary>
    /// Stores the pane value used by this model.
    /// </summary>
    public XlsxWorksheetPane? Pane;
    /// <summary>
    /// Indicates whether pane is present and should be written.
    /// </summary>
    public bool HasPane;
    /// <summary>
    /// Stores the selection value used by this model.
    /// </summary>
    public XlsxWorksheetSelection? Selection;
    /// <summary>
    /// Indicates whether selection is present and should be written.
    /// </summary>
    public bool HasSelection;
    /// <summary>
    /// Stores the created view value used by this model.
    /// </summary>
    public bool CreatedView;
    /// <summary>
    /// Stores the cleared value used by this model.
    /// </summary>
    public bool Cleared;
    /// <summary>
    /// Initializes a new worksheet pane result with its default state.
    /// </summary>
    public XlsxWorksheetPaneResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        View = null;
        HasView = false;
        Pane = null;
        HasPane = false;
        Selection = null;
        HasSelection = false;
        CreatedView = false;
        Cleared = false;
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

    internal static void AddReferenceDiagnostics(XlsxWorksheetPaneResult result, XlsxA1CellReferenceResult referenceResult)
    {
        foreach (XlsxDiagnostic diagnostic in referenceResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
