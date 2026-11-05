using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents auto filter column data used by the public workbook API.
/// </summary>
public class XlsxAutoFilterColumn
{
    /// <summary>
    /// Stores the column id value used by this model.
    /// </summary>
    public uint ColumnId;
    /// <summary>
    /// Indicates whether column id is present and should be written.
    /// </summary>
    public bool HasColumnId;
    /// <summary>
    /// Stores the hidden button value used by this model.
    /// </summary>
    public bool HiddenButton;
    /// <summary>
    /// Indicates whether hidden button is present and should be written.
    /// </summary>
    public bool HasHiddenButton;
    /// <summary>
    /// Stores the show button value used by this model.
    /// </summary>
    public bool ShowButton;
    /// <summary>
    /// Indicates whether show button is present and should be written.
    /// </summary>
    public bool HasShowButton;
    /// <summary>
    /// Contains values.
    /// </summary>
    public XlsxAutoFilterValues Values;
    /// <summary>
    /// Indicates whether values is present and should be written.
    /// </summary>
    public bool HasValues;
    /// <summary>
    /// Contains custom filters.
    /// </summary>
    public XlsxCustomFilters CustomFilters;
    /// <summary>
    /// Indicates whether custom filters is present and should be written.
    /// </summary>
    public bool HasCustomFilters;
    /// <summary>
    /// Stores the dynamic filter value used by this model.
    /// </summary>
    public XlsxDynamicFilter DynamicFilter;
    /// <summary>
    /// Indicates whether dynamic filter is present and should be written.
    /// </summary>
    public bool HasDynamicFilter;
    /// <summary>
    /// Stores the top10 value used by this model.
    /// </summary>
    public XlsxTop10Filter Top10;
    /// <summary>
    /// Indicates whether top10 is present and should be written.
    /// </summary>
    public bool HasTop10;
    /// <summary>
    /// Stores the color filter value used by this model.
    /// </summary>
    public XlsxColorFilter ColorFilter;
    /// <summary>
    /// Indicates whether color filter is present and should be written.
    /// </summary>
    public bool HasColorFilter;
    /// <summary>
    /// Stores the icon filter value used by this model.
    /// </summary>
    public XlsxIconFilter IconFilter;
    /// <summary>
    /// Indicates whether icon filter is present and should be written.
    /// </summary>
    public bool HasIconFilter;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string ExtensionListRawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new auto filter column with its default state.
    /// </summary>
    public XlsxAutoFilterColumn()
    {
        ColumnId = 0;
        HasColumnId = false;
        HiddenButton = false;
        HasHiddenButton = false;
        ShowButton = false;
        HasShowButton = false;
        Values = new XlsxAutoFilterValues();
        HasValues = false;
        CustomFilters = new XlsxCustomFilters();
        HasCustomFilters = false;
        DynamicFilter = new XlsxDynamicFilter();
        HasDynamicFilter = false;
        Top10 = new XlsxTop10Filter();
        HasTop10 = false;
        ColorFilter = new XlsxColorFilter();
        HasColorFilter = false;
        IconFilter = new XlsxIconFilter();
        HasIconFilter = false;
        ExtensionListRawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
