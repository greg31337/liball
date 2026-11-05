using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML workbook part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxWorkbookPart
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
    /// Stores the markup compatibility value used by this model.
    /// </summary>
    public XlsxMarkupCompatibilityAttributes MarkupCompatibility;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains sheets.
    /// </summary>
    public List<XlsxWorkbookSheet> Sheets;
    /// <summary>
    /// Contains workbook views.
    /// </summary>
    public List<XlsxWorkbookView> WorkbookViews;
    /// <summary>
    /// Contains defined names.
    /// </summary>
    public List<XlsxWorkbookDefinedName> DefinedNames;
    /// <summary>
    /// Contains pivot caches.
    /// </summary>
    public List<XlsxWorkbookPivotCacheReference> PivotCaches;
    /// <summary>
    /// Contains external references.
    /// </summary>
    public List<XlsxWorkbookExternalReference> ExternalReferences;
    /// <summary>
    /// Contains calculation properties.
    /// </summary>
    public XlsxWorkbookCalculationProperties CalculationProperties;
    /// <summary>
    /// Indicates whether calculation properties is present and should be written.
    /// </summary>
    public bool HasCalculationProperties;
    /// <summary>
    /// Stores the workbook protection value used by this model.
    /// </summary>
    public XlsxWorkbookProtection? WorkbookProtection;
    /// <summary>
    /// Indicates whether workbook protection is present and should be written.
    /// </summary>
    public bool HasWorkbookProtection;
    /// <summary>
    /// Stores the extension list value used by this model.
    /// </summary>
    public XlsxExtensionList ExtensionList;
    /// <summary>
    /// Indicates whether extension list is present and should be written.
    /// </summary>
    public bool HasExtensionList;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new workbook part with its default state.
    /// </summary>
    public XlsxWorkbookPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = XlsxPackagePaths.Workbook;
        NamespaceUri = XlsxNamespaces.Spreadsheet;
        MarkupCompatibility = new XlsxMarkupCompatibilityAttributes();
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        Sheets = new List<XlsxWorkbookSheet>();
        WorkbookViews = new List<XlsxWorkbookView>();
        DefinedNames = new List<XlsxWorkbookDefinedName>();
        PivotCaches = new List<XlsxWorkbookPivotCacheReference>();
        ExternalReferences = new List<XlsxWorkbookExternalReference>();
        CalculationProperties = new XlsxWorkbookCalculationProperties();
        HasCalculationProperties = false;
        WorkbookProtection = new XlsxWorkbookProtection();
        HasWorkbookProtection = false;
        ExtensionList = new XlsxExtensionList();
        HasExtensionList = false;
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Returns sheet by name from the current model.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <returns>The workbook sheet produced by the operation.</returns>
    internal XlsxWorkbookSheet? GetSheetByName(string name)
    {
        foreach (XlsxWorkbookSheet sheet in Sheets)
        {
            if (sheet.Name == name)
            {
                return sheet;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns defined name from the current model.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <returns>The workbook defined name produced by the operation.</returns>
    internal XlsxWorkbookDefinedName? GetDefinedName(string name)
    {
        foreach (XlsxWorkbookDefinedName definedName in DefinedNames)
        {
            if (definedName.Name == name)
            {
                return definedName;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns defined name from the current model.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The workbook defined name produced by the operation.</returns>
    internal XlsxWorkbookDefinedName? GetDefinedNameForSheet(string name, int localSheetId)
    {
        XlsxWorkbookDefinedName? local = GetLocalDefinedName(name, localSheetId);
        if (local != null)
        {
            return local;
        }

        return GetGlobalDefinedName(name);
    }

    /// <summary>
    /// Returns global defined name from the current model.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <returns>The workbook defined name produced by the operation.</returns>
    internal XlsxWorkbookDefinedName? GetGlobalDefinedName(string name)
    {
        foreach (XlsxWorkbookDefinedName definedName in DefinedNames)
        {
            if (definedName != null && definedName.Name == name && !definedName.HasLocalSheetId)
            {
                return definedName;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns local defined name from the current model.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The workbook defined name produced by the operation.</returns>
    internal XlsxWorkbookDefinedName? GetLocalDefinedName(string name, int localSheetId)
    {
        foreach (XlsxWorkbookDefinedName definedName in DefinedNames)
        {
            if (definedName != null && definedName.Name == name && definedName.HasLocalSheetId && definedName.LocalSheetId == localSheetId)
            {
                return definedName;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns print area from the current model.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The workbook defined name produced by the operation.</returns>
    internal XlsxWorkbookDefinedName? GetPrintArea(int localSheetId)
    {
        return GetLocalDefinedName("_xlnm.Print_Area", localSheetId);
    }

    /// <summary>
    /// Returns print titles from the current model.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>The workbook defined name produced by the operation.</returns>
    internal XlsxWorkbookDefinedName? GetPrintTitles(int localSheetId)
    {
        return GetLocalDefinedName("_xlnm.Print_Titles", localSheetId);
    }

    /// <summary>
    /// Sets print area and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <param name = "sheetName">The sheet name value used by the operation.</param>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookPrintDefinitionResult SetPrintArea(int localSheetId, string sheetName, string reference)
    {
        XlsxWorkbookPrintDefinitionResult result = ValidatePrintDefinition(localSheetId, sheetName, reference, "workbook.printAreaReferenceMissing");
        if (!result.IsValid)
        {
            return result;
        }

        XlsxWorkbookDefinedName definedName = GetOrCreateLocalDefinedName("_xlnm.Print_Area", localSheetId, result);
        definedName.Text = BuildSheetQualifiedReference(sheetName, reference);
        definedName.Hidden = true;
        definedName.HasHidden = true;
        result.DefinedName = definedName;
        result.HasDefinedName = true;
        return result;
    }

    /// <summary>
    /// Sets print titles and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <param name = "sheetName">The sheet name value used by the operation.</param>
    /// <param name = "rowReference">An A1-style cell or range reference.</param>
    /// <param name = "columnReference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookPrintDefinitionResult SetPrintTitles(int localSheetId, string sheetName, string rowReference, string columnReference)
    {
        XlsxWorkbookPrintDefinitionResult result = ValidatePrintTitles(localSheetId, sheetName, rowReference, columnReference);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxWorkbookDefinedName definedName = GetOrCreateLocalDefinedName("_xlnm.Print_Titles", localSheetId, result);
        definedName.Text = BuildPrintTitlesText(sheetName, rowReference, columnReference);
        definedName.Hidden = true;
        definedName.HasHidden = true;
        result.DefinedName = definedName;
        result.HasDefinedName = true;
        return result;
    }

    /// <summary>
    /// Clears print area while retaining the surrounding model object.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookPrintDefinitionResult ClearPrintArea(int localSheetId)
    {
        return ClearLocalDefinedName("_xlnm.Print_Area", localSheetId);
    }

    /// <summary>
    /// Clears print titles while retaining the surrounding model object.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookPrintDefinitionResult ClearPrintTitles(int localSheetId)
    {
        return ClearLocalDefinedName("_xlnm.Print_Titles", localSheetId);
    }

    /// <summary>
    /// Performs the update defined name references for row insert operation using the current model state.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <param name = "startRow">The start row value used by the operation.</param>
    /// <param name = "rowCount">The row count value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferencesForRowInsert(int localSheetId, int startRow, int rowCount)
    {
        return UpdateDefinedNameReferences(localSheetId, XlsxFormulaReferenceUpdateKind.RowInsert, startRow, 0, rowCount);
    }

    /// <summary>
    /// Performs the update defined name references for row delete operation using the current model state.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <param name = "startRow">The start row value used by the operation.</param>
    /// <param name = "rowCount">The row count value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferencesForRowDelete(int localSheetId, int startRow, int rowCount)
    {
        return UpdateDefinedNameReferences(localSheetId, XlsxFormulaReferenceUpdateKind.RowDelete, startRow, 0, rowCount);
    }

    /// <summary>
    /// Performs the update defined name references for column insert operation using the current model state.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <param name = "startColumn">The start column value used by the operation.</param>
    /// <param name = "columnCount">The column count value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferencesForColumnInsert(int localSheetId, int startColumn, int columnCount)
    {
        return UpdateDefinedNameReferences(localSheetId, XlsxFormulaReferenceUpdateKind.ColumnInsert, 0, startColumn, columnCount);
    }

    /// <summary>
    /// Performs the update defined name references for column delete operation using the current model state.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <param name = "startColumn">The start column value used by the operation.</param>
    /// <param name = "columnCount">The column count value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferencesForColumnDelete(int localSheetId, int startColumn, int columnCount)
    {
        return UpdateDefinedNameReferences(localSheetId, XlsxFormulaReferenceUpdateKind.ColumnDelete, 0, startColumn, columnCount);
    }

    /// <summary>
    /// Performs the update defined name references operation using the current model state.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <param name = "kind">The kind value used by the operation.</param>
    /// <param name = "startRow">The start row value used by the operation.</param>
    /// <param name = "startColumn">The start column value used by the operation.</param>
    /// <param name = "count">The count value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxFormulaReferenceUpdateResult UpdateDefinedNameReferences(int localSheetId, XlsxFormulaReferenceUpdateKind kind, int startRow, int startColumn, int count)
    {
        XlsxFormulaReferenceUpdateResult result = new XlsxFormulaReferenceUpdateResult();
        if (DefinedNames == null)
        {
            result.AddError("workbook.definedNamesNull", "Workbook defined names list must not be null.");
            return result;
        }

        if (localSheetId < 0)
        {
            result.AddError("workbook.definedNameLocalSheetIdRange", "Defined name local sheet id must not be negative.");
            return result;
        }

        foreach (XlsxWorkbookDefinedName definedName in DefinedNames)
        {
            if (definedName != null && definedName.HasLocalSheetId && definedName.LocalSheetId == localSheetId && definedName.Text != null && definedName.Text.Length > 0)
            {
                XlsxFormulaReferenceUpdateResult update = XlsxWorksheetPart.UpdateFormulaReferences(definedName.Text, kind, startRow, startColumn, count, 0, 0, 0, 0, 0, 0, "", "");
                XlsxWorksheetPart.CopyFormulaReferenceUpdateDiagnostics(result, update);
                result.UpdatedReferenceCount = result.UpdatedReferenceCount + update.UpdatedReferenceCount;
                result.DeletedReferenceCount = result.DeletedReferenceCount + update.DeletedReferenceCount;
                result.RenamedSheetCount = result.RenamedSheetCount + update.RenamedSheetCount;
                if (update.IsValid)
                {
                    definedName.Text = update.Formula;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Clears local defined name while retaining the surrounding model object.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookPrintDefinitionResult ClearLocalDefinedName(string name, int localSheetId)
    {
        XlsxWorkbookPrintDefinitionResult result = ValidateLocalDefinedNameAccess(localSheetId);
        if (!result.IsValid)
        {
            return result;
        }

        int index = 0;
        while (index < DefinedNames.Count)
        {
            XlsxWorkbookDefinedName definedName = DefinedNames[index];
            if (definedName != null && definedName.Name == name && definedName.HasLocalSheetId && definedName.LocalSheetId == localSheetId)
            {
                result.DefinedName = definedName;
                result.HasDefinedName = true;
                result.RemovedDefinedName = true;
                DefinedNames.RemoveAt(index);
                return result;
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Returns or create local defined name from the current model.
    /// </summary>
    /// <param name = "name">The name value used by the operation.</param>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <param name = "result">The result value used by the operation.</param>
    /// <returns>The workbook defined name produced by the operation.</returns>
    internal XlsxWorkbookDefinedName GetOrCreateLocalDefinedName(string name, int localSheetId, XlsxWorkbookPrintDefinitionResult result)
    {
        foreach (XlsxWorkbookDefinedName definedName in DefinedNames)
        {
            if (definedName != null && definedName.Name == name && definedName.HasLocalSheetId && definedName.LocalSheetId == localSheetId)
            {
                return definedName;
            }
        }

        XlsxWorkbookDefinedName created = new XlsxWorkbookDefinedName();
        created.Name = name;
        created.LocalSheetId = localSheetId;
        created.HasLocalSheetId = true;
        DefinedNames.Add(created);
        result.CreatedDefinedName = true;
        return created;
    }

    /// <summary>
    /// Checks local defined name access and returns diagnostics rather than throwing for expected invalid input.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookPrintDefinitionResult ValidateLocalDefinedNameAccess(int localSheetId)
    {
        XlsxWorkbookPrintDefinitionResult result = new XlsxWorkbookPrintDefinitionResult();
        if (DefinedNames == null)
        {
            result.AddError("workbook.definedNamesNull", "Workbook defined names list must not be null.");
            return result;
        }

        if (localSheetId < 0)
        {
            result.AddError("workbook.printDefinitionLocalSheetIdRange", "Print definition local sheet id must not be negative.");
        }

        return result;
    }

    /// <summary>
    /// Checks print definition and returns diagnostics rather than throwing for expected invalid input.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <param name = "sheetName">The sheet name value used by the operation.</param>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <param name = "missingReferenceCode">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookPrintDefinitionResult ValidatePrintDefinition(int localSheetId, string sheetName, string reference, string missingReferenceCode)
    {
        XlsxWorkbookPrintDefinitionResult result = ValidateLocalDefinedNameAccess(localSheetId);
        if (sheetName == null || sheetName.Length == 0)
        {
            result.AddError("workbook.printDefinitionSheetNameMissing", "Print definition sheet name must not be empty.");
        }

        if (reference == null || reference.Length == 0)
        {
            result.AddError(missingReferenceCode, "Print definition reference must not be empty.");
        }

        return result;
    }

    /// <summary>
    /// Checks print titles and returns diagnostics rather than throwing for expected invalid input.
    /// </summary>
    /// <param name = "localSheetId">The local sheet id value used by the operation.</param>
    /// <param name = "sheetName">The sheet name value used by the operation.</param>
    /// <param name = "rowReference">An A1-style cell or range reference.</param>
    /// <param name = "columnReference">An A1-style cell or range reference.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxWorkbookPrintDefinitionResult ValidatePrintTitles(int localSheetId, string sheetName, string rowReference, string columnReference)
    {
        XlsxWorkbookPrintDefinitionResult result = ValidatePrintDefinition(localSheetId, sheetName, "titles", "workbook.printTitlesReferenceMissing");
        if (!result.IsValid)
        {
            return result;
        }

        bool hasRows = rowReference != null && rowReference.Length > 0;
        bool hasColumns = columnReference != null && columnReference.Length > 0;
        if (!hasRows && !hasColumns)
        {
            result.AddError("workbook.printTitlesReferenceMissing", "Print titles require row titles, column titles, or both.");
        }

        return result;
    }

    internal static string BuildPrintTitlesText(string sheetName, string rowReference, string columnReference)
    {
        string rows = "";
        string columns = "";
        if (rowReference != null && rowReference.Length > 0)
        {
            rows = BuildSheetQualifiedReference(sheetName, rowReference);
        }

        if (columnReference != null && columnReference.Length > 0)
        {
            columns = BuildSheetQualifiedReference(sheetName, columnReference);
        }

        if (rows.Length > 0 && columns.Length > 0)
        {
            return rows + "," + columns;
        }

        if (rows.Length > 0)
        {
            return rows;
        }

        return columns;
    }

    internal static string BuildSheetQualifiedReference(string sheetName, string reference)
    {
        if (reference != null && reference.IndexOf('!') >= 0)
        {
            return reference;
        }

        return QuoteSheetNameForFormula(sheetName) + "!" + reference;
    }

    internal static string QuoteSheetNameForFormula(string sheetName)
    {
        if (IsSimpleFormulaSheetName(sheetName))
        {
            return sheetName;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("'");
        foreach (char character in sheetName)
        {
            if (character == '\'')
            {
                builder.Append("''");
            }
            else
            {
                builder.Append(character);
            }
        }

        builder.Append("'");
        return builder.ToString();
    }

    internal static bool IsSimpleFormulaSheetName(string sheetName)
    {
        if (sheetName == null || sheetName.Length == 0)
        {
            return false;
        }

        foreach (char character in sheetName)
        {
            bool letter = (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
            bool digit = character >= '0' && character <= '9';
            if (!letter && !digit && character != '_')
            {
                return false;
            }
        }

        return true;
    }
}
