using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents an editable worksheet part, including cells, layout, filters, validation rules, tables, and preserved OOXML content.
/// </summary>
/// <remarks>
/// Cell coordinates are one-based. A1 references are normalized during edits, and structural edits update dependent ranges and formulas when supported.
/// </remarks>
public class XlsxWorksheetPart
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Contains sheet properties.
    /// </summary>
    public XlsxWorksheetProperties SheetProperties;
    /// <summary>
    /// Indicates whether sheet properties is present and should be written.
    /// </summary>
    public bool HasSheetProperties;
    /// <summary>
    /// Stores the dimension value used by this model.
    /// </summary>
    public XlsxWorksheetDimension Dimension;
    /// <summary>
    /// Indicates whether dimension is present and should be written.
    /// </summary>
    public bool HasDimension;
    /// <summary>
    /// Contains sheet views.
    /// </summary>
    public List<XlsxWorksheetView> SheetViews;
    /// <summary>
    /// Contains format properties.
    /// </summary>
    public XlsxWorksheetFormatProperties FormatProperties;
    /// <summary>
    /// Indicates whether format properties is present and should be written.
    /// </summary>
    public bool HasFormatProperties;
    /// <summary>
    /// Contains columns.
    /// </summary>
    public List<XlsxWorksheetColumn> Columns;
    /// <summary>
    /// Contains rows.
    /// </summary>
    public List<XlsxWorksheetRow> Rows;
    /// <summary>
    /// Contains merged cells.
    /// </summary>
    public List<XlsxWorksheetMergeCell> MergedCells;
    /// <summary>
    /// Contains hyperlinks.
    /// </summary>
    public List<XlsxWorksheetHyperlink> Hyperlinks;
    /// <summary>
    /// Stores the comments part value used by this model.
    /// </summary>
    public XlsxCommentsPart CommentsPart;
    /// <summary>
    /// Indicates whether comments part is present and should be written.
    /// </summary>
    public bool HasCommentsPart;
    /// <summary>
    /// Stores the legacy comment drawing part value used by this model.
    /// </summary>
    public XlsxLegacyCommentDrawingPart LegacyCommentDrawingPart;
    /// <summary>
    /// Indicates whether legacy comment drawing part is present and should be written.
    /// </summary>
    public bool HasLegacyCommentDrawingPart;
    /// <summary>
    /// Contains page margins.
    /// </summary>
    public XlsxWorksheetPageMargins? PageMargins;
    /// <summary>
    /// Indicates whether page margins is present and should be written.
    /// </summary>
    public bool HasPageMargins;
    /// <summary>
    /// Stores the page setup value used by this model.
    /// </summary>
    public XlsxWorksheetPageSetup? PageSetup;
    /// <summary>
    /// Indicates whether page setup is present and should be written.
    /// </summary>
    public bool HasPageSetup;
    /// <summary>
    /// Contains print options.
    /// </summary>
    public XlsxWorksheetPrintOptions? PrintOptions;
    /// <summary>
    /// Indicates whether print options is present and should be written.
    /// </summary>
    public bool HasPrintOptions;
    /// <summary>
    /// Stores the header footer value used by this model.
    /// </summary>
    public XlsxWorksheetHeaderFooter? HeaderFooter;
    /// <summary>
    /// Indicates whether header footer is present and should be written.
    /// </summary>
    public bool HasHeaderFooter;
    /// <summary>
    /// Contains row breaks.
    /// </summary>
    public List<XlsxWorksheetPageBreak> RowBreaks;
    /// <summary>
    /// Contains column breaks.
    /// </summary>
    public List<XlsxWorksheetPageBreak> ColumnBreaks;
    /// <summary>
    /// Stores the sheet protection value used by this model.
    /// </summary>
    public XlsxWorksheetProtection? SheetProtection;
    /// <summary>
    /// Indicates whether sheet protection is present and should be written.
    /// </summary>
    public bool HasSheetProtection;
    /// <summary>
    /// Stores the auto filter value used by this model.
    /// </summary>
    public XlsxAutoFilter AutoFilter;
    /// <summary>
    /// Indicates whether auto filter is present and should be written.
    /// </summary>
    public bool HasAutoFilter;
    /// <summary>
    /// Stores the drawing value used by this model.
    /// </summary>
    public XlsxSheetDrawingReference Drawing;
    /// <summary>
    /// Indicates whether drawing is present and should be written.
    /// </summary>
    public bool HasDrawing;
    /// <summary>
    /// Contains data validations.
    /// </summary>
    public XlsxDataValidations DataValidations;
    /// <summary>
    /// Indicates whether data validations is present and should be written.
    /// </summary>
    public bool HasDataValidations;
    /// <summary>
    /// Contains conditional formattings.
    /// </summary>
    public List<XlsxConditionalFormatting> ConditionalFormattings;
    /// <summary>
    /// Stores worksheet scenarios.
    /// </summary>
    public XlsxWorksheetScenarios Scenarios;
    /// <summary>
    /// Indicates whether scenarios are present and should be written.
    /// </summary>
    public bool HasScenarios;
    /// <summary>
    /// Contains custom worksheet views.
    /// </summary>
    public List<XlsxWorksheetCustomView> CustomSheetViews;
    /// <summary>
    /// Contains ignored-error rules.
    /// </summary>
    public List<XlsxWorksheetIgnoredError> IgnoredErrors;
    /// <summary>
    /// Contains worksheet smart tags.
    /// </summary>
    public List<XlsxWorksheetSmartTag> SmartTags;
    /// <summary>
    /// Contains web-publish items.
    /// </summary>
    public List<XlsxWorksheetWebPublishItem> WebPublishItems;
    /// <summary>
    /// Contains table references.
    /// </summary>
    public List<XlsxWorksheetTableReference> TableReferences;
    /// <summary>
    /// Contains pivot table references.
    /// </summary>
    public List<XlsxWorksheetPivotTableReference> PivotTableReferences;
    /// <summary>
    /// Contains worksheet OLE-object and embedded-package references.
    /// </summary>
    public List<XlsxWorksheetEmbeddedObjectReference> EmbeddedObjectReferences;
    /// <summary>
    /// Contains tables.
    /// </summary>
    public List<XlsxTablePart> Tables;
    /// <summary>
    /// Contains sparkline groups.
    /// </summary>
    public List<XlsxSparklineGroup> SparklineGroups;
    /// <summary>
    /// Indicates whether sparkline groups is present and should be written.
    /// </summary>
    public bool HasSparklineGroups;
    /// <summary>
    /// Stores the extension list value used by this model.
    /// </summary>
    public XlsxExtensionList ExtensionList;
    /// <summary>
    /// Indicates whether extension list is present and should be written.
    /// </summary>
    public bool HasExtensionList;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new worksheet part with its default state.
    /// </summary>
    public XlsxWorksheetPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = XlsxPackagePaths.Worksheet1;
        NamespaceUri = XlsxNamespaces.Spreadsheet;
        SheetProperties = new XlsxWorksheetProperties();
        HasSheetProperties = false;
        Dimension = new XlsxWorksheetDimension();
        HasDimension = false;
        SheetViews = new List<XlsxWorksheetView>();
        FormatProperties = new XlsxWorksheetFormatProperties();
        HasFormatProperties = false;
        Columns = new List<XlsxWorksheetColumn>();
        Rows = new List<XlsxWorksheetRow>();
        MergedCells = new List<XlsxWorksheetMergeCell>();
        Hyperlinks = new List<XlsxWorksheetHyperlink>();
        CommentsPart = new XlsxCommentsPart();
        HasCommentsPart = false;
        LegacyCommentDrawingPart = new XlsxLegacyCommentDrawingPart();
        HasLegacyCommentDrawingPart = false;
        PageMargins = new XlsxWorksheetPageMargins();
        HasPageMargins = false;
        PageSetup = new XlsxWorksheetPageSetup();
        HasPageSetup = false;
        PrintOptions = new XlsxWorksheetPrintOptions();
        HasPrintOptions = false;
        HeaderFooter = new XlsxWorksheetHeaderFooter();
        HasHeaderFooter = false;
        RowBreaks = new List<XlsxWorksheetPageBreak>();
        ColumnBreaks = new List<XlsxWorksheetPageBreak>();
        SheetProtection = new XlsxWorksheetProtection();
        HasSheetProtection = false;
        AutoFilter = new XlsxAutoFilter();
        HasAutoFilter = false;
        Drawing = new XlsxSheetDrawingReference();
        HasDrawing = false;
        DataValidations = new XlsxDataValidations();
        HasDataValidations = false;
        ConditionalFormattings = new List<XlsxConditionalFormatting>();
        Scenarios = new XlsxWorksheetScenarios();
        HasScenarios = false;
        CustomSheetViews = new List<XlsxWorksheetCustomView>();
        IgnoredErrors = new List<XlsxWorksheetIgnoredError>();
        SmartTags = new List<XlsxWorksheetSmartTag>();
        WebPublishItems = new List<XlsxWorksheetWebPublishItem>();
        TableReferences = new List<XlsxWorksheetTableReference>();
        PivotTableReferences = new List<XlsxWorksheetPivotTableReference>();
        EmbeddedObjectReferences = new List<XlsxWorksheetEmbeddedObjectReference>();
        Tables = new List<XlsxTablePart>();
        SparklineGroups = new List<XlsxSparklineGroup>();
        HasSparklineGroups = false;
        ExtensionList = new XlsxExtensionList();
        HasExtensionList = false;
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    internal XlsxWorksheetMetadataResult SetTabColorRgb(string rgb)
    {
        XlsxWorksheetMetadataResult result = new XlsxWorksheetMetadataResult();
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseColor(rgb);
        foreach (XlsxDiagnostic diagnostic in parsed.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
        if (!result.IsValid)
        {
            return result;
        }
        if (SheetProperties == null)
        {
            SheetProperties = new XlsxWorksheetProperties();
        }
        SheetProperties.TabColor = parsed.ColorValue;
        SheetProperties.HasTabColor = true;
        SheetProperties.RawXml = "";
        HasSheetProperties = true;
        return result;
    }

    internal XlsxWorksheetMetadataResult SetOutlineProperties(bool summaryBelow, bool summaryRight, bool showOutlineSymbols, bool applyStyles)
    {
        XlsxWorksheetMetadataResult result = new XlsxWorksheetMetadataResult();
        if (SheetProperties == null)
        {
            SheetProperties = new XlsxWorksheetProperties();
        }
        XlsxWorksheetOutlineProperties outline = new XlsxWorksheetOutlineProperties();
        outline.SummaryBelow = summaryBelow;
        outline.HasSummaryBelow = true;
        outline.SummaryRight = summaryRight;
        outline.HasSummaryRight = true;
        outline.ShowOutlineSymbols = showOutlineSymbols;
        outline.HasShowOutlineSymbols = true;
        outline.ApplyStyles = applyStyles;
        outline.HasApplyStyles = true;
        SheetProperties.OutlineProperties = outline;
        SheetProperties.HasOutlineProperties = true;
        SheetProperties.RawXml = "";
        HasSheetProperties = true;
        return result;
    }

    internal XlsxWorksheetMetadataResult AddScenario(string name)
    {
        XlsxWorksheetMetadataResult result = new XlsxWorksheetMetadataResult();
        string scenarioName = XlsxCellValueConverter.NormalizeText(name);
        if (scenarioName.Length == 0)
        {
            result.AddError("worksheet.scenarioNameMissing", "Worksheet scenario name must not be empty.");
            return result;
        }
        if (Scenarios == null)
        {
            Scenarios = new XlsxWorksheetScenarios();
        }
        foreach (XlsxWorksheetScenario existing in Scenarios.Items)
        {
            if (existing != null && string.Equals(existing.Name, scenarioName, StringComparison.OrdinalIgnoreCase))
            {
                result.AddError("worksheet.scenarioNameDuplicate", "Worksheet scenario names must be unique.");
                return result;
            }
        }
        XlsxWorksheetScenario scenario = new XlsxWorksheetScenario();
        scenario.Name = scenarioName;
        Scenarios.Items.Add(scenario);
        HasScenarios = true;
        result.Scenario = scenario;
        return result;
    }

    internal XlsxWorksheetMetadataResult AddCustomSheetView(string guid)
    {
        XlsxWorksheetMetadataResult result = new XlsxWorksheetMetadataResult();
        string identifier = XlsxCellValueConverter.NormalizeText(guid);
        if (identifier.Length == 0)
        {
            result.AddError("worksheet.customViewGuidMissing", "Custom worksheet view guid must not be empty.");
            return result;
        }
        foreach (XlsxWorksheetCustomView existing in CustomSheetViews)
        {
            if (existing != null && string.Equals(existing.Guid, identifier, StringComparison.OrdinalIgnoreCase))
            {
                result.AddError("worksheet.customViewGuidDuplicate", "Custom worksheet view guid must be unique.");
                return result;
            }
        }
        XlsxWorksheetCustomView view = new XlsxWorksheetCustomView();
        view.Guid = identifier;
        CustomSheetViews.Add(view);
        result.CustomView = view;
        return result;
    }

    internal XlsxWorksheetMetadataResult AddIgnoredError(string squareReference)
    {
        XlsxWorksheetMetadataResult result = new XlsxWorksheetMetadataResult();
        string references = XlsxCellValueConverter.NormalizeText(squareReference);
        if (!ValidateMetadataRangeSequence(result, references))
        {
            return result;
        }
        XlsxWorksheetIgnoredError ignored = new XlsxWorksheetIgnoredError();
        ignored.SquareReference = references;
        IgnoredErrors.Add(ignored);
        result.IgnoredError = ignored;
        return result;
    }

    internal XlsxWorksheetMetadataResult AddSmartTag(string reference, uint typeId)
    {
        XlsxWorksheetMetadataResult result = new XlsxWorksheetMetadataResult();
        XlsxA1CellReferenceResult parsed = XlsxA1CellReferenceConverter.Parse(reference);
        foreach (XlsxDiagnostic diagnostic in parsed.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
        if (typeId == 0)
        {
            result.AddError("worksheet.smartTagTypeRange", "Smart-tag type id must be greater than zero.");
        }
        if (!result.IsValid)
        {
            return result;
        }
        XlsxWorksheetSmartTag smartTag = new XlsxWorksheetSmartTag();
        smartTag.Reference = parsed.Text;
        smartTag.TypeId = typeId;
        smartTag.HasTypeId = true;
        SmartTags.Add(smartTag);
        result.SmartTag = smartTag;
        return result;
    }

    internal XlsxWorksheetMetadataResult AddWebPublishItem(uint id, string destinationFile)
    {
        XlsxWorksheetMetadataResult result = new XlsxWorksheetMetadataResult();
        string destination = XlsxCellValueConverter.NormalizeText(destinationFile);
        if (id == 0)
        {
            result.AddError("worksheet.webPublishIdRange", "Web-publish item id must be greater than zero.");
        }
        if (destination.Length == 0)
        {
            result.AddError("worksheet.webPublishDestinationMissing", "Web-publish destination file must not be empty.");
        }
        if (!result.IsValid)
        {
            return result;
        }
        foreach (XlsxWorksheetWebPublishItem existing in WebPublishItems)
        {
            if (existing != null && existing.HasId && existing.Id == id)
            {
                result.AddError("worksheet.webPublishIdDuplicate", "Web-publish item id must be unique.");
                return result;
            }
        }
        XlsxWorksheetWebPublishItem item = new XlsxWorksheetWebPublishItem();
        item.Id = id;
        item.HasId = true;
        item.DestinationFile = destination;
        WebPublishItems.Add(item);
        result.WebPublishItem = item;
        return result;
    }

    internal static bool ValidateMetadataRangeSequence(XlsxWorksheetMetadataResult result, string squareReference)
    {
        if (squareReference.Length == 0)
        {
            result.AddError("worksheet.metadataReferenceMissing", "Worksheet metadata range reference must not be empty.");
            return false;
        }
        string[] references = squareReference.Split(' ');
        foreach (string reference in references)
        {
            if (reference.Length == 0)
            {
                continue;
            }
            XlsxA1RangeReferenceResult parsed = XlsxA1RangeReferenceConverter.Parse(reference);
            foreach (XlsxDiagnostic diagnostic in parsed.Validation.Diagnostics)
            {
                result.AddDiagnostic(diagnostic);
            }
        }
        return result.IsValid;
    }

    /// <summary>
    /// Adds sparkline to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "cellReference">An A1-style cell or range reference.</param>
    /// <param name = "dataRange">The data range value used by the operation.</param>
    /// <param name = "type">The type value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxSparklineOperationResult AddSparkline(string cellReference, string dataRange, string type)
    {
        XlsxSparklineOperationResult result = new XlsxSparklineOperationResult();
        if (SparklineGroups == null)
        {
            result.AddError("sparkline.groupsNull", "Worksheet sparkline group list must not be null.");
            return result;
        }

        XlsxA1CellReferenceResult cell = XlsxA1CellReferenceConverter.Parse(cellReference);
        if (!cell.IsValid)
        {
            foreach (XlsxDiagnostic diagnostic in cell.Validation.Diagnostics)
            {
                result.AddDiagnostic(diagnostic);
            }

            return result;
        }

        string formula = XlsxCellValueConverter.NormalizeText(dataRange);
        if (formula.Length == 0)
        {
            result.AddError("sparkline.formulaMissing", "Sparkline data range formula must not be empty.");
            return result;
        }

        string sparklineType = XlsxCellValueConverter.NormalizeText(type);
        if (sparklineType.Length == 0)
        {
            sparklineType = "line";
        }

        if (!XlsxSparklineGroupsWriter.IsSupportedSparklineType(sparklineType))
        {
            result.AddError("sparkline.typeUnsupported", "Sparkline type must be line, column, or stacked.");
            return result;
        }

        XlsxSparklineGroup? group = GetSparklineGroupByType(sparklineType);
        if (group == null)
        {
            group = new XlsxSparklineGroup();
            group.Type = sparklineType;
            SparklineGroups.Add(group);
        }

        group.RawXml = "";
        XlsxSparkline sparkline = new XlsxSparkline();
        sparkline.CellReference = XlsxCellValueConverter.NormalizeText(cellReference);
        sparkline.Formula = formula;
        group.Sparklines.Add(sparkline);
        HasSparklineGroups = true;
        if (ExtensionList != null)
        {
            ExtensionList.RawXml = "";
        }

        result.Group = group;
        result.HasGroup = true;
        result.Sparkline = sparkline;
        result.HasSparkline = true;
        return result;
    }

    /// <summary>
    /// Returns sparkline group by type from the current model.
    /// </summary>
    /// <param name = "type">The type value used by the operation.</param>
    /// <returns>The sparkline group produced by the operation.</returns>
    internal XlsxSparklineGroup? GetSparklineGroupByType(string type)
    {
        if (SparklineGroups == null)
        {
            return null;
        }

        foreach (XlsxSparklineGroup group in SparklineGroups)
        {
            if (group != null && group.Type == type)
            {
                return group;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns row from the current model.
    /// </summary>
    /// <param name = "rowIndex">A one-based worksheet row index.</param>
    /// <returns>The worksheet row produced by the operation.</returns>
    internal XlsxWorksheetRow? GetRowByUnsignedIndex(uint rowIndex)
    {
        return GetRow((int)rowIndex);
    }

    /// <summary>
    /// Returns row from the current model.
    /// </summary>
    /// <param name = "rowIndex">A one-based worksheet row index.</param>
    /// <returns>The worksheet row produced by the operation.</returns>
    internal XlsxWorksheetRow? GetRow(int rowIndex)
    {
        if (!IsValidCellRow(rowIndex) || Rows == null)
        {
            return null;
        }

        int sequence = 1;
        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row != null && GetEffectiveRowIndex(row, sequence) == rowIndex)
            {
                return row;
            }

            sequence = sequence + 1;
        }

        return null;
    }

    /// <summary>
    /// Returns cell from the current model.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>The worksheet cell produced by the operation.</returns>
    internal XlsxWorksheetCell? GetCell(string? reference)
    {
        return GetCellByReference(reference);
    }

    /// <summary>
    /// Returns cell by reference from the current model.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>The worksheet cell produced by the operation.</returns>
    internal XlsxWorksheetCell? GetCellByReference(string? reference)
    {
        XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(reference);
        if (!referenceResult.IsValid)
        {
            return null;
        }

        return GetCellAt(referenceResult.Row, referenceResult.Column);
    }

    /// <summary>
    /// Returns cell from the current model.
    /// </summary>
    /// <param name = "rowIndex">A one-based worksheet row index.</param>
    /// <param name = "columnIndex">A one-based worksheet column index.</param>
    /// <returns>The worksheet cell produced by the operation.</returns>
    internal XlsxWorksheetCell? GetCellAt(int rowIndex, int columnIndex)
    {
        if (!IsValidCellCoordinate(rowIndex, columnIndex) || Rows == null)
        {
            return null;
        }

        int rowSequence = 1;
        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row != null && row.Cells != null)
            {
                XlsxWorksheetCell? cell = GetCellInRow(row, rowSequence, rowIndex, columnIndex);
                if (cell != null)
                {
                    return cell;
                }
            }

            rowSequence = rowSequence + 1;
        }

        return null;
    }

    /// <summary>
    /// Performs the contains cell operation using the current model state.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns><c>true</c> when the requested condition is met; otherwise, <c>false</c>.</returns>
    internal bool ContainsCell(string reference)
    {
        return GetCellByReference(reference) != null;
    }

    /// <summary>
    /// Performs the contains cell operation using the current model state.
    /// </summary>
    /// <param name = "rowIndex">A one-based worksheet row index.</param>
    /// <param name = "columnIndex">A one-based worksheet column index.</param>
    /// <returns><c>true</c> when the requested condition is met; otherwise, <c>false</c>.</returns>
    internal bool ContainsCellAt(int rowIndex, int columnIndex)
    {
        return GetCellAt(rowIndex, columnIndex) != null;
    }

    /// <summary>
    /// Returns range from the current model.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>The worksheet range produced by the operation.</returns>
    internal XlsxWorksheetRange GetRange(string reference)
    {
        return new XlsxWorksheetRange(this, reference);
    }

    /// <summary>
    /// Performs the range operation using the current model state.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>The worksheet range produced by the operation.</returns>
    internal XlsxWorksheetRange Range(string reference)
    {
        return GetRange(reference);
    }

    /// <summary>
    /// Performs the import data table operation using the current model state.
    /// </summary>
    /// <param name = "topLeftCell">The top left cell value used by the operation.</param>
    /// <param name = "dataTable">The data table value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxDataTableImportResult ImportDataTable(string topLeftCell, System.Data.DataTable? dataTable)
    {
        return ImportDataTableWithHeaders(topLeftCell, dataTable, true);
    }

    /// <summary>
    /// Performs the import data table operation using the current model state.
    /// </summary>
    /// <param name = "topLeftCell">The top left cell value used by the operation.</param>
    /// <param name = "dataTable">The data table value used by the operation.</param>
    /// <param name = "includeHeaders">The include headers value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxDataTableImportResult ImportDataTableWithHeaders(string topLeftCell, System.Data.DataTable? dataTable, bool includeHeaders)
    {
        XlsxDataTableImportResult result = ValidateDataTableImport(topLeftCell, dataTable, includeHeaders);
        if (!result.IsValid || dataTable == null)
        {
            return result;
        }

        WriteDataTableImport(result, topLeftCell, dataTable, includeHeaders);
        return result;
    }

    /// <summary>
    /// Performs the import data table as table operation using the current model state.
    /// </summary>
    /// <param name = "topLeftCell">The top left cell value used by the operation.</param>
    /// <param name = "dataTable">The data table value used by the operation.</param>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxDataTableImportResult ImportDataTableAsTable(string topLeftCell, System.Data.DataTable? dataTable, string tableName)
    {
        return ImportDataTableAsTableWithHeaders(topLeftCell, dataTable, tableName, true);
    }

    /// <summary>
    /// Performs the import data table as table operation using the current model state.
    /// </summary>
    /// <param name = "topLeftCell">The top left cell value used by the operation.</param>
    /// <param name = "dataTable">The data table value used by the operation.</param>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "includeHeaders">The include headers value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxDataTableImportResult ImportDataTableAsTableWithHeaders(string topLeftCell, System.Data.DataTable? dataTable, string tableName, bool includeHeaders)
    {
        XlsxDataTableImportResult result = ValidateDataTableImport(topLeftCell, dataTable, includeHeaders);
        if (!result.IsValid || dataTable == null)
        {
            return result;
        }

        XlsxWorksheetTableResult validation = ValidateNewTable(result.Reference, tableName);
        XlsxDataTableImportResult.AddTableDiagnostics(result, validation);
        if (!result.IsValid)
        {
            return result;
        }

        WriteDataTableImport(result, topLeftCell, dataTable, includeHeaders);
        XlsxWorksheetTableResult tableResult = AddTableWithHeaderRow(result.Reference, tableName, includeHeaders);
        XlsxDataTableImportResult.AddTableDiagnostics(result, tableResult);
        if (tableResult.IsValid)
        {
            result.Table = tableResult.Table;
            result.HasTable = true;
        }

        return result;
    }

    /// <summary>
    /// Performs the export data table operation using the current model state.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxDataTableExportResult ExportDataTable(string reference)
    {
        return ExportDataTableWithHeaders(reference, true);
    }

    /// <summary>
    /// Performs the export data table operation using the current model state.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "firstRowAsHeaders">The first row as headers value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxDataTableExportResult ExportDataTableWithHeaders(string reference, bool firstRowAsHeaders)
    {
        XlsxDataTableExportResult result = ValidateDataTableExportRange(reference, firstRowAsHeaders);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(reference);
        NormalizeRangeBounds(range);
        System.Data.DataTable dataTable = new System.Data.DataTable();
        AddExportRangeColumns(dataTable, range, firstRowAsHeaders);
        int firstDataRow = range.First.Row;
        if (firstRowAsHeaders)
        {
            firstDataRow = firstDataRow + 1;
            result.HeaderRowCount = 1;
        }

        AddExportRows(result, dataTable, firstDataRow, range.First.Column, range.Last.Row, range.Last.Column);
        result.DataTable = dataTable;
        result.HasDataTable = true;
        result.Reference = FormatRange(range);
        result.ColumnCount = dataTable.Columns.Count;
        result.DataRowCount = dataTable.Rows.Count;
        return result;
    }

    /// <summary>
    /// Performs the import csv operation using the current model state.
    /// </summary>
    /// <param name = "topLeftCell">The top left cell value used by the operation.</param>
    /// <param name = "csv">The csv value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxCsvImportResult ImportCsv(string topLeftCell, string csv)
    {
        return ImportCsvWithOptions(topLeftCell, csv, new XlsxCsvOptions());
    }

    /// <summary>
    /// Performs the import csv operation using the current model state.
    /// </summary>
    /// <param name = "topLeftCell">The top left cell value used by the operation.</param>
    /// <param name = "csv">The csv value used by the operation.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxCsvImportResult ImportCsvWithOptions(string topLeftCell, string csv, XlsxCsvOptions options)
    {
        XlsxCsvImportResult result = ValidateCsvImport(topLeftCell, csv, options);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxCsvOptions effectiveOptions = NormalizeCsvOptions(options);
        XlsxCsvParseResult parseResult = ParseCsv(csv, effectiveOptions);
        XlsxCsvImportResult.AddParseDiagnostics(result, parseResult);
        if (!result.IsValid)
        {
            return result;
        }

        PopulateCsvImportRange(result, topLeftCell, parseResult.Rows);
        if (!result.IsValid)
        {
            return result;
        }

        WriteCsvImport(result, topLeftCell, parseResult.Rows);
        return result;
    }

    /// <summary>
    /// Performs the export csv operation using the current model state.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxCsvExportResult ExportCsv(string reference)
    {
        return ExportCsvWithOptions(reference, new XlsxCsvOptions());
    }

    /// <summary>
    /// Performs the export csv operation using the current model state.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxCsvExportResult ExportCsvWithOptions(string reference, XlsxCsvOptions options)
    {
        XlsxCsvExportResult result = ValidateCsvExportRange(reference, options);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxCsvOptions effectiveOptions = NormalizeCsvOptions(options);
        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(reference);
        NormalizeRangeBounds(range);
        StringBuilder builder = new StringBuilder();
        int rowIndex = range.First.Row;
        while (rowIndex <= range.Last.Row)
        {
            if (rowIndex > range.First.Row)
            {
                builder.Append(effectiveOptions.NewLine);
            }

            int columnIndex = range.First.Column;
            while (columnIndex <= range.Last.Column)
            {
                if (columnIndex > range.First.Column)
                {
                    builder.Append(effectiveOptions.Delimiter);
                }

                WriteCsvField(builder, GetCsvCellText(rowIndex, columnIndex), effectiveOptions);
                result.ExportedCellCount = result.ExportedCellCount + 1;
                columnIndex = columnIndex + 1;
            }

            result.ExportedRowCount = result.ExportedRowCount + 1;
            rowIndex = rowIndex + 1;
        }

        result.Csv = builder.ToString();
        result.HasCsv = true;
        result.Reference = FormatRange(range);
        result.RowCount = GetRangeRowCount(range);
        result.ColumnCount = GetRangeColumnCount(range);
        return result;
    }

    /// <summary>
    /// Performs the export table to data table operation using the current model state.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxDataTableExportResult ExportTableToDataTable(string tableName)
    {
        XlsxWorksheetTableResult tableResult = ValidateExistingTable(tableName);
        XlsxDataTableExportResult result = new XlsxDataTableExportResult();
        XlsxDataTableExportResult.AddTableDiagnostics(result, tableResult);
        if (!result.IsValid || tableResult.Table == null)
        {
            return result;
        }

        XlsxTablePart? table = tableResult.Table;
        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(table.Reference);
        if (!range.IsValid)
        {
            XlsxDataTableExportResult.AddRangeDiagnostics(result, range);
            return result;
        }

        NormalizeRangeBounds(range);
        System.Data.DataTable dataTable = new System.Data.DataTable();
        AddExportTableColumns(dataTable, table);
        int firstDataRow = range.First.Row;
        if (TableHasHeaderRow(table))
        {
            firstDataRow = firstDataRow + 1;
            result.HeaderRowCount = 1;
        }

        int lastDataRow = range.Last.Row;
        if (TableHasTotalsRow(table))
        {
            lastDataRow = lastDataRow - 1;
        }

        AddExportRows(result, dataTable, firstDataRow, range.First.Column, lastDataRow, range.First.Column + dataTable.Columns.Count - 1);
        result.DataTable = dataTable;
        result.HasDataTable = true;
        result.Reference = table.Reference;
        result.ColumnCount = dataTable.Columns.Count;
        result.DataRowCount = dataTable.Rows.Count;
        return result;
    }

    /// <summary>
    /// Returns table from the current model.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <returns>The table part produced by the operation.</returns>
    internal XlsxTablePart? GetTable(string name)
    {
        if (Tables == null)
        {
            return null;
        }

        string normalizedName = NormalizeTableName(name);
        foreach (XlsxTablePart table in Tables)
        {
            if (table != null && (TableNamesEqual(table.Name, normalizedName) || TableNamesEqual(table.DisplayName, normalizedName)))
            {
                return table;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds table to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "name">The name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult AddTable(string reference, string name)
    {
        return AddTableWithHeaderRow(reference, name, true);
    }

    /// <summary>
    /// Adds table to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "hasHeaderRow">The has header row value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult AddTableWithHeaderRow(string reference, string name, bool hasHeaderRow)
    {
        XlsxWorksheetTableResult result = ValidateNewTable(reference, name);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(reference);
        XlsxTablePart table = new XlsxTablePart();
        table.RawXml = "";
        table.PartPath = XlsxPackagePaths.TablesDirectory + "table" + GetNextTableId().ToString(CultureInfo.InvariantCulture) + ".xml";
        table.Id = GetNextTableId();
        table.HasId = true;
        table.Name = NormalizeTableName(name);
        table.DisplayName = table.Name;
        table.Reference = FormatRange(range);
        table.HeaderRowCount = hasHeaderRow ? 1u : 0u;
        table.HasHeaderRowCount = true;
        table.TotalsRowCount = 0;
        table.HasTotalsRowCount = true;
        table.TotalsRowShown = false;
        table.HasTotalsRowShown = true;
        table.HasStyleInfo = true;
        table.StyleInfo.Name = "TableStyleMedium2";
        table.StyleInfo.ShowRowStripes = true;
        table.StyleInfo.HasShowRowStripes = true;
        AddGeneratedTableColumns(result, table, range, hasHeaderRow);
        if (!result.IsValid)
        {
            return result;
        }

        table.TableColumnCount = (uint)table.Columns.Count;
        table.HasTableColumnCount = true;
        if (hasHeaderRow)
        {
            table.AutoFilterEnabled = true;
            RefreshTableAutoFilterAndSortRawXml(table);
        }

        XlsxWorksheetTableReference referenceItem = new XlsxWorksheetTableReference();
        referenceItem.RelationshipId = GetNextTableRelationshipId();
        Tables.Add(table);
        TableReferences.Add(referenceItem);
        result.Table = table;
        result.HasTable = true;
        result.TableReference = referenceItem;
        result.HasTableReference = true;
        return result;
    }

    /// <summary>
    /// Performs the resize table operation using the current model state.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult ResizeTable(string tableName, string reference)
    {
        XlsxWorksheetTableResult result = ValidateExistingTableAndRange(tableName, reference);
        if (!result.IsValid || result.Table == null)
        {
            return result;
        }

        XlsxTablePart? table = result.Table;
        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(reference);
        int newColumnCount = GetRangeColumnCount(range);
        TrimOrExtendTableColumns(result, table, range, newColumnCount);
        if (!result.IsValid)
        {
            return result;
        }

        table.RawXml = "";
        table.Reference = FormatRange(range);
        table.TableColumnCount = (uint)table.Columns.Count;
        table.HasTableColumnCount = true;
        WriteTableHeaderCells(result, table);
        ApplyTableColumnFormulas(result, table);
        ApplyTableTotalsRowCells(result, table);
        RefreshTableAutoFilterAndSortRawXml(table);
        return result;
    }

    /// <summary>
    /// Performs the rename table operation using the current model state.
    /// </summary>
    /// <param name = "oldName">The old name value used by the operation.</param>
    /// <param name = "newName">The new name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult RenameTable(string oldName, string newName)
    {
        XlsxWorksheetTableResult result = ValidateExistingTable(oldName);
        if (!result.IsValid || result.Table == null)
        {
            return result;
        }

        XlsxValidationResult nameValidation = ValidateTableName(newName);
        CopyDiagnostics(result, nameValidation);
        if (GetTable(newName) != null && !TableNamesEqual(oldName, newName))
        {
            result.AddError("worksheetTable.nameDuplicate", "Worksheet table names must be unique.");
        }

        if (!result.IsValid)
        {
            return result;
        }

        string previousName = result.Table.Name;
        string replacementName = NormalizeTableName(newName);
        result.Table.RawXml = "";
        result.Table.Name = replacementName;
        result.Table.DisplayName = result.Table.Name;
        UpdateStructuredReferencesForTableRename(result, previousName, replacementName);
        return result;
    }

    internal void UpdateStructuredReferencesForTableRename(XlsxWorksheetTableResult result, string oldName, string newName)
    {
        foreach (XlsxWorksheetRow row in Rows)
        {
            foreach (XlsxWorksheetCell cell in row.Cells)
            {
                if (!cell.HasFormula && XlsxCellValueConverter.NormalizeText(cell.Formula).Length == 0)
                {
                    continue;
                }

                string previous = GetFormulaText(cell);
                string updated = RenameStructuredTableFormula(previous, oldName, newName);
                if (updated == previous)
                {
                    continue;
                }

                cell.Formula = updated;
                if (cell.FormulaInfo != null)
                {
                    cell.FormulaInfo.Text = updated;
                }
                result.UpdatedCellCount = result.UpdatedCellCount + 1;
                result.FormulaCellCount = result.FormulaCellCount + 1;
            }
        }

        foreach (XlsxTablePart table in Tables)
        {
            bool tableChanged = false;
            foreach (XlsxTableColumn column in table.Columns)
            {
                string calculated = RenameStructuredTableFormula(column.CalculatedColumnFormula, oldName, newName);
                string total = RenameStructuredTableFormula(column.TotalsRowFormula, oldName, newName);
                if (calculated != column.CalculatedColumnFormula)
                {
                    column.CalculatedColumnFormula = calculated;
                    column.CalculatedColumnFormulaRawXml = "";
                    tableChanged = true;
                }
                if (total != column.TotalsRowFormula)
                {
                    column.TotalsRowFormula = total;
                    column.TotalsRowFormulaRawXml = "";
                    tableChanged = true;
                }
            }
            if (tableChanged)
            {
                table.RawXml = "";
            }
        }

        if (DataValidations != null && DataValidations.Items != null)
        {
            bool validationsChanged = false;
            foreach (XlsxDataValidation validation in DataValidations.Items)
            {
                string first = RenameStructuredTableFormula(validation.Formula1, oldName, newName);
                string second = RenameStructuredTableFormula(validation.Formula2, oldName, newName);
                if (first != validation.Formula1 || second != validation.Formula2)
                {
                    validation.Formula1 = first;
                    validation.Formula2 = second;
                    validationsChanged = true;
                }
            }
            if (validationsChanged)
            {
                DataValidations.RawXml = "";
            }
        }

        if (ConditionalFormattings != null)
        {
            foreach (XlsxConditionalFormatting formatting in ConditionalFormattings)
            {
                bool formattingChanged = false;
                foreach (XlsxConditionalFormatRule rule in formatting.Rules)
                {
                    int index = 0;
                    while (index < rule.Formulas.Count)
                    {
                        string updated = RenameStructuredTableFormula(rule.Formulas[index], oldName, newName);
                        if (updated != rule.Formulas[index])
                        {
                            rule.Formulas[index] = updated;
                            formattingChanged = true;
                        }
                        index = index + 1;
                    }
                }
                if (formattingChanged)
                {
                    formatting.RawXml = "";
                }
            }
        }
    }

    internal static string RenameStructuredTableFormula(string formula, string oldName, string newName)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(formula);
        XlsxFormulaTokenizeResult tokens = new XlsxFormulaTokenizer().Tokenize(normalized);
        if (!tokens.IsValid)
        {
            return normalized;
        }

        StringBuilder builder = new StringBuilder();
        int previousEnd = 0;
        int index = 0;
        while (index + 1 < tokens.Tokens.Count)
        {
            XlsxFormulaToken token = tokens.Tokens[index];
            XlsxFormulaToken next = tokens.Tokens[index + 1];
            if (token.Kind == XlsxFormulaTokenKind.Identifier && next.Kind == XlsxFormulaTokenKind.StructuredReference && token.Start + token.Length == next.Start && string.Equals(token.Text, oldName, StringComparison.OrdinalIgnoreCase))
            {
                builder.Append(normalized, previousEnd, token.Start - previousEnd);
                builder.Append(newName);
                previousEnd = token.Start + token.Length;
            }
            index = index + 1;
        }

        if (previousEnd == 0)
        {
            return normalized;
        }

        builder.Append(normalized, previousEnd, normalized.Length - previousEnd);
        return builder.ToString();
    }

    /// <summary>
    /// Adds table column to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult AddTableColumn(string tableName, string columnName)
    {
        XlsxWorksheetTableResult result = ValidateExistingTable(tableName);
        if (!result.IsValid || result.Table == null)
        {
            return result;
        }

        XlsxTablePart? table = result.Table;
        XlsxValidationResult nameValidation = ValidateTableColumnName(columnName);
        CopyDiagnostics(result, nameValidation);
        if (FindTableColumn(table, columnName) != null)
        {
            result.AddError("worksheetTable.columnNameDuplicate", "Table column names must be unique within a table.");
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(table.Reference);
        if (!range.IsValid)
        {
            XlsxWorksheetTableResult.AddRangeDiagnostics(result, range);
        }

        if (!result.IsValid)
        {
            return result;
        }

        table.RawXml = "";
        range.Last.Column = range.Last.Column + 1;
        range.HasLast = true;
        table.Reference = FormatRange(range);
        XlsxTableColumn column = CreateTableColumn(GetNextTableColumnId(table), NormalizeTableColumnName(columnName));
        table.Columns.Add(column);
        table.TableColumnCount = (uint)table.Columns.Count;
        table.HasTableColumnCount = true;
        WriteTableHeaderCells(result, table);
        ApplyTableColumnFormulas(result, table);
        ApplyTableTotalsRowCells(result, table);
        RefreshTableAutoFilterAndSortRawXml(table);
        result.Column = column;
        result.HasColumn = true;
        result.AddedColumnCount = 1;
        return result;
    }

    /// <summary>
    /// Performs the rename table column operation using the current model state.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "oldColumnName">The old column name value used by the operation.</param>
    /// <param name = "newColumnName">The new column name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult RenameTableColumn(string tableName, string oldColumnName, string newColumnName)
    {
        XlsxWorksheetTableResult result = ValidateExistingTableAndColumn(tableName, oldColumnName);
        if (!result.IsValid || result.Table == null || result.Column == null)
        {
            return result;
        }

        XlsxTablePart? table = result.Table;
        XlsxValidationResult nameValidation = ValidateTableColumnName(newColumnName);
        CopyDiagnostics(result, nameValidation);
        XlsxTableColumn? existing = FindTableColumn(table, newColumnName);
        if (existing != null && existing != result.Column)
        {
            result.AddError("worksheetTable.columnNameDuplicate", "Table column names must be unique within a table.");
        }

        if (!result.IsValid)
        {
            return result;
        }

        table.RawXml = "";
        result.Column.Name = NormalizeTableColumnName(newColumnName);
        if (TableNamesEqual(table.SortColumnName, oldColumnName))
        {
            table.SortColumnName = result.Column.Name;
        }

        WriteTableHeaderCells(result, table);
        ApplyTableTotalsRowCells(result, table);
        RefreshTableAutoFilterAndSortRawXml(table);
        return result;
    }

    /// <summary>
    /// Performs the delete table column operation using the current model state.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult DeleteTableColumn(string tableName, string columnName)
    {
        XlsxWorksheetTableResult result = ValidateExistingTableAndColumn(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.Column == null)
        {
            return result;
        }

        XlsxTablePart? table = result.Table;
        if (table.Columns.Count <= 1)
        {
            result.AddError("worksheetTable.removeLastColumn", "Table must retain at least one column.");
            return result;
        }

        int index = GetTableColumnIndex(table, columnName);
        table.RawXml = "";
        if (table.SortStateEnabled && TableNamesEqual(table.SortColumnName, columnName))
        {
            table.SortStateEnabled = false;
            table.SortStateRawXml = "";
            table.SortColumnName = "";
        }

        table.Columns.RemoveAt(index);
        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(table.Reference);
        if (range.IsValid && range.Last.Column > range.First.Column)
        {
            range.Last.Column = range.Last.Column - 1;
            table.Reference = FormatRange(range);
        }

        table.TableColumnCount = (uint)table.Columns.Count;
        table.HasTableColumnCount = true;
        WriteTableHeaderCells(result, table);
        ApplyTableColumnFormulas(result, table);
        ApplyTableTotalsRowCells(result, table);
        RefreshTableAutoFilterAndSortRawXml(table);
        result.RemovedColumnCount = 1;
        return result;
    }

    /// <summary>
    /// Sets table header row and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "visible">The visible value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult SetTableHeaderRow(string tableName, bool visible)
    {
        XlsxWorksheetTableResult result = ValidateExistingTable(tableName);
        if (!result.IsValid || result.Table == null)
        {
            return result;
        }

        result.Table.RawXml = "";
        result.Table.HeaderRowCount = visible ? 1u : 0u;
        result.Table.HasHeaderRowCount = true;
        result.Table.AutoFilterEnabled = visible && result.Table.AutoFilterEnabled;
        if (!visible)
        {
            result.Table.AutoFilterRawXml = "";
            result.Table.SortStateEnabled = false;
            result.Table.SortStateRawXml = "";
        }

        if (visible)
        {
            WriteTableHeaderCells(result, result.Table);
        }

        ApplyTableColumnFormulas(result, result.Table);
        ApplyTableTotalsRowCells(result, result.Table);
        RefreshTableAutoFilterAndSortRawXml(result.Table);
        return result;
    }

    /// <summary>
    /// Sets table totals row and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "visible">The visible value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult SetTableTotalsRow(string tableName, bool visible)
    {
        XlsxWorksheetTableResult result = ValidateExistingTable(tableName);
        if (!result.IsValid || result.Table == null)
        {
            return result;
        }

        XlsxTablePart? table = result.Table;
        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(table.Reference);
        if (!range.IsValid)
        {
            XlsxWorksheetTableResult.AddRangeDiagnostics(result, range);
            return result;
        }

        bool currentlyVisible = TableHasTotalsRow(table);
        table.RawXml = "";
        if (visible && !currentlyVisible && range.Last.Row < XlsxXmlValueParser.MaxSpreadsheetRows)
        {
            range.Last.Row = range.Last.Row + 1;
            table.Reference = FormatRange(range);
        }
        else if (!visible && currentlyVisible && range.Last.Row > range.First.Row)
        {
            ClearTableTotalsRowCells(result, table);
            range.Last.Row = range.Last.Row - 1;
            table.Reference = FormatRange(range);
        }

        table.TotalsRowCount = visible ? 1u : 0u;
        table.HasTotalsRowCount = true;
        table.TotalsRowShown = visible;
        table.HasTotalsRowShown = true;
        ApplyTableColumnFormulas(result, table);
        ApplyTableTotalsRowCells(result, table);
        RefreshTableAutoFilterAndSortRawXml(table);
        return result;
    }

    /// <summary>
    /// Sets table column formula and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult SetTableColumnFormula(string tableName, string columnName, string formula)
    {
        XlsxWorksheetTableResult result = ValidateExistingTableAndColumn(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.Column == null)
        {
            return result;
        }

        result.Table.RawXml = "";
        result.Column.CalculatedColumnFormula = XlsxCellValueConverter.NormalizeText(formula);
        result.Column.CalculatedColumnFormulaRawXml = "";
        ApplySingleTableColumnFormula(result, result.Table, result.Column, GetTableColumnIndex(result.Table, columnName));
        return result;
    }

    /// <summary>
    /// Sets table column totals row formula and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult SetTableColumnTotalsRowFormula(string tableName, string columnName, string formula)
    {
        XlsxWorksheetTableResult result = ValidateExistingTableAndColumn(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.Column == null)
        {
            return result;
        }

        result.Table.RawXml = "";
        result.Column.TotalsRowFormula = XlsxCellValueConverter.NormalizeText(formula);
        result.Column.TotalsRowFormulaRawXml = "";
        ApplyTableTotalsRowCells(result, result.Table);
        return result;
    }

    /// <summary>
    /// Sets table column totals row function and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <param name = "functionName">The function name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult SetTableColumnTotalsRowFunction(string tableName, string columnName, string functionName)
    {
        XlsxWorksheetTableResult result = ValidateExistingTableAndColumn(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.Column == null)
        {
            return result;
        }

        result.Table.RawXml = "";
        result.Column.TotalsRowFunction = XlsxCellValueConverter.NormalizeText(functionName);
        return result;
    }

    /// <summary>
    /// Sets table column totals row label and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <param name = "label">The label value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult SetTableColumnTotalsRowLabel(string tableName, string columnName, string label)
    {
        XlsxWorksheetTableResult result = ValidateExistingTableAndColumn(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.Column == null)
        {
            return result;
        }

        result.Table.RawXml = "";
        result.Column.TotalsRowLabel = XlsxCellValueConverter.NormalizeText(label);
        ApplyTableTotalsRowCells(result, result.Table);
        return result;
    }

    /// <summary>
    /// Sets table auto filter and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "enabled">The enabled value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult SetTableAutoFilter(string tableName, bool enabled)
    {
        XlsxWorksheetTableResult result = ValidateExistingTable(tableName);
        if (!result.IsValid || result.Table == null)
        {
            return result;
        }

        XlsxTablePart? table = result.Table;
        if (enabled && !TableHasHeaderRow(table))
        {
            result.AddError("worksheetTable.autoFilterHeaderMissing", "Table auto filter requires a visible header row.");
            return result;
        }

        table.RawXml = "";
        table.AutoFilterEnabled = enabled;
        if (!enabled)
        {
            table.AutoFilterRawXml = "";
            table.SortStateEnabled = false;
            table.SortStateRawXml = "";
            table.SortColumnName = "";
        }
        else
        {
            RefreshTableAutoFilterAndSortRawXml(table);
        }

        return result;
    }

    /// <summary>
    /// Clears table auto filter while retaining the surrounding model object.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult ClearTableAutoFilter(string tableName)
    {
        return SetTableAutoFilter(tableName, false);
    }

    /// <summary>
    /// Sets table sort state and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <param name = "descending">The descending value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult SetTableSortState(string tableName, string columnName, bool descending)
    {
        XlsxWorksheetTableResult result = ValidateExistingTableAndColumn(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.Column == null)
        {
            return result;
        }

        XlsxTablePart? table = result.Table;
        if (!TableHasHeaderRow(table))
        {
            result.AddError("worksheetTable.sortHeaderMissing", "Table sort state requires a visible header row.");
            return result;
        }

        string dataReference = GetTableDataBodyReference(table);
        if (dataReference.Length == 0)
        {
            result.AddError("worksheetTable.sortRangeEmpty", "Table sort state requires at least one data row.");
            return result;
        }

        table.RawXml = "";
        table.AutoFilterEnabled = true;
        table.SortStateEnabled = true;
        table.SortColumnName = result.Column.Name;
        table.SortDescending = descending;
        RefreshTableAutoFilterAndSortRawXml(table);
        return result;
    }

    /// <summary>
    /// Clears table sort state while retaining the surrounding model object.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult ClearTableSortState(string tableName)
    {
        XlsxWorksheetTableResult result = ValidateExistingTable(tableName);
        if (!result.IsValid || result.Table == null)
        {
            return result;
        }

        result.Table.RawXml = "";
        result.Table.SortStateEnabled = false;
        result.Table.SortStateRawXml = "";
        result.Table.SortColumnName = "";
        result.Table.SortDescending = false;
        return result;
    }

    /// <summary>
    /// Adds auto filter to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult AddAutoFilter(string reference)
    {
        XlsxWorksheetFilterResult result = new XlsxWorksheetFilterResult();
        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(reference);
        if (!range.IsValid)
        {
            XlsxWorksheetFilterResult.AddRangeDiagnostics(result, range);
            return result;
        }

        AutoFilter = new XlsxAutoFilter();
        AutoFilter.NamespaceUri = NormalizeSpreadsheetNamespace(NamespaceUri);
        AutoFilter.Reference = FormatRange(range);
        AutoFilter.RawXml = "";
        HasAutoFilter = true;
        result.AutoFilter = AutoFilter;
        result.HasAutoFilter = true;
        return result;
    }

    /// <summary>
    /// Clears auto filter while retaining the surrounding model object.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult ClearAutoFilter()
    {
        XlsxWorksheetFilterResult result = new XlsxWorksheetFilterResult();
        AutoFilter = new XlsxAutoFilter();
        HasAutoFilter = false;
        return result;
    }

    /// <summary>
    /// Clears auto filter column while retaining the surrounding model object.
    /// </summary>
    /// <param name = "columnId">The column id value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult ClearAutoFilterColumn(uint columnId)
    {
        XlsxWorksheetFilterResult result = ValidateWorksheetAutoFilterColumn(columnId);
        if (!result.IsValid || result.AutoFilter == null)
        {
            return result;
        }

        RemoveAutoFilterColumn(result.AutoFilter, columnId);
        result.AutoFilter.RawXml = "";
        return result;
    }

    /// <summary>
    /// Sets auto filter values and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "columnId">The column id value used by the operation.</param>
    /// <param name = "values">The values value used by the operation.</param>
    /// <param name = "includeBlanks">The include blanks value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetAutoFilterValues(uint columnId, List<string> values, bool includeBlanks)
    {
        XlsxWorksheetFilterResult result = ValidateWorksheetAutoFilterColumn(columnId);
        if (!result.IsValid || result.AutoFilter == null)
        {
            return result;
        }

        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, columnId);
        ClearColumnFilterTypes(column);
        column.HasValues = true;
        column.Values = new XlsxAutoFilterValues();
        column.Values.Blank = includeBlanks;
        column.Values.HasBlank = true;
        if (values != null)
        {
            foreach (string value in values)
            {
                column.Values.Values.Add(XlsxCellValueConverter.NormalizeText(value));
            }
        }

        result.FilterColumn = column;
        result.HasFilterColumn = true;
        result.AutoFilter.RawXml = "";
        return result;
    }

    /// <summary>
    /// Sets auto filter custom and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "columnId">The column id value used by the operation.</param>
    /// <param name = "firstOperator">The first operator value used by the operation.</param>
    /// <param name = "firstValue">The first value value used by the operation.</param>
    /// <param name = "secondOperator">The second operator value used by the operation.</param>
    /// <param name = "secondValue">The second value value used by the operation.</param>
    /// <param name = "andOperator">The and operator value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetAutoFilterCustom(uint columnId, string firstOperator, string firstValue, string secondOperator, string secondValue, bool andOperator)
    {
        XlsxWorksheetFilterResult result = ValidateWorksheetAutoFilterColumn(columnId);
        if (!result.IsValid || result.AutoFilter == null)
        {
            return result;
        }

        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, columnId);
        ApplyCustomFilter(column, firstOperator, firstValue, secondOperator, secondValue, andOperator);
        result.FilterColumn = column;
        result.HasFilterColumn = true;
        result.AutoFilter.RawXml = "";
        return result;
    }

    /// <summary>
    /// Sets auto filter dynamic and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "columnId">The column id value used by the operation.</param>
    /// <param name = "filterType">The filter type value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetAutoFilterDynamic(uint columnId, string filterType)
    {
        XlsxWorksheetFilterResult result = ValidateWorksheetAutoFilterColumn(columnId);
        if (!result.IsValid || result.AutoFilter == null)
        {
            return result;
        }

        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, columnId);
        ClearColumnFilterTypes(column);
        column.HasDynamicFilter = true;
        column.DynamicFilter = new XlsxDynamicFilter();
        column.DynamicFilter.Type = XlsxCellValueConverter.NormalizeText(filterType);
        result.FilterColumn = column;
        result.HasFilterColumn = true;
        result.AutoFilter.RawXml = "";
        return result;
    }

    /// <summary>
    /// Sets auto filter date group and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "columnId">The column id value used by the operation.</param>
    /// <param name = "year">The year value used by the operation.</param>
    /// <param name = "month">The month value used by the operation.</param>
    /// <param name = "day">The day value used by the operation.</param>
    /// <param name = "grouping">The grouping value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetAutoFilterDateGroup(uint columnId, uint year, uint month, uint day, string grouping)
    {
        XlsxWorksheetFilterResult result = ValidateWorksheetAutoFilterColumn(columnId);
        if (!result.IsValid || result.AutoFilter == null)
        {
            return result;
        }

        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, columnId);
        ClearColumnFilterTypes(column);
        column.HasValues = true;
        column.Values = new XlsxAutoFilterValues();
        XlsxDateGroupItem item = new XlsxDateGroupItem();
        item.Year = year;
        item.HasYear = true;
        item.Month = month;
        item.HasMonth = true;
        item.Day = day;
        item.HasDay = true;
        item.DateTimeGrouping = XlsxCellValueConverter.NormalizeText(grouping);
        column.Values.DateGroupItems.Add(item);
        result.FilterColumn = column;
        result.HasFilterColumn = true;
        result.AutoFilter.RawXml = "";
        return result;
    }

    /// <summary>
    /// Sets auto filter top10 and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "columnId">The column id value used by the operation.</param>
    /// <param name = "value">The value value used by the operation.</param>
    /// <param name = "top">The top value used by the operation.</param>
    /// <param name = "percent">The percent value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetAutoFilterTop10(uint columnId, double value, bool top, bool percent)
    {
        XlsxWorksheetFilterResult result = ValidateWorksheetAutoFilterColumn(columnId);
        if (!result.IsValid || result.AutoFilter == null)
        {
            return result;
        }

        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, columnId);
        ApplyTop10Filter(column, value, top, percent);
        result.FilterColumn = column;
        result.HasFilterColumn = true;
        result.AutoFilter.RawXml = "";
        return result;
    }

    /// <summary>
    /// Sets auto filter color and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "columnId">The column id value used by the operation.</param>
    /// <param name = "dxfId">The dxf id value used by the operation.</param>
    /// <param name = "cellColor">The cell color value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetAutoFilterColor(uint columnId, uint dxfId, bool cellColor)
    {
        XlsxWorksheetFilterResult result = ValidateWorksheetAutoFilterColumn(columnId);
        if (!result.IsValid || result.AutoFilter == null)
        {
            return result;
        }

        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, columnId);
        ApplyColorFilter(column, dxfId, cellColor);
        result.FilterColumn = column;
        result.HasFilterColumn = true;
        result.AutoFilter.RawXml = "";
        return result;
    }

    /// <summary>
    /// Sets auto filter icon and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "columnId">The column id value used by the operation.</param>
    /// <param name = "iconSet">The icon set value used by the operation.</param>
    /// <param name = "iconId">The icon id value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetAutoFilterIcon(uint columnId, string iconSet, uint iconId)
    {
        XlsxWorksheetFilterResult result = ValidateWorksheetAutoFilterColumn(columnId);
        if (!result.IsValid || result.AutoFilter == null)
        {
            return result;
        }

        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, columnId);
        ApplyIconFilter(column, iconSet, iconId);
        result.FilterColumn = column;
        result.HasFilterColumn = true;
        result.AutoFilter.RawXml = "";
        return result;
    }

    /// <summary>
    /// Clears table column filter while retaining the surrounding model object.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult ClearTableColumnFilter(string tableName, string columnName)
    {
        XlsxWorksheetFilterResult result = PrepareTableAutoFilter(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.AutoFilter == null)
        {
            return result;
        }

        uint columnId = (uint)GetTableColumnIndex(result.Table, columnName);
        RemoveAutoFilterColumn(result.AutoFilter, columnId);
        SaveTableAutoFilter(result);
        return result;
    }

    /// <summary>
    /// Sets table filter values and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <param name = "values">The values value used by the operation.</param>
    /// <param name = "includeBlanks">The include blanks value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetTableFilterValues(string tableName, string columnName, List<string> values, bool includeBlanks)
    {
        XlsxWorksheetFilterResult result = PrepareTableAutoFilter(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.AutoFilter == null)
        {
            return result;
        }

        uint columnId = (uint)GetTableColumnIndex(result.Table, columnName);
        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, columnId);
        ClearColumnFilterTypes(column);
        column.HasValues = true;
        column.Values = new XlsxAutoFilterValues();
        column.Values.Blank = includeBlanks;
        column.Values.HasBlank = true;
        if (values != null)
        {
            foreach (string value in values)
            {
                column.Values.Values.Add(XlsxCellValueConverter.NormalizeText(value));
            }
        }

        result.FilterColumn = column;
        result.HasFilterColumn = true;
        SaveTableAutoFilter(result);
        return result;
    }

    /// <summary>
    /// Sets table custom filter and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <param name = "firstOperator">The first operator value used by the operation.</param>
    /// <param name = "firstValue">The first value value used by the operation.</param>
    /// <param name = "secondOperator">The second operator value used by the operation.</param>
    /// <param name = "secondValue">The second value value used by the operation.</param>
    /// <param name = "andOperator">The and operator value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetTableCustomFilter(string tableName, string columnName, string firstOperator, string firstValue, string secondOperator, string secondValue, bool andOperator)
    {
        XlsxWorksheetFilterResult result = PrepareTableAutoFilter(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.AutoFilter == null)
        {
            return result;
        }

        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, (uint)GetTableColumnIndex(result.Table, columnName));
        ApplyCustomFilter(column, firstOperator, firstValue, secondOperator, secondValue, andOperator);
        result.FilterColumn = column;
        result.HasFilterColumn = true;
        SaveTableAutoFilter(result);
        return result;
    }

    /// <summary>
    /// Sets table dynamic filter and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <param name = "filterType">The filter type value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetTableDynamicFilter(string tableName, string columnName, string filterType)
    {
        XlsxWorksheetFilterResult result = PrepareTableAutoFilter(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.AutoFilter == null)
        {
            return result;
        }

        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, (uint)GetTableColumnIndex(result.Table, columnName));
        ClearColumnFilterTypes(column);
        column.HasDynamicFilter = true;
        column.DynamicFilter = new XlsxDynamicFilter();
        column.DynamicFilter.Type = XlsxCellValueConverter.NormalizeText(filterType);
        result.FilterColumn = column;
        result.HasFilterColumn = true;
        SaveTableAutoFilter(result);
        return result;
    }

    /// <summary>
    /// Sets table date group filter and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <param name = "year">The year value used by the operation.</param>
    /// <param name = "month">The month value used by the operation.</param>
    /// <param name = "day">The day value used by the operation.</param>
    /// <param name = "grouping">The grouping value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetTableDateGroupFilter(string tableName, string columnName, uint year, uint month, uint day, string grouping)
    {
        XlsxWorksheetFilterResult result = PrepareTableAutoFilter(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.AutoFilter == null)
        {
            return result;
        }

        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, (uint)GetTableColumnIndex(result.Table, columnName));
        ClearColumnFilterTypes(column);
        column.HasValues = true;
        column.Values = new XlsxAutoFilterValues();
        XlsxDateGroupItem item = new XlsxDateGroupItem();
        item.Year = year;
        item.HasYear = true;
        item.Month = month;
        item.HasMonth = true;
        item.Day = day;
        item.HasDay = true;
        item.DateTimeGrouping = XlsxCellValueConverter.NormalizeText(grouping);
        column.Values.DateGroupItems.Add(item);
        result.FilterColumn = column;
        result.HasFilterColumn = true;
        SaveTableAutoFilter(result);
        return result;
    }

    /// <summary>
    /// Sets table top10 filter and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <param name = "value">The value value used by the operation.</param>
    /// <param name = "top">The top value used by the operation.</param>
    /// <param name = "percent">The percent value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetTableTop10Filter(string tableName, string columnName, double value, bool top, bool percent)
    {
        XlsxWorksheetFilterResult result = PrepareTableAutoFilter(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.AutoFilter == null)
        {
            return result;
        }

        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, (uint)GetTableColumnIndex(result.Table, columnName));
        ApplyTop10Filter(column, value, top, percent);
        result.FilterColumn = column;
        result.HasFilterColumn = true;
        SaveTableAutoFilter(result);
        return result;
    }

    /// <summary>
    /// Sets table color filter and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <param name = "dxfId">The dxf id value used by the operation.</param>
    /// <param name = "cellColor">The cell color value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetTableColorFilter(string tableName, string columnName, uint dxfId, bool cellColor)
    {
        XlsxWorksheetFilterResult result = PrepareTableAutoFilter(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.AutoFilter == null)
        {
            return result;
        }

        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, (uint)GetTableColumnIndex(result.Table, columnName));
        ApplyColorFilter(column, dxfId, cellColor);
        result.FilterColumn = column;
        result.HasFilterColumn = true;
        SaveTableAutoFilter(result);
        return result;
    }

    /// <summary>
    /// Sets table icon filter and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "columnName">The column name value used by the operation.</param>
    /// <param name = "iconSet">The icon set value used by the operation.</param>
    /// <param name = "iconId">The icon id value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetFilterResult SetTableIconFilter(string tableName, string columnName, string iconSet, uint iconId)
    {
        XlsxWorksheetFilterResult result = PrepareTableAutoFilter(tableName, columnName);
        if (!result.IsValid || result.Table == null || result.AutoFilter == null)
        {
            return result;
        }

        XlsxAutoFilterColumn column = GetOrCreateAutoFilterColumn(result.AutoFilter, (uint)GetTableColumnIndex(result.Table, columnName));
        ApplyIconFilter(column, iconSet, iconId);
        result.FilterColumn = column;
        result.HasFilterColumn = true;
        SaveTableAutoFilter(result);
        return result;
    }

    /// <summary>
    /// Adds data validation to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetDataValidationResult AddDataValidation(string range)
    {
        return AddDataValidationCore(range, "", "", "", "");
    }

    /// <summary>
    /// Adds list data validation to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "values">The values value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetDataValidationResult AddListDataValidation(string range, List<string> values)
    {
        XlsxWorksheetDataValidationResult result = new XlsxWorksheetDataValidationResult();
        string formula = BuildInlineListFormula(result, values);
        if (!result.IsValid)
        {
            return result;
        }

        return AddDataValidationCore(range, "list", "", formula, "");
    }

    /// <summary>
    /// Adds list data validation formula to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetDataValidationResult AddListDataValidationFormula(string range, string formula)
    {
        return AddDataValidationCore(range, "list", "", XlsxCellValueConverter.NormalizeText(formula), "");
    }

    /// <summary>
    /// Adds whole number data validation to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "operation">The operation value used by the operation.</param>
    /// <param name = "formula1">Excel formula text without a leading equals sign.</param>
    /// <param name = "formula2">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetDataValidationResult AddWholeNumberDataValidation(string range, string operation, string formula1, string formula2)
    {
        return AddDataValidationCore(range, "whole", operation, formula1, formula2);
    }

    /// <summary>
    /// Adds decimal data validation to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "operation">The operation value used by the operation.</param>
    /// <param name = "formula1">Excel formula text without a leading equals sign.</param>
    /// <param name = "formula2">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetDataValidationResult AddDecimalDataValidation(string range, string operation, string formula1, string formula2)
    {
        return AddDataValidationCore(range, "decimal", operation, formula1, formula2);
    }

    /// <summary>
    /// Adds date data validation to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "operation">The operation value used by the operation.</param>
    /// <param name = "formula1">Excel formula text without a leading equals sign.</param>
    /// <param name = "formula2">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetDataValidationResult AddDateDataValidation(string range, string operation, string formula1, string formula2)
    {
        return AddDataValidationCore(range, "date", operation, formula1, formula2);
    }

    /// <summary>
    /// Adds time data validation to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "operation">The operation value used by the operation.</param>
    /// <param name = "formula1">Excel formula text without a leading equals sign.</param>
    /// <param name = "formula2">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetDataValidationResult AddTimeDataValidation(string range, string operation, string formula1, string formula2)
    {
        return AddDataValidationCore(range, "time", operation, formula1, formula2);
    }

    /// <summary>
    /// Adds text length data validation to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "operation">The operation value used by the operation.</param>
    /// <param name = "formula1">Excel formula text without a leading equals sign.</param>
    /// <param name = "formula2">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetDataValidationResult AddTextLengthDataValidation(string range, string operation, string formula1, string formula2)
    {
        return AddDataValidationCore(range, "textLength", operation, formula1, formula2);
    }

    /// <summary>
    /// Adds custom data validation to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetDataValidationResult AddCustomDataValidation(string range, string formula)
    {
        return AddDataValidationCore(range, "custom", "", formula, "");
    }

    /// <summary>
    /// Sets data validation messages and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "validation">The validation value used by the operation.</param>
    /// <param name = "showInputMessage">The show input message value used by the operation.</param>
    /// <param name = "promptTitle">The prompt title value used by the operation.</param>
    /// <param name = "prompt">The prompt value used by the operation.</param>
    /// <param name = "showErrorMessage">The show error message value used by the operation.</param>
    /// <param name = "errorTitle">The error title value used by the operation.</param>
    /// <param name = "error">The error value used by the operation.</param>
    /// <param name = "errorStyle">The error style value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetDataValidationResult SetDataValidationMessages(XlsxDataValidation validation, bool showInputMessage, string promptTitle, string prompt, bool showErrorMessage, string errorTitle, string error, string errorStyle)
    {
        XlsxWorksheetDataValidationResult result = new XlsxWorksheetDataValidationResult();
        if (validation == null)
        {
            result.AddError("worksheetDataValidation.validationNull", "Data validation must not be null.");
            return result;
        }

        validation.ShowInputMessage = showInputMessage;
        validation.HasShowInputMessage = true;
        validation.PromptTitle = XlsxCellValueConverter.NormalizeText(promptTitle);
        validation.Prompt = XlsxCellValueConverter.NormalizeText(prompt);
        validation.ShowErrorMessage = showErrorMessage;
        validation.HasShowErrorMessage = true;
        validation.ErrorTitle = XlsxCellValueConverter.NormalizeText(errorTitle);
        validation.Error = XlsxCellValueConverter.NormalizeText(error);
        validation.ErrorStyle = XlsxCellValueConverter.NormalizeText(errorStyle);
        result.DataValidation = validation;
        result.HasDataValidation = true;
        result.DataValidations = DataValidations;
        result.HasDataValidations = HasDataValidations;
        if (HasDataValidations && DataValidations != null)
        {
            DataValidations.RawXml = "";
        }

        return result;
    }

    /// <summary>
    /// Sets data validation allow blank and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "validation">The validation value used by the operation.</param>
    /// <param name = "allowBlank">The allow blank value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetDataValidationResult SetDataValidationAllowBlank(XlsxDataValidation? validation, bool allowBlank)
    {
        XlsxWorksheetDataValidationResult result = new XlsxWorksheetDataValidationResult();
        if (validation == null)
        {
            result.AddError("worksheetDataValidation.validationNull", "Data validation must not be null.");
            return result;
        }

        validation.AllowBlank = allowBlank;
        validation.HasAllowBlank = true;
        result.DataValidation = validation;
        result.HasDataValidation = true;
        if (HasDataValidations && DataValidations != null)
        {
            DataValidations.RawXml = "";
            result.DataValidations = DataValidations;
            result.HasDataValidations = true;
        }

        return result;
    }

    /// <summary>
    /// Sets data validation show drop down and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "validation">The validation value used by the operation.</param>
    /// <param name = "showDropDown">The show drop down value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetDataValidationResult SetDataValidationShowDropDown(XlsxDataValidation validation, bool showDropDown)
    {
        XlsxWorksheetDataValidationResult result = new XlsxWorksheetDataValidationResult();
        if (validation == null)
        {
            result.AddError("worksheetDataValidation.validationNull", "Data validation must not be null.");
            return result;
        }

        validation.ShowDropDown = showDropDown;
        validation.HasShowDropDown = true;
        result.DataValidation = validation;
        result.HasDataValidation = true;
        if (HasDataValidations && DataValidations != null)
        {
            DataValidations.RawXml = "";
            result.DataValidations = DataValidations;
            result.HasDataValidations = true;
        }

        return result;
    }

    /// <summary>
    /// Clears data validations while retaining the surrounding model object.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetDataValidationResult ClearDataValidations()
    {
        XlsxWorksheetDataValidationResult result = new XlsxWorksheetDataValidationResult();
        if (HasDataValidations && DataValidations != null && DataValidations.Items != null)
        {
            result.RemovedCount = DataValidations.Items.Count;
        }

        DataValidations = new XlsxDataValidations();
        HasDataValidations = false;
        return result;
    }

    internal XlsxWorksheetDataValidationResult AddDataValidationCore(string range, string type, string operation, string formula1, string formula2)
    {
        XlsxWorksheetDataValidationResult result = PrepareDataValidations();
        string sqref = NormalizeDataValidationSqref(result, range);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxDataValidation validation = new XlsxDataValidation();
        validation.Type = XlsxCellValueConverter.NormalizeText(type);
        validation.Operator = XlsxCellValueConverter.NormalizeText(operation);
        validation.Sqref = sqref;
        validation.Formula1 = XlsxCellValueConverter.NormalizeText(formula1);
        validation.Formula1RawXml = "";
        validation.Formula2 = XlsxCellValueConverter.NormalizeText(formula2);
        validation.Formula2RawXml = "";
        DataValidations.Items.Add(validation);
        DataValidations.Count = (uint)DataValidations.Items.Count;
        DataValidations.HasCount = true;
        DataValidations.RawXml = "";
        result.DataValidation = validation;
        result.HasDataValidation = true;
        result.AddedCount = 1;
        return result;
    }

    internal XlsxWorksheetDataValidationResult PrepareDataValidations()
    {
        XlsxWorksheetDataValidationResult result = new XlsxWorksheetDataValidationResult();
        if (!HasDataValidations || DataValidations == null)
        {
            DataValidations = new XlsxDataValidations();
            DataValidations.NamespaceUri = NormalizeSpreadsheetNamespace(NamespaceUri);
            HasDataValidations = true;
        }

        if (DataValidations.Items == null)
        {
            result.AddError("worksheetDataValidation.itemsNull", "Worksheet data validation item list must not be null.");
            return result;
        }

        DataValidations.NamespaceUri = NormalizeSpreadsheetNamespace(DataValidations.NamespaceUri);
        DataValidations.RawXml = "";
        result.DataValidations = DataValidations;
        result.HasDataValidations = true;
        return result;
    }

    internal static string NormalizeDataValidationSqref(XlsxWorksheetDataValidationResult result, string sqref)
    {
        if (sqref == null || sqref.Length == 0)
        {
            result.AddError("worksheetDataValidation.sqrefMissing", "Data validation range must not be empty.");
            return "";
        }

        StringBuilder builder = new StringBuilder();
        string[] references = sqref.Split(' ');
        foreach (string reference in references)
        {
            string trimmed = reference.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(trimmed);
            if (!range.IsValid)
            {
                XlsxWorksheetDataValidationResult.AddRangeDiagnostics(result, range);
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(FormatRange(range));
        }

        if (builder.Length == 0 && result.IsValid)
        {
            result.AddError("worksheetDataValidation.sqrefMissing", "Data validation range must not be empty.");
        }

        return builder.ToString();
    }

    internal static string BuildInlineListFormula(XlsxWorksheetDataValidationResult result, List<string> values)
    {
        if (values == null)
        {
            result.AddError("worksheetDataValidation.listValuesNull", "List validation values must not be null.");
            return "";
        }

        if (values.Count == 0)
        {
            result.AddError("worksheetDataValidation.listValuesEmpty", "List validation values must not be empty.");
            return "";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append('"');
        int index = 0;
        foreach (string value in values)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            AppendEscapedInlineListValue(builder, XlsxCellValueConverter.NormalizeText(value));
            index = index + 1;
        }

        builder.Append('"');
        return builder.ToString();
    }

    /// <summary>
    /// Adds conditional formatting rule to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "type">The type value used by the operation.</param>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult AddConditionalFormattingRule(string range, string type, string formula)
    {
        XlsxWorksheetConditionalFormattingResult result = AddConditionalFormattingRuleCore(range, type);
        if (!result.IsValid || result.Rule == null || result.ConditionalFormatting == null)
        {
            return result;
        }

        string normalizedFormula = XlsxCellValueConverter.NormalizeText(formula);
        if (normalizedFormula.Length > 0)
        {
            result.Rule.Formulas.Add(normalizedFormula);
        }

        return result;
    }

    /// <summary>
    /// Adds expression conditional formatting to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult AddExpressionConditionalFormatting(string range, string formula)
    {
        return AddConditionalFormattingRule(range, "expression", formula);
    }

    /// <summary>
    /// Adds cell is conditional formatting to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "operation">The operation value used by the operation.</param>
    /// <param name = "formula1">Excel formula text without a leading equals sign.</param>
    /// <param name = "formula2">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult AddCellIsConditionalFormatting(string range, string operation, string formula1, string formula2)
    {
        XlsxWorksheetConditionalFormattingResult result = AddConditionalFormattingRuleCore(range, "cellIs");
        if (!result.IsValid || result.Rule == null || result.ConditionalFormatting == null)
        {
            return result;
        }

        result.Rule.Operator = XlsxCellValueConverter.NormalizeText(operation);
        string normalizedFormula1 = XlsxCellValueConverter.NormalizeText(formula1);
        string normalizedFormula2 = XlsxCellValueConverter.NormalizeText(formula2);
        if (normalizedFormula1.Length > 0)
        {
            result.Rule.Formulas.Add(normalizedFormula1);
        }

        if (normalizedFormula2.Length > 0)
        {
            result.Rule.Formulas.Add(normalizedFormula2);
        }

        return result;
    }

    /// <summary>
    /// Adds contains text conditional formatting to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult AddContainsTextConditionalFormatting(string range, string text)
    {
        XlsxWorksheetConditionalFormattingResult result = AddConditionalFormattingRuleCore(range, "containsText");
        if (!result.IsValid || result.Rule == null || result.ConditionalFormatting == null)
        {
            return result;
        }

        string firstCell = GetFirstCellReference(result.ConditionalFormatting.Sqref);
        result.Rule.Operator = "containsText";
        result.Rule.Text = XlsxCellValueConverter.NormalizeText(text);
        result.Rule.Formulas.Add(BuildContainsTextFormula(firstCell, result.Rule.Text));
        return result;
    }

    /// <summary>
    /// Adds top10 conditional formatting to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "rank">The rank value used by the operation.</param>
    /// <param name = "top">The top value used by the operation.</param>
    /// <param name = "percent">The percent value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult AddTop10ConditionalFormatting(string range, uint rank, bool top, bool percent)
    {
        XlsxWorksheetConditionalFormattingResult result = AddConditionalFormattingRuleCore(range, "top10");
        if (!result.IsValid || result.Rule == null || result.ConditionalFormatting == null)
        {
            return result;
        }

        result.Rule.Rank = rank;
        result.Rule.HasRank = true;
        result.Rule.Bottom = !top;
        result.Rule.HasBottom = true;
        result.Rule.Percent = percent;
        result.Rule.HasPercent = true;
        return result;
    }

    /// <summary>
    /// Adds above average conditional formatting to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "aboveAverage">The above average value used by the operation.</param>
    /// <param name = "equalAverage">The equal average value used by the operation.</param>
    /// <param name = "stdDev">The std dev value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult AddAboveAverageConditionalFormatting(string range, bool aboveAverage, bool equalAverage, uint stdDev)
    {
        XlsxWorksheetConditionalFormattingResult result = AddConditionalFormattingRuleCore(range, "aboveAverage");
        if (!result.IsValid || result.Rule == null || result.ConditionalFormatting == null)
        {
            return result;
        }

        result.Rule.AboveAverage = aboveAverage;
        result.Rule.HasAboveAverage = true;
        result.Rule.EqualAverage = equalAverage;
        result.Rule.HasEqualAverage = true;
        if (stdDev > 0)
        {
            result.Rule.StdDev = stdDev;
            result.Rule.HasStdDev = true;
        }

        return result;
    }

    /// <summary>
    /// Adds duplicate values conditional formatting to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult AddDuplicateValuesConditionalFormatting(string range)
    {
        return AddConditionalFormattingRuleCore(range, "duplicateValues");
    }

    /// <summary>
    /// Adds unique values conditional formatting to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult AddUniqueValuesConditionalFormatting(string range)
    {
        return AddConditionalFormattingRuleCore(range, "uniqueValues");
    }

    /// <summary>
    /// Adds color scale conditional formatting to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "minColorRgb">The min color rgb value used by the operation.</param>
    /// <param name = "midpointType">The midpoint type value used by the operation.</param>
    /// <param name = "midpointValue">The midpoint value value used by the operation.</param>
    /// <param name = "midpointColorRgb">The midpoint color rgb value used by the operation.</param>
    /// <param name = "maxColorRgb">The max color rgb value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult AddColorScaleConditionalFormatting(string range, string minColorRgb, string midpointType, string midpointValue, string midpointColorRgb, string maxColorRgb)
    {
        XlsxWorksheetConditionalFormattingResult result = AddConditionalFormattingRuleCore(range, "colorScale");
        if (!result.IsValid || result.Rule == null || result.ConditionalFormatting == null)
        {
            return result;
        }

        result.Rule.HasColorScale = true;
        result.Rule.ColorScale = new XlsxConditionalFormatColorScale();
        result.Rule.ColorScale.Values.Add(CreateConditionalFormatValueObject("min", ""));
        string normalizedMidpointType = XlsxCellValueConverter.NormalizeText(midpointType);
        if (normalizedMidpointType.Length > 0)
        {
            result.Rule.ColorScale.Values.Add(CreateConditionalFormatValueObject(normalizedMidpointType, XlsxCellValueConverter.NormalizeText(midpointValue)));
        }

        result.Rule.ColorScale.Values.Add(CreateConditionalFormatValueObject("max", ""));
        result.Rule.ColorScale.Colors.Add(CreateConditionalFormatRgbColor(minColorRgb));
        if (normalizedMidpointType.Length > 0)
        {
            result.Rule.ColorScale.Colors.Add(CreateConditionalFormatRgbColor(midpointColorRgb));
        }

        result.Rule.ColorScale.Colors.Add(CreateConditionalFormatRgbColor(maxColorRgb));
        return result;
    }

    /// <summary>
    /// Adds data bar conditional formatting to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "colorRgb">The color rgb value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult AddDataBarConditionalFormatting(string range, string colorRgb)
    {
        XlsxWorksheetConditionalFormattingResult result = AddConditionalFormattingRuleCore(range, "dataBar");
        if (!result.IsValid || result.Rule == null || result.ConditionalFormatting == null)
        {
            return result;
        }

        result.Rule.HasDataBar = true;
        result.Rule.DataBar = new XlsxConditionalFormatDataBar();
        result.Rule.DataBar.ShowValue = true;
        result.Rule.DataBar.HasShowValue = true;
        result.Rule.DataBar.Values.Add(CreateConditionalFormatValueObject("min", ""));
        result.Rule.DataBar.Values.Add(CreateConditionalFormatValueObject("max", ""));
        result.Rule.DataBar.Colors.Add(CreateConditionalFormatRgbColor(colorRgb));
        return result;
    }

    /// <summary>
    /// Adds icon set conditional formatting to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "range">The range value used by the operation.</param>
    /// <param name = "iconSet">The icon set value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult AddIconSetConditionalFormatting(string range, string iconSet)
    {
        XlsxWorksheetConditionalFormattingResult result = AddConditionalFormattingRuleCore(range, "iconSet");
        if (!result.IsValid || result.Rule == null || result.ConditionalFormatting == null)
        {
            return result;
        }

        result.Rule.HasIconSet = true;
        result.Rule.IconSet = new XlsxConditionalFormatIconSet();
        result.Rule.IconSet.IconSet = XlsxCellValueConverter.NormalizeText(iconSet);
        result.Rule.IconSet.Percent = true;
        result.Rule.IconSet.HasPercent = true;
        result.Rule.IconSet.Values.Add(CreateConditionalFormatValueObject("percent", "0"));
        result.Rule.IconSet.Values.Add(CreateConditionalFormatValueObject("percent", "33"));
        result.Rule.IconSet.Values.Add(CreateConditionalFormatValueObject("percent", "67"));
        return result;
    }

    /// <summary>
    /// Sets conditional formatting dxf id and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "rule">The rule value used by the operation.</param>
    /// <param name = "dxfId">The dxf id value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult SetConditionalFormattingDxfId(XlsxConditionalFormatRule? rule, uint dxfId)
    {
        XlsxWorksheetConditionalFormattingResult result = ValidateConditionalFormattingRuleArgument(rule);
        if (!result.IsValid || rule == null)
        {
            return result;
        }

        rule.DxfId = dxfId;
        rule.HasDxfId = true;
        MarkConditionalFormattingRuleChanged(result, rule);
        return result;
    }

    /// <summary>
    /// Sets conditional formatting stop if true and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "rule">The rule value used by the operation.</param>
    /// <param name = "stopIfTrue">The stop if true value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult SetConditionalFormattingStopIfTrue(XlsxConditionalFormatRule rule, bool stopIfTrue)
    {
        XlsxWorksheetConditionalFormattingResult result = ValidateConditionalFormattingRuleArgument(rule);
        if (!result.IsValid || rule == null)
        {
            return result;
        }

        rule.StopIfTrue = stopIfTrue;
        rule.HasStopIfTrue = true;
        MarkConditionalFormattingRuleChanged(result, rule);
        return result;
    }

    /// <summary>
    /// Clears conditional formattings while retaining the surrounding model object.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetConditionalFormattingResult ClearConditionalFormattings()
    {
        XlsxWorksheetConditionalFormattingResult result = new XlsxWorksheetConditionalFormattingResult();
        if (ConditionalFormattings != null)
        {
            result.RemovedCount = ConditionalFormattings.Count;
        }

        ConditionalFormattings = new List<XlsxConditionalFormatting>();
        return result;
    }

    internal XlsxWorksheetConditionalFormattingResult AddConditionalFormattingRuleCore(string range, string type)
    {
        XlsxWorksheetConditionalFormattingResult result = PrepareConditionalFormattings();
        string sqref = NormalizeConditionalFormattingSqref(result, range);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxConditionalFormatting formatting = new XlsxConditionalFormatting();
        formatting.NamespaceUri = NormalizeSpreadsheetNamespace(NamespaceUri);
        formatting.Sqref = sqref;
        formatting.RawXml = "";
        XlsxConditionalFormatRule rule = new XlsxConditionalFormatRule();
        rule.Type = XlsxCellValueConverter.NormalizeText(type);
        rule.Priority = GetNextConditionalFormattingPriority();
        rule.HasPriority = true;
        formatting.Rules.Add(rule);
        ConditionalFormattings.Add(formatting);
        result.ConditionalFormatting = formatting;
        result.HasConditionalFormatting = true;
        result.Rule = rule;
        result.HasRule = true;
        result.AddedCount = 1;
        return result;
    }

    internal XlsxWorksheetConditionalFormattingResult PrepareConditionalFormattings()
    {
        XlsxWorksheetConditionalFormattingResult result = new XlsxWorksheetConditionalFormattingResult();
        if (ConditionalFormattings == null)
        {
            result.AddError("worksheetConditionalFormatting.itemsNull", "Worksheet conditional formatting list must not be null.");
            return result;
        }

        return result;
    }

    internal static string NormalizeConditionalFormattingSqref(XlsxWorksheetConditionalFormattingResult result, string sqref)
    {
        if (sqref == null || sqref.Length == 0)
        {
            result.AddError("worksheetConditionalFormatting.sqrefMissing", "Conditional formatting range must not be empty.");
            return "";
        }

        StringBuilder builder = new StringBuilder();
        string[] references = sqref.Split(' ');
        foreach (string reference in references)
        {
            string trimmed = reference.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(trimmed);
            if (!range.IsValid)
            {
                XlsxWorksheetConditionalFormattingResult.AddRangeDiagnostics(result, range);
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(FormatRange(range));
        }

        if (builder.Length == 0 && result.IsValid)
        {
            result.AddError("worksheetConditionalFormatting.sqrefMissing", "Conditional formatting range must not be empty.");
        }

        return builder.ToString();
    }

    internal uint GetNextConditionalFormattingPriority()
    {
        uint next = 1;
        foreach (XlsxConditionalFormatting formatting in ConditionalFormattings)
        {
            if (formatting == null || formatting.Rules == null)
            {
                continue;
            }

            foreach (XlsxConditionalFormatRule rule in formatting.Rules)
            {
                if (rule != null && rule.HasPriority && rule.Priority >= next)
                {
                    next = rule.Priority + 1;
                }
            }
        }

        return next;
    }

    internal static XlsxConditionalFormatValueObject CreateConditionalFormatValueObject(string type, string value)
    {
        XlsxConditionalFormatValueObject valueObject = new XlsxConditionalFormatValueObject();
        valueObject.Type = XlsxCellValueConverter.NormalizeText(type);
        valueObject.Value = XlsxCellValueConverter.NormalizeText(value);
        return valueObject;
    }

    internal static XlsxConditionalFormatColor CreateConditionalFormatRgbColor(string rgb)
    {
        XlsxConditionalFormatColor color = new XlsxConditionalFormatColor();
        color.Rgb = XlsxCellValueConverter.NormalizeText(rgb);
        return color;
    }

    internal static string GetFirstCellReference(string sqref)
    {
        if (sqref == null || sqref.Length == 0)
        {
            return "";
        }

        string[] references = sqref.Split(' ');
        foreach (string reference in references)
        {
            string trimmed = reference.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(trimmed);
            if (!range.IsValid)
            {
                return "";
            }

            NormalizeRangeBounds(range);
            return XlsxA1CellReferenceConverter.FormatString(range.First.Row, range.First.Column, false, false);
        }

        return "";
    }

    internal static string BuildContainsTextFormula(string firstCell, string text)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("NOT(ISERROR(SEARCH(\"");
        AppendEscapedFormulaString(builder, XlsxCellValueConverter.NormalizeText(text));
        builder.Append("\",");
        builder.Append(XlsxCellValueConverter.NormalizeText(firstCell));
        builder.Append(")))");
        return builder.ToString();
    }

    internal static void AppendEscapedFormulaString(StringBuilder builder, string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '"')
            {
                builder.Append("\"\"");
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }
    }

    internal XlsxWorksheetConditionalFormattingResult ValidateConditionalFormattingRuleArgument(XlsxConditionalFormatRule? rule)
    {
        XlsxWorksheetConditionalFormattingResult result = new XlsxWorksheetConditionalFormattingResult();
        if (rule == null)
        {
            result.AddError("worksheetConditionalFormatting.ruleNull", "Conditional formatting rule must not be null.");
            return result;
        }

        result.Rule = rule;
        result.HasRule = true;
        return result;
    }

    internal void MarkConditionalFormattingRuleChanged(XlsxWorksheetConditionalFormattingResult result, XlsxConditionalFormatRule rule)
    {
        foreach (XlsxConditionalFormatting formatting in ConditionalFormattings)
        {
            if (formatting == null || formatting.Rules == null)
            {
                continue;
            }

            foreach (XlsxConditionalFormatRule candidate in formatting.Rules)
            {
                if (candidate == rule)
                {
                    formatting.RawXml = "";
                    result.ConditionalFormatting = formatting;
                    result.HasConditionalFormatting = true;
                    return;
                }
            }
        }
    }

    internal static void AppendEscapedInlineListValue(StringBuilder builder, string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '"')
            {
                builder.Append("\"\"");
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }
    }

    /// <summary>
    /// Sets table style and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "styleName">The style name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult SetTableStyle(string tableName, string styleName)
    {
        return SetTableStyleWithStripeOptions(tableName, styleName, true, false);
    }

    /// <summary>
    /// Sets table style and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "styleName">The style name value used by the operation.</param>
    /// <param name = "showRowStripes">The show row stripes value used by the operation.</param>
    /// <param name = "showColumnStripes">The show column stripes value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetTableResult SetTableStyleWithStripeOptions(string tableName, string styleName, bool showRowStripes, bool showColumnStripes)
    {
        XlsxWorksheetTableResult result = ValidateExistingTable(tableName);
        if (!result.IsValid || result.Table == null)
        {
            return result;
        }

        result.Table.RawXml = "";
        if (result.Table.StyleInfo == null)
        {
            result.Table.StyleInfo = new XlsxTableStyleInfo();
        }

        result.Table.HasStyleInfo = true;
        result.Table.StyleInfo.Name = XlsxCellValueConverter.NormalizeText(styleName);
        result.Table.StyleInfo.ShowRowStripes = showRowStripes;
        result.Table.StyleInfo.HasShowRowStripes = true;
        result.Table.StyleInfo.ShowColumnStripes = showColumnStripes;
        result.Table.StyleInfo.HasShowColumnStripes = true;
        return result;
    }

    /// <summary>
    /// Inserts worksheet rows and updates affected cells, ranges, formulas, and related worksheet metadata.
    /// </summary>
    /// <param name = "rowIndex">A one-based worksheet row index.</param>
    /// <param name = "count">The count value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRowEditResult InsertRows(int rowIndex, int count)
    {
        XlsxWorksheetRowEditResult result = ValidateRowEdit(rowIndex, count);
        if (!result.IsValid)
        {
            return result;
        }

        int sequence = 1;
        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row == null)
            {
                result.AddError("worksheetRows.rowNull", "Worksheet rows must not contain null entries.");
            }
            else
            {
                int effectiveRow = GetEffectiveRowIndex(row, sequence);
                if (effectiveRow >= rowIndex && !IsValidCellRow(effectiveRow + count))
                {
                    result.AddError("worksheetRows.shiftOutsideWorksheet", "Inserted rows would move existing rows outside worksheet bounds.");
                }
            }

            sequence = sequence + 1;
        }

        if (!result.IsValid)
        {
            return result;
        }

        sequence = 1;
        foreach (XlsxWorksheetRow row in Rows)
        {
            int effectiveRow = GetEffectiveRowIndex(row, sequence);
            if (effectiveRow >= rowIndex)
            {
                MoveRowForRowEdit(result, row, effectiveRow + count);
                result.MovedRowCount = result.MovedRowCount + 1;
                result.AffectedRowCount = result.AffectedRowCount + 1;
            }

            sequence = sequence + 1;
        }

        SortRowsByEffectiveIndex();
        UpdateWorksheetStructuresForRowInsert(result, rowIndex, count);
        RefreshStoredDimensionAfterGridEdit();
        return result;
    }

    /// <summary>
    /// Deletes worksheet rows and updates affected cells, ranges, formulas, and related worksheet metadata.
    /// </summary>
    /// <param name = "rowIndex">A one-based worksheet row index.</param>
    /// <param name = "count">The count value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetRowEditResult DeleteRows(int rowIndex, int count)
    {
        XlsxWorksheetRowEditResult result = ValidateRowEdit(rowIndex, count);
        if (!result.IsValid)
        {
            return result;
        }

        int lastDeletedRow = rowIndex + count - 1;
        int sequence = 1;
        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row == null)
            {
                result.AddError("worksheetRows.rowNull", "Worksheet rows must not contain null entries.");
            }

            sequence = sequence + 1;
        }

        if (!result.IsValid)
        {
            return result;
        }

        List<XlsxWorksheetRow> keptRows = new List<XlsxWorksheetRow>();
        sequence = 1;
        foreach (XlsxWorksheetRow row in Rows)
        {
            int effectiveRow = GetEffectiveRowIndex(row, sequence);
            if (effectiveRow >= rowIndex && effectiveRow <= lastDeletedRow)
            {
                result.RemovedRowCount = result.RemovedRowCount + 1;
                result.AffectedRowCount = result.AffectedRowCount + 1;
            }
            else
            {
                if (effectiveRow > lastDeletedRow)
                {
                    MoveRowForRowEdit(result, row, effectiveRow - count);
                    result.MovedRowCount = result.MovedRowCount + 1;
                    result.AffectedRowCount = result.AffectedRowCount + 1;
                }

                keptRows.Add(row);
            }

            sequence = sequence + 1;
        }

        Rows.Clear();
        foreach (XlsxWorksheetRow row in keptRows)
        {
            Rows.Add(row);
        }

        SortRowsByEffectiveIndex();
        UpdateWorksheetStructuresForRowDelete(result, rowIndex, count);
        RefreshStoredDimensionAfterGridEdit();
        return result;
    }

    /// <summary>
    /// Inserts worksheet columns and updates affected cells, ranges, formulas, and related worksheet metadata.
    /// </summary>
    /// <param name = "columnIndex">A one-based worksheet column index.</param>
    /// <param name = "count">The count value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetColumnEditResult InsertColumns(int columnIndex, int count)
    {
        XlsxWorksheetColumnEditResult result = ValidateColumnEdit(columnIndex, count);
        if (!result.IsValid)
        {
            return result;
        }

        ValidateRowsForColumnEdit(result);
        XlsxWorksheetColumnMetadataStore store = LoadColumnMetadataForColumnEdit(result);
        ValidateColumnInsertBounds(result, store, columnIndex, count);
        if (!result.IsValid)
        {
            return result;
        }

        MoveCellsForColumnInsertion(result, columnIndex, count);
        MoveMetadataForColumnInsertion(result, store, columnIndex, count);
        UpdateWorksheetStructuresForColumnInsert(result, columnIndex, count);
        RefreshStoredDimensionAfterGridEdit();
        return result;
    }

    /// <summary>
    /// Deletes worksheet columns and updates affected cells, ranges, formulas, and related worksheet metadata.
    /// </summary>
    /// <param name = "columnIndex">A one-based worksheet column index.</param>
    /// <param name = "count">The count value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetColumnEditResult DeleteColumns(int columnIndex, int count)
    {
        XlsxWorksheetColumnEditResult result = ValidateColumnEdit(columnIndex, count);
        if (!result.IsValid)
        {
            return result;
        }

        ValidateRowsForColumnEdit(result);
        XlsxWorksheetColumnMetadataStore store = LoadColumnMetadataForColumnEdit(result);
        if (!result.IsValid)
        {
            return result;
        }

        MoveCellsForColumnDeletion(result, columnIndex, count);
        MoveMetadataForColumnDeletion(result, store, columnIndex, count);
        UpdateWorksheetStructuresForColumnDelete(result, columnIndex, count);
        RefreshStoredDimensionAfterGridEdit();
        return result;
    }

    /// <summary>
    /// Returns or create cell by reference from the current model.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult GetOrCreateCellByReference(string reference)
    {
        XlsxWorksheetCellAccessResult result = new XlsxWorksheetCellAccessResult();
        XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(reference);
        if (!referenceResult.IsValid)
        {
            XlsxWorksheetCellAccessResult.AddReferenceDiagnostics(result, referenceResult);
            return result;
        }

        return GetOrCreateCell(referenceResult.Row, referenceResult.Column);
    }

    /// <summary>
    /// Returns or create cell from the current model.
    /// </summary>
    /// <param name = "rowIndex">A one-based worksheet row index.</param>
    /// <param name = "columnIndex">A one-based worksheet column index.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult GetOrCreateCell(int rowIndex, int columnIndex)
    {
        XlsxWorksheetCellAccessResult result = ValidateCellCoordinate(rowIndex, columnIndex);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxWorksheetCell? existing = GetCellAt(rowIndex, columnIndex);
        if (existing != null)
        {
            result.Cell = existing;
            result.HasCell = true;
            return result;
        }

        XlsxWorksheetRow? row = GetOrCreateRowForCellAccess(result, rowIndex);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = XlsxA1CellReferenceConverter.FormatString(rowIndex, columnIndex, false, false);
        InsertCellInRow(row, cell, columnIndex);
        result.Cell = cell;
        result.HasCell = true;
        result.CreatedCell = true;
        return result;
    }

    /// <summary>
    /// Sets cell by reference and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "cell">The cell value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetCellByReference(string reference, XlsxWorksheetCell cell)
    {
        XlsxWorksheetCellAccessResult result = new XlsxWorksheetCellAccessResult();
        XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(reference);
        if (!referenceResult.IsValid)
        {
            XlsxWorksheetCellAccessResult.AddReferenceDiagnostics(result, referenceResult);
            return result;
        }

        return SetCell(referenceResult.Row, referenceResult.Column, cell);
    }

    /// <summary>
    /// Sets cell and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "rowIndex">A one-based worksheet row index.</param>
    /// <param name = "columnIndex">A one-based worksheet column index.</param>
    /// <param name = "cell">The cell value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetCell(int rowIndex, int columnIndex, XlsxWorksheetCell cell)
    {
        XlsxWorksheetCellAccessResult result = ValidateCellCoordinate(rowIndex, columnIndex);
        if (!result.IsValid)
        {
            return result;
        }

        if (cell == null)
        {
            result.AddError("worksheetCell.cellNull", "Worksheet cell must not be null.");
            return result;
        }

        cell.Reference = XlsxA1CellReferenceConverter.FormatString(rowIndex, columnIndex, false, false);
        if (ReplaceExistingCell(rowIndex, columnIndex, cell))
        {
            result.Cell = cell;
            result.HasCell = true;
            return result;
        }

        XlsxWorksheetRow? row = GetOrCreateRowForCellAccess(result, rowIndex);
        if (!result.IsValid)
        {
            return result;
        }

        InsertCellInRow(row, cell, columnIndex);
        result.Cell = cell;
        result.HasCell = true;
        result.CreatedCell = true;
        return result;
    }

    /// <summary>
    /// Sets cell value by reference and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetCellValueByReference(string reference, XlsxCellValue value)
    {
        XlsxWorksheetCellAccessResult result = new XlsxWorksheetCellAccessResult();
        XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(reference);
        if (!referenceResult.IsValid)
        {
            XlsxWorksheetCellAccessResult.AddReferenceDiagnostics(result, referenceResult);
            return result;
        }

        return SetCellValueAt(referenceResult.Row, referenceResult.Column, value);
    }

    /// <summary>
    /// Sets cell value and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetCellValue(string reference, XlsxCellValue value)
    {
        return SetCellValueByReference(reference, value);
    }

    /// <summary>
    /// Sets cell value and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "rowIndex">A one-based worksheet row index.</param>
    /// <param name = "columnIndex">A one-based worksheet column index.</param>
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetCellValueAt(int rowIndex, int columnIndex, XlsxCellValue value)
    {
        XlsxWorksheetCellAccessResult result = new XlsxWorksheetCellAccessResult();
        if (value == null)
        {
            result.AddError("cellValue.valueNull", "Cell value must not be null.");
            return result;
        }

        XlsxWorksheetCellAccessResult cellResult = GetOrCreateCell(rowIndex, columnIndex);
        XlsxWorksheetCellAccessResult.AddCellAccessDiagnostics(result, cellResult);
        result.Cell = cellResult.Cell;
        result.HasCell = cellResult.HasCell;
        result.CreatedRow = cellResult.CreatedRow;
        result.CreatedCell = cellResult.CreatedCell;
        if (!result.IsValid || !result.HasCell)
        {
            return result;
        }

        XlsxCellValueResult valueResult = XlsxCellValueConverter.ApplyToWorksheetCell(value, result.Cell);
        XlsxWorksheetCellAccessResult.AddValueDiagnostics(result, valueResult);
        result.Value = valueResult.Value;
        result.HasValue = valueResult.IsValid;
        return result;
    }

    /// <summary>
    /// Sets blank value and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetBlankValue(string reference)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateBlank());
    }

    /// <summary>
    /// Sets number value and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetNumberValue(string reference, double value)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateNumber(value));
    }

    internal XlsxWorksheetCellAccessResult SetDecimalValue(string reference, decimal value)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateDecimal(value));
    }

    /// <summary>
    /// Sets text value and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetTextValue(string reference, string value)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateText(value));
    }

    /// <summary>
    /// Sets shared string value and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "index">The index value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetSharedStringValue(string reference, uint index)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateSharedString(index));
    }

    /// <summary>
    /// Sets inline string value and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetInlineStringValue(string reference, string value)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateInlineString(value));
    }

    /// <summary>
    /// Sets boolean value and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetBooleanValue(string reference, bool value)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateBoolean(value));
    }

    /// <summary>
    /// Sets error value and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetErrorValue(string reference, string value)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateError(value));
    }

    internal XlsxWorksheetCellAccessResult SetTypedErrorValue(string reference, XlsxCellErrorKind value)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateTypedError(value));
    }

    /// <summary>
    /// Sets date time value and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "value">The value value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetDateTimeValue(string reference, System.DateTime value)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateDateTime(value));
    }

    internal XlsxWorksheetCellAccessResult SetIsoDateValue(string reference, string isoDateText)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateIsoDate(isoDateText));
    }

    internal XlsxWorksheetCellAccessResult SetDurationValue(string reference, System.TimeSpan value)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateDuration(value));
    }

    /// <summary>
    /// Sets formula cached value and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <param name = "cachedValue">The cached value value used by the operation.</param>
    /// <param name = "cachedType">The cached type value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetFormulaCachedValue(string reference, string formula, string cachedValue, string cachedType)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateFormulaCachedValue(formula, cachedValue, cachedType));
    }

    internal XlsxWorksheetCellAccessResult SetFormulaValue(string reference, string formula, XlsxCellValue cachedValue)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateFormulaValue(formula, cachedValue));
    }

    internal XlsxWorksheetCellAccessResult SetRichTextValue(string reference, XlsxInlineString value)
    {
        return SetCellValueByReference(reference, XlsxCellValue.CreateRichTextValue(value));
    }

    /// <summary>
    /// Sets array formula and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "rangeReference">An A1-style cell or range reference.</param>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <param name = "cachedValue">The cached value value used by the operation.</param>
    /// <param name = "cachedType">The cached type value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetArrayFormula(string reference, string rangeReference, string formula, string cachedValue, string cachedType)
    {
        XlsxCellFormula formulaInfo = XlsxCellFormula.CreateArray(formula, rangeReference);
        return SetRangeFormula(reference, rangeReference, formulaInfo, cachedValue, cachedType);
    }

    /// <summary>
    /// Sets dynamic array formula and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "spillRangeReference">An A1-style cell or range reference.</param>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <param name = "cachedValue">The cached value value used by the operation.</param>
    /// <param name = "cachedType">The cached type value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetDynamicArrayFormula(string reference, string spillRangeReference, string formula, string cachedValue, string cachedType)
    {
        XlsxCellFormula formulaInfo = XlsxCellFormula.CreateDynamicArray(formula, spillRangeReference);
        return SetRangeFormula(reference, spillRangeReference, formulaInfo, cachedValue, cachedType);
    }

    /// <summary>
    /// Sets range formula and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "rangeReference">An A1-style cell or range reference.</param>
    /// <param name = "formulaInfo">Excel formula text without a leading equals sign.</param>
    /// <param name = "cachedValue">The cached value value used by the operation.</param>
    /// <param name = "cachedType">The cached type value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCellAccessResult SetRangeFormula(string reference, string rangeReference, XlsxCellFormula formulaInfo, string cachedValue, string cachedType)
    {
        XlsxWorksheetCellAccessResult result = new XlsxWorksheetCellAccessResult();
        XlsxA1CellReferenceResult cellReference = XlsxA1CellReferenceConverter.Parse(reference);
        if (!cellReference.IsValid)
        {
            XlsxWorksheetCellAccessResult.AddReferenceDiagnostics(result, cellReference);
            return result;
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(rangeReference);
        if (!range.IsValid)
        {
            XlsxWorksheetCellAccessResult.AddRangeDiagnostics(result, range);
            return result;
        }

        if (!IsCellInsideRange(cellReference, range))
        {
            result.AddError("worksheetFormula.anchorOutsideRange", "Formula anchor cell must be inside the formula range.");
            return result;
        }

        XlsxWorksheetCellAccessResult cellResult = GetOrCreateCell(cellReference.Row, cellReference.Column);
        XlsxWorksheetCellAccessResult.AddCellAccessDiagnostics(result, cellResult);
        result.Cell = cellResult.Cell;
        result.HasCell = cellResult.HasCell;
        result.CreatedRow = cellResult.CreatedRow;
        result.CreatedCell = cellResult.CreatedCell;
        if (!result.IsValid || !result.HasCell)
        {
            return result;
        }

        ApplyFormulaInfoToCell(result.Cell, formulaInfo, cachedValue, cachedType);
        return result;
    }

    internal static void ApplyFormulaInfoToCell(XlsxWorksheetCell cell, XlsxCellFormula formulaInfo, string cachedValue, string cachedType)
    {
        cell.FormulaInfo = formulaInfo;
        cell.Formula = formulaInfo.Text;
        cell.HasFormula = true;
        cell.Value = XlsxCellValueConverter.NormalizeText(cachedValue);
        cell.Type = XlsxCellValueConverter.NormalizeText(cachedType);
        cell.FormulaInfo.CachedValue = cell.Value;
        cell.FormulaInfo.CachedType = cell.Type;
        cell.InlineStringText = "";
        cell.InlineString = new XlsxInlineString();
        cell.HasInlineString = false;
    }

    internal static bool IsCellInsideRange(XlsxA1CellReferenceResult cellReference, XlsxA1RangeReferenceResult range)
    {
        int firstRow = range.First.Row;
        int lastRow = range.First.Row;
        int firstColumn = range.First.Column;
        int lastColumn = range.First.Column;
        if (range.HasLast)
        {
            lastRow = range.Last.Row;
            lastColumn = range.Last.Column;
        }

        if (firstRow > lastRow)
        {
            int temporary = firstRow;
            firstRow = lastRow;
            lastRow = temporary;
        }

        if (firstColumn > lastColumn)
        {
            int temporary = firstColumn;
            firstColumn = lastColumn;
            lastColumn = temporary;
        }

        return cellReference.Row >= firstRow && cellReference.Row <= lastRow && cellReference.Column >= firstColumn && cellReference.Column <= lastColumn;
    }

    /// <summary>
    /// Performs the build formula dependency graph operation using the current model state.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaDependencyGraphResult BuildFormulaDependencyGraph()
    {
        XlsxFormulaDependencyGraphResult result = new XlsxFormulaDependencyGraphResult();
        if (Rows == null)
        {
            result.AddError("formulaDependency.rowsNull", "Worksheet rows list must not be null.");
            return result;
        }

        Dictionary<string, XlsxFormulaDependencyNode> nodes = new Dictionary<string, XlsxFormulaDependencyNode>();
        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row != null && row.Cells != null)
            {
                AddFormulaDependencyNodes(result, nodes, row);
            }
        }

        AddFormulaDependentLinks(nodes);
        foreach (KeyValuePair<string, XlsxFormulaDependencyNode> item in nodes)
        {
            result.Nodes.Add(item.Value);
        }

        return result;
    }

    internal static void AddFormulaDependencyNodes(XlsxFormulaDependencyGraphResult result, Dictionary<string, XlsxFormulaDependencyNode> nodes, XlsxWorksheetRow row)
    {
        foreach (XlsxWorksheetCell cell in row.Cells)
        {
            if (cell != null && cell.Reference != null && cell.Reference.Length > 0 && cell.HasFormula && cell.FormulaInfo != null)
            {
                string formulaText = cell.FormulaInfo.Text;
                if ((formulaText == null || formulaText.Length == 0) && cell.Formula != null && cell.Formula.Length > 0)
                {
                    formulaText = cell.Formula;
                }

                if (formulaText != null && formulaText.Length > 0)
                {
                    XlsxFormulaDependencyNode node = GetOrCreateDependencyNode(nodes, cell.Reference);
                    node.Formula = formulaText;
                    XlsxFormulaParseResult parseResult = new XlsxFormulaParser().Parse(formulaText);
                    CopyFormulaParseDiagnostics(result, parseResult);
                    if (parseResult.HasRoot)
                    {
                        AddFormulaNodeDependencies(nodes, node, parseResult.Root);
                    }
                }
            }
        }
    }

    internal static void AddFormulaDependentLinks(Dictionary<string, XlsxFormulaDependencyNode> nodes)
    {
        foreach (KeyValuePair<string, XlsxFormulaDependencyNode> item in nodes)
        {
            XlsxFormulaDependencyNode node = item.Value;
            foreach (string precedent in node.Precedents)
            {
                XlsxFormulaDependencyNode precedentNode = GetOrCreateDependencyNode(nodes, precedent);
                AddUniqueText(precedentNode.Dependents, node.CellReference);
            }
        }
    }

    internal static void AddFormulaNodeDependencies(Dictionary<string, XlsxFormulaDependencyNode> nodes, XlsxFormulaDependencyNode dependencyNode, XlsxFormulaNode formulaNode)
    {
        if (formulaNode == null)
        {
            return;
        }

        if (formulaNode.Kind == XlsxFormulaNodeKind.Reference)
        {
            AddReferenceDependency(nodes, dependencyNode, formulaNode.Text);
        }
        else if (formulaNode.Kind == XlsxFormulaNodeKind.Range)
        {
            AddRangeDependency(nodes, dependencyNode, formulaNode);
        }
        else if (formulaNode.Kind == XlsxFormulaNodeKind.ExternalReference)
        {
            AddUniqueText(dependencyNode.ExternalPrecedents, formulaNode.Text);
        }
        else if (formulaNode.Kind == XlsxFormulaNodeKind.Name || formulaNode.Kind == XlsxFormulaNodeKind.StructuredReference)
        {
            AddUniqueText(dependencyNode.Names, formulaNode.Text);
        }

        foreach (XlsxFormulaNode child in formulaNode.Children)
        {
            AddFormulaNodeDependencies(nodes, dependencyNode, child);
        }
    }

    internal static void AddReferenceDependency(Dictionary<string, XlsxFormulaDependencyNode> nodes, XlsxFormulaDependencyNode dependencyNode, string referenceText)
    {
        if (referenceText == null || referenceText.Length == 0)
        {
            return;
        }

        if (referenceText.IndexOf('!') >= 0)
        {
            AddUniqueText(dependencyNode.ExternalPrecedents, referenceText);
            return;
        }

        XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(referenceText);
        if (reference.IsValid)
        {
            string normalized = XlsxA1CellReferenceConverter.FormatString(reference.Row, reference.Column, false, false);
            AddUniqueText(dependencyNode.Precedents, normalized);
            GetOrCreateDependencyNode(nodes, normalized);
        }
        else
        {
            AddUniqueText(dependencyNode.Names, referenceText);
        }
    }

    internal static void AddRangeDependency(Dictionary<string, XlsxFormulaDependencyNode> nodes, XlsxFormulaDependencyNode dependencyNode, XlsxFormulaNode rangeNode)
    {
        if (rangeNode.Children.Count < 2)
        {
            AddReferenceDependency(nodes, dependencyNode, rangeNode.Text);
            return;
        }

        XlsxFormulaNode firstNode = rangeNode.Children[0];
        XlsxFormulaNode lastNode = rangeNode.Children[1];
        if (firstNode.Kind != XlsxFormulaNodeKind.Reference || lastNode.Kind != XlsxFormulaNodeKind.Reference || firstNode.Text.IndexOf('!') >= 0 || lastNode.Text.IndexOf('!') >= 0)
        {
            AddUniqueText(dependencyNode.ExternalPrecedents, rangeNode.Text);
            return;
        }

        XlsxA1CellReferenceResult first = XlsxA1CellReferenceConverter.Parse(firstNode.Text);
        XlsxA1CellReferenceResult last = XlsxA1CellReferenceConverter.Parse(lastNode.Text);
        if (!first.IsValid || !last.IsValid)
        {
            AddReferenceDependency(nodes, dependencyNode, rangeNode.Text);
            return;
        }

        int minRow = first.Row;
        int maxRow = last.Row;
        int minColumn = first.Column;
        int maxColumn = last.Column;
        if (minRow > maxRow)
        {
            int temporary = minRow;
            minRow = maxRow;
            maxRow = temporary;
        }

        if (minColumn > maxColumn)
        {
            int temporary = minColumn;
            minColumn = maxColumn;
            maxColumn = temporary;
        }

        int count = (maxRow - minRow + 1) * (maxColumn - minColumn + 1);
        if (count > 10000)
        {
            AddUniqueText(dependencyNode.ExternalPrecedents, rangeNode.Text);
            return;
        }

        int row = minRow;
        while (row <= maxRow)
        {
            int column = minColumn;
            while (column <= maxColumn)
            {
                string reference = XlsxA1CellReferenceConverter.FormatString(row, column, false, false);
                AddUniqueText(dependencyNode.Precedents, reference);
                GetOrCreateDependencyNode(nodes, reference);
                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal static XlsxFormulaDependencyNode GetOrCreateDependencyNode(Dictionary<string, XlsxFormulaDependencyNode> nodes, string cellReference)
    {
        if (nodes.ContainsKey(cellReference))
        {
            return nodes[cellReference];
        }

        XlsxFormulaDependencyNode node = new XlsxFormulaDependencyNode();
        node.CellReference = cellReference;
        nodes.Add(cellReference, node);
        return node;
    }

    internal static void AddUniqueText(List<string> items, string text)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }

        foreach (string item in items)
        {
            if (item == text)
            {
                return;
            }
        }

        items.Add(text);
    }

    internal static void CopyFormulaParseDiagnostics(XlsxFormulaDependencyGraphResult result, XlsxFormulaParseResult parseResult)
    {
        foreach (XlsxDiagnostic diagnostic in parseResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Performs the expand shared formulas operation using the current model state.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxSharedFormulaResult ExpandSharedFormulas()
    {
        XlsxSharedFormulaResult result = new XlsxSharedFormulaResult();
        if (Rows == null)
        {
            result.AddError("sharedFormula.rowsNull", "Worksheet rows list must not be null.");
            return result;
        }

        Dictionary<uint, XlsxWorksheetCell> masters = new Dictionary<uint, XlsxWorksheetCell>();
        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row != null && row.Cells != null)
            {
                AddSharedFormulaMasters(masters, row);
            }
        }

        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row != null && row.Cells != null)
            {
                ExpandSharedFormulasInRow(result, masters, row);
            }
        }

        return result;
    }

    /// <summary>
    /// Performs the compact shared formulas operation using the current model state.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxSharedFormulaResult CompactSharedFormulas()
    {
        XlsxSharedFormulaResult result = new XlsxSharedFormulaResult();
        if (Rows == null)
        {
            result.AddError("sharedFormula.rowsNull", "Worksheet rows list must not be null.");
            return result;
        }

        Dictionary<string, List<XlsxWorksheetCell>> groups = new Dictionary<string, List<XlsxWorksheetCell>>();
        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row != null && row.Cells != null)
            {
                AddFormulaCompactionCandidates(result, groups, row);
            }
        }

        uint sharedIndex = GetNextSharedFormulaIndex();
        foreach (KeyValuePair<string, List<XlsxWorksheetCell>> group in groups)
        {
            if (group.Value.Count > 1)
            {
                CompactSharedFormulaGroup(result, group.Value, sharedIndex);
                sharedIndex = sharedIndex + 1;
            }
        }

        return result;
    }

    internal static void AddSharedFormulaMasters(Dictionary<uint, XlsxWorksheetCell> masters, XlsxWorksheetRow row)
    {
        foreach (XlsxWorksheetCell cell in row.Cells)
        {
            if (IsSharedFormulaMaster(cell))
            {
                masters[cell.FormulaInfo.SharedIndex] = cell;
            }
        }
    }

    internal static void ExpandSharedFormulasInRow(XlsxSharedFormulaResult result, Dictionary<uint, XlsxWorksheetCell> masters, XlsxWorksheetRow row)
    {
        foreach (XlsxWorksheetCell cell in row.Cells)
        {
            if (IsSharedFormula(cell))
            {
                ExpandSharedFormulaCell(result, masters, cell);
            }
        }
    }

    internal static void ExpandSharedFormulaCell(XlsxSharedFormulaResult result, Dictionary<uint, XlsxWorksheetCell> masters, XlsxWorksheetCell cell)
    {
        if (!cell.FormulaInfo.HasSharedIndex)
        {
            result.AddError("sharedFormula.sharedIndexMissing", "Shared formula cell must have a shared index.");
            return;
        }

        if (!masters.ContainsKey(cell.FormulaInfo.SharedIndex))
        {
            result.AddError("sharedFormula.masterMissing", "Shared formula master cell was not found.");
            return;
        }

        XlsxWorksheetCell master = masters[cell.FormulaInfo.SharedIndex];
        string expanded = master.FormulaInfo.Text;
        if (!IsSharedFormulaMaster(cell))
        {
            XlsxA1CellReferenceResult masterReference = XlsxA1CellReferenceConverter.Parse(master.Reference);
            XlsxA1CellReferenceResult cellReference = XlsxA1CellReferenceConverter.Parse(cell.Reference);
            if (masterReference.IsValid && cellReference.IsValid)
            {
                int rowDelta = cellReference.Row - masterReference.Row;
                int columnDelta = cellReference.Column - masterReference.Column;
                expanded = TranslateFormulaReferences(master.FormulaInfo.Text, rowDelta, columnDelta);
            }

            result.ExpandedFormulaCount = result.ExpandedFormulaCount + 1;
        }

        cell.FormulaInfo.Type = "";
        cell.FormulaInfo.Text = expanded;
        cell.FormulaInfo.Reference = "";
        cell.FormulaInfo.HasSharedIndex = false;
        cell.Formula = expanded;
        cell.HasFormula = true;
    }

    internal static void AddFormulaCompactionCandidates(XlsxSharedFormulaResult result, Dictionary<string, List<XlsxWorksheetCell>> groups, XlsxWorksheetRow row)
    {
        foreach (XlsxWorksheetCell cell in row.Cells)
        {
            if (IsNormalFormulaCell(cell))
            {
                XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(cell.Reference);
                if (!reference.IsValid)
                {
                    result.AddError("sharedFormula.cellReferenceInvalid", "Formula cell reference is invalid.");
                    continue;
                }

                string pattern = NormalizeFormulaForShared(cell.FormulaInfo.Text, reference.Row, reference.Column);
                if (!groups.ContainsKey(pattern))
                {
                    groups.Add(pattern, new List<XlsxWorksheetCell>());
                }

                groups[pattern].Add(cell);
            }
        }
    }

    internal static void CompactSharedFormulaGroup(XlsxSharedFormulaResult result, List<XlsxWorksheetCell> cells, uint sharedIndex)
    {
        XlsxWorksheetCell master = cells[0];
        string reference = BuildSharedFormulaReference(cells);
        master.FormulaInfo.Type = "shared";
        master.FormulaInfo.SharedIndex = sharedIndex;
        master.FormulaInfo.HasSharedIndex = true;
        master.FormulaInfo.Reference = reference;
        master.Formula = master.FormulaInfo.Text;
        int index = 1;
        while (index < cells.Count)
        {
            XlsxWorksheetCell cell = cells[index];
            cell.FormulaInfo.Type = "shared";
            cell.FormulaInfo.SharedIndex = sharedIndex;
            cell.FormulaInfo.HasSharedIndex = true;
            cell.FormulaInfo.Reference = "";
            cell.FormulaInfo.Text = "";
            cell.Formula = "";
            cell.HasFormula = true;
            result.CompactedFollowerCount = result.CompactedFollowerCount + 1;
            index = index + 1;
        }

        result.CompactedGroupCount = result.CompactedGroupCount + 1;
    }

    internal uint GetNextSharedFormulaIndex()
    {
        uint next = 0;
        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row != null && row.Cells != null)
            {
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (cell != null && cell.HasFormula && cell.FormulaInfo != null && cell.FormulaInfo.HasSharedIndex && cell.FormulaInfo.SharedIndex >= next)
                    {
                        next = cell.FormulaInfo.SharedIndex + 1;
                    }
                }
            }
        }

        return next;
    }

    /// <summary>
    /// Calculates formula from the current in-memory model.
    /// </summary>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaCalculationResult CalculateFormula(string formula)
    {
        return XlsxFormulaCalculationEngine.CreateForWorksheet(this).EvaluateFormula(formula);
    }

    /// <summary>
    /// Parses and evaluates the formula stored in one cell, then updates its cached value when calculation succeeds.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaCalculationResult CalculateCellFormula(string reference)
    {
        XlsxFormulaCalculationResult result = new XlsxFormulaCalculationResult();
        XlsxWorksheetCell? cell = GetCellByReference(reference);
        if (cell == null)
        {
            result.AddError("formulaCalculation.cellMissing", "Formula cell was not found.");
            result.Value = XlsxFormulaCalculationValue.CreateError("#REF!");
            return result;
        }

        if (!IsCalculableFormulaCell(cell))
        {
            result.AddError("formulaCalculation.formulaMissing", "Worksheet cell does not contain a calculable formula.");
            result.Value = XlsxFormulaCalculationValue.CreateError("#VALUE!");
            return result;
        }

        XlsxFormulaCalculationResult calculation = CalculateFormula(GetFormulaText(cell));
        CopyFormulaCalculationDiagnostics(result, calculation);
        result.Value = calculation.Value;
        result.Formula = calculation.Formula;
        result.CalculatedFormulaCount = calculation.CalculatedFormulaCount;
        if (calculation.IsValid && calculation.Value != null)
        {
            ApplyFormulaCalculationValueToCell(cell, calculation.Value);
            result.CalculatedFormulaCount = result.CalculatedFormulaCount + 1;
        }

        return result;
    }

    /// <summary>
    /// Calculates cell formula with spill from the current in-memory model.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaCalculationResult CalculateCellFormulaWithSpill(string? reference)
    {
        XlsxFormulaCalculationResult result = new XlsxFormulaCalculationResult();
        XlsxWorksheetCell? cell = GetCellByReference(reference);
        if (cell == null)
        {
            result.AddError("formulaCalculation.cellMissing", "Formula cell was not found.");
            result.Value = XlsxFormulaCalculationValue.CreateError("#REF!");
            return result;
        }

        if (!IsCalculableFormulaCell(cell))
        {
            result.AddError("formulaCalculation.formulaMissing", "Worksheet cell does not contain a calculable formula.");
            result.Value = XlsxFormulaCalculationValue.CreateError("#VALUE!");
            return result;
        }

        XlsxFormulaCalculationResult calculation = CalculateFormula(GetFormulaText(cell));
        CopyFormulaCalculationDiagnostics(result, calculation);
        result.Value = calculation.Value;
        result.Formula = calculation.Formula;
        if (!calculation.IsValid || calculation.Value == null)
        {
            return result;
        }

        if (calculation.Value.Kind == XlsxFormulaCalculationValueKind.Range)
        {
            ApplyFormulaCalculationSpillToWorksheet(result, reference, cell, calculation.Value);
        }
        else
        {
            ApplyFormulaCalculationValueToCell(cell, calculation.Value);
        }

        result.CalculatedFormulaCount = 1;
        return result;
    }

    /// <summary>
    /// Builds formula dependencies and evaluates all supported worksheet formulas in dependency order.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaCalculationResult CalculateAllFormulas()
    {
        XlsxFormulaCalculationResult result = new XlsxFormulaCalculationResult();
        if (Rows == null)
        {
            result.AddError("formulaCalculation.rowsNull", "Worksheet rows list must not be null.");
            result.Value = XlsxFormulaCalculationValue.CreateError("#VALUE!");
            return result;
        }

        XlsxFormulaCalculationEngine engine = XlsxFormulaCalculationEngine.CreateForWorksheet(this);
        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row != null && row.Cells != null)
            {
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (IsCalculableFormulaCell(cell))
                    {
                        XlsxFormulaCalculationResult calculation = engine.EvaluateFormula(GetFormulaText(cell));
                        CopyFormulaCalculationDiagnostics(result, calculation);
                        result.Formula = calculation.Formula;
                        result.Value = calculation.Value;
                        if (calculation.IsValid)
                        {
                            ApplyFormulaCalculationValueToCell(cell, calculation.Value);
                            result.CalculatedFormulaCount = result.CalculatedFormulaCount + 1;
                        }
                    }
                }
            }
        }

        return result;
    }

    internal static bool IsCalculableFormulaCell(XlsxWorksheetCell cell)
    {
        if (cell == null || !cell.HasFormula || cell.FormulaInfo == null)
        {
            return false;
        }

        return GetFormulaText(cell).Length > 0;
    }

    internal static string GetFormulaText(XlsxWorksheetCell cell)
    {
        if (cell == null)
        {
            return "";
        }

        if (cell.FormulaInfo != null && cell.FormulaInfo.Text != null && cell.FormulaInfo.Text.Length > 0)
        {
            return cell.FormulaInfo.Text;
        }

        return XlsxCellValueConverter.NormalizeText(cell.Formula);
    }

    internal static void ApplyFormulaCalculationValueToCell(XlsxWorksheetCell cell, XlsxFormulaCalculationValue? value)
    {
        if (cell == null || value == null)
        {
            return;
        }

        if (cell.FormulaInfo == null)
        {
            cell.FormulaInfo = XlsxCellFormula.CreateNormal(cell.Formula);
        }

        XlsxFormulaCalculationValue? scalar = ScalarFormulaCalculationValue(value);
        string cachedValue = GetFormulaCalculationCachedValue(scalar);
        string cachedType = GetFormulaCalculationCachedType(scalar);
        cell.Value = cachedValue;
        cell.Type = cachedType;
        cell.FormulaInfo.CachedValue = cachedValue;
        cell.FormulaInfo.CachedType = cachedType;
        cell.HasFormula = true;
    }

    internal void ApplyFormulaCalculationSpillToWorksheet(XlsxFormulaCalculationResult result, string? anchorReference, XlsxWorksheetCell anchorCell, XlsxFormulaCalculationValue value)
    {
        XlsxA1CellReferenceResult anchor = XlsxA1CellReferenceConverter.Parse(anchorReference);
        if (!anchor.IsValid || value == null || value.Kind != XlsxFormulaCalculationValueKind.Range)
        {
            result.AddError("formulaCalculation.spillRangeInvalid", "Formula spill range is invalid.");
            return;
        }

        if (!CanApplyFormulaCalculationSpill(anchor.Row, anchor.Column, anchorCell, value))
        {
            XlsxFormulaCalculationValue spillError = XlsxFormulaCalculationValue.CreateError("#SPILL!");
            ApplyFormulaCalculationValueToCell(anchorCell, spillError);
            result.Value = spillError;
            result.AddError("formulaCalculation.spillBlocked", "Formula spill range is blocked by existing cell content.");
            return;
        }

        ApplyFormulaCalculationValueToCell(anchorCell, value);
        int row = 1;
        while (row <= value.Rows)
        {
            int column = 1;
            while (column <= value.Columns)
            {
                if (!(row == 1 && column == 1))
                {
                    XlsxFormulaCalculationValue item = XlsxFormulaCalculationEngine.GetRangeItem(value, row, column);
                    XlsxCellValue cellValue = CreateCellValueFromFormulaCalculationValue(item);
                    SetCellValueAt(anchor.Row + row - 1, anchor.Column + column - 1, cellValue);
                    result.CalculatedSpillCellCount = result.CalculatedSpillCellCount + 1;
                }

                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal bool CanApplyFormulaCalculationSpill(int anchorRow, int anchorColumn, XlsxWorksheetCell anchorCell, XlsxFormulaCalculationValue value)
    {
        int row = 1;
        while (row <= value.Rows)
        {
            int column = 1;
            while (column <= value.Columns)
            {
                if (!(row == 1 && column == 1))
                {
                    XlsxWorksheetCell? cell = GetCellAt(anchorRow + row - 1, anchorColumn + column - 1);
                    if (cell != null && cell != anchorCell && HasBlockingCellContent(cell))
                    {
                        return false;
                    }
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return true;
    }

    internal static bool HasBlockingCellContent(XlsxWorksheetCell cell)
    {
        if (cell == null)
        {
            return false;
        }

        if (cell.HasFormula || XlsxCellValueConverter.NormalizeText(cell.Value).Length > 0 || XlsxCellValueConverter.NormalizeText(cell.Type).Length > 0 || cell.HasInlineString || XlsxCellValueConverter.NormalizeText(cell.InlineStringText).Length > 0)
        {
            return true;
        }

        return false;
    }

    internal static XlsxCellValue CreateCellValueFromFormulaCalculationValue(XlsxFormulaCalculationValue value)
    {
        XlsxFormulaCalculationValue? scalar = ScalarFormulaCalculationValue(value);
        if (scalar == null || scalar.Kind == XlsxFormulaCalculationValueKind.Blank)
        {
            return XlsxCellValue.CreateBlank();
        }

        if (scalar.Kind == XlsxFormulaCalculationValueKind.Number)
        {
            return XlsxCellValue.CreateNumber(scalar.Number);
        }

        if (scalar.Kind == XlsxFormulaCalculationValueKind.Boolean)
        {
            return XlsxCellValue.CreateBoolean(scalar.Boolean);
        }

        if (scalar.Kind == XlsxFormulaCalculationValueKind.Text)
        {
            return XlsxCellValue.CreateText(scalar.Text);
        }

        if (scalar.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return XlsxCellValue.CreateError(scalar.Error);
        }

        return XlsxCellValue.CreateBlank();
    }

    internal static XlsxFormulaCalculationValue? ScalarFormulaCalculationValue(XlsxFormulaCalculationValue value)
    {
        return XlsxFormulaCalculationEngine.Scalarize(value);
    }

    internal static string GetFormulaCalculationCachedValue(XlsxFormulaCalculationValue? value)
    {
        if (value == null || value.Kind == XlsxFormulaCalculationValueKind.Blank)
        {
            return "";
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Number)
        {
            return XlsxXmlValueWriter.WriteDouble(value.Number);
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Boolean)
        {
            return XlsxXmlValueWriter.WriteBoolean(value.Boolean);
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Text)
        {
            return XlsxCellValueConverter.NormalizeText(value.Text);
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return XlsxCellValueConverter.NormalizeText(value.Error);
        }

        return "";
    }

    internal static string GetFormulaCalculationCachedType(XlsxFormulaCalculationValue? value)
    {
        if (value == null)
        {
            return "";
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Boolean)
        {
            return "b";
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Text)
        {
            return "str";
        }

        if (value.Kind == XlsxFormulaCalculationValueKind.Error)
        {
            return "e";
        }

        return "";
    }

    internal static void CopyFormulaCalculationDiagnostics(XlsxFormulaCalculationResult result, XlsxFormulaCalculationResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Performs the update formula references for row insert operation using the current model state.
    /// </summary>
    /// <param name = "startRow">The start row value used by the operation.</param>
    /// <param name = "rowCount">The row count value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaReferenceUpdateResult UpdateFormulaReferencesForRowInsert(int startRow, int rowCount)
    {
        return UpdateWorksheetFormulaReferences(XlsxFormulaReferenceUpdateKind.RowInsert, startRow, 0, rowCount, 0, 0, 0, 0, 0, 0, "", "");
    }

    /// <summary>
    /// Performs the update formula references for row delete operation using the current model state.
    /// </summary>
    /// <param name = "startRow">The start row value used by the operation.</param>
    /// <param name = "rowCount">The row count value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaReferenceUpdateResult UpdateFormulaReferencesForRowDelete(int startRow, int rowCount)
    {
        return UpdateWorksheetFormulaReferences(XlsxFormulaReferenceUpdateKind.RowDelete, startRow, 0, rowCount, 0, 0, 0, 0, 0, 0, "", "");
    }

    /// <summary>
    /// Performs the update formula references for column insert operation using the current model state.
    /// </summary>
    /// <param name = "startColumn">The start column value used by the operation.</param>
    /// <param name = "columnCount">The column count value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaReferenceUpdateResult UpdateFormulaReferencesForColumnInsert(int startColumn, int columnCount)
    {
        return UpdateWorksheetFormulaReferences(XlsxFormulaReferenceUpdateKind.ColumnInsert, 0, startColumn, columnCount, 0, 0, 0, 0, 0, 0, "", "");
    }

    /// <summary>
    /// Performs the update formula references for column delete operation using the current model state.
    /// </summary>
    /// <param name = "startColumn">The start column value used by the operation.</param>
    /// <param name = "columnCount">The column count value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaReferenceUpdateResult UpdateFormulaReferencesForColumnDelete(int startColumn, int columnCount)
    {
        return UpdateWorksheetFormulaReferences(XlsxFormulaReferenceUpdateKind.ColumnDelete, 0, startColumn, columnCount, 0, 0, 0, 0, 0, 0, "", "");
    }

    /// <summary>
    /// Performs the move formula references operation using the current model state.
    /// </summary>
    /// <param name = "sourceFirstRow">The source first row value used by the operation.</param>
    /// <param name = "sourceFirstColumn">The source first column value used by the operation.</param>
    /// <param name = "sourceLastRow">The source last row value used by the operation.</param>
    /// <param name = "sourceLastColumn">The source last column value used by the operation.</param>
    /// <param name = "rowDelta">The row delta value used by the operation.</param>
    /// <param name = "columnDelta">The column delta value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaReferenceUpdateResult MoveFormulaReferences(int sourceFirstRow, int sourceFirstColumn, int sourceLastRow, int sourceLastColumn, int rowDelta, int columnDelta)
    {
        return UpdateWorksheetFormulaReferences(XlsxFormulaReferenceUpdateKind.Move, 0, 0, 0, sourceFirstRow, sourceFirstColumn, sourceLastRow, sourceLastColumn, rowDelta, columnDelta, "", "");
    }

    /// <summary>
    /// Performs the copy formula references operation using the current model state.
    /// </summary>
    /// <param name = "rowDelta">The row delta value used by the operation.</param>
    /// <param name = "columnDelta">The column delta value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaReferenceUpdateResult CopyFormulaReferences(int rowDelta, int columnDelta)
    {
        return UpdateWorksheetFormulaReferences(XlsxFormulaReferenceUpdateKind.Copy, 0, 0, 0, 0, 0, 0, 0, rowDelta, columnDelta, "", "");
    }

    /// <summary>
    /// Performs the rename formula sheet references operation using the current model state.
    /// </summary>
    /// <param name = "oldSheetName">The old sheet name value used by the operation.</param>
    /// <param name = "newSheetName">The new sheet name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaReferenceUpdateResult RenameFormulaSheetReferences(string oldSheetName, string newSheetName)
    {
        return UpdateWorksheetFormulaReferences(XlsxFormulaReferenceUpdateKind.SheetRename, 0, 0, 0, 0, 0, 0, 0, 0, 0, oldSheetName, newSheetName);
    }

    internal XlsxFormulaReferenceUpdateResult UpdateWorksheetFormulaReferences(XlsxFormulaReferenceUpdateKind kind, int startRow, int startColumn, int count, int sourceFirstRow, int sourceFirstColumn, int sourceLastRow, int sourceLastColumn, int rowDelta, int columnDelta, string oldSheetName, string newSheetName)
    {
        XlsxFormulaReferenceUpdateResult result = new XlsxFormulaReferenceUpdateResult();
        if (Rows == null)
        {
            result.AddError("formulaReferenceUpdater.rowsNull", "Worksheet rows list must not be null.");
            return result;
        }

        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row != null && row.Cells != null)
            {
                UpdateFormulaReferencesInRow(result, row, kind, startRow, startColumn, count, sourceFirstRow, sourceFirstColumn, sourceLastRow, sourceLastColumn, rowDelta, columnDelta, oldSheetName, newSheetName);
            }
        }

        return result;
    }

    internal static void UpdateFormulaReferencesInRow(XlsxFormulaReferenceUpdateResult result, XlsxWorksheetRow row, XlsxFormulaReferenceUpdateKind kind, int startRow, int startColumn, int count, int sourceFirstRow, int sourceFirstColumn, int sourceLastRow, int sourceLastColumn, int rowDelta, int columnDelta, string oldSheetName, string newSheetName)
    {
        foreach (XlsxWorksheetCell cell in row.Cells)
        {
            if (cell != null && cell.HasFormula && cell.FormulaInfo != null)
            {
                string formulaText = cell.FormulaInfo.Text;
                if ((formulaText == null || formulaText.Length == 0) && cell.Formula != null && cell.Formula.Length > 0)
                {
                    formulaText = cell.Formula;
                }

                if (formulaText != null && formulaText.Length > 0)
                {
                    XlsxFormulaReferenceUpdateResult update = UpdateFormulaReferences(formulaText, kind, startRow, startColumn, count, sourceFirstRow, sourceFirstColumn, sourceLastRow, sourceLastColumn, rowDelta, columnDelta, oldSheetName, newSheetName);
                    CopyFormulaReferenceUpdateDiagnostics(result, update);
                    result.UpdatedReferenceCount = result.UpdatedReferenceCount + update.UpdatedReferenceCount;
                    result.DeletedReferenceCount = result.DeletedReferenceCount + update.DeletedReferenceCount;
                    result.RenamedSheetCount = result.RenamedSheetCount + update.RenamedSheetCount;
                    if (update.IsValid)
                    {
                        cell.FormulaInfo.Text = update.Formula;
                        cell.Formula = update.Formula;
                    }
                }
            }
        }
    }

    internal static XlsxFormulaReferenceUpdateResult UpdateFormulaForRowInsert(string formula, int startRow, int rowCount)
    {
        return UpdateFormulaReferences(formula, XlsxFormulaReferenceUpdateKind.RowInsert, startRow, 0, rowCount, 0, 0, 0, 0, 0, 0, "", "");
    }

    internal static XlsxFormulaReferenceUpdateResult UpdateFormulaForRowDelete(string formula, int startRow, int rowCount)
    {
        return UpdateFormulaReferences(formula, XlsxFormulaReferenceUpdateKind.RowDelete, startRow, 0, rowCount, 0, 0, 0, 0, 0, 0, "", "");
    }

    internal static XlsxFormulaReferenceUpdateResult UpdateFormulaForColumnInsert(string formula, int startColumn, int columnCount)
    {
        return UpdateFormulaReferences(formula, XlsxFormulaReferenceUpdateKind.ColumnInsert, 0, startColumn, columnCount, 0, 0, 0, 0, 0, 0, "", "");
    }

    internal static XlsxFormulaReferenceUpdateResult UpdateFormulaForColumnDelete(string formula, int startColumn, int columnCount)
    {
        return UpdateFormulaReferences(formula, XlsxFormulaReferenceUpdateKind.ColumnDelete, 0, startColumn, columnCount, 0, 0, 0, 0, 0, 0, "", "");
    }

    /// <summary>
    /// Performs the move formula references operation using the current model state.
    /// </summary>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <param name = "sourceFirstRow">The source first row value used by the operation.</param>
    /// <param name = "sourceFirstColumn">The source first column value used by the operation.</param>
    /// <param name = "sourceLastRow">The source last row value used by the operation.</param>
    /// <param name = "sourceLastColumn">The source last column value used by the operation.</param>
    /// <param name = "rowDelta">The row delta value used by the operation.</param>
    /// <param name = "columnDelta">The column delta value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxFormulaReferenceUpdateResult MoveFormulaReferences(string formula, int sourceFirstRow, int sourceFirstColumn, int sourceLastRow, int sourceLastColumn, int rowDelta, int columnDelta)
    {
        return UpdateFormulaReferences(formula, XlsxFormulaReferenceUpdateKind.Move, 0, 0, 0, sourceFirstRow, sourceFirstColumn, sourceLastRow, sourceLastColumn, rowDelta, columnDelta, "", "");
    }

    /// <summary>
    /// Performs the copy formula references operation using the current model state.
    /// </summary>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <param name = "rowDelta">The row delta value used by the operation.</param>
    /// <param name = "columnDelta">The column delta value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxFormulaReferenceUpdateResult CopyFormulaReferences(string formula, int rowDelta, int columnDelta)
    {
        return UpdateFormulaReferences(formula, XlsxFormulaReferenceUpdateKind.Copy, 0, 0, 0, 0, 0, 0, 0, rowDelta, columnDelta, "", "");
    }

    /// <summary>
    /// Performs the rename formula sheet references operation using the current model state.
    /// </summary>
    /// <param name = "formula">Excel formula text without a leading equals sign.</param>
    /// <param name = "oldSheetName">The old sheet name value used by the operation.</param>
    /// <param name = "newSheetName">The new sheet name value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxFormulaReferenceUpdateResult RenameFormulaSheetReferences(string formula, string oldSheetName, string newSheetName)
    {
        return UpdateFormulaReferences(formula, XlsxFormulaReferenceUpdateKind.SheetRename, 0, 0, 0, 0, 0, 0, 0, 0, 0, oldSheetName, newSheetName);
    }

    internal static XlsxFormulaReferenceUpdateResult UpdateFormulaReferences(string? formula, XlsxFormulaReferenceUpdateKind kind, int startRow, int startColumn, int count, int sourceFirstRow, int sourceFirstColumn, int sourceLastRow, int sourceLastColumn, int rowDelta, int columnDelta, string oldSheetName, string newSheetName)
    {
        XlsxFormulaReferenceUpdateResult result = new XlsxFormulaReferenceUpdateResult();
        result.OriginalFormula = XlsxCellValueConverter.NormalizeText(formula);
        result.Formula = result.OriginalFormula;
        ValidateFormulaReferenceUpdate(result, kind, startRow, startColumn, count, sourceFirstRow, sourceFirstColumn, sourceLastRow, sourceLastColumn, oldSheetName, newSheetName);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxFormulaTokenizeResult tokenizeResult = new XlsxFormulaTokenizer().Tokenize(formula);
        foreach (XlsxDiagnostic diagnostic in tokenizeResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        if (!tokenizeResult.IsValid)
        {
            return result;
        }

        StringBuilder builder = new StringBuilder();
        int cursor = 0;
        int index = 0;
        while (index < tokenizeResult.Tokens.Count)
        {
            XlsxFormulaToken token = tokenizeResult.Tokens[index];
            if (kind == XlsxFormulaReferenceUpdateKind.SheetRename && IsSheetNameBeforeBang(tokenizeResult.Tokens, index) && FormulaSheetNamesEqual(token.Text, oldSheetName))
            {
                AppendFormulaGap(builder, result.OriginalFormula, cursor, token.Start);
                string renamed = XlsxWorkbookPart.QuoteSheetNameForFormula(newSheetName);
                builder.Append(renamed);
                cursor = token.Start + token.Length;
                result.RenamedSheetCount = result.RenamedSheetCount + 1;
                index = index + 1;
                continue;
            }

            if (token.Kind == XlsxFormulaTokenKind.CellReference && IsCellRangeAt(tokenizeResult.Tokens, index))
            {
                XlsxFormulaToken last = tokenizeResult.Tokens[index + 2];
                AppendFormulaGap(builder, result.OriginalFormula, cursor, token.Start);
                string originalRange = result.OriginalFormula.Substring(token.Start, last.Start + last.Length - token.Start);
                string updatedRange = UpdateFormulaRangeReference(originalRange, token.Text, last.Text, kind, startRow, startColumn, count, sourceFirstRow, sourceFirstColumn, sourceLastRow, sourceLastColumn, rowDelta, columnDelta);
                builder.Append(updatedRange);
                CountFormulaReferenceUpdate(result, originalRange, updatedRange);
                cursor = last.Start + last.Length;
                index = index + 3;
                continue;
            }

            if (token.Kind == XlsxFormulaTokenKind.CellReference)
            {
                AppendFormulaGap(builder, result.OriginalFormula, cursor, token.Start);
                string updated = UpdateFormulaCellReference(token.Text, kind, startRow, startColumn, count, sourceFirstRow, sourceFirstColumn, sourceLastRow, sourceLastColumn, rowDelta, columnDelta);
                builder.Append(updated);
                CountFormulaReferenceUpdate(result, token.Text, updated);
                cursor = token.Start + token.Length;
            }

            index = index + 1;
        }

        AppendFormulaGap(builder, result.OriginalFormula, cursor, result.OriginalFormula.Length);
        result.Formula = builder.ToString();
        return result;
    }

    internal static void ValidateFormulaReferenceUpdate(XlsxFormulaReferenceUpdateResult result, XlsxFormulaReferenceUpdateKind kind, int startRow, int startColumn, int count, int sourceFirstRow, int sourceFirstColumn, int sourceLastRow, int sourceLastColumn, string oldSheetName, string newSheetName)
    {
        if (result.OriginalFormula == null)
        {
            result.AddError("formulaReferenceUpdater.formulaNull", "Formula text must not be null.");
            return;
        }

        if ((kind == XlsxFormulaReferenceUpdateKind.RowInsert || kind == XlsxFormulaReferenceUpdateKind.RowDelete) && (startRow <= 0 || count <= 0))
        {
            result.AddError("formulaReferenceUpdater.rowOperationRange", "Row update start and count must be positive.");
        }

        if ((kind == XlsxFormulaReferenceUpdateKind.ColumnInsert || kind == XlsxFormulaReferenceUpdateKind.ColumnDelete) && (startColumn <= 0 || count <= 0))
        {
            result.AddError("formulaReferenceUpdater.columnOperationRange", "Column update start and count must be positive.");
        }

        if (kind == XlsxFormulaReferenceUpdateKind.Move && (sourceFirstRow <= 0 || sourceFirstColumn <= 0 || sourceLastRow < sourceFirstRow || sourceLastColumn < sourceFirstColumn))
        {
            result.AddError("formulaReferenceUpdater.moveRange", "Move source range must be a valid worksheet range.");
        }

        if (kind == XlsxFormulaReferenceUpdateKind.SheetRename && (oldSheetName == null || oldSheetName.Length == 0 || newSheetName == null || newSheetName.Length == 0))
        {
            result.AddError("formulaReferenceUpdater.sheetNameMissing", "Sheet rename requires old and new sheet names.");
        }
    }

    internal static string UpdateFormulaRangeReference(string originalRange, string firstText, string lastText, XlsxFormulaReferenceUpdateKind kind, int startRow, int startColumn, int count, int sourceFirstRow, int sourceFirstColumn, int sourceLastRow, int sourceLastColumn, int rowDelta, int columnDelta)
    {
        if (kind == XlsxFormulaReferenceUpdateKind.RowDelete)
        {
            return UpdateFormulaRangeForRowDelete(originalRange, firstText, lastText, startRow, count);
        }

        if (kind == XlsxFormulaReferenceUpdateKind.ColumnDelete)
        {
            return UpdateFormulaRangeForColumnDelete(originalRange, firstText, lastText, startColumn, count);
        }

        string first = UpdateFormulaCellReference(firstText, kind, startRow, startColumn, count, sourceFirstRow, sourceFirstColumn, sourceLastRow, sourceLastColumn, rowDelta, columnDelta);
        string last = UpdateFormulaCellReference(lastText, kind, startRow, startColumn, count, sourceFirstRow, sourceFirstColumn, sourceLastRow, sourceLastColumn, rowDelta, columnDelta);
        if (first == "#REF!" || last == "#REF!")
        {
            return "#REF!";
        }

        return first + ":" + last;
    }

    internal static string UpdateFormulaCellReference(string text, XlsxFormulaReferenceUpdateKind kind, int startRow, int startColumn, int count, int sourceFirstRow, int sourceFirstColumn, int sourceLastRow, int sourceLastColumn, int rowDelta, int columnDelta)
    {
        XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(text);
        if (!reference.IsValid)
        {
            return text;
        }

        int row = reference.Row;
        int column = reference.Column;
        if (kind == XlsxFormulaReferenceUpdateKind.RowInsert)
        {
            if (row >= startRow)
            {
                row = row + count;
            }
        }
        else if (kind == XlsxFormulaReferenceUpdateKind.RowDelete)
        {
            if (row >= startRow && row < startRow + count)
            {
                return "#REF!";
            }

            if (row >= startRow + count)
            {
                row = row - count;
            }
        }
        else if (kind == XlsxFormulaReferenceUpdateKind.ColumnInsert)
        {
            if (column >= startColumn)
            {
                column = column + count;
            }
        }
        else if (kind == XlsxFormulaReferenceUpdateKind.ColumnDelete)
        {
            if (column >= startColumn && column < startColumn + count)
            {
                return "#REF!";
            }

            if (column >= startColumn + count)
            {
                column = column - count;
            }
        }
        else if (kind == XlsxFormulaReferenceUpdateKind.Move)
        {
            if (row >= sourceFirstRow && row <= sourceLastRow && column >= sourceFirstColumn && column <= sourceLastColumn)
            {
                row = row + rowDelta;
                column = column + columnDelta;
            }
        }
        else if (kind == XlsxFormulaReferenceUpdateKind.Copy)
        {
            if (!reference.IsAbsoluteRow)
            {
                row = row + rowDelta;
            }

            if (!reference.IsAbsoluteColumn)
            {
                column = column + columnDelta;
            }
        }

        string updated = XlsxA1CellReferenceConverter.FormatString(row, column, reference.IsAbsoluteRow, reference.IsAbsoluteColumn);
        if (updated.Length == 0)
        {
            return "#REF!";
        }

        return updated;
    }

    internal static string UpdateFormulaRangeForRowDelete(string originalRange, string firstText, string lastText, int startRow, int rowCount)
    {
        XlsxA1CellReferenceResult first = XlsxA1CellReferenceConverter.Parse(firstText);
        XlsxA1CellReferenceResult last = XlsxA1CellReferenceConverter.Parse(lastText);
        if (!first.IsValid || !last.IsValid)
        {
            return originalRange;
        }

        int minRow = first.Row;
        int maxRow = last.Row;
        bool reversedRows = false;
        if (minRow > maxRow)
        {
            minRow = last.Row;
            maxRow = first.Row;
            reversedRows = true;
        }

        int deleteLast = startRow + rowCount - 1;
        if (maxRow < startRow)
        {
            return originalRange;
        }

        if (minRow > deleteLast)
        {
            return FormatFormulaRange(first.Row - rowCount, first.Column, first.IsAbsoluteRow, first.IsAbsoluteColumn, last.Row - rowCount, last.Column, last.IsAbsoluteRow, last.IsAbsoluteColumn);
        }

        if (minRow >= startRow && maxRow <= deleteLast)
        {
            return "#REF!";
        }

        int newMinRow = minRow;
        int newMaxRow = maxRow;
        if (startRow <= minRow)
        {
            newMinRow = deleteLast + 1;
        }

        if (newMinRow > deleteLast)
        {
            newMinRow = newMinRow - rowCount;
        }

        if (newMaxRow > deleteLast)
        {
            newMaxRow = newMaxRow - rowCount;
        }

        if (reversedRows)
        {
            return FormatFormulaRange(newMaxRow, first.Column, first.IsAbsoluteRow, first.IsAbsoluteColumn, newMinRow, last.Column, last.IsAbsoluteRow, last.IsAbsoluteColumn);
        }

        return FormatFormulaRange(newMinRow, first.Column, first.IsAbsoluteRow, first.IsAbsoluteColumn, newMaxRow, last.Column, last.IsAbsoluteRow, last.IsAbsoluteColumn);
    }

    internal static string UpdateFormulaRangeForColumnDelete(string originalRange, string firstText, string lastText, int startColumn, int columnCount)
    {
        XlsxA1CellReferenceResult first = XlsxA1CellReferenceConverter.Parse(firstText);
        XlsxA1CellReferenceResult last = XlsxA1CellReferenceConverter.Parse(lastText);
        if (!first.IsValid || !last.IsValid)
        {
            return originalRange;
        }

        int minColumn = first.Column;
        int maxColumn = last.Column;
        bool reversedColumns = false;
        if (minColumn > maxColumn)
        {
            minColumn = last.Column;
            maxColumn = first.Column;
            reversedColumns = true;
        }

        int deleteLast = startColumn + columnCount - 1;
        if (maxColumn < startColumn)
        {
            return originalRange;
        }

        if (minColumn > deleteLast)
        {
            return FormatFormulaRange(first.Row, first.Column - columnCount, first.IsAbsoluteRow, first.IsAbsoluteColumn, last.Row, last.Column - columnCount, last.IsAbsoluteRow, last.IsAbsoluteColumn);
        }

        if (minColumn >= startColumn && maxColumn <= deleteLast)
        {
            return "#REF!";
        }

        int newMinColumn = minColumn;
        int newMaxColumn = maxColumn;
        if (startColumn <= minColumn)
        {
            newMinColumn = deleteLast + 1;
        }

        if (newMinColumn > deleteLast)
        {
            newMinColumn = newMinColumn - columnCount;
        }

        if (newMaxColumn > deleteLast)
        {
            newMaxColumn = newMaxColumn - columnCount;
        }

        if (reversedColumns)
        {
            return FormatFormulaRange(first.Row, newMaxColumn, first.IsAbsoluteRow, first.IsAbsoluteColumn, last.Row, newMinColumn, last.IsAbsoluteRow, last.IsAbsoluteColumn);
        }

        return FormatFormulaRange(first.Row, newMinColumn, first.IsAbsoluteRow, first.IsAbsoluteColumn, last.Row, newMaxColumn, last.IsAbsoluteRow, last.IsAbsoluteColumn);
    }

    internal static string FormatFormulaRange(int firstRow, int firstColumn, bool firstAbsoluteRow, bool firstAbsoluteColumn, int lastRow, int lastColumn, bool lastAbsoluteRow, bool lastAbsoluteColumn)
    {
        string first = XlsxA1CellReferenceConverter.FormatString(firstRow, firstColumn, firstAbsoluteRow, firstAbsoluteColumn);
        string last = XlsxA1CellReferenceConverter.FormatString(lastRow, lastColumn, lastAbsoluteRow, lastAbsoluteColumn);
        if (first.Length == 0 || last.Length == 0)
        {
            return "#REF!";
        }

        return first + ":" + last;
    }

    internal static bool IsCellRangeAt(List<XlsxFormulaToken> tokens, int index)
    {
        return index + 2 < tokens.Count && tokens[index + 1].Kind == XlsxFormulaTokenKind.RangeOperator && tokens[index + 2].Kind == XlsxFormulaTokenKind.CellReference;
    }

    internal static bool IsSheetNameBeforeBang(List<XlsxFormulaToken> tokens, int index)
    {
        return index + 1 < tokens.Count && (tokens[index].Kind == XlsxFormulaTokenKind.Identifier || tokens[index].Kind == XlsxFormulaTokenKind.SheetName) && tokens[index + 1].Kind == XlsxFormulaTokenKind.Bang;
    }

    internal static bool FormulaSheetNamesEqual(string tokenText, string sheetName)
    {
        string decoded = DecodeFormulaSheetName(tokenText);
        return string.Equals(decoded, sheetName, System.StringComparison.OrdinalIgnoreCase);
    }

    internal static string DecodeFormulaSheetName(string tokenText)
    {
        if (tokenText == null || tokenText.Length < 2 || tokenText[0] != '\'' || tokenText[tokenText.Length - 1] != '\'')
        {
            return XlsxCellValueConverter.NormalizeText(tokenText);
        }

        StringBuilder builder = new StringBuilder();
        int index = 1;
        while (index < tokenText.Length - 1)
        {
            if (tokenText[index] == '\'' && index + 1 < tokenText.Length - 1 && tokenText[index + 1] == '\'')
            {
                builder.Append("'");
                index = index + 2;
            }
            else
            {
                builder.Append(tokenText[index]);
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal static void AppendFormulaGap(StringBuilder builder, string formula, int start, int end)
    {
        if (end > start)
        {
            builder.Append(formula.Substring(start, end - start));
        }
    }

    internal static void CountFormulaReferenceUpdate(XlsxFormulaReferenceUpdateResult result, string original, string updated)
    {
        if (updated != original)
        {
            result.UpdatedReferenceCount = result.UpdatedReferenceCount + 1;
            if (updated == "#REF!")
            {
                result.DeletedReferenceCount = result.DeletedReferenceCount + 1;
            }
        }
    }

    internal static void CopyFormulaReferenceUpdateDiagnostics(XlsxFormulaReferenceUpdateResult result, XlsxFormulaReferenceUpdateResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static string TranslateFormulaReferences(string formula, int rowDelta, int columnDelta)
    {
        XlsxFormulaTokenizeResult tokenizeResult = new XlsxFormulaTokenizer().Tokenize(formula);
        if (!tokenizeResult.IsValid)
        {
            return XlsxCellValueConverter.NormalizeText(formula);
        }

        StringBuilder builder = new StringBuilder();
        foreach (XlsxFormulaToken token in tokenizeResult.Tokens)
        {
            if (token.Kind == XlsxFormulaTokenKind.CellReference)
            {
                builder.Append(TranslateCellReference(token.Text, rowDelta, columnDelta));
            }
            else
            {
                builder.Append(token.Text);
            }
        }

        return builder.ToString();
    }

    internal static string NormalizeFormulaForShared(string formula, int baseRow, int baseColumn)
    {
        XlsxFormulaTokenizeResult tokenizeResult = new XlsxFormulaTokenizer().Tokenize(formula);
        if (!tokenizeResult.IsValid)
        {
            return XlsxCellValueConverter.NormalizeText(formula);
        }

        StringBuilder builder = new StringBuilder();
        foreach (XlsxFormulaToken token in tokenizeResult.Tokens)
        {
            if (token.Kind == XlsxFormulaTokenKind.CellReference)
            {
                builder.Append(NormalizeCellReferenceForShared(token.Text, baseRow, baseColumn));
            }
            else
            {
                builder.Append(token.Text);
            }
        }

        return builder.ToString();
    }

    internal static string TranslateCellReference(string text, int rowDelta, int columnDelta)
    {
        XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(text);
        if (!reference.IsValid)
        {
            return text;
        }

        int row = reference.Row;
        int column = reference.Column;
        if (!reference.IsAbsoluteRow)
        {
            row = row + rowDelta;
        }

        if (!reference.IsAbsoluteColumn)
        {
            column = column + columnDelta;
        }

        string shifted = XlsxA1CellReferenceConverter.FormatString(row, column, reference.IsAbsoluteRow, reference.IsAbsoluteColumn);
        if (shifted.Length == 0)
        {
            return text;
        }

        return shifted;
    }

    internal static string NormalizeCellReferenceForShared(string text, int baseRow, int baseColumn)
    {
        XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(text);
        if (!reference.IsValid)
        {
            return text;
        }

        StringBuilder builder = new StringBuilder();
        if (reference.IsAbsoluteRow)
        {
            builder.Append("R$");
            builder.Append(reference.Row.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            builder.Append("R[");
            builder.Append((reference.Row - baseRow).ToString(CultureInfo.InvariantCulture));
            builder.Append("]");
        }

        if (reference.IsAbsoluteColumn)
        {
            builder.Append("C$");
            builder.Append(reference.Column.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            builder.Append("C[");
            builder.Append((reference.Column - baseColumn).ToString(CultureInfo.InvariantCulture));
            builder.Append("]");
        }

        return builder.ToString();
    }

    internal static string BuildSharedFormulaReference(List<XlsxWorksheetCell> cells)
    {
        int minRow = XlsxXmlValueParser.MaxSpreadsheetRows;
        int minColumn = XlsxXmlValueParser.MaxSpreadsheetColumns;
        int maxRow = 0;
        int maxColumn = 0;
        foreach (XlsxWorksheetCell cell in cells)
        {
            XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(cell.Reference);
            if (reference.IsValid)
            {
                if (reference.Row < minRow)
                {
                    minRow = reference.Row;
                }

                if (reference.Row > maxRow)
                {
                    maxRow = reference.Row;
                }

                if (reference.Column < minColumn)
                {
                    minColumn = reference.Column;
                }

                if (reference.Column > maxColumn)
                {
                    maxColumn = reference.Column;
                }
            }
        }

        string first = XlsxA1CellReferenceConverter.FormatString(minRow, minColumn, false, false);
        string last = XlsxA1CellReferenceConverter.FormatString(maxRow, maxColumn, false, false);
        if (first == last)
        {
            return first;
        }

        return first + ":" + last;
    }

    internal static bool IsSharedFormula(XlsxWorksheetCell cell)
    {
        return cell != null && cell.HasFormula && cell.FormulaInfo != null && cell.FormulaInfo.Type == "shared";
    }

    internal static bool IsSharedFormulaMaster(XlsxWorksheetCell cell)
    {
        return IsSharedFormula(cell) && cell.FormulaInfo.Text != null && cell.FormulaInfo.Text.Length > 0;
    }

    internal static bool IsNormalFormulaCell(XlsxWorksheetCell cell)
    {
        if (cell == null || !cell.HasFormula || cell.FormulaInfo == null)
        {
            return false;
        }

        if (cell.FormulaInfo.Text == null || cell.FormulaInfo.Text.Length == 0 || cell.Reference == null || cell.Reference.Length == 0)
        {
            return false;
        }

        return cell.FormulaInfo.Type == null || cell.FormulaInfo.Type.Length == 0 || cell.FormulaInfo.Type == "normal";
    }

    /// <summary>
    /// Returns used range from the current model.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetUsedRangeResult GetUsedRange()
    {
        return CalculateUsedRange();
    }

    /// <summary>
    /// Calculates used range from the current in-memory model.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetUsedRangeResult CalculateUsedRange()
    {
        XlsxWorksheetUsedRangeResult result = new XlsxWorksheetUsedRangeResult();
        if (Rows == null)
        {
            result.AddError("worksheetUsedRange.rowsNull", "Worksheet rows list must not be null.");
            return result;
        }

        int rowSequence = 1;
        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row != null && row.Cells != null)
            {
                AddRowToUsedRange(result, row, rowSequence);
            }
            else if (row != null && row.Cells == null)
            {
                result.AddError("worksheetUsedRange.cellsNull", "Worksheet row cell lists must not be null.");
            }

            rowSequence = rowSequence + 1;
        }

        result.UpdateRangeReference();
        return result;
    }

    /// <summary>
    /// Calculates dimension reference from the current in-memory model.
    /// </summary>
    /// <returns>The text produced by the operation.</returns>
    internal string CalculateDimensionReference()
    {
        XlsxWorksheetUsedRangeResult result = CalculateUsedRange();
        if (!result.IsValid || !result.HasRange)
        {
            return "";
        }

        return result.Reference;
    }

    /// <summary>
    /// Performs the apply calculated dimension operation using the current model state.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetUsedRangeResult ApplyCalculatedDimension()
    {
        XlsxWorksheetUsedRangeResult result = CalculateUsedRange();
        if (!result.IsValid)
        {
            return result;
        }

        if (!result.HasRange)
        {
            HasDimension = false;
            if (Dimension != null)
            {
                Dimension.Reference = "";
            }
            return result;
        }

        if (Dimension == null)
        {
            Dimension = new XlsxWorksheetDimension();
        }
        Dimension.Reference = result.Reference;
        HasDimension = true;
        return result;
    }

    internal void RefreshStoredDimensionAfterGridEdit()
    {
        if (!HasDimension)
        {
            return;
        }

        ApplyCalculatedDimension();
    }

    /// <summary>
    /// Returns dimension range from the current model.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetUsedRangeResult GetDimensionRange()
    {
        XlsxWorksheetUsedRangeResult result = new XlsxWorksheetUsedRangeResult();
        if (!HasDimension || Dimension == null || Dimension.Reference == null || Dimension.Reference.Length == 0)
        {
            return result;
        }

        XlsxA1RangeReferenceResult rangeResult = XlsxA1RangeReferenceConverter.Parse(Dimension.Reference);
        if (!rangeResult.IsValid)
        {
            XlsxWorksheetUsedRangeResult.AddRangeDiagnostics(result, rangeResult);
            return result;
        }

        result.SetBounds(rangeResult.First.Row, rangeResult.First.Column);
        if (rangeResult.HasLast)
        {
            result.SetBounds(rangeResult.Last.Row, rangeResult.Last.Column);
        }

        result.UpdateRangeReference();
        return result;
    }

    /// <summary>
    /// Checks merged cells and returns diagnostics rather than throwing for expected invalid input.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetMergeCellsResult ValidateMergedCells()
    {
        XlsxWorksheetMergeCellsResult result = new XlsxWorksheetMergeCellsResult();
        if (MergedCells == null)
        {
            result.AddError("worksheet.mergedCellsNull", "Worksheet merged cells list must not be null.");
            return result;
        }

        int index = 0;
        while (index < MergedCells.Count)
        {
            XlsxWorksheetMergeCell mergeCell = MergedCells[index];
            if (mergeCell == null)
            {
                XlsxDiagnostic diagnostic = result.AddError("worksheet.mergeCellNull", "Worksheet merged cells must not contain null entries.");
                diagnostic.SetLocation(PartPath, "mergeCell", index.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                XlsxWorksheetMergeCellsResult rangeResult = ParseMergeReference(mergeCell.Reference);
                AddMergeDiagnostics(result, rangeResult);
                if (rangeResult.IsValid)
                {
                    ApplyMergeRangeToCell(mergeCell, rangeResult);
                    ValidateMergeDoesNotOverlap(result, rangeResult, index);
                }
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Performs the merge cells operation using the current model state.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetMergeCellsResult MergeCells(string reference)
    {
        XlsxWorksheetMergeCellsResult result = ParseMergeReference(reference);
        if (!result.IsValid)
        {
            return result;
        }

        if (MergedCells == null)
        {
            result.AddError("worksheet.mergedCellsNull", "Worksheet merged cells list must not be null.");
            return result;
        }

        if (FindOverlappingMerge(result, -1) != null)
        {
            result.AddError("worksheet.mergeCellOverlap", "Merged cell range overlaps an existing merged cell range.");
            return result;
        }

        XlsxWorksheetMergeCell mergeCell = new XlsxWorksheetMergeCell();
        ApplyMergeRangeToCell(mergeCell, result);
        MergedCells.Add(mergeCell);
        result.MergedCell = mergeCell;
        result.HasMergedCell = true;
        return result;
    }

    /// <summary>
    /// Performs the merge cells operation using the current model state.
    /// </summary>
    /// <param name = "firstRow">The first row value used by the operation.</param>
    /// <param name = "firstColumn">The first column value used by the operation.</param>
    /// <param name = "lastRow">The last row value used by the operation.</param>
    /// <param name = "lastColumn">The last column value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetMergeCellsResult MergeCellsByCoordinates(int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        string reference = FormatMergeReference(firstRow, firstColumn, lastRow, lastColumn);
        return MergeCells(reference);
    }

    /// <summary>
    /// Performs the unmerge cells operation using the current model state.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetMergeCellsResult UnmergeCells(string reference)
    {
        XlsxWorksheetMergeCellsResult result = ParseMergeSelection(reference);
        if (!result.IsValid)
        {
            return result;
        }

        if (MergedCells == null)
        {
            result.AddError("worksheet.mergedCellsNull", "Worksheet merged cells list must not be null.");
            return result;
        }

        int index = MergedCells.Count - 1;
        while (index >= 0)
        {
            XlsxWorksheetMergeCell mergeCell = MergedCells[index];
            if (mergeCell != null)
            {
                XlsxWorksheetMergeCellsResult existing = ParseMergeReference(mergeCell.Reference);
                if (existing.IsValid && RangesOverlap(result, existing))
                {
                    MergedCells.RemoveAt(index);
                    result.RemovedCount = result.RemovedCount + 1;
                }
            }

            index = index - 1;
        }

        return result;
    }

    /// <summary>
    /// Performs the unmerge cells operation using the current model state.
    /// </summary>
    /// <param name = "firstRow">The first row value used by the operation.</param>
    /// <param name = "firstColumn">The first column value used by the operation.</param>
    /// <param name = "lastRow">The last row value used by the operation.</param>
    /// <param name = "lastColumn">The last column value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetMergeCellsResult UnmergeCellsByCoordinates(int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        string reference = FormatMergeReference(firstRow, firstColumn, lastRow, lastColumn);
        return UnmergeCells(reference);
    }

    /// <summary>
    /// Returns merged cell from the current model.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>The worksheet merge cell produced by the operation.</returns>
    internal XlsxWorksheetMergeCell? GetMergedCell(string reference)
    {
        XlsxWorksheetMergeCellsResult result = ParseMergeSelection(reference);
        if (!result.IsValid || MergedCells == null)
        {
            return null;
        }

        int index = 0;
        while (index < MergedCells.Count)
        {
            XlsxWorksheetMergeCell mergeCell = MergedCells[index];
            if (mergeCell != null)
            {
                XlsxWorksheetMergeCellsResult existing = ParseMergeReference(mergeCell.Reference);
                if (existing.IsValid && RangesOverlap(result, existing))
                {
                    return mergeCell;
                }
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Performs the is cell merged operation using the current model state.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns><c>true</c> when the requested condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsCellMerged(string reference)
    {
        return GetMergedCell(reference) != null;
    }

    /// <summary>
    /// Performs the freeze panes operation using the current model state.
    /// </summary>
    /// <param name = "topLeftCell">The top left cell value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetPaneResult FreezePanes(string topLeftCell)
    {
        XlsxWorksheetPaneResult result = new XlsxWorksheetPaneResult();
        XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(topLeftCell);
        if (!reference.IsValid)
        {
            result.AddError("worksheetPane.topLeftCellInvalid", "Freeze panes top-left cell must be a valid A1 cell reference.");
            XlsxWorksheetPaneResult.AddReferenceDiagnostics(result, reference);
            return result;
        }

        return FreezePanesByCount(reference.Row - 1, reference.Column - 1);
    }

    /// <summary>
    /// Performs the freeze panes operation using the current model state.
    /// </summary>
    /// <param name = "frozenRows">The frozen rows value used by the operation.</param>
    /// <param name = "frozenColumns">The frozen columns value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetPaneResult FreezePanesByCount(int frozenRows, int frozenColumns)
    {
        XlsxWorksheetPaneResult result = ValidateFreezePaneCounts(frozenRows, frozenColumns);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxWorksheetView? view = GetOrCreatePrimarySheetView(result);
        if (!result.IsValid || view == null)
        {
            return result;
        }

        string topLeftCell = XlsxA1CellReferenceConverter.FormatString(frozenRows + 1, frozenColumns + 1, false, false);
        string activePane = GetPaneNameForSplit(frozenRows > 0, frozenColumns > 0);
        XlsxWorksheetPane pane = new XlsxWorksheetPane();
        if (frozenColumns > 0)
        {
            pane.XSplit = frozenColumns;
            pane.HasXSplit = true;
        }

        if (frozenRows > 0)
        {
            pane.YSplit = frozenRows;
            pane.HasYSplit = true;
        }

        pane.TopLeftCell = topLeftCell;
        pane.ActivePane = activePane;
        pane.State = "frozen";
        ApplyPaneToView(view, pane, activePane, topLeftCell);
        result.View = view;
        result.HasView = true;
        result.Pane = pane;
        result.HasPane = true;
        if (view.Selections.Count > 0 && view.Selections[0] != null)
        {
            result.Selection = view.Selections[0];
            result.HasSelection = true;
        }

        return result;
    }

    /// <summary>
    /// Performs the split panes operation using the current model state.
    /// </summary>
    /// <param name = "xSplit">The x split value used by the operation.</param>
    /// <param name = "ySplit">The y split value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetPaneResult SplitPanes(double xSplit, double ySplit)
    {
        return SplitPanesAtCell(xSplit, ySplit, "A1");
    }

    /// <summary>
    /// Performs the split panes operation using the current model state.
    /// </summary>
    /// <param name = "xSplit">The x split value used by the operation.</param>
    /// <param name = "ySplit">The y split value used by the operation.</param>
    /// <param name = "topLeftCell">The top left cell value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetPaneResult SplitPanesAtCell(double xSplit, double ySplit, string topLeftCell)
    {
        XlsxWorksheetPaneResult result = ValidateSplitPaneValues(xSplit, ySplit, topLeftCell);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxWorksheetView? view = GetOrCreatePrimarySheetView(result);
        if (!result.IsValid || view == null)
        {
            return result;
        }

        string activePane = GetPaneNameForSplit(ySplit > 0, xSplit > 0);
        string activeCell = NormalizePaneTopLeftCell(topLeftCell);
        XlsxWorksheetPane pane = new XlsxWorksheetPane();
        if (xSplit > 0)
        {
            pane.XSplit = xSplit;
            pane.HasXSplit = true;
        }

        if (ySplit > 0)
        {
            pane.YSplit = ySplit;
            pane.HasYSplit = true;
        }

        pane.TopLeftCell = activeCell;
        pane.ActivePane = activePane;
        pane.State = "split";
        ApplyPaneToView(view, pane, activePane, activeCell);
        result.View = view;
        result.HasView = true;
        result.Pane = pane;
        result.HasPane = true;
        if (view.Selections.Count > 0 && view.Selections[0] != null)
        {
            result.Selection = view.Selections[0];
            result.HasSelection = true;
        }

        return result;
    }

    /// <summary>
    /// Clears panes while retaining the surrounding model object.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetPaneResult ClearPanes()
    {
        XlsxWorksheetPaneResult result = new XlsxWorksheetPaneResult();
        if (SheetViews == null)
        {
            result.AddError("worksheetPane.sheetViewsNull", "Worksheet sheet views list must not be null.");
            return result;
        }

        if (SheetViews.Count == 0)
        {
            return result;
        }

        XlsxWorksheetView view = SheetViews[0];
        if (view == null)
        {
            result.AddError("worksheetPane.sheetViewNull", "Worksheet primary sheet view must not be null.");
            return result;
        }

        view.Pane = new XlsxWorksheetPane();
        view.HasPane = false;
        if (view.Selections == null)
        {
            view.Selections = new List<XlsxWorksheetSelection>();
        }
        else
        {
            view.Selections.Clear();
        }

        result.View = view;
        result.HasView = true;
        result.Cleared = true;
        return result;
    }

    /// <summary>
    /// Adds comment to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "cellAddress">The cell address value used by the operation.</param>
    /// <param name = "author">The author value used by the operation.</param>
    /// <param name = "text">The text value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCommentResult AddComment(string cellAddress, string author, string text)
    {
        return AddCommentWithVisibility(cellAddress, author, text, false);
    }

    /// <summary>
    /// Adds comment to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "cellAddress">The cell address value used by the operation.</param>
    /// <param name = "author">The author value used by the operation.</param>
    /// <param name = "text">The text value used by the operation.</param>
    /// <param name = "visible">The visible value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetCommentResult AddCommentWithVisibility(string cellAddress, string author, string text, bool visible)
    {
        XlsxWorksheetCommentResult result = ValidateNewComment(cellAddress, author, text);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxWorksheetCellAccessResult cellResult = GetOrCreateCellByReference(result.Reference);
        XlsxWorksheetCommentResult.AddCellAccessDiagnostics(result, cellResult);
        result.Cell = cellResult.Cell;
        result.HasCell = cellResult.HasCell;
        result.CreatedCell = cellResult.CreatedCell;
        result.CreatedRow = cellResult.CreatedRow;
        if (!result.IsValid)
        {
            return result;
        }

        EnsureCommentParts();
        if (CommentsPart.Authors == null)
        {
            result.AddError("worksheet.commentAuthorsNull", "Worksheet comment authors list must not be null.");
            return result;
        }

        if (CommentsPart.Comments == null)
        {
            result.AddError("worksheet.commentsNull", "Worksheet comments list must not be null.");
            return result;
        }

        if (LegacyCommentDrawingPart.Drawings == null)
        {
            result.AddError("worksheet.commentDrawingsNull", "Worksheet comment drawing list must not be null.");
            return result;
        }

        CommentsPart.RawXml = "";
        LegacyCommentDrawingPart.RawXml = "";
        uint authorId = GetOrAddCommentAuthor(NormalizeCommentAuthor(author), result);
        uint shapeId = GetOrCreateCommentShapeId(result.Reference);
        XlsxComment? comment = CommentsPart.GetComment(result.Reference);
        if (comment == null)
        {
            comment = new XlsxComment();
            CommentsPart.Comments.Add(comment);
            result.CreatedComment = true;
        }
        else
        {
            result.ReplacedComment = true;
        }

        ApplyCommentValues(comment, result.Reference, authorId, NormalizeCommentAuthor(author), NormalizeCommentText(text), shapeId);
        XlsxLegacyCommentDrawing? drawing = FindLegacyCommentDrawing(result.Reference);
        if (drawing == null)
        {
            drawing = new XlsxLegacyCommentDrawing();
            LegacyCommentDrawingPart.Drawings.Add(drawing);
            result.CreatedDrawing = true;
        }

        ApplyCommentDrawingValues(drawing, result.Reference, result.Row, result.Column, shapeId, visible);
        ApplyDrawingValuesToComment(comment, drawing);
        result.Comment = comment;
        result.HasComment = true;
        result.CommentsPart = CommentsPart;
        result.HasCommentsPart = true;
        result.DrawingPart = LegacyCommentDrawingPart;
        result.HasDrawingPart = true;
        result.Drawing = drawing;
        result.HasDrawing = true;
        result.AuthorId = authorId;
        result.ShapeId = shapeId;
        return result;
    }

    /// <summary>
    /// Adds hyperlink to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "target">The target value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetHyperlinkResult AddHyperlink(string reference, string target)
    {
        return AddHyperlinkWithDisplay(reference, target, target, "");
    }

    /// <summary>
    /// Adds hyperlink to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "target">The target value used by the operation.</param>
    /// <param name = "display">The display value used by the operation.</param>
    /// <param name = "tooltip">The tooltip value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetHyperlinkResult AddHyperlinkWithDisplay(string reference, string target, string display, string tooltip)
    {
        string normalizedTarget = NormalizeHyperlinkText(target);
        if (XlsxPackagePath.LooksLikeExternalUri(normalizedTarget))
        {
            return AddExternalHyperlink(reference, normalizedTarget, display, tooltip);
        }

        if (normalizedTarget.StartsWith("#"))
        {
            normalizedTarget = normalizedTarget.Substring(1);
        }

        return AddInternalHyperlink(reference, normalizedTarget, display, tooltip);
    }

    /// <summary>
    /// Adds external hyperlink to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "target">The target value used by the operation.</param>
    /// <param name = "display">The display value used by the operation.</param>
    /// <param name = "tooltip">The tooltip value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetHyperlinkResult AddExternalHyperlink(string reference, string target, string display, string tooltip)
    {
        XlsxWorksheetHyperlinkResult result = ValidateNewHyperlink(reference, target);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxWorksheetHyperlink hyperlink = new XlsxWorksheetHyperlink();
        hyperlink.Reference = XlsxA1RangeReferenceConverter.Parse(reference).Text;
        hyperlink.RelationshipId = GetNextHyperlinkRelationshipId();
        hyperlink.Target = NormalizeHyperlinkText(target);
        hyperlink.TargetMode = "External";
        hyperlink.IsExternal = true;
        hyperlink.Display = NormalizeHyperlinkText(display);
        hyperlink.Tooltip = NormalizeHyperlinkText(tooltip);
        Hyperlinks.Add(hyperlink);
        result.Hyperlink = hyperlink;
        result.HasHyperlink = true;
        return result;
    }

    /// <summary>
    /// Adds internal hyperlink to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "location">The location value used by the operation.</param>
    /// <param name = "display">The display value used by the operation.</param>
    /// <param name = "tooltip">The tooltip value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetHyperlinkResult AddInternalHyperlink(string reference, string location, string display, string tooltip)
    {
        XlsxWorksheetHyperlinkResult result = ValidateNewHyperlink(reference, location);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxWorksheetHyperlink hyperlink = new XlsxWorksheetHyperlink();
        hyperlink.Reference = XlsxA1RangeReferenceConverter.Parse(reference).Text;
        hyperlink.Location = NormalizeHyperlinkText(location);
        hyperlink.Display = NormalizeHyperlinkText(display);
        hyperlink.Tooltip = NormalizeHyperlinkText(tooltip);
        Hyperlinks.Add(hyperlink);
        result.Hyperlink = hyperlink;
        result.HasHyperlink = true;
        return result;
    }

    /// <summary>
    /// Performs the resolve hyperlink relationships operation using the current model state.
    /// </summary>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetHyperlinkResult ResolveHyperlinkRelationships(XlsxRelationshipsPart relationships)
    {
        XlsxWorksheetHyperlinkResult result = new XlsxWorksheetHyperlinkResult();
        if (Hyperlinks == null)
        {
            result.AddError("worksheet.hyperlinksNull", "Worksheet hyperlinks list must not be null.");
            return result;
        }

        int index = 0;
        while (index < Hyperlinks.Count)
        {
            XlsxWorksheetHyperlink hyperlink = Hyperlinks[index];
            if (hyperlink == null)
            {
                result.AddError("worksheet.hyperlinkNull", "Worksheet hyperlinks must not contain null entries.");
            }
            else if (hyperlink.RelationshipId != null && hyperlink.RelationshipId.Length > 0)
            {
                ResolveHyperlinkRelationship(result, hyperlink, relationships);
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Performs the resolve drawing relationship operation using the current model state.
    /// </summary>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxSheetDrawingRelationshipResult ResolveDrawingRelationship(XlsxRelationshipsPart relationships)
    {
        XlsxSheetDrawingRelationshipResult result = new XlsxSheetDrawingRelationshipResult();
        if (!HasDrawing)
        {
            return result;
        }

        if (Drawing == null)
        {
            result.AddError("worksheet.drawingNull", "Worksheet drawing reference must not be null when HasDrawing is true.");
            return result;
        }

        result.Drawing = Drawing;
        result.HasDrawing = true;
        ResolveDrawingRelationship(result, Drawing, relationships, "worksheet");
        return result;
    }

    /// <summary>
    /// Performs the resolve pivot table relationships operation using the current model state.
    /// </summary>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetPivotTableRelationshipResult ResolvePivotTableRelationships(XlsxRelationshipsPart relationships)
    {
        XlsxWorksheetPivotTableRelationshipResult result = new XlsxWorksheetPivotTableRelationshipResult();
        if (PivotTableReferences == null)
        {
            result.AddError("worksheet.pivotTableReferencesNull", "Worksheet pivot table reference list must not be null.");
            return result;
        }

        if (PivotTableReferences.Count == 0)
        {
            return result;
        }

        if (relationships == null)
        {
            result.AddError("worksheet.pivotTableRelationshipsMissing", "Pivot table relationship targets cannot be resolved because the relationships part is missing.");
            return result;
        }

        int index = 0;
        foreach (XlsxWorksheetPivotTableReference pivotTable in PivotTableReferences)
        {
            if (pivotTable == null)
            {
                result.AddError("worksheet.pivotTableReferenceNull", "Worksheet pivot table reference list must not contain null entries.");
            }
            else
            {
                ResolvePivotTableRelationship(result, pivotTable, relationships, index);
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Creates hyperlink relationships and initializes the fields required by the OOXML format.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetHyperlinkResult CreateHyperlinkRelationships()
    {
        return CreateHyperlinkRelationshipsForPartPath(PartPath);
    }

    /// <summary>
    /// Creates hyperlink relationships and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "worksheetPartPath">The worksheet part path value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorksheetHyperlinkResult CreateHyperlinkRelationshipsForPartPath(string worksheetPartPath)
    {
        XlsxWorksheetHyperlinkResult result = new XlsxWorksheetHyperlinkResult();
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        relationships.SourcePartPath = XlsxPackagePath.GetRelationshipSourcePartPath(worksheetPartPath);
        relationships.PartPath = XlsxPackagePath.GetRelationshipPartPath(worksheetPartPath);
        result.Relationships = relationships;
        result.HasRelationships = true;
        if (Hyperlinks == null)
        {
            result.AddError("worksheet.hyperlinksNull", "Worksheet hyperlinks list must not be null.");
            return result;
        }

        foreach (XlsxWorksheetHyperlink hyperlink in Hyperlinks)
        {
            if (hyperlink != null && hyperlink.RelationshipId != null && hyperlink.RelationshipId.Length > 0)
            {
                XlsxRelationship? relationship = CreateHyperlinkRelationship(result, hyperlink);
                if (relationship != null)
                {
                    relationships.Relationships.Add(relationship);
                }
            }
        }

        XlsxRelationshipsWriteResult writeValidation = XlsxPartRelationshipsWriter.ValidateRelationshipsPart(relationships);
        AddRelationshipDiagnostics(result, writeValidation.Validation);
        return result;
    }

    internal XlsxDataTableImportResult ValidateDataTableImport(string topLeftCell, System.Data.DataTable? dataTable, bool includeHeaders)
    {
        XlsxDataTableImportResult result = new XlsxDataTableImportResult();
        if (Rows == null)
        {
            result.AddError("dataTableImport.rowsNull", "Worksheet rows list must not be null.");
        }

        if (dataTable == null)
        {
            result.AddError("dataTableImport.tableNull", "DataTable must not be null.");
            return result;
        }

        if (dataTable.Columns.Count == 0)
        {
            result.AddError("dataTableImport.columnsMissing", "DataTable must contain at least one column.");
        }

        XlsxA1CellReferenceResult topLeft = XlsxA1CellReferenceConverter.Parse(topLeftCell);
        if (!topLeft.IsValid)
        {
            XlsxDataTableImportResult.AddRangeDiagnostics(result, topLeft);
            return result;
        }

        int rowCount = dataTable.Rows.Count;
        if (includeHeaders)
        {
            rowCount = rowCount + 1;
            result.HeaderRowCount = 1;
        }

        result.DataRowCount = dataTable.Rows.Count;
        result.ColumnCount = dataTable.Columns.Count;
        if (rowCount == 0)
        {
            result.AddError("dataTableImport.emptyRange", "DataTable import must produce at least one worksheet row.");
            return result;
        }

        int lastRow = topLeft.Row + rowCount - 1;
        int lastColumn = topLeft.Column + dataTable.Columns.Count - 1;
        if (lastRow > XlsxXmlValueParser.MaxSpreadsheetRows || lastColumn > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            result.AddError("dataTableImport.rangeOutsideWorksheet", "DataTable import range must fit within worksheet bounds.");
            return result;
        }

        result.Reference = FormatCellRange(topLeft.Row, topLeft.Column, lastRow, lastColumn);
        return result;
    }

    internal void WriteDataTableImport(XlsxDataTableImportResult result, string topLeftCell, System.Data.DataTable dataTable, bool includeHeaders)
    {
        XlsxA1CellReferenceResult topLeft = XlsxA1CellReferenceConverter.Parse(topLeftCell);
        int row = topLeft.Row;
        if (includeHeaders)
        {
            WriteDataTableHeaders(result, row, topLeft.Column, dataTable);
            row = row + 1;
        }

        WriteDataTableRows(result, row, topLeft.Column, dataTable);
    }

    internal void WriteDataTableHeaders(XlsxDataTableImportResult result, int rowIndex, int firstColumn, System.Data.DataTable dataTable)
    {
        int columnIndex = 0;
        while (columnIndex < dataTable.Columns.Count)
        {
            string header = NormalizeDataTableColumnName(dataTable.Columns[columnIndex], columnIndex);
            WriteDataTableCell(result, rowIndex, firstColumn + columnIndex, XlsxCellValue.CreateText(header));
            columnIndex = columnIndex + 1;
        }
    }

    internal void WriteDataTableRows(XlsxDataTableImportResult result, int firstRow, int firstColumn, System.Data.DataTable dataTable)
    {
        int rowIndex = 0;
        while (rowIndex < dataTable.Rows.Count)
        {
            System.Data.DataRow dataRow = dataTable.Rows[rowIndex];
            int columnIndex = 0;
            while (columnIndex < dataTable.Columns.Count)
            {
                XlsxCellValue value = CreateCellValueFromDataTableValue(dataRow[columnIndex]);
                WriteDataTableCell(result, firstRow + rowIndex, firstColumn + columnIndex, value);
                columnIndex = columnIndex + 1;
            }

            rowIndex = rowIndex + 1;
        }
    }

    internal void WriteDataTableCell(XlsxDataTableImportResult result, int rowIndex, int columnIndex, XlsxCellValue value)
    {
        XlsxWorksheetCellAccessResult cellResult = SetCellValueAt(rowIndex, columnIndex, value);
        XlsxDataTableImportResult.AddCellAccessDiagnostics(result, cellResult);
        if (cellResult.IsValid)
        {
            result.ImportedCellCount = result.ImportedCellCount + 1;
            if (cellResult.CreatedCell)
            {
                result.CreatedCellCount = result.CreatedCellCount + 1;
            }
        }
    }

    internal static XlsxCellValue CreateCellValueFromDataTableValue(object value)
    {
        if (value == null || value == System.DBNull.Value)
        {
            return XlsxCellValue.CreateBlank();
        }

        if (value is bool)
        {
            return XlsxCellValue.CreateBoolean((bool)value);
        }

        if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal)
        {
            return XlsxCellValue.CreateNumber(System.Convert.ToDouble(value, CultureInfo.InvariantCulture));
        }

        if (value is System.DateTime)
        {
            return XlsxCellValue.CreateDateTime((System.DateTime)value);
        }

        if (value is char)
        {
            return XlsxCellValue.CreateText(((char)value).ToString());
        }

        return XlsxCellValue.CreateText(System.Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    internal static string NormalizeDataTableColumnName(System.Data.DataColumn column, int columnIndex)
    {
        string name = "";
        if (column != null)
        {
            name = XlsxCellValueConverter.NormalizeText(column.ColumnName).Trim();
        }

        if (name.Length == 0)
        {
            name = "Column" + (columnIndex + 1).ToString(CultureInfo.InvariantCulture);
        }

        return name;
    }

    internal static string FormatCellRange(int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        XlsxA1RangeReferenceValue range = new XlsxA1RangeReferenceValue();
        range.First.Row = firstRow;
        range.First.Column = firstColumn;
        range.Last.Row = lastRow;
        range.Last.Column = lastColumn;
        range.HasLast = firstRow != lastRow || firstColumn != lastColumn;
        return XlsxA1RangeReferenceConverter.FormatString(range);
    }

    internal XlsxDataTableExportResult ValidateDataTableExportRange(string reference, bool firstRowAsHeaders)
    {
        XlsxDataTableExportResult result = new XlsxDataTableExportResult();
        if (Rows == null)
        {
            result.AddError("dataTableExport.rowsNull", "Worksheet rows list must not be null.");
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(reference);
        if (!range.IsValid)
        {
            XlsxDataTableExportResult.AddRangeDiagnostics(result, range);
            return result;
        }

        NormalizeRangeBounds(range);
        if (GetRangeColumnCount(range) <= 0 || GetRangeRowCount(range) <= 0)
        {
            result.AddError("dataTableExport.rangeEmpty", "DataTable export range must include at least one cell.");
        }

        return result;
    }

    internal void AddExportRangeColumns(System.Data.DataTable dataTable, XlsxA1RangeReferenceResult range, bool firstRowAsHeaders)
    {
        int column = range.First.Column;
        int index = 1;
        while (column <= range.Last.Column)
        {
            string name = "Column" + index.ToString(CultureInfo.InvariantCulture);
            if (firstRowAsHeaders)
            {
                string header = GetExportCellText(range.First.Row, column);
                if (header.Length > 0)
                {
                    name = header;
                }
            }

            dataTable.Columns.Add(CreateUniqueDataTableColumnName(dataTable, name), new object ().GetType());
            column = column + 1;
            index = index + 1;
        }
    }

    internal void AddExportTableColumns(System.Data.DataTable dataTable, XlsxTablePart table)
    {
        int index = 0;
        while (table.Columns != null && index < table.Columns.Count)
        {
            XlsxTableColumn column = table.Columns[index];
            string name = "Column" + (index + 1).ToString(CultureInfo.InvariantCulture);
            if (column != null && column.Name != null && column.Name.Length > 0)
            {
                name = column.Name;
            }

            dataTable.Columns.Add(CreateUniqueDataTableColumnName(dataTable, name), new object ().GetType());
            index = index + 1;
        }
    }

    internal void AddExportRows(XlsxDataTableExportResult result, System.Data.DataTable dataTable, int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        if (firstRow > lastRow || firstColumn > lastColumn)
        {
            return;
        }

        int rowIndex = firstRow;
        while (rowIndex <= lastRow)
        {
            System.Data.DataRow dataRow = dataTable.NewRow();
            int columnIndex = firstColumn;
            int dataColumnIndex = 0;
            while (columnIndex <= lastColumn && dataColumnIndex < dataTable.Columns.Count)
            {
                object value = CreateDataTableObjectFromWorksheetCell(GetCellAt(rowIndex, columnIndex));
                dataRow[dataColumnIndex] = value;
                result.ExportedCellCount = result.ExportedCellCount + 1;
                columnIndex = columnIndex + 1;
                dataColumnIndex = dataColumnIndex + 1;
            }

            dataTable.Rows.Add(dataRow);
            rowIndex = rowIndex + 1;
        }
    }

    internal object CreateDataTableObjectFromWorksheetCell(XlsxWorksheetCell? cell)
    {
        if (cell == null || IsWorksheetCellBlank(cell))
        {
            return System.DBNull.Value;
        }

        string type = XlsxCellValueConverter.NormalizeText(cell.Type);
        string value = XlsxCellValueConverter.NormalizeText(cell.Value);
        if (cell.HasInlineString || type == "inlineStr")
        {
            if (cell.InlineStringText != null && cell.InlineStringText.Length > 0)
            {
                return cell.InlineStringText;
            }
        }

        if (value.Length == 0)
        {
            return System.DBNull.Value;
        }

        if (type == "b")
        {
            XlsxXmlValueParseResult boolean = XlsxXmlValueParser.ParseBoolean(value);
            if (boolean.IsValid)
            {
                return boolean.BooleanValue;
            }
        }

        if (type == "d")
        {
            XlsxXmlValueParseResult dateTime = XlsxXmlValueParser.ParseDateTime(value);
            if (dateTime.IsValid)
            {
                return dateTime.DateTimeValue;
            }
        }

        if (type == "n" || type.Length == 0)
        {
            XlsxXmlValueParseResult number = XlsxXmlValueParser.ParseDouble(value);
            if (number.IsValid)
            {
                return number.DoubleValue;
            }
        }

        return value;
    }

    internal string GetExportCellText(int rowIndex, int columnIndex)
    {
        object value = CreateDataTableObjectFromWorksheetCell(GetCellAt(rowIndex, columnIndex));
        if (value == null || value == System.DBNull.Value)
        {
            return "";
        }

        return XlsxCellValueConverter.NormalizeText(System.Convert.ToString(value, CultureInfo.InvariantCulture)).Trim();
    }

    internal static bool IsWorksheetCellBlank(XlsxWorksheetCell cell)
    {
        if (cell == null)
        {
            return true;
        }

        if (cell.HasFormula)
        {
            return false;
        }

        if (cell.HasInlineString || (cell.InlineStringText != null && cell.InlineStringText.Length > 0))
        {
            return false;
        }

        if (cell.Type != null && cell.Type.Length > 0)
        {
            return false;
        }

        if (cell.Value != null && cell.Value.Length > 0)
        {
            return false;
        }

        return true;
    }

    internal static string CreateUniqueDataTableColumnName(System.Data.DataTable dataTable, string name)
    {
        string baseName = XlsxCellValueConverter.NormalizeText(name).Trim();
        if (baseName.Length == 0)
        {
            baseName = "Column";
        }

        string candidate = baseName;
        int suffix = 2;
        while (dataTable.Columns.Contains(candidate))
        {
            candidate = baseName + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            suffix = suffix + 1;
        }

        return candidate;
    }

    internal XlsxCsvImportResult ValidateCsvImport(string topLeftCell, string csv, XlsxCsvOptions options)
    {
        XlsxCsvImportResult result = new XlsxCsvImportResult();
        if (Rows == null)
        {
            result.AddError("csvImport.rowsNull", "Worksheet rows list must not be null.");
        }

        AddCsvOptionsDiagnostics(result, options);
        if (csv == null)
        {
            result.AddError("csvImport.csvNull", "CSV text must not be null.");
        }

        XlsxA1CellReferenceResult topLeft = XlsxA1CellReferenceConverter.Parse(topLeftCell);
        if (!topLeft.IsValid)
        {
            XlsxCsvImportResult.AddRangeDiagnostics(result, topLeft);
        }

        return result;
    }

    internal void PopulateCsvImportRange(XlsxCsvImportResult result, string topLeftCell, List<List<string>> rows)
    {
        if (rows == null)
        {
            result.AddError("csvImport.parsedRowsNull", "Parsed CSV rows list must not be null.");
            return;
        }

        XlsxA1CellReferenceResult topLeft = XlsxA1CellReferenceConverter.Parse(topLeftCell);
        if (!topLeft.IsValid)
        {
            XlsxCsvImportResult.AddRangeDiagnostics(result, topLeft);
            return;
        }

        int maxColumnCount = GetCsvMaxColumnCount(rows);
        result.ImportedRowCount = rows.Count;
        result.ColumnCount = maxColumnCount;
        result.ImportedCellCount = GetCsvCellCount(rows);
        if (rows.Count == 0 || maxColumnCount == 0)
        {
            return;
        }

        int lastRow = topLeft.Row + rows.Count - 1;
        int lastColumn = topLeft.Column + maxColumnCount - 1;
        if (lastRow > XlsxXmlValueParser.MaxSpreadsheetRows || lastColumn > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            result.AddError("csvImport.rangeOutsideWorksheet", "CSV import range must fit within worksheet bounds.");
            return;
        }

        result.Reference = FormatCellRange(topLeft.Row, topLeft.Column, lastRow, lastColumn);
    }

    internal void WriteCsvImport(XlsxCsvImportResult result, string topLeftCell, List<List<string>> rows)
    {
        XlsxA1CellReferenceResult topLeft = XlsxA1CellReferenceConverter.Parse(topLeftCell);
        int rowOffset = 0;
        while (rowOffset < rows.Count)
        {
            List<string> row = rows[rowOffset];
            int columnOffset = 0;
            while (row != null && columnOffset < row.Count)
            {
                WriteCsvCell(result, topLeft.Row + rowOffset, topLeft.Column + columnOffset, row[columnOffset]);
                columnOffset = columnOffset + 1;
            }

            rowOffset = rowOffset + 1;
        }
    }

    internal void WriteCsvCell(XlsxCsvImportResult result, int rowIndex, int columnIndex, string text)
    {
        XlsxWorksheetCellAccessResult cellResult = SetCellValueAt(rowIndex, columnIndex, XlsxCellValue.CreateText(text));
        XlsxCsvImportResult.AddCellAccessDiagnostics(result, cellResult);
        if (cellResult.IsValid && cellResult.CreatedCell)
        {
            result.CreatedCellCount = result.CreatedCellCount + 1;
        }
    }

    internal XlsxCsvExportResult ValidateCsvExportRange(string reference, XlsxCsvOptions options)
    {
        XlsxCsvExportResult result = new XlsxCsvExportResult();
        if (Rows == null)
        {
            result.AddError("csvExport.rowsNull", "Worksheet rows list must not be null.");
        }

        AddCsvOptionsDiagnostics(result, options);
        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(reference);
        if (!range.IsValid)
        {
            XlsxCsvExportResult.AddRangeDiagnostics(result, range);
            return result;
        }

        NormalizeRangeBounds(range);
        if (GetRangeColumnCount(range) <= 0 || GetRangeRowCount(range) <= 0)
        {
            result.AddError("csvExport.rangeEmpty", "CSV export range must include at least one cell.");
        }

        return result;
    }

    internal static XlsxCsvParseResult ParseCsv(string csv, XlsxCsvOptions options)
    {
        XlsxCsvParseResult result = new XlsxCsvParseResult();
        if (csv == null)
        {
            result.AddError("csv.csvNull", "CSV text must not be null.");
            return result;
        }

        if (csv.Length == 0)
        {
            return result;
        }

        XlsxCsvOptions effectiveOptions = NormalizeCsvOptions(options);
        List<string> row = new List<string>();
        StringBuilder field = new StringBuilder();
        bool inQuotes = false;
        bool quoteClosed = false;
        bool fieldStarted = false;
        bool endedWithRecordSeparator = false;
        bool reportedTrailingText = false;
        int index = 0;
        while (index < csv.Length)
        {
            char value = csv[index];
            if (inQuotes)
            {
                if (value == effectiveOptions.Quote)
                {
                    if (index + 1 < csv.Length && csv[index + 1] == effectiveOptions.Quote)
                    {
                        field.Append(effectiveOptions.Quote);
                        index = index + 2;
                    }
                    else
                    {
                        inQuotes = false;
                        quoteClosed = true;
                        index = index + 1;
                    }
                }
                else
                {
                    field.Append(value);
                    index = index + 1;
                }

                fieldStarted = true;
                endedWithRecordSeparator = false;
            }
            else if (quoteClosed && value != effectiveOptions.Delimiter && value != '\r' && value != '\n')
            {
                if (!reportedTrailingText)
                {
                    result.AddError("csv.quoteTrailingText", "Quoted CSV fields must be followed by a delimiter or record separator.");
                    reportedTrailingText = true;
                }

                field.Append(value);
                quoteClosed = false;
                fieldStarted = true;
                endedWithRecordSeparator = false;
                index = index + 1;
            }
            else if (value == effectiveOptions.Quote && !fieldStarted)
            {
                inQuotes = true;
                fieldStarted = true;
                endedWithRecordSeparator = false;
                index = index + 1;
            }
            else if (value == effectiveOptions.Delimiter)
            {
                row.Add(field.ToString());
                field.Length = 0;
                quoteClosed = false;
                fieldStarted = false;
                endedWithRecordSeparator = false;
                index = index + 1;
            }
            else if (value == '\r' || value == '\n')
            {
                row.Add(field.ToString());
                field.Length = 0;
                result.Rows.Add(row);
                row = new List<string>();
                quoteClosed = false;
                fieldStarted = false;
                endedWithRecordSeparator = true;
                if (value == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n')
                {
                    index = index + 2;
                }
                else
                {
                    index = index + 1;
                }
            }
            else
            {
                field.Append(value);
                fieldStarted = true;
                endedWithRecordSeparator = false;
                index = index + 1;
            }
        }

        if (inQuotes)
        {
            result.AddError("csv.quoteUnclosed", "Quoted CSV field must be closed before the end of the CSV text.");
        }

        if (!endedWithRecordSeparator)
        {
            row.Add(field.ToString());
            result.Rows.Add(row);
        }

        return result;
    }

    internal static void WriteCsvField(StringBuilder builder, string text, XlsxCsvOptions options)
    {
        string value = XlsxCellValueConverter.NormalizeText(text);
        bool mustQuote = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == options.Delimiter || character == options.Quote || character == '\r' || character == '\n')
            {
                mustQuote = true;
            }

            index = index + 1;
        }

        if (!mustQuote)
        {
            builder.Append(value);
            return;
        }

        builder.Append(options.Quote);
        index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == options.Quote)
            {
                builder.Append(options.Quote);
                builder.Append(options.Quote);
            }
            else
            {
                builder.Append(character);
            }

            index = index + 1;
        }

        builder.Append(options.Quote);
    }

    internal string GetCsvCellText(int rowIndex, int columnIndex)
    {
        XlsxWorksheetCell? cell = GetCellAt(rowIndex, columnIndex);
        if (cell == null || IsWorksheetCellBlank(cell))
        {
            return "";
        }

        XlsxCellValueResult valueResult = XlsxCellValueConverter.FromWorksheetCell(cell);
        if (!valueResult.IsValid || valueResult.Value == null)
        {
            return XlsxCellValueConverter.NormalizeText(cell.Value);
        }

        XlsxCellValue value = valueResult.Value;
        if (value.Kind == XlsxCellValueKind.Blank)
        {
            return "";
        }

        if (value.Kind == XlsxCellValueKind.Number)
        {
            return XlsxCellValueConverter.GetExactText(value, XlsxXmlValueWriter.WriteDouble(value.Number));
        }

        if (value.Kind == XlsxCellValueKind.Decimal || value.Kind == XlsxCellValueKind.Duration)
        {
            return XlsxCellValueConverter.NormalizeText(value.ExactText);
        }

        if (value.Kind == XlsxCellValueKind.Boolean)
        {
            if (value.Boolean)
            {
                return "TRUE";
            }

            return "FALSE";
        }

        if (value.Kind == XlsxCellValueKind.DateTime)
        {
            return XlsxCellValueConverter.GetExactText(value, XlsxXmlValueWriter.WriteDateTime(value.DateTime));
        }

        if (value.Kind == XlsxCellValueKind.FormulaCachedValue)
        {
            if (value.HasTypedCachedValue && value.TypedCachedValue != null)
            {
                XlsxWorksheetCell cachedCell = new XlsxWorksheetCell();
                XlsxCellValueResult cachedResult = XlsxCellValueConverter.ApplyToWorksheetCell(value.TypedCachedValue, cachedCell);
                if (cachedResult.IsValid)
                {
                    return cachedCell.Value;
                }
            }
            return XlsxCellValueConverter.NormalizeText(value.CachedValue);
        }

        if (value.Kind == XlsxCellValueKind.SharedString)
        {
            return XlsxCellValueConverter.NormalizeText(value.RawValue);
        }

        if (value.Kind == XlsxCellValueKind.UnknownRawValue)
        {
            return XlsxCellValueConverter.NormalizeText(value.RawValue);
        }

        return XlsxCellValueConverter.NormalizeText(value.Text);
    }

    internal static int GetCsvMaxColumnCount(List<List<string>> rows)
    {
        int maxColumnCount = 0;
        int rowIndex = 0;
        while (rows != null && rowIndex < rows.Count)
        {
            if (rows[rowIndex] != null && rows[rowIndex].Count > maxColumnCount)
            {
                maxColumnCount = rows[rowIndex].Count;
            }

            rowIndex = rowIndex + 1;
        }

        return maxColumnCount;
    }

    internal static int GetCsvCellCount(List<List<string>> rows)
    {
        int cellCount = 0;
        int rowIndex = 0;
        while (rows != null && rowIndex < rows.Count)
        {
            if (rows[rowIndex] != null)
            {
                cellCount = cellCount + rows[rowIndex].Count;
            }

            rowIndex = rowIndex + 1;
        }

        return cellCount;
    }

    internal static XlsxCsvOptions NormalizeCsvOptions(XlsxCsvOptions options)
    {
        if (options == null)
        {
            return new XlsxCsvOptions();
        }

        XlsxCsvOptions normalized = new XlsxCsvOptions();
        normalized.Delimiter = options.Delimiter;
        normalized.Quote = options.Quote;
        normalized.NewLine = options.NewLine;
        return normalized;
    }

    internal static XlsxValidationResult ValidateCsvOptions(XlsxCsvOptions options)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        XlsxCsvOptions effectiveOptions = NormalizeCsvOptions(options);
        if (effectiveOptions.Delimiter == '\0')
        {
            result.AddError("csvOptions.delimiterMissing", "CSV delimiter must not be the null character.");
        }

        if (effectiveOptions.Quote == '\0')
        {
            result.AddError("csvOptions.quoteMissing", "CSV quote character must not be the null character.");
        }

        if (effectiveOptions.Delimiter == effectiveOptions.Quote)
        {
            result.AddError("csvOptions.delimiterQuoteSame", "CSV delimiter and quote character must be different.");
        }

        if (effectiveOptions.Delimiter == '\r' || effectiveOptions.Delimiter == '\n')
        {
            result.AddError("csvOptions.delimiterLineBreak", "CSV delimiter must not be a line break.");
        }

        if (effectiveOptions.Quote == '\r' || effectiveOptions.Quote == '\n')
        {
            result.AddError("csvOptions.quoteLineBreak", "CSV quote character must not be a line break.");
        }

        if (effectiveOptions.NewLine == null || effectiveOptions.NewLine.Length == 0)
        {
            result.AddError("csvOptions.newLineMissing", "CSV newline text must not be null or empty.");
        }

        return result;
    }

    internal static void AddCsvOptionsDiagnostics(XlsxCsvImportResult result, XlsxCsvOptions options)
    {
        XlsxValidationResult validation = ValidateCsvOptions(options);
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddCsvOptionsDiagnostics(XlsxCsvExportResult result, XlsxCsvOptions options)
    {
        XlsxValidationResult validation = ValidateCsvOptions(options);
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal XlsxWorksheetTableResult ValidateNewTable(string reference, string name)
    {
        XlsxWorksheetTableResult result = new XlsxWorksheetTableResult();
        ValidateTableStorage(result);
        XlsxValidationResult nameValidation = ValidateTableName(name);
        CopyDiagnostics(result, nameValidation);
        if (GetTable(name) != null)
        {
            result.AddError("worksheetTable.nameDuplicate", "Worksheet table names must be unique.");
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(reference);
        if (!range.IsValid)
        {
            XlsxWorksheetTableResult.AddRangeDiagnostics(result, range);
        }
        else
        {
            NormalizeRangeBounds(range);
            if (GetRangeColumnCount(range) <= 0 || GetRangeRowCount(range) <= 0)
            {
                result.AddError("worksheetTable.rangeEmpty", "Table range must include at least one cell.");
            }

            if (TableRangeOverlapsExistingTable(null, FormatRange(range)))
            {
                result.AddError("worksheetTable.rangeOverlap", "Table range must not overlap an existing table.");
            }
        }

        return result;
    }

    internal XlsxWorksheetTableResult ValidateExistingTable(string tableName)
    {
        XlsxWorksheetTableResult result = new XlsxWorksheetTableResult();
        ValidateTableStorage(result);
        XlsxTablePart? table = GetTable(tableName);
        if (table == null)
        {
            result.AddError("worksheetTable.notFound", "Worksheet table was not found.");
            return result;
        }

        result.Table = table;
        result.HasTable = true;
        return result;
    }

    internal XlsxWorksheetTableResult ValidateExistingTableAndRange(string tableName, string reference)
    {
        XlsxWorksheetTableResult result = ValidateExistingTable(tableName);
        if (!result.IsValid || result.Table == null)
        {
            return result;
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(reference);
        if (!range.IsValid)
        {
            XlsxWorksheetTableResult.AddRangeDiagnostics(result, range);
            return result;
        }

        NormalizeRangeBounds(range);
        if (GetRangeColumnCount(range) <= 0 || GetRangeRowCount(range) <= 0)
        {
            result.AddError("worksheetTable.rangeEmpty", "Table range must include at least one cell.");
        }

        if (TableRangeOverlapsExistingTable(result.Table, FormatRange(range)))
        {
            result.AddError("worksheetTable.rangeOverlap", "Table range must not overlap an existing table.");
        }

        return result;
    }

    internal XlsxWorksheetTableResult ValidateExistingTableAndColumn(string tableName, string columnName)
    {
        XlsxWorksheetTableResult result = ValidateExistingTable(tableName);
        if (!result.IsValid || result.Table == null)
        {
            return result;
        }

        XlsxTableColumn? column = FindTableColumn(result.Table, columnName);
        if (column == null)
        {
            result.AddError("worksheetTable.columnNotFound", "Worksheet table column was not found.");
            return result;
        }

        result.Column = column;
        result.HasColumn = true;
        return result;
    }

    internal void ValidateTableStorage(XlsxWorksheetTableResult result)
    {
        if (Tables == null)
        {
            result.AddError("worksheetTable.tablesNull", "Worksheet table list must not be null.");
        }

        if (TableReferences == null)
        {
            result.AddError("worksheetTable.tableReferencesNull", "Worksheet table reference list must not be null.");
        }
    }

    internal static XlsxValidationResult ValidateTableName(string name)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        string normalized = NormalizeTableName(name);
        if (normalized.Length == 0)
        {
            result.AddError("worksheetTable.nameMissing", "Table name must not be empty.");
            return result;
        }

        if (normalized.Length > 255)
        {
            result.AddError("worksheetTable.nameTooLong", "Table name must be 255 characters or fewer.");
        }

        if (!IsValidTableNameSyntax(normalized))
        {
            result.AddError("worksheetTable.nameInvalid", "Table name must start with a letter or underscore and contain only letters, digits, or underscores.");
        }

        XlsxA1CellReferenceResult cell = XlsxA1CellReferenceConverter.Parse(normalized);
        if (cell.IsValid)
        {
            result.AddError("worksheetTable.nameCellReference", "Table name must not be an A1 cell reference.");
        }

        return result;
    }

    internal static XlsxValidationResult ValidateTableColumnName(string name)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        string normalized = NormalizeTableColumnName(name);
        if (normalized.Length == 0)
        {
            result.AddError("worksheetTable.columnNameMissing", "Table column name must not be empty.");
        }

        return result;
    }

    internal static bool IsValidTableNameSyntax(string name)
    {
        if (name == null || name.Length == 0)
        {
            return false;
        }

        if (!IsTableNameStartCharacter(name[0]))
        {
            return false;
        }

        int index = 1;
        while (index < name.Length)
        {
            if (!IsTableNameCharacter(name[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsTableNameStartCharacter(char value)
    {
        return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z') || value == '_';
    }

    internal static bool IsTableNameCharacter(char value)
    {
        return IsTableNameStartCharacter(value) || (value >= '0' && value <= '9');
    }

    internal uint GetNextTableId()
    {
        uint next = 1;
        if (Tables == null)
        {
            return next;
        }

        foreach (XlsxTablePart table in Tables)
        {
            if (table != null && table.HasId && table.Id >= next)
            {
                next = table.Id + 1;
            }
        }

        return next;
    }

    internal string GetNextTableRelationshipId()
    {
        int next = 1;
        if (TableReferences != null)
        {
            foreach (XlsxWorksheetTableReference reference in TableReferences)
            {
                if (reference != null)
                {
                    int id = ParseRelationshipNumber(reference.RelationshipId);
                    if (id >= next)
                    {
                        next = id + 1;
                    }
                }
            }
        }

        return "rIdTable" + next.ToString(CultureInfo.InvariantCulture);
    }

    internal static int ParseRelationshipNumber(string relationshipId)
    {
        string text = XlsxCellValueConverter.NormalizeText(relationshipId);
        int index = text.Length - 1;
        while (index >= 0 && text[index] >= '0' && text[index] <= '9')
        {
            index = index - 1;
        }

        if (index == text.Length - 1)
        {
            return 0;
        }

        int value = 0;
        if (int.TryParse(text.Substring(index + 1), NumberStyles.None, CultureInfo.InvariantCulture, out value))
        {
            return value;
        }

        return 0;
    }

    internal void AddGeneratedTableColumns(XlsxWorksheetTableResult result, XlsxTablePart table, XlsxA1RangeReferenceResult range, bool hasHeaderRow)
    {
        int columnCount = GetRangeColumnCount(range);
        int index = 0;
        while (index < columnCount)
        {
            string columnName = "";
            if (hasHeaderRow)
            {
                columnName = GetTableHeaderCellText(range.First.Row, range.First.Column + index);
            }

            if (columnName.Length == 0)
            {
                columnName = CreateUniqueGeneratedColumnName(table, index + 1);
            }

            XlsxTableColumn column = CreateTableColumn((uint)(index + 1), columnName);
            table.Columns.Add(column);
            if (hasHeaderRow)
            {
                WriteTextCell(result, range.First.Row, range.First.Column + index, column.Name);
            }

            index = index + 1;
        }
    }

    internal void TrimOrExtendTableColumns(XlsxWorksheetTableResult result, XlsxTablePart? table, XlsxA1RangeReferenceResult range, int newColumnCount)
    {
        if (table == null || table.Columns == null)
        {
            result.AddError("worksheetTable.columnsNull", "Table columns list must not be null.");
            return;
        }

        while (table.Columns.Count > newColumnCount)
        {
            table.Columns.RemoveAt(table.Columns.Count - 1);
            result.RemovedColumnCount = result.RemovedColumnCount + 1;
        }

        while (table.Columns.Count < newColumnCount)
        {
            string columnName = CreateUniqueGeneratedColumnName(table, table.Columns.Count + 1);
            XlsxTableColumn column = CreateTableColumn(GetNextTableColumnId(table), columnName);
            table.Columns.Add(column);
            result.AddedColumnCount = result.AddedColumnCount + 1;
        }
    }

    internal static XlsxTableColumn CreateTableColumn(uint id, string name)
    {
        XlsxTableColumn column = new XlsxTableColumn();
        column.Id = id;
        column.HasId = true;
        column.Name = NormalizeTableColumnName(name);
        return column;
    }

    internal static uint GetNextTableColumnId(XlsxTablePart table)
    {
        uint next = 1;
        if (table == null || table.Columns == null)
        {
            return next;
        }

        foreach (XlsxTableColumn column in table.Columns)
        {
            if (column != null && column.HasId && column.Id >= next)
            {
                next = column.Id + 1;
            }
        }

        return next;
    }

    internal string CreateUniqueGeneratedColumnName(XlsxTablePart table, int index)
    {
        string candidate = "Column" + index.ToString(CultureInfo.InvariantCulture);
        int suffix = index;
        while (FindTableColumn(table, candidate) != null)
        {
            suffix = suffix + 1;
            candidate = "Column" + suffix.ToString(CultureInfo.InvariantCulture);
        }

        return candidate;
    }

    internal XlsxTableColumn? FindTableColumn(XlsxTablePart? table, string columnName)
    {
        if (table == null)
        {
            return null;
        }

        int index = GetTableColumnIndex(table, columnName);
        if (index < 0)
        {
            return null;
        }

        return table.Columns[index];
    }

    internal int GetTableColumnIndex(XlsxTablePart? table, string columnName)
    {
        if (table == null || table.Columns == null)
        {
            return -1;
        }

        string normalized = NormalizeTableColumnName(columnName);
        int index = 0;
        while (index < table.Columns.Count)
        {
            XlsxTableColumn column = table.Columns[index];
            if (column != null && string.Equals(column.Name, normalized, System.StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void WriteTableHeaderCells(XlsxWorksheetTableResult result, XlsxTablePart table)
    {
        if (table == null || !TableHasHeaderRow(table) || table.Columns == null)
        {
            return;
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(table.Reference);
        if (!range.IsValid)
        {
            XlsxWorksheetTableResult.AddRangeDiagnostics(result, range);
            return;
        }

        int index = 0;
        while (index < table.Columns.Count)
        {
            XlsxTableColumn column = table.Columns[index];
            if (column != null)
            {
                WriteTextCell(result, range.First.Row, range.First.Column + index, column.Name);
            }

            index = index + 1;
        }
    }

    internal void ApplyTableColumnFormulas(XlsxWorksheetTableResult result, XlsxTablePart table)
    {
        if (table == null || table.Columns == null)
        {
            return;
        }

        int index = 0;
        while (index < table.Columns.Count)
        {
            ApplySingleTableColumnFormula(result, table, table.Columns[index], index);
            index = index + 1;
        }
    }

    internal void ApplySingleTableColumnFormula(XlsxWorksheetTableResult result, XlsxTablePart table, XlsxTableColumn column, int columnOffset)
    {
        if (column == null || column.CalculatedColumnFormula == null || column.CalculatedColumnFormula.Length == 0)
        {
            return;
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(table.Reference);
        if (!range.IsValid)
        {
            XlsxWorksheetTableResult.AddRangeDiagnostics(result, range);
            return;
        }

        int firstDataRow = range.First.Row;
        if (TableHasHeaderRow(table))
        {
            firstDataRow = firstDataRow + 1;
        }

        int lastDataRow = range.Last.Row;
        if (TableHasTotalsRow(table))
        {
            lastDataRow = lastDataRow - 1;
        }

        int row = firstDataRow;
        while (row <= lastDataRow)
        {
            XlsxWorksheetCellAccessResult cellResult = GetOrCreateCell(row, range.First.Column + columnOffset);
            XlsxWorksheetTableResult.AddCellAccessDiagnostics(result, cellResult);
            if (cellResult.IsValid && cellResult.HasCell)
            {
                XlsxCellFormula formula = XlsxCellFormula.CreateNormal(column.CalculatedColumnFormula);
                ApplyFormulaInfoToCell(cellResult.Cell, formula, "", "");
                result.FormulaCellCount = result.FormulaCellCount + 1;
                result.UpdatedCellCount = result.UpdatedCellCount + 1;
                if (cellResult.CreatedCell)
                {
                    result.CreatedCellCount = result.CreatedCellCount + 1;
                }
            }

            row = row + 1;
        }
    }

    internal void ApplyTableTotalsRowCells(XlsxWorksheetTableResult result, XlsxTablePart table)
    {
        if (table == null || !TableHasTotalsRow(table) || table.Columns == null)
        {
            return;
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(table.Reference);
        if (!range.IsValid)
        {
            XlsxWorksheetTableResult.AddRangeDiagnostics(result, range);
            return;
        }

        int index = 0;
        while (index < table.Columns.Count)
        {
            XlsxTableColumn column = table.Columns[index];
            if (column != null)
            {
                if (column.TotalsRowFormula != null && column.TotalsRowFormula.Length > 0)
                {
                    WriteFormulaCell(result, range.Last.Row, range.First.Column + index, column.TotalsRowFormula);
                }
                else if (column.TotalsRowLabel != null && column.TotalsRowLabel.Length > 0)
                {
                    WriteTextCell(result, range.Last.Row, range.First.Column + index, column.TotalsRowLabel);
                }
                else
                {
                    string totalFormula = CreateTableTotalFunctionFormula(column);
                    if (totalFormula.Length > 0)
                    {
                        WriteFormulaCell(result, range.Last.Row, range.First.Column + index, totalFormula);
                    }
                    else if (column.HasTotalFunction || column.TotalsRowFunction == "none")
                    {
                        XlsxWorksheetCell? cell = GetCellAt(range.Last.Row, range.First.Column + index);
                        if (cell != null)
                        {
                            XlsxCellValueConverter.ClearTypedCellContent(cell);
                            result.UpdatedCellCount = result.UpdatedCellCount + 1;
                        }
                    }
                }
            }

            index = index + 1;
        }
    }

    internal static string CreateTableTotalFunctionFormula(XlsxTableColumn column)
    {
        string token = XlsxCellValueConverter.NormalizeText(column.TotalsRowFunction);
        if (column.HasTotalFunction)
        {
            token = XlsxTablePartWriter.GetTotalFunctionToken(column.TotalFunction);
        }
        int functionNumber = 0;
        if (token == "average")
        {
            functionNumber = 101;
        }
        else if (token == "countNums")
        {
            functionNumber = 102;
        }
        else if (token == "count")
        {
            functionNumber = 103;
        }
        else if (token == "max")
        {
            functionNumber = 104;
        }
        else if (token == "min")
        {
            functionNumber = 105;
        }
        else if (token == "stdDev")
        {
            functionNumber = 107;
        }
        else if (token == "sum")
        {
            functionNumber = 109;
        }
        else if (token == "var")
        {
            functionNumber = 110;
        }
        if (functionNumber == 0)
        {
            return "";
        }
        string name = XlsxCellValueConverter.NormalizeText(column.Name).Replace("]", "]]", StringComparison.Ordinal);
        return "SUBTOTAL(" + functionNumber.ToString(CultureInfo.InvariantCulture) + ",[" + name + "])";
    }

    internal void ClearTableTotalsRowCells(XlsxWorksheetTableResult result, XlsxTablePart table)
    {
        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(table.Reference);
        if (!range.IsValid || table.Columns == null)
        {
            return;
        }

        int index = 0;
        while (index < table.Columns.Count)
        {
            XlsxWorksheetCell? cell = GetCellAt(range.Last.Row, range.First.Column + index);
            if (cell != null)
            {
                XlsxCellValueConverter.ClearTypedCellContent(cell);
                result.UpdatedCellCount = result.UpdatedCellCount + 1;
            }

            index = index + 1;
        }
    }

    internal static void RefreshTableAutoFilterAndSortRawXml(XlsxTablePart table)
    {
        if (table == null)
        {
            return;
        }

        if (table.AutoFilterEnabled && TableHasHeaderRow(table))
        {
            string filterReference = GetTableAutoFilterReference(table);
            if (filterReference.Length > 0)
            {
                table.AutoFilterRawXml = BuildTableAutoFilterRawXml(table, filterReference);
            }
            else
            {
                table.AutoFilterRawXml = "";
            }
        }
        else if (table.AutoFilterEnabled)
        {
            table.AutoFilterRawXml = "";
        }

        if (table.SortStateEnabled)
        {
            string sortReference = GetTableDataBodyReference(table);
            string columnReference = GetTableSortColumnReference(table);
            if (sortReference.Length > 0 && columnReference.Length > 0)
            {
                table.SortStateRawXml = BuildTableSortStateRawXml(table.NamespaceUri, sortReference, columnReference, table.SortDescending);
            }
            else
            {
                table.SortStateRawXml = "";
            }
        }
    }

    internal static string BuildTableAutoFilterRawXml(XlsxTablePart table, string reference)
    {
        if (table != null && table.AutoFilterRawXml != null && table.AutoFilterRawXml.Length > 0)
        {
            XlsxAutoFilter filter = new XlsxAutoFilterReader().ReadString(table.AutoFilterRawXml);
            if (filter.IsValid)
            {
                filter.NamespaceUri = NormalizeSpreadsheetNamespace(table.NamespaceUri);
                filter.Reference = reference;
                filter.RawXml = "";
                return BuildAutoFilterRawXml(filter);
            }
        }

        string namespaceUri = table == null ? XlsxNamespaces.Spreadsheet : table.NamespaceUri;
        return BuildTableAutoFilterRawXml(namespaceUri, reference);
    }

    internal static string GetTableAutoFilterReference(XlsxTablePart? table)
    {
        if (table == null)
        {
            return "";
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(table.Reference);
        if (!range.IsValid)
        {
            return "";
        }

        NormalizeRangeBounds(range);
        if (TableHasTotalsRow(table) && range.Last.Row > range.First.Row)
        {
            range.Last.Row = range.Last.Row - 1;
        }

        if (range.Last.Row < range.First.Row)
        {
            return "";
        }

        return FormatRange(range);
    }

    internal static string GetTableDataBodyReference(XlsxTablePart? table)
    {
        if (table == null)
        {
            return "";
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(table.Reference);
        if (!range.IsValid)
        {
            return "";
        }

        NormalizeRangeBounds(range);
        if (TableHasHeaderRow(table))
        {
            range.First.Row = range.First.Row + 1;
        }

        if (TableHasTotalsRow(table))
        {
            range.Last.Row = range.Last.Row - 1;
        }

        if (range.First.Row > range.Last.Row)
        {
            return "";
        }

        return FormatRange(range);
    }

    internal static string GetTableSortColumnReference(XlsxTablePart table)
    {
        if (table == null || table.Columns == null)
        {
            return "";
        }

        int columnIndex = -1;
        int index = 0;
        while (index < table.Columns.Count)
        {
            XlsxTableColumn column = table.Columns[index];
            if (column != null && string.Equals(column.Name, table.SortColumnName, System.StringComparison.OrdinalIgnoreCase))
            {
                columnIndex = index;
                break;
            }

            index = index + 1;
        }

        if (columnIndex < 0)
        {
            return "";
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(table.Reference);
        if (!range.IsValid)
        {
            return "";
        }

        NormalizeRangeBounds(range);
        int firstRow = range.First.Row;
        if (TableHasHeaderRow(table))
        {
            firstRow = firstRow + 1;
        }

        int lastRow = range.Last.Row;
        if (TableHasTotalsRow(table))
        {
            lastRow = lastRow - 1;
        }

        if (firstRow > lastRow)
        {
            return "";
        }

        int columnNumber = range.First.Column + columnIndex;
        XlsxA1RangeReferenceValue value = new XlsxA1RangeReferenceValue();
        value.First.Row = firstRow;
        value.First.Column = columnNumber;
        value.Last.Row = lastRow;
        value.Last.Column = columnNumber;
        value.HasLast = firstRow != lastRow;
        return XlsxA1RangeReferenceConverter.FormatString(value);
    }

    internal static string BuildTableAutoFilterRawXml(string namespaceUri, string reference)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (XmlWriter writer = XmlWriter.Create(stream, CreateRawFragmentWriterSettings()))
            {
                writer.WriteStartElement("", "autoFilter", NormalizeSpreadsheetNamespace(namespaceUri));
                writer.WriteAttributeString("ref", reference);
                writer.WriteEndElement();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    internal static string BuildTableSortStateRawXml(string namespaceUri, string sortReference, string columnReference, bool descending)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (XmlWriter writer = XmlWriter.Create(stream, CreateRawFragmentWriterSettings()))
            {
                writer.WriteStartElement("", "sortState", NormalizeSpreadsheetNamespace(namespaceUri));
                writer.WriteAttributeString("ref", sortReference);
                writer.WriteStartElement("", "sortCondition", NormalizeSpreadsheetNamespace(namespaceUri));
                if (descending)
                {
                    writer.WriteAttributeString("descending", XlsxXmlValueWriter.WriteBoolean(descending));
                }

                writer.WriteAttributeString("ref", columnReference);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    internal XlsxWorksheetFilterResult ValidateWorksheetAutoFilterColumn(uint columnId)
    {
        XlsxWorksheetFilterResult result = new XlsxWorksheetFilterResult();
        if (!HasAutoFilter || AutoFilter == null)
        {
            result.AddError("worksheetFilter.autoFilterMissing", "Worksheet auto filter has not been added.");
            return result;
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(AutoFilter.Reference);
        if (!range.IsValid)
        {
            XlsxWorksheetFilterResult.AddRangeDiagnostics(result, range);
            return result;
        }

        if (columnId >= (uint)GetRangeColumnCount(range))
        {
            result.AddError("worksheetFilter.columnOutOfRange", "Auto filter column id must be within the auto filter range.");
            return result;
        }

        AutoFilter.NamespaceUri = NormalizeSpreadsheetNamespace(AutoFilter.NamespaceUri);
        result.AutoFilter = AutoFilter;
        result.HasAutoFilter = true;
        return result;
    }

    internal XlsxWorksheetFilterResult PrepareTableAutoFilter(string tableName, string columnName)
    {
        XlsxWorksheetTableResult tableResult = ValidateExistingTableAndColumn(tableName, columnName);
        XlsxWorksheetFilterResult result = new XlsxWorksheetFilterResult();
        XlsxWorksheetFilterResult.AddTableDiagnostics(result, tableResult);
        if (!result.IsValid || result.Table == null)
        {
            return result;
        }

        XlsxTablePart? table = result.Table;
        if (!TableHasHeaderRow(table))
        {
            result.AddError("worksheetTable.autoFilterHeaderMissing", "Table auto filter requires a visible header row.");
            return result;
        }

        string reference = GetTableAutoFilterReference(table);
        if (reference.Length == 0)
        {
            result.AddError("worksheetTable.autoFilterRangeEmpty", "Table auto filter requires a non-empty filter range.");
            return result;
        }

        XlsxAutoFilter? filter = null;
        if (table.AutoFilterRawXml != null && table.AutoFilterRawXml.Length > 0)
        {
            filter = new XlsxAutoFilterReader().ReadString(table.AutoFilterRawXml);
            foreach (XlsxDiagnostic diagnostic in filter.Validation.Diagnostics)
            {
                result.AddDiagnostic(diagnostic);
            }

            if (!result.IsValid)
            {
                return result;
            }
        }
        else
        {
            filter = new XlsxAutoFilter();
        }

        filter.RawXml = "";
        filter.NamespaceUri = NormalizeSpreadsheetNamespace(table.NamespaceUri);
        filter.Reference = reference;
        table.RawXml = "";
        table.AutoFilterEnabled = true;
        result.AutoFilter = filter;
        result.HasAutoFilter = true;
        return result;
    }

    internal static void SaveTableAutoFilter(XlsxWorksheetFilterResult result)
    {
        if (result.Table == null || result.AutoFilter == null)
        {
            return;
        }

        XlsxAutoFilterWriteResult validation = XlsxAutoFilterWriter.ValidateAutoFilter(result.AutoFilter);
        foreach (XlsxDiagnostic diagnostic in validation.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        if (!result.IsValid)
        {
            return;
        }

        result.Table.AutoFilterRawXml = BuildAutoFilterRawXml(result.AutoFilter);
        result.Table.AutoFilterEnabled = true;
    }

    internal static string BuildAutoFilterRawXml(XlsxAutoFilter? filter)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (XmlWriter writer = XmlWriter.Create(stream, CreateRawFragmentWriterSettings()))
            {
                XlsxAutoFilterWriter.WriteAutoFilterElement(writer, filter);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    internal static XlsxAutoFilterColumn GetOrCreateAutoFilterColumn(XlsxAutoFilter filter, uint columnId)
    {
        foreach (XlsxAutoFilterColumn column in filter.FilterColumns)
        {
            if (column != null && column.HasColumnId && column.ColumnId == columnId)
            {
                return column;
            }
        }

        XlsxAutoFilterColumn created = new XlsxAutoFilterColumn();
        created.ColumnId = columnId;
        created.HasColumnId = true;
        filter.FilterColumns.Add(created);
        return created;
    }

    internal static void RemoveAutoFilterColumn(XlsxAutoFilter filter, uint columnId)
    {
        int index = filter.FilterColumns.Count - 1;
        while (index >= 0)
        {
            XlsxAutoFilterColumn column = filter.FilterColumns[index];
            if (column != null && column.HasColumnId && column.ColumnId == columnId)
            {
                filter.FilterColumns.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    internal static void ClearColumnFilterTypes(XlsxAutoFilterColumn column)
    {
        column.Values = new XlsxAutoFilterValues();
        column.HasValues = false;
        column.CustomFilters = new XlsxCustomFilters();
        column.HasCustomFilters = false;
        column.DynamicFilter = new XlsxDynamicFilter();
        column.HasDynamicFilter = false;
        column.Top10 = new XlsxTop10Filter();
        column.HasTop10 = false;
        column.ColorFilter = new XlsxColorFilter();
        column.HasColorFilter = false;
        column.IconFilter = new XlsxIconFilter();
        column.HasIconFilter = false;
    }

    internal static void ApplyCustomFilter(XlsxAutoFilterColumn column, string firstOperator, string firstValue, string secondOperator, string secondValue, bool andOperator)
    {
        ClearColumnFilterTypes(column);
        column.HasCustomFilters = true;
        column.CustomFilters = new XlsxCustomFilters();
        column.CustomFilters.And = andOperator;
        column.CustomFilters.HasAnd = true;
        AddCustomFilter(column.CustomFilters, firstOperator, firstValue);
        if ((secondOperator != null && secondOperator.Length > 0) || (secondValue != null && secondValue.Length > 0))
        {
            AddCustomFilter(column.CustomFilters, secondOperator, secondValue);
        }
    }

    internal static void AddCustomFilter(XlsxCustomFilters filters, string? operation, string value)
    {
        XlsxCustomFilter filter = new XlsxCustomFilter();
        filter.Operator = XlsxCellValueConverter.NormalizeText(operation);
        filter.Value = XlsxCellValueConverter.NormalizeText(value);
        filters.Filters.Add(filter);
    }

    internal static void ApplyTop10Filter(XlsxAutoFilterColumn column, double value, bool top, bool percent)
    {
        ClearColumnFilterTypes(column);
        column.HasTop10 = true;
        column.Top10 = new XlsxTop10Filter();
        column.Top10.Value = value;
        column.Top10.HasValue = true;
        column.Top10.Top = top;
        column.Top10.HasTop = true;
        column.Top10.Percent = percent;
        column.Top10.HasPercent = true;
    }

    internal static void ApplyColorFilter(XlsxAutoFilterColumn column, uint dxfId, bool cellColor)
    {
        ClearColumnFilterTypes(column);
        column.HasColorFilter = true;
        column.ColorFilter = new XlsxColorFilter();
        column.ColorFilter.DxfId = dxfId;
        column.ColorFilter.HasDxfId = true;
        column.ColorFilter.CellColor = cellColor;
        column.ColorFilter.HasCellColor = true;
    }

    internal static void ApplyIconFilter(XlsxAutoFilterColumn column, string iconSet, uint iconId)
    {
        ClearColumnFilterTypes(column);
        column.HasIconFilter = true;
        column.IconFilter = new XlsxIconFilter();
        column.IconFilter.IconSet = XlsxCellValueConverter.NormalizeText(iconSet);
        column.IconFilter.IconId = iconId;
        column.IconFilter.HasIconId = true;
    }

    internal static XmlWriterSettings CreateRawFragmentWriterSettings()
    {
        XmlWriterSettings settings = XlsxXmlWriterHelpers.CreateStableWriterSettings();
        settings.OmitXmlDeclaration = true;
        return settings;
    }

    internal static string NormalizeSpreadsheetNamespace(string namespaceUri)
    {
        if (namespaceUri == null || namespaceUri.Length == 0)
        {
            return XlsxNamespaces.Spreadsheet;
        }

        return namespaceUri;
    }

    internal void WriteTextCell(XlsxWorksheetTableResult result, int row, int column, string text)
    {
        XlsxWorksheetCellAccessResult cellResult = SetTextValue(XlsxA1CellReferenceConverter.FormatString(row, column, false, false), XlsxCellValueConverter.NormalizeText(text));
        XlsxWorksheetTableResult.AddCellAccessDiagnostics(result, cellResult);
        if (cellResult.IsValid)
        {
            result.UpdatedCellCount = result.UpdatedCellCount + 1;
            if (cellResult.CreatedCell)
            {
                result.CreatedCellCount = result.CreatedCellCount + 1;
            }
        }
    }

    internal void WriteFormulaCell(XlsxWorksheetTableResult result, int row, int column, string formulaText)
    {
        XlsxWorksheetCellAccessResult cellResult = GetOrCreateCell(row, column);
        XlsxWorksheetTableResult.AddCellAccessDiagnostics(result, cellResult);
        if (cellResult.IsValid && cellResult.HasCell)
        {
            XlsxCellFormula formula = XlsxCellFormula.CreateNormal(formulaText);
            ApplyFormulaInfoToCell(cellResult.Cell, formula, "", "");
            result.FormulaCellCount = result.FormulaCellCount + 1;
            result.UpdatedCellCount = result.UpdatedCellCount + 1;
            if (cellResult.CreatedCell)
            {
                result.CreatedCellCount = result.CreatedCellCount + 1;
            }
        }
    }

    internal string GetTableHeaderCellText(int row, int column)
    {
        XlsxWorksheetCell? cell = GetCellAt(row, column);
        if (cell == null)
        {
            return "";
        }

        if (cell.InlineStringText != null && cell.InlineStringText.Length > 0)
        {
            return cell.InlineStringText;
        }

        return XlsxCellValueConverter.NormalizeText(cell.Value);
    }

    internal bool TableRangeOverlapsExistingTable(XlsxTablePart? ignoredTable, string reference)
    {
        if (Tables == null)
        {
            return false;
        }

        XlsxA1RangeReferenceResult candidate = XlsxA1RangeReferenceConverter.Parse(reference);
        if (!candidate.IsValid)
        {
            return false;
        }

        NormalizeRangeBounds(candidate);
        foreach (XlsxTablePart table in Tables)
        {
            if (table != null && table != ignoredTable && RangesOverlap(candidate, table.Reference))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool RangesOverlap(XlsxA1RangeReferenceResult left, string rightReference)
    {
        XlsxA1RangeReferenceResult right = XlsxA1RangeReferenceConverter.Parse(rightReference);
        if (!right.IsValid)
        {
            return false;
        }

        NormalizeRangeBounds(right);
        return left.First.Row <= right.Last.Row && left.Last.Row >= right.First.Row && left.First.Column <= right.Last.Column && left.Last.Column >= right.First.Column;
    }

    internal static void NormalizeRangeBounds(XlsxA1RangeReferenceResult range)
    {
        if (range == null || !range.IsValid)
        {
            return;
        }

        if (!range.HasLast)
        {
            range.Last.Row = range.First.Row;
            range.Last.Column = range.First.Column;
            range.HasLast = true;
        }

        if (range.First.Row > range.Last.Row)
        {
            int temporary = range.First.Row;
            range.First.Row = range.Last.Row;
            range.Last.Row = temporary;
        }

        if (range.First.Column > range.Last.Column)
        {
            int temporary = range.First.Column;
            range.First.Column = range.Last.Column;
            range.Last.Column = temporary;
        }
    }

    internal static string FormatRange(XlsxA1RangeReferenceResult range)
    {
        NormalizeRangeBounds(range);
        XlsxA1RangeReferenceValue value = new XlsxA1RangeReferenceValue();
        value.First.Row = range.First.Row;
        value.First.Column = range.First.Column;
        value.Last.Row = range.Last.Row;
        value.Last.Column = range.Last.Column;
        value.HasLast = range.First.Row != range.Last.Row || range.First.Column != range.Last.Column;
        return XlsxA1RangeReferenceConverter.FormatString(value);
    }

    internal static int GetRangeColumnCount(XlsxA1RangeReferenceResult range)
    {
        NormalizeRangeBounds(range);
        return range.Last.Column - range.First.Column + 1;
    }

    internal static int GetRangeRowCount(XlsxA1RangeReferenceResult range)
    {
        NormalizeRangeBounds(range);
        return range.Last.Row - range.First.Row + 1;
    }

    internal static bool TableHasHeaderRow(XlsxTablePart? table)
    {
        if (table == null)
        {
            return false;
        }

        if (table.HasHeaderRowCount)
        {
            return table.HeaderRowCount > 0;
        }

        return true;
    }

    internal static bool TableHasTotalsRow(XlsxTablePart table)
    {
        if (table == null)
        {
            return false;
        }

        if (table.HasTotalsRowCount)
        {
            return table.TotalsRowCount > 0;
        }

        if (table.HasTotalsRowShown)
        {
            return table.TotalsRowShown;
        }

        return false;
    }

    internal static string NormalizeTableName(string name)
    {
        return XlsxCellValueConverter.NormalizeText(name).Trim();
    }

    internal static string NormalizeTableColumnName(string name)
    {
        return XlsxCellValueConverter.NormalizeText(name).Trim();
    }

    internal static bool TableNamesEqual(string left, string right)
    {
        return string.Equals(NormalizeTableName(left), NormalizeTableName(right), System.StringComparison.OrdinalIgnoreCase);
    }

    internal static void CopyDiagnostics(XlsxWorksheetTableResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    internal XlsxWorksheetCellAccessResult ValidateCellCoordinate(int rowIndex, int columnIndex)
    {
        XlsxWorksheetCellAccessResult result = new XlsxWorksheetCellAccessResult();
        if (!IsValidCellRow(rowIndex))
        {
            result.AddError("worksheetCell.rowRange", "Worksheet cell row must be within worksheet limits.");
        }

        if (!IsValidCellColumn(columnIndex))
        {
            result.AddError("worksheetCell.columnRange", "Worksheet cell column must be within worksheet limits.");
        }

        return result;
    }

    internal void AddRowToUsedRange(XlsxWorksheetUsedRangeResult result, XlsxWorksheetRow row, int rowSequence)
    {
        int cellSequence = 1;
        foreach (XlsxWorksheetCell cell in row.Cells)
        {
            if (cell != null)
            {
                AddCellToUsedRange(result, row, rowSequence, cell, cellSequence);
            }

            cellSequence = cellSequence + 1;
        }
    }

    internal void AddCellToUsedRange(XlsxWorksheetUsedRangeResult result, XlsxWorksheetRow row, int rowSequence, XlsxWorksheetCell cell, int cellSequence)
    {
        if (!IsUsedCell(cell))
        {
            return;
        }

        int rowIndex = GetEffectiveRowIndex(row, rowSequence);
        int columnIndex = cellSequence;
        if (cell.Reference != null && cell.Reference.Length > 0)
        {
            XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(cell.Reference);
            if (!referenceResult.IsValid)
            {
                XlsxWorksheetUsedRangeResult.AddReferenceDiagnostics(result, referenceResult);
                return;
            }

            rowIndex = referenceResult.Row;
            columnIndex = referenceResult.Column;
        }

        if (!IsValidCellCoordinate(rowIndex, columnIndex))
        {
            result.AddError("worksheetUsedRange.cellRange", "Worksheet used range cell address is outside worksheet limits.");
            return;
        }

        result.SetBounds(rowIndex, columnIndex);
    }

    internal XlsxWorksheetCell? GetCellInRow(XlsxWorksheetRow row, int rowSequence, int rowIndex, int columnIndex)
    {
        int cellSequence = 1;
        foreach (XlsxWorksheetCell cell in row.Cells)
        {
            if (cell != null && CellMatchesAddress(cell, row, rowSequence, cellSequence, rowIndex, columnIndex))
            {
                return cell;
            }

            cellSequence = cellSequence + 1;
        }

        return null;
    }

    internal bool ReplaceExistingCell(int rowIndex, int columnIndex, XlsxWorksheetCell replacement)
    {
        if (Rows == null)
        {
            return false;
        }

        int rowSequence = 1;
        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row != null && row.Cells != null)
            {
                int cellIndex = 0;
                int cellSequence = 1;
                while (cellIndex < row.Cells.Count)
                {
                    XlsxWorksheetCell cell = row.Cells[cellIndex];
                    if (cell != null && CellMatchesAddress(cell, row, rowSequence, cellSequence, rowIndex, columnIndex))
                    {
                        row.Cells[cellIndex] = replacement;
                        return true;
                    }

                    cellIndex = cellIndex + 1;
                    cellSequence = cellSequence + 1;
                }
            }

            rowSequence = rowSequence + 1;
        }

        return false;
    }

    internal XlsxWorksheetRow? GetOrCreateRowForCellAccess(XlsxWorksheetCellAccessResult result, int rowIndex)
    {
        if (Rows == null)
        {
            result.AddError("worksheetCell.rowsNull", "Worksheet rows list must not be null.");
            return null;
        }

        XlsxWorksheetRow? existing = GetRow(rowIndex);
        if (existing != null)
        {
            return existing;
        }

        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.RowIndex = (uint)rowIndex;
        InsertRow(row, rowIndex);
        result.CreatedRow = true;
        return row;
    }

    internal void InsertRow(XlsxWorksheetRow row, int rowIndex)
    {
        int insertIndex = 0;
        int sequence = 1;
        while (insertIndex < Rows.Count)
        {
            XlsxWorksheetRow current = Rows[insertIndex];
            if (current != null && GetEffectiveRowIndex(current, sequence) > rowIndex)
            {
                Rows.Insert(insertIndex, row);
                return;
            }

            insertIndex = insertIndex + 1;
            sequence = sequence + 1;
        }

        Rows.Add(row);
    }

    internal void InsertCellInRow(XlsxWorksheetRow? row, XlsxWorksheetCell cell, int columnIndex)
    {
        if (row == null)
        {
            return;
        }

        if (row.Cells == null)
        {
            row.Cells = new List<XlsxWorksheetCell>();
        }

        int insertIndex = 0;
        int sequence = 1;
        while (insertIndex < row.Cells.Count)
        {
            XlsxWorksheetCell current = row.Cells[insertIndex];
            if (current != null && GetEffectiveCellColumn(current, sequence) > columnIndex)
            {
                row.Cells.Insert(insertIndex, cell);
                return;
            }

            insertIndex = insertIndex + 1;
            sequence = sequence + 1;
        }

        row.Cells.Add(cell);
    }

    internal XlsxWorksheetRowEditResult ValidateRowEdit(int rowIndex, int count)
    {
        XlsxWorksheetRowEditResult result = new XlsxWorksheetRowEditResult();
        if (Rows == null)
        {
            result.AddError("worksheetRows.rowsNull", "Worksheet rows list must not be null.");
        }

        if (!IsValidCellRow(rowIndex))
        {
            result.AddError("worksheetRows.rowRange", "Worksheet row index must be within worksheet limits.");
        }

        if (count <= 0)
        {
            result.AddError("worksheetRows.countRange", "Worksheet row edit count must be greater than zero.");
        }

        if (IsValidCellRow(rowIndex) && count > 0 && count > XlsxXmlValueParser.MaxSpreadsheetRows - rowIndex + 1)
        {
            result.AddError("worksheetRows.rangeOutsideWorksheet", "Worksheet row edit range must fit within worksheet bounds.");
        }

        return result;
    }

    internal XlsxWorksheetColumnEditResult ValidateColumnEdit(int columnIndex, int count)
    {
        XlsxWorksheetColumnEditResult result = new XlsxWorksheetColumnEditResult();
        if (Rows == null)
        {
            result.AddError("worksheetColumns.rowsNull", "Worksheet rows list must not be null.");
        }

        if (Columns == null)
        {
            result.AddError("worksheetColumns.columnsNull", "Worksheet columns list must not be null.");
        }

        if (!IsValidCellColumn(columnIndex))
        {
            result.AddError("worksheetColumns.columnRange", "Worksheet column index must be within worksheet limits.");
        }

        if (count <= 0)
        {
            result.AddError("worksheetColumns.countRange", "Worksheet column edit count must be greater than zero.");
        }

        if (IsValidCellColumn(columnIndex) && count > 0 && count > XlsxXmlValueParser.MaxSpreadsheetColumns - columnIndex + 1)
        {
            result.AddError("worksheetColumns.rangeOutsideWorksheet", "Worksheet column edit range must fit within worksheet bounds.");
        }

        return result;
    }

    internal void ValidateRowsForColumnEdit(XlsxWorksheetColumnEditResult result)
    {
        int rowSequence = 1;
        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row == null)
            {
                result.AddError("worksheetColumns.rowNull", "Worksheet rows must not contain null entries.");
            }
            else if (row.Cells == null)
            {
                result.AddError("worksheetColumns.cellsNull", "Worksheet row cell lists must not be null.");
            }

            rowSequence = rowSequence + 1;
        }
    }

    internal XlsxWorksheetColumnMetadataStore LoadColumnMetadataForColumnEdit(XlsxWorksheetColumnEditResult result)
    {
        XlsxWorksheetColumnMetadataStore store = new XlsxWorksheetColumnMetadataStore();
        XlsxWorksheetColumnStoreResult loadResult = store.LoadColumns(Columns);
        foreach (XlsxDiagnostic diagnostic in loadResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        return store;
    }

    internal void ValidateColumnInsertBounds(XlsxWorksheetColumnEditResult result, XlsxWorksheetColumnMetadataStore store, int columnIndex, int count)
    {
        int rowSequence = 1;
        foreach (XlsxWorksheetRow row in Rows)
        {
            if (row != null && row.Cells != null)
            {
                int cellSequence = 1;
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (cell != null)
                    {
                        int effectiveColumn = GetEffectiveCellColumn(cell, cellSequence);
                        if (effectiveColumn >= columnIndex && !IsValidCellColumn(effectiveColumn + count))
                        {
                            result.AddError("worksheetColumns.shiftOutsideWorksheet", "Inserted columns would move existing cells outside worksheet bounds.");
                        }
                    }

                    cellSequence = cellSequence + 1;
                }
            }

            rowSequence = rowSequence + 1;
        }

        foreach (KeyValuePair<int, XlsxWorksheetColumn> item in store.Columns)
        {
            if (item.Key >= columnIndex && !IsValidCellColumn(item.Key + count))
            {
                result.AddError("worksheetColumns.shiftOutsideWorksheet", "Inserted columns would move existing column metadata outside worksheet bounds.");
            }
        }
    }

    internal void MoveCellsForColumnInsertion(XlsxWorksheetColumnEditResult result, int columnIndex, int count)
    {
        int rowSequence = 1;
        foreach (XlsxWorksheetRow row in Rows)
        {
            int rowIndex = GetEffectiveRowIndex(row, rowSequence);
            int cellSequence = 1;
            foreach (XlsxWorksheetCell cell in row.Cells)
            {
                if (cell != null)
                {
                    int effectiveColumn = GetEffectiveCellColumn(cell, cellSequence);
                    if (effectiveColumn >= columnIndex)
                    {
                        MoveCellForColumnEdit(result, cell, rowIndex, effectiveColumn + count);
                    }
                }

                cellSequence = cellSequence + 1;
            }

            SortCellsInRowByEffectiveColumn(row);
            rowSequence = rowSequence + 1;
        }
    }

    internal void MoveCellsForColumnDeletion(XlsxWorksheetColumnEditResult result, int columnIndex, int count)
    {
        int lastDeletedColumn = columnIndex + count - 1;
        int rowSequence = 1;
        foreach (XlsxWorksheetRow row in Rows)
        {
            int rowIndex = GetEffectiveRowIndex(row, rowSequence);
            List<XlsxWorksheetCell> keptCells = new List<XlsxWorksheetCell>();
            int cellSequence = 1;
            foreach (XlsxWorksheetCell cell in row.Cells)
            {
                if (cell != null)
                {
                    int effectiveColumn = GetEffectiveCellColumn(cell, cellSequence);
                    if (effectiveColumn >= columnIndex && effectiveColumn <= lastDeletedColumn)
                    {
                        result.RemovedCellCount = result.RemovedCellCount + 1;
                        result.AffectedCellCount = result.AffectedCellCount + 1;
                    }
                    else
                    {
                        if (effectiveColumn > lastDeletedColumn)
                        {
                            MoveCellForColumnEdit(result, cell, rowIndex, effectiveColumn - count);
                        }

                        keptCells.Add(cell);
                    }
                }

                cellSequence = cellSequence + 1;
            }

            row.Cells.Clear();
            foreach (XlsxWorksheetCell cell in keptCells)
            {
                row.Cells.Add(cell);
            }

            SortCellsInRowByEffectiveColumn(row);
            rowSequence = rowSequence + 1;
        }
    }

    internal void MoveMetadataForColumnInsertion(XlsxWorksheetColumnEditResult result, XlsxWorksheetColumnMetadataStore store, int columnIndex, int count)
    {
        XlsxWorksheetColumnMetadataStore updated = new XlsxWorksheetColumnMetadataStore();
        List<int> keys = store.GetSortedColumnIndexes();
        foreach (int key in keys)
        {
            int targetColumn = key;
            if (key >= columnIndex)
            {
                targetColumn = key + count;
                result.MovedColumnCount = result.MovedColumnCount + 1;
                result.AffectedColumnCount = result.AffectedColumnCount + 1;
            }

            XlsxWorksheetColumnStoreResult setResult = updated.SetColumn(targetColumn, store.Columns[key]);
            AddColumnStoreDiagnostics(result, setResult);
        }

        ApplyColumnMetadataForColumnEdit(result, updated);
    }

    internal void MoveMetadataForColumnDeletion(XlsxWorksheetColumnEditResult result, XlsxWorksheetColumnMetadataStore store, int columnIndex, int count)
    {
        int lastDeletedColumn = columnIndex + count - 1;
        XlsxWorksheetColumnMetadataStore updated = new XlsxWorksheetColumnMetadataStore();
        List<int> keys = store.GetSortedColumnIndexes();
        foreach (int key in keys)
        {
            if (key >= columnIndex && key <= lastDeletedColumn)
            {
                result.RemovedColumnCount = result.RemovedColumnCount + 1;
                result.AffectedColumnCount = result.AffectedColumnCount + 1;
            }
            else
            {
                int targetColumn = key;
                if (key > lastDeletedColumn)
                {
                    targetColumn = key - count;
                    result.MovedColumnCount = result.MovedColumnCount + 1;
                    result.AffectedColumnCount = result.AffectedColumnCount + 1;
                }

                XlsxWorksheetColumnStoreResult setResult = updated.SetColumn(targetColumn, store.Columns[key]);
                AddColumnStoreDiagnostics(result, setResult);
            }
        }

        ApplyColumnMetadataForColumnEdit(result, updated);
    }

    internal void ApplyColumnMetadataForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxWorksheetColumnMetadataStore store)
    {
        XlsxWorksheetColumnStoreResult applyResult = store.ApplyToWorksheetPart(this);
        AddColumnStoreDiagnostics(result, applyResult);
    }

    internal void MoveCellForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxWorksheetCell cell, int rowIndex, int newColumnIndex)
    {
        int referenceRow = rowIndex;
        bool absoluteRow = false;
        bool absoluteColumn = false;
        if (cell.Reference != null && cell.Reference.Length > 0)
        {
            XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(cell.Reference);
            if (reference.IsValid)
            {
                referenceRow = reference.Row;
                absoluteRow = reference.IsAbsoluteRow;
                absoluteColumn = reference.IsAbsoluteColumn;
            }
        }

        cell.Reference = XlsxA1CellReferenceConverter.FormatString(referenceRow, newColumnIndex, absoluteRow, absoluteColumn);
        result.MovedCellCount = result.MovedCellCount + 1;
        result.AffectedCellCount = result.AffectedCellCount + 1;
        result.UpdatedCellReferenceCount = result.UpdatedCellReferenceCount + 1;
    }

    internal void SortCellsInRowByEffectiveColumn(XlsxWorksheetRow row)
    {
        if (row == null || row.Cells == null)
        {
            return;
        }

        int outer = 1;
        while (outer < row.Cells.Count)
        {
            XlsxWorksheetCell current = row.Cells[outer];
            int currentColumn = GetEffectiveCellColumn(current, outer + 1);
            int inner = outer - 1;
            while (inner >= 0 && GetEffectiveCellColumn(row.Cells[inner], inner + 1) > currentColumn)
            {
                row.Cells[inner + 1] = row.Cells[inner];
                inner = inner - 1;
            }

            row.Cells[inner + 1] = current;
            outer = outer + 1;
        }
    }

    internal static void AddColumnStoreDiagnostics(XlsxWorksheetColumnEditResult result, XlsxWorksheetColumnStoreResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal void UpdateWorksheetStructuresForRowInsert(XlsxWorksheetRowEditResult result, int rowIndex, int count)
    {
        XlsxFormulaReferenceUpdateResult formulaResult = UpdateFormulaReferencesForRowInsert(rowIndex, count);
        AddFormulaUpdateToRowEditResult(result, formulaResult);
        UpdateMergedCellReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowInsert, rowIndex, count);
        UpdateHyperlinkReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowInsert, rowIndex, count);
        UpdateCommentReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowInsert, rowIndex, count);
        UpdateLegacyCommentDrawingReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowInsert, rowIndex, count);
        UpdatePageBreakIdsForRowEdit(result, RowBreaks, rowIndex, count, false);
        UpdateAutoFilterReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowInsert, rowIndex, count);
        UpdateDataValidationReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowInsert, rowIndex, count);
        UpdateConditionalFormattingReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowInsert, rowIndex, count);
        UpdateTableReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowInsert, rowIndex, count);
    }

    internal void UpdateWorksheetStructuresForRowDelete(XlsxWorksheetRowEditResult result, int rowIndex, int count)
    {
        XlsxFormulaReferenceUpdateResult formulaResult = UpdateFormulaReferencesForRowDelete(rowIndex, count);
        AddFormulaUpdateToRowEditResult(result, formulaResult);
        UpdateMergedCellReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowDelete, rowIndex, count);
        UpdateHyperlinkReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowDelete, rowIndex, count);
        UpdateCommentReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowDelete, rowIndex, count);
        UpdateLegacyCommentDrawingReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowDelete, rowIndex, count);
        UpdatePageBreakIdsForRowEdit(result, RowBreaks, rowIndex, count, true);
        UpdateAutoFilterReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowDelete, rowIndex, count);
        UpdateDataValidationReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowDelete, rowIndex, count);
        UpdateConditionalFormattingReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowDelete, rowIndex, count);
        UpdateTableReferencesForRowEdit(result, XlsxFormulaReferenceUpdateKind.RowDelete, rowIndex, count);
    }

    internal void UpdateWorksheetStructuresForColumnInsert(XlsxWorksheetColumnEditResult result, int columnIndex, int count)
    {
        XlsxFormulaReferenceUpdateResult formulaResult = UpdateFormulaReferencesForColumnInsert(columnIndex, count);
        AddFormulaUpdateToColumnEditResult(result, formulaResult);
        UpdateMergedCellReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnInsert, columnIndex, count);
        UpdateHyperlinkReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnInsert, columnIndex, count);
        UpdateCommentReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnInsert, columnIndex, count);
        UpdateLegacyCommentDrawingReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnInsert, columnIndex, count);
        UpdatePageBreakIdsForColumnEdit(result, ColumnBreaks, columnIndex, count, false);
        UpdateAutoFilterReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnInsert, columnIndex, count);
        UpdateDataValidationReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnInsert, columnIndex, count);
        UpdateConditionalFormattingReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnInsert, columnIndex, count);
        UpdateTableReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnInsert, columnIndex, count);
    }

    internal void UpdateWorksheetStructuresForColumnDelete(XlsxWorksheetColumnEditResult result, int columnIndex, int count)
    {
        XlsxFormulaReferenceUpdateResult formulaResult = UpdateFormulaReferencesForColumnDelete(columnIndex, count);
        AddFormulaUpdateToColumnEditResult(result, formulaResult);
        UpdateMergedCellReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnDelete, columnIndex, count);
        UpdateHyperlinkReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnDelete, columnIndex, count);
        UpdateCommentReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnDelete, columnIndex, count);
        UpdateLegacyCommentDrawingReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnDelete, columnIndex, count);
        UpdatePageBreakIdsForColumnEdit(result, ColumnBreaks, columnIndex, count, true);
        UpdateAutoFilterReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnDelete, columnIndex, count);
        UpdateDataValidationReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnDelete, columnIndex, count);
        UpdateConditionalFormattingReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnDelete, columnIndex, count);
        UpdateTableReferencesForColumnEdit(result, XlsxFormulaReferenceUpdateKind.ColumnDelete, columnIndex, count);
    }

    internal void UpdateMergedCellReferencesForRowEdit(XlsxWorksheetRowEditResult result, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int count)
    {
        if (MergedCells == null)
        {
            result.AddError("worksheetStructures.mergedCellsNull", "Worksheet merged cells list must not be null.");
            return;
        }

        int index = MergedCells.Count - 1;
        while (index >= 0)
        {
            XlsxWorksheetMergeCell mergeCell = MergedCells[index];
            if (mergeCell != null)
            {
                string updated = UpdateStructuralReference(mergeCell.Reference, kind, rowIndex, 0, count);
                if (StructuralReferenceDeleted(updated))
                {
                    MergedCells.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else if (updated != mergeCell.Reference)
                {
                    if (!ApplyUpdatedMergeReference(mergeCell, updated))
                    {
                        MergedCells.RemoveAt(index);
                        result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                    }
                    else
                    {
                        result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                    }
                }
            }

            index = index - 1;
        }
    }

    internal void UpdateMergedCellReferencesForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxFormulaReferenceUpdateKind kind, int columnIndex, int count)
    {
        if (MergedCells == null)
        {
            result.AddError("worksheetStructures.mergedCellsNull", "Worksheet merged cells list must not be null.");
            return;
        }

        int index = MergedCells.Count - 1;
        while (index >= 0)
        {
            XlsxWorksheetMergeCell mergeCell = MergedCells[index];
            if (mergeCell != null)
            {
                string updated = UpdateStructuralReference(mergeCell.Reference, kind, 0, columnIndex, count);
                if (StructuralReferenceDeleted(updated))
                {
                    MergedCells.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else if (updated != mergeCell.Reference)
                {
                    if (!ApplyUpdatedMergeReference(mergeCell, updated))
                    {
                        MergedCells.RemoveAt(index);
                        result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                    }
                    else
                    {
                        result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                    }
                }
            }

            index = index - 1;
        }
    }

    internal void UpdateHyperlinkReferencesForRowEdit(XlsxWorksheetRowEditResult result, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int count)
    {
        if (Hyperlinks == null)
        {
            result.AddError("worksheetStructures.hyperlinksNull", "Worksheet hyperlinks list must not be null.");
            return;
        }

        int index = Hyperlinks.Count - 1;
        while (index >= 0)
        {
            XlsxWorksheetHyperlink hyperlink = Hyperlinks[index];
            if (hyperlink != null)
            {
                string updated = UpdateStructuralReference(hyperlink.Reference, kind, rowIndex, 0, count);
                if (StructuralReferenceDeleted(updated))
                {
                    Hyperlinks.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else if (updated != hyperlink.Reference)
                {
                    hyperlink.Reference = updated;
                    result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                }
            }

            index = index - 1;
        }
    }

    internal void UpdateHyperlinkReferencesForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxFormulaReferenceUpdateKind kind, int columnIndex, int count)
    {
        if (Hyperlinks == null)
        {
            result.AddError("worksheetStructures.hyperlinksNull", "Worksheet hyperlinks list must not be null.");
            return;
        }

        int index = Hyperlinks.Count - 1;
        while (index >= 0)
        {
            XlsxWorksheetHyperlink hyperlink = Hyperlinks[index];
            if (hyperlink != null)
            {
                string updated = UpdateStructuralReference(hyperlink.Reference, kind, 0, columnIndex, count);
                if (StructuralReferenceDeleted(updated))
                {
                    Hyperlinks.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else if (updated != hyperlink.Reference)
                {
                    hyperlink.Reference = updated;
                    result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                }
            }

            index = index - 1;
        }
    }

    internal void UpdateCommentReferencesForRowEdit(XlsxWorksheetRowEditResult result, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int count)
    {
        if (CommentsPart == null || CommentsPart.Comments == null)
        {
            return;
        }

        CommentsPart.RawXml = "";
        int index = CommentsPart.Comments.Count - 1;
        while (index >= 0)
        {
            XlsxComment comment = CommentsPart.Comments[index];
            if (comment != null)
            {
                string updated = UpdateStructuralReference(comment.Reference, kind, rowIndex, 0, count);
                if (StructuralReferenceDeleted(updated))
                {
                    CommentsPart.Comments.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else if (updated != comment.Reference)
                {
                    UpdateCommentReference(comment, updated);
                    result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                }
            }

            index = index - 1;
        }
    }

    internal void UpdateCommentReferencesForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxFormulaReferenceUpdateKind kind, int columnIndex, int count)
    {
        if (CommentsPart == null || CommentsPart.Comments == null)
        {
            return;
        }

        CommentsPart.RawXml = "";
        int index = CommentsPart.Comments.Count - 1;
        while (index >= 0)
        {
            XlsxComment comment = CommentsPart.Comments[index];
            if (comment != null)
            {
                string updated = UpdateStructuralReference(comment.Reference, kind, 0, columnIndex, count);
                if (StructuralReferenceDeleted(updated))
                {
                    CommentsPart.Comments.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else if (updated != comment.Reference)
                {
                    UpdateCommentReference(comment, updated);
                    result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                }
            }

            index = index - 1;
        }
    }

    internal void UpdateLegacyCommentDrawingReferencesForRowEdit(XlsxWorksheetRowEditResult result, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int count)
    {
        if (LegacyCommentDrawingPart == null || LegacyCommentDrawingPart.Drawings == null)
        {
            return;
        }

        LegacyCommentDrawingPart.RawXml = "";
        int index = LegacyCommentDrawingPart.Drawings.Count - 1;
        while (index >= 0)
        {
            XlsxLegacyCommentDrawing drawing = LegacyCommentDrawingPart.Drawings[index];
            if (drawing != null)
            {
                string updated = UpdateStructuralReference(drawing.Reference, kind, rowIndex, 0, count);
                if (StructuralReferenceDeleted(updated))
                {
                    LegacyCommentDrawingPart.Drawings.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else if (updated != drawing.Reference)
                {
                    UpdateLegacyCommentDrawingReference(drawing, updated);
                    result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                }
            }

            index = index - 1;
        }
    }

    internal void UpdateLegacyCommentDrawingReferencesForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxFormulaReferenceUpdateKind kind, int columnIndex, int count)
    {
        if (LegacyCommentDrawingPart == null || LegacyCommentDrawingPart.Drawings == null)
        {
            return;
        }

        LegacyCommentDrawingPart.RawXml = "";
        int index = LegacyCommentDrawingPart.Drawings.Count - 1;
        while (index >= 0)
        {
            XlsxLegacyCommentDrawing drawing = LegacyCommentDrawingPart.Drawings[index];
            if (drawing != null)
            {
                string updated = UpdateStructuralReference(drawing.Reference, kind, 0, columnIndex, count);
                if (StructuralReferenceDeleted(updated))
                {
                    LegacyCommentDrawingPart.Drawings.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else if (updated != drawing.Reference)
                {
                    UpdateLegacyCommentDrawingReference(drawing, updated);
                    result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                }
            }

            index = index - 1;
        }
    }

    internal void UpdatePageBreakIdsForRowEdit(XlsxWorksheetRowEditResult result, List<XlsxWorksheetPageBreak> breaks, int rowIndex, int count, bool delete)
    {
        if (breaks == null)
        {
            return;
        }

        int index = breaks.Count - 1;
        while (index >= 0)
        {
            XlsxWorksheetPageBreak pageBreak = breaks[index];
            if (pageBreak != null && pageBreak.HasId)
            {
                int updated = UpdateStructuralIndex((int)pageBreak.Id, rowIndex, count, delete);
                if (updated == 0)
                {
                    breaks.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else if (updated != pageBreak.Id)
                {
                    pageBreak.Id = (uint)updated;
                    result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                }
            }

            index = index - 1;
        }
    }

    internal void UpdatePageBreakIdsForColumnEdit(XlsxWorksheetColumnEditResult result, List<XlsxWorksheetPageBreak> breaks, int columnIndex, int count, bool delete)
    {
        if (breaks == null)
        {
            return;
        }

        int index = breaks.Count - 1;
        while (index >= 0)
        {
            XlsxWorksheetPageBreak pageBreak = breaks[index];
            if (pageBreak != null && pageBreak.HasId)
            {
                int updated = UpdateStructuralIndex((int)pageBreak.Id, columnIndex, count, delete);
                if (updated == 0)
                {
                    breaks.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else if (updated != pageBreak.Id)
                {
                    pageBreak.Id = (uint)updated;
                    result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                }
            }

            index = index - 1;
        }
    }

    internal void UpdateAutoFilterReferencesForRowEdit(XlsxWorksheetRowEditResult result, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int count)
    {
        if (!HasAutoFilter || AutoFilter == null)
        {
            return;
        }

        bool changed = false;
        string updated = UpdateStructuralReference(AutoFilter.Reference, kind, rowIndex, 0, count);
        if (StructuralReferenceDeleted(updated))
        {
            AutoFilter = new XlsxAutoFilter();
            HasAutoFilter = false;
            result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
            return;
        }

        if (updated != AutoFilter.Reference)
        {
            AutoFilter.Reference = updated;
            result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
            changed = true;
        }

        if (UpdateAutoFilterSortStateReferencesForRowEdit(result, AutoFilter, kind, rowIndex, count))
        {
            changed = true;
        }

        if (changed)
        {
            AutoFilter.RawXml = "";
        }
    }

    internal void UpdateAutoFilterReferencesForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxFormulaReferenceUpdateKind kind, int columnIndex, int count)
    {
        if (!HasAutoFilter || AutoFilter == null)
        {
            return;
        }

        bool changed = false;
        string updated = UpdateStructuralReference(AutoFilter.Reference, kind, 0, columnIndex, count);
        if (StructuralReferenceDeleted(updated))
        {
            AutoFilter = new XlsxAutoFilter();
            HasAutoFilter = false;
            result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
            return;
        }

        if (updated != AutoFilter.Reference)
        {
            AutoFilter.Reference = updated;
            result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
            changed = true;
        }

        if (UpdateAutoFilterSortStateReferencesForColumnEdit(result, AutoFilter, kind, columnIndex, count))
        {
            changed = true;
        }

        if (changed)
        {
            AutoFilter.RawXml = "";
        }
    }

    internal static bool UpdateAutoFilterSortStateReferencesForRowEdit(XlsxWorksheetRowEditResult result, XlsxAutoFilter filter, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int count)
    {
        if (filter == null || !filter.HasSortState || filter.SortState == null)
        {
            return false;
        }

        bool changed = false;
        string updated = UpdateStructuralReference(filter.SortState.Reference, kind, rowIndex, 0, count);
        if (StructuralReferenceDeleted(updated))
        {
            filter.SortState = new XlsxSortState();
            filter.HasSortState = false;
            result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
            return true;
        }

        if (updated != filter.SortState.Reference)
        {
            filter.SortState.Reference = updated;
            result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
            changed = true;
        }

        if (filter.SortState.Conditions == null)
        {
            return changed;
        }

        int index = filter.SortState.Conditions.Count - 1;
        while (index >= 0)
        {
            XlsxSortCondition condition = filter.SortState.Conditions[index];
            if (condition != null)
            {
                updated = UpdateStructuralReference(condition.Reference, kind, rowIndex, 0, count);
                if (StructuralReferenceDeleted(updated))
                {
                    filter.SortState.Conditions.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                    changed = true;
                }
                else if (updated != condition.Reference)
                {
                    condition.Reference = updated;
                    result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                    changed = true;
                }
            }

            index = index - 1;
        }

        return changed;
    }

    internal static bool UpdateAutoFilterSortStateReferencesForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxAutoFilter filter, XlsxFormulaReferenceUpdateKind kind, int columnIndex, int count)
    {
        if (filter == null || !filter.HasSortState || filter.SortState == null)
        {
            return false;
        }

        bool changed = false;
        string updated = UpdateStructuralReference(filter.SortState.Reference, kind, 0, columnIndex, count);
        if (StructuralReferenceDeleted(updated))
        {
            filter.SortState = new XlsxSortState();
            filter.HasSortState = false;
            result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
            return true;
        }

        if (updated != filter.SortState.Reference)
        {
            filter.SortState.Reference = updated;
            result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
            changed = true;
        }

        if (filter.SortState.Conditions == null)
        {
            return changed;
        }

        int index = filter.SortState.Conditions.Count - 1;
        while (index >= 0)
        {
            XlsxSortCondition condition = filter.SortState.Conditions[index];
            if (condition != null)
            {
                updated = UpdateStructuralReference(condition.Reference, kind, 0, columnIndex, count);
                if (StructuralReferenceDeleted(updated))
                {
                    filter.SortState.Conditions.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                    changed = true;
                }
                else if (updated != condition.Reference)
                {
                    condition.Reference = updated;
                    result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                    changed = true;
                }
            }

            index = index - 1;
        }

        return changed;
    }

    internal void UpdateDataValidationReferencesForRowEdit(XlsxWorksheetRowEditResult result, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int count)
    {
        if (!HasDataValidations || DataValidations == null || DataValidations.Items == null)
        {
            return;
        }

        bool changed = false;
        int index = DataValidations.Items.Count - 1;
        while (index >= 0)
        {
            XlsxDataValidation validation = DataValidations.Items[index];
            if (validation != null)
            {
                XlsxStructuralSqrefUpdateResult sqrefResult = UpdateStructuralSqref(validation.Sqref, kind, rowIndex, 0, count);
                string updated = sqrefResult.Reference;
                result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + sqrefResult.UpdatedCount;
                result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + sqrefResult.RemovedCount;
                if (updated.Length == 0)
                {
                    DataValidations.Items.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                    changed = true;
                }
                else
                {
                    if (updated != validation.Sqref)
                    {
                        validation.Sqref = updated;
                        changed = true;
                    }

                    if (UpdateDataValidationFormulaReferencesForRowEdit(result, validation, kind, rowIndex, count))
                    {
                        changed = true;
                    }
                }
            }

            index = index - 1;
        }

        if (changed)
        {
            RefreshDataValidationsAfterStructuralEdit();
        }
    }

    internal void UpdateDataValidationReferencesForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxFormulaReferenceUpdateKind kind, int columnIndex, int count)
    {
        if (!HasDataValidations || DataValidations == null || DataValidations.Items == null)
        {
            return;
        }

        bool changed = false;
        int index = DataValidations.Items.Count - 1;
        while (index >= 0)
        {
            XlsxDataValidation validation = DataValidations.Items[index];
            if (validation != null)
            {
                XlsxStructuralSqrefUpdateResult sqrefResult = UpdateStructuralSqref(validation.Sqref, kind, 0, columnIndex, count);
                string updated = sqrefResult.Reference;
                result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + sqrefResult.UpdatedCount;
                result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + sqrefResult.RemovedCount;
                if (updated.Length == 0)
                {
                    DataValidations.Items.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                    changed = true;
                }
                else
                {
                    if (updated != validation.Sqref)
                    {
                        validation.Sqref = updated;
                        changed = true;
                    }

                    if (UpdateDataValidationFormulaReferencesForColumnEdit(result, validation, kind, columnIndex, count))
                    {
                        changed = true;
                    }
                }
            }

            index = index - 1;
        }

        if (changed)
        {
            RefreshDataValidationsAfterStructuralEdit();
        }
    }

    internal static bool UpdateDataValidationFormulaReferencesForRowEdit(XlsxWorksheetRowEditResult result, XlsxDataValidation validation, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int count)
    {
        bool changed = false;
        XlsxFormulaReferenceUpdateResult update = UpdateFormulaReferences(validation.Formula1, kind, rowIndex, 0, count, 0, 0, 0, 0, 0, 0, "", "");
        AddFormulaUpdateToRowEditResult(result, update);
        if (update.IsValid && update.Formula != validation.Formula1)
        {
            validation.Formula1 = update.Formula;
            validation.Formula1RawXml = "";
            changed = true;
        }

        update = UpdateFormulaReferences(validation.Formula2, kind, rowIndex, 0, count, 0, 0, 0, 0, 0, 0, "", "");
        AddFormulaUpdateToRowEditResult(result, update);
        if (update.IsValid && update.Formula != validation.Formula2)
        {
            validation.Formula2 = update.Formula;
            validation.Formula2RawXml = "";
            changed = true;
        }

        return changed;
    }

    internal static bool UpdateDataValidationFormulaReferencesForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxDataValidation validation, XlsxFormulaReferenceUpdateKind kind, int columnIndex, int count)
    {
        bool changed = false;
        XlsxFormulaReferenceUpdateResult update = UpdateFormulaReferences(validation.Formula1, kind, 0, columnIndex, count, 0, 0, 0, 0, 0, 0, "", "");
        AddFormulaUpdateToColumnEditResult(result, update);
        if (update.IsValid && update.Formula != validation.Formula1)
        {
            validation.Formula1 = update.Formula;
            validation.Formula1RawXml = "";
            changed = true;
        }

        update = UpdateFormulaReferences(validation.Formula2, kind, 0, columnIndex, count, 0, 0, 0, 0, 0, 0, "", "");
        AddFormulaUpdateToColumnEditResult(result, update);
        if (update.IsValid && update.Formula != validation.Formula2)
        {
            validation.Formula2 = update.Formula;
            validation.Formula2RawXml = "";
            changed = true;
        }

        return changed;
    }

    internal void RefreshDataValidationsAfterStructuralEdit()
    {
        if (DataValidations == null)
        {
            HasDataValidations = false;
            return;
        }

        if (DataValidations.Items == null || DataValidations.Items.Count == 0)
        {
            HasDataValidations = false;
            DataValidations.Count = 0;
            DataValidations.HasCount = true;
            DataValidations.RawXml = "";
            return;
        }

        HasDataValidations = true;
        DataValidations.Count = (uint)DataValidations.Items.Count;
        DataValidations.HasCount = true;
        DataValidations.RawXml = "";
    }

    internal void UpdateConditionalFormattingReferencesForRowEdit(XlsxWorksheetRowEditResult result, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int count)
    {
        if (ConditionalFormattings == null)
        {
            return;
        }

        int index = ConditionalFormattings.Count - 1;
        while (index >= 0)
        {
            XlsxConditionalFormatting formatting = ConditionalFormattings[index];
            if (formatting != null)
            {
                XlsxStructuralSqrefUpdateResult sqrefResult = UpdateStructuralSqref(formatting.Sqref, kind, rowIndex, 0, count);
                string updated = sqrefResult.Reference;
                result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + sqrefResult.UpdatedCount;
                result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + sqrefResult.RemovedCount;
                if (updated.Length == 0)
                {
                    ConditionalFormattings.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else
                {
                    if (updated != formatting.Sqref)
                    {
                        formatting.Sqref = updated;
                        formatting.RawXml = "";
                    }

                    if (UpdateConditionalFormattingFormulaReferencesForRowEdit(result, formatting, kind, rowIndex, count))
                    {
                        formatting.RawXml = "";
                    }
                }
            }

            index = index - 1;
        }
    }

    internal void UpdateConditionalFormattingReferencesForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxFormulaReferenceUpdateKind kind, int columnIndex, int count)
    {
        if (ConditionalFormattings == null)
        {
            return;
        }

        int index = ConditionalFormattings.Count - 1;
        while (index >= 0)
        {
            XlsxConditionalFormatting formatting = ConditionalFormattings[index];
            if (formatting != null)
            {
                XlsxStructuralSqrefUpdateResult sqrefResult = UpdateStructuralSqref(formatting.Sqref, kind, 0, columnIndex, count);
                string updated = sqrefResult.Reference;
                result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + sqrefResult.UpdatedCount;
                result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + sqrefResult.RemovedCount;
                if (updated.Length == 0)
                {
                    ConditionalFormattings.RemoveAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else
                {
                    if (updated != formatting.Sqref)
                    {
                        formatting.Sqref = updated;
                        formatting.RawXml = "";
                    }

                    if (UpdateConditionalFormattingFormulaReferencesForColumnEdit(result, formatting, kind, columnIndex, count))
                    {
                        formatting.RawXml = "";
                    }
                }
            }

            index = index - 1;
        }
    }

    internal static bool UpdateConditionalFormattingFormulaReferencesForRowEdit(XlsxWorksheetRowEditResult result, XlsxConditionalFormatting formatting, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int count)
    {
        if (formatting.Rules == null)
        {
            return false;
        }

        bool changed = false;
        foreach (XlsxConditionalFormatRule rule in formatting.Rules)
        {
            if (rule == null || rule.Formulas == null)
            {
                continue;
            }

            int index = 0;
            while (index < rule.Formulas.Count)
            {
                string formula = rule.Formulas[index];
                XlsxFormulaReferenceUpdateResult update = UpdateFormulaReferences(formula, kind, rowIndex, 0, count, 0, 0, 0, 0, 0, 0, "", "");
                AddFormulaUpdateToRowEditResult(result, update);
                if (update.IsValid && update.Formula != formula)
                {
                    rule.Formulas[index] = update.Formula;
                    if (rule.FormulaRawXml != null)
                    {
                        rule.FormulaRawXml.Clear();
                    }

                    changed = true;
                }

                index = index + 1;
            }
        }

        return changed;
    }

    internal static bool UpdateConditionalFormattingFormulaReferencesForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxConditionalFormatting formatting, XlsxFormulaReferenceUpdateKind kind, int columnIndex, int count)
    {
        if (formatting.Rules == null)
        {
            return false;
        }

        bool changed = false;
        foreach (XlsxConditionalFormatRule rule in formatting.Rules)
        {
            if (rule == null || rule.Formulas == null)
            {
                continue;
            }

            int index = 0;
            while (index < rule.Formulas.Count)
            {
                string formula = rule.Formulas[index];
                XlsxFormulaReferenceUpdateResult update = UpdateFormulaReferences(formula, kind, 0, columnIndex, count, 0, 0, 0, 0, 0, 0, "", "");
                AddFormulaUpdateToColumnEditResult(result, update);
                if (update.IsValid && update.Formula != formula)
                {
                    rule.Formulas[index] = update.Formula;
                    if (rule.FormulaRawXml != null)
                    {
                        rule.FormulaRawXml.Clear();
                    }

                    changed = true;
                }

                index = index + 1;
            }
        }

        return changed;
    }

    internal void UpdateTableReferencesForRowEdit(XlsxWorksheetRowEditResult result, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int count)
    {
        if (Tables == null)
        {
            return;
        }

        int index = Tables.Count - 1;
        while (index >= 0)
        {
            XlsxTablePart table = Tables[index];
            if (table != null)
            {
                string updated = UpdateStructuralReference(table.Reference, kind, rowIndex, 0, count);
                if (StructuralReferenceDeleted(updated))
                {
                    Tables.RemoveAt(index);
                    RemoveTableReferenceAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else
                {
                    if (updated != table.Reference)
                    {
                        table.RawXml = "";
                        table.Reference = updated;
                        result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                    }

                    UpdateTableFormulaReferencesForRowEdit(result, table, kind, rowIndex, count);
                    RefreshTableAutoFilterAndSortRawXml(table);
                }
            }

            index = index - 1;
        }
    }

    internal void UpdateTableReferencesForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxFormulaReferenceUpdateKind kind, int columnIndex, int count)
    {
        if (Tables == null)
        {
            return;
        }

        int index = Tables.Count - 1;
        while (index >= 0)
        {
            XlsxTablePart table = Tables[index];
            if (table != null)
            {
                string updated = UpdateStructuralReference(table.Reference, kind, 0, columnIndex, count);
                if (StructuralReferenceDeleted(updated))
                {
                    Tables.RemoveAt(index);
                    RemoveTableReferenceAt(index);
                    result.RemovedStructuralReferenceCount = result.RemovedStructuralReferenceCount + 1;
                }
                else
                {
                    if (updated != table.Reference)
                    {
                        table.RawXml = "";
                        table.Reference = updated;
                        result.StructuralReferenceUpdateCount = result.StructuralReferenceUpdateCount + 1;
                    }

                    UpdateTableFormulaReferencesForColumnEdit(result, table, kind, columnIndex, count);
                    RefreshTableAutoFilterAndSortRawXml(table);
                }
            }

            index = index - 1;
        }
    }

    internal void RemoveTableReferenceAt(int index)
    {
        if (TableReferences != null && index >= 0 && index < TableReferences.Count)
        {
            TableReferences.RemoveAt(index);
        }
    }

    internal static void UpdateTableFormulaReferencesForRowEdit(XlsxWorksheetRowEditResult result, XlsxTablePart table, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int count)
    {
        if (table == null || table.Columns == null)
        {
            return;
        }

        foreach (XlsxTableColumn column in table.Columns)
        {
            UpdateTableColumnFormulaForRowEdit(result, table, column, true, kind, rowIndex, count);
            UpdateTableColumnFormulaForRowEdit(result, table, column, false, kind, rowIndex, count);
        }
    }

    internal static void UpdateTableFormulaReferencesForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxTablePart table, XlsxFormulaReferenceUpdateKind kind, int columnIndex, int count)
    {
        if (table == null || table.Columns == null)
        {
            return;
        }

        foreach (XlsxTableColumn column in table.Columns)
        {
            UpdateTableColumnFormulaForColumnEdit(result, table, column, true, kind, columnIndex, count);
            UpdateTableColumnFormulaForColumnEdit(result, table, column, false, kind, columnIndex, count);
        }
    }

    internal static void UpdateTableColumnFormulaForRowEdit(XlsxWorksheetRowEditResult result, XlsxTablePart table, XlsxTableColumn column, bool calculated, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int count)
    {
        if (column == null)
        {
            return;
        }

        string formula = calculated ? column.CalculatedColumnFormula : column.TotalsRowFormula;
        if (formula == null || formula.Length == 0)
        {
            return;
        }

        XlsxFormulaReferenceUpdateResult update = UpdateFormulaReferences(formula, kind, rowIndex, 0, count, 0, 0, 0, 0, 0, 0, "", "");
        AddFormulaUpdateToRowEditResult(result, update);
        if (update.IsValid && update.Formula != formula)
        {
            table.RawXml = "";
            if (calculated)
            {
                column.CalculatedColumnFormula = update.Formula;
                column.CalculatedColumnFormulaRawXml = "";
            }
            else
            {
                column.TotalsRowFormula = update.Formula;
                column.TotalsRowFormulaRawXml = "";
            }
        }
    }

    internal static void UpdateTableColumnFormulaForColumnEdit(XlsxWorksheetColumnEditResult result, XlsxTablePart table, XlsxTableColumn column, bool calculated, XlsxFormulaReferenceUpdateKind kind, int columnIndex, int count)
    {
        if (column == null)
        {
            return;
        }

        string formula = calculated ? column.CalculatedColumnFormula : column.TotalsRowFormula;
        if (formula == null || formula.Length == 0)
        {
            return;
        }

        XlsxFormulaReferenceUpdateResult update = UpdateFormulaReferences(formula, kind, 0, columnIndex, count, 0, 0, 0, 0, 0, 0, "", "");
        AddFormulaUpdateToColumnEditResult(result, update);
        if (update.IsValid && update.Formula != formula)
        {
            table.RawXml = "";
            if (calculated)
            {
                column.CalculatedColumnFormula = update.Formula;
                column.CalculatedColumnFormulaRawXml = "";
            }
            else
            {
                column.TotalsRowFormula = update.Formula;
                column.TotalsRowFormulaRawXml = "";
            }
        }
    }

    internal static void UpdateCommentReference(XlsxComment comment, string reference)
    {
        comment.Reference = reference;
        XlsxA1CellReferenceResult cellReference = XlsxA1CellReferenceConverter.Parse(reference);
        if (cellReference.IsValid)
        {
            comment.Row = cellReference.Row - 1;
            comment.HasRow = true;
            comment.Column = cellReference.Column - 1;
            comment.HasColumn = true;
        }
    }

    internal static void UpdateLegacyCommentDrawingReference(XlsxLegacyCommentDrawing drawing, string reference)
    {
        drawing.Reference = reference;
        drawing.RawXml = "";
        XlsxA1CellReferenceResult cellReference = XlsxA1CellReferenceConverter.Parse(reference);
        if (cellReference.IsValid)
        {
            drawing.Row = cellReference.Row - 1;
            drawing.HasRow = true;
            drawing.Column = cellReference.Column - 1;
            drawing.HasColumn = true;
            drawing.Anchor = BuildDefaultCommentAnchor(cellReference.Row, cellReference.Column);
        }
    }

    internal static bool ApplyUpdatedMergeReference(XlsxWorksheetMergeCell mergeCell, string reference)
    {
        XlsxWorksheetMergeCellsResult range = ParseMergeReference(reference);
        if (!range.IsValid)
        {
            return false;
        }

        ApplyMergeRangeToCell(mergeCell, range);
        return true;
    }

    internal static string UpdateStructuralReference(string? reference, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int columnIndex, int count)
    {
        XlsxFormulaReferenceUpdateResult update = UpdateFormulaReferences(reference, kind, rowIndex, columnIndex, count, 0, 0, 0, 0, 0, 0, "", "");
        if (!update.IsValid)
        {
            return XlsxCellValueConverter.NormalizeText(reference);
        }

        return update.Formula;
    }

    internal static XlsxStructuralSqrefUpdateResult UpdateStructuralSqref(string sqref, XlsxFormulaReferenceUpdateKind kind, int rowIndex, int columnIndex, int count)
    {
        XlsxStructuralSqrefUpdateResult result = new XlsxStructuralSqrefUpdateResult();
        if (sqref == null || sqref.Length == 0)
        {
            return result;
        }

        StringBuilder builder = new StringBuilder();
        string[] references = sqref.Split(' ');
        foreach (string reference in references)
        {
            string trimmed = reference.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            string updated = UpdateStructuralReference(trimmed, kind, rowIndex, columnIndex, count);
            if (StructuralReferenceDeleted(updated))
            {
                result.RemovedCount = result.RemovedCount + 1;
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(updated);
            if (updated != trimmed)
            {
                result.UpdatedCount = result.UpdatedCount + 1;
            }
        }

        result.Reference = builder.ToString();
        return result;
    }

    internal static bool StructuralReferenceDeleted(string reference)
    {
        return reference == "#REF!" || (reference != null && reference.IndexOf("#REF!", StringComparison.Ordinal) >= 0);
    }

    internal static int UpdateStructuralIndex(int value, int startIndex, int count, bool delete)
    {
        if (!delete)
        {
            if (value >= startIndex)
            {
                return value + count;
            }

            return value;
        }

        int lastIndex = startIndex + count - 1;
        if (value >= startIndex && value <= lastIndex)
        {
            return 0;
        }

        if (value > lastIndex)
        {
            return value - count;
        }

        return value;
    }

    internal static void AddFormulaUpdateToRowEditResult(XlsxWorksheetRowEditResult result, XlsxFormulaReferenceUpdateResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        result.FormulaReferenceUpdateCount = result.FormulaReferenceUpdateCount + source.UpdatedReferenceCount;
        result.DeletedFormulaReferenceCount = result.DeletedFormulaReferenceCount + source.DeletedReferenceCount;
    }

    internal static void AddFormulaUpdateToColumnEditResult(XlsxWorksheetColumnEditResult result, XlsxFormulaReferenceUpdateResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        result.FormulaReferenceUpdateCount = result.FormulaReferenceUpdateCount + source.UpdatedReferenceCount;
        result.DeletedFormulaReferenceCount = result.DeletedFormulaReferenceCount + source.DeletedReferenceCount;
    }

    internal void MoveRowForRowEdit(XlsxWorksheetRowEditResult result, XlsxWorksheetRow row, int newRowIndex)
    {
        row.RowIndex = (uint)newRowIndex;
        if (row.Cells == null)
        {
            return;
        }

        foreach (XlsxWorksheetCell cell in row.Cells)
        {
            if (cell != null && cell.Reference != null && cell.Reference.Length > 0)
            {
                XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(cell.Reference);
                if (reference.IsValid)
                {
                    cell.Reference = XlsxA1CellReferenceConverter.FormatString(newRowIndex, reference.Column, reference.IsAbsoluteRow, reference.IsAbsoluteColumn);
                    result.UpdatedCellReferenceCount = result.UpdatedCellReferenceCount + 1;
                }
            }
        }
    }

    internal void SortRowsByEffectiveIndex()
    {
        if (Rows == null)
        {
            return;
        }

        int outer = 1;
        while (outer < Rows.Count)
        {
            XlsxWorksheetRow current = Rows[outer];
            int currentRowIndex = GetEffectiveRowIndex(current, outer + 1);
            int inner = outer - 1;
            while (inner >= 0 && GetEffectiveRowIndex(Rows[inner], inner + 1) > currentRowIndex)
            {
                Rows[inner + 1] = Rows[inner];
                inner = inner - 1;
            }

            Rows[inner + 1] = current;
            outer = outer + 1;
        }
    }

    internal bool CellMatchesAddress(XlsxWorksheetCell cell, XlsxWorksheetRow row, int rowSequence, int cellSequence, int rowIndex, int columnIndex)
    {
        if (cell.Reference != null && cell.Reference.Length > 0)
        {
            XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(cell.Reference);
            if (referenceResult.IsValid)
            {
                return referenceResult.Row == rowIndex && referenceResult.Column == columnIndex;
            }
        }

        if (cell.Reference == null || cell.Reference.Length == 0)
        {
            return GetEffectiveRowIndex(row, rowSequence) == rowIndex && cellSequence == columnIndex;
        }

        return false;
    }

    internal int GetEffectiveRowIndex(XlsxWorksheetRow row, int sequence)
    {
        if (row != null && row.RowIndex > 0)
        {
            return (int)row.RowIndex;
        }

        return sequence;
    }

    internal int GetEffectiveCellColumn(XlsxWorksheetCell? cell, int sequence)
    {
        if (cell != null && cell.Reference != null && cell.Reference.Length > 0)
        {
            XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(cell.Reference);
            if (referenceResult.IsValid)
            {
                return referenceResult.Column;
            }
        }

        return sequence;
    }

    internal static bool IsUsedCell(XlsxWorksheetCell cell)
    {
        if (cell == null)
        {
            return false;
        }

        if (cell.HasStyle)
        {
            return true;
        }

        if (cell.Type != null && cell.Type.Length > 0)
        {
            return true;
        }

        if (cell.Value != null && cell.Value.Length > 0)
        {
            return true;
        }

        if (cell.Formula != null && cell.Formula.Length > 0)
        {
            return true;
        }

        if (cell.HasFormula)
        {
            return true;
        }

        if (cell.InlineStringText != null && cell.InlineStringText.Length > 0)
        {
            return true;
        }

        if (cell.HasInlineString)
        {
            return true;
        }

        if (cell.UnknownAttributes != null && cell.UnknownAttributes.Count > 0)
        {
            return true;
        }

        if (cell.UnknownXml != null && cell.UnknownXml.Count > 0)
        {
            return true;
        }

        return false;
    }

    internal static bool IsValidCellCoordinate(int rowIndex, int columnIndex)
    {
        return IsValidCellRow(rowIndex) && IsValidCellColumn(columnIndex);
    }

    internal static bool IsValidCellRow(int rowIndex)
    {
        return rowIndex > 0 && rowIndex <= XlsxXmlValueParser.MaxSpreadsheetRows;
    }

    internal static bool IsValidCellColumn(int columnIndex)
    {
        return columnIndex > 0 && columnIndex <= XlsxXmlValueParser.MaxSpreadsheetColumns;
    }

    internal static XlsxWorksheetMergeCellsResult ParseMergeReference(string? reference)
    {
        return ParseMergeRange(reference, true);
    }

    internal static XlsxWorksheetMergeCellsResult ParseMergeSelection(string reference)
    {
        return ParseMergeRange(reference, false);
    }

    internal static XlsxWorksheetMergeCellsResult ParseMergeRange(string? reference, bool requireMultiCell)
    {
        XlsxWorksheetMergeCellsResult result = new XlsxWorksheetMergeCellsResult();
        XlsxA1RangeReferenceResult rangeResult = XlsxA1RangeReferenceConverter.Parse(reference);
        if (!rangeResult.IsValid)
        {
            result.AddError("worksheet.mergeCellReferenceInvalid", "Merged cell range reference is invalid.");
            AddRangeDiagnostics(result, rangeResult);
            return result;
        }

        int firstRow = rangeResult.First.Row;
        int firstColumn = rangeResult.First.Column;
        int lastRow = rangeResult.Last.Row;
        int lastColumn = rangeResult.Last.Column;
        if (firstRow > lastRow)
        {
            int temporary = firstRow;
            firstRow = lastRow;
            lastRow = temporary;
        }

        if (firstColumn > lastColumn)
        {
            int temporary = firstColumn;
            firstColumn = lastColumn;
            lastColumn = temporary;
        }

        if (requireMultiCell && firstRow == lastRow && firstColumn == lastColumn)
        {
            result.AddError("worksheet.mergeCellSingleCell", "Merged cell range must include at least two cells.");
            return result;
        }

        result.SetRange(firstRow, firstColumn, lastRow, lastColumn);
        return result;
    }

    internal static void AddRangeDiagnostics(XlsxWorksheetMergeCellsResult result, XlsxA1RangeReferenceResult rangeResult)
    {
        foreach (XlsxDiagnostic diagnostic in rangeResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddMergeDiagnostics(XlsxWorksheetMergeCellsResult target, XlsxWorksheetMergeCellsResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    internal void ValidateMergeDoesNotOverlap(XlsxWorksheetMergeCellsResult result, XlsxWorksheetMergeCellsResult range, int currentIndex)
    {
        if (FindOverlappingMerge(range, currentIndex) != null)
        {
            result.AddError("worksheet.mergeCellOverlap", "Merged cell range overlaps another merged cell range.");
        }
    }

    internal XlsxWorksheetMergeCell? FindOverlappingMerge(XlsxWorksheetMergeCellsResult range, int skipIndex)
    {
        if (MergedCells == null)
        {
            return null;
        }

        int index = 0;
        while (index < MergedCells.Count)
        {
            XlsxWorksheetMergeCell mergeCell = MergedCells[index];
            if (index != skipIndex && mergeCell != null)
            {
                XlsxWorksheetMergeCellsResult existing = ParseMergeReference(mergeCell.Reference);
                if (existing.IsValid && RangesOverlap(range, existing))
                {
                    return mergeCell;
                }
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool RangesOverlap(XlsxWorksheetMergeCellsResult left, XlsxWorksheetMergeCellsResult right)
    {
        if (left.LastRow < right.FirstRow || right.LastRow < left.FirstRow)
        {
            return false;
        }

        if (left.LastColumn < right.FirstColumn || right.LastColumn < left.FirstColumn)
        {
            return false;
        }

        return true;
    }

    internal static void ApplyMergeRangeToCell(XlsxWorksheetMergeCell mergeCell, XlsxWorksheetMergeCellsResult range)
    {
        mergeCell.Reference = range.Reference;
        mergeCell.FirstRow = range.FirstRow;
        mergeCell.FirstColumn = range.FirstColumn;
        mergeCell.LastRow = range.LastRow;
        mergeCell.LastColumn = range.LastColumn;
        mergeCell.HasRange = true;
    }

    internal static string FormatMergeReference(int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        XlsxA1RangeReferenceValue range = new XlsxA1RangeReferenceValue();
        range.First.Row = firstRow;
        range.First.Column = firstColumn;
        range.Last.Row = lastRow;
        range.Last.Column = lastColumn;
        range.HasLast = true;
        return XlsxA1RangeReferenceConverter.FormatString(range);
    }

    internal static void ResolveHyperlinkRelationship(XlsxWorksheetHyperlinkResult result, XlsxWorksheetHyperlink hyperlink, XlsxRelationshipsPart relationships)
    {
        if (relationships == null)
        {
            result.AddError("worksheet.hyperlinkRelationshipsMissing", "Worksheet hyperlink relationship target cannot be resolved because the relationships part is missing.");
            return;
        }

        XlsxRelationship? relationship = relationships.GetRelationship(hyperlink.RelationshipId);
        if (relationship == null)
        {
            XlsxDiagnostic diagnostic = result.AddError("worksheet.hyperlinkRelationshipMissing", "Worksheet hyperlink relationship target is missing.");
            diagnostic.SetRelationshipId(hyperlink.RelationshipId);
            return;
        }

        if (relationship.Type != XlsxRelationshipTypes.Hyperlink)
        {
            XlsxDiagnostic diagnostic = result.AddError("worksheet.hyperlinkRelationshipType", "Worksheet hyperlink relationship must use the hyperlink relationship type.");
            diagnostic.SetRelationshipId(hyperlink.RelationshipId);
            return;
        }

        ApplyHyperlinkRelationship(hyperlink, relationship);
    }

    /// <summary>
    /// Performs the resolve drawing relationship operation using the current model state.
    /// </summary>
    /// <param name = "result">The result value used by the operation.</param>
    /// <param name = "drawing">The drawing value used by the operation.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <param name = "ownerName">The owner name value used by the operation.</param>
    internal static void ResolveDrawingRelationship(XlsxSheetDrawingRelationshipResult result, XlsxSheetDrawingReference drawing, XlsxRelationshipsPart relationships, string ownerName)
    {
        if (relationships == null)
        {
            result.AddError(ownerName + ".drawingRelationshipsMissing", "Drawing relationship target cannot be resolved because the relationships part is missing.");
            return;
        }

        if (drawing.RelationshipId == null || drawing.RelationshipId.Length == 0)
        {
            result.AddError(ownerName + ".drawingRelationshipIdMissing", "Drawing reference must have an r:id attribute.");
            return;
        }

        XlsxRelationship? relationship = relationships.GetRelationship(drawing.RelationshipId);
        if (relationship == null)
        {
            XlsxDiagnostic diagnostic = result.AddError(ownerName + ".drawingRelationshipMissing", "Drawing relationship target is missing.");
            diagnostic.SetRelationshipId(drawing.RelationshipId);
            return;
        }

        if (relationship.Type != XlsxRelationshipTypes.Drawing)
        {
            XlsxDiagnostic diagnostic = result.AddError(ownerName + ".drawingRelationshipType", "Drawing relationship must use the drawing relationship type.");
            diagnostic.SetRelationshipId(drawing.RelationshipId);
            return;
        }

        ApplyDrawingRelationship(drawing, relationship);
        result.Relationship = relationship;
        result.HasRelationship = true;
    }

    internal static void ResolvePivotTableRelationship(XlsxWorksheetPivotTableRelationshipResult result, XlsxWorksheetPivotTableReference pivotTable, XlsxRelationshipsPart relationships, int index)
    {
        if (pivotTable.RelationshipId == null || pivotTable.RelationshipId.Length == 0)
        {
            XlsxDiagnostic diagnostic = result.AddError("worksheet.pivotTableRelationshipIdMissing", "Worksheet pivotTableDefinition must have an r:id attribute.");
            diagnostic.SetLocation("", "pivotTableDefinition", index.ToString(CultureInfo.InvariantCulture));
            return;
        }

        XlsxRelationship? relationship = relationships.GetRelationship(pivotTable.RelationshipId);
        if (relationship == null)
        {
            XlsxDiagnostic diagnostic = result.AddError("worksheet.pivotTableRelationshipMissing", "Worksheet pivotTableDefinition references a missing relationship.");
            diagnostic.SetLocation("", "pivotTableDefinition", "r:id");
            diagnostic.SetRelationshipId(pivotTable.RelationshipId);
            return;
        }

        if (relationship.Type != XlsxRelationshipTypes.PivotTable)
        {
            XlsxDiagnostic diagnostic = result.AddError("worksheet.pivotTableRelationshipType", "Worksheet pivotTableDefinition relationship must target a pivot table definition part.");
            diagnostic.SetLocation(relationships.PartPath, "Relationship", "Type");
            diagnostic.SetRelationshipId(pivotTable.RelationshipId);
            return;
        }

        ApplyPivotTableRelationship(pivotTable, relationship);
        result.PivotTables.Add(pivotTable);
    }

    internal static void ApplyDrawingRelationship(XlsxSheetDrawingReference drawing, XlsxRelationship relationship)
    {
        drawing.Relationship = relationship;
        drawing.HasRelationship = true;
        drawing.Target = relationship.Target;
        drawing.TargetPath = relationship.TargetPath;
    }

    internal static void ApplyPivotTableRelationship(XlsxWorksheetPivotTableReference pivotTable, XlsxRelationship relationship)
    {
        pivotTable.Relationship = relationship;
        pivotTable.HasRelationship = true;
        pivotTable.Target = relationship.Target;
        pivotTable.TargetPath = relationship.TargetPath;
    }

    internal static void ApplyHyperlinkRelationship(XlsxWorksheetHyperlink hyperlink, XlsxRelationship relationship)
    {
        hyperlink.Target = relationship.Target;
        hyperlink.TargetMode = relationship.TargetMode;
        hyperlink.IsExternal = relationship.IsExternal || relationship.TargetMode == "External";
        hyperlink.TargetPath = relationship.TargetPath;
        hyperlink.TargetFragment = relationship.TargetFragment;
        hyperlink.ExternalTarget = relationship.ExternalTarget;
        hyperlink.HasRelationship = true;
    }

    internal static XlsxRelationship? CreateHyperlinkRelationship(XlsxWorksheetHyperlinkResult result, XlsxWorksheetHyperlink hyperlink)
    {
        string target = GetHyperlinkRelationshipTarget(hyperlink);
        if (target.Length == 0)
        {
            XlsxDiagnostic diagnostic = result.AddError("worksheet.hyperlinkRelationshipTargetMissing", "Relationship-backed worksheet hyperlink must have a target.");
            diagnostic.SetRelationshipId(hyperlink.RelationshipId);
            return null;
        }

        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = hyperlink.RelationshipId;
        relationship.Type = XlsxRelationshipTypes.Hyperlink;
        relationship.Target = target;
        if (IsExternalHyperlinkTarget(hyperlink, target))
        {
            relationship.TargetMode = "External";
            relationship.IsExternal = true;
            relationship.ExternalTarget = target;
        }
        else
        {
            relationship.TargetMode = "Internal";
            relationship.IsExternal = false;
            relationship.TargetPath = target;
        }

        return relationship;
    }

    internal static string GetHyperlinkRelationshipTarget(XlsxWorksheetHyperlink hyperlink)
    {
        if (hyperlink.Target != null && hyperlink.Target.Length > 0)
        {
            return hyperlink.Target;
        }

        if (hyperlink.ExternalTarget != null && hyperlink.ExternalTarget.Length > 0)
        {
            return hyperlink.ExternalTarget;
        }

        if (hyperlink.TargetPath != null && hyperlink.TargetPath.Length > 0)
        {
            return hyperlink.TargetPath;
        }

        return "";
    }

    internal static bool IsExternalHyperlinkTarget(XlsxWorksheetHyperlink hyperlink, string target)
    {
        if (hyperlink.IsExternal || hyperlink.TargetMode == "External")
        {
            return true;
        }

        return XlsxPackagePath.LooksLikeExternalUri(target);
    }

    internal static void AddRelationshipDiagnostics(XlsxWorksheetHyperlinkResult result, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal XlsxWorksheetPaneResult ValidateFreezePaneCounts(int frozenRows, int frozenColumns)
    {
        XlsxWorksheetPaneResult result = new XlsxWorksheetPaneResult();
        if (SheetViews == null)
        {
            result.AddError("worksheetPane.sheetViewsNull", "Worksheet sheet views list must not be null.");
            return result;
        }

        if (frozenRows == 0 && frozenColumns == 0)
        {
            result.AddError("worksheetPane.freezeEmpty", "Freeze panes requires at least one frozen row or column.");
        }

        if (frozenRows < 0 || frozenRows >= XlsxXmlValueParser.MaxSpreadsheetRows)
        {
            result.AddError("worksheetPane.frozenRowsRange", "Frozen row count must be within worksheet limits.");
        }

        if (frozenColumns < 0 || frozenColumns >= XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            result.AddError("worksheetPane.frozenColumnsRange", "Frozen column count must be within worksheet limits.");
        }

        return result;
    }

    internal XlsxWorksheetPaneResult ValidateSplitPaneValues(double xSplit, double ySplit, string topLeftCell)
    {
        XlsxWorksheetPaneResult result = new XlsxWorksheetPaneResult();
        if (SheetViews == null)
        {
            result.AddError("worksheetPane.sheetViewsNull", "Worksheet sheet views list must not be null.");
            return result;
        }

        if (double.IsNaN(xSplit) || double.IsInfinity(xSplit) || double.IsNaN(ySplit) || double.IsInfinity(ySplit) || xSplit < 0 || ySplit < 0)
        {
            result.AddError("worksheetPane.splitRange", "Split pane offsets must be finite non-negative values.");
        }

        if (xSplit == 0 && ySplit == 0)
        {
            result.AddError("worksheetPane.splitEmpty", "Split panes requires at least one split offset.");
        }

        string normalizedTopLeftCell = NormalizePaneTopLeftCell(topLeftCell);
        if (normalizedTopLeftCell.Length > 0)
        {
            XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(normalizedTopLeftCell);
            if (!reference.IsValid)
            {
                result.AddError("worksheetPane.topLeftCellInvalid", "Split panes top-left cell must be a valid A1 cell reference.");
                XlsxWorksheetPaneResult.AddReferenceDiagnostics(result, reference);
            }
        }

        return result;
    }

    internal XlsxWorksheetView? GetOrCreatePrimarySheetView(XlsxWorksheetPaneResult result)
    {
        if (SheetViews == null)
        {
            result.AddError("worksheetPane.sheetViewsNull", "Worksheet sheet views list must not be null.");
            return null;
        }

        if (SheetViews.Count == 0)
        {
            XlsxWorksheetView view = new XlsxWorksheetView();
            view.WorkbookViewId = 0;
            view.HasWorkbookViewId = true;
            SheetViews.Add(view);
            result.CreatedView = true;
            return view;
        }

        XlsxWorksheetView existing = SheetViews[0];
        if (existing == null)
        {
            result.AddError("worksheetPane.sheetViewNull", "Worksheet primary sheet view must not be null.");
            return null;
        }

        if (existing.Selections == null)
        {
            existing.Selections = new List<XlsxWorksheetSelection>();
        }

        if (existing.Pane == null)
        {
            existing.Pane = new XlsxWorksheetPane();
        }

        return existing;
    }

    internal static void ApplyPaneToView(XlsxWorksheetView? view, XlsxWorksheetPane pane, string activePane, string activeCell)
    {
        if (view == null)
        {
            return;
        }

        view.Pane = pane;
        view.HasPane = true;
        if (view.Selections == null)
        {
            view.Selections = new List<XlsxWorksheetSelection>();
        }
        else
        {
            view.Selections.Clear();
        }

        XlsxWorksheetSelection selection = new XlsxWorksheetSelection();
        selection.Pane = activePane;
        selection.ActiveCell = activeCell;
        selection.Sqref = activeCell;
        view.Selections.Add(selection);
    }

    internal static string GetPaneNameForSplit(bool hasRows, bool hasColumns)
    {
        if (hasRows && hasColumns)
        {
            return "bottomRight";
        }

        if (hasRows)
        {
            return "bottomLeft";
        }

        if (hasColumns)
        {
            return "topRight";
        }

        return "topLeft";
    }

    internal static string NormalizePaneTopLeftCell(string text)
    {
        if (text == null || text.Length == 0)
        {
            return "A1";
        }

        return text.Trim();
    }

    internal XlsxWorksheetCommentResult ValidateNewComment(string cellAddress, string author, string text)
    {
        XlsxWorksheetCommentResult result = new XlsxWorksheetCommentResult();
        if (Rows == null)
        {
            result.AddError("worksheet.rowsNull", "Worksheet rows list must not be null.");
        }

        XlsxA1CellReferenceResult referenceResult = XlsxA1CellReferenceConverter.Parse(cellAddress);
        if (!referenceResult.IsValid)
        {
            result.AddError("worksheet.commentReferenceInvalid", "Worksheet comment reference must be a valid A1 cell reference.");
            XlsxWorksheetCommentResult.AddReferenceDiagnostics(result, referenceResult);
        }
        else
        {
            result.Reference = referenceResult.Text;
            result.Row = referenceResult.Row;
            result.Column = referenceResult.Column;
        }

        if (NormalizeCommentAuthor(author).Length == 0)
        {
            result.AddError("worksheet.commentAuthorMissing", "Worksheet comment author must not be empty.");
        }

        if (text == null)
        {
            result.AddError("worksheet.commentTextMissing", "Worksheet comment text must not be null.");
        }

        return result;
    }

    internal void EnsureCommentParts()
    {
        if (CommentsPart == null)
        {
            CommentsPart = new XlsxCommentsPart();
        }

        HasCommentsPart = true;
        if (LegacyCommentDrawingPart == null)
        {
            LegacyCommentDrawingPart = new XlsxLegacyCommentDrawingPart();
        }

        HasLegacyCommentDrawingPart = true;
    }

    internal uint GetOrAddCommentAuthor(string author, XlsxWorksheetCommentResult result)
    {
        uint existing = FindCommentAuthorId(author);
        if (existing != uint.MaxValue)
        {
            return existing;
        }

        CommentsPart.Authors.Add(author);
        result.CreatedAuthor = true;
        return (uint)(CommentsPart.Authors.Count - 1);
    }

    internal uint FindCommentAuthorId(string author)
    {
        if (CommentsPart == null || CommentsPart.Authors == null)
        {
            return uint.MaxValue;
        }

        int index = 0;
        while (index < CommentsPart.Authors.Count)
        {
            if (CommentsPart.Authors[index] == author)
            {
                return (uint)index;
            }

            index = index + 1;
        }

        return uint.MaxValue;
    }

    internal uint GetOrCreateCommentShapeId(string reference)
    {
        XlsxComment? comment = null;
        if (CommentsPart != null && CommentsPart.Comments != null)
        {
            comment = CommentsPart.GetComment(reference);
        }

        if (comment != null && comment.HasShapeId)
        {
            return comment.ShapeId;
        }

        return GetNextCommentShapeId();
    }

    internal uint GetNextCommentShapeId()
    {
        uint shapeId = 1025;
        if (CommentsPart != null && CommentsPart.Comments != null)
        {
            foreach (XlsxComment comment in CommentsPart.Comments)
            {
                if (comment != null && comment.HasShapeId && comment.ShapeId >= shapeId)
                {
                    shapeId = comment.ShapeId + 1;
                }
            }
        }

        if (LegacyCommentDrawingPart != null && LegacyCommentDrawingPart.Drawings != null)
        {
            foreach (XlsxLegacyCommentDrawing drawing in LegacyCommentDrawingPart.Drawings)
            {
                uint parsed = ParseLegacyShapeId(drawing);
                if (parsed >= shapeId)
                {
                    shapeId = parsed + 1;
                }
            }
        }

        return shapeId;
    }

    internal static uint ParseLegacyShapeId(XlsxLegacyCommentDrawing drawing)
    {
        if (drawing == null || drawing.ShapeId == null || drawing.ShapeId.Length == 0)
        {
            return 0;
        }

        int index = drawing.ShapeId.Length - 1;
        int multiplier = 1;
        uint value = 0;
        bool sawDigit = false;
        while (index >= 0)
        {
            char character = drawing.ShapeId[index];
            if (character < '0' || character > '9')
            {
                break;
            }

            value = value + (uint)((character - '0') * multiplier);
            multiplier = multiplier * 10;
            sawDigit = true;
            index = index - 1;
        }

        if (!sawDigit)
        {
            return 0;
        }

        return value;
    }

    internal XlsxLegacyCommentDrawing? FindLegacyCommentDrawing(string reference)
    {
        if (LegacyCommentDrawingPart == null || LegacyCommentDrawingPart.Drawings == null)
        {
            return null;
        }

        foreach (XlsxLegacyCommentDrawing drawing in LegacyCommentDrawingPart.Drawings)
        {
            if (drawing != null && drawing.Reference == reference)
            {
                return drawing;
            }
        }

        return null;
    }

    internal static void ApplyCommentValues(XlsxComment comment, string reference, uint authorId, string author, string text, uint shapeId)
    {
        comment.Reference = reference;
        comment.AuthorId = authorId;
        comment.HasAuthorId = true;
        comment.Author = author;
        comment.Text = text;
        comment.TextRawXml = "";
        comment.ShapeId = shapeId;
        comment.HasShapeId = true;
    }

    internal static void ApplyCommentDrawingValues(XlsxLegacyCommentDrawing drawing, string reference, int row, int column, uint shapeId, bool visible)
    {
        drawing.Reference = reference;
        drawing.Row = row - 1;
        drawing.HasRow = true;
        drawing.Column = column - 1;
        drawing.HasColumn = true;
        drawing.ShapeId = "_x0000_s" + shapeId.ToString(CultureInfo.InvariantCulture);
        drawing.Anchor = BuildDefaultCommentAnchor(row, column);
        drawing.Left = "60pt";
        drawing.Top = "12pt";
        drawing.Width = "108pt";
        drawing.Height = "59.25pt";
        drawing.Visible = visible;
        drawing.HasVisible = true;
        drawing.MoveWithCells = true;
        drawing.SizeWithCells = true;
        drawing.RawXml = "";
    }

    internal static void ApplyDrawingValuesToComment(XlsxComment comment, XlsxLegacyCommentDrawing drawing)
    {
        comment.Row = drawing.Row;
        comment.HasRow = drawing.HasRow;
        comment.Column = drawing.Column;
        comment.HasColumn = drawing.HasColumn;
        comment.Anchor = drawing.Anchor;
        comment.Style = drawing.Style;
        comment.Left = drawing.Left;
        comment.Top = drawing.Top;
        comment.Width = drawing.Width;
        comment.Height = drawing.Height;
        comment.Visible = drawing.Visible;
        comment.HasVisible = drawing.HasVisible;
        comment.MoveWithCells = drawing.MoveWithCells;
        comment.SizeWithCells = drawing.SizeWithCells;
        comment.HasPosition = true;
    }

    internal static string BuildDefaultCommentAnchor(int row, int column)
    {
        int startColumn = column;
        int startRow = row - 1;
        int endColumn = column + 2;
        int endRow = row + 3;
        return startColumn.ToString(CultureInfo.InvariantCulture) + ", 15, " + startRow.ToString(CultureInfo.InvariantCulture) + ", 2, " + endColumn.ToString(CultureInfo.InvariantCulture) + ", 15, " + endRow.ToString(CultureInfo.InvariantCulture) + ", 16";
    }

    internal static string NormalizeCommentAuthor(string text)
    {
        if (text == null)
        {
            return "";
        }

        return text.Trim();
    }

    internal static string NormalizeCommentText(string text)
    {
        if (text == null)
        {
            return "";
        }

        return text;
    }

    internal XlsxWorksheetHyperlinkResult ValidateNewHyperlink(string reference, string target)
    {
        XlsxWorksheetHyperlinkResult result = new XlsxWorksheetHyperlinkResult();
        if (Hyperlinks == null)
        {
            result.AddError("worksheet.hyperlinksNull", "Worksheet hyperlinks list must not be null.");
            return result;
        }

        string normalizedTarget = NormalizeHyperlinkText(target);
        if (normalizedTarget.Length == 0)
        {
            result.AddError("worksheet.hyperlinkTargetMissing", "Worksheet hyperlink target must not be empty.");
        }

        XlsxA1RangeReferenceResult referenceResult = XlsxA1RangeReferenceConverter.Parse(reference);
        if (!referenceResult.IsValid)
        {
            result.AddError("worksheet.hyperlinkReferenceInvalid", "Worksheet hyperlink reference must be a valid A1 cell or range reference.");
            AddHyperlinkRangeDiagnostics(result, referenceResult);
        }

        return result;
    }

    internal string GetNextHyperlinkRelationshipId()
    {
        int index = 1;
        while (true)
        {
            string relationshipId = "rId" + index.ToString(CultureInfo.InvariantCulture);
            if (!HasHyperlinkRelationshipId(relationshipId))
            {
                return relationshipId;
            }

            index = index + 1;
        }
    }

    internal bool HasHyperlinkRelationshipId(string relationshipId)
    {
        if (Hyperlinks == null)
        {
            return false;
        }

        foreach (XlsxWorksheetHyperlink hyperlink in Hyperlinks)
        {
            if (hyperlink != null && hyperlink.RelationshipId == relationshipId)
            {
                return true;
            }
        }

        return false;
    }

    internal static void AddHyperlinkRangeDiagnostics(XlsxWorksheetHyperlinkResult result, XlsxA1RangeReferenceResult referenceResult)
    {
        foreach (XlsxDiagnostic diagnostic in referenceResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static string NormalizeHyperlinkText(string text)
    {
        if (text == null)
        {
            return "";
        }

        return text.Trim();
    }
}
