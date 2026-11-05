using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents calculation chain cell data used by the public workbook API.
/// </summary>
public class XlsxCalculationChainCell
{
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string? Reference;
    /// <summary>
    /// Stores the sheet id value used by this model.
    /// </summary>
    public uint SheetId;
    /// <summary>
    /// Indicates whether sheet id is present and should be written.
    /// </summary>
    public bool HasSheetId;
    /// <summary>
    /// Stores the in child chain value used by this model.
    /// </summary>
    public bool InChildChain;
    /// <summary>
    /// Indicates whether in child chain is present and should be written.
    /// </summary>
    public bool HasInChildChain;
    /// <summary>
    /// Stores the new dependency level value used by this model.
    /// </summary>
    public bool NewDependencyLevel;
    /// <summary>
    /// Indicates whether new dependency level is present and should be written.
    /// </summary>
    public bool HasNewDependencyLevel;
    /// <summary>
    /// Stores the new thread value used by this model.
    /// </summary>
    public bool NewThread;
    /// <summary>
    /// Indicates whether new thread is present and should be written.
    /// </summary>
    public bool HasNewThread;
    /// <summary>
    /// Stores the array value used by this model.
    /// </summary>
    public bool Array;
    /// <summary>
    /// Indicates whether array is present and should be written.
    /// </summary>
    public bool HasArray;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new calculation chain cell with its default state.
    /// </summary>
    public XlsxCalculationChainCell()
    {
        Reference = "";
        SheetId = 0;
        HasSheetId = false;
        InChildChain = false;
        HasInChildChain = false;
        NewDependencyLevel = false;
        HasNewDependencyLevel = false;
        NewThread = false;
        HasNewThread = false;
        Array = false;
        HasArray = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
