using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet page break data used by the public workbook API.
/// </summary>
public class XlsxWorksheetPageBreak
{
    /// <summary>
    /// Stores the id value used by this model.
    /// </summary>
    public uint Id;
    /// <summary>
    /// Indicates whether id is present and should be written.
    /// </summary>
    public bool HasId;
    /// <summary>
    /// Stores the min value used by this model.
    /// </summary>
    public uint Min;
    /// <summary>
    /// Indicates whether min is present and should be written.
    /// </summary>
    public bool HasMin;
    /// <summary>
    /// Stores the max value used by this model.
    /// </summary>
    public uint Max;
    /// <summary>
    /// Indicates whether max is present and should be written.
    /// </summary>
    public bool HasMax;
    /// <summary>
    /// Stores the manual value used by this model.
    /// </summary>
    public bool Manual;
    /// <summary>
    /// Indicates whether manual is present and should be written.
    /// </summary>
    public bool HasManual;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new worksheet page break with its default state.
    /// </summary>
    public XlsxWorksheetPageBreak()
    {
        Id = 0;
        HasId = false;
        Min = 0;
        HasMin = false;
        Max = 0;
        HasMax = false;
        Manual = false;
        HasManual = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
