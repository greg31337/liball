using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents custom property data used by the public workbook API.
/// </summary>
public class XlsxCustomProperty
{
    /// <summary>
    /// Stores the format id value used by this model.
    /// </summary>
    public string? FormatId;
    /// <summary>
    /// Stores the property id value used by this model.
    /// </summary>
    public int PropertyId;
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string? Name;
    /// <summary>
    /// Stores the link target value used by this model.
    /// </summary>
    public string? LinkTarget;
    /// <summary>
    /// Stores the value type value used by this model.
    /// </summary>
    public string ValueType;
    /// <summary>
    /// Stores the text value value used by this model.
    /// </summary>
    public string TextValue;
    /// <summary>
    /// Indicates whether text value is present and should be written.
    /// </summary>
    public bool HasTextValue;
    /// <summary>
    /// Stores the int value value used by this model.
    /// </summary>
    public int IntValue;
    /// <summary>
    /// Indicates whether int value is present and should be written.
    /// </summary>
    public bool HasIntValue;
    /// <summary>
    /// Stores the double value value used by this model.
    /// </summary>
    public double DoubleValue;
    /// <summary>
    /// Indicates whether double value is present and should be written.
    /// </summary>
    public bool HasDoubleValue;
    /// <summary>
    /// Stores the bool value value used by this model.
    /// </summary>
    public bool BoolValue;
    /// <summary>
    /// Indicates whether bool value is present and should be written.
    /// </summary>
    public bool HasBoolValue;
    /// <summary>
    /// Stores the date time value value used by this model.
    /// </summary>
    public System.DateTime DateTimeValue;
    /// <summary>
    /// Indicates whether date time value is present and should be written.
    /// </summary>
    public bool HasDateTimeValue;
    /// <summary>
    /// Stores the raw value xml value used by this model.
    /// </summary>
    public string RawValueXml;
    /// <summary>
    /// Indicates whether raw value xml is present and should be written.
    /// </summary>
    public bool HasRawValueXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new custom property with its default state.
    /// </summary>
    public XlsxCustomProperty()
    {
        FormatId = "";
        PropertyId = 0;
        Name = "";
        LinkTarget = "";
        ValueType = "";
        TextValue = "";
        HasTextValue = false;
        IntValue = 0;
        HasIntValue = false;
        DoubleValue = 0;
        HasDoubleValue = false;
        BoolValue = false;
        HasBoolValue = false;
        DateTimeValue = default(System.DateTime);
        HasDateTimeValue = false;
        RawValueXml = "";
        HasRawValueXml = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
