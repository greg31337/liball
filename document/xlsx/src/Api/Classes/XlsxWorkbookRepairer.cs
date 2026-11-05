using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook repairer data used by the public workbook API.
/// </summary>
public class XlsxWorkbookRepairer : IXlsxWorkbookRepairer, IXlsxWorkbookRepairerOperations
{
    /// <summary>
    /// Initializes a new workbook repairer with its default state.
    /// </summary>
    public XlsxWorkbookRepairer()
    {
    }

    /// <summary>
    /// Performs the repair bytes operation using the current model state.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxWorkbookRepairResult RepairBytes(byte[] bytes)
    {
        return RepairBytesWithOptions(bytes, new XlsxWorkbookRepairOptions());
    }

    /// <summary>
    /// Performs the repair bytes operation using the current model state.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxWorkbookRepairResult RepairBytesWithOptions(byte[] bytes, XlsxWorkbookRepairOptions options)
    {
        XlsxWorkbookRepairResult result = new XlsxWorkbookRepairResult();
        XlsxWorkbookRepairOptions effectiveOptions = NormalizeOptions(options);
        if (bytes == null || bytes.Length == 0)
        {
            result.AddError("workbookRepair.bytesEmpty", "Workbook package bytes must not be empty.");
            return result;
        }

        XlsxReaderOptions readerOptions = NormalizeReaderOptions(effectiveOptions);
        XlsxWriterOptions writerOptions = NormalizeWriterOptions(effectiveOptions);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(bytes, readerOptions);
        if (!package.IsValid && package.Entries.Count == 0)
        {
            AddDiagnostics(result, package.Validation);
            return result;
        }

        byte[] packageBytes = bytes;
        if (effectiveOptions.RepairPackage)
        {
            List<XlsxPackageEntry> entries = RepairPackageEntries(result, package.Entries);
            XlsxZipPackageWriteResult packageWrite = new XlsxZipPackageWriter().WriteBytes(entries, writerOptions);
            AddDiagnostics(result, packageWrite.Validation);
            if (!packageWrite.IsValid)
            {
                return result;
            }

            packageBytes = packageWrite.Bytes;
        }

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(packageBytes, readerOptions);
        AddDiagnostics(result, open.Validation);
        if (open.Workbook == null)
        {
            return result;
        }

        XlsxWorkbookRepairResult workbookRepair = RepairWorkbookWithOptions(open.Workbook, effectiveOptions);
        AddRepairResult(result, workbookRepair);
        result.Workbook = open.Workbook;
        XlsxWorkbookSaveResult save = open.Workbook.SaveBytesWithOptions(writerOptions);
        AddDiagnostics(result, save.Validation);
        if (save.IsValid)
        {
            result.Bytes = save.Bytes;
        }
        else
        {
            result.Bytes = packageBytes;
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    /// <summary>
    /// Performs the repair workbook operation using the current model state.
    /// </summary>
    /// <param name = "workbook">The workbook model used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxWorkbookRepairResult RepairWorkbook(XlsxWorkbook workbook)
    {
        return RepairWorkbookWithOptions(workbook, new XlsxWorkbookRepairOptions());
    }

    /// <summary>
    /// Performs the repair workbook operation using the current model state.
    /// </summary>
    /// <param name = "workbook">The workbook model used by the operation.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxWorkbookRepairResult RepairWorkbookWithOptions(XlsxWorkbook workbook, XlsxWorkbookRepairOptions options)
    {
        return RepairWorkbookWithSharedStrings(workbook, null, options);
    }

    /// <summary>
    /// Performs the repair workbook operation using the current model state.
    /// </summary>
    /// <param name = "workbook">The workbook model used by the operation.</param>
    /// <param name = "sharedStrings">The shared strings value used by the operation.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxWorkbookRepairResult RepairWorkbookWithSharedStrings(XlsxWorkbook workbook, XlsxSharedStringTablePart? sharedStrings, XlsxWorkbookRepairOptions options)
    {
        XlsxWorkbookRepairResult result = new XlsxWorkbookRepairResult();
        XlsxWorkbookRepairOptions effectiveOptions = NormalizeOptions(options);
        result.Workbook = workbook;
        if (workbook == null)
        {
            result.AddError("workbookRepair.workbookNull", "Workbook must not be null.");
            return result;
        }

        RepairWorkbookShell(result, workbook);
        if (effectiveOptions.RepairStyles)
        {
            RepairStyles(result, workbook);
        }

        if (effectiveOptions.RepairWorksheets)
        {
            RepairWorksheets(result, workbook);
        }

        if (effectiveOptions.RepairRelationships)
        {
            RepairRelationships(result, workbook);
        }

        if (effectiveOptions.RepairSharedStrings)
        {
            RepairSharedStrings(result, workbook, sharedStrings);
        }

        if (effectiveOptions.RepairDimensions)
        {
            RepairDimensions(result, workbook);
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static XlsxWorkbookRepairOptions NormalizeOptions(XlsxWorkbookRepairOptions options)
    {
        if (options == null)
        {
            return new XlsxWorkbookRepairOptions();
        }

        return options;
    }

    internal static XlsxReaderOptions NormalizeReaderOptions(XlsxWorkbookRepairOptions options)
    {
        if (options == null || options.ReaderOptions == null)
        {
            return new XlsxReaderOptions();
        }

        return options.ReaderOptions;
    }

    internal static XlsxWriterOptions NormalizeWriterOptions(XlsxWorkbookRepairOptions options)
    {
        if (options == null || options.WriterOptions == null)
        {
            return new XlsxWriterOptions();
        }

        return options.WriterOptions;
    }

    internal static List<XlsxPackageEntry> RepairPackageEntries(XlsxWorkbookRepairResult result, List<XlsxPackageEntry> entries)
    {
        List<XlsxPackageEntry> repaired = CopyPackageEntries(result, entries);
        XlsxWorkbookSaveResult fallbackSave = XlsxWorkbook.Create().SaveBytes();
        if (!fallbackSave.IsValid)
        {
            AddDiagnostics(result, fallbackSave.Validation);
            return repaired;
        }

        XlsxZipPackageReadResult fallbackPackage = new XlsxZipPackageReader().ReadBytes(fallbackSave.Bytes, new XlsxReaderOptions());
        AddRequiredPackagePart(result, repaired, fallbackPackage, XlsxPackagePaths.ContentTypes);
        AddRequiredPackagePart(result, repaired, fallbackPackage, XlsxPackagePaths.PackageRelationships);
        AddRequiredPackagePart(result, repaired, fallbackPackage, XlsxPackagePaths.Workbook);
        AddRequiredPackagePart(result, repaired, fallbackPackage, XlsxPackagePaths.WorkbookRelationships);
        AddRequiredPackagePart(result, repaired, fallbackPackage, XlsxPackagePaths.Worksheet1);
        return repaired;
    }

    internal static List<XlsxPackageEntry> CopyPackageEntries(XlsxWorkbookRepairResult result, List<XlsxPackageEntry> entries)
    {
        List<XlsxPackageEntry> copied = new List<XlsxPackageEntry>();
        if (entries == null)
        {
            return copied;
        }

        Dictionary<string, bool> seen = new Dictionary<string, bool>(System.StringComparer.Ordinal);
        foreach (XlsxPackageEntry entry in entries)
        {
            if (entry == null || entry.Path == null || entry.Path.Length == 0)
            {
                result.AddRepair("workbookRepair.packageEntryRemoved", "Removed an unusable package entry during repair.");
                continue;
            }

            if (seen.ContainsKey(entry.Path))
            {
                result.AddRepair("workbookRepair.packageEntryDuplicateRemoved", "Removed a duplicate package entry during repair.");
                continue;
            }

            seen.Add(entry.Path, true);
            copied.Add(CopyPackageEntry(entry));
        }

        return copied;
    }

    internal static void AddRequiredPackagePart(XlsxWorkbookRepairResult result, List<XlsxPackageEntry> entries, XlsxZipPackageReadResult fallbackPackage, string path)
    {
        if (GetPackageEntry(entries, path) != null || fallbackPackage == null)
        {
            return;
        }

        XlsxPackageEntry? fallback = fallbackPackage.GetEntry(path);
        if (fallback == null)
        {
            return;
        }

        entries.Add(CopyPackageEntry(fallback));
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Info, "workbookRepair.packagePartAdded", "Added a missing required package part.");
        diagnostic.SetLocation(path, "", "");
        result.Repairs.Add(diagnostic.Code);
        result.RepairCount = result.RepairCount + 1;
        result.AddDiagnostic(diagnostic);
    }

    internal static XlsxPackageEntry? GetPackageEntry(List<XlsxPackageEntry> entries, string path)
    {
        if (entries == null)
        {
            return null;
        }

        foreach (XlsxPackageEntry entry in entries)
        {
            if (entry != null && entry.Path == path)
            {
                return entry;
            }
        }

        return null;
    }

    internal static XlsxPackageEntry CopyPackageEntry(XlsxPackageEntry entry)
    {
        XlsxPackageEntry copy = new XlsxPackageEntry();
        copy.Path = entry.Path;
        copy.ZipEntryName = entry.ZipEntryName;
        copy.CompressedBytes = entry.CompressedBytes;
        copy.UncompressedBytes = entry.UncompressedBytes;
        if (entry.Data == null)
        {
            copy.Data = new byte[0];
        }
        else
        {
            copy.Data = entry.Data;
        }

        return copy;
    }

    internal static void RepairWorkbookShell(XlsxWorkbookRepairResult result, XlsxWorkbook workbook)
    {
        if (workbook.WorkbookPart == null)
        {
            workbook.WorkbookPart = new XlsxWorkbookPart();
            result.AddRepair("workbookRepair.workbookPartCreated", "Created a missing workbook part.");
        }

        if (workbook.WorkbookPart.Sheets == null)
        {
            workbook.WorkbookPart.Sheets = new List<XlsxWorkbookSheet>();
            result.AddRepair("workbookRepair.sheetListCreated", "Created a missing workbook sheet list.");
        }

        if (workbook.Worksheets == null)
        {
            workbook.Worksheets = new List<XlsxWorksheetPart>();
            result.AddRepair("workbookRepair.worksheetListCreated", "Created a missing worksheet part list.");
        }

        if (workbook.PackageRelationships == null)
        {
            workbook.PackageRelationships = XlsxWorkbook.CreatePackageRelationships();
            result.AddRepair("workbookRepair.packageRelationshipsCreated", "Created missing package relationships.");
        }

        if (workbook.WorkbookRelationships == null)
        {
            workbook.WorkbookRelationships = XlsxWorkbook.CreateWorkbookRelationships();
            result.AddRepair("workbookRepair.workbookRelationshipsCreated", "Created missing workbook relationships.");
        }

        if (workbook.UnknownParts == null)
        {
            workbook.UnknownParts = new XlsxUnknownPartStore();
            result.AddRepair("workbookRepair.unknownPartStoreCreated", "Created a missing unknown part store.");
        }

        RepairWorkbookSheets(result, workbook);
        RepairWorksheetList(result, workbook);
    }

    internal static void RepairWorkbookSheets(XlsxWorkbookRepairResult result, XlsxWorkbook workbook)
    {
        Dictionary<string, bool> names = new Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);
        Dictionary<uint, bool> sheetIds = new Dictionary<uint, bool>();
        uint nextSheetId = 1;
        int index = 0;
        while (index < workbook.WorkbookPart.Sheets.Count)
        {
            XlsxWorkbookSheet sheet = workbook.WorkbookPart.Sheets[index];
            if (sheet == null)
            {
                workbook.WorkbookPart.Sheets.RemoveAt(index);
                result.AddRepair("workbookRepair.sheetNullRemoved", "Removed a null workbook sheet entry.");
                continue;
            }

            if (sheet.SheetId == 0 || sheetIds.ContainsKey(sheet.SheetId))
            {
                while (sheetIds.ContainsKey(nextSheetId))
                {
                    nextSheetId = nextSheetId + 1;
                }

                sheet.SheetId = nextSheetId;
                result.AddRepair("workbookRepair.sheetIdFixed", "Assigned a valid unique sheet id.");
            }

            sheetIds[sheet.SheetId] = true;
            if (sheet.SheetId >= nextSheetId)
            {
                nextSheetId = sheet.SheetId + 1;
            }

            if (sheet.Name == null || sheet.Name.Length == 0 || !XlsxWorkbookPartWriter.IsValidSheetName(sheet.Name) || names.ContainsKey(sheet.Name))
            {
                sheet.Name = GetNextSheetName(names);
                result.AddRepair("workbookRepair.sheetNameFixed", "Assigned a valid unique sheet name.");
            }

            names[sheet.Name] = true;
            if (!XlsxWorkbook.IsValidSheetState(sheet.State))
            {
                sheet.State = "";
                result.AddRepair("workbookRepair.sheetStateFixed", "Reset an unsupported sheet visibility state.");
            }

            if (sheet.SheetType == XlsxSheetType.Unknown)
            {
                sheet.SheetType = XlsxSheetType.Worksheet;
                result.AddRepair("workbookRepair.sheetTypeFixed", "Reset an unknown sheet type to worksheet.");
            }

            index = index + 1;
        }

        if (workbook.WorkbookPart.Sheets.Count == 0)
        {
            workbook.WorkbookPart.Sheets.Add(XlsxWorkbook.CreateSheet("Sheet1", 1, "rId1"));
            result.AddRepair("workbookRepair.sheetCreated", "Created a missing workbook sheet.");
        }
    }

    internal static string GetNextSheetName(Dictionary<string, bool> names)
    {
        int index = 1;
        while (true)
        {
            string name = "Sheet" + index.ToString(CultureInfo.InvariantCulture);
            if (!names.ContainsKey(name))
            {
                return name;
            }

            index = index + 1;
        }
    }

    internal static void RepairWorksheetList(XlsxWorkbookRepairResult result, XlsxWorkbook workbook)
    {
        int index = 0;
        while (index < workbook.Worksheets.Count)
        {
            if (workbook.Worksheets[index] == null)
            {
                workbook.Worksheets[index] = new XlsxWorksheetPart();
                result.AddRepair("workbookRepair.worksheetCreated", "Replaced a null worksheet part.");
            }

            index = index + 1;
        }

        while (workbook.Worksheets.Count < workbook.WorkbookPart.Sheets.Count)
        {
            XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
            worksheet.PartPath = GetWorksheetPartPath(workbook.WorkbookPart.Sheets[workbook.Worksheets.Count]);
            workbook.Worksheets.Add(worksheet);
            result.AddRepair("workbookRepair.worksheetCreated", "Created a missing worksheet part.");
        }

        index = 0;
        while (index < workbook.Worksheets.Count)
        {
            XlsxWorksheetPart worksheet = workbook.Worksheets[index];
            if (worksheet.PartPath == null || worksheet.PartPath.Length == 0)
            {
                if (index < workbook.WorkbookPart.Sheets.Count)
                {
                    worksheet.PartPath = GetWorksheetPartPath(workbook.WorkbookPart.Sheets[index]);
                }
                else
                {
                    worksheet.PartPath = "/xl/worksheets/sheet" + (index + 1).ToString(CultureInfo.InvariantCulture) + ".xml";
                }

                result.AddRepair("workbookRepair.worksheetPartPathFixed", "Assigned a worksheet part path.");
            }

            index = index + 1;
        }
    }

    internal static string GetWorksheetPartPath(XlsxWorkbookSheet sheet)
    {
        if (sheet != null && sheet.TargetPath != null && sheet.TargetPath.Length > 0)
        {
            return sheet.TargetPath;
        }

        uint sheetId = 1;
        if (sheet != null && sheet.SheetId > 0)
        {
            sheetId = sheet.SheetId;
        }

        return "/xl/worksheets/sheet" + sheetId.ToString(CultureInfo.InvariantCulture) + ".xml";
    }

    internal static void RepairRelationships(XlsxWorkbookRepairResult result, XlsxWorkbook workbook)
    {
        RepairRelationshipsPart(result, workbook.PackageRelationships, XlsxPackagePaths.PackageRelationships, "/");
        RepairRelationshipsPart(result, workbook.WorkbookRelationships, XlsxPackagePaths.WorkbookRelationships, XlsxPackagePaths.Workbook);
        EnsureOfficeDocumentRelationship(result, workbook.PackageRelationships);
        EnsureWorksheetRelationships(result, workbook);
    }

    internal static void RepairRelationshipsPart(XlsxWorkbookRepairResult result, XlsxRelationshipsPart relationships, string partPath, string sourcePartPath)
    {
        relationships.PartPath = partPath;
        relationships.SourcePartPath = sourcePartPath;
        if (relationships.Relationships == null)
        {
            relationships.Relationships = new List<XlsxRelationship>();
            result.AddRepair("workbookRepair.relationshipListCreated", "Created a missing relationships list.");
        }

        Dictionary<string, bool> ids = new Dictionary<string, bool>(System.StringComparer.Ordinal);
        int index = 0;
        while (index < relationships.Relationships.Count)
        {
            XlsxRelationship relationship = relationships.Relationships[index];
            if (relationship == null)
            {
                relationships.Relationships.RemoveAt(index);
                result.AddRepair("workbookRepair.relationshipNullRemoved", "Removed a null relationship entry.");
                continue;
            }

            if (relationship.Id == null || relationship.Id.Length == 0 || ids.ContainsKey(relationship.Id))
            {
                relationship.Id = GetNextRelationshipId(ids);
                result.AddRepair("workbookRepair.relationshipIdFixed", "Assigned a valid unique relationship id.");
            }

            ids[relationship.Id] = true;
            if (relationship.Type == null)
            {
                relationship.Type = "";
                result.AddRepair("workbookRepair.relationshipTypeFixed", "Normalized a null relationship type.");
            }

            if (relationship.Target == null)
            {
                relationship.Target = "";
                result.AddRepair("workbookRepair.relationshipTargetFixed", "Normalized a null relationship target.");
            }

            index = index + 1;
        }
    }

    internal static string GetNextRelationshipId(Dictionary<string, bool> ids)
    {
        int index = 1;
        while (true)
        {
            string id = "rId" + index.ToString(CultureInfo.InvariantCulture);
            if (!ids.ContainsKey(id))
            {
                return id;
            }

            index = index + 1;
        }
    }

    internal static void EnsureOfficeDocumentRelationship(XlsxWorkbookRepairResult result, XlsxRelationshipsPart relationships)
    {
        XlsxRelationship? relationship = relationships.GetFirstByType(XlsxRelationshipTypes.OfficeDocument);
        if (relationship == null)
        {
            relationship = new XlsxRelationship();
            relationship.Id = GetNextRelationshipId(BuildRelationshipIdMap(relationships));
            relationships.Relationships.Add(relationship);
            result.AddRepair("workbookRepair.officeDocumentRelationshipCreated", "Created a missing package workbook relationship.");
        }

        if (relationship.Type != XlsxRelationshipTypes.OfficeDocument || relationship.Target != "xl/workbook.xml" || relationship.IsExternal || relationship.TargetMode == "External")
        {
            relationship.Type = XlsxRelationshipTypes.OfficeDocument;
            relationship.Target = "xl/workbook.xml";
            relationship.TargetMode = "";
            relationship.IsExternal = false;
            result.AddRepair("workbookRepair.officeDocumentRelationshipFixed", "Repaired the package workbook relationship.");
        }
    }

    internal static void EnsureWorksheetRelationships(XlsxWorkbookRepairResult result, XlsxWorkbook workbook)
    {
        Dictionary<string, bool> sheetRelationshipIds = new Dictionary<string, bool>(System.StringComparer.Ordinal);
        int index = 0;
        while (index < workbook.WorkbookPart.Sheets.Count)
        {
            XlsxWorkbookSheet sheet = workbook.WorkbookPart.Sheets[index];
            if (sheet.RelationshipId == null || sheet.RelationshipId.Length == 0 || sheetRelationshipIds.ContainsKey(sheet.RelationshipId))
            {
                sheet.RelationshipId = GetNextRelationshipId(BuildRelationshipIdMap(workbook.WorkbookRelationships));
                result.AddRepair("workbookRepair.sheetRelationshipIdFixed", "Assigned a valid unique sheet relationship id.");
            }

            sheetRelationshipIds[sheet.RelationshipId] = true;
            XlsxRelationship? relationship = workbook.WorkbookRelationships.GetRelationship(sheet.RelationshipId);
            if (relationship == null)
            {
                relationship = new XlsxRelationship();
                relationship.Id = sheet.RelationshipId;
                workbook.WorkbookRelationships.Relationships.Add(relationship);
                result.AddRepair("workbookRepair.sheetRelationshipCreated", "Created a missing worksheet relationship.");
            }

            string target = GetWorkbookRelativeTarget(GetWorksheetPartForSheet(workbook, index), sheet);
            if (relationship.Type != XlsxRelationshipTypes.Worksheet || relationship.Target != target || relationship.IsExternal || relationship.TargetMode == "External")
            {
                relationship.Type = XlsxRelationshipTypes.Worksheet;
                relationship.Target = target;
                relationship.TargetMode = "";
                relationship.IsExternal = false;
                result.AddRepair("workbookRepair.sheetRelationshipFixed", "Repaired a worksheet relationship.");
            }

            index = index + 1;
        }
    }

    internal static Dictionary<string, bool> BuildRelationshipIdMap(XlsxRelationshipsPart relationships)
    {
        Dictionary<string, bool> ids = new Dictionary<string, bool>(System.StringComparer.Ordinal);
        if (relationships == null || relationships.Relationships == null)
        {
            return ids;
        }

        foreach (XlsxRelationship relationship in relationships.Relationships)
        {
            if (relationship != null && relationship.Id != null && relationship.Id.Length > 0 && !ids.ContainsKey(relationship.Id))
            {
                ids.Add(relationship.Id, true);
            }
        }

        return ids;
    }

    internal static XlsxWorksheetPart? GetWorksheetPartForSheet(XlsxWorkbook workbook, int index)
    {
        if (workbook.Worksheets != null && index >= 0 && index < workbook.Worksheets.Count)
        {
            return workbook.Worksheets[index];
        }

        return null;
    }

    internal static string GetWorkbookRelativeTarget(XlsxWorksheetPart? worksheet, XlsxWorkbookSheet sheet)
    {
        string path = "";
        if (worksheet != null && worksheet.PartPath != null && worksheet.PartPath.Length > 0)
        {
            path = worksheet.PartPath;
        }
        else
        {
            path = GetWorksheetPartPath(sheet);
        }

        if (path.StartsWith("/xl/"))
        {
            return path.Substring(4);
        }

        if (path.StartsWith("/"))
        {
            return path.Substring(1);
        }

        return path;
    }

    internal static void RepairDimensions(XlsxWorkbookRepairResult result, XlsxWorkbook workbook)
    {
        if (workbook.Worksheets == null)
        {
            return;
        }

        foreach (XlsxWorksheetPart worksheet in workbook.Worksheets)
        {
            if (worksheet == null)
            {
                continue;
            }

            string? before = "";
            bool hadDimension = worksheet.HasDimension;
            if (worksheet.Dimension != null)
            {
                before = worksheet.Dimension.Reference;
            }

            XlsxWorksheetUsedRangeResult usedRange = worksheet.ApplyCalculatedDimension();
            AddDiagnostics(result, usedRange.Validation);
            string? after = "";
            if (worksheet.Dimension != null)
            {
                after = worksheet.Dimension.Reference;
            }

            if (usedRange.IsValid && (hadDimension != worksheet.HasDimension || before != after))
            {
                result.AddRepair("workbookRepair.dimensionRecalculated", "Recalculated a worksheet dimension.");
            }
        }
    }

    internal static void RepairSharedStrings(XlsxWorkbookRepairResult result, XlsxWorkbook workbook, XlsxSharedStringTablePart? sharedStrings)
    {
        if (sharedStrings != null)
        {
            RepairSharedStringTable(result, sharedStrings);
        }

        if (workbook.Worksheets == null)
        {
            return;
        }

        foreach (XlsxWorksheetPart worksheet in workbook.Worksheets)
        {
            if (worksheet == null || worksheet.Rows == null)
            {
                continue;
            }

            foreach (XlsxWorksheetRow row in worksheet.Rows)
            {
                if (row == null || row.Cells == null)
                {
                    continue;
                }

                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    RepairSharedStringCell(result, cell, sharedStrings);
                }
            }
        }
    }

    internal static void RepairSharedStringTable(XlsxWorkbookRepairResult result, XlsxSharedStringTablePart sharedStrings)
    {
        if (sharedStrings == null)
        {
            return;
        }

        if (sharedStrings.Items == null)
        {
            sharedStrings.Items = new List<XlsxSharedStringItem>();
            result.AddRepair("workbookRepair.sharedStringItemsCreated", "Created a missing shared string item list.");
        }

        int index = 0;
        while (index < sharedStrings.Items.Count)
        {
            XlsxSharedStringItem item = sharedStrings.Items[index];
            if (item == null)
            {
                sharedStrings.Items.RemoveAt(index);
                result.AddRepair("workbookRepair.sharedStringNullRemoved", "Removed a null shared string item.");
                continue;
            }

            if (item.Text == null)
            {
                item.Text = "";
                result.AddRepair("workbookRepair.sharedStringTextFixed", "Normalized null shared string text.");
            }

            if (item.RichTextRuns == null)
            {
                item.RichTextRuns = new List<XlsxSharedStringRichTextRun>();
                result.AddRepair("workbookRepair.richTextRunsCreated", "Created a missing rich-text run list.");
            }

            index = index + 1;
        }

        sharedStrings.RecalculateCountsFromReferences();
    }

    internal static void RepairSharedStringCell(XlsxWorkbookRepairResult result, XlsxWorksheetCell cell, XlsxSharedStringTablePart? sharedStrings)
    {
        if (cell == null || cell.Type != "s")
        {
            return;
        }

        XlsxXmlValueParseResult parse = XlsxXmlValueParser.ParseUnsignedInteger(cell.Value);
        string text = "";
        bool repair = !parse.IsValid;
        if (parse.IsValid && sharedStrings != null && sharedStrings.Items != null)
        {
            XlsxSharedStringItem? item = sharedStrings.GetItem((int)parse.UnsignedIntValue);
            if (item != null)
            {
                text = item.Text;
            }
            else
            {
                repair = true;
            }
        }
        else if (sharedStrings == null)
        {
            repair = true;
        }

        if (repair)
        {
            cell.Type = "inlineStr";
            cell.Value = "";
            cell.InlineStringText = text;
            cell.InlineString = XlsxInlineString.CreateText(text);
            cell.HasInlineString = true;
            result.AddRepair("workbookRepair.sharedStringCellFixed", "Converted an unusable shared-string reference to an inline string.");
        }
    }

    internal static void RepairStyles(XlsxWorkbookRepairResult result, XlsxWorkbook workbook)
    {
        if (workbook.Styles == null)
        {
            workbook.Styles = new XlsxStylesPart();
            workbook.HasStyles = false;
            result.AddRepair("workbookRepair.stylesCreated", "Created a missing styles part model.");
        }

        XlsxStylesPart styles = workbook.Styles;
        if (styles.NumberFormats == null)
        {
            styles.NumberFormats = new List<XlsxNumberFormatDefinition>();
            result.AddRepair("workbookRepair.stylesListCreated", "Created a missing styles collection.");
        }

        if (styles.Fonts == null)
        {
            styles.Fonts = new List<XlsxRawStyleElement>();
            result.AddRepair("workbookRepair.stylesListCreated", "Created a missing styles collection.");
        }

        if (styles.Fills == null)
        {
            styles.Fills = new List<XlsxRawStyleElement>();
            result.AddRepair("workbookRepair.stylesListCreated", "Created a missing styles collection.");
        }

        if (styles.Borders == null)
        {
            styles.Borders = new List<XlsxRawStyleElement>();
            result.AddRepair("workbookRepair.stylesListCreated", "Created a missing styles collection.");
        }

        if (styles.CellStyleFormats == null)
        {
            styles.CellStyleFormats = new List<XlsxCellFormatDefinition>();
            result.AddRepair("workbookRepair.stylesListCreated", "Created a missing styles collection.");
        }

        if (styles.CellFormats == null)
        {
            styles.CellFormats = new List<XlsxCellFormatDefinition>();
            result.AddRepair("workbookRepair.stylesListCreated", "Created a missing styles collection.");
        }

        if (styles.DifferentialFormats == null)
        {
            styles.DifferentialFormats = new List<XlsxRawStyleElement>();
            result.AddRepair("workbookRepair.stylesListCreated", "Created a missing styles collection.");
        }

        if (styles.UnknownRootAttributes == null)
        {
            styles.UnknownRootAttributes = new List<XlsxXmlAttribute>();
            result.AddRepair("workbookRepair.stylesListCreated", "Created a missing styles collection.");
        }

        if (styles.UnknownXml == null)
        {
            styles.UnknownXml = new List<string>();
            result.AddRepair("workbookRepair.stylesListCreated", "Created a missing styles collection.");
        }

        RemoveNullNumberFormats(result, styles.NumberFormats);
        RemoveNullRawStyles(result, styles.Fonts, "workbookRepair.fontNullRemoved");
        RemoveNullRawStyles(result, styles.Fills, "workbookRepair.fillNullRemoved");
        RemoveNullRawStyles(result, styles.Borders, "workbookRepair.borderNullRemoved");
        RemoveNullCellFormats(result, styles.CellStyleFormats, "workbookRepair.cellStyleFormatNullRemoved");
        RemoveNullCellFormats(result, styles.CellFormats, "workbookRepair.cellFormatNullRemoved");
        RemoveNullRawStyles(result, styles.DifferentialFormats, "workbookRepair.differentialFormatNullRemoved");
        XlsxStyleInternPool.SyncCounts(styles);
        RepairCellStyleIndexes(result, workbook);
    }

    internal static void RemoveNullNumberFormats(XlsxWorkbookRepairResult result, List<XlsxNumberFormatDefinition> items)
    {
        int index = 0;
        while (index < items.Count)
        {
            if (items[index] == null)
            {
                items.RemoveAt(index);
                result.AddRepair("workbookRepair.numberFormatNullRemoved", "Removed a null number format.");
                continue;
            }

            index = index + 1;
        }
    }

    internal static void RemoveNullRawStyles(XlsxWorkbookRepairResult result, List<XlsxRawStyleElement> items, string code)
    {
        int index = 0;
        while (index < items.Count)
        {
            if (items[index] == null)
            {
                items.RemoveAt(index);
                result.AddRepair(code, "Removed a null raw style element.");
                continue;
            }

            index = index + 1;
        }
    }

    internal static void RemoveNullCellFormats(XlsxWorkbookRepairResult result, List<XlsxCellFormatDefinition> items, string code)
    {
        int index = 0;
        while (index < items.Count)
        {
            if (items[index] == null)
            {
                items.RemoveAt(index);
                result.AddRepair(code, "Removed a null cell format.");
                continue;
            }

            index = index + 1;
        }
    }

    internal static void RepairCellStyleIndexes(XlsxWorkbookRepairResult result, XlsxWorkbook workbook)
    {
        if (workbook.Worksheets == null)
        {
            return;
        }

        uint styleCount = 0;
        if (workbook.Styles != null && workbook.Styles.CellFormats != null)
        {
            styleCount = (uint)workbook.Styles.CellFormats.Count;
        }

        foreach (XlsxWorksheetPart worksheet in workbook.Worksheets)
        {
            if (worksheet == null || worksheet.Rows == null)
            {
                continue;
            }

            foreach (XlsxWorksheetRow row in worksheet.Rows)
            {
                if (row == null || row.Cells == null)
                {
                    continue;
                }

                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (cell != null && cell.HasStyle && (styleCount == 0 || cell.Style >= styleCount))
                    {
                        cell.Style = 0;
                        cell.HasStyle = false;
                        result.AddRepair("workbookRepair.cellStyleFixed", "Removed an out-of-range cell style reference.");
                    }
                }
            }
        }
    }

    internal static void RepairWorksheets(XlsxWorkbookRepairResult result, XlsxWorkbook workbook)
    {
        if (workbook.Worksheets == null)
        {
            return;
        }

        foreach (XlsxWorksheetPart worksheet in workbook.Worksheets)
        {
            if (worksheet == null)
            {
                continue;
            }

            RepairWorksheetLists(result, worksheet);
            RepairWorksheetRows(result, worksheet);
        }
    }

    internal static void RepairWorksheetLists(XlsxWorkbookRepairResult result, XlsxWorksheetPart worksheet)
    {
        if (worksheet.Dimension == null)
        {
            worksheet.Dimension = new XlsxWorksheetDimension();
            worksheet.HasDimension = false;
            result.AddRepair("workbookRepair.dimensionCreated", "Created a missing worksheet dimension model.");
        }

        if (worksheet.Rows == null)
        {
            worksheet.Rows = new List<XlsxWorksheetRow>();
            result.AddRepair("workbookRepair.rowsCreated", "Created a missing worksheet row list.");
        }

        if (worksheet.SheetViews == null)
        {
            worksheet.SheetViews = new List<XlsxWorksheetView>();
            result.AddRepair("workbookRepair.worksheetListCreated", "Created a missing worksheet collection.");
        }

        if (worksheet.Columns == null)
        {
            worksheet.Columns = new List<XlsxWorksheetColumn>();
            result.AddRepair("workbookRepair.worksheetListCreated", "Created a missing worksheet collection.");
        }

        if (worksheet.MergedCells == null)
        {
            worksheet.MergedCells = new List<XlsxWorksheetMergeCell>();
            result.AddRepair("workbookRepair.worksheetListCreated", "Created a missing worksheet collection.");
        }

        if (worksheet.Hyperlinks == null)
        {
            worksheet.Hyperlinks = new List<XlsxWorksheetHyperlink>();
            result.AddRepair("workbookRepair.worksheetListCreated", "Created a missing worksheet collection.");
        }

        if (worksheet.TableReferences == null)
        {
            worksheet.TableReferences = new List<XlsxWorksheetTableReference>();
            result.AddRepair("workbookRepair.worksheetListCreated", "Created a missing worksheet collection.");
        }

        if (worksheet.PivotTableReferences == null)
        {
            worksheet.PivotTableReferences = new List<XlsxWorksheetPivotTableReference>();
            result.AddRepair("workbookRepair.worksheetListCreated", "Created a missing worksheet collection.");
        }

        if (worksheet.Tables == null)
        {
            worksheet.Tables = new List<XlsxTablePart>();
            result.AddRepair("workbookRepair.worksheetListCreated", "Created a missing worksheet collection.");
        }

        if (worksheet.ConditionalFormattings == null)
        {
            worksheet.ConditionalFormattings = new List<XlsxConditionalFormatting>();
            result.AddRepair("workbookRepair.worksheetListCreated", "Created a missing worksheet collection.");
        }

        if (worksheet.SparklineGroups == null)
        {
            worksheet.SparklineGroups = new List<XlsxSparklineGroup>();
            result.AddRepair("workbookRepair.worksheetListCreated", "Created a missing worksheet collection.");
        }

        if (worksheet.UnknownRootAttributes == null)
        {
            worksheet.UnknownRootAttributes = new List<XlsxXmlAttribute>();
            result.AddRepair("workbookRepair.worksheetListCreated", "Created a missing worksheet collection.");
        }

        if (worksheet.UnknownXml == null)
        {
            worksheet.UnknownXml = new List<string>();
            result.AddRepair("workbookRepair.worksheetListCreated", "Created a missing worksheet collection.");
        }
    }

    internal static void RepairWorksheetRows(XlsxWorkbookRepairResult result, XlsxWorksheetPart worksheet)
    {
        int rowIndex = 0;
        while (rowIndex < worksheet.Rows.Count)
        {
            XlsxWorksheetRow row = worksheet.Rows[rowIndex];
            if (row == null)
            {
                worksheet.Rows.RemoveAt(rowIndex);
                result.AddRepair("workbookRepair.rowNullRemoved", "Removed a null worksheet row.");
                continue;
            }

            if (row.Cells == null)
            {
                row.Cells = new List<XlsxWorksheetCell>();
                result.AddRepair("workbookRepair.cellsCreated", "Created a missing worksheet cell list.");
            }

            if (row.UnknownAttributes == null)
            {
                row.UnknownAttributes = new List<XlsxXmlAttribute>();
                result.AddRepair("workbookRepair.rowUnknownAttributesCreated", "Created missing row unknown attributes.");
            }

            int cellIndex = 0;
            while (cellIndex < row.Cells.Count)
            {
                XlsxWorksheetCell cell = row.Cells[cellIndex];
                if (cell == null)
                {
                    row.Cells.RemoveAt(cellIndex);
                    result.AddRepair("workbookRepair.cellNullRemoved", "Removed a null worksheet cell.");
                    continue;
                }

                RepairCell(result, cell);
                cellIndex = cellIndex + 1;
            }

            rowIndex = rowIndex + 1;
        }
    }

    internal static void RepairCell(XlsxWorkbookRepairResult result, XlsxWorksheetCell cell)
    {
        if (cell.Reference != null && cell.Reference.Length > 0)
        {
            XlsxA1CellReferenceResult reference = XlsxA1CellReferenceConverter.Parse(cell.Reference);
            if (!reference.IsValid)
            {
                cell.Reference = "";
                result.AddRepair("workbookRepair.cellReferenceFixed", "Removed an invalid cell reference.");
            }
        }

        if (cell.Type == null)
        {
            cell.Type = "";
            result.AddRepair("workbookRepair.cellTypeFixed", "Normalized a null cell type.");
        }

        if (cell.Value == null)
        {
            cell.Value = "";
            result.AddRepair("workbookRepair.cellValueFixed", "Normalized a null cell value.");
        }

        if (cell.Formula == null)
        {
            cell.Formula = "";
            result.AddRepair("workbookRepair.cellFormulaFixed", "Normalized a null cell formula.");
        }

        if (cell.FormulaInfo == null)
        {
            cell.FormulaInfo = new XlsxCellFormula();
            cell.HasFormula = false;
            result.AddRepair("workbookRepair.cellFormulaInfoCreated", "Created missing cell formula metadata.");
        }

        if (cell.InlineStringText == null)
        {
            cell.InlineStringText = "";
            result.AddRepair("workbookRepair.inlineStringTextFixed", "Normalized null inline string text.");
        }

        if (cell.InlineString == null)
        {
            cell.InlineString = XlsxInlineString.CreateText(cell.InlineStringText);
            cell.HasInlineString = cell.Type == "inlineStr";
            result.AddRepair("workbookRepair.inlineStringCreated", "Created missing inline string metadata.");
        }

        if (cell.UnknownAttributes == null)
        {
            cell.UnknownAttributes = new List<XlsxXmlAttribute>();
            result.AddRepair("workbookRepair.cellUnknownAttributesCreated", "Created missing cell unknown attributes.");
        }

        if (cell.UnknownXml == null)
        {
            cell.UnknownXml = new List<string>();
            result.AddRepair("workbookRepair.cellUnknownXmlCreated", "Created missing cell unknown XML.");
        }
    }

    internal static void AddRepairResult(XlsxWorkbookRepairResult result, XlsxWorkbookRepairResult? source)
    {
        if (source == null)
        {
            return;
        }

        foreach (string repair in source.Repairs)
        {
            result.Repairs.Add(repair);
            result.RepairCount = result.RepairCount + 1;
        }

        AddDiagnostics(result, source.Validation);
    }

    internal static void AddDiagnostics(XlsxWorkbookRepairResult result, XlsxValidationResult source)
    {
        if (source == null)
        {
            return;
        }

        foreach (XlsxDiagnostic diagnostic in source.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddDiagnostics(XlsxWorkbookRepairResult result, XlsxWorksheetUsedRangeResult source)
    {
        if (source == null)
        {
            return;
        }

        AddDiagnostics(result, source.Validation);
    }
}
