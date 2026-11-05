using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents top10 filter data used by the public workbook API.
/// </summary>
public class XlsxTop10Filter
{
    /// <summary>
    /// Stores the top value used by this model.
    /// </summary>
    public bool Top;
    /// <summary>
    /// Indicates whether top is present and should be written.
    /// </summary>
    public bool HasTop;
    /// <summary>
    /// Stores the percent value used by this model.
    /// </summary>
    public bool Percent;
    /// <summary>
    /// Indicates whether percent is present and should be written.
    /// </summary>
    public bool HasPercent;
    /// <summary>
    /// Stores the value value used by this model.
    /// </summary>
    public double Value;
    /// <summary>
    /// Indicates whether value is present and should be written.
    /// </summary>
    public bool HasValue;
    /// <summary>
    /// Stores the filter value value used by this model.
    /// </summary>
    public double FilterValue;
    /// <summary>
    /// Indicates whether filter value is present and should be written.
    /// </summary>
    public bool HasFilterValue;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new top10 filter with its default state.
    /// </summary>
    public XlsxTop10Filter()
    {
        Top = false;
        HasTop = false;
        Percent = false;
        HasPercent = false;
        Value = 0;
        HasValue = false;
        FilterValue = 0;
        HasFilterValue = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
