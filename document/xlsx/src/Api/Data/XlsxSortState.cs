using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents sort state data used by the public workbook API.
/// </summary>
public class XlsxSortState
{
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the column sort value used by this model.
    /// </summary>
    public bool ColumnSort;
    /// <summary>
    /// Indicates whether column sort is present and should be written.
    /// </summary>
    public bool HasColumnSort;
    /// <summary>
    /// Stores the case sensitive value used by this model.
    /// </summary>
    public bool CaseSensitive;
    /// <summary>
    /// Indicates whether case sensitive is present and should be written.
    /// </summary>
    public bool HasCaseSensitive;
    /// <summary>
    /// Stores the sort method value used by this model.
    /// </summary>
    public string SortMethod;
    /// <summary>
    /// Contains conditions.
    /// </summary>
    public List<XlsxSortCondition> Conditions;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new sort state with its default state.
    /// </summary>
    public XlsxSortState()
    {
        Reference = "";
        ColumnSort = false;
        HasColumnSort = false;
        CaseSensitive = false;
        HasCaseSensitive = false;
        SortMethod = "";
        Conditions = new List<XlsxSortCondition>();
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
