using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents pivot table filter data used by the public workbook API.
/// </summary>
public class XlsxPivotTableFilter
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
    /// Stores the type value used by this model.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the id value used by this model.
    /// </summary>
    public uint Id;
    /// <summary>
    /// Indicates whether id is present and should be written.
    /// </summary>
    public bool HasId;
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the typed filter kind.
    /// </summary>
    public XlsxPivotFilterKind FilterKind;
    /// <summary>
    /// Indicates whether a typed filter kind is present.
    /// </summary>
    public bool HasFilterKind;
    /// <summary>
    /// Stores the label operand for a typed label filter.
    /// </summary>
    public string LabelValue;
    /// <summary>
    /// Stores the numeric operand for a typed value filter.
    /// </summary>
    public double NumericValue;
    /// <summary>
    /// Stores the data-field index used by a typed value filter.
    /// </summary>
    public int DataFieldIndex;
    /// <summary>
    /// Indicates whether a data-field index is present.
    /// </summary>
    public bool HasDataFieldIndex;
    /// <summary>
    /// Stores the evaluation order value used by this model.
    /// </summary>
    public int EvaluationOrder;
    /// <summary>
    /// Indicates whether evaluation order is present and should be written.
    /// </summary>
    public bool HasEvaluationOrder;
    /// <summary>
    /// Contains areas.
    /// </summary>
    public List<XlsxPivotTableArea> Areas;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new pivot table filter with its default state.
    /// </summary>
    public XlsxPivotTableFilter()
    {
        FieldIndex = 0;
        HasFieldIndex = false;
        Type = "";
        Id = 0;
        HasId = false;
        Name = "";
        FilterKind = XlsxPivotFilterKind.LabelEquals;
        HasFilterKind = false;
        LabelValue = "";
        NumericValue = 0;
        DataFieldIndex = 0;
        HasDataFieldIndex = false;
        EvaluationOrder = 0;
        HasEvaluationOrder = false;
        Areas = new List<XlsxPivotTableArea>();
        RawXml = "";
    }
}
