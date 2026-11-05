using System;
using System.Collections.Generic;

namespace odf;

internal class ListStyleDocumentState
{
    internal OdfNode[] OriginalNodes = [];
}

internal static class ListStyleDocumentModel
{
    internal static void LoadListStyles(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for list-style materialization.");
        }
        List<OdfListStyle> styles = new List<OdfListStyle>();
        List<OdfNode> nodes = new List<OdfNode>();
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            LoadListStyleSection(part, part.Styles, false, styles, nodes);
            LoadListStyleSection(
                part,
                part.AutomaticStyles,
                true,
                styles,
                nodes);
            index = index + 1;
        }
        document.ListStyles = styles.ToArray();
        ListStyleDocumentState state = new ListStyleDocumentState();
        state.OriginalNodes = nodes.ToArray();
        document.ListStyleState = state;
        ResolveListStyleRelationships(document);
    }

    internal static void SynchronizeListStyles(OdfDocument document)
    {
        if (document == null || document.ListStyles == null)
        {
            throw new OdfDocumentException(
                "A document and list-style collection are required for writing.");
        }
        ListStyleDocumentState state = document.ListStyleState
            ?? new ListStyleDocumentState();
        RemoveDeletedListStyles(document.ListStyles, state.OriginalNodes);
        List<OdfNode> nodes = new List<OdfNode>();
        int index = 0;
        while (index < document.ListStyles.Length)
        {
            OdfListStyle style = document.ListStyles[index];
            if (style == null)
            {
                throw new OdfDocumentException(
                    "The list-style collection contains a null style.");
            }
            SynchronizeListStyle(document, style);
            if (style.OriginalNode != null)
            {
                nodes.Add(style.OriginalNode);
            }
            index = index + 1;
        }
        state.OriginalNodes = nodes.ToArray();
        document.ListStyleState = state;
        StyleResolver.ResolveRelationships(document);
        ResolveListStyleRelationships(document);
    }

    static void LoadListStyleSection(
        OdfPart part,
        OdfNode? section,
        bool automatic,
        List<OdfListStyle> styles,
        List<OdfNode> nodes)
    {
        if (section == null)
        {
            return;
        }
        int index = 0;
        while (index < section.Children.Length)
        {
            OdfNode node = section.Children[index];
            if (IsListStyleNode(node))
            {
                styles.Add(CreateListStyle(part, node, automatic));
                nodes.Add(node);
            }
            index = index + 1;
        }
    }

    static OdfListStyle CreateListStyle(
        OdfPart part,
        OdfNode node,
        bool automatic)
    {
        OdfListStyle result = new OdfListStyle();
        result.Name = ReadAttributeValue(node, OdfNamespaces.Style, "name");
        result.DisplayName = ReadAttributeValue(
            node,
            OdfNamespaces.Style,
            "display-name");
        result.ConsecutiveNumbering = ReadAttributeValue(
            node,
            OdfNamespaces.Text,
            "consecutive-numbering");
        result.Kind = node.Name != null
                && node.Name.LocalName == "outline-style"
            ? OdfListStyleKind.Outline
            : OdfListStyleKind.List;
        result.PartPath = part.Path;
        result.IsAutomatic = automatic;
        result.Attributes =
            StylePropertyDocumentModel.ReadPropertyAttributes(node);
        result.OriginalAttributes =
            StylePropertyDocumentModel.AttributeNodes(result.Attributes);
        List<OdfListLevel> levels = new List<OdfListLevel>();
        List<OdfNode> passthrough = new List<OdfNode>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsListLevelNode(child))
            {
                levels.Add(CreateListLevel(child));
            }
            else
            {
                passthrough.Add(child);
            }
            index = index + 1;
        }
        result.Levels = levels.ToArray();
        result.PassthroughContent = passthrough.ToArray();
        result.Node = node;
        result.OriginalNode = node;
        SnapshotListStyle(result);
        return result;
    }

    static OdfListLevel CreateListLevel(OdfNode node)
    {
        OdfListLevel result = new OdfListLevel();
        result.Kind = ParseListLevelKind(node);
        result.Level = ReadAttributeValue(node, OdfNamespaces.Text, "level");
        result.StyleName = ReadAttributeValue(
            node,
            OdfNamespaces.Text,
            "style-name");
        result.NumberFormat = ReadAttributeValue(
            node,
            OdfNamespaces.Style,
            "num-format");
        result.NumberPrefix = ReadAttributeValue(
            node,
            OdfNamespaces.Style,
            "num-prefix");
        result.NumberSuffix = ReadAttributeValue(
            node,
            OdfNamespaces.Style,
            "num-suffix");
        result.NumberLetterSync = ReadAttributeValue(
            node,
            OdfNamespaces.Style,
            "num-letter-sync");
        result.NumberListFormatName = ReadAttributeValue(
            node,
            OdfNamespaces.Style,
            "num-list-format-name");
        result.DisplayLevels = ReadAttributeValue(
            node,
            OdfNamespaces.Text,
            "display-levels");
        result.StartValue = ReadAttributeValue(
            node,
            OdfNamespaces.Text,
            "start-value");
        result.BulletCharacter = ReadAttributeValue(
            node,
            OdfNamespaces.Text,
            "bullet-char");
        result.BulletRelativeSize = ReadAttributeValue(
            node,
            OdfNamespaces.Text,
            "bullet-relative-size");
        result.ImageHref = ReadAttributeValue(
            node,
            OdfNamespaces.XLink,
            "href");
        result.Attributes =
            StylePropertyDocumentModel.ReadPropertyAttributes(node);
        result.OriginalAttributes =
            StylePropertyDocumentModel.AttributeNodes(result.Attributes);
        OdfNode? levelProperties = FindStyleChild(
            node,
            "list-level-properties");
        result.LevelPropertiesNode = levelProperties;
        result.OriginalLevelPropertiesNode = levelProperties;
        result.LevelProperties =
            StylePropertyDocumentModel.ReadPropertyAttributes(levelProperties);
        result.OriginalLevelPropertyAttributes =
            StylePropertyDocumentModel.AttributeNodes(result.LevelProperties);
        result.LevelPropertyChildren =
            StylePropertyDocumentModel.PropertyChildren(levelProperties);
        result.OriginalLevelPropertyChildren =
            StylePropertyDocumentModel.CopyNodes(
                result.LevelPropertyChildren);
        result.LabelAlignmentNode = FindStyleChild(
            levelProperties,
            "list-level-label-alignment");
        result.OriginalLabelAlignmentNode = result.LabelAlignmentNode;
        ReadListPositionFields(result);
        OdfNode? textProperties = FindStyleChild(node, "text-properties");
        result.TextPropertiesNode = textProperties;
        result.OriginalTextPropertiesNode = textProperties;
        result.TextProperties =
            StylePropertyDocumentModel.ReadPropertyAttributes(textProperties);
        result.OriginalTextPropertyAttributes =
            StylePropertyDocumentModel.AttributeNodes(result.TextProperties);
        result.TextPropertyChildren =
            StylePropertyDocumentModel.PropertyChildren(textProperties);
        result.OriginalTextPropertyChildren =
            StylePropertyDocumentModel.CopyNodes(result.TextPropertyChildren);
        List<OdfNode> content = new List<OdfNode>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (child != levelProperties && child != textProperties)
            {
                content.Add(child);
            }
            index = index + 1;
        }
        result.Content = content.ToArray();
        result.OriginalContent =
            StylePropertyDocumentModel.CopyNodes(result.Content);
        result.ImageDataNode = FindOfficeChild(result.Content, "binary-data");
        result.OriginalImageDataNode = result.ImageDataNode;
        result.Node = node;
        result.OriginalNode = node;
        SnapshotListLevel(result);
        return result;
    }

    static void SynchronizeListStyle(
        OdfDocument document,
        OdfListStyle style)
    {
        if (style.Attributes == null
            || style.Levels == null
            || style.PassthroughContent == null)
        {
            throw new OdfDocumentException(
                "List-style attribute and content collections are required for writing.");
        }
        OdfNode section = EnsureListStyleSection(document, style);
        OdfNode? node = style.OriginalNode;
        bool isNew = node == null;
        bool contentChanged = isNew || ListStyleContentChanged(style);
        if (node == null)
        {
            node = CreateTextElement(
                FormatListStyleName(style.Kind),
                section.Location.PartPath);
            StyleDocumentModel.AppendNode(section, node);
            style.OriginalNode = node;
        }
        else if (node.Parent != section)
        {
            StyleDocumentModel.RemoveNodeFromParent(node);
            StyleDocumentModel.AppendNode(section, node);
        }
        ApplyListStyleKind(style, node, isNew);
        EnsureElementNamespace(node);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Style,
            "name",
            OdfNamespaces.StylePrefix,
            style.Name,
            style.OriginalName);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Style,
            "display-name",
            OdfNamespaces.StylePrefix,
            style.DisplayName,
            style.OriginalDisplayName);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Text,
            "consecutive-numbering",
            OdfNamespaces.TextPrefix,
            style.ConsecutiveNumbering,
            style.OriginalConsecutiveNumbering);
        StylePropertyDocumentModel.SynchronizeElementAttributes(
            node,
            style.Attributes,
            style.OriginalAttributes);
        style.OriginalAttributes =
            StylePropertyDocumentModel.AttributeNodes(style.Attributes);
        style.Name = ReadAttributeValue(node, OdfNamespaces.Style, "name");
        style.DisplayName = ReadAttributeValue(
            node,
            OdfNamespaces.Style,
            "display-name");
        style.ConsecutiveNumbering = ReadAttributeValue(
            node,
            OdfNamespaces.Text,
            "consecutive-numbering");
        SynchronizeListLevels(style.Levels, node);
        if (contentChanged)
        {
            SynchronizeListStyleChildren(style, node);
        }
        style.Node = node;
        style.PartPath = section.Location.PartPath;
        style.IsAutomatic = StyleDocumentModel.IsAutomaticSection(section);
        SnapshotListStyle(style);
    }

    static void SynchronizeListLevels(
        OdfListLevel[] levels,
        OdfNode parent)
    {
        int index = 0;
        while (index < levels.Length)
        {
            OdfListLevel level = levels[index];
            if (level == null
                || level.Attributes == null
                || level.LevelProperties == null
                || level.LevelPropertyChildren == null
                || level.TextProperties == null
                || level.TextPropertyChildren == null
                || level.Content == null)
            {
                throw new OdfDocumentException(
                    "Complete list-level collections are required for writing.");
            }
            OdfNode? node = level.OriginalNode;
            bool isNew = node == null;
            bool childrenChanged = isNew
                || level.ImageDataNode != level.OriginalImageDataNode;
            if (node == null)
            {
                node = CreateTextElement(
                    FormatListLevelName(level.Kind),
                    parent.Location.PartPath);
                level.OriginalNode = node;
            }
            ApplyListLevelKind(level, node, isNew);
            EnsureElementNamespace(node);
            ApplyListLevelFields(level);
            ApplyListPositionFields(level, parent.Location.PartPath);
            StylePropertyDocumentModel.SynchronizeElementAttributes(
                node,
                level.Attributes,
                level.OriginalAttributes);
            level.OriginalAttributes =
                StylePropertyDocumentModel.AttributeNodes(level.Attributes);
            ReadListLevelFields(level, node);
            if (level.ImageDataNode != level.OriginalImageDataNode)
            {
                level.Content = ReplaceLevelContentNode(
                    level.Content,
                    level.ImageDataNode,
                    level.OriginalImageDataNode,
                    OdfNamespaces.Office,
                    "binary-data");
            }
            if (level.LabelAlignmentNode
                != level.OriginalLabelAlignmentNode)
            {
                level.LevelPropertyChildren = ReplaceLevelContentNode(
                    level.LevelPropertyChildren,
                    level.LabelAlignmentNode,
                    level.OriginalLabelAlignmentNode,
                    OdfNamespaces.Style,
                    "list-level-label-alignment");
            }
            OdfNode? levelProperties =
                StylePropertyDocumentModel.SynchronizePropertySet(
                    node,
                    "list-level-properties",
                    level.LevelProperties,
                    level.LevelPropertyChildren,
                    level.OriginalLevelPropertiesNode,
                    level.OriginalLevelPropertyAttributes,
                    level.OriginalLevelPropertyChildren);
            if (levelProperties != level.OriginalLevelPropertiesNode)
            {
                childrenChanged = true;
            }
            level.LevelPropertiesNode = levelProperties;
            level.OriginalLevelPropertiesNode = levelProperties;
            level.OriginalLevelPropertyAttributes =
                StylePropertyDocumentModel.AttributeNodes(
                    level.LevelProperties);
            level.OriginalLevelPropertyChildren =
                StylePropertyDocumentModel.CopyNodes(
                    level.LevelPropertyChildren);
            OdfNode? textProperties =
                StylePropertyDocumentModel.SynchronizePropertySet(
                    node,
                    "text-properties",
                    level.TextProperties,
                    level.TextPropertyChildren,
                    level.OriginalTextPropertiesNode,
                    level.OriginalTextPropertyAttributes,
                    level.OriginalTextPropertyChildren);
            if (textProperties != level.OriginalTextPropertiesNode)
            {
                childrenChanged = true;
            }
            level.TextPropertiesNode = textProperties;
            level.OriginalTextPropertiesNode = textProperties;
            level.OriginalTextPropertyAttributes =
                StylePropertyDocumentModel.AttributeNodes(
                    level.TextProperties);
            level.OriginalTextPropertyChildren =
                StylePropertyDocumentModel.CopyNodes(
                    level.TextPropertyChildren);
            if (!NodesMatch(level.Content, level.OriginalContent))
            {
                childrenChanged = true;
            }
            if (childrenChanged)
            {
                SynchronizeListLevelChildren(level, node);
            }
            level.Node = node;
            level.LabelAlignmentNode = FindStyleChild(
                levelProperties,
                "list-level-label-alignment");
            ReadListPositionFields(level);
            level.ImageDataNode = FindOfficeChild(
                level.Content,
                "binary-data");
            level.OriginalLabelAlignmentNode = level.LabelAlignmentNode;
            level.OriginalImageDataNode = level.ImageDataNode;
            level.OriginalContent =
                StylePropertyDocumentModel.CopyNodes(level.Content);
            SnapshotListLevel(level);
            index = index + 1;
        }
    }

    static void ApplyListLevelFields(OdfListLevel level)
    {
        level.Attributes = ApplyConvenienceAttribute(level.Attributes, OdfNamespaces.Text, "level", OdfNamespaces.TextPrefix, level.Level, level.OriginalLevel);
        level.Attributes = ApplyConvenienceAttribute(level.Attributes, OdfNamespaces.Text, "style-name", OdfNamespaces.TextPrefix, level.StyleName, level.OriginalStyleName);
        level.Attributes = ApplyConvenienceAttribute(level.Attributes, OdfNamespaces.Style, "num-format", OdfNamespaces.StylePrefix, level.NumberFormat, level.OriginalNumberFormat);
        level.Attributes = ApplyConvenienceAttribute(level.Attributes, OdfNamespaces.Style, "num-prefix", OdfNamespaces.StylePrefix, level.NumberPrefix, level.OriginalNumberPrefix);
        level.Attributes = ApplyConvenienceAttribute(level.Attributes, OdfNamespaces.Style, "num-suffix", OdfNamespaces.StylePrefix, level.NumberSuffix, level.OriginalNumberSuffix);
        level.Attributes = ApplyConvenienceAttribute(level.Attributes, OdfNamespaces.Style, "num-letter-sync", OdfNamespaces.StylePrefix, level.NumberLetterSync, level.OriginalNumberLetterSync);
        level.Attributes = ApplyConvenienceAttribute(level.Attributes, OdfNamespaces.Style, "num-list-format-name", OdfNamespaces.StylePrefix, level.NumberListFormatName, level.OriginalNumberListFormatName);
        level.Attributes = ApplyConvenienceAttribute(level.Attributes, OdfNamespaces.Text, "display-levels", OdfNamespaces.TextPrefix, level.DisplayLevels, level.OriginalDisplayLevels);
        level.Attributes = ApplyConvenienceAttribute(level.Attributes, OdfNamespaces.Text, "start-value", OdfNamespaces.TextPrefix, level.StartValue, level.OriginalStartValue);
        level.Attributes = ApplyConvenienceAttribute(level.Attributes, OdfNamespaces.Text, "bullet-char", OdfNamespaces.TextPrefix, level.BulletCharacter, level.OriginalBulletCharacter);
        level.Attributes = ApplyConvenienceAttribute(level.Attributes, OdfNamespaces.Text, "bullet-relative-size", OdfNamespaces.TextPrefix, level.BulletRelativeSize, level.OriginalBulletRelativeSize);
        level.Attributes = ApplyConvenienceAttribute(level.Attributes, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, level.ImageHref, level.OriginalImageHref);
    }

    static void ReadListLevelFields(OdfListLevel level, OdfNode node)
    {
        level.Level = ReadAttributeValue(node, OdfNamespaces.Text, "level");
        level.StyleName = ReadAttributeValue(node, OdfNamespaces.Text, "style-name");
        level.NumberFormat = ReadAttributeValue(node, OdfNamespaces.Style, "num-format");
        level.NumberPrefix = ReadAttributeValue(node, OdfNamespaces.Style, "num-prefix");
        level.NumberSuffix = ReadAttributeValue(node, OdfNamespaces.Style, "num-suffix");
        level.NumberLetterSync = ReadAttributeValue(node, OdfNamespaces.Style, "num-letter-sync");
        level.NumberListFormatName = ReadAttributeValue(node, OdfNamespaces.Style, "num-list-format-name");
        level.DisplayLevels = ReadAttributeValue(node, OdfNamespaces.Text, "display-levels");
        level.StartValue = ReadAttributeValue(node, OdfNamespaces.Text, "start-value");
        level.BulletCharacter = ReadAttributeValue(node, OdfNamespaces.Text, "bullet-char");
        level.BulletRelativeSize = ReadAttributeValue(node, OdfNamespaces.Text, "bullet-relative-size");
        level.ImageHref = ReadAttributeValue(node, OdfNamespaces.XLink, "href");
    }

    static void ApplyListPositionFields(
        OdfListLevel level,
        string partPath)
    {
        level.LevelProperties = ApplyConvenienceAttribute(
            level.LevelProperties,
            OdfNamespaces.Text,
            "list-level-position-and-space-mode",
            OdfNamespaces.TextPrefix,
            level.PositionAndSpaceMode,
            level.OriginalPositionAndSpaceMode);
        level.LevelProperties = ApplyConvenienceAttribute(
            level.LevelProperties,
            OdfNamespaces.Text,
            "space-before",
            OdfNamespaces.TextPrefix,
            level.SpaceBefore,
            level.OriginalSpaceBefore);
        level.LevelProperties = ApplyConvenienceAttribute(
            level.LevelProperties,
            OdfNamespaces.Text,
            "min-label-width",
            OdfNamespaces.TextPrefix,
            level.MinimumLabelWidth,
            level.OriginalMinimumLabelWidth);
        level.LevelProperties = ApplyConvenienceAttribute(
            level.LevelProperties,
            OdfNamespaces.Text,
            "min-label-distance",
            OdfNamespaces.TextPrefix,
            level.MinimumLabelDistance,
            level.OriginalMinimumLabelDistance);
        if (level.LabelAlignmentNode == null && HasLabelAlignment(level))
        {
            level.LabelAlignmentNode = CreateStyleElement(
                "list-level-label-alignment",
                partPath);
        }
        OdfNode? alignment = level.LabelAlignmentNode;
        if (alignment != null)
        {
            WriteOptionalAttribute(
                alignment,
                OdfNamespaces.Text,
                "label-followed-by",
                OdfNamespaces.TextPrefix,
                level.LabelFollowedBy);
            WriteOptionalAttribute(
                alignment,
                OdfNamespaces.Text,
                "list-tab-stop-position",
                OdfNamespaces.TextPrefix,
                level.ListTabStopPosition);
            WriteOptionalAttribute(
                alignment,
                OdfNamespaces.Fo,
                "text-indent",
                OdfNamespaces.FoPrefix,
                level.LabelTextIndent);
            WriteOptionalAttribute(
                alignment,
                OdfNamespaces.Fo,
                "margin-left",
                OdfNamespaces.FoPrefix,
                level.LabelMarginLeft);
        }
    }

    static void ReadListPositionFields(OdfListLevel level)
    {
        OdfNode? properties = level.LevelPropertiesNode;
        if (properties != null)
        {
            level.PositionAndSpaceMode = ReadAttributeValue(
                properties,
                OdfNamespaces.Text,
                "list-level-position-and-space-mode");
            level.SpaceBefore = ReadAttributeValue(
                properties,
                OdfNamespaces.Text,
                "space-before");
            level.MinimumLabelWidth = ReadAttributeValue(
                properties,
                OdfNamespaces.Text,
                "min-label-width");
            level.MinimumLabelDistance = ReadAttributeValue(
                properties,
                OdfNamespaces.Text,
                "min-label-distance");
        }
        OdfNode? alignment = level.LabelAlignmentNode;
        if (alignment != null)
        {
            level.LabelFollowedBy = ReadAttributeValue(
                alignment,
                OdfNamespaces.Text,
                "label-followed-by");
            level.ListTabStopPosition = ReadAttributeValue(
                alignment,
                OdfNamespaces.Text,
                "list-tab-stop-position");
            level.LabelTextIndent = ReadAttributeValue(
                alignment,
                OdfNamespaces.Fo,
                "text-indent");
            level.LabelMarginLeft = ReadAttributeValue(
                alignment,
                OdfNamespaces.Fo,
                "margin-left");
        }
    }

    static OdfNode EnsureListStyleSection(
        OdfDocument document,
        OdfListStyle style)
    {
        bool automatic = style.Kind != OdfListStyleKind.Outline
            && style.IsAutomatic;
        string partPath = SelectListStylePartPath(
            document,
            style,
            automatic);
        OdfPart? part = StyleDocumentModel.FindPart(document, partPath);
        if (part == null)
        {
            part = StyleDocumentModel.CreateStylesPart(document);
        }
        OdfNode? section = automatic
            ? part.AutomaticStyles
            : part.Styles;
        if (section == null)
        {
            section = StyleDocumentModel.CreateStyleSection(
                document,
                part,
                automatic);
        }
        return section;
    }

    static string SelectListStylePartPath(
        OdfDocument document,
        OdfListStyle style,
        bool automatic)
    {
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            return "";
        }
        if (!automatic)
        {
            return OdfPartNames.Styles;
        }
        if (style.PartPath == OdfPartNames.Content
            || style.PartPath == OdfPartNames.Styles)
        {
            return style.PartPath;
        }
        if (style.OriginalPartPath == OdfPartNames.Content
            || style.OriginalPartPath == OdfPartNames.Styles)
        {
            return style.OriginalPartPath;
        }
        return OdfPartNames.Content;
    }

    static void ApplyListStyleKind(
        OdfListStyle style,
        OdfNode node,
        bool isNew)
    {
        if (!isNew && style.Kind == style.OriginalKind)
        {
            return;
        }
        NormalizeTextElement(node, FormatListStyleName(style.Kind));
    }

    static void ApplyListLevelKind(
        OdfListLevel level,
        OdfNode node,
        bool isNew)
    {
        if (!isNew && level.Kind == level.OriginalKind)
        {
            return;
        }
        NormalizeTextElement(node, FormatListLevelName(level.Kind));
    }

    static string FormatListStyleName(OdfListStyleKind kind)
    {
        return kind == OdfListStyleKind.Outline
            ? "outline-style"
            : "list-style";
    }

    static string FormatListLevelName(OdfListLevelKind kind)
    {
        if (kind == OdfListLevelKind.Bullet)
        {
            return "list-level-style-bullet";
        }
        if (kind == OdfListLevelKind.Image)
        {
            return "list-level-style-image";
        }
        if (kind == OdfListLevelKind.Outline)
        {
            return "outline-level-style";
        }
        return "list-level-style-number";
    }

    static OdfListLevelKind ParseListLevelKind(OdfNode node)
    {
        if (node.Name != null)
        {
            if (node.Name.LocalName == "list-level-style-bullet")
            {
                return OdfListLevelKind.Bullet;
            }
            if (node.Name.LocalName == "list-level-style-image")
            {
                return OdfListLevelKind.Image;
            }
            if (node.Name.LocalName == "outline-level-style")
            {
                return OdfListLevelKind.Outline;
            }
        }
        return OdfListLevelKind.Number;
    }

    static OdfStyleProperty[] ApplyConvenienceAttribute(
        OdfStyleProperty[] properties,
        string namespaceName,
        string localName,
        string prefix,
        string value,
        string originalValue)
    {
        if (value == originalValue)
        {
            return properties;
        }
        List<OdfStyleProperty> result = new List<OdfStyleProperty>();
        OdfStyleProperty? existing = null;
        int index = 0;
        while (index < properties.Length)
        {
            OdfStyleProperty property = properties[index];
            if (property == null || property.Name == null)
            {
                throw new OdfDocumentException(
                    "A list-style attribute and qualified name are required for writing.");
            }
            if (property.Name.NamespaceName == namespaceName
                && property.Name.LocalName == localName)
            {
                existing = property;
            }
            else
            {
                result.Add(property);
            }
            index = index + 1;
        }
        if (value.Length > 0)
        {
            if (existing == null)
            {
                existing = new OdfStyleProperty();
                existing.Name.NamespaceName = namespaceName;
                existing.Name.LocalName = localName;
                existing.Name.Prefix = prefix;
            }
            existing.LexicalValue = value;
            result.Add(existing);
        }
        return result.ToArray();
    }

    static OdfNode[] ReplaceLevelContentNode(
        OdfNode[] content,
        OdfNode? current,
        OdfNode? original,
        string namespaceName,
        string localName)
    {
        List<OdfNode> result = new List<OdfNode>();
        int index = 0;
        while (index < content.Length)
        {
            OdfNode node = content[index];
            if (node != original
                && !NodeMatches(node, namespaceName, localName))
            {
                result.Add(node);
            }
            index = index + 1;
        }
        if (current != null)
        {
            result.Add(current);
        }
        return result.ToArray();
    }

    static void SynchronizeListStyleChildren(
        OdfListStyle style,
        OdfNode node)
    {
        List<OdfNode> children = new List<OdfNode>();
        int index = 0;
        while (index < style.Levels.Length)
        {
            OdfNode? levelNode = style.Levels[index].Node;
            if (levelNode == null)
            {
                throw new OdfDocumentException(
                    "A synchronized list level requires an XML node.");
            }
            children.Add(levelNode);
            index = index + 1;
        }
        index = 0;
        while (index < style.PassthroughContent.Length)
        {
            children.Add(style.PassthroughContent[index]);
            index = index + 1;
        }
        SynchronizeElementChildren(node, children.ToArray());
    }

    static void SynchronizeListLevelChildren(
        OdfListLevel level,
        OdfNode node)
    {
        List<OdfNode> children = new List<OdfNode>();
        int index = 0;
        while (index < level.Content.Length)
        {
            children.Add(level.Content[index]);
            index = index + 1;
        }
        if (level.LevelPropertiesNode != null)
        {
            children.Add(level.LevelPropertiesNode);
        }
        if (level.TextPropertiesNode != null)
        {
            children.Add(level.TextPropertiesNode);
        }
        SynchronizeElementChildren(node, children.ToArray());
    }

    static void SynchronizeElementChildren(
        OdfNode parent,
        OdfNode[] children)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (!ContainsNode(children, parent.Children[index]))
            {
                parent.Children[index].Parent = null;
            }
            index = index + 1;
        }
        index = 0;
        while (index < children.Length)
        {
            OdfNode child = children[index];
            if (child == null)
            {
                throw new OdfDocumentException(
                    "List-style content cannot contain a null node.");
            }
            if (child.Parent != null && child.Parent != parent)
            {
                StyleDocumentModel.RemoveNodeFromParent(child);
            }
            child.Parent = parent;
            child.Location.PartPath = parent.Location.PartPath;
            index = index + 1;
        }
        parent.Children = StylePropertyDocumentModel.CopyNodes(children);
        parent.WasEmptyElement = children.Length == 0;
    }

    static bool ListStyleContentChanged(OdfListStyle style)
    {
        if (style.Levels.Length != style.OriginalLevelNodes.Length
            || !NodesMatch(
                style.PassthroughContent,
                style.OriginalPassthroughContent))
        {
            return true;
        }
        int index = 0;
        while (index < style.Levels.Length)
        {
            if (style.Levels[index] == null
                || style.Levels[index].OriginalNode
                    != style.OriginalLevelNodes[index])
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RemoveDeletedListStyles(
        OdfListStyle[] styles,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            if (!ListStyleRetainsNode(styles, node) && node.Parent != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static bool ListStyleRetainsNode(
        OdfListStyle[] styles,
        OdfNode node)
    {
        int index = 0;
        while (index < styles.Length)
        {
            if (styles[index] != null && styles[index].OriginalNode == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsListStyleNode(OdfNode node)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Text
            && (node.Name.LocalName == "list-style"
                || node.Name.LocalName == "outline-style");
    }

    static bool IsListLevelNode(OdfNode node)
    {
        if (node.Kind != OdfNodeKind.Element
            || node.Name == null
            || node.Name.NamespaceName != OdfNamespaces.Text)
        {
            return false;
        }
        string name = node.Name.LocalName;
        return name == "list-level-style-number"
            || name == "list-level-style-bullet"
            || name == "list-level-style-image"
            || name == "outline-level-style";
    }

    static OdfNode? FindStyleChild(OdfNode? parent, string localName)
    {
        if (parent == null)
        {
            return null;
        }
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (NodeMatches(child, OdfNamespaces.Style, localName))
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindOfficeChild(
        OdfNode[] children,
        string localName)
    {
        int index = 0;
        while (index < children.Length)
        {
            if (NodeMatches(
                children[index],
                OdfNamespaces.Office,
                localName))
            {
                return children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool NodeMatches(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName;
    }

    static string ReadAttributeValue(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            namespaceName,
            localName);
    }

    static OdfNode CreateTextElement(string localName, string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Text,
            localName,
            OdfNamespaces.TextPrefix);
        result.Location.PartPath = partPath;
        EnsureElementNamespace(result);
        return result;
    }

    static OdfNode CreateStyleElement(string localName, string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Style,
            localName,
            OdfNamespaces.StylePrefix);
        result.Location.PartPath = partPath;
        EnsureElementNamespace(result);
        return result;
    }

    static bool HasLabelAlignment(OdfListLevel level)
    {
        return level.LabelFollowedBy.Length > 0
            || level.ListTabStopPosition.Length > 0
            || level.LabelTextIndent.Length > 0
            || level.LabelMarginLeft.Length > 0;
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
            StyleDocumentModel.RemoveAttribute(
                node,
                namespaceName,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }

    static void EnsureElementNamespace(OdfNode node)
    {
        if (node.Name == null || node.Name.NamespaceName.Length == 0)
        {
            return;
        }
        string prefix = node.Name.Prefix;
        string namespaceName = node.Name.NamespaceName;
        OdfNode? current = node;
        while (current != null)
        {
            int index = 0;
            while (index < current.NamespaceDeclarations.Length)
            {
                OdfNamespaceDeclaration declaration =
                    current.NamespaceDeclarations[index];
                if (declaration.Prefix == prefix
                    && declaration.NamespaceName == namespaceName)
                {
                    return;
                }
                index = index + 1;
            }
            current = current.Parent;
        }
        OdfNamespaceDeclaration[] declarations =
            new OdfNamespaceDeclaration[
                node.NamespaceDeclarations.Length + 1];
        Array.Copy(
            node.NamespaceDeclarations,
            declarations,
            node.NamespaceDeclarations.Length);
        OdfNamespaceDeclaration created = new OdfNamespaceDeclaration();
        created.Prefix = prefix;
        created.NamespaceName = namespaceName;
        created.Location.PartPath = node.Location.PartPath;
        declarations[declarations.Length - 1] = created;
        node.NamespaceDeclarations = declarations;
    }

    static void NormalizeTextElement(OdfNode node, string localName)
    {
        if (node.Name == null)
        {
            node.Name = new OdfQualifiedName();
        }
        node.Kind = OdfNodeKind.Element;
        node.Name.NamespaceName = OdfNamespaces.Text;
        node.Name.LocalName = localName;
        node.Name.Prefix = OdfNamespaces.TextPrefix;
    }

    static bool ContainsNode(OdfNode[] nodes, OdfNode target)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool NodesMatch(OdfNode[] first, OdfNode[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static void ResolveListStyleRelationships(OdfDocument document)
    {
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            style.ListStyle = FindListStyleByName(
                document.ListStyles,
                style.ListStyleName);
            if (style.ListStyle != null)
            {
                style.ListStyleNode = style.ListStyle.Node;
            }
            index = index + 1;
        }
    }

    static OdfListStyle? FindListStyleByName(
        OdfListStyle[] styles,
        string name)
    {
        if (name.Length == 0)
        {
            return null;
        }
        int index = 0;
        while (index < styles.Length)
        {
            if (styles[index].Name == name)
            {
                return styles[index];
            }
            index = index + 1;
        }
        return null;
    }

    static void SnapshotListStyle(OdfListStyle style)
    {
        style.OriginalName = style.Name;
        style.OriginalDisplayName = style.DisplayName;
        style.OriginalKind = style.Kind;
        style.OriginalConsecutiveNumbering = style.ConsecutiveNumbering;
        style.OriginalPartPath = style.PartPath;
        style.OriginalIsAutomatic = style.IsAutomatic;
        OdfNode[] levelNodes = new OdfNode[style.Levels.Length];
        int index = 0;
        while (index < style.Levels.Length)
        {
            levelNodes[index] = style.Levels[index].OriginalNode
                ?? new OdfNode();
            index = index + 1;
        }
        style.OriginalLevelNodes = levelNodes;
        style.OriginalPassthroughContent =
            StylePropertyDocumentModel.CopyNodes(style.PassthroughContent);
    }

    static void SnapshotListLevel(OdfListLevel level)
    {
        level.OriginalKind = level.Kind;
        level.OriginalLevel = level.Level;
        level.OriginalStyleName = level.StyleName;
        level.OriginalNumberFormat = level.NumberFormat;
        level.OriginalNumberPrefix = level.NumberPrefix;
        level.OriginalNumberSuffix = level.NumberSuffix;
        level.OriginalNumberLetterSync = level.NumberLetterSync;
        level.OriginalNumberListFormatName = level.NumberListFormatName;
        level.OriginalDisplayLevels = level.DisplayLevels;
        level.OriginalStartValue = level.StartValue;
        level.OriginalBulletCharacter = level.BulletCharacter;
        level.OriginalBulletRelativeSize = level.BulletRelativeSize;
        level.OriginalImageHref = level.ImageHref;
        level.OriginalPositionAndSpaceMode = level.PositionAndSpaceMode;
        level.OriginalSpaceBefore = level.SpaceBefore;
        level.OriginalMinimumLabelWidth = level.MinimumLabelWidth;
        level.OriginalMinimumLabelDistance = level.MinimumLabelDistance;
        level.OriginalLabelFollowedBy = level.LabelFollowedBy;
        level.OriginalListTabStopPosition = level.ListTabStopPosition;
        level.OriginalLabelTextIndent = level.LabelTextIndent;
        level.OriginalLabelMarginLeft = level.LabelMarginLeft;
    }
}
