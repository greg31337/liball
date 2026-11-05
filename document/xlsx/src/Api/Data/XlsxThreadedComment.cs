using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents threaded comment data used by the public workbook API.
/// </summary>
public class XlsxThreadedComment
{
    /// <summary>
    /// Stores the id value used by this model.
    /// </summary>
    public string Id;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the person id value used by this model.
    /// </summary>
    public string PersonId;
    /// <summary>
    /// Stores the parent id value used by this model.
    /// </summary>
    public string ParentId;
    /// <summary>
    /// Stores the date time text value used by this model.
    /// </summary>
    public string DateTimeText;
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string TextRawXml;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new threaded comment with its default state.
    /// </summary>
    public XlsxThreadedComment()
    {
        Id = "";
        Reference = "";
        PersonId = "";
        ParentId = "";
        DateTimeText = "";
        Text = "";
        TextRawXml = "";
        RawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
