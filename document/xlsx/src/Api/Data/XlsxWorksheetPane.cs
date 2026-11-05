using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet pane data used by the public workbook API.
/// </summary>
public class XlsxWorksheetPane
{
    /// <summary>
    /// Stores the x split value used by this model.
    /// </summary>
    public double XSplit;
    /// <summary>
    /// Indicates whether x split is present and should be written.
    /// </summary>
    public bool HasXSplit;
    /// <summary>
    /// Stores the y split value used by this model.
    /// </summary>
    public double YSplit;
    /// <summary>
    /// Indicates whether y split is present and should be written.
    /// </summary>
    public bool HasYSplit;
    /// <summary>
    /// Stores the top left cell value used by this model.
    /// </summary>
    public string TopLeftCell;
    /// <summary>
    /// Stores the active pane value used by this model.
    /// </summary>
    public string ActivePane;
    /// <summary>
    /// Stores the state value used by this model.
    /// </summary>
    public string State;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new worksheet pane with its default state.
    /// </summary>
    public XlsxWorksheetPane()
    {
        XSplit = 0;
        HasXSplit = false;
        YSplit = 0;
        HasYSplit = false;
        TopLeftCell = "";
        ActivePane = "";
        State = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
