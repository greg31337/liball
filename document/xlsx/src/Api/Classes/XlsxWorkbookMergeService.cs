using System;
using System.Collections.Generic;
using System.Globalization;

namespace xlsx;

/// <summary>
/// Copies worksheets and merges workbooks while remapping workbook-global indexes, names, formulas, and package relationships.
/// </summary>
public class XlsxWorkbookMergeService : IXlsxWorkbookMergeService
{
    /// <summary>Initializes a workbook merge service.</summary>
    public XlsxWorkbookMergeService()
    {
    }

    /// <summary>Copies one worksheet and its dependent content into another workbook.</summary>
    /// <param name="destination">The workbook receiving the worksheet.</param><param name="source">The source workbook.</param><param name="sheetName">The source worksheet name.</param><param name="options">Conflict and dependency options.</param>
    /// <returns>The complete merge mappings and diagnostics.</returns>
    public XlsxWorkbookMergeResult CopyWorksheet(XlsxWorkbook destination, XlsxWorkbook source, string sheetName, XlsxWorkbookMergeOptions? options)
    {
        XlsxWorkbookMergeResult result = new XlsxWorkbookMergeResult();
        XlsxWorkbookMergeContext? context = CreateMergeContext(destination, source, options, result);
        if (context == null) { return result; }
        int sourceIndex = FindSheetIndex(context.Source, sheetName);
        if (sourceIndex < 0 || sourceIndex >= context.Source.Worksheets.Count)
        {
            result.AddError("workbookMerge.sheetNotFound", "The requested source worksheet was not found.");
            return result;
        }
        List<int> sourceIndexes = new List<int>();
        sourceIndexes.Add(sourceIndex);
        PlanSheetMappings(context, sourceIndexes);
        PreflightMerge(context, sourceIndexes);
        if (!result.IsValid) { return result; }
        MergeStyles(context);
        MergeSharedStrings(context);
        CopyPlannedSheets(context, sourceIndexes);
        if (context.Options.CopyDefinedNames) { CopyDefinedNames(context, sourceIndexes); }
        UpdateCopiedFormulaReferences(context);
        StoreCopiedWorksheets(context);
        return result;
    }

    /// <summary>Merges every source worksheet and applicable workbook-global content into another workbook.</summary>
    /// <param name="destination">The workbook receiving the content.</param><param name="source">The source workbook.</param><param name="options">Conflict and dependency options.</param>
    /// <returns>The complete merge mappings and diagnostics.</returns>
    public XlsxWorkbookMergeResult MergeWorkbooks(XlsxWorkbook destination, XlsxWorkbook source, XlsxWorkbookMergeOptions? options)
    {
        XlsxWorkbookMergeResult result = new XlsxWorkbookMergeResult();
        XlsxWorkbookMergeContext? context = CreateMergeContext(destination, source, options, result);
        if (context == null) { return result; }
        List<int> sourceIndexes = new List<int>();
        int index = 0;
        while (index < context.Source.Worksheets.Count)
        {
            sourceIndexes.Add(index);
            index = index + 1;
        }
        PlanSheetMappings(context, sourceIndexes);
        PreflightMerge(context, sourceIndexes);
        if (!result.IsValid) { return result; }
        MergeStyles(context);
        MergeSharedStrings(context);
        CopyPlannedSheets(context, sourceIndexes);
        if (context.Options.CopyDefinedNames) { CopyDefinedNames(context, sourceIndexes); }
        UpdateCopiedFormulaReferences(context);
        StoreCopiedWorksheets(context);
        return result;
    }

    internal static XlsxWorkbookMergeContext? CreateMergeContext(XlsxWorkbook destination, XlsxWorkbook source, XlsxWorkbookMergeOptions? options, XlsxWorkbookMergeResult result)
    {
        if (destination == null || source == null)
        {
            result.AddError("workbookMerge.workbookMissing", "Source and destination workbooks are required.");
            return null;
        }
        if (destination == source)
        {
            result.AddError("workbookMerge.sameWorkbook", "Source and destination workbooks must be different objects.");
            return null;
        }
        XlsxWorkbookMergeOptions effectiveOptions = options == null ? new XlsxWorkbookMergeOptions() : options;
        if (!IsValidConflictPolicy(effectiveOptions.SheetNameConflictPolicy) || !IsValidConflictPolicy(effectiveOptions.DefinedNameConflictPolicy) || !IsValidConflictPolicy(effectiveOptions.TableNameConflictPolicy))
        {
            result.AddError("workbookMerge.conflictPolicyInvalid", "Workbook merge conflict policies are invalid.");
            return null;
        }
        XlsxWorkbookSaveResult sourceSave = source.SaveBytes();
        foreach (XlsxDiagnostic diagnostic in sourceSave.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        if (!result.IsValid) { return null; }
        XlsxReaderOptions readerOptions = new XlsxReaderOptions();
        readerOptions.AllowMacroEnabledPackages = true;
        readerOptions.SelectedRanges.Add("A1:XFD1048576");
        XlsxWorkbookOpenResult sourceOpen = XlsxWorkbook.OpenBytes(sourceSave.Bytes, readerOptions);
        foreach (XlsxDiagnostic diagnostic in sourceOpen.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        if (!result.IsValid || sourceOpen.Workbook == null) { return null; }
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(sourceSave.Bytes, readerOptions);
        foreach (XlsxDiagnostic diagnostic in package.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        XlsxPackageEntry? contentTypesEntry = package.GetEntry(XlsxPackagePaths.ContentTypes);
        if (!result.IsValid || contentTypesEntry == null)
        {
            result.AddError("workbookMerge.contentTypesMissing", "The source workbook content-types part is required.");
            return null;
        }
        XlsxContentTypesPart contentTypes = new XlsxContentTypesReader().ReadBytes(contentTypesEntry.Data);
        foreach (XlsxDiagnostic diagnostic in contentTypes.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        if (!result.IsValid) { return null; }
        return new XlsxWorkbookMergeContext(destination, sourceOpen.Workbook, package, contentTypes, effectiveOptions, result);
    }

    internal static bool IsValidConflictPolicy(XlsxWorkbookMergeConflictPolicy policy)
    {
        return policy == XlsxWorkbookMergeConflictPolicy.Fail || policy == XlsxWorkbookMergeConflictPolicy.RenameIncoming;
    }

    internal static void PlanSheetMappings(XlsxWorkbookMergeContext context, List<int> sourceIndexes)
    {
        List<string> plannedNames = new List<string>();
        foreach (int sourceIndex in sourceIndexes)
        {
            string sourceName = XlsxFormulaService.GetSheetName(context.Source, sourceIndex);
            string destinationName = sourceName;
            if (ContainsSheetName(context.Destination, destinationName) || ContainsText(plannedNames, destinationName))
            {
                if (context.Options.SheetNameConflictPolicy == XlsxWorkbookMergeConflictPolicy.Fail)
                {
                    context.Result.AddError("workbookMerge.sheetNameConflict", "A source worksheet name already exists in the destination workbook.");
                    continue;
                }
                destinationName = CreateUniqueSheetName(context.Destination, plannedNames, sourceName);
            }
            plannedNames.Add(destinationName);
            XlsxWorkbookMergeMapping mapping = CreateMapping(sourceName, destinationName, context.Source.Worksheets[sourceIndex].PartPath, "");
            context.Result.SheetNameMappings.Add(mapping);
        }
    }

    internal static void PreflightMerge(XlsxWorkbookMergeContext context, List<int> sourceIndexes)
    {
        if (!context.Result.IsValid) { return; }
        if (context.Options.DefinedNameConflictPolicy == XlsxWorkbookMergeConflictPolicy.Fail && context.Options.CopyDefinedNames)
        {
            foreach (XlsxWorkbookDefinedName sourceName in context.Source.WorkbookPart.DefinedNames)
            {
                if (sourceName == null || sourceName.HasLocalSheetId || !DefinedNameAppliesToIndexes(sourceName, context.Source, sourceIndexes)) { continue; }
                if (ContainsGlobalDefinedName(context.Destination, XlsxCellValueConverter.NormalizeText(sourceName.Name)))
                {
                    context.Result.AddError("workbookMerge.definedNameConflict", "A source defined name already exists in the destination workbook.");
                    return;
                }
            }
        }
        if (context.Options.TableNameConflictPolicy == XlsxWorkbookMergeConflictPolicy.Fail)
        {
            foreach (int sourceIndex in sourceIndexes)
            {
                XlsxWorksheetPart worksheet = context.Source.Worksheets[sourceIndex];
                foreach (XlsxTablePart table in worksheet.Tables)
                {
                    if (ContainsTableName(context.Destination, table.Name))
                    {
                        context.Result.AddError("workbookMerge.tableNameConflict", "A source table name already exists in the destination workbook.");
                        return;
                    }
                }
            }
        }
    }

    internal static void MergeStyles(XlsxWorkbookMergeContext context)
    {
        XlsxStylesPart sourceStyles = context.Source.Styles;
        if (sourceStyles == null || sourceStyles.CellFormats == null || sourceStyles.CellFormats.Count == 0)
        {
            context.StyleMap = new int[0];
            return;
        }
        if (context.Destination.Styles == null) { context.Destination.Styles = new XlsxStylesPart(); }
        XlsxStylesPart destinationStyles = context.Destination.Styles;
        XlsxStylesPartWriteResult write = new XlsxStylesPartWriter().WriteBytes(sourceStyles);
        foreach (XlsxDiagnostic diagnostic in write.Validation.Diagnostics) { context.Result.AddDiagnostic(diagnostic); }
        if (!context.Result.IsValid) { return; }
        XlsxStylesPart incoming = new XlsxStylesPartReader().ReadBytes(write.Bytes);
        foreach (XlsxDiagnostic diagnostic in incoming.Validation.Diagnostics) { context.Result.AddDiagnostic(diagnostic); }
        if (!context.Result.IsValid) { return; }

        int fontOffset = destinationStyles.Fonts.Count;
        int fillOffset = destinationStyles.Fills.Count;
        int borderOffset = destinationStyles.Borders.Count;
        int styleFormatOffset = destinationStyles.CellStyleFormats.Count;
        int differentialOffset = destinationStyles.DifferentialFormats.Count;
        Dictionary<uint, uint> numberFormats = MergeNumberFormats(destinationStyles, incoming);
        AppendRawStyleCollections(destinationStyles, incoming);
        foreach (XlsxCellFormatDefinition format in incoming.CellStyleFormats)
        {
            AdjustCellFormat(format, fontOffset, fillOffset, borderOffset, styleFormatOffset, numberFormats);
            destinationStyles.CellStyleFormats.Add(format);
        }
        context.StyleMap = new int[incoming.CellFormats.Count];
        int styleIndex = 0;
        while (styleIndex < incoming.CellFormats.Count)
        {
            XlsxCellFormatDefinition format = incoming.CellFormats[styleIndex];
            AdjustCellFormat(format, fontOffset, fillOffset, borderOffset, styleFormatOffset, numberFormats);
            int destinationIndex = destinationStyles.CellFormats.Count;
            destinationStyles.CellFormats.Add(format);
            context.StyleMap[styleIndex] = destinationIndex;
            context.Result.StyleMappings.Add(CreateMapping(styleIndex.ToString(CultureInfo.InvariantCulture), destinationIndex.ToString(CultureInfo.InvariantCulture), "", ""));
            styleIndex = styleIndex + 1;
        }
        foreach (XlsxRawStyleElement differential in incoming.DifferentialFormats) { destinationStyles.DifferentialFormats.Add(differential); }
        foreach (XlsxDifferentialStyle typedDifferential in incoming.TypedDifferentialFormats) { destinationStyles.TypedDifferentialFormats.Add(typedDifferential); }
        if (differentialOffset >= 0) { destinationStyles.DifferentialFormatsCount = (uint)destinationStyles.DifferentialFormats.Count; }
        destinationStyles.HasDifferentialFormatsCount = destinationStyles.DifferentialFormats.Count > 0;
        destinationStyles.CellStyleFormatsCount = (uint)destinationStyles.CellStyleFormats.Count;
        destinationStyles.HasCellStyleFormatsCount = destinationStyles.CellStyleFormats.Count > 0;
        destinationStyles.CellFormatsCount = (uint)destinationStyles.CellFormats.Count;
        destinationStyles.HasCellFormatsCount = destinationStyles.CellFormats.Count > 0;
        XlsxStylesPartOperations.UpdateCounts(destinationStyles);
        context.Destination.HasStyles = true;
    }

    internal static Dictionary<uint, uint> MergeNumberFormats(XlsxStylesPart destination, XlsxStylesPart incoming)
    {
        Dictionary<uint, uint> mappings = new Dictionary<uint, uint>();
        uint nextId = 164;
        foreach (XlsxNumberFormatDefinition existing in destination.NumberFormats) { if (existing.HasNumberFormatId && existing.NumberFormatId >= nextId) { nextId = existing.NumberFormatId + 1; } }
        foreach (XlsxNumberFormatDefinition format in incoming.NumberFormats)
        {
            uint sourceId = format.NumberFormatId;
            uint destinationId = sourceId;
            XlsxNumberFormatDefinition? conflict = FindNumberFormat(destination, sourceId);
            if (conflict != null && conflict.FormatCode != format.FormatCode)
            {
                destinationId = nextId;
                nextId = nextId + 1;
                format.NumberFormatId = destinationId;
            }
            if (conflict == null || destinationId != sourceId) { destination.NumberFormats.Add(format); }
            mappings[sourceId] = destinationId;
        }
        destination.NumberFormatsCount = (uint)destination.NumberFormats.Count;
        destination.HasNumberFormatsCount = destination.NumberFormats.Count > 0;
        return mappings;
    }

    internal static XlsxNumberFormatDefinition? FindNumberFormat(XlsxStylesPart styles, uint id)
    {
        foreach (XlsxNumberFormatDefinition format in styles.NumberFormats) { if (format.HasNumberFormatId && format.NumberFormatId == id) { return format; } }
        return null;
    }

    internal static void AppendRawStyleCollections(XlsxStylesPart destination, XlsxStylesPart incoming)
    {
        foreach (XlsxRawStyleElement value in incoming.Fonts) { destination.Fonts.Add(value); }
        foreach (XlsxStyleFont value in incoming.TypedFonts) { destination.TypedFonts.Add(value); }
        foreach (XlsxRawStyleElement value in incoming.Fills) { destination.Fills.Add(value); }
        foreach (XlsxStyleFill value in incoming.TypedFills) { destination.TypedFills.Add(value); }
        foreach (XlsxRawStyleElement value in incoming.Borders) { destination.Borders.Add(value); }
        foreach (XlsxStyleBorder value in incoming.TypedBorders) { destination.TypedBorders.Add(value); }
    }

    internal static void AdjustCellFormat(XlsxCellFormatDefinition format, int fontOffset, int fillOffset, int borderOffset, int styleFormatOffset, Dictionary<uint, uint> numberFormats)
    {
        if (format.HasFontId) { format.FontId = format.FontId + (uint)fontOffset; }
        if (format.HasFillId) { format.FillId = format.FillId + (uint)fillOffset; }
        if (format.HasBorderId) { format.BorderId = format.BorderId + (uint)borderOffset; }
        if (format.HasFormatId) { format.FormatId = format.FormatId + (uint)styleFormatOffset; }
        if (format.HasNumberFormatId && numberFormats.ContainsKey(format.NumberFormatId)) { format.NumberFormatId = numberFormats[format.NumberFormatId]; }
    }

    internal static void MergeSharedStrings(XlsxWorkbookMergeContext context)
    {
        if (context.Source.SharedStrings == null || context.Source.SharedStrings.Items == null)
        {
            context.SharedStringMap = new int[0];
            return;
        }
        if (context.Destination.SharedStrings == null) { context.Destination.SharedStrings = new XlsxSharedStringTablePart(); }
        context.SharedStringMap = new int[context.Source.SharedStrings.Items.Count];
        int sourceIndex = 0;
        while (sourceIndex < context.Source.SharedStrings.Items.Count)
        {
            XlsxSharedStringItem item = context.Source.SharedStrings.Items[sourceIndex];
            int destinationIndex = FindSharedString(context.Destination.SharedStrings, item);
            if (destinationIndex < 0)
            {
                context.Destination.SharedStrings.Items.Add(item);
                destinationIndex = context.Destination.SharedStrings.Items.Count - 1;
            }
            context.SharedStringMap[sourceIndex] = destinationIndex;
            context.Result.SharedStringMappings.Add(CreateMapping(sourceIndex.ToString(CultureInfo.InvariantCulture), destinationIndex.ToString(CultureInfo.InvariantCulture), "", ""));
            sourceIndex = sourceIndex + 1;
        }
        context.Destination.SharedStrings.RecalculateCountsFromReferences();
        context.Destination.HasSharedStrings = context.Destination.SharedStrings.Items.Count > 0;
    }

    internal static int FindSharedString(XlsxSharedStringTablePart table, XlsxSharedStringItem item)
    {
        int index = 0;
        while (index < table.Items.Count)
        {
            XlsxSharedStringItem existing = table.Items[index];
            if (existing.IsRichText == item.IsRichText && existing.Text == item.Text && existing.RichTextRuns.Count == item.RichTextRuns.Count) { return index; }
            index = index + 1;
        }
        return -1;
    }

    internal static void CopyPlannedSheets(XlsxWorkbookMergeContext context, List<int> sourceIndexes)
    {
        int mappingIndex = 0;
        foreach (int sourceIndex in sourceIndexes)
        {
            if (mappingIndex >= context.Result.SheetNameMappings.Count) { return; }
            XlsxWorkbookMergeMapping sheetMapping = context.Result.SheetNameMappings[mappingIndex];
            CopySheetCore(context, sourceIndex, sheetMapping);
            mappingIndex = mappingIndex + 1;
            if (!context.Result.IsValid) { return; }
        }
    }

    internal static void CopySheetCore(XlsxWorkbookMergeContext context, int sourceIndex, XlsxWorkbookMergeMapping sheetMapping)
    {
        XlsxWorkbookSheetOperationResult add = context.Destination.AddSheet(sheetMapping.Destination);
        foreach (XlsxDiagnostic diagnostic in add.Validation.Diagnostics) { context.Result.AddDiagnostic(diagnostic); }
        if (!context.Result.IsValid || add.Sheet == null) { return; }
        string destinationPath = context.Destination.GetWorkbookRelationshipTargetPath(add.Sheet.RelationshipId);
        if (destinationPath.Length == 0)
        {
            context.Result.AddError("workbookMerge.sheetTargetMissing", "The destination worksheet relationship target could not be resolved.");
            return;
        }
        XlsxWorksheetPart worksheet = context.Source.Worksheets[sourceIndex];
        string sourcePath = worksheet.PartPath;
        worksheet.PartPath = destinationPath;
        worksheet.Tables = new List<XlsxTablePart>();
        worksheet.CommentsPart = new XlsxCommentsPart();
        worksheet.HasCommentsPart = false;
        AdjustWorksheetIndexes(worksheet, context.StyleMap, context.SharedStringMap);
        context.Destination.Worksheets.Add(worksheet);
        context.CopiedWorksheets.Add(worksheet);
        sheetMapping.DestinationPath = destinationPath;
        context.Result.PartMappings.Add(CreateMapping("", "", sourcePath, destinationPath));
        context.Result.CopiedSheetCount = context.Result.CopiedSheetCount + 1;
        if (context.Options.CopyDependentParts)
        {
            CopyRelationshipGraph(context, sourcePath, destinationPath);
            HydrateCopiedParts(context, worksheet);
        }
    }

    internal static void AdjustWorksheetIndexes(XlsxWorksheetPart worksheet, int[] styleMap, int[] sharedStringMap)
    {
        foreach (XlsxWorksheetColumn column in worksheet.Columns)
        {
            if (column.HasStyle && column.Style < (uint)styleMap.Length) { column.Style = (uint)styleMap[(int)column.Style]; }
        }
        foreach (XlsxWorksheetRow row in worksheet.Rows)
        {
            if (row.HasStyle && row.Style < (uint)styleMap.Length) { row.Style = (uint)styleMap[(int)row.Style]; }
            foreach (XlsxWorksheetCell cell in row.Cells)
            {
                if (cell.HasStyle && cell.Style < (uint)styleMap.Length) { cell.Style = (uint)styleMap[(int)cell.Style]; }
                if (cell.Type == "s")
                {
                    XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseUnsignedInteger(cell.Value);
                    if (parsed.IsValid && parsed.UnsignedIntValue < (uint)sharedStringMap.Length) { cell.Value = sharedStringMap[(int)parsed.UnsignedIntValue].ToString(CultureInfo.InvariantCulture); }
                }
            }
        }
    }

    internal static void CopyRelationshipGraph(XlsxWorkbookMergeContext context, string sourcePartPath, string destinationPartPath)
    {
        string sourceRelationshipPath = XlsxPackagePath.GetRelationshipPartPath(sourcePartPath);
        XlsxPackageEntry? entry = context.SourcePackage.GetEntry(sourceRelationshipPath);
        if (entry == null) { return; }
        XlsxRelationshipsPart relationships = new XlsxPartRelationshipsReader().ReadBytes(sourcePartPath, entry.Data, new XlsxReaderOptions());
        foreach (XlsxDiagnostic diagnostic in relationships.Validation.Diagnostics) { context.Result.AddDiagnostic(diagnostic); }
        if (!context.Result.IsValid) { return; }
        relationships.SourcePartPath = destinationPartPath;
        relationships.PartPath = XlsxPackagePath.GetRelationshipPartPath(destinationPartPath);
        foreach (XlsxRelationship relationship in relationships.Relationships)
        {
            string identifier = XlsxCellValueConverter.NormalizeText(relationship.Id);
            XlsxWorkbookMergeMapping relationshipMapping = CreateMapping(sourcePartPath + "#" + identifier, destinationPartPath + "#" + identifier, "", "");
            context.Result.RelationshipMappings.Add(relationshipMapping);
            bool external = relationship.IsExternal || relationship.TargetMode == "External";
            if (external) { continue; }
            XlsxPackagePathResult resolved = XlsxPackagePath.ResolveRelationshipTarget(sourcePartPath, relationship.Target, false);
            foreach (XlsxDiagnostic diagnostic in resolved.Validation.Diagnostics) { context.Result.AddDiagnostic(diagnostic); }
            if (!resolved.Validation.IsValid) { continue; }
            string sourceTargetPath = resolved.Path;
            XlsxWorkbookMergeMapping? partMapping = FindPartMapping(context.Result.PartMappings, sourceTargetPath);
            if (partMapping == null)
            {
                string destinationTargetPath = CreateUniquePartPath(context.Destination, context.Result.PartMappings, sourceTargetPath);
                partMapping = CreateMapping("", "", sourceTargetPath, destinationTargetPath);
                context.Result.PartMappings.Add(partMapping);
                AddTypedPartMapping(context.Result, relationship.Type, sourceTargetPath, destinationTargetPath);
                CopyPartPayload(context, sourceTargetPath, destinationTargetPath);
                if (!context.Result.IsValid) { return; }
                CopyRelationshipGraph(context, sourceTargetPath, destinationTargetPath);
                if (!context.Result.IsValid) { return; }
            }
            relationshipMapping.SourcePath = sourceTargetPath;
            relationshipMapping.DestinationPath = partMapping.DestinationPath;
            relationship.TargetPath = partMapping.DestinationPath;
            relationship.Target = GetRelativePartTarget(destinationPartPath, partMapping.DestinationPath) + XlsxCellValueConverter.NormalizeText(relationship.TargetFragment);
        }
        XlsxRelationshipsWriteResult write = new XlsxPartRelationshipsWriter().WriteBytes(relationships);
        foreach (XlsxDiagnostic diagnostic in write.Validation.Diagnostics) { context.Result.AddDiagnostic(diagnostic); }
        if (!context.Result.IsValid) { return; }
        StoreUnknownPart(context.Destination, relationships.PartPath, XlsxContentTypes.Relationships, write.Bytes);
        context.Destination.ReplaceRelationshipAggregate(relationships);
    }

    internal static void CopyPartPayload(XlsxWorkbookMergeContext context, string sourcePath, string destinationPath)
    {
        XlsxPackageEntry? entry = context.SourcePackage.GetEntry(sourcePath);
        if (entry == null)
        {
            context.Result.AddError("workbookMerge.dependentPartMissing", "A worksheet relationship target is missing from the source package.");
            return;
        }
        string contentType = XlsxCellValueConverter.NormalizeText(context.SourceContentTypes.GetContentType(sourcePath));
        StoreUnknownPart(context.Destination, destinationPath, contentType, entry.Data);
    }

    internal static void StoreUnknownPart(XlsxWorkbook workbook, string path, string contentType, byte[] data)
    {
        if (workbook.UnknownParts == null) { workbook.UnknownParts = new XlsxUnknownPartStore(); }
        workbook.UnknownParts.RemovePart(path);
        workbook.UnknownParts.AddPart(path, contentType, data);
    }

    internal static void AddTypedPartMapping(XlsxWorkbookMergeResult result, string? relationshipType, string sourcePath, string destinationPath)
    {
        string type = XlsxCellValueConverter.NormalizeText(relationshipType);
        XlsxWorkbookMergeMapping mapping = CreateMapping("", "", sourcePath, destinationPath);
        if (type == XlsxRelationshipTypes.Table) { result.TableMappings.Add(mapping); }
        else if (type == XlsxRelationshipTypes.PivotTable || type == XlsxRelationshipTypes.PivotCacheDefinition || type == XlsxRelationshipTypes.PivotCacheRecords) { result.PivotMappings.Add(mapping); }
        else if (type == XlsxRelationshipTypes.Drawing) { result.DrawingMappings.Add(mapping); }
        else if (type == XlsxRelationshipTypes.Chart) { result.ChartMappings.Add(mapping); }
        else if (type == XlsxRelationshipTypes.Comments || type == XlsxRelationshipTypes.ThreadedComments) { result.CommentMappings.Add(mapping); }
    }

    internal static XlsxWorkbookMergeMapping? FindPartMapping(List<XlsxWorkbookMergeMapping> mappings, string sourcePath)
    {
        foreach (XlsxWorkbookMergeMapping mapping in mappings) { if (mapping.SourcePath == sourcePath) { return mapping; } }
        return null;
    }

    internal static string CreateUniquePartPath(XlsxWorkbook destination, List<XlsxWorkbookMergeMapping> mappings, string sourcePath)
    {
        if (!IsDestinationPartPathUsed(destination, mappings, sourcePath)) { return sourcePath; }
        int slash = sourcePath.LastIndexOf('/');
        int dot = sourcePath.LastIndexOf('.');
        if (dot <= slash) { dot = sourcePath.Length; }
        string prefix = sourcePath.Substring(0, dot);
        string extension = sourcePath.Substring(dot);
        int index = 2;
        while (true)
        {
            string candidate = prefix + "Copy" + index.ToString(CultureInfo.InvariantCulture) + extension;
            if (!IsDestinationPartPathUsed(destination, mappings, candidate)) { return candidate; }
            index = index + 1;
        }
    }

    internal static bool IsDestinationPartPathUsed(XlsxWorkbook workbook, List<XlsxWorkbookMergeMapping> mappings, string path)
    {
        foreach (XlsxWorkbookMergeMapping mapping in mappings) { if (mapping.DestinationPath == path) { return true; } }
        if (workbook.UnknownParts != null && workbook.UnknownParts.ContainsPart(path)) { return true; }
        foreach (XlsxPackagePart part in workbook.PackageParts) { if (part.Path == path) { return true; } }
        foreach (XlsxWorksheetPart part in workbook.Worksheets) { if (part.PartPath == path) { return true; } }
        foreach (XlsxDrawingPart part in workbook.Drawings) { if (part.PartPath == path) { return true; } }
        foreach (XlsxChartPart part in workbook.Charts) { if (part.PartPath == path) { return true; } }
        foreach (XlsxCommentsPart part in workbook.Comments) { if (part.PartPath == path) { return true; } }
        foreach (XlsxThreadedCommentsPart part in workbook.ThreadedComments) { if (part.PartPath == path) { return true; } }
        foreach (XlsxPivotTableDefinitionPart part in workbook.PivotTableDefinitions) { if (part.PartPath == path) { return true; } }
        foreach (XlsxPivotCacheDefinitionPart part in workbook.PivotCacheDefinitions) { if (part.PartPath == path) { return true; } }
        return false;
    }

    internal static string GetRelativePartTarget(string sourcePartPath, string targetPartPath)
    {
        int sourceSlash = sourcePartPath.LastIndexOf('/');
        string sourceDirectory = sourceSlash <= 0 ? "" : sourcePartPath.Substring(1, sourceSlash - 1);
        string target = targetPartPath.StartsWith("/", StringComparison.Ordinal) ? targetPartPath.Substring(1) : targetPartPath;
        string[] sourceSegments = sourceDirectory.Length == 0 ? new string[0] : sourceDirectory.Split('/');
        string[] targetSegments = target.Split('/');
        int common = 0;
        while (common < sourceSegments.Length && common < targetSegments.Length && sourceSegments[common] == targetSegments[common]) { common = common + 1; }
        string relative = "";
        int index = common;
        while (index < sourceSegments.Length) { relative = relative + "../"; index = index + 1; }
        index = common;
        while (index < targetSegments.Length)
        {
            if (relative.Length > 0 && !relative.EndsWith("/", StringComparison.Ordinal)) { relative = relative + "/"; }
            relative = relative + targetSegments[index];
            index = index + 1;
        }
        return relative;
    }

    internal static void HydrateCopiedParts(XlsxWorkbookMergeContext context, XlsxWorksheetPart worksheet)
    {
        foreach (XlsxWorkbookMergeMapping mapping in context.Result.TableMappings)
        {
            if (!IsDirectRelationshipTarget(context.Result.RelationshipMappings, worksheet.PartPath, mapping.DestinationPath)) { continue; }
            HydrateTable(context, worksheet, mapping);
        }
        foreach (XlsxWorkbookMergeMapping mapping in context.Result.PivotMappings) { HydratePivot(context, mapping); }
        foreach (XlsxWorkbookMergeMapping mapping in context.Result.DrawingMappings) { HydrateDrawing(context, mapping); }
        foreach (XlsxWorkbookMergeMapping mapping in context.Result.ChartMappings) { HydrateChart(context, mapping); }
        foreach (XlsxWorkbookMergeMapping mapping in context.Result.CommentMappings) { HydrateComments(context, mapping); }
    }

    internal static bool IsDirectRelationshipTarget(List<XlsxWorkbookMergeMapping> relationships, string destinationSourcePath, string destinationTargetPath)
    {
        foreach (XlsxWorkbookMergeMapping mapping in relationships)
        {
            if (mapping.Destination.StartsWith(destinationSourcePath + "#", StringComparison.Ordinal) && mapping.DestinationPath == destinationTargetPath) { return true; }
        }
        return false;
    }

    internal static void HydrateTable(XlsxWorkbookMergeContext context, XlsxWorksheetPart worksheet, XlsxWorkbookMergeMapping mapping)
    {
        XlsxUnknownPart? raw = context.Destination.UnknownParts.GetPart(mapping.DestinationPath);
        if (raw == null) { return; }
        XlsxTablePart table = new XlsxTablePartReader().ReadBytes(raw.Data);
        table.PartPath = mapping.DestinationPath;
        string sourceName = table.Name;
        string destinationName = sourceName;
        if (ContainsTableName(context.Destination, destinationName) || ContainsMappedDestinationName(context.Result.TableMappings, mapping, destinationName))
        {
            destinationName = CreateUniqueTableName(context.Destination, context.Result.TableMappings, sourceName);
        }
        table.Name = destinationName;
        table.DisplayName = destinationName;
        table.Id = GetNextTableId(context.Destination, context.Result.TableMappings);
        table.HasId = true;
        table.RawXml = "";
        mapping.Source = sourceName;
        mapping.Destination = destinationName;
        if (sourceName != destinationName && context.Options.UpdateFormulaReferences) { UpdateStructuredReferences(worksheet, sourceName, destinationName, context.Result); }
        XlsxTablePartWriteResult write = new XlsxTablePartWriter().WriteBytes(table);
        foreach (XlsxDiagnostic diagnostic in write.Validation.Diagnostics) { context.Result.AddDiagnostic(diagnostic); }
        if (write.IsValid) { StoreUnknownPart(context.Destination, table.PartPath, XlsxContentTypes.Table, write.Bytes); }
        worksheet.Tables.Add(table);
    }

    internal static void HydratePivot(XlsxWorkbookMergeContext context, XlsxWorkbookMergeMapping mapping)
    {
        XlsxUnknownPart? raw = context.Destination.UnknownParts.GetPart(mapping.DestinationPath);
        if (raw == null) { return; }
        string contentType = XlsxCellValueConverter.NormalizeText(raw.ContentType);
        if (contentType == XlsxContentTypes.PivotTable && !ContainsPivotTablePath(context.Destination, mapping.DestinationPath))
        {
            XlsxPivotTableDefinitionPart part = new XlsxPivotTableDefinitionReader().ReadBytes(mapping.DestinationPath, raw.Data);
            mapping.Source = part.Name;
            part.Name = CreateUniquePivotName(context.Destination, part.Name);
            mapping.Destination = part.Name;
            context.Destination.PivotTableDefinitions.Add(part);
        }
        else if (contentType == XlsxContentTypes.PivotCacheDefinition && !ContainsPivotCachePath(context.Destination, mapping.DestinationPath))
        {
            XlsxPivotCacheDefinitionPart part = new XlsxPivotCacheDefinitionReader().ReadBytes(mapping.DestinationPath, raw.Data);
            context.Destination.PivotCacheDefinitions.Add(part);
        }
    }

    internal static void HydrateDrawing(XlsxWorkbookMergeContext context, XlsxWorkbookMergeMapping mapping)
    {
        if (ContainsDrawingPath(context.Destination, mapping.DestinationPath)) { return; }
        XlsxUnknownPart? raw = context.Destination.UnknownParts.GetPart(mapping.DestinationPath);
        if (raw == null) { return; }
        XlsxDrawingPart part = new XlsxDrawingPartReader().ReadBytes(raw.Data);
        part.PartPath = mapping.DestinationPath;
        context.Destination.Drawings.Add(part);
    }

    internal static void HydrateChart(XlsxWorkbookMergeContext context, XlsxWorkbookMergeMapping mapping)
    {
        if (ContainsChartPath(context.Destination, mapping.DestinationPath)) { return; }
        XlsxUnknownPart? raw = context.Destination.UnknownParts.GetPart(mapping.DestinationPath);
        if (raw == null) { return; }
        XlsxChartPart part = new XlsxChartPartReader().ReadBytesFromPart(mapping.DestinationPath, raw.Data);
        context.Destination.Charts.Add(part);
    }

    internal static void HydrateComments(XlsxWorkbookMergeContext context, XlsxWorkbookMergeMapping mapping)
    {
        XlsxUnknownPart? raw = context.Destination.UnknownParts.GetPart(mapping.DestinationPath);
        if (raw == null) { return; }
        string contentType = XlsxCellValueConverter.NormalizeText(raw.ContentType);
        if (contentType == XlsxContentTypes.Comments && !ContainsCommentsPath(context.Destination, mapping.DestinationPath))
        {
            XlsxCommentsPart part = new XlsxCommentsPartReader().ReadBytes(raw.Data);
            part.PartPath = mapping.DestinationPath;
            context.Destination.Comments.Add(part);
        }
        else if (contentType == XlsxContentTypes.ThreadedComments && !ContainsThreadedCommentsPath(context.Destination, mapping.DestinationPath))
        {
            XlsxThreadedCommentsPart part = new XlsxThreadedCommentsPartReader().ReadBytes(raw.Data);
            part.PartPath = mapping.DestinationPath;
            context.Destination.ThreadedComments.Add(part);
        }
    }

    internal static void CopyDefinedNames(XlsxWorkbookMergeContext context, List<int> sourceIndexes)
    {
        foreach (XlsxWorkbookDefinedName definedName in context.Source.WorkbookPart.DefinedNames)
        {
            if (definedName == null || !DefinedNameAppliesToIndexes(definedName, context.Source, sourceIndexes)) { continue; }
            string sourceName = XlsxCellValueConverter.NormalizeText(definedName.Name);
            string destinationName = sourceName;
            if (!definedName.HasLocalSheetId && ContainsGlobalDefinedName(context.Destination, destinationName))
            {
                destinationName = CreateUniqueDefinedName(context.Destination, context.Result.DefinedNameMappings, sourceName);
            }
            definedName.Name = destinationName;
            if (definedName.HasLocalSheetId)
            {
                string localSourceSheet = XlsxFormulaService.GetSheetName(context.Source, definedName.LocalSheetId);
                XlsxWorkbookMergeMapping? localMapping = FindNameMapping(context.Result.SheetNameMappings, localSourceSheet);
                if (localMapping == null) { continue; }
                definedName.LocalSheetId = FindSheetIndex(context.Destination, localMapping.Destination);
            }
            string originalText = definedName.Text;
            definedName.Text = UpdateFormulaForSheetMappings(originalText, context.Result.SheetNameMappings, context.Result);
            context.Destination.WorkbookPart.DefinedNames.Add(definedName);
            XlsxWorkbookMergeMapping mapping = CreateMapping(sourceName, destinationName, "", "");
            context.Result.DefinedNameMappings.Add(mapping);
            if (originalText != definedName.Text) { context.Result.FormulaMappings.Add(CreateMapping(originalText, definedName.Text, "definedName:" + sourceName, "definedName:" + destinationName)); }
        }
    }

    internal static bool DefinedNameAppliesToIndexes(XlsxWorkbookDefinedName definedName, XlsxWorkbook source, List<int> sourceIndexes)
    {
        if (definedName.HasLocalSheetId) { return ContainsIndex(sourceIndexes, definedName.LocalSheetId); }
        if (sourceIndexes.Count == source.Worksheets.Count) { return true; }
        foreach (int index in sourceIndexes)
        {
            string sheetName = XlsxFormulaService.GetSheetName(source, index);
            XlsxFormulaReferenceUpdateResult update = XlsxWorksheetPart.RenameFormulaSheetReferences(definedName.Text, sheetName, "__MERGED_SHEET__");
            if (update.IsValid && update.RenamedSheetCount > 0) { return true; }
        }
        return false;
    }

    internal static bool ContainsIndex(List<int> indexes, int value)
    {
        foreach (int index in indexes) { if (index == value) { return true; } }
        return false;
    }

    internal static void UpdateCopiedFormulaReferences(XlsxWorkbookMergeContext context)
    {
        if (!context.Options.UpdateFormulaReferences) { return; }
        foreach (XlsxWorksheetPart worksheet in context.CopiedWorksheets)
        {
            foreach (XlsxWorksheetRow row in worksheet.Rows)
            {
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (!cell.HasFormula && XlsxCellValueConverter.NormalizeText(cell.Formula).Length == 0) { continue; }
                    string original = XlsxWorksheetPart.GetFormulaText(cell);
                    string updated = UpdateFormulaForSheetMappings(original, context.Result.SheetNameMappings, context.Result);
                    updated = UpdateFormulaForTableMappings(updated, context.Result.TableMappings);
                    if (original == updated) { continue; }
                    cell.Formula = updated;
                    if (cell.FormulaInfo != null) { cell.FormulaInfo.Text = updated; }
                    context.Result.FormulaMappings.Add(CreateMapping(original, updated, worksheet.PartPath + "#" + XlsxCellValueConverter.NormalizeText(cell.Reference), worksheet.PartPath + "#" + XlsxCellValueConverter.NormalizeText(cell.Reference)));
                }
            }
        }
        UpdateChartFormulaReferences(context);
    }

    internal static string UpdateFormulaForSheetMappings(string formula, List<XlsxWorkbookMergeMapping> mappings, XlsxWorkbookMergeResult result)
    {
        string updated = XlsxCellValueConverter.NormalizeText(formula);
        foreach (XlsxWorkbookMergeMapping mapping in mappings)
        {
            if (mapping.Source == mapping.Destination) { continue; }
            XlsxFormulaReferenceUpdateResult update = XlsxWorksheetPart.RenameFormulaSheetReferences(updated, mapping.Source, mapping.Destination);
            foreach (XlsxDiagnostic diagnostic in update.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
            if (update.IsValid) { updated = update.Formula; }
        }
        return updated;
    }

    internal static string UpdateFormulaForTableMappings(string formula, List<XlsxWorkbookMergeMapping> mappings)
    {
        string updated = formula;
        foreach (XlsxWorkbookMergeMapping mapping in mappings)
        {
            if (mapping.Source.Length > 0 && mapping.Source != mapping.Destination) { updated = updated.Replace(mapping.Source + "[", mapping.Destination + "[", StringComparison.OrdinalIgnoreCase); }
        }
        return updated;
    }

    internal static void UpdateStructuredReferences(XlsxWorksheetPart worksheet, string sourceName, string destinationName, XlsxWorkbookMergeResult result)
    {
        foreach (XlsxWorksheetRow row in worksheet.Rows)
        {
            foreach (XlsxWorksheetCell cell in row.Cells)
            {
                if (!cell.HasFormula && XlsxCellValueConverter.NormalizeText(cell.Formula).Length == 0) { continue; }
                string original = XlsxWorksheetPart.GetFormulaText(cell);
                string updated = original.Replace(sourceName + "[", destinationName + "[", StringComparison.OrdinalIgnoreCase);
                if (updated == original) { continue; }
                cell.Formula = updated;
                if (cell.FormulaInfo != null) { cell.FormulaInfo.Text = updated; }
                result.FormulaMappings.Add(CreateMapping(original, updated, worksheet.PartPath + "#" + XlsxCellValueConverter.NormalizeText(cell.Reference), worksheet.PartPath + "#" + XlsxCellValueConverter.NormalizeText(cell.Reference)));
            }
        }
    }

    internal static void UpdateChartFormulaReferences(XlsxWorkbookMergeContext context)
    {
        foreach (XlsxWorkbookMergeMapping mapping in context.Result.ChartMappings)
        {
            XlsxChartPart? chart = FindChartByPath(context.Destination, mapping.DestinationPath);
            if (chart == null) { continue; }
            bool changed = false;
            foreach (XlsxChartSeriesReference series in chart.Series)
            {
                string text = UpdateFormulaForSheetMappings(series.TextFormula, context.Result.SheetNameMappings, context.Result);
                string category = UpdateFormulaForSheetMappings(series.CategoryFormula, context.Result.SheetNameMappings, context.Result);
                string values = UpdateFormulaForSheetMappings(series.ValuesFormula, context.Result.SheetNameMappings, context.Result);
                string bubble = UpdateFormulaForSheetMappings(series.BubbleSizeFormula, context.Result.SheetNameMappings, context.Result);
                if (text != series.TextFormula || category != series.CategoryFormula || values != series.ValuesFormula || bubble != series.BubbleSizeFormula) { changed = true; }
                series.TextFormula = text;
                series.CategoryFormula = category;
                series.ValuesFormula = values;
                series.BubbleSizeFormula = bubble;
            }
            if (!changed) { continue; }
            chart.RawXml = "";
            XlsxChartPartWriteResult write = new XlsxChartPartWriter().WriteBytes(chart);
            foreach (XlsxDiagnostic diagnostic in write.Validation.Diagnostics) { context.Result.AddDiagnostic(diagnostic); }
            if (write.IsValid) { StoreUnknownPart(context.Destination, chart.PartPath, XlsxContentTypes.Chart, write.Bytes); }
        }
    }

    internal static void StoreCopiedWorksheets(XlsxWorkbookMergeContext context)
    {
        foreach (XlsxWorksheetPart worksheet in context.CopiedWorksheets)
        {
            XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteBytes(worksheet);
            foreach (XlsxDiagnostic diagnostic in write.Validation.Diagnostics) { context.Result.AddDiagnostic(diagnostic); }
            if (write.IsValid) { StoreUnknownPart(context.Destination, worksheet.PartPath, XlsxContentTypes.Worksheet, write.Bytes); }
        }
    }

    internal static bool ContainsSheetName(XlsxWorkbook workbook, string name)
    {
        return FindSheetIndex(workbook, name) >= 0;
    }

    internal static int FindSheetIndex(XlsxWorkbook workbook, string? name)
    {
        if (workbook == null || workbook.WorkbookPart == null || workbook.WorkbookPart.Sheets == null) { return -1; }
        int index = 0;
        while (index < workbook.WorkbookPart.Sheets.Count)
        {
            if (string.Equals(workbook.WorkbookPart.Sheets[index].Name, name, StringComparison.OrdinalIgnoreCase)) { return index; }
            index = index + 1;
        }
        return -1;
    }

    internal static string CreateUniqueSheetName(XlsxWorkbook workbook, List<string> plannedNames, string sourceName)
    {
        int suffix = 2;
        while (true)
        {
            string suffixText = " (" + suffix.ToString(CultureInfo.InvariantCulture) + ")";
            int maximumBaseLength = 31 - suffixText.Length;
            string baseName = sourceName.Length > maximumBaseLength ? sourceName.Substring(0, maximumBaseLength) : sourceName;
            string candidate = baseName + suffixText;
            if (!ContainsSheetName(workbook, candidate) && !ContainsText(plannedNames, candidate)) { return candidate; }
            suffix = suffix + 1;
        }
    }

    internal static bool ContainsText(List<string> values, string value)
    {
        foreach (string item in values) { if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) { return true; } }
        return false;
    }

    internal static bool ContainsGlobalDefinedName(XlsxWorkbook workbook, string name)
    {
        foreach (XlsxWorkbookDefinedName definedName in workbook.WorkbookPart.DefinedNames)
        {
            if (!definedName.HasLocalSheetId && string.Equals(definedName.Name, name, StringComparison.OrdinalIgnoreCase)) { return true; }
        }
        return false;
    }

    internal static string CreateUniqueDefinedName(XlsxWorkbook workbook, List<XlsxWorkbookMergeMapping> mappings, string sourceName)
    {
        int suffix = 2;
        while (true)
        {
            string candidate = sourceName + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            if (!ContainsGlobalDefinedName(workbook, candidate) && !ContainsMappedDestinationName(mappings, null, candidate)) { return candidate; }
            suffix = suffix + 1;
        }
    }

    internal static bool ContainsTableName(XlsxWorkbook workbook, string name)
    {
        foreach (XlsxWorksheetPart worksheet in workbook.Worksheets)
        {
            foreach (XlsxTablePart table in worksheet.Tables) { if (string.Equals(table.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(table.DisplayName, name, StringComparison.OrdinalIgnoreCase)) { return true; } }
        }
        foreach (XlsxPackagePart packagePart in workbook.PackageParts)
        {
            if (packagePart.ContentType != XlsxContentTypes.Table) { continue; }
            XlsxTablePart table = new XlsxTablePartReader().ReadBytes(packagePart.Data);
            if (string.Equals(table.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(table.DisplayName, name, StringComparison.OrdinalIgnoreCase)) { return true; }
        }
        return false;
    }

    internal static bool ContainsMappedDestinationName(List<XlsxWorkbookMergeMapping> mappings, XlsxWorkbookMergeMapping? excluded, string name)
    {
        foreach (XlsxWorkbookMergeMapping mapping in mappings)
        {
            if (mapping != excluded && mapping.Destination.Length > 0 && string.Equals(mapping.Destination, name, StringComparison.OrdinalIgnoreCase)) { return true; }
        }
        return false;
    }

    internal static string CreateUniqueTableName(XlsxWorkbook workbook, List<XlsxWorkbookMergeMapping> mappings, string sourceName)
    {
        int suffix = 2;
        while (true)
        {
            string candidate = sourceName + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            if (!ContainsTableName(workbook, candidate) && !ContainsMappedDestinationName(mappings, null, candidate)) { return candidate; }
            suffix = suffix + 1;
        }
    }

    internal static uint GetNextTableId(XlsxWorkbook workbook, List<XlsxWorkbookMergeMapping> mappings)
    {
        uint maximum = 0;
        foreach (XlsxWorksheetPart worksheet in workbook.Worksheets)
        {
            foreach (XlsxTablePart table in worksheet.Tables) { if (table.HasId && table.Id > maximum) { maximum = table.Id; } }
        }
        return maximum + 1 + (uint)mappings.Count;
    }

    internal static bool ContainsPivotTablePath(XlsxWorkbook workbook, string path)
    {
        foreach (XlsxPivotTableDefinitionPart part in workbook.PivotTableDefinitions) { if (part.PartPath == path) { return true; } }
        return false;
    }

    internal static bool ContainsPivotCachePath(XlsxWorkbook workbook, string path)
    {
        foreach (XlsxPivotCacheDefinitionPart part in workbook.PivotCacheDefinitions) { if (part.PartPath == path) { return true; } }
        return false;
    }

    internal static string CreateUniquePivotName(XlsxWorkbook workbook, string sourceName)
    {
        string baseName = sourceName.Length == 0 ? "PivotTable" : sourceName;
        string candidate = baseName;
        int suffix = 2;
        while (ContainsPivotName(workbook, candidate))
        {
            candidate = baseName + suffix.ToString(CultureInfo.InvariantCulture);
            suffix = suffix + 1;
        }
        return candidate;
    }

    internal static bool ContainsPivotName(XlsxWorkbook workbook, string name)
    {
        foreach (XlsxPivotTableDefinitionPart part in workbook.PivotTableDefinitions) { if (string.Equals(part.Name, name, StringComparison.OrdinalIgnoreCase)) { return true; } }
        return false;
    }

    internal static bool ContainsDrawingPath(XlsxWorkbook workbook, string path)
    {
        foreach (XlsxDrawingPart part in workbook.Drawings) { if (part.PartPath == path) { return true; } }
        return false;
    }

    internal static bool ContainsChartPath(XlsxWorkbook workbook, string path)
    {
        return FindChartByPath(workbook, path) != null;
    }

    internal static XlsxChartPart? FindChartByPath(XlsxWorkbook workbook, string path)
    {
        foreach (XlsxChartPart part in workbook.Charts) { if (part.PartPath == path) { return part; } }
        return null;
    }

    internal static bool ContainsCommentsPath(XlsxWorkbook workbook, string path)
    {
        foreach (XlsxCommentsPart part in workbook.Comments) { if (part.PartPath == path) { return true; } }
        return false;
    }

    internal static bool ContainsThreadedCommentsPath(XlsxWorkbook workbook, string path)
    {
        foreach (XlsxThreadedCommentsPart part in workbook.ThreadedComments) { if (part.PartPath == path) { return true; } }
        return false;
    }

    internal static XlsxWorkbookMergeMapping? FindNameMapping(List<XlsxWorkbookMergeMapping> mappings, string source)
    {
        foreach (XlsxWorkbookMergeMapping mapping in mappings) { if (string.Equals(mapping.Source, source, StringComparison.OrdinalIgnoreCase)) { return mapping; } }
        return null;
    }

    internal static XlsxWorkbookMergeMapping CreateMapping(string source, string destination, string sourcePath, string destinationPath)
    {
        XlsxWorkbookMergeMapping mapping = new XlsxWorkbookMergeMapping();
        mapping.Source = XlsxCellValueConverter.NormalizeText(source);
        mapping.Destination = XlsxCellValueConverter.NormalizeText(destination);
        mapping.SourcePath = XlsxCellValueConverter.NormalizeText(sourcePath);
        mapping.DestinationPath = XlsxCellValueConverter.NormalizeText(destinationPath);
        return mapping;
    }
}
