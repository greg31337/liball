using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook calculation properties data used by the public workbook API.
/// </summary>
public class XlsxWorkbookCalculationProperties
{
    /// <summary>
    /// Stores the calc id value used by this model.
    /// </summary>
    public uint CalcId;
    /// <summary>
    /// Indicates whether calc id is present and should be written.
    /// </summary>
    public bool HasCalcId;
    /// <summary>
    /// Stores the calc mode value used by this model.
    /// </summary>
    public string? CalcMode;
    /// <summary>
    /// Stores the ref mode value used by this model.
    /// </summary>
    public string? RefMode;
    /// <summary>
    /// Stores the full calc on load value used by this model.
    /// </summary>
    public bool FullCalcOnLoad;
    /// <summary>
    /// Indicates whether full calc on load is present and should be written.
    /// </summary>
    public bool HasFullCalcOnLoad;
    /// <summary>
    /// Stores the force full calc value used by this model.
    /// </summary>
    public bool ForceFullCalc;
    /// <summary>
    /// Indicates whether force full calc is present and should be written.
    /// </summary>
    public bool HasForceFullCalc;
    /// <summary>
    /// Stores the iterate value used by this model.
    /// </summary>
    public bool Iterate;
    /// <summary>
    /// Indicates whether iterate is present and should be written.
    /// </summary>
    public bool HasIterate;
    /// <summary>
    /// Stores the number of iterate items processed by the operation.
    /// </summary>
    public uint IterateCount;
    /// <summary>
    /// Indicates whether iterate count is present and should be written.
    /// </summary>
    public bool HasIterateCount;
    /// <summary>
    /// Stores the iterate delta value used by this model.
    /// </summary>
    public double IterateDelta;
    /// <summary>
    /// Indicates whether iterate delta is present and should be written.
    /// </summary>
    public bool HasIterateDelta;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new workbook calculation properties with its default state.
    /// </summary>
    public XlsxWorkbookCalculationProperties()
    {
        CalcId = 0;
        HasCalcId = false;
        CalcMode = "";
        RefMode = "";
        FullCalcOnLoad = false;
        HasFullCalcOnLoad = false;
        ForceFullCalc = false;
        HasForceFullCalc = false;
        Iterate = false;
        HasIterate = false;
        IterateCount = 0;
        HasIterateCount = false;
        IterateDelta = 0;
        HasIterateDelta = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
