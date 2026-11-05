using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class DataStyleDocumentState
{
    internal OdfNode[] OriginalNodes = [];
}

internal static class DataStyleDocumentModel
{
    internal static void LoadDataStyles(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for data-style materialization.");
        }
        List<OdfDataStyle> styles = new List<OdfDataStyle>();
        List<OdfNode> nodes = new List<OdfNode>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            LoadDataStyleSection(part, part.Styles, false, styles, nodes);
            LoadDataStyleSection(
                part,
                part.AutomaticStyles,
                true,
                styles,
                nodes);
            partIndex = partIndex + 1;
        }
        document.DataStyles = styles.ToArray();
        DataStyleDocumentState state = new DataStyleDocumentState();
        state.OriginalNodes = nodes.ToArray();
        document.DataStyleState = state;
        ResolveDataStyleRelationships(document);
    }

    internal static void SynchronizeDataStyles(OdfDocument document)
    {
        if (document == null || document.DataStyles == null)
        {
            throw new OdfDocumentException(
                "A document and data-style collection are required for writing.");
        }
        DataStyleDocumentState state = document.DataStyleState
            ?? new DataStyleDocumentState();
        RemoveDeletedDataStyles(document.DataStyles, state.OriginalNodes);
        List<OdfNode> nodes = new List<OdfNode>();
        int index = 0;
        while (index < document.DataStyles.Length)
        {
            OdfDataStyle style = document.DataStyles[index];
            if (style == null)
            {
                throw new OdfDocumentException(
                    "The data-style collection contains a null style.");
            }
            SynchronizeDataStyle(document, style);
            if (style.OriginalNode != null)
            {
                nodes.Add(style.OriginalNode);
            }
            index = index + 1;
        }
        state.OriginalNodes = nodes.ToArray();
        document.DataStyleState = state;
        StyleResolver.ResolveRelationships(document);
        ResolveDataStyleRelationships(document);
    }

    static void LoadDataStyleSection(
        OdfPart part,
        OdfNode? section,
        bool automatic,
        List<OdfDataStyle> styles,
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
            if (IsDataStyleNode(node))
            {
                OdfDataStyle style = CreateDataStyle(part, node, automatic);
                styles.Add(style);
                nodes.Add(node);
            }
            index = index + 1;
        }
    }

    static OdfDataStyle CreateDataStyle(
        OdfPart part,
        OdfNode node,
        bool automatic)
    {
        OdfDataStyle result = new OdfDataStyle();
        result.Name = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "name");
        result.DisplayName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "display-name");
        result.PartPath = part.Path;
        result.IsAutomatic = automatic;
        result.Attributes =
            StylePropertyDocumentModel.ReadPropertyAttributes(node);
        result.OriginalAttributes =
            StylePropertyDocumentModel.AttributeNodes(result.Attributes);
        ReadRootConvenienceFields(result, node);
        OdfNode? textProperties = FindTextPropertiesNode(node);
        result.TextPropertiesNode = textProperties;
        result.OriginalTextPropertiesNode = textProperties;
        result.TextProperties =
            StylePropertyDocumentModel.ReadPropertyAttributes(textProperties);
        result.OriginalTextPropertyAttributes =
            StylePropertyDocumentModel.AttributeNodes(result.TextProperties);
        result.TextPropertyChildren =
            StylePropertyDocumentModel.PropertyChildren(textProperties);
        result.OriginalTextPropertyChildren =
            StylePropertyDocumentModel.CopyNodes(
                result.TextPropertyChildren);
        List<OdfDataStyleComponent> components =
            new List<OdfDataStyleComponent>();
        List<OdfDataStyleMap> maps = new List<OdfDataStyleMap>();
        List<OdfNode> passthrough = new List<OdfNode>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsTextPropertiesNode(child))
            {
            }
            else if (IsMapNode(child))
            {
                maps.Add(CreateDataStyleMap(child));
            }
            else if (child.Kind == OdfNodeKind.Element)
            {
                components.Add(CreateDataStyleComponent(child));
            }
            else
            {
                passthrough.Add(child);
            }
            index = index + 1;
        }
        result.Components = components.ToArray();
        result.Maps = maps.ToArray();
        result.PassthroughContent = passthrough.ToArray();
        result.Kind = ParseDataStyleKind(node, result.Components);
        result.FormatPattern = InterpretFormatPattern(result.Components);
        result.Node = node;
        result.OriginalNode = node;
        SnapshotDataStyle(result);
        return result;
    }

    static OdfDataStyleComponent CreateDataStyleComponent(OdfNode node)
    {
        OdfDataStyleComponent result = new OdfDataStyleComponent();
        if (node.Name != null)
        {
            result.Name.NamespaceName = node.Name.NamespaceName;
            result.Name.LocalName = node.Name.LocalName;
            result.Name.Prefix = node.Name.Prefix;
        }
        result.Attributes =
            StylePropertyDocumentModel.ReadPropertyAttributes(node);
        result.OriginalAttributes =
            StylePropertyDocumentModel.AttributeNodes(result.Attributes);
        result.Content = StylePropertyDocumentModel.PropertyChildren(node);
        result.OriginalContent =
            StylePropertyDocumentModel.CopyNodes(result.Content);
        result.Text = ReadDirectText(node.Children);
        result.Node = node;
        result.OriginalNode = node;
        SnapshotDataStyleComponent(result);
        return result;
    }

    static OdfDataStyleMap CreateDataStyleMap(OdfNode node)
    {
        OdfDataStyleMap result = new OdfDataStyleMap();
        result.Condition = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "condition");
        result.ApplyStyleName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "apply-style-name");
        result.BaseCellAddress = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "base-cell-address");
        result.Attributes =
            StylePropertyDocumentModel.ReadPropertyAttributes(node);
        result.OriginalAttributes =
            StylePropertyDocumentModel.AttributeNodes(result.Attributes);
        result.Node = node;
        result.OriginalNode = node;
        SnapshotDataStyleMap(result);
        return result;
    }

    static void SynchronizeDataStyle(
        OdfDocument document,
        OdfDataStyle style)
    {
        if (style.Attributes == null
            || style.TextProperties == null
            || style.TextPropertyChildren == null
            || style.Components == null
            || style.Maps == null
            || style.PassthroughContent == null)
        {
            throw new OdfDocumentException(
                "Data-style attribute and content collections are required for writing.");
        }
        OdfNode section = EnsureDataStyleSection(document, style);
        OdfNode? node = style.OriginalNode;
        bool isNew = node == null;
        bool topLevelChanged = isNew || DataStyleContentChanged(style);
        if (node == null)
        {
            node = CreateDataStyleNode(style, section.Location.PartPath);
            StyleDocumentModel.AppendNode(section, node);
            style.OriginalNode = node;
        }
        else if (node.Parent != section)
        {
            StyleDocumentModel.RemoveNodeFromParent(node);
            StyleDocumentModel.AppendNode(section, node);
        }
        ApplyDataStyleKind(style, node, isNew);
        if (node.Name != null)
        {
            EnsureElementNamespace(
                node,
                node.Name.Prefix,
                node.Name.NamespaceName);
        }
        ApplyRootConvenienceChanges(style);
        StylePropertyDocumentModel.SynchronizeElementAttributes(
            node,
            style.Attributes,
            style.OriginalAttributes);
        style.OriginalAttributes =
            StylePropertyDocumentModel.AttributeNodes(style.Attributes);
        style.Name = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "name");
        style.DisplayName = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "display-name");
        ReadRootConvenienceFields(style, node);
        OdfNode? textProperties =
            StylePropertyDocumentModel.SynchronizePropertySet(
                node,
                "text-properties",
                style.TextProperties,
                style.TextPropertyChildren,
                style.OriginalTextPropertiesNode,
                style.OriginalTextPropertyAttributes,
                style.OriginalTextPropertyChildren);
        style.TextPropertiesNode = textProperties;
        style.OriginalTextPropertiesNode = textProperties;
        style.OriginalTextPropertyAttributes =
            StylePropertyDocumentModel.AttributeNodes(style.TextProperties);
        style.OriginalTextPropertyChildren =
            StylePropertyDocumentModel.CopyNodes(
                style.TextPropertyChildren);
        SynchronizeDataStyleComponents(style.Components, node);
        SynchronizeDataStyleMaps(style.Maps, node);
        if (topLevelChanged)
        {
            SynchronizeDataStyleChildren(style, node);
        }
        style.Node = node;
        style.PartPath = section.Location.PartPath;
        style.IsAutomatic = StyleDocumentModel.IsAutomaticSection(section);
        style.FormatPattern = InterpretFormatPattern(style.Components);
        SnapshotDataStyle(style);
    }

    static OdfNode EnsureDataStyleSection(
        OdfDocument document,
        OdfDataStyle style)
    {
        string partPath = SelectDataStylePartPath(document, style);
        OdfPart? part = StyleDocumentModel.FindPart(document, partPath);
        if (part == null)
        {
            part = StyleDocumentModel.CreateStylesPart(document);
        }
        OdfNode? section = style.IsAutomatic
            ? part.AutomaticStyles
            : part.Styles;
        if (section == null)
        {
            section = StyleDocumentModel.CreateStyleSection(
                document,
                part,
                style.IsAutomatic);
        }
        return section;
    }

    static string SelectDataStylePartPath(
        OdfDocument document,
        OdfDataStyle style)
    {
        if (document.PackageKind == OdfPackageKind.Flat)
        {
            return "";
        }
        if (!style.IsAutomatic)
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

    static OdfNode CreateDataStyleNode(
        OdfDataStyle style,
        string partPath)
    {
        string localName = FormatDataStyleElementName(style.Kind);
        if (localName.Length == 0)
        {
            throw new OdfDocumentException(
                "A new data style requires a recognized style kind.");
        }
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Number,
            localName,
            OdfNamespaces.NumberPrefix);
        result.Location.PartPath = partPath;
        EnsureElementNamespace(
            result,
            OdfNamespaces.NumberPrefix,
            OdfNamespaces.Number);
        return result;
    }

    static void ApplyDataStyleKind(
        OdfDataStyle style,
        OdfNode node,
        bool isNew)
    {
        if (!isNew && style.Kind == style.OriginalKind)
        {
            return;
        }
        string localName = FormatDataStyleElementName(style.Kind);
        if (localName.Length == 0)
        {
            throw new OdfDocumentException(
                "A data style requires a recognized style kind when changing its kind.");
        }
        if (node.Name == null)
        {
            node.Name = new OdfQualifiedName();
        }
        node.Name.NamespaceName = OdfNamespaces.Number;
        node.Name.LocalName = localName;
        node.Name.Prefix = OdfNamespaces.NumberPrefix;
        EnsureElementNamespace(
            node,
            OdfNamespaces.NumberPrefix,
            OdfNamespaces.Number);
    }

    static void ApplyRootConvenienceChanges(OdfDataStyle style)
    {
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
            OdfNamespaces.Number,
            "language",
            OdfNamespaces.NumberPrefix,
            style.Language,
            style.OriginalLanguage);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Number,
            "country",
            OdfNamespaces.NumberPrefix,
            style.Country,
            style.OriginalCountry);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Number,
            "script",
            OdfNamespaces.NumberPrefix,
            style.Script,
            style.OriginalScript);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Number,
            "rfc-language-tag",
            OdfNamespaces.NumberPrefix,
            style.LanguageTag,
            style.OriginalLanguageTag);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Number,
            "title",
            OdfNamespaces.NumberPrefix,
            style.Title,
            style.OriginalTitle);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Style,
            "volatile",
            OdfNamespaces.StylePrefix,
            style.Volatile,
            style.OriginalVolatile);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Number,
            "automatic-order",
            OdfNamespaces.NumberPrefix,
            style.AutomaticOrder,
            style.OriginalAutomaticOrder);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Number,
            "format-source",
            OdfNamespaces.NumberPrefix,
            style.FormatSource,
            style.OriginalFormatSource);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Number,
            "truncate-on-overflow",
            OdfNamespaces.NumberPrefix,
            style.TruncateOnOverflow,
            style.OriginalTruncateOnOverflow);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Number,
            "transliteration-format",
            OdfNamespaces.NumberPrefix,
            style.TransliterationFormat,
            style.OriginalTransliterationFormat);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Number,
            "transliteration-language",
            OdfNamespaces.NumberPrefix,
            style.TransliterationLanguage,
            style.OriginalTransliterationLanguage);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Number,
            "transliteration-country",
            OdfNamespaces.NumberPrefix,
            style.TransliterationCountry,
            style.OriginalTransliterationCountry);
        style.Attributes = ApplyConvenienceAttribute(
            style.Attributes,
            OdfNamespaces.Number,
            "transliteration-style",
            OdfNamespaces.NumberPrefix,
            style.TransliterationStyle,
            style.OriginalTransliterationStyle);
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
                    "A data-style attribute and qualified name are required for writing.");
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

    static void SynchronizeDataStyleComponents(
        OdfDataStyleComponent[] components,
        OdfNode parent)
    {
        int index = 0;
        while (index < components.Length)
        {
            OdfDataStyleComponent component = components[index];
            if (component == null
                || component.Name == null
                || component.Attributes == null
                || component.Content == null
                || component.Name.LocalName.Length == 0)
            {
                throw new OdfDocumentException(
                    "A complete data-style component is required for writing.");
            }
            OdfNode? node = component.OriginalNode;
            if (node == null)
            {
                node = new OdfNode();
                node.Kind = OdfNodeKind.Element;
                node.Name = new OdfQualifiedName();
                node.Location.PartPath = parent.Location.PartPath;
                component.OriginalNode = node;
            }
            ApplyComponentName(component, node);
            StylePropertyDocumentModel.SynchronizeElementAttributes(
                node,
                component.Attributes,
                component.OriginalAttributes);
            component.OriginalAttributes =
                StylePropertyDocumentModel.AttributeNodes(
                    component.Attributes);
            if (component.Text != component.OriginalText)
            {
                component.Content = ReplaceDirectText(
                    component.Content,
                    component.Text,
                    node.Location.PartPath);
            }
            if (!NodesMatch(component.Content, component.OriginalContent))
            {
                SynchronizeElementChildren(node, component.Content);
            }
            if (node.Name != null)
            {
                component.Name.NamespaceName = node.Name.NamespaceName;
                component.Name.LocalName = node.Name.LocalName;
                component.Name.Prefix = node.Name.Prefix;
            }
            component.Text = ReadDirectText(node.Children);
            component.Node = node;
            component.OriginalContent =
                StylePropertyDocumentModel.CopyNodes(component.Content);
            SnapshotDataStyleComponent(component);
            index = index + 1;
        }
    }

    static void ApplyComponentName(
        OdfDataStyleComponent component,
        OdfNode node)
    {
        bool typedChanged = component.Name.NamespaceName
                != component.OriginalNamespaceName
            || component.Name.LocalName != component.OriginalLocalName
            || component.Name.Prefix != component.OriginalPrefix;
        if (!typedChanged)
        {
            if (node.Name != null)
            {
                EnsureElementNamespace(
                    node,
                    node.Name.Prefix,
                    node.Name.NamespaceName);
            }
            return;
        }
        if (node.Name == null)
        {
            node.Name = new OdfQualifiedName();
        }
        node.Name.NamespaceName = component.Name.NamespaceName;
        node.Name.LocalName = component.Name.LocalName;
        node.Name.Prefix = SelectElementPrefix(component.Name);
        component.Name.Prefix = SelectElementPrefix(component.Name);
        EnsureElementNamespace(
            node,
            component.Name.Prefix,
            component.Name.NamespaceName);
    }

    static void SynchronizeDataStyleMaps(
        OdfDataStyleMap[] maps,
        OdfNode parent)
    {
        int index = 0;
        while (index < maps.Length)
        {
            OdfDataStyleMap map = maps[index];
            if (map == null || map.Attributes == null)
            {
                throw new OdfDocumentException(
                    "A complete data-style map is required for writing.");
            }
            OdfNode? node = map.OriginalNode;
            if (node == null)
            {
                node = new OdfNode();
                node.Kind = OdfNodeKind.Element;
                node.Name = StyleDocumentModel.QualifiedName(
                    OdfNamespaces.Style,
                    "map",
                    OdfNamespaces.StylePrefix);
                node.Location.PartPath = parent.Location.PartPath;
                map.OriginalNode = node;
            }
            map.Attributes = ApplyConvenienceAttribute(
                map.Attributes,
                OdfNamespaces.Style,
                "condition",
                OdfNamespaces.StylePrefix,
                map.Condition,
                map.OriginalCondition);
            map.Attributes = ApplyConvenienceAttribute(
                map.Attributes,
                OdfNamespaces.Style,
                "apply-style-name",
                OdfNamespaces.StylePrefix,
                map.ApplyStyleName,
                map.OriginalApplyStyleName);
            map.Attributes = ApplyConvenienceAttribute(
                map.Attributes,
                OdfNamespaces.Style,
                "base-cell-address",
                OdfNamespaces.StylePrefix,
                map.BaseCellAddress,
                map.OriginalBaseCellAddress);
            StylePropertyDocumentModel.SynchronizeElementAttributes(
                node,
                map.Attributes,
                map.OriginalAttributes);
            map.OriginalAttributes =
                StylePropertyDocumentModel.AttributeNodes(map.Attributes);
            map.Condition = StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.Style,
                "condition");
            map.ApplyStyleName = StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.Style,
                "apply-style-name");
            map.BaseCellAddress = StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.Style,
                "base-cell-address");
            map.Node = node;
            SnapshotDataStyleMap(map);
            index = index + 1;
        }
    }

    static bool DataStyleContentChanged(OdfDataStyle style)
    {
        if (!ComponentNodesMatch(
                style.Components,
                style.OriginalComponentNodes)
            || !MapNodesMatch(style.Maps, style.OriginalMapNodes)
            || !NodesMatch(
                style.PassthroughContent,
                style.OriginalPassthroughContent))
        {
            return true;
        }
        bool needsTextProperties = style.TextProperties.Length > 0
            || style.TextPropertyChildren.Length > 0;
        return (style.OriginalTextPropertiesNode == null
                && needsTextProperties)
            || (style.OriginalTextPropertiesNode != null
                && !needsTextProperties);
    }

    static void SynchronizeDataStyleChildren(
        OdfDataStyle style,
        OdfNode node)
    {
        List<OdfNode> children = new List<OdfNode>();
        if (style.TextPropertiesNode != null)
        {
            children.Add(style.TextPropertiesNode);
        }
        int index = 0;
        while (index < style.Components.Length)
        {
            OdfNode? componentNode = style.Components[index].Node;
            if (componentNode == null)
            {
                throw new OdfDocumentException(
                    "A synchronized data-style component requires an XML node.");
            }
            children.Add(componentNode);
            index = index + 1;
        }
        index = 0;
        while (index < style.PassthroughContent.Length)
        {
            children.Add(style.PassthroughContent[index]);
            index = index + 1;
        }
        index = 0;
        while (index < style.Maps.Length)
        {
            OdfNode? mapNode = style.Maps[index].Node;
            if (mapNode == null)
            {
                throw new OdfDocumentException(
                    "A synchronized data-style map requires an XML node.");
            }
            children.Add(mapNode);
            index = index + 1;
        }
        SynchronizeElementChildren(node, children.ToArray());
    }

    static void SynchronizeElementChildren(
        OdfNode node,
        OdfNode[] children)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode old = node.Children[index];
            if (!ContainsNode(children, old))
            {
                old.Parent = null;
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
                    "Data-style content cannot contain a null node.");
            }
            if (child.Parent != null && child.Parent != node)
            {
                StyleDocumentModel.RemoveNodeFromParent(child);
            }
            child.Parent = node;
            SetNodePartPath(child, node.Location.PartPath);
            index = index + 1;
        }
        node.Children = StylePropertyDocumentModel.CopyNodes(children);
        node.WasEmptyElement = children.Length == 0;
    }

    static OdfNode[] ReplaceDirectText(
        OdfNode[] children,
        string text,
        string partPath)
    {
        List<OdfNode> result = new List<OdfNode>();
        if (text.Length > 0)
        {
            OdfNode textNode = new OdfNode();
            textNode.Kind = OdfNodeKind.Text;
            textNode.Value = text;
            textNode.Location.PartPath = partPath;
            result.Add(textNode);
        }
        int index = 0;
        while (index < children.Length)
        {
            OdfNode child = children[index];
            if (child.Kind != OdfNodeKind.Text)
            {
                result.Add(child);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void RemoveDeletedDataStyles(
        OdfDataStyle[] styles,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            if (!DataStyleRetainsNode(styles, node) && node.Parent != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static bool DataStyleRetainsNode(
        OdfDataStyle[] styles,
        OdfNode node)
    {
        int index = 0;
        while (index < styles.Length)
        {
            OdfDataStyle style = styles[index];
            if (style != null && style.OriginalNode == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void ReadRootConvenienceFields(
        OdfDataStyle style,
        OdfNode node)
    {
        style.Language = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Number,
            "language");
        style.Country = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Number,
            "country");
        style.Script = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Number,
            "script");
        style.LanguageTag = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Number,
            "rfc-language-tag");
        style.Title = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Number,
            "title");
        style.Volatile = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Style,
            "volatile");
        style.AutomaticOrder = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Number,
            "automatic-order");
        style.FormatSource = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Number,
            "format-source");
        style.TruncateOnOverflow = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Number,
            "truncate-on-overflow");
        style.TransliterationFormat = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Number,
            "transliteration-format");
        style.TransliterationLanguage = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Number,
            "transliteration-language");
        style.TransliterationCountry = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Number,
            "transliteration-country");
        style.TransliterationStyle = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Number,
            "transliteration-style");
    }

    static OdfDataStyleKind ParseDataStyleKind(
        OdfNode node,
        OdfDataStyleComponent[] components)
    {
        if (node.Name == null)
        {
            return OdfDataStyleKind.Unknown;
        }
        string name = node.Name.LocalName;
        if (name == "number-style")
        {
            if (ContainsNumberComponent(components, "scientific-number"))
            {
                return OdfDataStyleKind.Scientific;
            }
            if (ContainsNumberComponent(components, "fraction"))
            {
                return OdfDataStyleKind.Fraction;
            }
            return OdfDataStyleKind.Number;
        }
        if (name == "percentage-style")
        {
            return OdfDataStyleKind.Percentage;
        }
        if (name == "currency-style")
        {
            return OdfDataStyleKind.Currency;
        }
        if (name == "date-style")
        {
            return OdfDataStyleKind.Date;
        }
        if (name == "time-style")
        {
            return OdfDataStyleKind.Time;
        }
        if (name == "boolean-style")
        {
            return OdfDataStyleKind.Boolean;
        }
        if (name == "text-style")
        {
            return OdfDataStyleKind.Text;
        }
        return OdfDataStyleKind.Unknown;
    }

    static string FormatDataStyleElementName(OdfDataStyleKind kind)
    {
        if (kind == OdfDataStyleKind.Number
            || kind == OdfDataStyleKind.Scientific
            || kind == OdfDataStyleKind.Fraction)
        {
            return "number-style";
        }
        if (kind == OdfDataStyleKind.Percentage)
        {
            return "percentage-style";
        }
        if (kind == OdfDataStyleKind.Currency)
        {
            return "currency-style";
        }
        if (kind == OdfDataStyleKind.Date)
        {
            return "date-style";
        }
        if (kind == OdfDataStyleKind.Time)
        {
            return "time-style";
        }
        if (kind == OdfDataStyleKind.Boolean)
        {
            return "boolean-style";
        }
        if (kind == OdfDataStyleKind.Text)
        {
            return "text-style";
        }
        return "";
    }

    static bool ContainsNumberComponent(
        OdfDataStyleComponent[] components,
        string localName)
    {
        int index = 0;
        while (index < components.Length)
        {
            OdfDataStyleComponent component = components[index];
            if (component.Name.NamespaceName == OdfNamespaces.Number
                && component.Name.LocalName == localName)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsDataStyleNode(OdfNode node)
    {
        if (node.Kind != OdfNodeKind.Element
            || node.Name == null
            || node.Name.NamespaceName != OdfNamespaces.Number)
        {
            return false;
        }
        string name = node.Name.LocalName;
        return name == "number-style"
            || name == "percentage-style"
            || name == "currency-style"
            || name == "date-style"
            || name == "time-style"
            || name == "boolean-style"
            || name == "text-style";
    }

    static OdfNode? FindTextPropertiesNode(OdfNode node)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (IsTextPropertiesNode(node.Children[index]))
            {
                return node.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsTextPropertiesNode(OdfNode node)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Style
            && node.Name.LocalName == "text-properties";
    }

    static bool IsMapNode(OdfNode node)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Style
            && node.Name.LocalName == "map";
    }

    static string InterpretFormatPattern(
        OdfDataStyleComponent[] components)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < components.Length)
        {
            AppendComponentPattern(result, components[index]);
            index = index + 1;
        }
        return result.ToString();
    }

    static void AppendComponentPattern(
        StringBuilder result,
        OdfDataStyleComponent component)
    {
        if (component.Name.NamespaceName != OdfNamespaces.Number)
        {
            result.Append(component.Text);
            return;
        }
        string name = component.Name.LocalName;
        if (name == "text" || name == "currency-symbol")
        {
            result.Append(component.Text);
            return;
        }
        if (name == "fill-character")
        {
            result.Append('*');
            result.Append(component.Text);
            return;
        }
        if (name == "number")
        {
            int numberStart = result.Length;
            AppendNumberPattern(result, component);
            int integerEnd = FindIntegerPatternEnd(result, numberStart);
            InsertEmbeddedTextPattern(
                result,
                component.Content,
                numberStart,
                integerEnd);
            return;
        }
        if (name == "scientific-number")
        {
            AppendScientificPattern(result, component);
            return;
        }
        if (name == "fraction")
        {
            AppendFractionPattern(result, component);
            return;
        }
        if (name == "day")
        {
            AppendLongOrShortPattern(result, component, "d", "dd");
            return;
        }
        if (name == "month")
        {
            AppendMonthPattern(result, component);
            return;
        }
        if (name == "year")
        {
            AppendLongOrShortPattern(result, component, "yy", "yyyy");
            return;
        }
        if (name == "day-of-week")
        {
            AppendLongOrShortPattern(result, component, "ddd", "dddd");
            return;
        }
        if (name == "week-of-year")
        {
            result.Append("ww");
            return;
        }
        if (name == "quarter")
        {
            AppendLongOrShortPattern(result, component, "q", "qq");
            return;
        }
        if (name == "era")
        {
            AppendLongOrShortPattern(result, component, "g", "gg");
            return;
        }
        if (name == "hours")
        {
            AppendLongOrShortPattern(result, component, "h", "hh");
            return;
        }
        if (name == "minutes")
        {
            AppendLongOrShortPattern(result, component, "m", "mm");
            return;
        }
        if (name == "seconds")
        {
            AppendSecondsPattern(result, component);
            return;
        }
        if (name == "am-pm")
        {
            result.Append("AM/PM");
            return;
        }
        if (name == "boolean")
        {
            result.Append("BOOLEAN");
            return;
        }
        if (name == "text-content")
        {
            result.Append('@');
            return;
        }
        if (component.Text.Length > 0)
        {
            result.Append(component.Text);
        }
        else
        {
            result.Append('{');
            result.Append(name);
            result.Append('}');
        }
    }

    static void AppendNumberPattern(
        StringBuilder result,
        OdfDataStyleComponent component)
    {
        int integerDigits = ReadBoundedCount(
            ReadComponentAttribute(component, "min-integer-digits"),
            1);
        bool grouping = ReadComponentAttribute(component, "grouping")
            == "true";
        if (grouping)
        {
            result.Append("#,##");
        }
        AppendRepeated(result, '0', integerDigits);
        int decimalPlaces = ReadBoundedCount(
            ReadComponentAttribute(component, "decimal-places"),
            0);
        if (decimalPlaces > 0)
        {
            result.Append('.');
            AppendRepeated(result, '0', decimalPlaces);
        }
    }

    static void AppendScientificPattern(
        StringBuilder result,
        OdfDataStyleComponent component)
    {
        AppendNumberPattern(result, component);
        result.Append('E');
        if (ReadComponentAttribute(component, "forced-exponent-sign")
            == "true")
        {
            result.Append('+');
        }
        int exponentDigits = ReadBoundedCount(
            ReadComponentAttribute(component, "min-exponent-digits"),
            1);
        AppendRepeated(result, '0', exponentDigits);
    }

    static void AppendFractionPattern(
        StringBuilder result,
        OdfDataStyleComponent component)
    {
        int integerDigits = ReadBoundedCount(
            ReadComponentAttribute(component, "min-integer-digits"),
            0);
        if (integerDigits > 0)
        {
            AppendRepeated(result, '0', integerDigits);
            result.Append(' ');
        }
        int numeratorDigits = ReadBoundedCount(
            ReadComponentAttribute(component, "min-numerator-digits"),
            1);
        AppendRepeated(result, '?', numeratorDigits);
        result.Append('/');
        string denominator = ReadComponentAttribute(
            component,
            "denominator-value");
        if (denominator.Length > 0)
        {
            result.Append(denominator);
        }
        else
        {
            int denominatorDigits = ReadBoundedCount(
                ReadComponentAttribute(
                    component,
                    "min-denominator-digits"),
                1);
            AppendRepeated(result, '?', denominatorDigits);
        }
    }

    static void AppendMonthPattern(
        StringBuilder result,
        OdfDataStyleComponent component)
    {
        bool textual = ReadComponentAttribute(component, "textual")
            == "true";
        bool longForm = ReadComponentAttribute(component, "style")
            == "long";
        if (textual)
        {
            result.Append(longForm ? "MMMM" : "MMM");
        }
        else
        {
            result.Append(longForm ? "MM" : "M");
        }
    }

    static void AppendSecondsPattern(
        StringBuilder result,
        OdfDataStyleComponent component)
    {
        bool longForm = ReadComponentAttribute(component, "style")
            == "long";
        result.Append(longForm ? "ss" : "s");
        int decimalPlaces = ReadBoundedCount(
            ReadComponentAttribute(component, "decimal-places"),
            0);
        if (decimalPlaces > 0)
        {
            result.Append('.');
            AppendRepeated(result, '0', decimalPlaces);
        }
    }

    static void AppendLongOrShortPattern(
        StringBuilder result,
        OdfDataStyleComponent component,
        string shortPattern,
        string longPattern)
    {
        result.Append(
            ReadComponentAttribute(component, "style") == "long"
                ? longPattern
                : shortPattern);
    }

    static int FindIntegerPatternEnd(
        StringBuilder result,
        int numberStart)
    {
        int index = numberStart;
        while (index < result.Length && result[index] != '.')
        {
            index = index + 1;
        }
        return index;
    }

    static void InsertEmbeddedTextPattern(
        StringBuilder result,
        OdfNode[] children,
        int numberStart,
        int integerEnd)
    {
        int index = 0;
        while (index < children.Length)
        {
            OdfNode child = children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Number
                && child.Name.LocalName == "embedded-text")
            {
                int position = ReadBoundedCount(
                    ReadNodeAttribute(child, "position"),
                    1);
                int digit = 0;
                int insertion = numberStart;
                int patternIndex = integerEnd - 1;
                while (patternIndex >= numberStart && digit < position)
                {
                    char character = result[patternIndex];
                    if (character == '0'
                        || character == '#'
                        || character == '?')
                    {
                        digit = digit + 1;
                        insertion = patternIndex;
                    }
                    patternIndex = patternIndex - 1;
                }
                string text = ReadDirectText(child.Children);
                result.Insert(insertion, text);
                integerEnd = integerEnd + text.Length;
            }
            index = index + 1;
        }
    }

    static string ReadNodeAttribute(OdfNode node, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Number
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
    }

    static string ReadComponentAttribute(
        OdfDataStyleComponent component,
        string localName)
    {
        int index = 0;
        while (index < component.Attributes.Length)
        {
            OdfStyleProperty property = component.Attributes[index];
            if (property.Name.NamespaceName == OdfNamespaces.Number
                && property.Name.LocalName == localName)
            {
                return property.LexicalValue;
            }
            index = index + 1;
        }
        return "";
    }

    static int ReadBoundedCount(string value, int fallback)
    {
        if (value.Length == 0)
        {
            return fallback;
        }
        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return fallback;
            }
            result = (result * 10) + (character - '0');
            if (result > 64)
            {
                return 64;
            }
            index = index + 1;
        }
        return result;
    }

    static void AppendRepeated(
        StringBuilder result,
        char character,
        int count)
    {
        int index = 0;
        while (index < count)
        {
            result.Append(character);
            index = index + 1;
        }
    }

    static string ReadDirectText(OdfNode[] children)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == OdfNodeKind.Text)
            {
                result.Append(children[index].Value);
            }
            index = index + 1;
        }
        return result.ToString();
    }

    static string SelectElementPrefix(OdfQualifiedName name)
    {
        if (name.NamespaceName.Length == 0)
        {
            return "";
        }
        if (name.Prefix.Length > 0)
        {
            return name.Prefix;
        }
        if (name.NamespaceName == OdfNamespaces.Number)
        {
            return OdfNamespaces.NumberPrefix;
        }
        if (name.NamespaceName == OdfNamespaces.Style)
        {
            return OdfNamespaces.StylePrefix;
        }
        throw new OdfDocumentException(
            "A qualified data-style component requires a namespace prefix.");
    }

    static void EnsureElementNamespace(
        OdfNode node,
        string prefix,
        string namespaceName)
    {
        if (prefix.Length == 0 && namespaceName.Length == 0)
        {
            return;
        }
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

    static void SetNodePartPath(OdfNode node, string partPath)
    {
        node.Location.PartPath = partPath;
        int index = 0;
        while (index < node.Attributes.Length)
        {
            node.Attributes[index].Location.PartPath = partPath;
            index = index + 1;
        }
        index = 0;
        while (index < node.NamespaceDeclarations.Length)
        {
            node.NamespaceDeclarations[index].Location.PartPath = partPath;
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            SetNodePartPath(node.Children[index], partPath);
            index = index + 1;
        }
    }

    static bool ComponentNodesMatch(
        OdfDataStyleComponent[] components,
        OdfNode[] nodes)
    {
        if (components.Length != nodes.Length)
        {
            return false;
        }
        int index = 0;
        while (index < components.Length)
        {
            if (components[index] == null
                || components[index].OriginalNode != nodes[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool MapNodesMatch(OdfDataStyleMap[] maps, OdfNode[] nodes)
    {
        if (maps.Length != nodes.Length)
        {
            return false;
        }
        int index = 0;
        while (index < maps.Length)
        {
            if (maps[index] == null
                || maps[index].OriginalNode != nodes[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool NodesMatch(OdfNode[] first, OdfNode[] second)
    {
        if (first == null || second == null || first.Length != second.Length)
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

    static void ResolveDataStyleRelationships(OdfDocument document)
    {
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            style.DataStyle = FindDataStyleByName(
                document.DataStyles,
                style.DataStyleName);
            if (style.DataStyle != null)
            {
                style.DataStyleNode = style.DataStyle.Node;
            }
            index = index + 1;
        }
    }

    static OdfDataStyle? FindDataStyleByName(
        OdfDataStyle[] styles,
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

    static void SnapshotDataStyle(OdfDataStyle style)
    {
        style.OriginalName = style.Name;
        style.OriginalDisplayName = style.DisplayName;
        style.OriginalKind = style.Kind;
        style.OriginalPartPath = style.PartPath;
        style.OriginalIsAutomatic = style.IsAutomatic;
        style.OriginalLanguage = style.Language;
        style.OriginalCountry = style.Country;
        style.OriginalScript = style.Script;
        style.OriginalLanguageTag = style.LanguageTag;
        style.OriginalTitle = style.Title;
        style.OriginalVolatile = style.Volatile;
        style.OriginalAutomaticOrder = style.AutomaticOrder;
        style.OriginalFormatSource = style.FormatSource;
        style.OriginalTruncateOnOverflow = style.TruncateOnOverflow;
        style.OriginalTransliterationFormat = style.TransliterationFormat;
        style.OriginalTransliterationLanguage = style.TransliterationLanguage;
        style.OriginalTransliterationCountry = style.TransliterationCountry;
        style.OriginalTransliterationStyle = style.TransliterationStyle;
        style.OriginalComponentNodes = ComponentNodes(style.Components);
        style.OriginalMapNodes = MapNodes(style.Maps);
        style.OriginalPassthroughContent =
            StylePropertyDocumentModel.CopyNodes(style.PassthroughContent);
    }

    static OdfNode[] ComponentNodes(OdfDataStyleComponent[] components)
    {
        OdfNode[] result = new OdfNode[components.Length];
        int index = 0;
        while (index < components.Length)
        {
            result[index] = components[index].OriginalNode ?? new OdfNode();
            index = index + 1;
        }
        return result;
    }

    static OdfNode[] MapNodes(OdfDataStyleMap[] maps)
    {
        OdfNode[] result = new OdfNode[maps.Length];
        int index = 0;
        while (index < maps.Length)
        {
            result[index] = maps[index].OriginalNode ?? new OdfNode();
            index = index + 1;
        }
        return result;
    }

    static void SnapshotDataStyleComponent(
        OdfDataStyleComponent component)
    {
        component.OriginalNamespaceName = component.Name.NamespaceName;
        component.OriginalLocalName = component.Name.LocalName;
        component.OriginalPrefix = component.Name.Prefix;
        component.OriginalText = component.Text;
    }

    static void SnapshotDataStyleMap(OdfDataStyleMap map)
    {
        map.OriginalCondition = map.Condition;
        map.OriginalApplyStyleName = map.ApplyStyleName;
        map.OriginalBaseCellAddress = map.BaseCellAddress;
    }
}
