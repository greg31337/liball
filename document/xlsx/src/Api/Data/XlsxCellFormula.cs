using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents cell formula data used by the public workbook API.
/// </summary>
public class XlsxCellFormula
{
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the type value used by this model.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the shared index value used by this model.
    /// </summary>
    public uint SharedIndex;
    /// <summary>
    /// Indicates whether shared index is present and should be written.
    /// </summary>
    public bool HasSharedIndex;
    /// <summary>
    /// Stores the always calculate array value used by this model.
    /// </summary>
    public bool AlwaysCalculateArray;
    /// <summary>
    /// Indicates whether always calculate array is present and should be written.
    /// </summary>
    public bool HasAlwaysCalculateArray;
    /// <summary>
    /// Stores the calculate cell value used by this model.
    /// </summary>
    public bool CalculateCell;
    /// <summary>
    /// Indicates whether calculate cell is present and should be written.
    /// </summary>
    public bool HasCalculateCell;
    /// <summary>
    /// Stores the data table2 d value used by this model.
    /// </summary>
    public bool DataTable2D;
    /// <summary>
    /// Indicates whether data table2 d is present and should be written.
    /// </summary>
    public bool HasDataTable2D;
    /// <summary>
    /// Stores the data table row value used by this model.
    /// </summary>
    public bool DataTableRow;
    /// <summary>
    /// Indicates whether data table row is present and should be written.
    /// </summary>
    public bool HasDataTableRow;
    /// <summary>
    /// Stores the input1 deleted value used by this model.
    /// </summary>
    public bool Input1Deleted;
    /// <summary>
    /// Indicates whether input1 deleted is present and should be written.
    /// </summary>
    public bool HasInput1Deleted;
    /// <summary>
    /// Stores the input2 deleted value used by this model.
    /// </summary>
    public bool Input2Deleted;
    /// <summary>
    /// Indicates whether input2 deleted is present and should be written.
    /// </summary>
    public bool HasInput2Deleted;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Input1Reference;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Input2Reference;
    /// <summary>
    /// Stores the assigns to name value used by this model.
    /// </summary>
    public bool AssignsToName;
    /// <summary>
    /// Indicates whether assigns to name is present and should be written.
    /// </summary>
    public bool HasAssignsToName;
    /// <summary>
    /// Stores the dynamic array value used by this model.
    /// </summary>
    public bool DynamicArray;
    /// <summary>
    /// Indicates whether dynamic array is present and should be written.
    /// </summary>
    public bool HasDynamicArray;
    /// <summary>
    /// Stores the cached value value used by this model.
    /// </summary>
    public string CachedValue;
    /// <summary>
    /// Stores the cached type value used by this model.
    /// </summary>
    public string? CachedType;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new cell formula with its default state.
    /// </summary>
    public XlsxCellFormula()
    {
        Text = "";
        Type = "";
        Reference = "";
        SharedIndex = 0;
        HasSharedIndex = false;
        AlwaysCalculateArray = false;
        HasAlwaysCalculateArray = false;
        CalculateCell = false;
        HasCalculateCell = false;
        DataTable2D = false;
        HasDataTable2D = false;
        DataTableRow = false;
        HasDataTableRow = false;
        Input1Deleted = false;
        HasInput1Deleted = false;
        Input2Deleted = false;
        HasInput2Deleted = false;
        Input1Reference = "";
        Input2Reference = "";
        AssignsToName = false;
        HasAssignsToName = false;
        DynamicArray = false;
        HasDynamicArray = false;
        CachedValue = "";
        CachedType = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }

    internal static XlsxCellFormula CreateNormal(string text)
    {
        XlsxCellFormula formula = new XlsxCellFormula();
        formula.Text = XlsxCellValueConverter.NormalizeText(text);
        return formula;
    }

    internal static XlsxCellFormula CreateShared(string text, uint sharedIndex, string reference)
    {
        XlsxCellFormula formula = CreateNormal(text);
        formula.Type = "shared";
        formula.SharedIndex = sharedIndex;
        formula.HasSharedIndex = true;
        formula.Reference = XlsxCellValueConverter.NormalizeText(reference);
        return formula;
    }

    internal static XlsxCellFormula CreateArray(string text, string reference)
    {
        XlsxCellFormula formula = CreateNormal(text);
        formula.Type = "array";
        formula.Reference = XlsxCellValueConverter.NormalizeText(reference);
        return formula;
    }

    internal static XlsxCellFormula CreateDynamicArray(string text, string reference)
    {
        XlsxCellFormula formula = CreateNormal(text);
        formula.Type = "dynamicArray";
        formula.Reference = XlsxCellValueConverter.NormalizeText(reference);
        formula.DynamicArray = true;
        formula.HasDynamicArray = true;
        return formula;
    }

    internal static XlsxCellFormula CreateDataTable(string text, string reference, string input1Reference, string input2Reference)
    {
        XlsxCellFormula formula = CreateNormal(text);
        formula.Type = "dataTable";
        formula.Reference = XlsxCellValueConverter.NormalizeText(reference);
        formula.Input1Reference = XlsxCellValueConverter.NormalizeText(input1Reference);
        formula.Input2Reference = XlsxCellValueConverter.NormalizeText(input2Reference);
        return formula;
    }
}
