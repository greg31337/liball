using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads workbook part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxWorkbookPartReader : IXlsxWorkbookPartReader
{
    /// <summary>
    /// Initializes a new workbook part reader with its default state.
    /// </summary>
    public XlsxWorkbookPartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The workbook part produced by the operation.</returns>
    public XlsxWorkbookPart ReadString(string xml)
    {
        return ReadStringWithOptions(xml, new XlsxReaderOptions());
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The workbook part produced by the operation.</returns>
    public XlsxWorkbookPart ReadStringWithOptions(string xml, XlsxReaderOptions options)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        return ReadBytes(bytes, options);
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The workbook part produced by the operation.</returns>
    public XlsxWorkbookPart ReadBytes(byte[] bytes, XlsxReaderOptions options)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return CreateMissingRootResult();
        }

        XlsxXmlReadLimitResult limits = XlsxXmlReaderHelpers.ValidateBytesLimits(bytes, options, XlsxPackagePaths.Workbook);
        if (!limits.IsValid)
        {
            XlsxWorkbookPart limitedPart = new XlsxWorkbookPart();
            foreach (XlsxDiagnostic diagnostic in limits.Validation.Diagnostics) { limitedPart.AddDiagnostic(diagnostic); }
            return limitedPart;
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadValidatedStream(stream);
        }
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <param name = "options">The options that control limits, preservation, and operation behavior.</param>
    /// <returns>The workbook part produced by the operation.</returns>
    public XlsxWorkbookPart ReadStream(Stream stream, XlsxReaderOptions options)
    {
        if (stream == null || !stream.CanRead) { return CreateMissingRootResult(); }
        using (MemoryStream buffer = new MemoryStream())
        {
            stream.CopyTo(buffer);
            return ReadBytes(buffer.ToArray(), options);
        }
    }

    internal static XlsxWorkbookPart ReadValidatedStream(Stream stream)
    {
        XlsxWorkbookPart part = new XlsxWorkbookPart();
        bool sawRoot = false;
        using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
        {
            while (!reader.EOF)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Read();
                    continue;
                }

                if (!sawRoot)
                {
                    sawRoot = true;
                    ReadRootElement(part, reader);
                    reader.Read();
                    continue;
                }

                if (reader.Depth == 1 && IsWorkbookNamespace(part, reader.NamespaceURI) && reader.LocalName == "sheets")
                {
                    ReadSheets(part, reader);
                    continue;
                }

                if (reader.Depth == 1 && IsWorkbookNamespace(part, reader.NamespaceURI) && reader.LocalName == "bookViews")
                {
                    ReadBookViews(part, reader);
                    continue;
                }

                if (reader.Depth == 1 && IsWorkbookNamespace(part, reader.NamespaceURI) && reader.LocalName == "definedNames")
                {
                    ReadDefinedNames(part, reader);
                    continue;
                }

                if (reader.Depth == 1 && IsWorkbookNamespace(part, reader.NamespaceURI) && reader.LocalName == "pivotCaches")
                {
                    ReadPivotCaches(part, reader);
                    continue;
                }

                if (reader.Depth == 1 && IsWorkbookNamespace(part, reader.NamespaceURI) && reader.LocalName == "externalReferences")
                {
                    ReadExternalReferences(part, reader);
                    continue;
                }

                if (reader.Depth == 1 && IsWorkbookNamespace(part, reader.NamespaceURI) && reader.LocalName == "calcPr")
                {
                    ReadCalculationProperties(part, reader);
                    continue;
                }

                if (reader.Depth == 1 && IsWorkbookNamespace(part, reader.NamespaceURI) && reader.LocalName == "workbookProtection")
                {
                    ReadWorkbookProtection(part, reader);
                    continue;
                }

                if (reader.Depth == 1 && IsWorkbookNamespace(part, reader.NamespaceURI) && reader.LocalName == "extLst")
                {
                    ReadExtensionList(part, reader);
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.missingRoot", "Workbook XML must contain a workbook root element."));
        }

        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxWorkbookPart CreateMissingRootResult()
    {
        XlsxWorkbookPart part = new XlsxWorkbookPart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.missingRoot", "Workbook XML must contain a workbook root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxWorkbookPart part, XmlReader reader)
    {
        if (reader.LocalName != "workbook" || !IsSpreadsheetNamespace(reader.NamespaceURI))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.invalidRoot", "Workbook XML root must be a workbook element in a SpreadsheetML namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        part.NamespaceUri = reader.NamespaceURI;
        part.MarkupCompatibility = XlsxMarkupCompatibility.ReadAttributes(reader);
        ReadUnknownWorkbookAttributes(reader, part.UnknownRootAttributes);
    }

    internal static void ReadSheets(XlsxWorkbookPart part, XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int containerDepth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == containerDepth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                reader.Read();
                continue;
            }

            if (reader.Depth == containerDepth + 1 && IsWorkbookNamespace(part, reader.NamespaceURI) && reader.LocalName == "sheet")
            {
                ReadSheet(part, reader);
                continue;
            }

            part.UnknownXml.Add(reader.ReadOuterXml());
        }
    }

    internal static void ReadSheet(XlsxWorkbookPart part, XmlReader reader)
    {
        XlsxWorkbookSheet sheet = new XlsxWorkbookSheet();
        sheet.Name = reader.GetAttribute("name");
        sheet.State = reader.GetAttribute("state");
        sheet.RelationshipId = XlsxRelationshipTypeCompatibility.GetRelationshipAttribute(reader, "id");
        ReadUnknownSheetAttributes(reader, sheet.UnknownAttributes);
        string? sheetIdText = reader.GetAttribute("sheetId");
        if (sheet.Name == null || sheet.Name.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.sheet.nameMissing", "Workbook sheet must have a name attribute.");
            diagnostic.SetLocation(part.PartPath, "sheet", "name");
            part.AddDiagnostic(diagnostic);
        }

        if (sheet.RelationshipId == null || sheet.RelationshipId.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.sheet.relationshipIdMissing", "Workbook sheet must have an r:id attribute.");
            diagnostic.SetLocation(part.PartPath, "sheet", "r:id");
            part.AddDiagnostic(diagnostic);
        }

        if (sheetIdText == null || sheetIdText.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.sheet.sheetIdMissing", "Workbook sheet must have a sheetId attribute.");
            diagnostic.SetLocation(part.PartPath, "sheet", "sheetId");
            part.AddDiagnostic(diagnostic);
        }
        else
        {
            XlsxXmlValueParseResult sheetIdResult = XlsxXmlValueParser.ParseUnsignedInteger(sheetIdText);
            if (sheetIdResult.IsValid)
            {
                sheet.SheetId = sheetIdResult.UnsignedIntValue;
            }
            else
            {
                AddValueDiagnostics(part, sheetIdResult, "sheet", "sheetId");
            }
        }

        part.Sheets.Add(sheet);
        reader.ReadOuterXml();
    }

    internal static void ReadBookViews(XlsxWorkbookPart part, XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int containerDepth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == containerDepth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                reader.Read();
                continue;
            }

            if (reader.Depth == containerDepth + 1 && IsWorkbookNamespace(part, reader.NamespaceURI) && reader.LocalName == "workbookView")
            {
                ReadWorkbookView(part, reader);
                continue;
            }

            part.UnknownXml.Add(reader.ReadOuterXml());
        }
    }

    internal static void ReadWorkbookView(XlsxWorkbookPart part, XmlReader reader)
    {
        XlsxWorkbookView view = new XlsxWorkbookView();
        view.Visibility = reader.GetAttribute("visibility");
        ReadOptionalUnsignedAttribute(part, reader, "activeTab", "workbookView", view, 1);
        ReadOptionalUnsignedAttribute(part, reader, "firstSheet", "workbookView", view, 2);
        ReadOptionalUnsignedAttribute(part, reader, "tabRatio", "workbookView", view, 3);
        ReadOptionalIntegerAttribute(part, reader, "xWindow", "workbookView", view, 1);
        ReadOptionalIntegerAttribute(part, reader, "yWindow", "workbookView", view, 2);
        ReadOptionalIntegerAttribute(part, reader, "windowWidth", "workbookView", view, 3);
        ReadOptionalIntegerAttribute(part, reader, "windowHeight", "workbookView", view, 4);
        ReadUnknownWorkbookViewAttributes(reader, view.UnknownAttributes);
        part.WorkbookViews.Add(view);
        reader.ReadOuterXml();
    }

    internal static void ReadDefinedNames(XlsxWorkbookPart part, XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int containerDepth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == containerDepth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                reader.Read();
                continue;
            }

            if (reader.Depth == containerDepth + 1 && IsWorkbookNamespace(part, reader.NamespaceURI) && reader.LocalName == "definedName")
            {
                ReadDefinedName(part, reader);
                continue;
            }

            part.UnknownXml.Add(reader.ReadOuterXml());
        }
    }

    internal static void ReadDefinedName(XlsxWorkbookPart part, XmlReader reader)
    {
        XlsxWorkbookDefinedName definedName = new XlsxWorkbookDefinedName();
        definedName.Name = reader.GetAttribute("name");
        definedName.Comment = reader.GetAttribute("comment");
        definedName.CustomMenu = reader.GetAttribute("customMenu");
        definedName.Description = reader.GetAttribute("description");
        definedName.Help = reader.GetAttribute("help");
        definedName.StatusBar = reader.GetAttribute("statusBar");
        definedName.ShortcutKey = reader.GetAttribute("shortcutKey");
        ReadOptionalDefinedNameIntegerAttribute(part, reader, "localSheetId", definedName);
        ReadOptionalDefinedNameBooleanAttribute(part, reader, "hidden", definedName, 1);
        ReadOptionalDefinedNameBooleanAttribute(part, reader, "function", definedName, 2);
        ReadOptionalDefinedNameBooleanAttribute(part, reader, "vbProcedure", definedName, 3);
        ReadOptionalDefinedNameBooleanAttribute(part, reader, "xlm", definedName, 4);
        ReadOptionalDefinedNameUnsignedAttribute(part, reader, "functionGroupId", definedName);
        ReadOptionalDefinedNameBooleanAttribute(part, reader, "publishToServer", definedName, 5);
        ReadOptionalDefinedNameBooleanAttribute(part, reader, "workbookParameter", definedName, 6);
        ReadUnknownDefinedNameAttributes(reader, definedName.UnknownAttributes);
        if (definedName.Name == null || definedName.Name.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.definedName.nameMissing", "Defined name must have a name attribute.");
            diagnostic.SetLocation(part.PartPath, "definedName", "name");
            part.AddDiagnostic(diagnostic);
        }

        definedName.Text = reader.ReadElementContentAsString();
        part.DefinedNames.Add(definedName);
    }

    internal static void ReadPivotCaches(XlsxWorkbookPart part, XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int containerDepth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == containerDepth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                reader.Read();
                continue;
            }

            if (reader.Depth == containerDepth + 1 && IsWorkbookNamespace(part, reader.NamespaceURI) && reader.LocalName == "pivotCache")
            {
                ReadPivotCache(part, reader);
                continue;
            }

            part.UnknownXml.Add(reader.ReadOuterXml());
        }
    }

    internal static void ReadPivotCache(XlsxWorkbookPart part, XmlReader reader)
    {
        XlsxWorkbookPivotCacheReference cache = new XlsxWorkbookPivotCacheReference();
        cache.RelationshipId = XlsxCellValueConverter.NormalizeText(XlsxRelationshipTypeCompatibility.GetRelationshipAttribute(reader, "id"));
        ReadPivotCacheUnknownAttributes(reader, cache.UnknownAttributes);
        string? cacheIdText = reader.GetAttribute("cacheId");
        if (cacheIdText == null || cacheIdText.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.pivotCache.cacheIdMissing", "Workbook pivotCache must have a cacheId attribute.");
            diagnostic.SetLocation(part.PartPath, "pivotCache", "cacheId");
            part.AddDiagnostic(diagnostic);
        }
        else
        {
            XlsxXmlValueParseResult cacheId = XlsxXmlValueParser.ParseUnsignedInteger(cacheIdText);
            if (cacheId.IsValid)
            {
                cache.CacheId = cacheId.UnsignedIntValue;
                cache.HasCacheId = true;
            }
            else
            {
                AddValueDiagnostics(part, cacheId, "pivotCache", "cacheId");
            }
        }

        if (cache.RelationshipId.Length == 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.pivotCache.relationshipIdMissing", "Workbook pivotCache must have an r:id attribute.");
            diagnostic.SetLocation(part.PartPath, "pivotCache", "r:id");
            part.AddDiagnostic(diagnostic);
        }

        part.PivotCaches.Add(cache);
        reader.ReadOuterXml();
    }

    internal static void ReadPivotCacheUnknownAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool knownCacheId = reader.NamespaceURI == "" && reader.LocalName == "cacheId";
            bool knownRelationship = XlsxRelationshipTypeCompatibility.IsRelationshipNamespace(reader.NamespaceURI) && reader.LocalName == "id";
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!knownCacheId && !knownRelationship && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static void ReadExternalReferences(XlsxWorkbookPart part, XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsWorkbookNamespace(part, reader.NamespaceURI) && reader.LocalName == "externalReference")
            {
                XlsxWorkbookExternalReference reference = new XlsxWorkbookExternalReference();
                reference.RelationshipId = XlsxCellValueConverter.NormalizeText(XlsxRelationshipTypeCompatibility.GetRelationshipAttribute(reader, "id"));
                ReadExternalReferenceUnknownAttributes(reader, reference.UnknownAttributes);
                part.ExternalReferences.Add(reference);
                reader.ReadOuterXml();
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                part.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static void ReadExternalReferenceUnknownAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool knownRelationshipId = XlsxRelationshipTypeCompatibility.IsRelationshipNamespace(reader.NamespaceURI) && reader.LocalName == "id";
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!knownRelationshipId && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static void ReadCalculationProperties(XlsxWorkbookPart part, XmlReader reader)
    {
        XlsxWorkbookCalculationProperties calc = new XlsxWorkbookCalculationProperties();
        ReadOptionalCalculationUnsignedAttribute(part, reader, "calcId", calc, 1);
        calc.CalcMode = reader.GetAttribute("calcMode");
        calc.RefMode = reader.GetAttribute("refMode");
        ReadOptionalCalculationBooleanAttribute(part, reader, "fullCalcOnLoad", calc, 1);
        ReadOptionalCalculationBooleanAttribute(part, reader, "forceFullCalc", calc, 2);
        ReadOptionalCalculationBooleanAttribute(part, reader, "iterate", calc, 3);
        ReadOptionalCalculationUnsignedAttribute(part, reader, "iterateCount", calc, 2);
        ReadOptionalCalculationDoubleAttribute(part, reader, "iterateDelta", calc);
        ReadUnknownCalculationAttributes(reader, calc.UnknownAttributes);
        part.CalculationProperties = calc;
        part.HasCalculationProperties = true;
        reader.ReadOuterXml();
    }

    internal static void ReadWorkbookProtection(XlsxWorkbookPart part, XmlReader reader)
    {
        XlsxWorkbookProtection protection = new XlsxWorkbookProtection();
        protection.WorkbookPassword = reader.GetAttribute("workbookPassword");
        protection.WorkbookAlgorithmName = reader.GetAttribute("workbookAlgorithmName");
        protection.WorkbookHashValue = reader.GetAttribute("workbookHashValue");
        protection.WorkbookSaltValue = reader.GetAttribute("workbookSaltValue");
        protection.RevisionsPassword = reader.GetAttribute("revisionsPassword");
        protection.RevisionsAlgorithmName = reader.GetAttribute("revisionsAlgorithmName");
        protection.RevisionsHashValue = reader.GetAttribute("revisionsHashValue");
        protection.RevisionsSaltValue = reader.GetAttribute("revisionsSaltValue");
        ReadOptionalProtectionBooleanAttribute(part, reader, "lockStructure", protection, 1);
        ReadOptionalProtectionBooleanAttribute(part, reader, "lockWindows", protection, 2);
        ReadOptionalProtectionBooleanAttribute(part, reader, "lockRevision", protection, 3);
        ReadOptionalProtectionUnsignedAttribute(part, reader, "workbookSpinCount", protection, 1);
        ReadOptionalProtectionUnsignedAttribute(part, reader, "revisionsSpinCount", protection, 2);
        ReadUnknownProtectionAttributes(reader, protection.UnknownAttributes);
        part.WorkbookProtection = protection;
        part.HasWorkbookProtection = true;
        reader.ReadOuterXml();
    }

    internal static void ReadExtensionList(XlsxWorkbookPart part, XmlReader reader)
    {
        string rawXml = reader.ReadOuterXml();
        XlsxExtensionList list = new XlsxExtensionListReader().ReadString(rawXml);
        foreach (XlsxDiagnostic diagnostic in list.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, "extLst", "");
            part.AddDiagnostic(diagnostic);
        }

        part.ExtensionList = list;
        part.HasExtensionList = true;
    }

    internal static void ReadOptionalUnsignedAttribute(XlsxWorkbookPart part, XmlReader reader, string attributeName, string elementName, XlsxWorkbookView view, int target)
    {
        string? text = reader.GetAttribute(attributeName);
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            AddValueDiagnostics(part, result, elementName, attributeName);
            return;
        }

        if (target == 1)
        {
            view.ActiveTab = result.UnsignedIntValue;
            view.HasActiveTab = true;
        }
        else if (target == 2)
        {
            view.FirstSheet = result.UnsignedIntValue;
            view.HasFirstSheet = true;
        }
        else if (target == 3)
        {
            view.TabRatio = result.UnsignedIntValue;
            view.HasTabRatio = true;
        }
    }

    internal static void ReadOptionalIntegerAttribute(XlsxWorkbookPart part, XmlReader reader, string attributeName, string elementName, XlsxWorkbookView view, int target)
    {
        string? text = reader.GetAttribute(attributeName);
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseInteger(text);
        if (!result.IsValid)
        {
            AddValueDiagnostics(part, result, elementName, attributeName);
            return;
        }

        if (target == 1)
        {
            view.WindowX = result.IntValue;
            view.HasWindowX = true;
        }
        else if (target == 2)
        {
            view.WindowY = result.IntValue;
            view.HasWindowY = true;
        }
        else if (target == 3)
        {
            view.WindowWidth = result.IntValue;
            view.HasWindowWidth = true;
        }
        else if (target == 4)
        {
            view.WindowHeight = result.IntValue;
            view.HasWindowHeight = true;
        }
    }

    internal static void ReadOptionalDefinedNameIntegerAttribute(XlsxWorkbookPart part, XmlReader reader, string attributeName, XlsxWorkbookDefinedName definedName)
    {
        string? text = reader.GetAttribute(attributeName);
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseInteger(text);
        if (!result.IsValid)
        {
            AddValueDiagnostics(part, result, "definedName", attributeName);
            return;
        }

        definedName.LocalSheetId = result.IntValue;
        definedName.HasLocalSheetId = true;
        if (definedName.LocalSheetId < 0)
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "workbook.definedName.localSheetIdRange", "Defined name local sheet id must not be negative.");
            diagnostic.SetLocation(part.PartPath, "definedName", attributeName);
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void ReadOptionalDefinedNameBooleanAttribute(XlsxWorkbookPart part, XmlReader reader, string attributeName, XlsxWorkbookDefinedName definedName, int target)
    {
        string? text = reader.GetAttribute(attributeName);
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseBoolean(text);
        if (!result.IsValid)
        {
            AddValueDiagnostics(part, result, "definedName", attributeName);
            return;
        }

        if (target == 1)
        {
            definedName.Hidden = result.BooleanValue;
            definedName.HasHidden = true;
        }
        else if (target == 2)
        {
            definedName.Function = result.BooleanValue;
            definedName.HasFunction = true;
        }
        else if (target == 3)
        {
            definedName.VbProcedure = result.BooleanValue;
            definedName.HasVbProcedure = true;
        }
        else if (target == 4)
        {
            definedName.Xlm = result.BooleanValue;
            definedName.HasXlm = true;
        }
        else if (target == 5)
        {
            definedName.PublishToServer = result.BooleanValue;
            definedName.HasPublishToServer = true;
        }
        else if (target == 6)
        {
            definedName.WorkbookParameter = result.BooleanValue;
            definedName.HasWorkbookParameter = true;
        }
    }

    internal static void ReadOptionalDefinedNameUnsignedAttribute(XlsxWorkbookPart part, XmlReader reader, string attributeName, XlsxWorkbookDefinedName definedName)
    {
        string? text = reader.GetAttribute(attributeName);
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            AddValueDiagnostics(part, result, "definedName", attributeName);
            return;
        }

        definedName.FunctionGroupId = result.UnsignedIntValue;
        definedName.HasFunctionGroupId = true;
    }

    internal static void ReadOptionalCalculationUnsignedAttribute(XlsxWorkbookPart part, XmlReader reader, string attributeName, XlsxWorkbookCalculationProperties calc, int target)
    {
        string? text = reader.GetAttribute(attributeName);
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            AddValueDiagnostics(part, result, "calcPr", attributeName);
            return;
        }

        if (target == 1)
        {
            calc.CalcId = result.UnsignedIntValue;
            calc.HasCalcId = true;
        }
        else if (target == 2)
        {
            calc.IterateCount = result.UnsignedIntValue;
            calc.HasIterateCount = true;
        }
    }

    internal static void ReadOptionalCalculationBooleanAttribute(XlsxWorkbookPart part, XmlReader reader, string attributeName, XlsxWorkbookCalculationProperties calc, int target)
    {
        string? text = reader.GetAttribute(attributeName);
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseBoolean(text);
        if (!result.IsValid)
        {
            AddValueDiagnostics(part, result, "calcPr", attributeName);
            return;
        }

        if (target == 1)
        {
            calc.FullCalcOnLoad = result.BooleanValue;
            calc.HasFullCalcOnLoad = true;
        }
        else if (target == 2)
        {
            calc.ForceFullCalc = result.BooleanValue;
            calc.HasForceFullCalc = true;
        }
        else if (target == 3)
        {
            calc.Iterate = result.BooleanValue;
            calc.HasIterate = true;
        }
    }

    internal static void ReadOptionalCalculationDoubleAttribute(XlsxWorkbookPart part, XmlReader reader, string attributeName, XlsxWorkbookCalculationProperties calc)
    {
        string? text = reader.GetAttribute(attributeName);
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseDouble(text);
        if (!result.IsValid)
        {
            AddValueDiagnostics(part, result, "calcPr", attributeName);
            return;
        }

        calc.IterateDelta = result.DoubleValue;
        calc.HasIterateDelta = true;
    }

    internal static void ReadOptionalProtectionBooleanAttribute(XlsxWorkbookPart part, XmlReader reader, string attributeName, XlsxWorkbookProtection protection, int target)
    {
        string? text = reader.GetAttribute(attributeName);
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseBoolean(text);
        if (!result.IsValid)
        {
            AddValueDiagnostics(part, result, "workbookProtection", attributeName);
            return;
        }

        if (target == 1)
        {
            protection.LockStructure = result.BooleanValue;
            protection.HasLockStructure = true;
        }
        else if (target == 2)
        {
            protection.LockWindows = result.BooleanValue;
            protection.HasLockWindows = true;
        }
        else if (target == 3)
        {
            protection.LockRevision = result.BooleanValue;
            protection.HasLockRevision = true;
        }
    }

    internal static void ReadOptionalProtectionUnsignedAttribute(XlsxWorkbookPart part, XmlReader reader, string attributeName, XlsxWorkbookProtection protection, int target)
    {
        string? text = reader.GetAttribute(attributeName);
        if (text == null || text.Length == 0)
        {
            return;
        }

        XlsxXmlValueParseResult result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        if (!result.IsValid)
        {
            AddValueDiagnostics(part, result, "workbookProtection", attributeName);
            return;
        }

        if (target == 1)
        {
            protection.WorkbookSpinCount = result.UnsignedIntValue;
            protection.HasWorkbookSpinCount = true;
        }
        else if (target == 2)
        {
            protection.RevisionsSpinCount = result.UnsignedIntValue;
            protection.HasRevisionsSpinCount = true;
        }
    }

    internal static void AddValueDiagnostics(XlsxWorkbookPart part, XlsxXmlValueParseResult valueResult, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in valueResult.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, elementName, attributeName);
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void ReadUnknownWorkbookAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            bool markupCompatibility = reader.NamespaceURI == XlsxNamespaces.MarkupCompatibility;
            if (!namespaceDeclaration && !markupCompatibility)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static void ReadUnknownSheetAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool knownName = reader.NamespaceURI == "" && reader.LocalName == "name";
            bool knownSheetId = reader.NamespaceURI == "" && reader.LocalName == "sheetId";
            bool knownState = reader.NamespaceURI == "" && reader.LocalName == "state";
            bool knownRelationshipId = XlsxRelationshipTypeCompatibility.IsRelationshipNamespace(reader.NamespaceURI) && reader.LocalName == "id";
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!knownName && !knownSheetId && !knownState && !knownRelationshipId && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static void ReadUnknownWorkbookViewAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = IsKnownWorkbookViewAttribute(reader);
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static void ReadUnknownDefinedNameAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = IsKnownDefinedNameAttribute(reader);
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static void ReadUnknownCalculationAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = IsKnownCalculationAttribute(reader);
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static void ReadUnknownProtectionAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = IsKnownProtectionAttribute(reader);
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static bool IsKnownWorkbookViewAttribute(XmlReader reader)
    {
        if (reader.NamespaceURI != "")
        {
            return false;
        }

        if (reader.LocalName == "visibility" || reader.LocalName == "activeTab" || reader.LocalName == "firstSheet" || reader.LocalName == "tabRatio")
        {
            return true;
        }

        if (reader.LocalName == "xWindow" || reader.LocalName == "yWindow" || reader.LocalName == "windowWidth" || reader.LocalName == "windowHeight")
        {
            return true;
        }

        return false;
    }

    internal static bool IsKnownDefinedNameAttribute(XmlReader reader)
    {
        if (reader.NamespaceURI != "")
        {
            return false;
        }

        if (reader.LocalName == "name" || reader.LocalName == "localSheetId" || reader.LocalName == "comment" || reader.LocalName == "customMenu" || reader.LocalName == "description" || reader.LocalName == "help" || reader.LocalName == "statusBar" || reader.LocalName == "shortcutKey")
        {
            return true;
        }

        if (reader.LocalName == "hidden" || reader.LocalName == "function" || reader.LocalName == "vbProcedure" || reader.LocalName == "xlm" || reader.LocalName == "functionGroupId" || reader.LocalName == "publishToServer" || reader.LocalName == "workbookParameter")
        {
            return true;
        }

        return false;
    }

    internal static bool IsKnownCalculationAttribute(XmlReader reader)
    {
        if (reader.NamespaceURI != "")
        {
            return false;
        }

        if (reader.LocalName == "calcId" || reader.LocalName == "calcMode" || reader.LocalName == "refMode")
        {
            return true;
        }

        if (reader.LocalName == "fullCalcOnLoad" || reader.LocalName == "forceFullCalc" || reader.LocalName == "iterate" || reader.LocalName == "iterateCount" || reader.LocalName == "iterateDelta")
        {
            return true;
        }

        return false;
    }

    internal static bool IsKnownProtectionAttribute(XmlReader reader)
    {
        if (reader.NamespaceURI != "")
        {
            return false;
        }

        if (reader.LocalName == "workbookPassword" || reader.LocalName == "workbookAlgorithmName" || reader.LocalName == "workbookHashValue" || reader.LocalName == "workbookSaltValue" || reader.LocalName == "workbookSpinCount")
        {
            return true;
        }

        if (reader.LocalName == "revisionsPassword" || reader.LocalName == "revisionsAlgorithmName" || reader.LocalName == "revisionsHashValue" || reader.LocalName == "revisionsSaltValue" || reader.LocalName == "revisionsSpinCount")
        {
            return true;
        }

        if (reader.LocalName == "lockStructure" || reader.LocalName == "lockWindows" || reader.LocalName == "lockRevision")
        {
            return true;
        }

        return false;
    }

    internal static bool IsWorkbookNamespace(XlsxWorkbookPart part, string namespaceUri)
    {
        if (namespaceUri == part.NamespaceUri)
        {
            return true;
        }

        return IsSpreadsheetNamespace(namespaceUri);
    }

    internal static bool IsSpreadsheetNamespace(string namespaceUri)
    {
        if (namespaceUri == XlsxNamespaces.Spreadsheet)
        {
            return true;
        }

        if (namespaceUri == XlsxNamespaces.StrictSpreadsheet)
        {
            return true;
        }

        return false;
    }
}
