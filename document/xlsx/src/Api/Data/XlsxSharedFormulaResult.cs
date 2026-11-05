using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a shared formula operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxSharedFormulaResult
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
    /// Stores the number of expanded formula items processed by the operation.
    /// </summary>
    public int ExpandedFormulaCount;
    /// <summary>
    /// Stores the number of compacted group items processed by the operation.
    /// </summary>
    public int CompactedGroupCount;
    /// <summary>
    /// Stores the number of compacted follower items processed by the operation.
    /// </summary>
    public int CompactedFollowerCount;
    /// <summary>
    /// Initializes a new shared formula result with its default state.
    /// </summary>
    public XlsxSharedFormulaResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        ExpandedFormulaCount = 0;
        CompactedGroupCount = 0;
        CompactedFollowerCount = 0;
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
