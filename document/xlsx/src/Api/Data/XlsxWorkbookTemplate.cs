using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook template data used by the public workbook API.
/// </summary>
public class XlsxWorkbookTemplate
{
    /// <summary>
    /// Stores the workbook value used by this model.
    /// </summary>
    public XlsxWorkbook Workbook;
    /// <summary>
    /// Initializes a new workbook template with its default state.
    /// </summary>
    public XlsxWorkbookTemplate()
    {
        Workbook = new XlsxWorkbook();
    }

    /// <summary>
    /// Opens and validates an OOXML workbook package from an in-memory byte array.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookTemplateOpenResult OpenBytes(byte[] bytes)
    {
        XlsxWorkbookTemplateOpenResult result = new XlsxWorkbookTemplateOpenResult();
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.PreserveUnknownParts = true;
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(bytes, options);
        CopyOpenDiagnostics(result, openResult.Validation);
        if (!result.IsValid || openResult.Workbook == null)
        {
            return result;
        }

        XlsxWorkbookTemplate template = new XlsxWorkbookTemplate();
        template.Workbook = openResult.Workbook;
        result.Template = template;
        return result;
    }

    /// <summary>
    /// Opens and validates an OOXML workbook package from a file path.
    /// </summary>
    /// <param name = "filePath">The file-system path to read or write.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal static XlsxWorkbookTemplateOpenResult OpenFile(string filePath)
    {
        byte[] bytes = File.ReadAllBytes(filePath);
        return OpenBytes(bytes);
    }

    /// <summary>
    /// Sets named range and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookTemplateEditResult SetNamedRange(string name, string reference)
    {
        XlsxWorkbookTemplateEditResult result = ValidateDefinedNameEdit(name, reference);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxWorkbookDefinedName definedName = GetOrCreateDefinedName(name, false, 0);
        definedName.Text = reference;
        return result;
    }

    /// <summary>
    /// Sets local named range and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookTemplateEditResult SetLocalNamedRange(string name, int localSheetId, string reference)
    {
        XlsxWorkbookTemplateEditResult result = ValidateDefinedNameEdit(name, reference);
        if (!result.IsValid)
        {
            return result;
        }

        if (localSheetId < 0)
        {
            result.AddError("template.namedRange.localSheetIdInvalid", "Local sheet id must not be negative.");
            return result;
        }

        XlsxWorkbookDefinedName definedName = GetOrCreateDefinedName(name, true, localSheetId);
        definedName.Text = reference;
        return result;
    }

    /// <summary>
    /// Performs the resize table operation using the current model state.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookTemplateEditResult ResizeTable(string tableName, string reference)
    {
        XlsxWorkbookTemplateEditResult result = new XlsxWorkbookTemplateEditResult();
        string name = XlsxCellValueConverter.NormalizeText(tableName);
        string rangeReference = XlsxCellValueConverter.NormalizeText(reference);
        if (name.Length == 0)
        {
            result.AddError("template.table.nameMissing", "Template table name must not be empty.");
            return result;
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(rangeReference);
        CopyRangeDiagnostics(result, range.Validation);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxUnknownPart? tableUnknownPart = FindTableUnknownPart(name, result);
        if (tableUnknownPart == null)
        {
            result.AddError("template.table.notFound", "Template table was not found.");
            return result;
        }

        XlsxTablePart table = new XlsxTablePartReader().ReadBytes(tableUnknownPart.Data);
        table.PartPath = tableUnknownPart.Path;
        CopyTableDiagnostics(result, table.Validation);
        if (!result.IsValid)
        {
            return result;
        }

        table.Reference = rangeReference;
        table.RawXml = "";
        XlsxTablePartWriteResult writeResult = new XlsxTablePartWriter().WriteBytes(table);
        CopyTableWriteDiagnostics(result, writeResult.Validation);
        if (!result.IsValid)
        {
            return result;
        }

        tableUnknownPart.Data = writeResult.Bytes;
        tableUnknownPart.ContentType = XlsxContentTypes.Table;
        return result;
    }

    /// <summary>
    /// Performs the save as bytes operation using the current model state.
    /// </summary>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookSaveResult SaveAsBytes()
    {
        XlsxWriterOptions options = new XlsxWriterOptions();
        options.PackageVariant = XlsxPackageVariant.Xlsx;
        XlsxPackageVariant originalVariant = Workbook.PackageVariant;
        Workbook.PackageVariant = XlsxPackageVariant.Xlsx;
        XlsxWorkbookSaveResult result = Workbook.SaveBytesWithOptions(options);
        Workbook.PackageVariant = originalVariant;
        return result;
    }

    /// <summary>
    /// Performs the save as file operation using the current model state.
    /// </summary>
    /// <param name = "filePath">The file-system path to read or write.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookSaveResult SaveAsFile(string filePath)
    {
        XlsxWorkbookSaveResult result = SaveAsBytes();
        if (result.IsValid)
        {
            File.WriteAllBytes(filePath, result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Returns or create defined name from the current model.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "local">The local value used by the operation.</param>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The workbook defined name produced by the operation.</returns>
    internal XlsxWorkbookDefinedName GetOrCreateDefinedName(string name, bool local, int localSheetId)
    {
        foreach (XlsxWorkbookDefinedName definedName in Workbook.WorkbookPart.DefinedNames)
        {
            bool sameName = definedName.Name == name;
            bool sameScope = definedName.HasLocalSheetId == local;
            if (local)
            {
                sameScope = sameScope && definedName.LocalSheetId == localSheetId;
            }

            if (sameName && sameScope)
            {
                return definedName;
            }
        }

        XlsxWorkbookDefinedName created = new XlsxWorkbookDefinedName();
        created.Name = name;
        created.HasLocalSheetId = local;
        created.LocalSheetId = localSheetId;
        Workbook.WorkbookPart.DefinedNames.Add(created);
        return created;
    }

    /// <summary>
    /// Performs the find table unknown part operation using the current model state.
    /// </summary>
    /// <param name = "tableName">The table name value used by the operation.</param>
    /// <param name = "result">The result value used by the operation.</param>
    /// <returns>The unknown part produced by the operation.</returns>
    internal XlsxUnknownPart? FindTableUnknownPart(string tableName, XlsxWorkbookTemplateEditResult result)
    {
        if (Workbook == null || Workbook.UnknownParts == null || Workbook.UnknownParts.Parts == null)
        {
            return null;
        }

        foreach (XlsxUnknownPart part in Workbook.UnknownParts.Parts)
        {
            if (!IsTablePart(part))
            {
                continue;
            }

            XlsxTablePart table = new XlsxTablePartReader().ReadBytes(part.Data);
            table.PartPath = part.Path;
            CopyTableDiagnostics(result, table.Validation);
            if (table.Name == tableName || table.DisplayName == tableName)
            {
                return part;
            }
        }

        return null;
    }

    internal static bool IsTablePart(XlsxUnknownPart part)
    {
        if (part == null || part.Path == null)
        {
            return false;
        }

        if (part.ContentType == XlsxContentTypes.Table)
        {
            return true;
        }

        return part.Path.StartsWith(XlsxPackagePaths.TablesDirectory, System.StringComparison.Ordinal) && part.Path.EndsWith(".xml", System.StringComparison.Ordinal);
    }

    internal static XlsxWorkbookTemplateEditResult ValidateDefinedNameEdit(string name, string reference)
    {
        XlsxWorkbookTemplateEditResult result = new XlsxWorkbookTemplateEditResult();
        if (name == null || name.Length == 0)
        {
            result.AddError("template.namedRange.nameMissing", "Template named range name must not be empty.");
        }

        if (reference == null || reference.Length == 0)
        {
            result.AddError("template.namedRange.referenceMissing", "Template named range reference must not be empty.");
        }

        return result;
    }

    internal static void CopyOpenDiagnostics(XlsxWorkbookTemplateOpenResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyRangeDiagnostics(XlsxWorkbookTemplateEditResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyTableDiagnostics(XlsxWorkbookTemplateEditResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyTableWriteDiagnostics(XlsxWorkbookTemplateEditResult target, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }
}
