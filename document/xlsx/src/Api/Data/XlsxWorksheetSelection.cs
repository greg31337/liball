using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet selection data used by the public workbook API.
/// </summary>
public class XlsxWorksheetSelection
{
    /// <summary>
    /// Stores the pane value used by this model.
    /// </summary>
    public string Pane;
    /// <summary>
    /// Stores the active cell value used by this model.
    /// </summary>
    public string ActiveCell;
    /// <summary>
    /// Stores the active cell id value used by this model.
    /// </summary>
    public uint ActiveCellId;
    /// <summary>
    /// Indicates whether active cell id is present and should be written.
    /// </summary>
    public bool HasActiveCellId;
    /// <summary>
    /// Stores the sqref value used by this model.
    /// </summary>
    public string Sqref;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new worksheet selection with its default state.
    /// </summary>
    public XlsxWorksheetSelection()
    {
        Pane = "";
        ActiveCell = "";
        ActiveCellId = 0;
        HasActiveCellId = false;
        Sqref = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
