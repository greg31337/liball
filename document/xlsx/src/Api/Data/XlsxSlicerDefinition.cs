using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents slicer definition data used by the public workbook API.
/// </summary>
public class XlsxSlicerDefinition
{
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the cache name value used by this model.
    /// </summary>
    public string CacheName;
    /// <summary>
    /// Stores the caption value used by this model.
    /// </summary>
    public string Caption;
    /// <summary>
    /// Stores the number of column items processed by the operation.
    /// </summary>
    public uint ColumnCount;
    /// <summary>
    /// Indicates whether column count is present and should be written.
    /// </summary>
    public bool HasColumnCount;
    /// <summary>Stores the slicer style name.</summary>
    public string StyleName;
    /// <summary>Indicates whether the slicer caption is shown.</summary>
    public bool ShowCaption;
    /// <summary>Indicates whether the show-caption flag is present.</summary>
    public bool HasShowCaption;
    /// <summary>Indicates whether the slicer position is locked.</summary>
    public bool LockedPosition;
    /// <summary>Indicates whether the locked-position flag is present.</summary>
    public bool HasLockedPosition;
    /// <summary>Stores the slicer row height.</summary>
    public uint RowHeight;
    /// <summary>Indicates whether row height is present.</summary>
    public bool HasRowHeight;
    /// <summary>Stores the first displayed item index.</summary>
    public uint StartItem;
    /// <summary>Indicates whether the first displayed item is present.</summary>
    public bool HasStartItem;
    /// <summary>Contains unknown attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Contains preserved child XML not represented by typed fields.</summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new slicer definition with its default state.
    /// </summary>
    public XlsxSlicerDefinition()
    {
        Name = "";
        CacheName = "";
        Caption = "";
        ColumnCount = 0;
        HasColumnCount = false;
        StyleName = "";
        ShowCaption = true;
        HasShowCaption = false;
        LockedPosition = false;
        HasLockedPosition = false;
        RowHeight = 0;
        HasRowHeight = false;
        StartItem = 0;
        HasStartItem = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
        RawXml = "";
    }
}
