using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents pivot table data field data used by the public workbook API.
/// </summary>
public class XlsxPivotTableDataField
{
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the field index value used by this model.
    /// </summary>
    public int FieldIndex;
    /// <summary>
    /// Indicates whether field index is present and should be written.
    /// </summary>
    public bool HasFieldIndex;
    /// <summary>
    /// Stores the subtotal value used by this model.
    /// </summary>
    public string Subtotal;
    /// <summary>
    /// Stores the typed summary function.
    /// </summary>
    public XlsxPivotSummaryFunction SummaryFunction;
    /// <summary>
    /// Indicates whether a typed summary function is present.
    /// </summary>
    public bool HasSummaryFunction;
    /// <summary>
    /// Stores the base field value used by this model.
    /// </summary>
    public int BaseField;
    /// <summary>
    /// Indicates whether base field is present and should be written.
    /// </summary>
    public bool HasBaseField;
    /// <summary>
    /// Stores the base item value used by this model.
    /// </summary>
    public int BaseItem;
    /// <summary>
    /// Indicates whether base item is present and should be written.
    /// </summary>
    public bool HasBaseItem;
    /// <summary>
    /// Stores the number format id value used by this model.
    /// </summary>
    public uint NumberFormatId;
    /// <summary>
    /// Indicates whether number format id is present and should be written.
    /// </summary>
    public bool HasNumberFormatId;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new pivot table data field with its default state.
    /// </summary>
    public XlsxPivotTableDataField()
    {
        Name = "";
        FieldIndex = 0;
        HasFieldIndex = false;
        Subtotal = "";
        SummaryFunction = XlsxPivotSummaryFunction.Sum;
        HasSummaryFunction = false;
        BaseField = 0;
        HasBaseField = false;
        BaseItem = 0;
        HasBaseItem = false;
        NumberFormatId = 0;
        HasNumberFormatId = false;
        RawXml = "";
    }
}
