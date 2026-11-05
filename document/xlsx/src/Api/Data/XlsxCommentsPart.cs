using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML comments part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxCommentsPart
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
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains authors.
    /// </summary>
    public List<string> Authors;
    /// <summary>
    /// Contains comments.
    /// </summary>
    public List<XlsxComment> Comments;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new comments part with its default state.
    /// </summary>
    public XlsxCommentsPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "/xl/comments/comment1.xml";
        NamespaceUri = XlsxNamespaces.Spreadsheet;
        RawXml = "";
        Authors = new List<string>();
        Comments = new List<XlsxComment>();
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Returns comment from the current model.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>The comment produced by the operation.</returns>
    internal XlsxComment? GetComment(string reference)
    {
        foreach (XlsxComment comment in Comments)
        {
            if (comment != null && comment.Reference == reference)
            {
                return comment;
            }
        }

        return null;
    }

    /// <summary>
    /// Performs the apply legacy drawing operation using the current model state.
    /// </summary>
    /// <param name = "drawingPart">The drawing part value used by the operation.</param>
    internal void ApplyLegacyDrawing(XlsxLegacyCommentDrawingPart drawingPart)
    {
        if (drawingPart == null)
        {
            return;
        }

        foreach (XlsxLegacyCommentDrawing drawing in drawingPart.Drawings)
        {
            if (drawing != null && drawing.Reference != null && drawing.Reference.Length > 0)
            {
                XlsxComment? comment = GetComment(drawing.Reference);
                if (comment != null)
                {
                    comment.Row = drawing.Row;
                    comment.HasRow = drawing.HasRow;
                    comment.Column = drawing.Column;
                    comment.HasColumn = drawing.HasColumn;
                    comment.Anchor = drawing.Anchor;
                    comment.Style = drawing.Style;
                    comment.Left = drawing.Left;
                    comment.Top = drawing.Top;
                    comment.Width = drawing.Width;
                    comment.Height = drawing.Height;
                    comment.Visible = drawing.Visible;
                    comment.HasVisible = drawing.HasVisible;
                    comment.MoveWithCells = drawing.MoveWithCells;
                    comment.SizeWithCells = drawing.SizeWithCells;
                    comment.HasPosition = true;
                }
            }
        }
    }
}
