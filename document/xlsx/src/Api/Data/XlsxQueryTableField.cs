using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents query table field data used by the public workbook API.
/// </summary>
public class XlsxQueryTableField
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
    /// Stores the table column id value used by this model.
    /// </summary>
    public uint TableColumnId;
    /// <summary>
    /// Indicates whether table column id is present and should be written.
    /// </summary>
    public bool HasTableColumnId;
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Initializes a new query table field with its default state.
    /// </summary>
    public XlsxQueryTableField()
    {
        Id = 0;
        HasId = false;
        TableColumnId = 0;
        HasTableColumnId = false;
        Name = "";
    }
}
