using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains a parsed or resolved workbook reference and its validation diagnostics.
/// </summary>
/// <remarks>
/// A result can describe an A1 or R1C1 range, a defined name, a print name, or a table structured reference.
/// </remarks>
public class XlsxReferenceResolutionResult
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the reference was parsed or resolved.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the reference text supplied by the caller.
    /// </summary>
    public string OriginalText;
    /// <summary>
    /// Stores normalized reference text or the resolved A1 range.
    /// </summary>
    public string NormalizedText;
    /// <summary>
    /// Stores the external workbook qualifier without square brackets.
    /// </summary>
    public string WorkbookName;
    /// <summary>
    /// Indicates whether a workbook qualifier is present.
    /// </summary>
    public bool HasWorkbookName;
    /// <summary>
    /// Stores the first or only worksheet name.
    /// </summary>
    public string FirstSheetName;
    /// <summary>
    /// Stores the last worksheet name in a three-dimensional reference.
    /// </summary>
    public string LastSheetName;
    /// <summary>
    /// Indicates whether a worksheet qualifier is present.
    /// </summary>
    public bool HasSheetName;
    /// <summary>
    /// Indicates whether the reference spans multiple worksheets.
    /// </summary>
    public bool IsThreeDimensional;
    /// <summary>
    /// Stores the parsed or resolved A1 range.
    /// </summary>
    public XlsxA1RangeReferenceValue A1Range;
    /// <summary>
    /// Indicates whether an A1 range is available.
    /// </summary>
    public bool HasA1Range;
    /// <summary>
    /// Stores the first or only R1C1 reference.
    /// </summary>
    public XlsxR1C1ReferenceValue FirstR1C1;
    /// <summary>
    /// Stores the last R1C1 reference when a range is present.
    /// </summary>
    public XlsxR1C1ReferenceValue LastR1C1;
    /// <summary>
    /// Indicates whether parsed R1C1 data is available.
    /// </summary>
    public bool HasR1C1Reference;
    /// <summary>
    /// Indicates whether a second R1C1 endpoint is present.
    /// </summary>
    public bool HasLastR1C1;
    /// <summary>
    /// Stores the defined-name identifier.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the resolved defined name.
    /// </summary>
    public XlsxWorkbookDefinedName? DefinedName;
    /// <summary>
    /// Indicates whether a defined name was resolved.
    /// </summary>
    public bool HasDefinedName;
    /// <summary>
    /// Indicates whether the resolved name is the built-in print-area name.
    /// </summary>
    public bool IsPrintArea;
    /// <summary>
    /// Indicates whether the resolved name is the built-in print-titles name.
    /// </summary>
    public bool IsPrintTitles;
    /// <summary>
    /// Indicates whether the input is a table structured reference.
    /// </summary>
    public bool IsStructuredReference;
    /// <summary>
    /// Stores the table identifier from a structured reference.
    /// </summary>
    public string TableName;
    /// <summary>
    /// Stores the structured item selector such as <c>#Data</c> or <c>#Headers</c>.
    /// </summary>
    public string StructuredItem;
    /// <summary>
    /// Stores the first or only selected table column name.
    /// </summary>
    public string FirstColumnName;
    /// <summary>
    /// Stores the last selected table column name.
    /// </summary>
    public string LastColumnName;
    /// <summary>
    /// Stores the resolved table.
    /// </summary>
    public XlsxTablePart? Table;
    /// <summary>
    /// Indicates whether a table was resolved.
    /// </summary>
    public bool HasTable;
    /// <summary>
    /// Stores the resolved worksheet.
    /// </summary>
    public XlsxWorksheetPart? Worksheet;
    /// <summary>
    /// Indicates whether a worksheet was resolved.
    /// </summary>
    public bool HasWorksheet;
    /// <summary>
    /// Indicates whether the reference or name targets an external workbook.
    /// </summary>
    public bool IsExternal;

    /// <summary>
    /// Initializes an empty successful reference result.
    /// </summary>
    public XlsxReferenceResolutionResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        OriginalText = "";
        NormalizedText = "";
        WorkbookName = "";
        HasWorkbookName = false;
        FirstSheetName = "";
        LastSheetName = "";
        HasSheetName = false;
        IsThreeDimensional = false;
        A1Range = new XlsxA1RangeReferenceValue();
        HasA1Range = false;
        FirstR1C1 = new XlsxR1C1ReferenceValue();
        LastR1C1 = new XlsxR1C1ReferenceValue();
        HasR1C1Reference = false;
        HasLastR1C1 = false;
        Name = "";
        DefinedName = null;
        HasDefinedName = false;
        IsPrintArea = false;
        IsPrintTitles = false;
        IsStructuredReference = false;
        TableName = "";
        StructuredItem = "";
        FirstColumnName = "";
        LastColumnName = "";
        Table = null;
        HasTable = false;
        Worksheet = null;
        HasWorksheet = false;
        IsExternal = false;
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
