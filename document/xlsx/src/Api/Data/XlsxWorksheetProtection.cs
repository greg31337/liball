using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet protection data used by the public workbook API.
/// </summary>
public class XlsxWorksheetProtection
{
    /// <summary>
    /// Stores the sheet value used by this model.
    /// </summary>
    public bool Sheet;
    /// <summary>
    /// Indicates whether sheet is present and should be written.
    /// </summary>
    public bool HasSheet;
    /// <summary>
    /// Contains objects.
    /// </summary>
    public bool Objects;
    /// <summary>
    /// Indicates whether objects is present and should be written.
    /// </summary>
    public bool HasObjects;
    /// <summary>
    /// Contains scenarios.
    /// </summary>
    public bool Scenarios;
    /// <summary>
    /// Indicates whether scenarios is present and should be written.
    /// </summary>
    public bool HasScenarios;
    /// <summary>
    /// Contains format cells.
    /// </summary>
    public bool FormatCells;
    /// <summary>
    /// Indicates whether format cells is present and should be written.
    /// </summary>
    public bool HasFormatCells;
    /// <summary>
    /// Contains format columns.
    /// </summary>
    public bool FormatColumns;
    /// <summary>
    /// Indicates whether format columns is present and should be written.
    /// </summary>
    public bool HasFormatColumns;
    /// <summary>
    /// Contains format rows.
    /// </summary>
    public bool FormatRows;
    /// <summary>
    /// Indicates whether format rows is present and should be written.
    /// </summary>
    public bool HasFormatRows;
    /// <summary>
    /// Contains insert columns.
    /// </summary>
    public bool InsertColumns;
    /// <summary>
    /// Indicates whether insert columns is present and should be written.
    /// </summary>
    public bool HasInsertColumns;
    /// <summary>
    /// Contains insert rows.
    /// </summary>
    public bool InsertRows;
    /// <summary>
    /// Indicates whether insert rows is present and should be written.
    /// </summary>
    public bool HasInsertRows;
    /// <summary>
    /// Contains insert hyperlinks.
    /// </summary>
    public bool InsertHyperlinks;
    /// <summary>
    /// Indicates whether insert hyperlinks is present and should be written.
    /// </summary>
    public bool HasInsertHyperlinks;
    /// <summary>
    /// Contains delete columns.
    /// </summary>
    public bool DeleteColumns;
    /// <summary>
    /// Indicates whether delete columns is present and should be written.
    /// </summary>
    public bool HasDeleteColumns;
    /// <summary>
    /// Contains delete rows.
    /// </summary>
    public bool DeleteRows;
    /// <summary>
    /// Indicates whether delete rows is present and should be written.
    /// </summary>
    public bool HasDeleteRows;
    /// <summary>
    /// Contains select locked cells.
    /// </summary>
    public bool SelectLockedCells;
    /// <summary>
    /// Indicates whether select locked cells is present and should be written.
    /// </summary>
    public bool HasSelectLockedCells;
    /// <summary>
    /// Stores the sort value used by this model.
    /// </summary>
    public bool Sort;
    /// <summary>
    /// Indicates whether sort is present and should be written.
    /// </summary>
    public bool HasSort;
    /// <summary>
    /// Stores the auto filter value used by this model.
    /// </summary>
    public bool AutoFilter;
    /// <summary>
    /// Indicates whether auto filter is present and should be written.
    /// </summary>
    public bool HasAutoFilter;
    /// <summary>
    /// Contains pivot tables.
    /// </summary>
    public bool PivotTables;
    /// <summary>
    /// Indicates whether pivot tables is present and should be written.
    /// </summary>
    public bool HasPivotTables;
    /// <summary>
    /// Contains select unlocked cells.
    /// </summary>
    public bool SelectUnlockedCells;
    /// <summary>
    /// Indicates whether select unlocked cells is present and should be written.
    /// </summary>
    public bool HasSelectUnlockedCells;
    /// <summary>
    /// Stores the password value used by this model.
    /// </summary>
    public string? Password;
    /// <summary>
    /// Stores the algorithm name value used by this model.
    /// </summary>
    public string? AlgorithmName;
    /// <summary>
    /// Indicates whether h value is present and should be written.
    /// </summary>
    public string? HashValue;
    /// <summary>
    /// Stores the salt value value used by this model.
    /// </summary>
    public string? SaltValue;
    /// <summary>
    /// Stores the number of spin items processed by the operation.
    /// </summary>
    public uint SpinCount;
    /// <summary>
    /// Indicates whether spin count is present and should be written.
    /// </summary>
    public bool HasSpinCount;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new worksheet protection with its default state.
    /// </summary>
    public XlsxWorksheetProtection()
    {
        Sheet = false;
        HasSheet = false;
        Objects = false;
        HasObjects = false;
        Scenarios = false;
        HasScenarios = false;
        FormatCells = false;
        HasFormatCells = false;
        FormatColumns = false;
        HasFormatColumns = false;
        FormatRows = false;
        HasFormatRows = false;
        InsertColumns = false;
        HasInsertColumns = false;
        InsertRows = false;
        HasInsertRows = false;
        InsertHyperlinks = false;
        HasInsertHyperlinks = false;
        DeleteColumns = false;
        HasDeleteColumns = false;
        DeleteRows = false;
        HasDeleteRows = false;
        SelectLockedCells = false;
        HasSelectLockedCells = false;
        Sort = false;
        HasSort = false;
        AutoFilter = false;
        HasAutoFilter = false;
        PivotTables = false;
        HasPivotTables = false;
        SelectUnlockedCells = false;
        HasSelectUnlockedCells = false;
        Password = "";
        AlgorithmName = "";
        HashValue = "";
        SaltValue = "";
        SpinCount = 0;
        HasSpinCount = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
