using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads worksheet part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxWorksheetPartReader : IXlsxWorksheetPartReader
{
    /// <summary>
    /// Initializes a new worksheet part reader with its default state.
    /// </summary>
    public XlsxWorksheetPartReader()
    {
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <returns>The worksheet part produced by the operation.</returns>
    public XlsxWorksheetPart ReadString(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return CreateMissingRootResult();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        return ReadBytes(bytes);
    }

    /// <summary>
    /// Parses the supplied OOXML text and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "xml">The OOXML text produced or preserved by the operation.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>The worksheet part produced by the operation.</returns>
    public XlsxWorksheetPart ReadStringWithRelationships(string xml, XlsxRelationshipsPart relationships)
    {
        XlsxWorksheetPart part = ReadString(xml);
        XlsxWorksheetHyperlinkResult result = part.ResolveHyperlinkRelationships(relationships);
        AddHyperlinkDiagnostics(part, result);
        XlsxSheetDrawingRelationshipResult drawingResult = part.ResolveDrawingRelationship(relationships);
        AddDrawingRelationshipDiagnostics(part, drawingResult);
        XlsxWorksheetPivotTableRelationshipResult pivotTableResult = part.ResolvePivotTableRelationships(relationships);
        AddPivotTableRelationshipDiagnostics(part, pivotTableResult);
        part.IsValid = part.Validation.IsValid;
        return part;
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The worksheet part produced by the operation.</returns>
    public XlsxWorksheetPart ReadBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return CreateMissingRootResult();
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return ReadStream(stream);
        }
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>The worksheet part produced by the operation.</returns>
    public XlsxWorksheetPart ReadBytesWithRelationships(byte[] bytes, XlsxRelationshipsPart relationships)
    {
        XlsxWorksheetPart part = ReadBytes(bytes);
        XlsxWorksheetHyperlinkResult result = part.ResolveHyperlinkRelationships(relationships);
        AddHyperlinkDiagnostics(part, result);
        XlsxSheetDrawingRelationshipResult drawingResult = part.ResolveDrawingRelationship(relationships);
        AddDrawingRelationshipDiagnostics(part, drawingResult);
        XlsxWorksheetPivotTableRelationshipResult pivotTableResult = part.ResolvePivotTableRelationships(relationships);
        AddPivotTableRelationshipDiagnostics(part, pivotTableResult);
        part.IsValid = part.Validation.IsValid;
        return part;
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The worksheet part produced by the operation.</returns>
    public XlsxWorksheetPart ReadStream(Stream stream)
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
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

                if (reader.Depth == 1 && IsWorksheetNamespace(part, reader.NamespaceURI))
                {
                    ReadWorksheetChild(part, reader);
                    continue;
                }

                part.UnknownXml.Add(reader.ReadOuterXml());
            }
        }

        if (!sawRoot)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.missingRoot", "Worksheet XML must contain a worksheet root element."));
        }

        XlsxWorksheetMergeCellsResult mergeValidation = part.ValidateMergedCells();
        AddMergeDiagnostics(part, mergeValidation);
        part.IsValid = part.Validation.IsValid;
        return part;
    }

    internal static XlsxWorksheetPart CreateMissingRootResult()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.missingRoot", "Worksheet XML must contain a worksheet root element."));
        return part;
    }

    internal static void ReadRootElement(XlsxWorksheetPart part, XmlReader reader)
    {
        if (reader.LocalName != "worksheet" || !XlsxWorkbookPartReader.IsSpreadsheetNamespace(reader.NamespaceURI))
        {
            XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "worksheet.invalidRoot", "Worksheet XML root must be a worksheet element in a SpreadsheetML namespace.");
            diagnostic.SetLocation(part.PartPath, reader.LocalName, "");
            part.AddDiagnostic(diagnostic);
        }

        part.NamespaceUri = reader.NamespaceURI;
        XlsxCorePropertiesReader.ReadUnknownRootAttributes(reader, part.UnknownRootAttributes);
    }

    internal static void ReadWorksheetChild(XlsxWorksheetPart part, XmlReader reader)
    {
        if (reader.LocalName == "sheetPr")
        {
            ReadSheetProperties(part, reader);
        }
        else if (reader.LocalName == "dimension")
        {
            part.Dimension.Reference = reader.GetAttribute("ref");
            part.HasDimension = true;
            reader.ReadOuterXml();
        }
        else if (reader.LocalName == "sheetViews")
        {
            ReadSheetViews(part, reader);
        }
        else if (reader.LocalName == "sheetFormatPr")
        {
            ReadFormatProperties(part, reader);
        }
        else if (reader.LocalName == "cols")
        {
            ReadColumns(part, reader);
        }
        else if (reader.LocalName == "sheetData")
        {
            ReadSheetData(part, reader);
        }
        else if (reader.LocalName == "mergeCells")
        {
            ReadMergeCells(part, reader);
        }
        else if (reader.LocalName == "hyperlinks")
        {
            ReadHyperlinks(part, reader);
        }
        else if (reader.LocalName == "pageMargins")
        {
            ReadPageMargins(part, reader);
        }
        else if (reader.LocalName == "pageSetup")
        {
            ReadPageSetup(part, reader);
        }
        else if (reader.LocalName == "headerFooter")
        {
            ReadHeaderFooter(part, reader);
        }
        else if (reader.LocalName == "rowBreaks")
        {
            ReadPageBreaks(part, reader, part.RowBreaks);
        }
        else if (reader.LocalName == "colBreaks")
        {
            ReadPageBreaks(part, reader, part.ColumnBreaks);
        }
        else if (reader.LocalName == "printOptions")
        {
            ReadPrintOptions(part, reader);
        }
        else if (reader.LocalName == "sheetProtection")
        {
            ReadSheetProtection(part, reader);
        }
        else if (reader.LocalName == "autoFilter")
        {
            string rawXml = reader.ReadOuterXml();
            part.AutoFilter = new XlsxAutoFilterReader().ReadString(rawXml);
            part.AutoFilter.RawXml = rawXml;
            part.HasAutoFilter = true;
            AddAutoFilterDiagnostics(part, part.AutoFilter);
        }
        else if (reader.LocalName == "dataValidations")
        {
            string rawXml = reader.ReadOuterXml();
            part.DataValidations = new XlsxDataValidationReader().ReadString(rawXml);
            part.DataValidations.RawXml = rawXml;
            part.HasDataValidations = true;
            AddDataValidationDiagnostics(part, part.DataValidations);
        }
        else if (reader.LocalName == "conditionalFormatting")
        {
            string rawXml = reader.ReadOuterXml();
            XlsxConditionalFormatting formatting = new XlsxConditionalFormattingReader().ReadString(rawXml);
            formatting.RawXml = rawXml;
            part.ConditionalFormattings.Add(formatting);
            AddConditionalFormattingDiagnostics(part, formatting);
        }
        else if (reader.LocalName == "scenarios")
        {
            ReadScenarios(part, reader.ReadOuterXml());
        }
        else if (reader.LocalName == "customSheetViews")
        {
            ReadCustomSheetViews(part, reader.ReadOuterXml());
        }
        else if (reader.LocalName == "ignoredErrors")
        {
            ReadIgnoredErrors(part, reader.ReadOuterXml());
        }
        else if (reader.LocalName == "cellSmartTags" || reader.LocalName == "smartTags")
        {
            ReadSmartTags(part, reader.ReadOuterXml());
        }
        else if (reader.LocalName == "webPublishItems")
        {
            ReadWebPublishItems(part, reader.ReadOuterXml());
        }
        else if (reader.LocalName == "drawing")
        {
            part.Drawing = ReadDrawingReference(reader);
            part.HasDrawing = true;
        }
        else if (reader.LocalName == "oleObjects")
        {
            ReadEmbeddedObjectReferences(part, reader);
        }
        else if (reader.LocalName == "tableParts")
        {
            ReadTableReferences(part, reader);
        }
        else if (reader.LocalName == "pivotTableDefinition")
        {
            ReadPivotTableReference(part, reader);
        }
        else if (reader.LocalName == "extLst")
        {
            string rawXml = reader.ReadOuterXml();
            part.ExtensionList = new XlsxExtensionListReader().ReadString(rawXml);
            part.HasExtensionList = true;
            XlsxSparklineGroupsReader.ReadWorksheetExtensionList(part, rawXml);
        }
        else
        {
            part.UnknownXml.Add(reader.ReadOuterXml());
        }
    }

    internal static void ReadEmbeddedObjectReferences(XlsxWorksheetPart part, XmlReader reader)
    {
        int depth = reader.Depth;
        if (reader.IsEmptyElement) { reader.Read(); return; }
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth) { reader.Read(); return; }
            if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == part.NamespaceUri && reader.LocalName == "oleObject")
            {
                XlsxWorksheetEmbeddedObjectReference reference = new XlsxWorksheetEmbeddedObjectReference();
                reference.ProgramId = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("progId"));
                reference.RelationshipId = XlsxCellValueConverter.NormalizeText(XlsxRelationshipTypeCompatibility.GetRelationshipAttribute(reader, "id"));
                XlsxXmlValueParseResult shapeId = XlsxXmlValueParser.ParseUnsignedInteger(reader.GetAttribute("shapeId"));
                if (shapeId.IsValid)
                {
                    reference.ShapeId = shapeId.UnsignedIntValue;
                    reference.HasShapeId = true;
                }
                reference.IsOleObject = reference.ProgramId != "Package";
                reference.RawXml = reader.ReadOuterXml();
                part.EmbeddedObjectReferences.Add(reference);
                continue;
            }
            reader.Read();
        }
    }

    internal static void AddAutoFilterDiagnostics(XlsxWorksheetPart part, XlsxAutoFilter filter)
    {
        foreach (XlsxDiagnostic diagnostic in filter.Validation.Diagnostics)
        {
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddDataValidationDiagnostics(XlsxWorksheetPart part, XlsxDataValidations validations)
    {
        foreach (XlsxDiagnostic diagnostic in validations.Validation.Diagnostics)
        {
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddConditionalFormattingDiagnostics(XlsxWorksheetPart part, XlsxConditionalFormatting formatting)
    {
        foreach (XlsxDiagnostic diagnostic in formatting.Validation.Diagnostics)
        {
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddDrawingRelationshipDiagnostics(XlsxWorksheetPart part, XlsxSheetDrawingRelationshipResult result)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddPivotTableRelationshipDiagnostics(XlsxWorksheetPart part, XlsxWorksheetPivotTableRelationshipResult result)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static XlsxSheetDrawingReference ReadDrawingReference(XmlReader reader)
    {
        XlsxSheetDrawingReference drawing = new XlsxSheetDrawingReference();
        drawing.RelationshipId = NormalizeWorksheetText(XlsxRelationshipTypeCompatibility.GetRelationshipAttribute(reader, "id"));
        ReadUnknownDrawingAttributes(reader, drawing.UnknownAttributes);
        drawing.RawXml = reader.ReadOuterXml();
        return drawing;
    }

    internal static void ReadUnknownDrawingAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
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

    internal static void ReadSheetProperties(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetProperties properties = new XlsxWorksheetProperties();
        properties.CodeName = reader.GetAttribute("codeName");
        SetOptionalBool(part, reader.GetAttribute("enableFormatConditionsCalculation"), "sheetPr", "enableFormatConditionsCalculation", properties, 1);
        ReadUnknownAttributes(reader, properties.UnknownAttributes, "codeName", "enableFormatConditionsCalculation");
        properties.RawXml = reader.ReadOuterXml();
        ReadSheetPropertyChildren(part, properties);
        part.SheetProperties = properties;
        part.HasSheetProperties = true;
    }

    internal static void ReadSheetPropertyChildren(XlsxWorksheetPart part, XlsxWorksheetProperties properties)
    {
        using (StringReader stringReader = new StringReader(properties.RawXml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1)
                    {
                        if (reader.LocalName == "tabColor")
                        {
                            ReadTabColor(part, properties, reader);
                            continue;
                        }
                        if (reader.LocalName == "outlinePr")
                        {
                            ReadOutlineProperties(part, properties, reader);
                            continue;
                        }
                        if (reader.LocalName == "pageSetUpPr")
                        {
                            ReadPageSetupProperties(part, properties, reader);
                            continue;
                        }
                        properties.UnknownXml.Add(reader.ReadOuterXml());
                        continue;
                    }
                    reader.Read();
                }
            }
        }
    }

    internal static void ReadTabColor(XlsxWorksheetPart part, XlsxWorksheetProperties properties, XmlReader reader)
    {
        XlsxSpreadsheetColor color = new XlsxSpreadsheetColor();
        string? rgb = reader.GetAttribute("rgb");
        if (rgb != null)
        {
            XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseColor(rgb);
            AddMetadataDiagnostics(part, parsed.Validation, "sheetPr", "tabColor");
            if (parsed.IsValid)
            {
                color.Rgb = parsed.ColorValue.Rgb;
                color.HasRgb = true;
            }
        }
        string? autoText = reader.GetAttribute("auto");
        if (autoText != null)
        {
            XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseBoolean(autoText);
            AddMetadataDiagnostics(part, parsed.Validation, "tabColor", "auto");
            if (parsed.IsValid)
            {
                color.IsAuto = parsed.BooleanValue;
                color.HasAuto = true;
            }
        }
        ReadColorUInt(part, reader.GetAttribute("indexed"), color, 1);
        ReadColorUInt(part, reader.GetAttribute("theme"), color, 2);
        string? tintText = reader.GetAttribute("tint");
        if (tintText != null)
        {
            XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseDouble(tintText);
            AddMetadataDiagnostics(part, parsed.Validation, "tabColor", "tint");
            if (parsed.IsValid)
            {
                color.Tint = parsed.DoubleValue;
                color.HasTint = true;
            }
        }
        properties.TabColor = color;
        properties.HasTabColor = true;
        reader.ReadOuterXml();
    }

    internal static void ReadColorUInt(XlsxWorksheetPart part, string? text, XlsxSpreadsheetColor color, int kind)
    {
        if (text == null)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseUnsignedInteger(text);
        AddMetadataDiagnostics(part, parsed.Validation, "tabColor", kind == 1 ? "indexed" : "theme");
        if (parsed.IsValid && kind == 1)
        {
            color.Indexed = parsed.UnsignedIntValue;
            color.HasIndexed = true;
        }
        else if (parsed.IsValid)
        {
            color.Theme = parsed.UnsignedIntValue;
            color.HasTheme = true;
        }
    }

    internal static void ReadOutlineProperties(XlsxWorksheetPart part, XlsxWorksheetProperties properties, XmlReader reader)
    {
        XlsxWorksheetOutlineProperties outline = new XlsxWorksheetOutlineProperties();
        SetOutlineBoolean(part, outline, reader.GetAttribute("applyStyles"), 1);
        SetOutlineBoolean(part, outline, reader.GetAttribute("summaryBelow"), 2);
        SetOutlineBoolean(part, outline, reader.GetAttribute("summaryRight"), 3);
        SetOutlineBoolean(part, outline, reader.GetAttribute("showOutlineSymbols"), 4);
        ReadMetadataUnknownAttributes(reader, outline.UnknownAttributes, "|applyStyles|summaryBelow|summaryRight|showOutlineSymbols|");
        properties.OutlineProperties = outline;
        properties.HasOutlineProperties = true;
        reader.ReadOuterXml();
    }

    internal static void SetOutlineBoolean(XlsxWorksheetPart part, XlsxWorksheetOutlineProperties outline, string? text, int kind)
    {
        if (text == null)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseBoolean(text);
        AddMetadataDiagnostics(part, parsed.Validation, "outlinePr", "boolean");
        if (!parsed.IsValid)
        {
            return;
        }
        if (kind == 1)
        {
            outline.ApplyStyles = parsed.BooleanValue;
            outline.HasApplyStyles = true;
        }
        else if (kind == 2)
        {
            outline.SummaryBelow = parsed.BooleanValue;
            outline.HasSummaryBelow = true;
        }
        else if (kind == 3)
        {
            outline.SummaryRight = parsed.BooleanValue;
            outline.HasSummaryRight = true;
        }
        else
        {
            outline.ShowOutlineSymbols = parsed.BooleanValue;
            outline.HasShowOutlineSymbols = true;
        }
    }

    internal static void ReadPageSetupProperties(XlsxWorksheetPart part, XlsxWorksheetProperties properties, XmlReader reader)
    {
        XlsxWorksheetPageSetupProperties pageSetup = new XlsxWorksheetPageSetupProperties();
        SetPageSetupPropertyBoolean(part, pageSetup, reader.GetAttribute("autoPageBreaks"), true);
        SetPageSetupPropertyBoolean(part, pageSetup, reader.GetAttribute("fitToPage"), false);
        ReadMetadataUnknownAttributes(reader, pageSetup.UnknownAttributes, "|autoPageBreaks|fitToPage|");
        properties.PageSetupProperties = pageSetup;
        properties.HasPageSetupProperties = true;
        reader.ReadOuterXml();
    }

    internal static void SetPageSetupPropertyBoolean(XlsxWorksheetPart part, XlsxWorksheetPageSetupProperties pageSetup, string? text, bool autoPageBreaks)
    {
        if (text == null)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseBoolean(text);
        AddMetadataDiagnostics(part, parsed.Validation, "pageSetUpPr", autoPageBreaks ? "autoPageBreaks" : "fitToPage");
        if (parsed.IsValid && autoPageBreaks)
        {
            pageSetup.AutoPageBreaks = parsed.BooleanValue;
            pageSetup.HasAutoPageBreaks = true;
        }
        else if (parsed.IsValid)
        {
            pageSetup.FitToPage = parsed.BooleanValue;
            pageSetup.HasFitToPage = true;
        }
    }

    internal static void ReadScenarios(XlsxWorksheetPart part, string xml)
    {
        XlsxWorksheetScenarios scenarios = new XlsxWorksheetScenarios();
        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 0)
                    {
                        string? current = reader.GetAttribute("current");
                        if (current != null)
                        {
                            XlsxXmlValueParseResult parsed = ReadMetadataUInt(part, current, "scenarios", "current");
                            scenarios.Current = parsed.UnsignedIntValue;
                            scenarios.HasCurrent = parsed.IsValid;
                        }
                        string? show = reader.GetAttribute("show");
                        if (show != null)
                        {
                            XlsxXmlValueParseResult parsed = ReadMetadataUInt(part, show, "scenarios", "show");
                            scenarios.Show = parsed.UnsignedIntValue;
                            scenarios.HasShow = parsed.IsValid;
                        }
                        scenarios.SquareReference = NormalizeWorksheetText(reader.GetAttribute("sqref"));
                        ReadMetadataUnknownAttributes(reader, scenarios.UnknownAttributes, "|current|show|sqref|");
                        reader.Read();
                        continue;
                    }
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1 && reader.LocalName == "scenario")
                    {
                        scenarios.Items.Add(ReadScenario(part, reader.ReadOuterXml()));
                        continue;
                    }
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1)
                    {
                        scenarios.UnknownXml.Add(reader.ReadOuterXml());
                        continue;
                    }
                    reader.Read();
                }
            }
        }
        part.Scenarios = scenarios;
        part.HasScenarios = true;
    }

    internal static XlsxWorksheetScenario ReadScenario(XlsxWorksheetPart part, string xml)
    {
        XlsxWorksheetScenario scenario = new XlsxWorksheetScenario();
        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 0)
                    {
                        scenario.Name = NormalizeWorksheetText(reader.GetAttribute("name"));
                        scenario.Comment = NormalizeWorksheetText(reader.GetAttribute("comment"));
                        scenario.User = NormalizeWorksheetText(reader.GetAttribute("user"));
                        SetScenarioBoolean(part, scenario, reader.GetAttribute("locked"), true);
                        SetScenarioBoolean(part, scenario, reader.GetAttribute("hidden"), false);
                        ReadMetadataUnknownAttributes(reader, scenario.UnknownAttributes, "|name|comment|user|locked|hidden|count|");
                        reader.Read();
                        continue;
                    }
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1 && reader.LocalName == "inputCells")
                    {
                        scenario.InputCells.Add(ReadScenarioInputCell(part, reader));
                        continue;
                    }
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1)
                    {
                        scenario.UnknownXml.Add(reader.ReadOuterXml());
                        continue;
                    }
                    reader.Read();
                }
            }
        }
        return scenario;
    }

    internal static void SetScenarioBoolean(XlsxWorksheetPart part, XlsxWorksheetScenario scenario, string? text, bool locked)
    {
        if (text == null)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = ReadMetadataBoolean(part, text, "scenario", locked ? "locked" : "hidden");
        if (parsed.IsValid && locked)
        {
            scenario.Locked = parsed.BooleanValue;
            scenario.HasLocked = true;
        }
        else if (parsed.IsValid)
        {
            scenario.Hidden = parsed.BooleanValue;
            scenario.HasHidden = true;
        }
    }

    internal static XlsxWorksheetScenarioInputCell ReadScenarioInputCell(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetScenarioInputCell input = new XlsxWorksheetScenarioInputCell();
        input.Reference = NormalizeWorksheetText(reader.GetAttribute("r"));
        input.Value = NormalizeWorksheetText(reader.GetAttribute("val"));
        string? numberFormatId = reader.GetAttribute("numFmtId");
        if (numberFormatId != null)
        {
            XlsxXmlValueParseResult parsed = ReadMetadataUInt(part, numberFormatId, "inputCells", "numFmtId");
            input.NumberFormatId = parsed.UnsignedIntValue;
            input.HasNumberFormatId = parsed.IsValid;
        }
        SetScenarioInputBoolean(part, input, reader.GetAttribute("deleted"), true);
        SetScenarioInputBoolean(part, input, reader.GetAttribute("undone"), false);
        ReadMetadataUnknownAttributes(reader, input.UnknownAttributes, "|r|val|numFmtId|deleted|undone|");
        reader.ReadOuterXml();
        return input;
    }

    internal static void SetScenarioInputBoolean(XlsxWorksheetPart part, XlsxWorksheetScenarioInputCell input, string? text, bool deleted)
    {
        if (text == null)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = ReadMetadataBoolean(part, text, "inputCells", deleted ? "deleted" : "undone");
        if (parsed.IsValid && deleted)
        {
            input.Deleted = parsed.BooleanValue;
            input.HasDeleted = true;
        }
        else if (parsed.IsValid)
        {
            input.Undone = parsed.BooleanValue;
            input.HasUndone = true;
        }
    }

    internal static void ReadCustomSheetViews(XlsxWorksheetPart part, string xml)
    {
        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1 && reader.LocalName == "customSheetView")
                    {
                        string rawXml = reader.ReadOuterXml();
                        part.CustomSheetViews.Add(ReadCustomSheetView(part, rawXml));
                        continue;
                    }
                    reader.Read();
                }
            }
        }
    }

    internal static XlsxWorksheetCustomView ReadCustomSheetView(XlsxWorksheetPart part, string xml)
    {
        XlsxWorksheetCustomView view = new XlsxWorksheetCustomView();
        view.RawXml = xml;
        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                reader.MoveToContent();
                view.Guid = NormalizeWorksheetText(reader.GetAttribute("guid"));
                SetCustomViewUInt(part, view, reader.GetAttribute("scale"), true);
                SetCustomViewUInt(part, view, reader.GetAttribute("colorId"), false);
                SetCustomViewBoolean(part, view, reader.GetAttribute("showFormulas"), 1);
                SetCustomViewBoolean(part, view, reader.GetAttribute("showGridLines"), 2);
                SetCustomViewBoolean(part, view, reader.GetAttribute("showRowCol"), 3);
                SetCustomViewBoolean(part, view, reader.GetAttribute("showPageBreaks"), 4);
                ReadMetadataUnknownAttributes(reader, view.UnknownAttributes, "|guid|scale|colorId|showFormulas|showGridLines|showRowCol|showPageBreaks|");
            }
        }
        return view;
    }

    internal static void SetCustomViewUInt(XlsxWorksheetPart part, XlsxWorksheetCustomView view, string? text, bool scale)
    {
        if (text == null)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = ReadMetadataUInt(part, text, "customSheetView", scale ? "scale" : "colorId");
        if (parsed.IsValid && scale)
        {
            view.Scale = parsed.UnsignedIntValue;
            view.HasScale = true;
        }
        else if (parsed.IsValid)
        {
            view.ColorId = parsed.UnsignedIntValue;
            view.HasColorId = true;
        }
    }

    internal static void SetCustomViewBoolean(XlsxWorksheetPart part, XlsxWorksheetCustomView view, string? text, int kind)
    {
        if (text == null)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = ReadMetadataBoolean(part, text, "customSheetView", "boolean");
        if (!parsed.IsValid)
        {
            return;
        }
        if (kind == 1)
        {
            view.ShowFormulas = parsed.BooleanValue;
            view.HasShowFormulas = true;
        }
        else if (kind == 2)
        {
            view.ShowGridLines = parsed.BooleanValue;
            view.HasShowGridLines = true;
        }
        else if (kind == 3)
        {
            view.ShowRowColumnHeaders = parsed.BooleanValue;
            view.HasShowRowColumnHeaders = true;
        }
        else
        {
            view.ShowPageBreaks = parsed.BooleanValue;
            view.HasShowPageBreaks = true;
        }
    }

    internal static void ReadIgnoredErrors(XlsxWorksheetPart part, string xml)
    {
        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1 && reader.LocalName == "ignoredError")
                    {
                        XlsxWorksheetIgnoredError ignored = new XlsxWorksheetIgnoredError();
                        ignored.SquareReference = NormalizeWorksheetText(reader.GetAttribute("sqref"));
                        SetIgnoredErrorBoolean(part, ignored, reader.GetAttribute("evalError"), 1);
                        SetIgnoredErrorBoolean(part, ignored, reader.GetAttribute("twoDigitTextYear"), 2);
                        SetIgnoredErrorBoolean(part, ignored, reader.GetAttribute("numberStoredAsText"), 3);
                        SetIgnoredErrorBoolean(part, ignored, reader.GetAttribute("formula"), 4);
                        SetIgnoredErrorBoolean(part, ignored, reader.GetAttribute("formulaRange"), 5);
                        SetIgnoredErrorBoolean(part, ignored, reader.GetAttribute("unlockedFormula"), 6);
                        SetIgnoredErrorBoolean(part, ignored, reader.GetAttribute("emptyCellReference"), 7);
                        SetIgnoredErrorBoolean(part, ignored, reader.GetAttribute("listDataValidation"), 8);
                        SetIgnoredErrorBoolean(part, ignored, reader.GetAttribute("calculatedColumn"), 9);
                        ReadMetadataUnknownAttributes(reader, ignored.UnknownAttributes, "|sqref|evalError|twoDigitTextYear|numberStoredAsText|formula|formulaRange|unlockedFormula|emptyCellReference|listDataValidation|calculatedColumn|");
                        part.IgnoredErrors.Add(ignored);
                        reader.ReadOuterXml();
                        continue;
                    }
                    reader.Read();
                }
            }
        }
    }

    internal static void SetIgnoredErrorBoolean(XlsxWorksheetPart part, XlsxWorksheetIgnoredError ignored, string? text, int kind)
    {
        if (text == null)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = ReadMetadataBoolean(part, text, "ignoredError", "boolean");
        if (!parsed.IsValid)
        {
            return;
        }
        if (kind == 1) { ignored.EvaluationError = parsed.BooleanValue; ignored.HasEvaluationError = true; }
        else if (kind == 2) { ignored.TwoDigitTextYear = parsed.BooleanValue; ignored.HasTwoDigitTextYear = true; }
        else if (kind == 3) { ignored.NumberStoredAsText = parsed.BooleanValue; ignored.HasNumberStoredAsText = true; }
        else if (kind == 4) { ignored.Formula = parsed.BooleanValue; ignored.HasFormula = true; }
        else if (kind == 5) { ignored.FormulaRange = parsed.BooleanValue; ignored.HasFormulaRange = true; }
        else if (kind == 6) { ignored.UnlockedFormula = parsed.BooleanValue; ignored.HasUnlockedFormula = true; }
        else if (kind == 7) { ignored.EmptyCellReference = parsed.BooleanValue; ignored.HasEmptyCellReference = true; }
        else if (kind == 8) { ignored.ListDataValidation = parsed.BooleanValue; ignored.HasListDataValidation = true; }
        else { ignored.CalculatedColumn = parsed.BooleanValue; ignored.HasCalculatedColumn = true; }
    }

    internal static void ReadSmartTags(XlsxWorksheetPart part, string xml)
    {
        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1 && reader.LocalName == "cellSmartTag")
                    {
                        part.SmartTags.Add(ReadSmartTag(part, reader.ReadOuterXml()));
                        continue;
                    }
                    reader.Read();
                }
            }
        }
    }

    internal static XlsxWorksheetSmartTag ReadSmartTag(XlsxWorksheetPart part, string xml)
    {
        XlsxWorksheetSmartTag smartTag = new XlsxWorksheetSmartTag();
        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 0)
                    {
                        smartTag.Reference = NormalizeWorksheetText(reader.GetAttribute("r"));
                        string? type = reader.GetAttribute("type");
                        if (type != null)
                        {
                            XlsxXmlValueParseResult parsed = ReadMetadataUInt(part, type, "cellSmartTag", "type");
                            smartTag.TypeId = parsed.UnsignedIntValue;
                            smartTag.HasTypeId = parsed.IsValid;
                        }
                        SetSmartTagBoolean(part, smartTag, reader.GetAttribute("deleted"), true);
                        SetSmartTagBoolean(part, smartTag, reader.GetAttribute("xmlBased"), false);
                        ReadMetadataUnknownAttributes(reader, smartTag.UnknownAttributes, "|r|type|deleted|xmlBased|");
                        reader.Read();
                        continue;
                    }
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1 && reader.LocalName == "cellSmartTagPr")
                    {
                        XlsxWorksheetSmartTagProperty property = new XlsxWorksheetSmartTagProperty();
                        property.Key = NormalizeWorksheetText(reader.GetAttribute("key"));
                        property.Value = NormalizeWorksheetText(reader.GetAttribute("val"));
                        ReadMetadataUnknownAttributes(reader, property.UnknownAttributes, "|key|val|");
                        smartTag.Properties.Add(property);
                        reader.ReadOuterXml();
                        continue;
                    }
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1)
                    {
                        smartTag.UnknownXml.Add(reader.ReadOuterXml());
                        continue;
                    }
                    reader.Read();
                }
            }
        }
        return smartTag;
    }

    internal static void SetSmartTagBoolean(XlsxWorksheetPart part, XlsxWorksheetSmartTag smartTag, string? text, bool deleted)
    {
        if (text == null)
        {
            return;
        }
        XlsxXmlValueParseResult parsed = ReadMetadataBoolean(part, text, "cellSmartTag", deleted ? "deleted" : "xmlBased");
        if (parsed.IsValid && deleted)
        {
            smartTag.Deleted = parsed.BooleanValue;
            smartTag.HasDeleted = true;
        }
        else if (parsed.IsValid)
        {
            smartTag.XmlBased = parsed.BooleanValue;
            smartTag.HasXmlBased = true;
        }
    }

    internal static void ReadWebPublishItems(XlsxWorksheetPart part, string xml)
    {
        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1 && reader.LocalName == "webPublishItem")
                    {
                        XlsxWorksheetWebPublishItem item = new XlsxWorksheetWebPublishItem();
                        string? id = reader.GetAttribute("id");
                        if (id != null)
                        {
                            XlsxXmlValueParseResult parsed = ReadMetadataUInt(part, id, "webPublishItem", "id");
                            item.Id = parsed.UnsignedIntValue;
                            item.HasId = parsed.IsValid;
                        }
                        item.DivisionId = NormalizeWorksheetText(reader.GetAttribute("divId"));
                        item.SourceType = NormalizeWorksheetText(reader.GetAttribute("sourceType"));
                        item.SourceReference = NormalizeWorksheetText(reader.GetAttribute("sourceRef"));
                        item.SourceObject = NormalizeWorksheetText(reader.GetAttribute("sourceObject"));
                        item.DestinationFile = NormalizeWorksheetText(reader.GetAttribute("destinationFile"));
                        item.Title = NormalizeWorksheetText(reader.GetAttribute("title"));
                        string? autoRepublish = reader.GetAttribute("autoRepublish");
                        if (autoRepublish != null)
                        {
                            XlsxXmlValueParseResult parsed = ReadMetadataBoolean(part, autoRepublish, "webPublishItem", "autoRepublish");
                            item.AutoRepublish = parsed.BooleanValue;
                            item.HasAutoRepublish = parsed.IsValid;
                        }
                        ReadMetadataUnknownAttributes(reader, item.UnknownAttributes, "|id|divId|sourceType|sourceRef|sourceObject|destinationFile|title|autoRepublish|");
                        part.WebPublishItems.Add(item);
                        reader.ReadOuterXml();
                        continue;
                    }
                    reader.Read();
                }
            }
        }
    }

    internal static XlsxXmlValueParseResult ReadMetadataBoolean(XlsxWorksheetPart part, string text, string elementName, string attributeName)
    {
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseBoolean(text);
        AddMetadataDiagnostics(part, parsed.Validation, elementName, attributeName);
        return parsed;
    }

    internal static XlsxXmlValueParseResult ReadMetadataUInt(XlsxWorksheetPart part, string text, string elementName, string attributeName)
    {
        XlsxXmlValueParseResult parsed = XlsxXmlValueParser.ParseUnsignedInteger(text);
        AddMetadataDiagnostics(part, parsed.Validation, elementName, attributeName);
        return parsed;
    }

    internal static void AddMetadataDiagnostics(XlsxWorksheetPart part, XlsxValidationResult validation, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, elementName, attributeName);
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void ReadMetadataUnknownAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes, string knownNames)
    {
        if (!reader.HasAttributes)
        {
            return;
        }
        while (reader.MoveToNextAttribute())
        {
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            bool known = reader.NamespaceURI == "" && knownNames.IndexOf("|" + reader.LocalName + "|", StringComparison.Ordinal) >= 0;
            if (!namespaceDeclaration && !known)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }
        reader.MoveToElement();
    }

    internal static void ReadSheetViews(XlsxWorksheetPart part, XmlReader reader)
    {
        int depth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "sheetView")
            {
                ReadSheetView(part, reader);
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

    internal static void ReadSheetView(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetView view = new XlsxWorksheetView();
        view.View = reader.GetAttribute("view");
        SetOptionalUInt(part, reader.GetAttribute("workbookViewId"), "sheetView", "workbookViewId", view, 1);
        SetOptionalBool(part, reader.GetAttribute("showGridLines"), "sheetView", "showGridLines", view, 1);
        SetOptionalBool(part, reader.GetAttribute("showRowColHeaders"), "sheetView", "showRowColHeaders", view, 2);
        SetOptionalBool(part, reader.GetAttribute("rightToLeft"), "sheetView", "rightToLeft", view, 3);
        SetOptionalBool(part, reader.GetAttribute("tabSelected"), "sheetView", "tabSelected", view, 4);
        SetOptionalUInt(part, reader.GetAttribute("zoomScale"), "sheetView", "zoomScale", view, 2);
        ReadUnknownSheetViewAttributes(reader, view.UnknownAttributes);
        if (reader.IsEmptyElement)
        {
            part.SheetViews.Add(view);
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
                part.SheetViews.Add(view);
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && IsWorksheetNamespace(part, reader.NamespaceURI))
            {
                if (reader.LocalName == "pane")
                {
                    view.Pane = ReadPane(part, reader);
                    view.HasPane = true;
                    continue;
                }

                if (reader.LocalName == "selection")
                {
                    view.Selections.Add(ReadSelection(part, reader));
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                view.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static XlsxWorksheetPane ReadPane(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetPane pane = new XlsxWorksheetPane();
        SetOptionalDouble(part, reader.GetAttribute("xSplit"), "pane", "xSplit", pane, 1);
        SetOptionalDouble(part, reader.GetAttribute("ySplit"), "pane", "ySplit", pane, 2);
        pane.TopLeftCell = NormalizeWorksheetText(reader.GetAttribute("topLeftCell"));
        pane.ActivePane = NormalizeWorksheetText(reader.GetAttribute("activePane"));
        pane.State = NormalizeWorksheetText(reader.GetAttribute("state"));
        ReadUnknownPaneAttributes(reader, pane.UnknownAttributes);
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return pane;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return pane;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                pane.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }

        return pane;
    }

    internal static XlsxWorksheetSelection ReadSelection(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetSelection selection = new XlsxWorksheetSelection();
        selection.Pane = NormalizeWorksheetText(reader.GetAttribute("pane"));
        selection.ActiveCell = NormalizeWorksheetText(reader.GetAttribute("activeCell"));
        SetOptionalUInt(part, reader.GetAttribute("activeCellId"), "selection", "activeCellId", selection, 1);
        selection.Sqref = NormalizeWorksheetText(reader.GetAttribute("sqref"));
        ReadUnknownSelectionAttributes(reader, selection.UnknownAttributes);
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return selection;
        }

        int depth = reader.Depth;
        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return selection;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                selection.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }

        return selection;
    }

    internal static void ReadFormatProperties(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetFormatProperties format = new XlsxWorksheetFormatProperties();
        SetOptionalUInt(part, reader.GetAttribute("baseColWidth"), "sheetFormatPr", "baseColWidth", format, 1);
        SetOptionalDouble(part, reader.GetAttribute("defaultColWidth"), "sheetFormatPr", "defaultColWidth", format, 1);
        SetOptionalDouble(part, reader.GetAttribute("defaultRowHeight"), "sheetFormatPr", "defaultRowHeight", format, 2);
        SetOptionalBool(part, reader.GetAttribute("customHeight"), "sheetFormatPr", "customHeight", format, 1);
        SetOptionalBool(part, reader.GetAttribute("zeroHeight"), "sheetFormatPr", "zeroHeight", format, 2);
        ReadUnknownFormatAttributes(reader, format.UnknownAttributes);
        part.FormatProperties = format;
        part.HasFormatProperties = true;
        reader.ReadOuterXml();
    }

    internal static void ReadColumns(XlsxWorksheetPart part, XmlReader reader)
    {
        int depth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "col")
            {
                XlsxWorksheetColumn column = new XlsxWorksheetColumn();
                SetOptionalUInt(part, reader.GetAttribute("min"), "col", "min", column, 1);
                SetOptionalUInt(part, reader.GetAttribute("max"), "col", "max", column, 2);
                SetOptionalDouble(part, reader.GetAttribute("width"), "col", "width", column, 1);
                SetOptionalUInt(part, reader.GetAttribute("style"), "col", "style", column, 3);
                SetOptionalBool(part, reader.GetAttribute("hidden"), "col", "hidden", column, 1);
                SetOptionalBool(part, reader.GetAttribute("bestFit"), "col", "bestFit", column, 2);
                SetOptionalBool(part, reader.GetAttribute("customWidth"), "col", "customWidth", column, 3);
                SetOptionalUInt(part, reader.GetAttribute("outlineLevel"), "col", "outlineLevel", column, 4);
                SetOptionalBool(part, reader.GetAttribute("collapsed"), "col", "collapsed", column, 4);
                ReadUnknownColumnAttributes(reader, column.UnknownAttributes);
                part.Columns.Add(column);
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

    internal static void ReadSheetData(XlsxWorksheetPart part, XmlReader reader)
    {
        int depth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "row")
            {
                ReadRow(part, reader);
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

    internal static void ReadRow(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.Spans = reader.GetAttribute("spans");
        SetOptionalUInt(part, reader.GetAttribute("r"), "row", "r", row, 1);
        SetOptionalDouble(part, reader.GetAttribute("ht"), "row", "ht", row, 1);
        SetOptionalUInt(part, reader.GetAttribute("s"), "row", "s", row, 2);
        SetOptionalBool(part, reader.GetAttribute("hidden"), "row", "hidden", row, 1);
        SetOptionalBool(part, reader.GetAttribute("customHeight"), "row", "customHeight", row, 2);
        ReadUnknownRowAttributes(reader, row.UnknownAttributes);
        if (reader.IsEmptyElement)
        {
            part.Rows.Add(row);
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
                part.Rows.Add(row);
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "c")
            {
                ReadCell(part, reader, row);
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

    internal static void ReadCell(XlsxWorksheetPart part, XmlReader reader, XlsxWorksheetRow row)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reader.GetAttribute("r");
        cell.Type = reader.GetAttribute("t");
        SetOptionalUInt(part, reader.GetAttribute("s"), "c", "s", cell, 1);
        ReadUnknownCellAttributes(reader, cell.UnknownAttributes);
        if (reader.IsEmptyElement)
        {
            row.Cells.Add(cell);
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
                row.Cells.Add(cell);
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsWorksheetNamespace(part, reader.NamespaceURI))
            {
                if (reader.LocalName == "v")
                {
                    cell.Value = reader.ReadElementContentAsString();
                    if (cell.HasFormula && cell.FormulaInfo != null)
                    {
                        cell.FormulaInfo.CachedValue = cell.Value;
                        cell.FormulaInfo.CachedType = cell.Type;
                    }

                    continue;
                }
                else if (reader.LocalName == "f")
                {
                    ReadCellFormula(part, reader, cell);
                    continue;
                }
                else if (reader.LocalName == "is")
                {
                    cell.InlineString = XlsxInlineStringReader.ReadInlineStringElement(reader, part.NamespaceUri);
                    cell.InlineStringText = cell.InlineString.Text;
                    cell.HasInlineString = true;
                    continue;
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                cell.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static void ReadCellFormula(XlsxWorksheetPart part, XmlReader reader, XlsxWorksheetCell cell)
    {
        XlsxCellFormula formula = new XlsxCellFormula();
        formula.Type = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("t"));
        formula.Reference = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("ref"));
        formula.Input1Reference = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("r1"));
        formula.Input2Reference = XlsxCellValueConverter.NormalizeText(reader.GetAttribute("r2"));
        ReadOptionalCellFormulaUnsignedAttribute(part, reader, "si", formula, 1);
        ReadOptionalCellFormulaBooleanAttribute(part, reader, "aca", formula, 1);
        ReadOptionalCellFormulaBooleanAttribute(part, reader, "ca", formula, 2);
        ReadOptionalCellFormulaBooleanAttribute(part, reader, "dt2D", formula, 3);
        ReadOptionalCellFormulaBooleanAttribute(part, reader, "dtr", formula, 4);
        ReadOptionalCellFormulaBooleanAttribute(part, reader, "del1", formula, 5);
        ReadOptionalCellFormulaBooleanAttribute(part, reader, "del2", formula, 6);
        ReadOptionalCellFormulaBooleanAttribute(part, reader, "bx", formula, 7);
        ReadOptionalCellFormulaBooleanAttribute(part, reader, "dynamicArray", formula, 8);
        ReadUnknownCellFormulaAttributes(reader, formula.UnknownAttributes);
        if (reader.IsEmptyElement)
        {
            formula.Text = "";
            reader.Read();
        }
        else
        {
            formula.Text = reader.ReadElementContentAsString();
        }

        formula.CachedValue = cell.Value;
        formula.CachedType = cell.Type;
        cell.FormulaInfo = formula;
        cell.HasFormula = true;
        cell.Formula = formula.Text;
    }

    internal static void ReadOptionalCellFormulaUnsignedAttribute(XlsxWorksheetPart part, XmlReader reader, string attributeName, XlsxCellFormula formula, int target)
    {
        string? text = reader.GetAttribute(attributeName);
        XlsxXmlValueParseResult result = ParseOptionalUInt(part, text, "f", attributeName);
        if (!result.IsValid || text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            formula.SharedIndex = result.UnsignedIntValue;
            formula.HasSharedIndex = true;
        }
    }

    internal static void ReadOptionalCellFormulaBooleanAttribute(XlsxWorksheetPart part, XmlReader reader, string attributeName, XlsxCellFormula formula, int target)
    {
        string? text = reader.GetAttribute(attributeName);
        XlsxXmlValueParseResult result = ParseOptionalBool(part, text, "f", attributeName);
        if (!result.IsValid || text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            formula.AlwaysCalculateArray = result.BooleanValue;
            formula.HasAlwaysCalculateArray = true;
        }
        else if (target == 2)
        {
            formula.CalculateCell = result.BooleanValue;
            formula.HasCalculateCell = true;
        }
        else if (target == 3)
        {
            formula.DataTable2D = result.BooleanValue;
            formula.HasDataTable2D = true;
        }
        else if (target == 4)
        {
            formula.DataTableRow = result.BooleanValue;
            formula.HasDataTableRow = true;
        }
        else if (target == 5)
        {
            formula.Input1Deleted = result.BooleanValue;
            formula.HasInput1Deleted = true;
        }
        else if (target == 6)
        {
            formula.Input2Deleted = result.BooleanValue;
            formula.HasInput2Deleted = true;
        }
        else if (target == 7)
        {
            formula.AssignsToName = result.BooleanValue;
            formula.HasAssignsToName = true;
        }
        else if (target == 8)
        {
            formula.DynamicArray = result.BooleanValue;
            formula.HasDynamicArray = true;
        }
    }

    internal static string ReadInlineStringText(XlsxWorksheetPart part, XmlReader reader)
    {
        StringBuilder builder = new StringBuilder();
        int depth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return "";
        }

        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return builder.ToString();
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "t" && IsWorksheetNamespace(part, reader.NamespaceURI))
            {
                builder.Append(reader.ReadElementContentAsString());
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                reader.ReadOuterXml();
                continue;
            }

            reader.Read();
        }

        return builder.ToString();
    }

    internal static void ReadMergeCells(XlsxWorksheetPart part, XmlReader reader)
    {
        int depth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "mergeCell")
            {
                XlsxWorksheetMergeCell mergeCell = new XlsxWorksheetMergeCell();
                mergeCell.Reference = reader.GetAttribute("ref");
                part.MergedCells.Add(mergeCell);
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

    internal static void ReadHyperlinks(XlsxWorksheetPart part, XmlReader reader)
    {
        int depth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.Read();
        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                reader.Read();
                return;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && reader.LocalName == "hyperlink")
            {
                XlsxWorksheetHyperlink hyperlink = new XlsxWorksheetHyperlink();
                hyperlink.Reference = reader.GetAttribute("ref");
                hyperlink.RelationshipId = XlsxRelationshipTypeCompatibility.GetRelationshipAttribute(reader, "id");
                hyperlink.Location = reader.GetAttribute("location");
                hyperlink.Display = reader.GetAttribute("display");
                hyperlink.Tooltip = reader.GetAttribute("tooltip");
                ReadUnknownHyperlinkAttributes(reader, hyperlink.UnknownAttributes);
                part.Hyperlinks.Add(hyperlink);
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

    internal static void ReadPageMargins(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetPageMargins margins = new XlsxWorksheetPageMargins();
        SetOptionalDouble(part, reader.GetAttribute("left"), "pageMargins", "left", margins, 1);
        SetOptionalDouble(part, reader.GetAttribute("right"), "pageMargins", "right", margins, 2);
        SetOptionalDouble(part, reader.GetAttribute("top"), "pageMargins", "top", margins, 3);
        SetOptionalDouble(part, reader.GetAttribute("bottom"), "pageMargins", "bottom", margins, 4);
        SetOptionalDouble(part, reader.GetAttribute("header"), "pageMargins", "header", margins, 5);
        SetOptionalDouble(part, reader.GetAttribute("footer"), "pageMargins", "footer", margins, 6);
        part.PageMargins = margins;
        part.HasPageMargins = true;
        reader.ReadOuterXml();
    }

    internal static void ReadPageSetup(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetPageSetup setup = new XlsxWorksheetPageSetup();
        setup.Orientation = reader.GetAttribute("orientation");
        SetOptionalUInt(part, reader.GetAttribute("paperSize"), "pageSetup", "paperSize", setup, 1);
        SetOptionalUInt(part, reader.GetAttribute("scale"), "pageSetup", "scale", setup, 2);
        SetOptionalUInt(part, reader.GetAttribute("fitToWidth"), "pageSetup", "fitToWidth", setup, 3);
        SetOptionalUInt(part, reader.GetAttribute("fitToHeight"), "pageSetup", "fitToHeight", setup, 4);
        SetOptionalUInt(part, reader.GetAttribute("firstPageNumber"), "pageSetup", "firstPageNumber", setup, 5);
        SetOptionalBool(part, reader.GetAttribute("useFirstPageNumber"), "pageSetup", "useFirstPageNumber", setup, 1);
        ReadUnknownPageSetupAttributes(reader, setup.UnknownAttributes);
        part.PageSetup = setup;
        part.HasPageSetup = true;
        reader.ReadOuterXml();
    }

    internal static void ReadPrintOptions(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetPrintOptions options = new XlsxWorksheetPrintOptions();
        SetOptionalBool(part, reader.GetAttribute("horizontalCentered"), "printOptions", "horizontalCentered", options, 1);
        SetOptionalBool(part, reader.GetAttribute("verticalCentered"), "printOptions", "verticalCentered", options, 2);
        SetOptionalBool(part, reader.GetAttribute("headings"), "printOptions", "headings", options, 3);
        SetOptionalBool(part, reader.GetAttribute("gridLines"), "printOptions", "gridLines", options, 4);
        part.PrintOptions = options;
        part.HasPrintOptions = true;
        reader.ReadOuterXml();
    }

    internal static void ReadHeaderFooter(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetHeaderFooter headerFooter = new XlsxWorksheetHeaderFooter();
        SetOptionalBool(part, reader.GetAttribute("differentOddEven"), "headerFooter", "differentOddEven", headerFooter, 1);
        SetOptionalBool(part, reader.GetAttribute("differentFirst"), "headerFooter", "differentFirst", headerFooter, 2);
        SetOptionalBool(part, reader.GetAttribute("scaleWithDoc"), "headerFooter", "scaleWithDoc", headerFooter, 3);
        SetOptionalBool(part, reader.GetAttribute("alignWithMargins"), "headerFooter", "alignWithMargins", headerFooter, 4);
        ReadUnknownHeaderFooterAttributes(reader, headerFooter.UnknownAttributes);
        part.HeaderFooter = headerFooter;
        part.HasHeaderFooter = true;
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

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsWorksheetNamespace(part, reader.NamespaceURI))
            {
                ReadHeaderFooterText(headerFooter, reader);
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                headerFooter.UnknownXml.Add(reader.ReadOuterXml());
                continue;
            }

            reader.Read();
        }
    }

    internal static void ReadHeaderFooterText(XlsxWorksheetHeaderFooter headerFooter, XmlReader reader)
    {
        string localName = reader.LocalName;
        string text = reader.ReadElementContentAsString();
        if (localName == "oddHeader")
        {
            headerFooter.OddHeader = text;
        }
        else if (localName == "oddFooter")
        {
            headerFooter.OddFooter = text;
        }
        else if (localName == "evenHeader")
        {
            headerFooter.EvenHeader = text;
        }
        else if (localName == "evenFooter")
        {
            headerFooter.EvenFooter = text;
        }
        else if (localName == "firstHeader")
        {
            headerFooter.FirstHeader = text;
        }
        else if (localName == "firstFooter")
        {
            headerFooter.FirstFooter = text;
        }
    }

    internal static void ReadPageBreaks(XlsxWorksheetPart part, XmlReader reader, List<XlsxWorksheetPageBreak> breaks)
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

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsWorksheetNamespace(part, reader.NamespaceURI) && reader.LocalName == "brk")
            {
                breaks.Add(ReadPageBreak(part, reader));
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

    internal static XlsxWorksheetPageBreak ReadPageBreak(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetPageBreak pageBreak = new XlsxWorksheetPageBreak();
        SetOptionalUInt(part, reader.GetAttribute("id"), "brk", "id", pageBreak, 1);
        SetOptionalUInt(part, reader.GetAttribute("min"), "brk", "min", pageBreak, 2);
        SetOptionalUInt(part, reader.GetAttribute("max"), "brk", "max", pageBreak, 3);
        SetOptionalBool(part, reader.GetAttribute("man"), "brk", "man", pageBreak, 1);
        ReadUnknownPageBreakAttributes(reader, pageBreak.UnknownAttributes);
        reader.ReadOuterXml();
        return pageBreak;
    }

    internal static void ReadTableReferences(XlsxWorksheetPart part, XmlReader reader)
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

            if (reader.NodeType == XmlNodeType.Element && reader.Depth == depth + 1 && IsWorksheetNamespace(part, reader.NamespaceURI) && reader.LocalName == "tablePart")
            {
                XlsxWorksheetTableReference tableReference = new XlsxWorksheetTableReference();
                tableReference.RelationshipId = XlsxCellValueConverter.NormalizeText(XlsxRelationshipTypeCompatibility.GetRelationshipAttribute(reader, "id"));
                ReadUnknownTableReferenceAttributes(reader, tableReference.UnknownAttributes);
                part.TableReferences.Add(tableReference);
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

    internal static void ReadUnknownTableReferenceAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool relationshipId = XlsxRelationshipTypeCompatibility.IsRelationshipNamespace(reader.NamespaceURI) && reader.LocalName == "id";
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!relationshipId && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static void ReadPivotTableReference(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetPivotTableReference pivotTableReference = new XlsxWorksheetPivotTableReference();
        pivotTableReference.RelationshipId = NormalizeWorksheetText(XlsxRelationshipTypeCompatibility.GetRelationshipAttribute(reader, "id"));
        ReadUnknownPivotTableReferenceAttributes(reader, pivotTableReference.UnknownAttributes);
        part.PivotTableReferences.Add(pivotTableReference);
        reader.ReadOuterXml();
    }

    internal static void ReadUnknownPivotTableReferenceAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
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

    internal static void ReadSheetProtection(XlsxWorksheetPart part, XmlReader reader)
    {
        XlsxWorksheetProtection protection = new XlsxWorksheetProtection();
        protection.Password = reader.GetAttribute("password");
        protection.AlgorithmName = reader.GetAttribute("algorithmName");
        protection.HashValue = reader.GetAttribute("hashValue");
        protection.SaltValue = reader.GetAttribute("saltValue");
        SetOptionalBool(part, reader.GetAttribute("sheet"), "sheetProtection", "sheet", protection, 1);
        SetOptionalBool(part, reader.GetAttribute("objects"), "sheetProtection", "objects", protection, 2);
        SetOptionalBool(part, reader.GetAttribute("scenarios"), "sheetProtection", "scenarios", protection, 3);
        SetOptionalBool(part, reader.GetAttribute("formatCells"), "sheetProtection", "formatCells", protection, 4);
        SetOptionalBool(part, reader.GetAttribute("formatColumns"), "sheetProtection", "formatColumns", protection, 5);
        SetOptionalBool(part, reader.GetAttribute("formatRows"), "sheetProtection", "formatRows", protection, 6);
        SetOptionalBool(part, reader.GetAttribute("insertColumns"), "sheetProtection", "insertColumns", protection, 7);
        SetOptionalBool(part, reader.GetAttribute("insertRows"), "sheetProtection", "insertRows", protection, 8);
        SetOptionalBool(part, reader.GetAttribute("insertHyperlinks"), "sheetProtection", "insertHyperlinks", protection, 9);
        SetOptionalBool(part, reader.GetAttribute("deleteColumns"), "sheetProtection", "deleteColumns", protection, 10);
        SetOptionalBool(part, reader.GetAttribute("deleteRows"), "sheetProtection", "deleteRows", protection, 11);
        SetOptionalBool(part, reader.GetAttribute("selectLockedCells"), "sheetProtection", "selectLockedCells", protection, 12);
        SetOptionalBool(part, reader.GetAttribute("sort"), "sheetProtection", "sort", protection, 13);
        SetOptionalBool(part, reader.GetAttribute("autoFilter"), "sheetProtection", "autoFilter", protection, 14);
        SetOptionalBool(part, reader.GetAttribute("pivotTables"), "sheetProtection", "pivotTables", protection, 15);
        SetOptionalBool(part, reader.GetAttribute("selectUnlockedCells"), "sheetProtection", "selectUnlockedCells", protection, 16);
        SetOptionalUInt(part, reader.GetAttribute("spinCount"), "sheetProtection", "spinCount", protection, 1);
        ReadUnknownSheetProtectionAttributes(reader, protection.UnknownAttributes);
        part.SheetProtection = protection;
        part.HasSheetProtection = true;
        reader.ReadOuterXml();
    }

    internal static bool IsWorksheetNamespace(XlsxWorksheetPart part, string namespaceUri)
    {
        if (namespaceUri == part.NamespaceUri)
        {
            return true;
        }

        return XlsxWorkbookPartReader.IsSpreadsheetNamespace(namespaceUri);
    }

    internal static void SetOptionalUInt(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetView view, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            view.WorkbookViewId = result.UnsignedIntValue;
            view.HasWorkbookViewId = true;
        }
        else if (target == 2)
        {
            view.ZoomScale = result.UnsignedIntValue;
            view.HasZoomScale = true;
        }
    }

    internal static void SetOptionalUInt(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetSelection selection, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            selection.ActiveCellId = result.UnsignedIntValue;
            selection.HasActiveCellId = true;
        }
    }

    internal static void SetOptionalUInt(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetFormatProperties format, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            format.BaseColumnWidth = result.UnsignedIntValue;
            format.HasBaseColumnWidth = true;
        }
    }

    internal static void SetOptionalUInt(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetColumn column, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            column.Min = result.UnsignedIntValue;
        }
        else if (target == 2)
        {
            column.Max = result.UnsignedIntValue;
        }
        else if (target == 3)
        {
            column.Style = result.UnsignedIntValue;
            column.HasStyle = true;
        }
        else if (target == 4)
        {
            column.OutlineLevel = result.UnsignedIntValue;
            column.HasOutlineLevel = true;
        }
    }

    internal static void SetOptionalUInt(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetRow row, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            row.RowIndex = result.UnsignedIntValue;
        }
        else if (target == 2)
        {
            row.Style = result.UnsignedIntValue;
            row.HasStyle = true;
        }
    }

    internal static void SetOptionalUInt(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetCell cell, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            cell.Style = result.UnsignedIntValue;
            cell.HasStyle = true;
        }
    }

    internal static void SetOptionalUInt(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetPageSetup setup, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            setup.PaperSize = result.UnsignedIntValue;
            setup.HasPaperSize = true;
        }
        else if (target == 2)
        {
            setup.Scale = result.UnsignedIntValue;
            setup.HasScale = true;
        }
        else if (target == 3)
        {
            setup.FitToWidth = result.UnsignedIntValue;
            setup.HasFitToWidth = true;
        }
        else if (target == 4)
        {
            setup.FitToHeight = result.UnsignedIntValue;
            setup.HasFitToHeight = true;
        }
        else if (target == 5)
        {
            setup.FirstPageNumber = result.UnsignedIntValue;
            setup.HasFirstPageNumber = true;
        }
    }

    internal static void SetOptionalUInt(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetPageBreak pageBreak, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            pageBreak.Id = result.UnsignedIntValue;
            pageBreak.HasId = true;
        }
        else if (target == 2)
        {
            pageBreak.Min = result.UnsignedIntValue;
            pageBreak.HasMin = true;
        }
        else if (target == 3)
        {
            pageBreak.Max = result.UnsignedIntValue;
            pageBreak.HasMax = true;
        }
    }

    internal static void SetOptionalUInt(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetProtection protection, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalUInt(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            protection.SpinCount = result.UnsignedIntValue;
            protection.HasSpinCount = true;
        }
    }

    internal static void SetOptionalDouble(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetFormatProperties format, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalDouble(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            format.DefaultColumnWidth = result.DoubleValue;
            format.HasDefaultColumnWidth = true;
        }
        else if (target == 2)
        {
            format.DefaultRowHeight = result.DoubleValue;
            format.HasDefaultRowHeight = true;
        }
    }

    internal static void SetOptionalDouble(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetColumn column, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalDouble(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            column.Width = result.DoubleValue;
            column.HasWidth = true;
        }
    }

    internal static void SetOptionalDouble(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetPane pane, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalDouble(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            pane.XSplit = result.DoubleValue;
            pane.HasXSplit = true;
        }
        else if (target == 2)
        {
            pane.YSplit = result.DoubleValue;
            pane.HasYSplit = true;
        }
    }

    internal static void SetOptionalDouble(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetRow row, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalDouble(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            row.Height = result.DoubleValue;
            row.HasHeight = true;
        }
    }

    internal static void SetOptionalDouble(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetPageMargins margins, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalDouble(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            margins.Left = result.DoubleValue;
        }
        else if (target == 2)
        {
            margins.Right = result.DoubleValue;
        }
        else if (target == 3)
        {
            margins.Top = result.DoubleValue;
        }
        else if (target == 4)
        {
            margins.Bottom = result.DoubleValue;
        }
        else if (target == 5)
        {
            margins.Header = result.DoubleValue;
        }
        else if (target == 6)
        {
            margins.Footer = result.DoubleValue;
        }
    }

    internal static void SetOptionalBool(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetProperties properties, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            properties.EnableFormatConditionsCalculation = result.BooleanValue;
            properties.HasEnableFormatConditionsCalculation = true;
        }
    }

    internal static void SetOptionalBool(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetView view, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            view.ShowGridLines = result.BooleanValue;
            view.HasShowGridLines = true;
        }
        else if (target == 2)
        {
            view.ShowRowColHeaders = result.BooleanValue;
            view.HasShowRowColHeaders = true;
        }
        else if (target == 3)
        {
            view.RightToLeft = result.BooleanValue;
            view.HasRightToLeft = true;
        }
        else if (target == 4)
        {
            view.TabSelected = result.BooleanValue;
            view.HasTabSelected = true;
        }
    }

    internal static void SetOptionalBool(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetFormatProperties format, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            format.CustomHeight = result.BooleanValue;
            format.HasCustomHeight = true;
        }
        else if (target == 2)
        {
            format.ZeroHeight = result.BooleanValue;
            format.HasZeroHeight = true;
        }
    }

    internal static void SetOptionalBool(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetColumn column, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            column.Hidden = result.BooleanValue;
            column.HasHidden = true;
        }
        else if (target == 2)
        {
            column.BestFit = result.BooleanValue;
            column.HasBestFit = true;
        }
        else if (target == 3)
        {
            column.CustomWidth = result.BooleanValue;
            column.HasCustomWidth = true;
        }
        else if (target == 4)
        {
            column.Collapsed = result.BooleanValue;
            column.HasCollapsed = true;
        }
    }

    internal static void SetOptionalBool(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetRow row, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            row.Hidden = result.BooleanValue;
            row.HasHidden = true;
        }
        else if (target == 2)
        {
            row.CustomHeight = result.BooleanValue;
            row.HasCustomHeight = true;
        }
    }

    internal static void SetOptionalBool(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetPageSetup setup, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            setup.UseFirstPageNumber = result.BooleanValue;
            setup.HasUseFirstPageNumber = true;
        }
    }

    internal static void SetOptionalBool(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetHeaderFooter headerFooter, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            headerFooter.DifferentOddEven = result.BooleanValue;
            headerFooter.HasDifferentOddEven = true;
        }
        else if (target == 2)
        {
            headerFooter.DifferentFirst = result.BooleanValue;
            headerFooter.HasDifferentFirst = true;
        }
        else if (target == 3)
        {
            headerFooter.ScaleWithDoc = result.BooleanValue;
            headerFooter.HasScaleWithDoc = true;
        }
        else if (target == 4)
        {
            headerFooter.AlignWithMargins = result.BooleanValue;
            headerFooter.HasAlignWithMargins = true;
        }
    }

    internal static void SetOptionalBool(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetPageBreak pageBreak, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            pageBreak.Manual = result.BooleanValue;
            pageBreak.HasManual = true;
        }
    }

    internal static void SetOptionalBool(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetPrintOptions options, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            options.HorizontalCentered = result.BooleanValue;
            options.HasHorizontalCentered = true;
        }
        else if (target == 2)
        {
            options.VerticalCentered = result.BooleanValue;
            options.HasVerticalCentered = true;
        }
        else if (target == 3)
        {
            options.Headings = result.BooleanValue;
            options.HasHeadings = true;
        }
        else if (target == 4)
        {
            options.GridLines = result.BooleanValue;
            options.HasGridLines = true;
        }
    }

    internal static void SetOptionalBool(XlsxWorksheetPart part, string? text, string elementName, string attributeName, XlsxWorksheetProtection protection, int target)
    {
        XlsxXmlValueParseResult result = ParseOptionalBool(part, text, elementName, attributeName);
        if (!result.IsValid)
        {
            return;
        }

        if (text == null || text.Length == 0)
        {
            return;
        }

        if (target == 1)
        {
            protection.Sheet = result.BooleanValue;
            protection.HasSheet = true;
        }
        else if (target == 2)
        {
            protection.Objects = result.BooleanValue;
            protection.HasObjects = true;
        }
        else if (target == 3)
        {
            protection.Scenarios = result.BooleanValue;
            protection.HasScenarios = true;
        }
        else if (target == 4)
        {
            protection.FormatCells = result.BooleanValue;
            protection.HasFormatCells = true;
        }
        else if (target == 5)
        {
            protection.FormatColumns = result.BooleanValue;
            protection.HasFormatColumns = true;
        }
        else if (target == 6)
        {
            protection.FormatRows = result.BooleanValue;
            protection.HasFormatRows = true;
        }
        else if (target == 7)
        {
            protection.InsertColumns = result.BooleanValue;
            protection.HasInsertColumns = true;
        }
        else if (target == 8)
        {
            protection.InsertRows = result.BooleanValue;
            protection.HasInsertRows = true;
        }
        else if (target == 9)
        {
            protection.InsertHyperlinks = result.BooleanValue;
            protection.HasInsertHyperlinks = true;
        }
        else if (target == 10)
        {
            protection.DeleteColumns = result.BooleanValue;
            protection.HasDeleteColumns = true;
        }
        else if (target == 11)
        {
            protection.DeleteRows = result.BooleanValue;
            protection.HasDeleteRows = true;
        }
        else if (target == 12)
        {
            protection.SelectLockedCells = result.BooleanValue;
            protection.HasSelectLockedCells = true;
        }
        else if (target == 13)
        {
            protection.Sort = result.BooleanValue;
            protection.HasSort = true;
        }
        else if (target == 14)
        {
            protection.AutoFilter = result.BooleanValue;
            protection.HasAutoFilter = true;
        }
        else if (target == 15)
        {
            protection.PivotTables = result.BooleanValue;
            protection.HasPivotTables = true;
        }
        else if (target == 16)
        {
            protection.SelectUnlockedCells = result.BooleanValue;
            protection.HasSelectUnlockedCells = true;
        }
    }

    internal static XlsxXmlValueParseResult ParseOptionalUInt(XlsxWorksheetPart part, string? text, string elementName, string attributeName)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        if (text == null || text.Length == 0)
        {
            return result;
        }

        result = XlsxXmlValueParser.ParseUnsignedInteger(text);
        AddValueDiagnostics(part, result, elementName, attributeName);
        return result;
    }

    internal static XlsxXmlValueParseResult ParseOptionalDouble(XlsxWorksheetPart part, string? text, string elementName, string attributeName)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        if (text == null || text.Length == 0)
        {
            return result;
        }

        result = XlsxXmlValueParser.ParseDouble(text);
        AddValueDiagnostics(part, result, elementName, attributeName);
        return result;
    }

    internal static XlsxXmlValueParseResult ParseOptionalBool(XlsxWorksheetPart part, string? text, string elementName, string attributeName)
    {
        XlsxXmlValueParseResult result = new XlsxXmlValueParseResult();
        if (text == null || text.Length == 0)
        {
            return result;
        }

        result = XlsxXmlValueParser.ParseBoolean(text);
        AddValueDiagnostics(part, result, elementName, attributeName);
        return result;
    }

    internal static void AddValueDiagnostics(XlsxWorksheetPart part, XlsxXmlValueParseResult valueResult, string elementName, string attributeName)
    {
        foreach (XlsxDiagnostic diagnostic in valueResult.Validation.Diagnostics)
        {
            diagnostic.SetLocation(part.PartPath, elementName, attributeName);
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddMergeDiagnostics(XlsxWorksheetPart part, XlsxWorksheetMergeCellsResult mergeResult)
    {
        foreach (XlsxDiagnostic diagnostic in mergeResult.Validation.Diagnostics)
        {
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddHyperlinkDiagnostics(XlsxWorksheetPart part, XlsxWorksheetHyperlinkResult hyperlinkResult)
    {
        foreach (XlsxDiagnostic diagnostic in hyperlinkResult.Validation.Diagnostics)
        {
            part.AddDiagnostic(diagnostic);
        }
    }

    internal static void ReadUnknownAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes, string known1, string known2)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = reader.NamespaceURI == "" && (reader.LocalName == known1 || reader.LocalName == known2);
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static void ReadUnknownSheetViewAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        ReadUnknownAttributesByKind(reader, attributes, 1);
    }

    internal static void ReadUnknownPaneAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        ReadUnknownAttributesByKind(reader, attributes, 9);
    }

    internal static void ReadUnknownSelectionAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        ReadUnknownAttributesByKind(reader, attributes, 10);
    }

    internal static void ReadUnknownFormatAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        ReadUnknownAttributesByKind(reader, attributes, 2);
    }

    internal static void ReadUnknownColumnAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        ReadUnknownAttributesByKind(reader, attributes, 3);
    }

    internal static void ReadUnknownRowAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        ReadUnknownAttributesByKind(reader, attributes, 4);
    }

    internal static void ReadUnknownCellAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        ReadUnknownAttributesByKind(reader, attributes, 5);
    }

    internal static void ReadUnknownCellFormulaAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        ReadUnknownAttributesByKind(reader, attributes, 13);
    }

    internal static void ReadUnknownHyperlinkAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        ReadUnknownAttributesByKind(reader, attributes, 6);
    }

    internal static void ReadUnknownPageSetupAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        ReadUnknownAttributesByKind(reader, attributes, 7);
    }

    internal static void ReadUnknownHeaderFooterAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        ReadUnknownAttributesByKind(reader, attributes, 11);
    }

    internal static void ReadUnknownPageBreakAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        ReadUnknownAttributesByKind(reader, attributes, 12);
    }

    internal static void ReadUnknownSheetProtectionAttributes(XmlReader reader, List<XlsxXmlAttribute> attributes)
    {
        ReadUnknownAttributesByKind(reader, attributes, 8);
    }

    internal static void ReadUnknownAttributesByKind(XmlReader reader, List<XlsxXmlAttribute> attributes, int kind)
    {
        if (!reader.HasAttributes)
        {
            return;
        }

        while (reader.MoveToNextAttribute())
        {
            bool known = IsKnownAttribute(reader, kind);
            bool namespaceDeclaration = reader.Prefix == "xmlns" || reader.Name == "xmlns";
            if (!known && !namespaceDeclaration)
            {
                attributes.Add(XlsxMarkupCompatibility.CreateAttributeFromReader(reader));
            }
        }

        reader.MoveToElement();
    }

    internal static bool IsKnownAttribute(XmlReader reader, int kind)
    {
        if (kind == 6 && XlsxRelationshipTypeCompatibility.IsRelationshipNamespace(reader.NamespaceURI) && reader.LocalName == "id")
        {
            return true;
        }

        if (reader.NamespaceURI != "")
        {
            return false;
        }

        if (kind == 1)
        {
            return reader.LocalName == "workbookViewId" || reader.LocalName == "view" || reader.LocalName == "showGridLines" || reader.LocalName == "showRowColHeaders" || reader.LocalName == "rightToLeft" || reader.LocalName == "tabSelected" || reader.LocalName == "zoomScale";
        }
        else if (kind == 2)
        {
            return reader.LocalName == "baseColWidth" || reader.LocalName == "defaultColWidth" || reader.LocalName == "defaultRowHeight" || reader.LocalName == "customHeight" || reader.LocalName == "zeroHeight";
        }
        else if (kind == 3)
        {
            return reader.LocalName == "min" || reader.LocalName == "max" || reader.LocalName == "width" || reader.LocalName == "style" || reader.LocalName == "hidden" || reader.LocalName == "bestFit" || reader.LocalName == "customWidth" || reader.LocalName == "outlineLevel" || reader.LocalName == "collapsed";
        }
        else if (kind == 4)
        {
            return reader.LocalName == "r" || reader.LocalName == "spans" || reader.LocalName == "ht" || reader.LocalName == "hidden" || reader.LocalName == "customHeight" || reader.LocalName == "s";
        }
        else if (kind == 5)
        {
            return reader.LocalName == "r" || reader.LocalName == "t" || reader.LocalName == "s";
        }
        else if (kind == 6)
        {
            return reader.LocalName == "ref" || reader.LocalName == "location" || reader.LocalName == "display" || reader.LocalName == "tooltip";
        }
        else if (kind == 7)
        {
            return reader.LocalName == "paperSize" || reader.LocalName == "orientation" || reader.LocalName == "scale" || reader.LocalName == "fitToWidth" || reader.LocalName == "fitToHeight" || reader.LocalName == "firstPageNumber" || reader.LocalName == "useFirstPageNumber";
        }
        else if (kind == 8)
        {
            return reader.LocalName == "sheet" || reader.LocalName == "objects" || reader.LocalName == "scenarios" || reader.LocalName == "formatCells" || reader.LocalName == "formatColumns" || reader.LocalName == "formatRows" || reader.LocalName == "insertColumns" || reader.LocalName == "insertRows" || reader.LocalName == "insertHyperlinks" || reader.LocalName == "deleteColumns" || reader.LocalName == "deleteRows" || reader.LocalName == "selectLockedCells" || reader.LocalName == "sort" || reader.LocalName == "autoFilter" || reader.LocalName == "pivotTables" || reader.LocalName == "selectUnlockedCells" || reader.LocalName == "password" || reader.LocalName == "algorithmName" || reader.LocalName == "hashValue" || reader.LocalName == "saltValue" || reader.LocalName == "spinCount";
        }
        else if (kind == 9)
        {
            return reader.LocalName == "xSplit" || reader.LocalName == "ySplit" || reader.LocalName == "topLeftCell" || reader.LocalName == "activePane" || reader.LocalName == "state";
        }
        else if (kind == 10)
        {
            return reader.LocalName == "pane" || reader.LocalName == "activeCell" || reader.LocalName == "activeCellId" || reader.LocalName == "sqref";
        }
        else if (kind == 11)
        {
            return reader.LocalName == "differentOddEven" || reader.LocalName == "differentFirst" || reader.LocalName == "scaleWithDoc" || reader.LocalName == "alignWithMargins";
        }
        else if (kind == 12)
        {
            return reader.LocalName == "id" || reader.LocalName == "min" || reader.LocalName == "max" || reader.LocalName == "man";
        }
        else if (kind == 13)
        {
            return reader.LocalName == "t" || reader.LocalName == "ref" || reader.LocalName == "si" || reader.LocalName == "aca" || reader.LocalName == "ca" || reader.LocalName == "dt2D" || reader.LocalName == "dtr" || reader.LocalName == "del1" || reader.LocalName == "del2" || reader.LocalName == "r1" || reader.LocalName == "r2" || reader.LocalName == "bx" || reader.LocalName == "dynamicArray";
        }

        return false;
    }

    internal static string NormalizeWorksheetText(string? text)
    {
        if (text == null)
        {
            return "";
        }

        return text;
    }
}
