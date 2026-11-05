using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace odf;

internal static class MetadataDocumentModel
{
    internal static void LoadMetadata(OdfDocument document)
    {
        OdfDocumentMetadata metadata = new OdfDocumentMetadata();
        OdfNode? section = FindMetadataSection(document);
        metadata.Node = section;
        if (section == null)
        {
            document.Metadata = metadata;
            return;
        }

        List<string> keywords = new List<string>();
        List<OdfNode> keywordNodes = new List<OdfNode>();
        List<OdfUserDefinedMetadata> userDefined =
            new List<OdfUserDefinedMetadata>();
        List<OdfNode> userDefinedNodes = new List<OdfNode>();
        int index = 0;
        while (index < section.Children.Length)
        {
            OdfNode child = section.Children[index];
            if (IsElement(child, OdfNamespaces.DublinCore, "title"))
            {
                SetScalar(metadata, child, 1);
            }
            else if (IsElement(child, OdfNamespaces.DublinCore, "description"))
            {
                SetScalar(metadata, child, 2);
            }
            else if (IsElement(child, OdfNamespaces.DublinCore, "subject"))
            {
                SetScalar(metadata, child, 3);
            }
            else if (IsElement(child, OdfNamespaces.DublinCore, "creator"))
            {
                SetScalar(metadata, child, 4);
            }
            else if (IsElement(child, OdfNamespaces.DublinCore, "date"))
            {
                SetScalar(metadata, child, 5);
            }
            else if (IsElement(child, OdfNamespaces.DublinCore, "language"))
            {
                SetScalar(metadata, child, 6);
            }
            else if (IsElement(child, OdfNamespaces.Metadata, "generator"))
            {
                SetScalar(metadata, child, 7);
            }
            else if (IsElement(child, OdfNamespaces.Metadata, "initial-creator"))
            {
                SetScalar(metadata, child, 8);
            }
            else if (IsElement(child, OdfNamespaces.Metadata, "creation-date"))
            {
                SetScalar(metadata, child, 9);
            }
            else if (IsElement(child, OdfNamespaces.Metadata, "printed-by"))
            {
                SetScalar(metadata, child, 10);
            }
            else if (IsElement(child, OdfNamespaces.Metadata, "print-date"))
            {
                SetScalar(metadata, child, 11);
            }
            else if (IsElement(child, OdfNamespaces.Metadata, "editing-duration"))
            {
                SetScalar(metadata, child, 12);
            }
            else if (IsElement(child, OdfNamespaces.Metadata, "editing-cycles"))
            {
                SetScalar(metadata, child, 13);
            }
            else if (IsElement(child, OdfNamespaces.Metadata, "keyword"))
            {
                keywords.Add(ReadText(child));
                keywordNodes.Add(child);
            }
            else if (IsElement(child, OdfNamespaces.Metadata, "template"))
            {
                LoadTemplate(metadata.Template, child);
            }
            else if (IsElement(child, OdfNamespaces.Metadata, "auto-reload"))
            {
                LoadAutoReload(metadata.AutoReload, child);
            }
            else if (IsElement(child, OdfNamespaces.Metadata, "hyperlink-behaviour"))
            {
                LoadHyperlinkBehavior(metadata.HyperlinkBehavior, child);
            }
            else if (IsElement(child, OdfNamespaces.Metadata, "user-defined"))
            {
                userDefined.Add(LoadUserDefined(child));
                userDefinedNodes.Add(child);
            }
            else if (IsElement(child, OdfNamespaces.Metadata, "document-statistic"))
            {
                LoadStatistics(metadata.Statistics, child);
            }
            index = index + 1;
        }
        metadata.Keywords = keywords.ToArray();
        metadata.KeywordNodes = keywordNodes.ToArray();
        metadata.UserDefined = userDefined.ToArray();
        metadata.UserDefinedNodes = userDefinedNodes.ToArray();
        document.Metadata = metadata;
    }

    internal static void SynchronizeMetadata(OdfDocument document)
    {
        OdfDocumentMetadata metadata = document.Metadata;
        if (metadata == null)
        {
            throw new OdfDocumentException("Document metadata is required.");
        }
        OdfNode? section = metadata.Node;
        if (section == null)
        {
            if (!HasContent(metadata))
            {
                return;
            }
            section = CreateMetadataSection(document);
            metadata.Node = section;
        }

        metadata.TitleNode = WriteScalar(section, metadata.TitleNode,
            OdfNamespaces.DublinCore, "title", OdfNamespaces.DublinCorePrefix,
            metadata.Title);
        metadata.DescriptionNode = WriteScalar(section, metadata.DescriptionNode,
            OdfNamespaces.DublinCore, "description", OdfNamespaces.DublinCorePrefix,
            metadata.Description);
        metadata.SubjectNode = WriteScalar(section, metadata.SubjectNode,
            OdfNamespaces.DublinCore, "subject", OdfNamespaces.DublinCorePrefix,
            metadata.Subject);
        metadata.CreatorNode = WriteScalar(section, metadata.CreatorNode,
            OdfNamespaces.DublinCore, "creator", OdfNamespaces.DublinCorePrefix,
            metadata.Creator);
        metadata.DateNode = WriteScalar(section, metadata.DateNode,
            OdfNamespaces.DublinCore, "date", OdfNamespaces.DublinCorePrefix,
            metadata.Date);
        metadata.LanguageNode = WriteScalar(section, metadata.LanguageNode,
            OdfNamespaces.DublinCore, "language", OdfNamespaces.DublinCorePrefix,
            metadata.Language);
        metadata.GeneratorNode = WriteScalar(section, metadata.GeneratorNode,
            OdfNamespaces.Metadata, "generator", OdfNamespaces.MetadataPrefix,
            metadata.Generator);
        metadata.InitialCreatorNode = WriteScalar(section, metadata.InitialCreatorNode,
            OdfNamespaces.Metadata, "initial-creator", OdfNamespaces.MetadataPrefix,
            metadata.InitialCreator);
        metadata.CreationDateNode = WriteScalar(section, metadata.CreationDateNode,
            OdfNamespaces.Metadata, "creation-date", OdfNamespaces.MetadataPrefix,
            metadata.CreationDate);
        metadata.PrintedByNode = WriteScalar(section, metadata.PrintedByNode,
            OdfNamespaces.Metadata, "printed-by", OdfNamespaces.MetadataPrefix,
            metadata.PrintedBy);
        metadata.PrintDateNode = WriteScalar(section, metadata.PrintDateNode,
            OdfNamespaces.Metadata, "print-date", OdfNamespaces.MetadataPrefix,
            metadata.PrintDate);
        metadata.EditingDurationNode = WriteScalar(section,
            metadata.EditingDurationNode, OdfNamespaces.Metadata,
            "editing-duration", OdfNamespaces.MetadataPrefix,
            metadata.EditingDuration);
        if (metadata.EditingCycles >= 0)
        {
            string cycles = metadata.EditingCycles.ToString(
                CultureInfo.InvariantCulture);
            metadata.EditingCyclesNode = WriteScalar(section,
                metadata.EditingCyclesNode, OdfNamespaces.Metadata,
                "editing-cycles", OdfNamespaces.MetadataPrefix, cycles);
        }
        metadata.KeywordNodes = SynchronizeKeywords(
            section, metadata.Keywords, metadata.KeywordNodes);
        SynchronizeTemplate(section, metadata.Template);
        SynchronizeAutoReload(section, metadata.AutoReload);
        SynchronizeHyperlinkBehavior(section, metadata.HyperlinkBehavior);
        metadata.UserDefinedNodes = SynchronizeUserDefined(
            section, metadata.UserDefined, metadata.UserDefinedNodes);
        SynchronizeStatistics(section, metadata.Statistics);
    }

    static void SetScalar(
        OdfDocumentMetadata metadata,
        OdfNode node,
        int field)
    {
        string value = ReadText(node);
        if (field == 1 && metadata.TitleNode == null)
        {
            metadata.Title = value;
            metadata.TitleNode = node;
        }
        else if (field == 2 && metadata.DescriptionNode == null)
        {
            metadata.Description = value;
            metadata.DescriptionNode = node;
        }
        else if (field == 3 && metadata.SubjectNode == null)
        {
            metadata.Subject = value;
            metadata.SubjectNode = node;
        }
        else if (field == 4 && metadata.CreatorNode == null)
        {
            metadata.Creator = value;
            metadata.CreatorNode = node;
        }
        else if (field == 5 && metadata.DateNode == null)
        {
            metadata.Date = value;
            metadata.IsDateValid = IsDate(value);
            metadata.DateNode = node;
        }
        else if (field == 6 && metadata.LanguageNode == null)
        {
            metadata.Language = value;
            metadata.LanguageNode = node;
        }
        else if (field == 7 && metadata.GeneratorNode == null)
        {
            metadata.Generator = value;
            metadata.GeneratorNode = node;
        }
        else if (field == 8 && metadata.InitialCreatorNode == null)
        {
            metadata.InitialCreator = value;
            metadata.InitialCreatorNode = node;
        }
        else if (field == 9 && metadata.CreationDateNode == null)
        {
            metadata.CreationDate = value;
            metadata.IsCreationDateValid = IsDate(value);
            metadata.CreationDateNode = node;
        }
        else if (field == 10 && metadata.PrintedByNode == null)
        {
            metadata.PrintedBy = value;
            metadata.PrintedByNode = node;
        }
        else if (field == 11 && metadata.PrintDateNode == null)
        {
            metadata.PrintDate = value;
            metadata.IsPrintDateValid = IsDate(value);
            metadata.PrintDateNode = node;
        }
        else if (field == 12 && metadata.EditingDurationNode == null)
        {
            metadata.EditingDuration = value;
            metadata.IsEditingDurationValid =
                OdfLexicalParser.ParseDuration(value).IsValid;
            metadata.EditingDurationNode = node;
        }
        else if (field == 13 && metadata.EditingCyclesNode == null)
        {
            OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(value);
            if (parsed.IsValid && parsed.IntegerValue >= 0)
            {
                metadata.EditingCycles = parsed.IntegerValue;
                metadata.IsEditingCyclesValid = true;
            }
            metadata.EditingCyclesNode = node;
        }
    }

    static void LoadTemplate(OdfMetadataTemplate value, OdfNode node)
    {
        if (value.IsPresent)
        {
            return;
        }
        value.IsPresent = true;
        value.Node = node;
        value.Href = Read(node, OdfNamespaces.XLink, "href");
        value.XLinkType = Read(node, OdfNamespaces.XLink, "type");
        value.Actuate = Read(node, OdfNamespaces.XLink, "actuate");
        value.Title = Read(node, OdfNamespaces.XLink, "title");
        value.Date = Read(node, OdfNamespaces.Metadata, "date");
        value.IsDateValid = IsDate(value.Date);
    }

    static void LoadAutoReload(OdfMetadataAutoReload value, OdfNode node)
    {
        if (value.IsPresent)
        {
            return;
        }
        value.IsPresent = true;
        value.Node = node;
        value.Href = Read(node, OdfNamespaces.XLink, "href");
        value.XLinkType = Read(node, OdfNamespaces.XLink, "type");
        value.Show = Read(node, OdfNamespaces.XLink, "show");
        value.Actuate = Read(node, OdfNamespaces.XLink, "actuate");
        value.Delay = Read(node, OdfNamespaces.Metadata, "delay");
        value.IsDelayValid = OdfLexicalParser.ParseDuration(value.Delay).IsValid;
    }

    static void LoadHyperlinkBehavior(
        OdfMetadataHyperlinkBehavior value,
        OdfNode node)
    {
        if (value.IsPresent)
        {
            return;
        }
        value.IsPresent = true;
        value.Node = node;
        value.TargetFrameName = Read(
            node, OdfNamespaces.Office, "target-frame-name");
        value.Show = Read(node, OdfNamespaces.XLink, "show");
    }

    static OdfUserDefinedMetadata LoadUserDefined(OdfNode node)
    {
        OdfUserDefinedMetadata result = new OdfUserDefinedMetadata();
        result.Node = node;
        result.Name = Read(node, OdfNamespaces.Metadata, "name");
        result.ValueType = Read(node, OdfNamespaces.Metadata, "value-type");
        result.Currency = Read(node, OdfNamespaces.Metadata, "currency");
        result.LexicalValue = ReadText(node);
        ParseUserDefined(result);
        RememberUserDefined(result);
        return result;
    }

    static void ParseUserDefined(OdfUserDefinedMetadata result)
    {
        string type = result.ValueType;
        if (type.Length == 0 || type == "string")
        {
            result.Kind = OdfValueKind.String;
            result.StringValue = result.LexicalValue;
            result.IsValid = true;
            return;
        }
        if (type == "float" || type == "percentage" || type == "currency")
        {
            OdfLexicalValue parsed = OdfLexicalParser.ParseFloat(
                result.LexicalValue);
            result.Kind = type == "percentage"
                ? OdfValueKind.Percentage
                : type == "currency"
                    ? OdfValueKind.Currency
                    : OdfValueKind.Float;
            result.NumericValue = parsed.FloatValue;
            result.IsValid = parsed.IsValid;
            return;
        }
        if (type == "date")
        {
            result.Kind = OdfValueKind.Date;
            result.DateValue = result.LexicalValue;
            result.IsValid = IsDate(result.LexicalValue);
            if (OdfLexicalParser.ParseDateTime(result.LexicalValue).IsValid)
            {
                result.Kind = OdfValueKind.DateTime;
            }
            return;
        }
        if (type == "time")
        {
            result.Kind = OdfValueKind.Duration;
            result.DurationValue = result.LexicalValue;
            result.IsValid = OdfLexicalParser.ParseDuration(
                result.LexicalValue).IsValid;
            return;
        }
        if (type == "boolean")
        {
            OdfLexicalValue parsed = OdfLexicalParser.ParseBoolean(
                result.LexicalValue);
            result.Kind = OdfValueKind.Boolean;
            result.BooleanValue = parsed.BooleanValue;
            result.IsValid = parsed.IsValid;
        }
    }

    static void RememberUserDefined(OdfUserDefinedMetadata value)
    {
        value.OriginalKind = value.Kind;
        value.OriginalValueType = value.ValueType;
        value.OriginalLexicalValue = value.LexicalValue;
        value.OriginalStringValue = value.StringValue;
        value.OriginalNumericValue = value.NumericValue;
        value.OriginalCurrency = value.Currency;
        value.OriginalDateValue = value.DateValue;
        value.OriginalDurationValue = value.DurationValue;
        value.OriginalBooleanValue = value.BooleanValue;
    }

    static void LoadStatistics(OdfDocumentStatistics value, OdfNode node)
    {
        if (value.IsPresent)
        {
            return;
        }
        value.IsPresent = true;
        value.Node = node;
        value.PageCount = ReadCount(node, "page-count");
        value.TableCount = ReadCount(node, "table-count");
        value.DrawCount = ReadCount(node, "draw-count");
        value.ImageCount = ReadCount(node, "image-count");
        value.ObjectCount = ReadCount(node, "object-count");
        value.OleObjectCount = ReadCount(node, "ole-object-count");
        value.ParagraphCount = ReadCount(node, "paragraph-count");
        value.WordCount = ReadCount(node, "word-count");
        value.CharacterCount = ReadCount(node, "character-count");
        value.NonWhitespaceCharacterCount = ReadCount(
            node, "non-whitespace-character-count");
        value.RowCount = ReadCount(node, "row-count");
        value.CellCount = ReadCount(node, "cell-count");
        value.FrameCount = ReadCount(node, "frame-count");
        value.SentenceCount = ReadCount(node, "sentence-count");
        value.SyllableCount = ReadCount(node, "syllable-count");
    }

    static long ReadCount(OdfNode node, string localName)
    {
        string lexical = Read(node, OdfNamespaces.Metadata, localName);
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(lexical);
        if (parsed.IsValid && parsed.IntegerValue >= 0)
        {
            return parsed.IntegerValue;
        }
        return -1;
    }

    static void SynchronizeTemplate(
        OdfNode section,
        OdfMetadataTemplate value)
    {
        OdfNode? node = RequireOptionalNode(
            section, value.Node, value.IsPresent, "template");
        value.Node = node;
        if (node == null)
        {
            return;
        }
        WriteOptionalAttribute(node, OdfNamespaces.XLink, "type",
            OdfNamespaces.XLinkPrefix, value.XLinkType);
        WriteOptionalAttribute(node, OdfNamespaces.XLink, "href",
            OdfNamespaces.XLinkPrefix, value.Href);
        WriteOptionalAttribute(node, OdfNamespaces.XLink, "actuate",
            OdfNamespaces.XLinkPrefix, value.Actuate);
        WriteOptionalAttribute(node, OdfNamespaces.XLink, "title",
            OdfNamespaces.XLinkPrefix, value.Title);
        WriteOptionalAttribute(node, OdfNamespaces.Metadata, "date",
            OdfNamespaces.MetadataPrefix, value.Date);
    }

    static void SynchronizeAutoReload(
        OdfNode section,
        OdfMetadataAutoReload value)
    {
        OdfNode? node = RequireOptionalNode(
            section, value.Node, value.IsPresent, "auto-reload");
        value.Node = node;
        if (node == null)
        {
            return;
        }
        WriteOptionalAttribute(node, OdfNamespaces.XLink, "type",
            OdfNamespaces.XLinkPrefix, value.XLinkType);
        WriteOptionalAttribute(node, OdfNamespaces.XLink, "href",
            OdfNamespaces.XLinkPrefix, value.Href);
        WriteOptionalAttribute(node, OdfNamespaces.XLink, "show",
            OdfNamespaces.XLinkPrefix, value.Show);
        WriteOptionalAttribute(node, OdfNamespaces.XLink, "actuate",
            OdfNamespaces.XLinkPrefix, value.Actuate);
        WriteOptionalAttribute(node, OdfNamespaces.Metadata, "delay",
            OdfNamespaces.MetadataPrefix, value.Delay);
    }

    static void SynchronizeHyperlinkBehavior(
        OdfNode section,
        OdfMetadataHyperlinkBehavior value)
    {
        OdfNode? node = RequireOptionalNode(
            section, value.Node, value.IsPresent, "hyperlink-behaviour");
        value.Node = node;
        if (node == null)
        {
            return;
        }
        WriteOptionalAttribute(node, OdfNamespaces.Office,
            "target-frame-name", OdfNamespaces.OfficePrefix,
            value.TargetFrameName);
        WriteOptionalAttribute(node, OdfNamespaces.XLink, "show",
            OdfNamespaces.XLinkPrefix, value.Show);
    }

    static OdfNode[] SynchronizeKeywords(
        OdfNode section,
        string[] values,
        OdfNode[] originalNodes)
    {
        if (values == null)
        {
            throw new OdfDocumentException("Metadata keywords are required.");
        }
        List<OdfNode> nodes = new List<OdfNode>();
        int index = 0;
        while (index < values.Length)
        {
            string value = values[index];
            if (value == null)
            {
                throw new OdfDocumentException("A metadata keyword cannot be null.");
            }
            OdfNode node;
            if (index < originalNodes.Length)
            {
                node = originalNodes[index];
            }
            else
            {
                node = CreateElement(OdfNamespaces.Metadata, "keyword",
                    OdfNamespaces.MetadataPrefix, section.Location.PartPath);
                StyleDocumentModel.AppendNode(section, node);
            }
            WriteText(node, value);
            nodes.Add(node);
            index = index + 1;
        }
        while (index < originalNodes.Length)
        {
            StyleDocumentModel.RemoveNodeFromParent(originalNodes[index]);
            index = index + 1;
        }
        return nodes.ToArray();
    }

    static OdfNode[] SynchronizeUserDefined(
        OdfNode section,
        OdfUserDefinedMetadata[] values,
        OdfNode[] originalNodes)
    {
        if (values == null)
        {
            throw new OdfDocumentException(
                "User-defined metadata values are required.");
        }
        List<OdfNode> retained = new List<OdfNode>();
        int index = 0;
        while (index < values.Length)
        {
            OdfUserDefinedMetadata value = values[index];
            if (value == null || value.Name.Length == 0)
            {
                throw new OdfDocumentException(
                    "User-defined metadata requires a non-empty name.");
            }
            OdfNode? node = value.Node;
            if (node == null || node.Parent != section)
            {
                node = CreateElement(OdfNamespaces.Metadata, "user-defined",
                    OdfNamespaces.MetadataPrefix, section.Location.PartPath);
                StyleDocumentModel.AppendNode(section, node);
                value.Node = node;
            }
            WriteOptionalAttribute(node, OdfNamespaces.Metadata, "name",
                OdfNamespaces.MetadataPrefix, value.Name);
            string valueType = UserDefinedValueType(value);
            WriteOptionalAttribute(node, OdfNamespaces.Metadata, "value-type",
                OdfNamespaces.MetadataPrefix, valueType);
            WriteOptionalAttribute(node, OdfNamespaces.Metadata, "currency",
                OdfNamespaces.MetadataPrefix, value.Currency);
            string lexical = UserDefinedLexical(value);
            WriteText(node, lexical);
            value.LexicalValue = lexical;
            value.ValueType = valueType;
            retained.Add(node);
            RememberUserDefined(value);
            index = index + 1;
        }
        index = 0;
        while (index < originalNodes.Length)
        {
            if (!ContainsNode(retained, originalNodes[index]))
            {
                StyleDocumentModel.RemoveNodeFromParent(originalNodes[index]);
            }
            index = index + 1;
        }
        return retained.ToArray();
    }

    static string UserDefinedValueType(OdfUserDefinedMetadata value)
    {
        if (value.Node != null
            && value.Kind == value.OriginalKind
            && value.ValueType == value.OriginalValueType)
        {
            return value.ValueType;
        }
        if (value.Kind == OdfValueKind.Float || value.Kind == OdfValueKind.Decimal)
        {
            return "float";
        }
        if (value.Kind == OdfValueKind.Percentage)
        {
            return "percentage";
        }
        if (value.Kind == OdfValueKind.Currency)
        {
            return "currency";
        }
        if (value.Kind == OdfValueKind.Date || value.Kind == OdfValueKind.DateTime)
        {
            return "date";
        }
        if (value.Kind == OdfValueKind.Time || value.Kind == OdfValueKind.Duration)
        {
            return "time";
        }
        if (value.Kind == OdfValueKind.Boolean)
        {
            return "boolean";
        }
        return "string";
    }

    static string UserDefinedLexical(OdfUserDefinedMetadata value)
    {
        bool unchanged = value.Kind == value.OriginalKind
            && value.ValueType == value.OriginalValueType
            && value.StringValue == value.OriginalStringValue
            && value.NumericValue == value.OriginalNumericValue
            && value.Currency == value.OriginalCurrency
            && value.DateValue == value.OriginalDateValue
            && value.DurationValue == value.OriginalDurationValue
            && value.BooleanValue == value.OriginalBooleanValue;
        if (value.Node != null && unchanged)
        {
            return value.LexicalValue;
        }
        if (value.Kind == OdfValueKind.Float
            || value.Kind == OdfValueKind.Decimal
            || value.Kind == OdfValueKind.Percentage
            || value.Kind == OdfValueKind.Currency)
        {
            return value.NumericValue.ToString("R", CultureInfo.InvariantCulture);
        }
        if (value.Kind == OdfValueKind.Date || value.Kind == OdfValueKind.DateTime)
        {
            return value.DateValue;
        }
        if (value.Kind == OdfValueKind.Time || value.Kind == OdfValueKind.Duration)
        {
            return value.DurationValue;
        }
        if (value.Kind == OdfValueKind.Boolean)
        {
            return value.BooleanValue ? "true" : "false";
        }
        return value.StringValue;
    }

    static void SynchronizeStatistics(
        OdfNode section,
        OdfDocumentStatistics value)
    {
        OdfNode? node = RequireOptionalNode(section, value.Node,
            value.IsPresent, "document-statistic");
        value.Node = node;
        if (node == null)
        {
            return;
        }
        WriteCount(node, "page-count", value.PageCount);
        WriteCount(node, "table-count", value.TableCount);
        WriteCount(node, "draw-count", value.DrawCount);
        WriteCount(node, "image-count", value.ImageCount);
        WriteCount(node, "object-count", value.ObjectCount);
        WriteCount(node, "ole-object-count", value.OleObjectCount);
        WriteCount(node, "paragraph-count", value.ParagraphCount);
        WriteCount(node, "word-count", value.WordCount);
        WriteCount(node, "character-count", value.CharacterCount);
        WriteCount(node, "non-whitespace-character-count",
            value.NonWhitespaceCharacterCount);
        WriteCount(node, "row-count", value.RowCount);
        WriteCount(node, "cell-count", value.CellCount);
        WriteCount(node, "frame-count", value.FrameCount);
        WriteCount(node, "sentence-count", value.SentenceCount);
        WriteCount(node, "syllable-count", value.SyllableCount);
    }

    static void WriteCount(OdfNode node, string localName, long value)
    {
        if (value < 0)
        {
            return;
        }
        StyleDocumentModel.WriteAttribute(node, OdfNamespaces.Metadata,
            localName, OdfNamespaces.MetadataPrefix,
            value.ToString(CultureInfo.InvariantCulture));
    }

    static OdfNode? RequireOptionalNode(
        OdfNode section,
        OdfNode? node,
        bool isPresent,
        string localName)
    {
        if (!isPresent)
        {
            if (node != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            return null;
        }
        if (node == null || node.Parent != section)
        {
            node = CreateElement(OdfNamespaces.Metadata, localName,
                OdfNamespaces.MetadataPrefix, section.Location.PartPath);
            StyleDocumentModel.AppendNode(section, node);
        }
        return node;
    }

    static OdfNode? WriteScalar(
        OdfNode section,
        OdfNode? node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length == 0 && node == null)
        {
            return null;
        }
        if (node == null || node.Parent != section)
        {
            node = CreateElement(namespaceName, localName, prefix,
                section.Location.PartPath);
            StyleDocumentModel.AppendNode(section, node);
        }
        WriteText(node, value);
        return node;
    }

    static OdfNode CreateMetadataSection(OdfDocument document)
    {
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            OdfPart? flatPart = FindFlatPart(document);
            if (flatPart == null || flatPart.Root == null)
            {
                throw new OdfDocumentException(
                    "The flat document root is not available.");
            }
            EnsureMetadataNamespaces(flatPart.Root);
            OdfNode section = CreateElement(OdfNamespaces.Office, "meta",
                OdfNamespaces.OfficePrefix, flatPart.Root.Location.PartPath);
            InsertBeforeBody(flatPart.Root, section);
            flatPart.Metadata = section;
            flatPart.Sections = ElementChildren(flatPart.Root);
            return section;
        }

        OdfNode root = CreateElement(OdfNamespaces.Office, "document-meta",
            OdfNamespaces.OfficePrefix, OdfPartNames.Metadata);
        root.NamespaceDeclarations = [];
        EnsureMetadataNamespaces(root);
        StyleDocumentModel.WriteAttribute(root, OdfNamespaces.Office,
            "version", OdfNamespaces.OfficePrefix, "1.4");
        OdfNode metadata = CreateElement(OdfNamespaces.Office, "meta",
            OdfNamespaces.OfficePrefix, OdfPartNames.Metadata);
        StyleDocumentModel.AppendNode(root, metadata);
        OdfPart part = OdfDocumentModel.CreateStandalonePart(
            [root], OdfPartNames.Metadata, OdfMediaTypes.Xml);
        OdfPart[] parts = new OdfPart[document.Parts.Length + 1];
        Array.Copy(document.Parts, parts, document.Parts.Length);
        parts[parts.Length - 1] = part;
        document.Parts = parts;
        OdfNode[] nodes = new OdfNode[document.Nodes.Length + 1];
        Array.Copy(document.Nodes, nodes, document.Nodes.Length);
        nodes[nodes.Length - 1] = root;
        document.Nodes = nodes;
        if (document.PartTrees != null)
        {
            document.PartTrees.MetadataNodes = [root];
            document.PartTrees.MetadataRoot = OdfPartRootRules.MapRoot(
                [root], OdfPartNames.Metadata);
        }
        return metadata;
    }

    static void EnsureMetadataNamespaces(OdfNode root)
    {
        EnsureNamespace(root, OdfNamespaces.OfficePrefix, OdfNamespaces.Office);
        EnsureNamespace(root, OdfNamespaces.MetadataPrefix, OdfNamespaces.Metadata);
        EnsureNamespace(root, OdfNamespaces.DublinCorePrefix, OdfNamespaces.DublinCore);
        EnsureNamespace(root, OdfNamespaces.XLinkPrefix, OdfNamespaces.XLink);
    }

    static void EnsureNamespace(
        OdfNode root,
        string prefix,
        string namespaceName)
    {
        int index = 0;
        while (index < root.NamespaceDeclarations.Length)
        {
            if (root.NamespaceDeclarations[index].Prefix == prefix)
            {
                return;
            }
            index = index + 1;
        }
        OdfNamespaceDeclaration declaration = new OdfNamespaceDeclaration();
        declaration.Prefix = prefix;
        declaration.NamespaceName = namespaceName;
        declaration.Location.PartPath = root.Location.PartPath;
        OdfNamespaceDeclaration[] declarations =
            new OdfNamespaceDeclaration[root.NamespaceDeclarations.Length + 1];
        Array.Copy(root.NamespaceDeclarations, declarations,
            root.NamespaceDeclarations.Length);
        declarations[declarations.Length - 1] = declaration;
        root.NamespaceDeclarations = declarations;
    }

    static void InsertBeforeBody(OdfNode root, OdfNode child)
    {
        int insertion = root.Children.Length;
        int index = 0;
        while (index < root.Children.Length)
        {
            if (IsElement(root.Children[index], OdfNamespaces.Office, "body"))
            {
                insertion = index;
                index = root.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        OdfNode[] children = new OdfNode[root.Children.Length + 1];
        Array.Copy(root.Children, 0, children, 0, insertion);
        children[insertion] = child;
        Array.Copy(root.Children, insertion, children, insertion + 1,
            root.Children.Length - insertion);
        root.Children = children;
        child.Parent = root;
        root.WasEmptyElement = false;
    }

    static OdfNode[] ElementChildren(OdfNode root)
    {
        List<OdfNode> result = new List<OdfNode>();
        int index = 0;
        while (index < root.Children.Length)
        {
            if (root.Children[index].Kind == OdfNodeKind.Element)
            {
                result.Add(root.Children[index]);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static bool HasContent(OdfDocumentMetadata value)
    {
        return value.Title.Length > 0
            || value.Description.Length > 0
            || value.Subject.Length > 0
            || value.Creator.Length > 0
            || value.Date.Length > 0
            || value.Language.Length > 0
            || value.Generator.Length > 0
            || value.InitialCreator.Length > 0
            || value.CreationDate.Length > 0
            || value.PrintedBy.Length > 0
            || value.PrintDate.Length > 0
            || value.EditingDuration.Length > 0
            || value.EditingCycles >= 0
            || value.Keywords.Length > 0
            || value.Template.IsPresent
            || value.AutoReload.IsPresent
            || value.HyperlinkBehavior.IsPresent
            || value.UserDefined.Length > 0
            || value.Statistics.IsPresent;
    }

    static OdfNode? FindMetadataSection(OdfDocument document)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            if (document.Parts[index].Metadata != null)
            {
                return document.Parts[index].Metadata;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfPart? FindFlatPart(OdfDocument document)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            if (document.Parts[index].IsFlatDocument)
            {
                return document.Parts[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode CreateElement(
        string namespaceName,
        string localName,
        string prefix,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            namespaceName, localName, prefix);
        result.Location.PartPath = partPath;
        result.WasEmptyElement = true;
        return result;
    }

    static string Read(OdfNode node, string namespaceName, string localName)
    {
        return StyleDocumentModel.ReadAttribute(node, namespaceName, localName);
    }

    static void WriteOptionalAttribute(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(node, namespaceName, localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node, namespaceName, localName, prefix, value);
        }
    }

    static string ReadText(OdfNode node)
    {
        StringBuilder result = new StringBuilder();
        AppendText(node, result);
        return result.ToString();
    }

    static void AppendText(OdfNode node, StringBuilder result)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (child.Kind == OdfNodeKind.Text)
            {
                result.Append(child.Value);
            }
            else if (child.Kind == OdfNodeKind.Element)
            {
                AppendText(child, result);
            }
            index = index + 1;
        }
    }

    static void WriteText(OdfNode node, string value)
    {
        if (ReadText(node) == value && node.Children.Length > 0)
        {
            return;
        }
        if (value.Length == 0)
        {
            node.Children = [];
            node.WasEmptyElement = true;
            return;
        }
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Parent = node;
        text.Location.PartPath = node.Location.PartPath;
        node.Children = [text];
        node.WasEmptyElement = false;
    }

    static bool IsDate(string value)
    {
        return OdfLexicalParser.ParseDate(value).IsValid
            || OdfLexicalParser.ParseDateTime(value).IsValid;
    }

    static bool IsElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName;
    }

    static bool ContainsNode(List<OdfNode> nodes, OdfNode node)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index] == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
