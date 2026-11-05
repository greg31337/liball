using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents pivot table field data used by the public workbook API.
/// </summary>
public class XlsxPivotTableField
{
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Contains axis.
    /// </summary>
    public string Axis;
    /// <summary>
    /// Stores the sort type value used by this model.
    /// </summary>
    public string SortType;
    /// <summary>
    /// Stores the data field value used by this model.
    /// </summary>
    public bool DataField;
    /// <summary>
    /// Indicates whether data field is present and should be written.
    /// </summary>
    public bool HasDataField;
    /// <summary>
    /// Stores the default subtotal value used by this model.
    /// </summary>
    public bool DefaultSubtotal;
    /// <summary>
    /// Indicates whether default subtotal is present and should be written.
    /// </summary>
    public bool HasDefaultSubtotal;
    /// <summary>
    /// Stores the show all value used by this model.
    /// </summary>
    public bool ShowAll;
    /// <summary>
    /// Indicates whether show all is present and should be written.
    /// </summary>
    public bool HasShowAll;
    /// <summary>
    /// Stores the number of items items processed by the operation.
    /// </summary>
    public uint ItemsCount;
    /// <summary>
    /// Indicates whether items count is present and should be written.
    /// </summary>
    public bool HasItemsCount;
    /// <summary>
    /// Contains items.
    /// </summary>
    public List<XlsxPivotTableItem> Items;
    /// <summary>
    /// Contains group items.
    /// </summary>
    public List<XlsxPivotTableItem> GroupItems;
    /// <summary>
    /// Indicates whether grouping is present and should be written.
    /// </summary>
    public bool HasGrouping;
    /// <summary>
    /// Contains calculated items associated with this field.
    /// </summary>
    public List<XlsxPivotTableCalculatedField> CalculatedItems;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new pivot table field with its default state.
    /// </summary>
    public XlsxPivotTableField()
    {
        Name = "";
        Axis = "";
        SortType = "";
        DataField = false;
        HasDataField = false;
        DefaultSubtotal = false;
        HasDefaultSubtotal = false;
        ShowAll = false;
        HasShowAll = false;
        ItemsCount = 0;
        HasItemsCount = false;
        Items = new List<XlsxPivotTableItem>();
        GroupItems = new List<XlsxPivotTableItem>();
        HasGrouping = false;
        CalculatedItems = new List<XlsxPivotTableCalculatedField>();
        RawXml = "";
    }
}
