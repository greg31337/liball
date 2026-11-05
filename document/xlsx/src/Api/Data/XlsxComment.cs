using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents comment data used by the public workbook API.
/// </summary>
public class XlsxComment
{
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string? Reference;
    /// <summary>
    /// Stores the author id value used by this model.
    /// </summary>
    public uint AuthorId;
    /// <summary>
    /// Indicates whether author id is present and should be written.
    /// </summary>
    public bool HasAuthorId;
    /// <summary>
    /// Stores the author value used by this model.
    /// </summary>
    public string Author;
    /// <summary>
    /// Stores the shape id value used by this model.
    /// </summary>
    public uint ShapeId;
    /// <summary>
    /// Indicates whether shape id is present and should be written.
    /// </summary>
    public bool HasShapeId;
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string TextRawXml;
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
    /// Indicates whether position is present and should be written.
    /// </summary>
    public bool HasPosition;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new comment with its default state.
    /// </summary>
    public XlsxComment()
    {
        Reference = "";
        AuthorId = 0;
        HasAuthorId = false;
        Author = "";
        ShapeId = 0;
        HasShapeId = false;
        Text = "";
        TextRawXml = "";
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
        HasPosition = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
