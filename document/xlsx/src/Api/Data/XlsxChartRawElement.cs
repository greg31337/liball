using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents chart raw element data used by the public workbook API.
/// </summary>
public class XlsxChartRawElement
{
    /// <summary>
    /// Stores the kind value used by this model.
    /// </summary>
    public string Kind;
    /// <summary>
    /// Stores the local name value used by this model.
    /// </summary>
    public string LocalName;
    /// <summary>Stores the namespace URI of the preserved chart vocabulary.</summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new chart raw element with its default state.
    /// </summary>
    public XlsxChartRawElement()
    {
        Kind = "";
        LocalName = "";
        NamespaceUri = "";
        RawXml = "";
    }
}
