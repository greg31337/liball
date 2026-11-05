using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Validates and writes workbook part data to supported byte arrays, streams, files, or XML text.
/// </summary>
/// <remarks>
/// Writers validate required fields before producing output. Caller-provided output streams remain open unless the API returns a streaming session whose <c>Close</c> method completes the document.
/// </remarks>
public class XlsxWorkbookPartWriter : IXlsxWorkbookPartWriter
{
    /// <summary>
    /// Initializes a new workbook part writer with its default state.
    /// </summary>
    public XlsxWorkbookPartWriter()
    {
    }

    /// <summary>
    /// Validates the input model and returns its serialized text representation.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxWorkbookPartWriteResult WriteString(XlsxWorkbookPart part)
    {
        XlsxWorkbookPartWriteResult result = WriteBytes(part);
        if (result.IsValid)
        {
            result.Xml = Encoding.UTF8.GetString(result.Bytes);
        }

        return result;
    }

    /// <summary>
    /// Validates the input model and returns its serialized UTF-8 bytes.
    /// </summary>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxWorkbookPartWriteResult WriteBytes(XlsxWorkbookPart part)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XlsxWorkbookPartWriteResult result = WriteStream(stream, part);
            if (result.IsValid)
            {
                result.Bytes = stream.ToArray();
                result.Xml = Encoding.UTF8.GetString(result.Bytes);
            }

            return result;
        }
    }

    /// <summary>
    /// Validates the input model and writes its serialized representation to the supplied stream.
    /// </summary>
    /// <param name = "stream">The writable output stream. The caller retains ownership unless a returned streaming session is explicitly closed.</param>
    /// <param name = "part">The typed part model to read, validate, or serialize.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    public XlsxWorkbookPartWriteResult WriteStream(Stream stream, XlsxWorkbookPart part)
    {
        XlsxWorkbookPartWriteResult result = ValidateMinimumWorkbookPart(part);
        if (!result.IsValid)
        {
            return result;
        }

        using (XmlWriter writer = XmlWriter.Create(stream, XlsxXmlWriterHelpers.CreateStableWriterSettings()))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("", "workbook", XlsxWorksheetPart.NormalizeSpreadsheetNamespace(part.NamespaceUri));
            writer.WriteAttributeString("xmlns", "r", "http://www.w3.org/2000/xmlns/", GetOutputRelationshipNamespace(part.NamespaceUri));
            WriteWorkbookNamespaceDeclarations(writer, part);
            XlsxXmlNamespaceMap map = XlsxXmlWriterHelpers.CreateOpenXmlNamespaceMap();
            XlsxMarkupCompatibility.WriteAttributes(writer, map, part.MarkupCompatibility);
            XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.UnknownRootAttributes);
            WriteBookViews(writer, part);
            WriteWorkbookProtection(writer, part);
            writer.WriteStartElement("", "sheets", GetOutputSpreadsheetNamespace(writer));
            foreach (XlsxWorkbookSheet sheet in part.Sheets)
            {
                WriteSheet(writer, sheet);
            }

            writer.WriteEndElement();
            WriteDefinedNames(writer, part);
            WriteExternalReferences(writer, part);
            WritePivotCaches(writer, part);
            WriteCalculationProperties(writer, part);
            WriteExtensionList(writer, part);
            foreach (string unknownXml in part.UnknownXml)
            {
                if (unknownXml != null && unknownXml.Length > 0)
                {
                    writer.WriteRaw(unknownXml);
                }
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return result;
    }

    internal static void WriteWorkbookNamespaceDeclarations(XmlWriter writer, XlsxWorkbookPart part)
    {
        if (part.MarkupCompatibility == null)
        {
            return;
        }

        List<XlsxXmlNamespace> namespaces = new List<XlsxXmlNamespace>();
        foreach (XlsxXmlNamespace namespaceDeclaration in part.MarkupCompatibility.NamespaceDeclarations)
        {
            if (namespaceDeclaration.Prefix == null || namespaceDeclaration.Prefix.Length == 0 || namespaceDeclaration.Prefix == "r")
            {
                continue;
            }

            namespaces.Add(namespaceDeclaration);
        }

        namespaces.Sort(XlsxXmlWriterHelpers.CompareNamespacesByPrefix);
        foreach (XlsxXmlNamespace namespaceDeclaration in namespaces)
        {
            writer.WriteAttributeString("xmlns", namespaceDeclaration.Prefix, "http://www.w3.org/2000/xmlns/", namespaceDeclaration.NamespaceUri);
        }
    }

    internal static string GetOutputSpreadsheetNamespace(XmlWriter writer)
    {
        string? strictPrefix = writer.LookupPrefix(XlsxNamespaces.StrictSpreadsheet);
        if (strictPrefix != null) { return XlsxNamespaces.StrictSpreadsheet; }
        return XlsxNamespaces.Spreadsheet;
    }

    internal static string GetOutputRelationshipNamespace(string? spreadsheetNamespace)
    {
        if (spreadsheetNamespace == XlsxNamespaces.StrictSpreadsheet) { return XlsxNamespaces.StrictOfficeDocumentRelationships; }
        return XlsxNamespaces.OfficeDocumentRelationships;
    }

    internal static void WriteBookViews(XmlWriter writer, XlsxWorkbookPart part)
    {
        if (part.WorkbookViews.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "bookViews", GetOutputSpreadsheetNamespace(writer));
        foreach (XlsxWorkbookView view in part.WorkbookViews)
        {
            WriteWorkbookView(writer, view);
        }

        writer.WriteEndElement();
    }

    internal static void WriteWorkbookView(XmlWriter writer, XlsxWorkbookView view)
    {
        writer.WriteStartElement("", "workbookView", GetOutputSpreadsheetNamespace(writer));
        if (view.Visibility != null && view.Visibility.Length > 0)
        {
            writer.WriteAttributeString("visibility", view.Visibility);
        }

        if (view.HasActiveTab)
        {
            writer.WriteAttributeString("activeTab", view.ActiveTab.ToString(CultureInfo.InvariantCulture));
        }

        if (view.HasFirstSheet)
        {
            writer.WriteAttributeString("firstSheet", view.FirstSheet.ToString(CultureInfo.InvariantCulture));
        }

        if (view.HasTabRatio)
        {
            writer.WriteAttributeString("tabRatio", view.TabRatio.ToString(CultureInfo.InvariantCulture));
        }

        if (view.HasWindowX)
        {
            writer.WriteAttributeString("xWindow", view.WindowX.ToString(CultureInfo.InvariantCulture));
        }

        if (view.HasWindowY)
        {
            writer.WriteAttributeString("yWindow", view.WindowY.ToString(CultureInfo.InvariantCulture));
        }

        if (view.HasWindowWidth)
        {
            writer.WriteAttributeString("windowWidth", view.WindowWidth.ToString(CultureInfo.InvariantCulture));
        }

        if (view.HasWindowHeight)
        {
            writer.WriteAttributeString("windowHeight", view.WindowHeight.ToString(CultureInfo.InvariantCulture));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, view.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteWorkbookProtection(XmlWriter writer, XlsxWorkbookPart part)
    {
        if (!part.HasWorkbookProtection)
        {
            return;
        }

        XlsxWorkbookProtection? protection = part.WorkbookProtection;
        if (protection == null)
        {
            return;
        }

        writer.WriteStartElement("", "workbookProtection", GetOutputSpreadsheetNamespace(writer));
        if (protection.WorkbookPassword != null && protection.WorkbookPassword.Length > 0)
        {
            writer.WriteAttributeString("workbookPassword", protection.WorkbookPassword);
        }

        if (protection.WorkbookAlgorithmName != null && protection.WorkbookAlgorithmName.Length > 0)
        {
            writer.WriteAttributeString("workbookAlgorithmName", protection.WorkbookAlgorithmName);
        }

        if (protection.WorkbookHashValue != null && protection.WorkbookHashValue.Length > 0)
        {
            writer.WriteAttributeString("workbookHashValue", protection.WorkbookHashValue);
        }

        if (protection.WorkbookSaltValue != null && protection.WorkbookSaltValue.Length > 0)
        {
            writer.WriteAttributeString("workbookSaltValue", protection.WorkbookSaltValue);
        }

        if (protection.HasWorkbookSpinCount)
        {
            writer.WriteAttributeString("workbookSpinCount", XlsxXmlValueWriter.WriteUnsignedInteger(protection.WorkbookSpinCount));
        }

        if (protection.RevisionsPassword != null && protection.RevisionsPassword.Length > 0)
        {
            writer.WriteAttributeString("revisionsPassword", protection.RevisionsPassword);
        }

        if (protection.RevisionsAlgorithmName != null && protection.RevisionsAlgorithmName.Length > 0)
        {
            writer.WriteAttributeString("revisionsAlgorithmName", protection.RevisionsAlgorithmName);
        }

        if (protection.RevisionsHashValue != null && protection.RevisionsHashValue.Length > 0)
        {
            writer.WriteAttributeString("revisionsHashValue", protection.RevisionsHashValue);
        }

        if (protection.RevisionsSaltValue != null && protection.RevisionsSaltValue.Length > 0)
        {
            writer.WriteAttributeString("revisionsSaltValue", protection.RevisionsSaltValue);
        }

        if (protection.HasRevisionsSpinCount)
        {
            writer.WriteAttributeString("revisionsSpinCount", XlsxXmlValueWriter.WriteUnsignedInteger(protection.RevisionsSpinCount));
        }

        if (protection.HasLockStructure)
        {
            writer.WriteAttributeString("lockStructure", XlsxXmlValueWriter.WriteBoolean(protection.LockStructure));
        }

        if (protection.HasLockWindows)
        {
            writer.WriteAttributeString("lockWindows", XlsxXmlValueWriter.WriteBoolean(protection.LockWindows));
        }

        if (protection.HasLockRevision)
        {
            writer.WriteAttributeString("lockRevision", XlsxXmlValueWriter.WriteBoolean(protection.LockRevision));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, protection.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteDefinedNames(XmlWriter writer, XlsxWorkbookPart part)
    {
        if (part.DefinedNames.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "definedNames", GetOutputSpreadsheetNamespace(writer));
        foreach (XlsxWorkbookDefinedName definedName in part.DefinedNames)
        {
            WriteDefinedName(writer, definedName);
        }

        writer.WriteEndElement();
    }

    internal static void WriteDefinedName(XmlWriter writer, XlsxWorkbookDefinedName definedName)
    {
        writer.WriteStartElement("", "definedName", GetOutputSpreadsheetNamespace(writer));
        writer.WriteAttributeString("name", definedName.Name);
        if (definedName.HasLocalSheetId)
        {
            writer.WriteAttributeString("localSheetId", definedName.LocalSheetId.ToString(CultureInfo.InvariantCulture));
        }

        if (definedName.Comment != null && definedName.Comment.Length > 0)
        {
            writer.WriteAttributeString("comment", definedName.Comment);
        }

        if (definedName.CustomMenu != null && definedName.CustomMenu.Length > 0)
        {
            writer.WriteAttributeString("customMenu", definedName.CustomMenu);
        }

        if (definedName.Description != null && definedName.Description.Length > 0)
        {
            writer.WriteAttributeString("description", definedName.Description);
        }

        if (definedName.Help != null && definedName.Help.Length > 0)
        {
            writer.WriteAttributeString("help", definedName.Help);
        }

        if (definedName.StatusBar != null && definedName.StatusBar.Length > 0)
        {
            writer.WriteAttributeString("statusBar", definedName.StatusBar);
        }

        if (definedName.HasHidden)
        {
            writer.WriteAttributeString("hidden", XlsxXmlValueWriter.WriteBoolean(definedName.Hidden));
        }

        if (definedName.HasFunction)
        {
            writer.WriteAttributeString("function", XlsxXmlValueWriter.WriteBoolean(definedName.Function));
        }

        if (definedName.HasVbProcedure)
        {
            writer.WriteAttributeString("vbProcedure", XlsxXmlValueWriter.WriteBoolean(definedName.VbProcedure));
        }

        if (definedName.HasXlm)
        {
            writer.WriteAttributeString("xlm", XlsxXmlValueWriter.WriteBoolean(definedName.Xlm));
        }

        if (definedName.HasFunctionGroupId)
        {
            writer.WriteAttributeString("functionGroupId", definedName.FunctionGroupId.ToString(CultureInfo.InvariantCulture));
        }

        if (definedName.ShortcutKey != null && definedName.ShortcutKey.Length > 0)
        {
            writer.WriteAttributeString("shortcutKey", definedName.ShortcutKey);
        }

        if (definedName.HasPublishToServer)
        {
            writer.WriteAttributeString("publishToServer", XlsxXmlValueWriter.WriteBoolean(definedName.PublishToServer));
        }

        if (definedName.HasWorkbookParameter)
        {
            writer.WriteAttributeString("workbookParameter", XlsxXmlValueWriter.WriteBoolean(definedName.WorkbookParameter));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, definedName.UnknownAttributes);
        if (definedName.Text != null && definedName.Text.Length > 0)
        {
            writer.WriteString(definedName.Text);
        }

        writer.WriteEndElement();
    }

    internal static void WritePivotCaches(XmlWriter writer, XlsxWorkbookPart part)
    {
        if (part.PivotCaches == null || part.PivotCaches.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "pivotCaches", GetOutputSpreadsheetNamespace(writer));
        foreach (XlsxWorkbookPivotCacheReference cache in part.PivotCaches)
        {
            WritePivotCache(writer, cache);
        }

        writer.WriteEndElement();
    }

    internal static void WriteExternalReferences(XmlWriter writer, XlsxWorkbookPart part)
    {
        if (part.ExternalReferences == null || part.ExternalReferences.Count == 0)
        {
            return;
        }

        writer.WriteStartElement("", "externalReferences", GetOutputSpreadsheetNamespace(writer));
        foreach (XlsxWorkbookExternalReference reference in part.ExternalReferences)
        {
            writer.WriteStartElement("", "externalReference", GetOutputSpreadsheetNamespace(writer));
            if (reference.RelationshipId != null && reference.RelationshipId.Length > 0)
            {
                writer.WriteAttributeString("r", "id", GetOutputRelationshipNamespace(GetOutputSpreadsheetNamespace(writer)), reference.RelationshipId);
            }

            XlsxContentTypesWriter.WriteUnknownAttributes(writer, reference.UnknownAttributes);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    internal static void WritePivotCache(XmlWriter writer, XlsxWorkbookPivotCacheReference cache)
    {
        writer.WriteStartElement("", "pivotCache", GetOutputSpreadsheetNamespace(writer));
        if (cache.HasCacheId)
        {
            writer.WriteAttributeString("cacheId", cache.CacheId.ToString(CultureInfo.InvariantCulture));
        }

        writer.WriteAttributeString("r", "id", GetOutputRelationshipNamespace(GetOutputSpreadsheetNamespace(writer)), cache.RelationshipId);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, cache.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteCalculationProperties(XmlWriter writer, XlsxWorkbookPart part)
    {
        if (!part.HasCalculationProperties)
        {
            return;
        }

        XlsxWorkbookCalculationProperties calc = part.CalculationProperties;
        writer.WriteStartElement("", "calcPr", GetOutputSpreadsheetNamespace(writer));
        if (calc.HasCalcId)
        {
            writer.WriteAttributeString("calcId", calc.CalcId.ToString(CultureInfo.InvariantCulture));
        }

        if (calc.CalcMode != null && calc.CalcMode.Length > 0)
        {
            writer.WriteAttributeString("calcMode", calc.CalcMode);
        }

        if (calc.RefMode != null && calc.RefMode.Length > 0)
        {
            writer.WriteAttributeString("refMode", calc.RefMode);
        }

        if (calc.HasFullCalcOnLoad)
        {
            writer.WriteAttributeString("fullCalcOnLoad", XlsxXmlValueWriter.WriteBoolean(calc.FullCalcOnLoad));
        }

        if (calc.HasForceFullCalc)
        {
            writer.WriteAttributeString("forceFullCalc", XlsxXmlValueWriter.WriteBoolean(calc.ForceFullCalc));
        }

        if (calc.HasIterate)
        {
            writer.WriteAttributeString("iterate", XlsxXmlValueWriter.WriteBoolean(calc.Iterate));
        }

        if (calc.HasIterateCount)
        {
            writer.WriteAttributeString("iterateCount", calc.IterateCount.ToString(CultureInfo.InvariantCulture));
        }

        if (calc.HasIterateDelta)
        {
            writer.WriteAttributeString("iterateDelta", XlsxXmlValueWriter.WriteDouble(calc.IterateDelta));
        }

        XlsxContentTypesWriter.WriteUnknownAttributes(writer, calc.UnknownAttributes);
        writer.WriteEndElement();
    }

    internal static void WriteExtensionList(XmlWriter writer, XlsxWorkbookPart part)
    {
        if (!part.HasExtensionList)
        {
            return;
        }

        if (part.ExtensionList.RawXml != null && part.ExtensionList.RawXml.Length > 0)
        {
            writer.WriteRaw(part.ExtensionList.RawXml);
            return;
        }

        writer.WriteStartElement("", "extLst", GetOutputSpreadsheetNamespace(writer));
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, part.ExtensionList.UnknownRootAttributes);
        foreach (XlsxExtension extension in part.ExtensionList.Extensions)
        {
            XlsxExtensionListWriter.WriteExtension(writer, extension, GetOutputSpreadsheetNamespace(writer), "");
        }

        foreach (string unknownXml in part.ExtensionList.UnknownXml)
        {
            if (unknownXml != null && unknownXml.Length > 0)
            {
                writer.WriteRaw(unknownXml);
            }
        }

        writer.WriteEndElement();
    }

    internal static XlsxWorkbookPartWriteResult ValidateMinimumWorkbookPart(XlsxWorkbookPart part)
    {
        XlsxWorkbookPartWriteResult result = new XlsxWorkbookPartWriteResult();
        if (part == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.partNull", "Workbook part must not be null."));
            return result;
        }

        if (part.Sheets.Count == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.sheetsMissing", "Minimum workbook must contain at least one sheet.");
            diagnostic.SetLocation(XlsxPackagePaths.Workbook, "sheets", "");
            result.AddDiagnostic(diagnostic);
        }

        Dictionary<string, bool> sheetNames = new Dictionary<string, bool>(System.StringComparer.OrdinalIgnoreCase);
        Dictionary<string, bool> sheetIds = new Dictionary<string, bool>();
        Dictionary<string, bool> relationshipIds = new Dictionary<string, bool>();
        foreach (XlsxWorkbookSheet sheet in part.Sheets)
        {
            ValidateSheet(result, sheet, sheetNames, sheetIds, relationshipIds);
        }

        ValidateDefinedNames(result, part);
        ValidatePivotCaches(result, part);
        ValidateExternalReferences(result, part);
        ValidateWorkbookProtection(result, part);
        return result;
    }

    internal static void ValidatePivotCaches(XlsxWorkbookPartWriteResult result, XlsxWorkbookPart part)
    {
        if (part.PivotCaches == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.pivotCachesNull", "Workbook pivot cache list must not be null."));
            return;
        }

        foreach (XlsxWorkbookPivotCacheReference cache in part.PivotCaches)
        {
            ValidatePivotCache(result, cache);
        }
    }

    internal static void ValidateExternalReferences(XlsxWorkbookPartWriteResult result, XlsxWorkbookPart part)
    {
        if (part.ExternalReferences == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.externalReferencesNull", "Workbook external reference list must not be null."));
            return;
        }

        foreach (XlsxWorkbookExternalReference reference in part.ExternalReferences)
        {
            if (reference == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.externalReferenceNull", "Workbook external reference list must not contain null entries."));
                continue;
            }

            if (reference.RelationshipId == null || reference.RelationshipId.Length == 0)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.externalReferenceRelationshipIdMissing", "Workbook externalReference must have an r:id attribute."));
            }
            else
            {
                XlsxXmlValueParseResult relationshipId = XlsxXmlValueParser.ParseRelationshipId(reference.RelationshipId);
                foreach (XlsxDiagnostic diagnostic in relationshipId.Validation.Diagnostics)
                {
                    diagnostic.SetLocation(part.PartPath, "externalReference", "r:id");
                    result.AddDiagnostic(diagnostic);
                }
            }

            if (reference.UnknownAttributes == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.externalReferenceUnknownAttributesNull", "Workbook externalReference unknown attribute list must not be null."));
            }
        }
    }

    internal static void ValidatePivotCache(XlsxWorkbookPartWriteResult result, XlsxWorkbookPivotCacheReference cache)
    {
        if (cache == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.pivotCacheNull", "Workbook pivot cache list must not contain null entries."));
            return;
        }

        if (!cache.HasCacheId)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.pivotCache.cacheIdMissing", "Workbook pivot cache must have a cache id."));
        }

        if (cache.RelationshipId == null || cache.RelationshipId.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.pivotCache.relationshipIdMissing", "Workbook pivot cache must have a relationship id."));
        }
        else
        {
            XlsxXmlValueParseResult relationshipId = XlsxXmlValueParser.ParseRelationshipId(cache.RelationshipId);
            foreach (XlsxDiagnostic diagnostic in relationshipId.Validation.Diagnostics)
            {
                result.AddDiagnostic(diagnostic);
            }
        }

        if (cache.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.pivotCacheUnknownAttributesNull", "Workbook pivot cache unknown attribute list must not be null."));
        }
    }

    internal static void ValidateDefinedNames(XlsxWorkbookPartWriteResult result, XlsxWorkbookPart part)
    {
        if (part.DefinedNames == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.definedNamesNull", "Workbook defined names list must not be null."));
            return;
        }

        Dictionary<string, bool> names = new Dictionary<string, bool>();
        int index = 0;
        foreach (XlsxWorkbookDefinedName definedName in part.DefinedNames)
        {
            if (definedName == null)
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.definedNameNull", "Workbook defined names must not contain null entries.");
                diagnostic.SetLocation(XlsxPackagePaths.Workbook, "definedName", index.ToString(CultureInfo.InvariantCulture));
                result.AddDiagnostic(diagnostic);
            }
            else
            {
                ValidateDefinedName(result, definedName, names, index);
            }

            index = index + 1;
        }
    }

    internal static void ValidateDefinedName(XlsxWorkbookPartWriteResult result, XlsxWorkbookDefinedName definedName, Dictionary<string, bool> names, int index)
    {
        if (definedName.Name == null || definedName.Name.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.definedName.nameMissing", "Defined name must have a name.");
            diagnostic.SetLocation(XlsxPackagePaths.Workbook, "definedName", "name");
            result.AddDiagnostic(diagnostic);
        }
        else
        {
            string key = BuildDefinedNameScopeKey(definedName);
            if (names.ContainsKey(key))
            {
                XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.definedName.duplicate", "Defined names must be unique within the same scope.");
                diagnostic.SetLocation(XlsxPackagePaths.Workbook, "definedName", index.ToString(CultureInfo.InvariantCulture));
                result.AddDiagnostic(diagnostic);
            }
            else
            {
                names.Add(key, true);
            }
        }

        if (definedName.HasLocalSheetId && definedName.LocalSheetId < 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.definedName.localSheetIdRange", "Defined name local sheet id must not be negative.");
            diagnostic.SetLocation(XlsxPackagePaths.Workbook, "definedName", "localSheetId");
            result.AddDiagnostic(diagnostic);
        }

        if (definedName.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.definedNameUnknownAttributesNull", "Defined name unknown attributes list must not be null."));
        }
    }

    internal static string BuildDefinedNameScopeKey(XlsxWorkbookDefinedName definedName)
    {
        if (definedName.HasLocalSheetId)
        {
            return definedName.Name + "|L" + definedName.LocalSheetId.ToString(CultureInfo.InvariantCulture);
        }

        return definedName.Name + "|G";
    }

    internal static void ValidateWorkbookProtection(XlsxWorkbookPartWriteResult result, XlsxWorkbookPart part)
    {
        if (!part.HasWorkbookProtection)
        {
            return;
        }

        if (part.WorkbookProtection == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.protectionNull", "Workbook protection must not be null when HasWorkbookProtection is true."));
            return;
        }

        if (part.WorkbookProtection.HasWorkbookSpinCount && part.WorkbookProtection.WorkbookSpinCount == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.protectionWorkbookSpinCountRange", "Workbook protection workbook spin count must be greater than zero when present."));
        }

        if (part.WorkbookProtection.HasRevisionsSpinCount && part.WorkbookProtection.RevisionsSpinCount == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.protectionRevisionsSpinCountRange", "Workbook protection revisions spin count must be greater than zero when present."));
        }

        ValidateWorkbookProtectionHashDefinitions(result, part.WorkbookProtection);

        if (part.WorkbookProtection.UnknownAttributes == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.protectionUnknownAttributesNull", "Workbook protection unknown attributes list must not be null."));
        }
    }

    internal static void ValidateWorkbookProtectionHashDefinitions(XlsxWorkbookPartWriteResult result, XlsxWorkbookProtection protection)
    {
        bool hasWorkbookAlgorithm = protection.WorkbookAlgorithmName != null && protection.WorkbookAlgorithmName.Length > 0;
        bool hasWorkbookHash = protection.WorkbookHashValue != null && protection.WorkbookHashValue.Length > 0;
        bool hasWorkbookSalt = protection.WorkbookSaltValue != null && protection.WorkbookSaltValue.Length > 0;
        bool hasWorkbookDefinition = hasWorkbookAlgorithm || hasWorkbookHash || hasWorkbookSalt || protection.HasWorkbookSpinCount;
        if (hasWorkbookDefinition && (!hasWorkbookAlgorithm || !hasWorkbookHash || !hasWorkbookSalt || !protection.HasWorkbookSpinCount))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.protectionWorkbookHashIncomplete", "Workbook protection modern hash settings require an algorithm, hash value, salt value, and spin count."));
        }

        bool hasRevisionsAlgorithm = protection.RevisionsAlgorithmName != null && protection.RevisionsAlgorithmName.Length > 0;
        bool hasRevisionsHash = protection.RevisionsHashValue != null && protection.RevisionsHashValue.Length > 0;
        bool hasRevisionsSalt = protection.RevisionsSaltValue != null && protection.RevisionsSaltValue.Length > 0;
        bool hasRevisionsDefinition = hasRevisionsAlgorithm || hasRevisionsHash || hasRevisionsSalt || protection.HasRevisionsSpinCount;
        if (hasRevisionsDefinition && (!hasRevisionsAlgorithm || !hasRevisionsHash || !hasRevisionsSalt || !protection.HasRevisionsSpinCount))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.protectionRevisionsHashIncomplete", "Workbook protection revisions hash settings require an algorithm, hash value, salt value, and spin count."));
        }
    }

    internal static void ValidateSheet(XlsxWorkbookPartWriteResult result, XlsxWorkbookSheet sheet, Dictionary<string, bool> sheetNames, Dictionary<string, bool> sheetIds, Dictionary<string, bool> relationshipIds)
    {
        if (sheet == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.sheetNull", "Workbook sheet entry must not be null."));
            return;
        }

        ValidateSheetName(result, sheet, sheetNames);
        ValidateSheetId(result, sheet, sheetIds);
        ValidateSheetRelationshipId(result, sheet, relationshipIds);
        ValidateSheetState(result, sheet);
    }

    internal static void ValidateSheetName(XlsxWorkbookPartWriteResult result, XlsxWorkbookSheet sheet, Dictionary<string, bool> sheetNames)
    {
        if (sheet.Name == null || sheet.Name.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.sheet.nameMissing", "Workbook sheet must have a name.");
            diagnostic.SetLocation(XlsxPackagePaths.Workbook, "sheet", "name");
            result.AddDiagnostic(diagnostic);
            return;
        }

        if (!IsValidSheetName(sheet.Name))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.sheet.nameInvalid", "Workbook sheet name contains invalid characters or exceeds the length limit.");
            diagnostic.SetLocation(XlsxPackagePaths.Workbook, "sheet", "name");
            result.AddDiagnostic(diagnostic);
            return;
        }

        if (sheetNames.ContainsKey(sheet.Name))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.sheet.nameDuplicate", "Workbook sheet names must be unique.");
            diagnostic.SetLocation(XlsxPackagePaths.Workbook, "sheet", "name");
            result.AddDiagnostic(diagnostic);
            return;
        }

        sheetNames.Add(sheet.Name, true);
    }

    internal static void ValidateSheetId(XlsxWorkbookPartWriteResult result, XlsxWorkbookSheet sheet, Dictionary<string, bool> sheetIds)
    {
        if (sheet.SheetId == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.sheet.sheetIdMissing", "Workbook sheet must have a positive sheetId.");
            diagnostic.SetLocation(XlsxPackagePaths.Workbook, "sheet", "sheetId");
            result.AddDiagnostic(diagnostic);
            return;
        }

        string key = sheet.SheetId.ToString(CultureInfo.InvariantCulture);
        if (sheetIds.ContainsKey(key))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.sheet.sheetIdDuplicate", "Workbook sheet IDs must be unique.");
            diagnostic.SetLocation(XlsxPackagePaths.Workbook, "sheet", "sheetId");
            result.AddDiagnostic(diagnostic);
            return;
        }

        sheetIds.Add(key, true);
    }

    internal static void ValidateSheetRelationshipId(XlsxWorkbookPartWriteResult result, XlsxWorkbookSheet sheet, Dictionary<string, bool> relationshipIds)
    {
        if (sheet.RelationshipId == null || sheet.RelationshipId.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.sheet.relationshipIdMissing", "Workbook sheet must have an r:id relationship ID.");
            diagnostic.SetLocation(XlsxPackagePaths.Workbook, "sheet", "r:id");
            result.AddDiagnostic(diagnostic);
            return;
        }

        XlsxXmlValueParseResult relationshipId = XlsxXmlValueParser.ParseRelationshipId(sheet.RelationshipId);
        foreach (XlsxDiagnostic diagnostic in relationshipId.Validation.Diagnostics)
        {
            diagnostic.SetLocation(XlsxPackagePaths.Workbook, "sheet", "r:id");
            result.AddDiagnostic(diagnostic);
        }

        if (!relationshipId.IsValid)
        {
            return;
        }

        if (relationshipIds.ContainsKey(sheet.RelationshipId))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.sheet.relationshipIdDuplicate", "Workbook sheet relationship IDs must be unique.");
            diagnostic.SetLocation(XlsxPackagePaths.Workbook, "sheet", "r:id");
            diagnostic.SetRelationshipId(sheet.RelationshipId);
            result.AddDiagnostic(diagnostic);
            return;
        }

        relationshipIds.Add(sheet.RelationshipId, true);
    }

    internal static void ValidateSheetState(XlsxWorkbookPartWriteResult result, XlsxWorkbookSheet sheet)
    {
        if (sheet.State == null || sheet.State.Length == 0)
        {
            return;
        }

        if (sheet.State == "visible" || sheet.State == "hidden" || sheet.State == "veryHidden")
        {
            return;
        }

        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.sheet.stateInvalid", "Workbook sheet state must be visible, hidden, or veryHidden.");
        diagnostic.SetLocation(XlsxPackagePaths.Workbook, "sheet", "state");
        result.AddDiagnostic(diagnostic);
    }

    internal static bool IsValidSheetName(string name)
    {
        if (name == null || name.Length == 0 || name.Length > 31)
        {
            return false;
        }

        int index = 0;
        while (index < name.Length)
        {
            char value = name[index];
            if (value == '[' || value == ']' || value == ':' || value == '*' || value == '?' || value == '/' || value == '\\')
            {
                return false;
            }

            if (value <= 31)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void WriteSheet(XmlWriter writer, XlsxWorkbookSheet sheet)
    {
        writer.WriteStartElement("", "sheet", GetOutputSpreadsheetNamespace(writer));
        writer.WriteAttributeString("name", sheet.Name);
        writer.WriteAttributeString("sheetId", sheet.SheetId.ToString(CultureInfo.InvariantCulture));
        if (sheet.State != null && sheet.State.Length > 0 && sheet.State != "visible")
        {
            writer.WriteAttributeString("state", sheet.State);
        }

        writer.WriteAttributeString("r", "id", GetOutputRelationshipNamespace(GetOutputSpreadsheetNamespace(writer)), sheet.RelationshipId);
        XlsxContentTypesWriter.WriteUnknownAttributes(writer, sheet.UnknownAttributes);
        writer.WriteEndElement();
    }
}
