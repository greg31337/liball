using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet header footer data used by the public workbook API.
/// </summary>
public class XlsxWorksheetHeaderFooter
{
    /// <summary>
    /// Stores the different odd even value used by this model.
    /// </summary>
    public bool DifferentOddEven;
    /// <summary>
    /// Indicates whether different odd even is present and should be written.
    /// </summary>
    public bool HasDifferentOddEven;
    /// <summary>
    /// Stores the different first value used by this model.
    /// </summary>
    public bool DifferentFirst;
    /// <summary>
    /// Indicates whether different first is present and should be written.
    /// </summary>
    public bool HasDifferentFirst;
    /// <summary>
    /// Stores the scale with doc value used by this model.
    /// </summary>
    public bool ScaleWithDoc;
    /// <summary>
    /// Indicates whether scale with doc is present and should be written.
    /// </summary>
    public bool HasScaleWithDoc;
    /// <summary>
    /// Contains align with margins.
    /// </summary>
    public bool AlignWithMargins;
    /// <summary>
    /// Indicates whether align with margins is present and should be written.
    /// </summary>
    public bool HasAlignWithMargins;
    /// <summary>
    /// Stores the odd header value used by this model.
    /// </summary>
    public string OddHeader;
    /// <summary>
    /// Stores the odd footer value used by this model.
    /// </summary>
    public string OddFooter;
    /// <summary>
    /// Stores the even header value used by this model.
    /// </summary>
    public string EvenHeader;
    /// <summary>
    /// Stores the even footer value used by this model.
    /// </summary>
    public string EvenFooter;
    /// <summary>
    /// Stores the first header value used by this model.
    /// </summary>
    public string FirstHeader;
    /// <summary>
    /// Stores the first footer value used by this model.
    /// </summary>
    public string FirstFooter;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new worksheet header footer with its default state.
    /// </summary>
    public XlsxWorksheetHeaderFooter()
    {
        DifferentOddEven = false;
        HasDifferentOddEven = false;
        DifferentFirst = false;
        HasDifferentFirst = false;
        ScaleWithDoc = false;
        HasScaleWithDoc = false;
        AlignWithMargins = false;
        HasAlignWithMargins = false;
        OddHeader = "";
        OddFooter = "";
        EvenHeader = "";
        EvenFooter = "";
        FirstHeader = "";
        FirstFooter = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
