using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents legacy comment drawing data used by the public workbook API.
/// </summary>
public class XlsxLegacyCommentDrawing
{
    /// <summary>
    /// Stores the shape id value used by this model.
    /// </summary>
    public string? ShapeId;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the row value used by this model.
    /// </summary>
    public int Row;
    /// <summary>
    /// Indicates whether row is present and should be written.
    /// </summary>
    public bool HasRow;
    /// <summary>
    /// Stores the column value used by this model.
    /// </summary>
    public int Column;
    /// <summary>
    /// Indicates whether column is present and should be written.
    /// </summary>
    public bool HasColumn;
    /// <summary>
    /// Stores the anchor value used by this model.
    /// </summary>
    public string Anchor;
    /// <summary>
    /// Stores the style value used by this model.
    /// </summary>
    public string? Style;
    /// <summary>
    /// Stores the left value used by this model.
    /// </summary>
    public string Left;
    /// <summary>
    /// Stores the top value used by this model.
    /// </summary>
    public string Top;
    /// <summary>
    /// Stores the width value used by this model.
    /// </summary>
    public string Width;
    /// <summary>
    /// Stores the height value used by this model.
    /// </summary>
    public string Height;
    /// <summary>
    /// Stores the visible value used by this model.
    /// </summary>
    public bool Visible;
    /// <summary>
    /// Indicates whether visible is present and should be written.
    /// </summary>
    public bool HasVisible;
    /// <summary>
    /// Contains move with cells.
    /// </summary>
    public bool MoveWithCells;
    /// <summary>
    /// Contains size with cells.
    /// </summary>
    public bool SizeWithCells;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new legacy comment drawing with its default state.
    /// </summary>
    public XlsxLegacyCommentDrawing()
    {
        ShapeId = "";
        Reference = "";
        Row = 0;
        HasRow = false;
        Column = 0;
        HasColumn = false;
        Anchor = "";
        Style = "";
        Left = "";
        Top = "";
        Width = "";
        Height = "";
        Visible = false;
        HasVisible = false;
        MoveWithCells = false;
        SizeWithCells = false;
        RawXml = "";
        UnknownXml = new List<string>();
    }
}
