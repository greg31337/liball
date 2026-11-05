using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a worksheet comment operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorksheetCommentResult
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
    /// Stores the cell value used by this model.
    /// </summary>
    public XlsxWorksheetCell? Cell;
    /// <summary>
    /// Indicates whether cell is present and should be written.
    /// </summary>
    public bool HasCell;
    /// <summary>
    /// Stores the created row value used by this model.
    /// </summary>
    public bool CreatedRow;
    /// <summary>
    /// Stores the created cell value used by this model.
    /// </summary>
    public bool CreatedCell;
    /// <summary>
    /// Stores the comments part value used by this model.
    /// </summary>
    public XlsxCommentsPart? CommentsPart;
    /// <summary>
    /// Indicates whether comments part is present and should be written.
    /// </summary>
    public bool HasCommentsPart;
    /// <summary>
    /// Stores the comment value used by this model.
    /// </summary>
    public XlsxComment? Comment;
    /// <summary>
    /// Indicates whether comment is present and should be written.
    /// </summary>
    public bool HasComment;
    /// <summary>
    /// Stores the drawing part value used by this model.
    /// </summary>
    public XlsxLegacyCommentDrawingPart? DrawingPart;
    /// <summary>
    /// Indicates whether drawing part is present and should be written.
    /// </summary>
    public bool HasDrawingPart;
    /// <summary>
    /// Stores the drawing value used by this model.
    /// </summary>
    public XlsxLegacyCommentDrawing? Drawing;
    /// <summary>
    /// Indicates whether drawing is present and should be written.
    /// </summary>
    public bool HasDrawing;
    /// <summary>
    /// Stores the created author value used by this model.
    /// </summary>
    public bool CreatedAuthor;
    /// <summary>
    /// Stores the created comment value used by this model.
    /// </summary>
    public bool CreatedComment;
    /// <summary>
    /// Stores the replaced comment value used by this model.
    /// </summary>
    public bool ReplacedComment;
    /// <summary>
    /// Stores the created drawing value used by this model.
    /// </summary>
    public bool CreatedDrawing;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the row value used by this model.
    /// </summary>
    public int Row;
    /// <summary>
    /// Stores the column value used by this model.
    /// </summary>
    public int Column;
    /// <summary>
    /// Stores the author id value used by this model.
    /// </summary>
    public uint AuthorId;
    /// <summary>
    /// Stores the shape id value used by this model.
    /// </summary>
    public uint ShapeId;
    /// <summary>
    /// Initializes a new worksheet comment result with its default state.
    /// </summary>
    public XlsxWorksheetCommentResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Cell = null;
        HasCell = false;
        CreatedRow = false;
        CreatedCell = false;
        CommentsPart = null;
        HasCommentsPart = false;
        Comment = null;
        HasComment = false;
        DrawingPart = null;
        HasDrawingPart = false;
        Drawing = null;
        HasDrawing = false;
        CreatedAuthor = false;
        CreatedComment = false;
        ReplacedComment = false;
        CreatedDrawing = false;
        Reference = "";
        Row = 0;
        Column = 0;
        AuthorId = 0;
        ShapeId = 0;
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

    internal static void AddReferenceDiagnostics(XlsxWorksheetCommentResult result, XlsxA1CellReferenceResult referenceResult)
    {
        foreach (XlsxDiagnostic diagnostic in referenceResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddCellAccessDiagnostics(XlsxWorksheetCommentResult result, XlsxWorksheetCellAccessResult cellResult)
    {
        foreach (XlsxDiagnostic diagnostic in cellResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
