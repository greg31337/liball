using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents pivot table area reference data used by the public workbook API.
/// </summary>
public class XlsxPivotTableAreaReference
{
    /// <summary>
    /// Stores the field index value used by this model.
    /// </summary>
    public int FieldIndex;
    /// <summary>
    /// Indicates whether field index is present and should be written.
    /// </summary>
    public bool HasFieldIndex;
    /// <summary>
    /// Stores the number of  items processed by the operation.
    /// </summary>
    public uint Count;
    /// <summary>
    /// Indicates whether count is present and should be written.
    /// </summary>
    public bool HasCount;
    /// <summary>
    /// Stores the selected value used by this model.
    /// </summary>
    public bool Selected;
    /// <summary>
    /// Indicates whether selected is present and should be written.
    /// </summary>
    public bool HasSelected;
    /// <summary>
    /// Stores the by position value used by this model.
    /// </summary>
    public bool ByPosition;
    /// <summary>
    /// Indicates whether by position is present and should be written.
    /// </summary>
    public bool HasByPosition;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new pivot table area reference with its default state.
    /// </summary>
    public XlsxPivotTableAreaReference()
    {
        FieldIndex = 0;
        HasFieldIndex = false;
        Count = 0;
        HasCount = false;
        Selected = false;
        HasSelected = false;
        ByPosition = false;
        HasByPosition = false;
        RawXml = "";
    }
}
