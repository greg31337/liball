using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents dynamic filter data used by the public workbook API.
/// </summary>
public class XlsxDynamicFilter
{
    /// <summary>
    /// Stores the type value used by this model.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the value value used by this model.
    /// </summary>
    public double Value;
    /// <summary>
    /// Indicates whether value is present and should be written.
    /// </summary>
    public bool HasValue;
    /// <summary>
    /// Stores the max value value used by this model.
    /// </summary>
    public double MaxValue;
    /// <summary>
    /// Indicates whether max value is present and should be written.
    /// </summary>
    public bool HasMaxValue;
    /// <summary>
    /// Stores the value iso value used by this model.
    /// </summary>
    public string ValueIso;
    /// <summary>
    /// Stores the max value iso value used by this model.
    /// </summary>
    public string MaxValueIso;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new dynamic filter with its default state.
    /// </summary>
    public XlsxDynamicFilter()
    {
        Type = "";
        Value = 0;
        HasValue = false;
        MaxValue = 0;
        HasMaxValue = false;
        ValueIso = "";
        MaxValueIso = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
