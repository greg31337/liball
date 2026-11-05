using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents custom filter data used by the public workbook API.
/// </summary>
public class XlsxCustomFilter
{
    /// <summary>
    /// Stores the operator value used by this model.
    /// </summary>
    public string Operator;
    /// <summary>
    /// Stores the value value used by this model.
    /// </summary>
    public string Value;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new custom filter with its default state.
    /// </summary>
    public XlsxCustomFilter()
    {
        Operator = "";
        Value = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
