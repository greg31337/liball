using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Controls safety limits, preservation behavior, selective loading, and statistics collection when a workbook is read.
/// </summary>
/// <remarks>
/// Limits are applied before or during package and XML parsing to reduce excessive memory use. Selected sheet and range filters are applied while loading worksheet data.
/// </remarks>
public class XlsxReaderOptions
{
    /// <summary>
    /// Contains preserve unknown parts.
    /// </summary>
    public bool PreserveUnknownParts;
    /// <summary>
    /// Stores the preserve unknown xml value used by this model.
    /// </summary>
    public bool PreserveUnknownXml;
    /// <summary>
    /// Contains allow macro enabled packages.
    /// </summary>
    public bool AllowMacroEnabledPackages;
    /// <summary>
    /// Stores the password value used by this model.
    /// </summary>
    public string Password;
    /// <summary>
    /// Stores the metadata only value used by this model.
    /// </summary>
    public bool MetadataOnly;
    /// <summary>
    /// Stores the values only value used by this model.
    /// </summary>
    public bool ValuesOnly;
    /// <summary>
    /// Stores the formulas only value used by this model.
    /// </summary>
    public bool FormulasOnly;
    /// <summary>
    /// Stores the styles only value used by this model.
    /// </summary>
    public bool StylesOnly;
    /// <summary>
    /// Contains selected sheets.
    /// </summary>
    public List<string> SelectedSheets;
    /// <summary>
    /// Contains selected ranges.
    /// </summary>
    public List<string> SelectedRanges;
    /// <summary>
    /// Contains max zip entries.
    /// </summary>
    public int MaxZipEntries;
    /// <summary>
    /// Contains max compressed bytes.
    /// </summary>
    public long MaxCompressedBytes;
    /// <summary>
    /// Contains max uncompressed bytes.
    /// </summary>
    public long MaxUncompressedBytes;
    /// <summary>
    /// Stores the max xml depth value used by this model.
    /// </summary>
    public int MaxXmlDepth;
    /// <summary>
    /// Contains max xml token characters.
    /// </summary>
    public int MaxXmlTokenCharacters;
    /// <summary>
    /// Stores the max relationships per part value used by this model.
    /// </summary>
    public int MaxRelationshipsPerPart;
    /// <summary>
    /// Contains max shared strings.
    /// </summary>
    public int MaxSharedStrings;
    /// <summary>
    /// Contains max cells.
    /// </summary>
    public int MaxCells;
    /// <summary>
    /// Contains max rows.
    /// </summary>
    public int MaxRows;
    /// <summary>
    /// Contains max columns.
    /// </summary>
    public int MaxColumns;
    /// <summary>
    /// Contains per-part, XML, workbook-content, and active-content security settings.
    /// </summary>
    public XlsxSecurityOptions SecurityOptions;
    /// <summary>
    /// Contains collect statistics.
    /// </summary>
    public bool CollectStatistics;
    /// <summary>
    /// Contains statistics.
    /// </summary>
    public XlsxPerformanceCounters Statistics;
    /// <summary>
    /// Initializes a new reader options with its default state.
    /// </summary>
    public XlsxReaderOptions()
    {
        PreserveUnknownParts = true;
        PreserveUnknownXml = true;
        AllowMacroEnabledPackages = false;
        Password = "";
        MetadataOnly = false;
        ValuesOnly = false;
        FormulasOnly = false;
        StylesOnly = false;
        SelectedSheets = new List<string>();
        SelectedRanges = new List<string>();
        MaxZipEntries = 10000;
        MaxCompressedBytes = 536870912;
        MaxUncompressedBytes = 2147483648;
        MaxXmlDepth = 128;
        MaxXmlTokenCharacters = 1048576;
        MaxRelationshipsPerPart = 10000;
        MaxSharedStrings = 10000000;
        MaxCells = 100000000;
        MaxRows = 1048576;
        MaxColumns = 16384;
        SecurityOptions = new XlsxSecurityOptions();
        CollectStatistics = false;
        Statistics = new XlsxPerformanceCounters();
    }

    /// <summary>
    /// Checks the current model or options and returns all validation diagnostics found.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxValidationResult Validate()
    {
        XlsxValidationResult result = new XlsxValidationResult();
        AddPositiveIntDiagnostic(result, MaxZipEntries, "reader.maxZipEntries", "MaxZipEntries must be greater than zero.");
        AddPositiveLongDiagnostic(result, MaxCompressedBytes, "reader.maxCompressedBytes", "MaxCompressedBytes must be greater than zero.");
        AddPositiveLongDiagnostic(result, MaxUncompressedBytes, "reader.maxUncompressedBytes", "MaxUncompressedBytes must be greater than zero.");
        AddPositiveIntDiagnostic(result, MaxXmlDepth, "reader.maxXmlDepth", "MaxXmlDepth must be greater than zero.");
        AddPositiveIntDiagnostic(result, MaxXmlTokenCharacters, "reader.maxXmlTokenCharacters", "MaxXmlTokenCharacters must be greater than zero.");
        AddPositiveIntDiagnostic(result, MaxRelationshipsPerPart, "reader.maxRelationshipsPerPart", "MaxRelationshipsPerPart must be greater than zero.");
        AddPositiveIntDiagnostic(result, MaxSharedStrings, "reader.maxSharedStrings", "MaxSharedStrings must be greater than zero.");
        AddPositiveIntDiagnostic(result, MaxCells, "reader.maxCells", "MaxCells must be greater than zero.");
        AddPositiveIntDiagnostic(result, MaxRows, "reader.maxRows", "MaxRows must be greater than zero.");
        AddPositiveIntDiagnostic(result, MaxColumns, "reader.maxColumns", "MaxColumns must be greater than zero.");
        if (SecurityOptions == null)
        {
            result.AddError("reader.securityOptions", "SecurityOptions must not be null.");
        }
        else
        {
            XlsxValidationResult securityValidation = SecurityOptions.Validate();
            foreach (XlsxDiagnostic diagnostic in securityValidation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        }
        if (MaxUncompressedBytes < MaxCompressedBytes)
        {
            result.AddError("reader.uncompressedLessThanCompressed", "MaxUncompressedBytes must be greater than or equal to MaxCompressedBytes.");
        }

        int loadModeCount = 0;
        if (MetadataOnly)
        {
            loadModeCount = loadModeCount + 1;
        }

        if (ValuesOnly)
        {
            loadModeCount = loadModeCount + 1;
        }

        if (FormulasOnly)
        {
            loadModeCount = loadModeCount + 1;
        }

        if (StylesOnly)
        {
            loadModeCount = loadModeCount + 1;
        }

        if (loadModeCount > 1)
        {
            result.AddError("reader.conflictingLoadModes", "Only one workbook load mode can be enabled at a time.");
        }

        return result;
    }

    internal void AddPositiveIntDiagnostic(XlsxValidationResult result, int value, string code, string message)
    {
        if (value <= 0)
        {
            result.AddError(code, message);
        }
    }

    internal void AddPositiveLongDiagnostic(XlsxValidationResult result, long value, string code, string message)
    {
        if (value <= 0)
        {
            result.AddError(code, message);
        }
    }
}
