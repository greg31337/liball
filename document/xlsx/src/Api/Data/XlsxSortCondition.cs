using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents sort condition data used by the public workbook API.
/// </summary>
public class XlsxSortCondition
{
    /// <summary>
    /// Stores the descending value used by this model.
    /// </summary>
    public bool Descending;
    /// <summary>
    /// Indicates whether descending is present and should be written.
    /// </summary>
    public bool HasDescending;
    /// <summary>
    /// Stores the sort by value used by this model.
    /// </summary>
    public string SortBy;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the custom list value used by this model.
    /// </summary>
    public string CustomList;
    /// <summary>
    /// Stores the dxf id value used by this model.
    /// </summary>
    public uint DxfId;
    /// <summary>
    /// Indicates whether dxf id is present and should be written.
    /// </summary>
    public bool HasDxfId;
    /// <summary>
    /// Stores the icon set value used by this model.
    /// </summary>
    public string IconSet;
    /// <summary>
    /// Stores the icon id value used by this model.
    /// </summary>
    public uint IconId;
    /// <summary>
    /// Indicates whether icon id is present and should be written.
    /// </summary>
    public bool HasIconId;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new sort condition with its default state.
    /// </summary>
    public XlsxSortCondition()
    {
        Descending = false;
        HasDescending = false;
        SortBy = "";
        Reference = "";
        CustomList = "";
        DxfId = 0;
        HasDxfId = false;
        IconSet = "";
        IconId = 0;
        HasIconId = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
