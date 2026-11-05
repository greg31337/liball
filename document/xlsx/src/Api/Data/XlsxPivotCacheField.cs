using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Represents a typed pivot-cache field.
/// </summary>
public class XlsxPivotCacheField
{
    /// <summary>Stores the field name.</summary>
    public string Name;
    /// <summary>Stores the field caption.</summary>
    public string Caption;
    /// <summary>Stores the number format identifier.</summary>
    public uint NumberFormatId;
    /// <summary>Indicates whether the number format identifier is present.</summary>
    public bool HasNumberFormatId;
    /// <summary>Indicates whether the field comes directly from the source data.</summary>
    public bool DatabaseField;
    /// <summary>Indicates whether the database-field flag is present.</summary>
    public bool HasDatabaseField;
    /// <summary>Contains typed shared items.</summary>
    public List<XlsxPivotCacheValue> SharedItems;
    /// <summary>Stores grouping information.</summary>
    public XlsxPivotCacheGrouping Grouping;
    /// <summary>Indicates whether grouping information is present.</summary>
    public bool HasGrouping;
    /// <summary>Contains unknown field attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Contains preserved field child XML.</summary>
    public List<string> UnknownXml;

    /// <summary>Initializes an empty pivot-cache field.</summary>
    public XlsxPivotCacheField()
    {
        Name = "";
        Caption = "";
        NumberFormatId = 0;
        HasNumberFormatId = false;
        DatabaseField = false;
        HasDatabaseField = false;
        SharedItems = new List<XlsxPivotCacheValue>();
        Grouping = new XlsxPivotCacheGrouping();
        HasGrouping = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
