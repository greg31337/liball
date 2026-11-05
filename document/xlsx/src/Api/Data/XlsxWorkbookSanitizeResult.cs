using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a workbook sanitize operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorkbookSanitizeResult
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
    /// Stores the macros removed value used by this model.
    /// </summary>
    public int MacrosRemoved;
    /// <summary>
    /// Stores the external links removed value used by this model.
    /// </summary>
    public int ExternalLinksRemoved;
    /// <summary>
    /// Stores the comments removed value used by this model.
    /// </summary>
    public int CommentsRemoved;
    /// <summary>
    /// Stores the comment drawings removed value used by this model.
    /// </summary>
    public int CommentDrawingsRemoved;
    /// <summary>
    /// Stores the hidden sheets removed value used by this model.
    /// </summary>
    public int HiddenSheetsRemoved;
    /// <summary>
    /// Stores the hidden sheets unhidden value used by this model.
    /// </summary>
    public int HiddenSheetsUnhidden;
    /// <summary>
    /// Stores the custom xml parts removed value used by this model.
    /// </summary>
    public int CustomXmlPartsRemoved;
    /// <summary>
    /// Stores the personal metadata parts removed value used by this model.
    /// </summary>
    public int PersonalMetadataPartsRemoved;
    /// <summary>
    /// Stores the ole objects removed value used by this model.
    /// </summary>
    public int OleObjectsRemoved;
    /// <summary>
    /// Stores the unknown parts removed value used by this model.
    /// </summary>
    public int UnknownPartsRemoved;
    /// <summary>
    /// Stores the relationships removed value used by this model.
    /// </summary>
    public int RelationshipsRemoved;
    /// <summary>
    /// Initializes a new workbook sanitize result with its default state.
    /// </summary>
    public XlsxWorkbookSanitizeResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        MacrosRemoved = 0;
        ExternalLinksRemoved = 0;
        CommentsRemoved = 0;
        CommentDrawingsRemoved = 0;
        HiddenSheetsRemoved = 0;
        HiddenSheetsUnhidden = 0;
        CustomXmlPartsRemoved = 0;
        PersonalMetadataPartsRemoved = 0;
        OleObjectsRemoved = 0;
        UnknownPartsRemoved = 0;
        RelationshipsRemoved = 0;
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

    /// <summary>
    /// Adds warning to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "code">The diagnostic code, which should remain stable across releases.</param>
    /// <param name = "message">The human-readable diagnostic message.</param>
    /// <returns>The diagnostic produced by the operation.</returns>
    internal XlsxDiagnostic AddWarning(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Warning, code, message);
        AddDiagnostic(diagnostic);
        return diagnostic;
    }

    internal static void AddDiagnostics(XlsxWorkbookSanitizeResult result, XlsxValidationResult validation)
    {
        if (validation == null)
        {
            return;
        }

        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
