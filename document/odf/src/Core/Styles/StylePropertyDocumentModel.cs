using System;
using System.Collections.Generic;

namespace odf;

internal static class StylePropertyDocumentModel
{
    internal static void LoadStyleProperties(OdfStyle style, OdfNode styleNode)
    {
        OdfNode? textNode = FindPropertySet(styleNode, "text-properties");
        OdfNode? paragraphNode = FindPropertySet(
            styleNode,
            "paragraph-properties");
        style.TextPropertiesNode = textNode;
        style.OriginalTextPropertiesNode = textNode;
        style.TextProperties = ReadPropertyAttributes(textNode);
        style.OriginalTextPropertyAttributes = AttributeNodes(
            style.TextProperties);
        style.TextPropertyChildren = PropertyChildren(textNode);
        style.OriginalTextPropertyChildren = CopyNodes(
            style.TextPropertyChildren);
        style.ParagraphPropertiesNode = paragraphNode;
        style.OriginalParagraphPropertiesNode = paragraphNode;
        style.ParagraphProperties = ReadPropertyAttributes(paragraphNode);
        style.OriginalParagraphPropertyAttributes = AttributeNodes(
            style.ParagraphProperties);
        style.ParagraphPropertyChildren = PropertyChildren(paragraphNode);
        style.OriginalParagraphPropertyChildren = CopyNodes(
            style.ParagraphPropertyChildren);
        OdfNode? sectionNode = FindPropertySet(
            styleNode,
            "section-properties");
        style.SectionPropertiesNode = sectionNode;
        style.OriginalSectionPropertiesNode = sectionNode;
        style.SectionProperties = ReadPropertyAttributes(sectionNode);
        style.OriginalSectionPropertyAttributes = AttributeNodes(
            style.SectionProperties);
        style.SectionPropertyChildren = PropertyChildren(sectionNode);
        style.OriginalSectionPropertyChildren = CopyNodes(
            style.SectionPropertyChildren);
        OdfNode? tableNode = FindPropertySet(styleNode, "table-properties");
        style.TablePropertiesNode = tableNode;
        style.OriginalTablePropertiesNode = tableNode;
        style.TableProperties = ReadPropertyAttributes(tableNode);
        style.OriginalTablePropertyAttributes = AttributeNodes(
            style.TableProperties);
        style.TablePropertyChildren = PropertyChildren(tableNode);
        style.OriginalTablePropertyChildren = CopyNodes(
            style.TablePropertyChildren);
        OdfNode? columnNode = FindPropertySet(
            styleNode,
            "table-column-properties");
        style.TableColumnPropertiesNode = columnNode;
        style.OriginalTableColumnPropertiesNode = columnNode;
        style.TableColumnProperties = ReadPropertyAttributes(columnNode);
        style.OriginalTableColumnPropertyAttributes = AttributeNodes(
            style.TableColumnProperties);
        style.TableColumnPropertyChildren = PropertyChildren(columnNode);
        style.OriginalTableColumnPropertyChildren = CopyNodes(
            style.TableColumnPropertyChildren);
        OdfNode? rowNode = FindPropertySet(
            styleNode,
            "table-row-properties");
        style.TableRowPropertiesNode = rowNode;
        style.OriginalTableRowPropertiesNode = rowNode;
        style.TableRowProperties = ReadPropertyAttributes(rowNode);
        style.OriginalTableRowPropertyAttributes = AttributeNodes(
            style.TableRowProperties);
        style.TableRowPropertyChildren = PropertyChildren(rowNode);
        style.OriginalTableRowPropertyChildren = CopyNodes(
            style.TableRowPropertyChildren);
        OdfNode? cellNode = FindPropertySet(
            styleNode,
            "table-cell-properties");
        style.TableCellPropertiesNode = cellNode;
        style.OriginalTableCellPropertiesNode = cellNode;
        style.TableCellProperties = ReadPropertyAttributes(cellNode);
        style.OriginalTableCellPropertyAttributes = AttributeNodes(
            style.TableCellProperties);
        style.TableCellPropertyChildren = PropertyChildren(cellNode);
        style.OriginalTableCellPropertyChildren = CopyNodes(
            style.TableCellPropertyChildren);
        OdfNode? graphicNode = FindPropertySet(
            styleNode,
            "graphic-properties");
        style.GraphicPropertiesNode = graphicNode;
        style.OriginalGraphicPropertiesNode = graphicNode;
        style.GraphicProperties = ReadPropertyAttributes(graphicNode);
        style.OriginalGraphicPropertyAttributes = AttributeNodes(
            style.GraphicProperties);
        style.GraphicPropertyChildren = PropertyChildren(graphicNode);
        style.OriginalGraphicPropertyChildren = CopyNodes(
            style.GraphicPropertyChildren);
        OdfNode? chartNode = FindPropertySet(styleNode, "chart-properties");
        style.ChartPropertiesNode = chartNode;
        style.OriginalChartPropertiesNode = chartNode;
        style.ChartProperties = ReadPropertyAttributes(chartNode);
        style.OriginalChartPropertyAttributes = AttributeNodes(
            style.ChartProperties);
        style.ChartPropertyChildren = PropertyChildren(chartNode);
        style.OriginalChartPropertyChildren = CopyNodes(
            style.ChartPropertyChildren);
        OdfNode? drawingPageNode = FindPropertySet(
            styleNode,
            "drawing-page-properties");
        style.DrawingPagePropertiesNode = drawingPageNode;
        style.OriginalDrawingPagePropertiesNode = drawingPageNode;
        style.DrawingPageProperties = ReadPropertyAttributes(drawingPageNode);
        style.OriginalDrawingPagePropertyAttributes = AttributeNodes(
            style.DrawingPageProperties);
        style.DrawingPagePropertyChildren = PropertyChildren(drawingPageNode);
        style.OriginalDrawingPagePropertyChildren = CopyNodes(
            style.DrawingPagePropertyChildren);
        OdfNode? rubyNode = FindPropertySet(styleNode, "ruby-properties");
        style.RubyPropertiesNode = rubyNode;
        style.OriginalRubyPropertiesNode = rubyNode;
        style.RubyProperties = ReadPropertyAttributes(rubyNode);
        style.OriginalRubyPropertyAttributes = AttributeNodes(
            style.RubyProperties);
        style.RubyPropertyChildren = PropertyChildren(rubyNode);
        style.OriginalRubyPropertyChildren = CopyNodes(
            style.RubyPropertyChildren);
        OdfNode? listNode = FindPropertySet(
            styleNode,
            "list-level-properties");
        style.ListPropertiesNode = listNode;
        style.OriginalListPropertiesNode = listNode;
        style.ListProperties = ReadPropertyAttributes(listNode);
        style.OriginalListPropertyAttributes = AttributeNodes(
            style.ListProperties);
        style.ListPropertyChildren = PropertyChildren(listNode);
        style.OriginalListPropertyChildren = CopyNodes(
            style.ListPropertyChildren);
    }

    internal static void SynchronizeStyleProperties(
        OdfStyle style,
        OdfNode styleNode)
    {
        OdfNode? textNode = SynchronizePropertySet(
            styleNode,
            "text-properties",
            style.TextProperties,
            style.TextPropertyChildren,
            style.OriginalTextPropertiesNode,
            style.OriginalTextPropertyAttributes,
            style.OriginalTextPropertyChildren);
        style.TextPropertiesNode = textNode;
        style.OriginalTextPropertiesNode = textNode;
        style.OriginalTextPropertyAttributes = AttributeNodes(
            style.TextProperties);
        style.OriginalTextPropertyChildren = CopyNodes(
            style.TextPropertyChildren);
        OdfNode? paragraphNode = SynchronizePropertySet(
            styleNode,
            "paragraph-properties",
            style.ParagraphProperties,
            style.ParagraphPropertyChildren,
            style.OriginalParagraphPropertiesNode,
            style.OriginalParagraphPropertyAttributes,
            style.OriginalParagraphPropertyChildren);
        style.ParagraphPropertiesNode = paragraphNode;
        style.OriginalParagraphPropertiesNode = paragraphNode;
        style.OriginalParagraphPropertyAttributes = AttributeNodes(
            style.ParagraphProperties);
        style.OriginalParagraphPropertyChildren = CopyNodes(
            style.ParagraphPropertyChildren);
        OdfNode? sectionNode = SynchronizePropertySet(
            styleNode,
            "section-properties",
            style.SectionProperties,
            style.SectionPropertyChildren,
            style.OriginalSectionPropertiesNode,
            style.OriginalSectionPropertyAttributes,
            style.OriginalSectionPropertyChildren);
        style.SectionPropertiesNode = sectionNode;
        style.OriginalSectionPropertiesNode = sectionNode;
        style.OriginalSectionPropertyAttributes = AttributeNodes(
            style.SectionProperties);
        style.OriginalSectionPropertyChildren = CopyNodes(
            style.SectionPropertyChildren);
        OdfNode? tableNode = SynchronizePropertySet(
            styleNode,
            "table-properties",
            style.TableProperties,
            style.TablePropertyChildren,
            style.OriginalTablePropertiesNode,
            style.OriginalTablePropertyAttributes,
            style.OriginalTablePropertyChildren);
        style.TablePropertiesNode = tableNode;
        style.OriginalTablePropertiesNode = tableNode;
        style.OriginalTablePropertyAttributes = AttributeNodes(
            style.TableProperties);
        style.OriginalTablePropertyChildren = CopyNodes(
            style.TablePropertyChildren);
        OdfNode? columnNode = SynchronizePropertySet(
            styleNode,
            "table-column-properties",
            style.TableColumnProperties,
            style.TableColumnPropertyChildren,
            style.OriginalTableColumnPropertiesNode,
            style.OriginalTableColumnPropertyAttributes,
            style.OriginalTableColumnPropertyChildren);
        style.TableColumnPropertiesNode = columnNode;
        style.OriginalTableColumnPropertiesNode = columnNode;
        style.OriginalTableColumnPropertyAttributes = AttributeNodes(
            style.TableColumnProperties);
        style.OriginalTableColumnPropertyChildren = CopyNodes(
            style.TableColumnPropertyChildren);
        OdfNode? rowNode = SynchronizePropertySet(
            styleNode,
            "table-row-properties",
            style.TableRowProperties,
            style.TableRowPropertyChildren,
            style.OriginalTableRowPropertiesNode,
            style.OriginalTableRowPropertyAttributes,
            style.OriginalTableRowPropertyChildren);
        style.TableRowPropertiesNode = rowNode;
        style.OriginalTableRowPropertiesNode = rowNode;
        style.OriginalTableRowPropertyAttributes = AttributeNodes(
            style.TableRowProperties);
        style.OriginalTableRowPropertyChildren = CopyNodes(
            style.TableRowPropertyChildren);
        OdfNode? cellNode = SynchronizePropertySet(
            styleNode,
            "table-cell-properties",
            style.TableCellProperties,
            style.TableCellPropertyChildren,
            style.OriginalTableCellPropertiesNode,
            style.OriginalTableCellPropertyAttributes,
            style.OriginalTableCellPropertyChildren);
        style.TableCellPropertiesNode = cellNode;
        style.OriginalTableCellPropertiesNode = cellNode;
        style.OriginalTableCellPropertyAttributes = AttributeNodes(
            style.TableCellProperties);
        style.OriginalTableCellPropertyChildren = CopyNodes(
            style.TableCellPropertyChildren);
        OdfNode? graphicNode = SynchronizePropertySet(
            styleNode,
            "graphic-properties",
            style.GraphicProperties,
            style.GraphicPropertyChildren,
            style.OriginalGraphicPropertiesNode,
            style.OriginalGraphicPropertyAttributes,
            style.OriginalGraphicPropertyChildren);
        style.GraphicPropertiesNode = graphicNode;
        style.OriginalGraphicPropertiesNode = graphicNode;
        style.OriginalGraphicPropertyAttributes = AttributeNodes(
            style.GraphicProperties);
        style.OriginalGraphicPropertyChildren = CopyNodes(
            style.GraphicPropertyChildren);
        OdfNode? chartNode = SynchronizePropertySet(
            styleNode,
            "chart-properties",
            style.ChartProperties,
            style.ChartPropertyChildren,
            style.OriginalChartPropertiesNode,
            style.OriginalChartPropertyAttributes,
            style.OriginalChartPropertyChildren);
        style.ChartPropertiesNode = chartNode;
        style.OriginalChartPropertiesNode = chartNode;
        style.OriginalChartPropertyAttributes = AttributeNodes(
            style.ChartProperties);
        style.OriginalChartPropertyChildren = CopyNodes(
            style.ChartPropertyChildren);
        OdfNode? drawingPageNode = SynchronizePropertySet(
            styleNode,
            "drawing-page-properties",
            style.DrawingPageProperties,
            style.DrawingPagePropertyChildren,
            style.OriginalDrawingPagePropertiesNode,
            style.OriginalDrawingPagePropertyAttributes,
            style.OriginalDrawingPagePropertyChildren);
        style.DrawingPagePropertiesNode = drawingPageNode;
        style.OriginalDrawingPagePropertiesNode = drawingPageNode;
        style.OriginalDrawingPagePropertyAttributes = AttributeNodes(
            style.DrawingPageProperties);
        style.OriginalDrawingPagePropertyChildren = CopyNodes(
            style.DrawingPagePropertyChildren);
        OdfNode? rubyNode = SynchronizePropertySet(
            styleNode,
            "ruby-properties",
            style.RubyProperties,
            style.RubyPropertyChildren,
            style.OriginalRubyPropertiesNode,
            style.OriginalRubyPropertyAttributes,
            style.OriginalRubyPropertyChildren);
        style.RubyPropertiesNode = rubyNode;
        style.OriginalRubyPropertiesNode = rubyNode;
        style.OriginalRubyPropertyAttributes = AttributeNodes(
            style.RubyProperties);
        style.OriginalRubyPropertyChildren = CopyNodes(
            style.RubyPropertyChildren);
        OdfNode? listNode = SynchronizePropertySet(
            styleNode,
            "list-level-properties",
            style.ListProperties,
            style.ListPropertyChildren,
            style.OriginalListPropertiesNode,
            style.OriginalListPropertyAttributes,
            style.OriginalListPropertyChildren);
        style.ListPropertiesNode = listNode;
        style.OriginalListPropertiesNode = listNode;
        style.OriginalListPropertyAttributes = AttributeNodes(
            style.ListProperties);
        style.OriginalListPropertyChildren = CopyNodes(
            style.ListPropertyChildren);
    }

    static OdfNode? FindPropertySet(OdfNode styleNode, string localName)
    {
        int index = 0;
        while (index < styleNode.Children.Length)
        {
            OdfNode child = styleNode.Children[index];
            if (IsPropertySet(child, localName))
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    internal static OdfStyleProperty[] ReadPropertyAttributes(OdfNode? node)
    {
        if (node == null)
        {
            return [];
        }
        OdfStyleProperty[] result = new OdfStyleProperty[
            node.Attributes.Length];
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            OdfStyleProperty property = new OdfStyleProperty();
            property.Name = CopyQualifiedName(attribute.Name);
            property.LexicalValue = attribute.Value;
            property.Attribute = attribute;
            property.OriginalAttribute = attribute;
            SnapshotProperty(property);
            result[index] = property;
            index = index + 1;
        }
        return result;
    }

    internal static OdfNode[] PropertyChildren(OdfNode? node)
    {
        if (node == null)
        {
            return [];
        }
        return CopyNodes(node.Children);
    }

    internal static void SynchronizeElementAttributes(
        OdfNode node,
        OdfStyleProperty[] properties,
        OdfAttribute[] originalAttributes)
    {
        if (node == null
            || properties == null
            || originalAttributes == null)
        {
            throw new OdfDocumentException(
                "An element and attribute collections are required for writing.");
        }
        SynchronizePropertyAttributes(
            node,
            properties,
            originalAttributes);
    }

    internal static OdfNode? SynchronizePropertySet(
        OdfNode styleNode,
        string localName,
        OdfStyleProperty[] properties,
        OdfNode[] children,
        OdfNode? originalNode,
        OdfAttribute[] originalAttributes,
        OdfNode[] originalChildren)
    {
        if (properties == null || children == null)
        {
            throw new OdfDocumentException(
                "Style property and child collections are required for writing.");
        }
        bool changed = PropertySetChanged(
            properties,
            children,
            originalAttributes,
            originalChildren);
        bool needsNode = properties.Length > 0 || children.Length > 0;
        OdfNode? node = originalNode;
        if (node == null && needsNode)
        {
            node = CreatePropertySet(localName, styleNode.Location.PartPath);
            InsertPropertySet(styleNode, node, localName);
        }
        else if (node != null && node.Parent == null && needsNode)
        {
            InsertPropertySet(styleNode, node, localName);
        }
        if (node == null)
        {
            return null;
        }
        SynchronizePropertyAttributes(node, properties, originalAttributes);
        if (!NodesMatch(children, originalChildren))
        {
            SynchronizePropertyChildren(node, children);
        }
        if (changed
            && node.Attributes.Length == 0
            && node.Children.Length == 0)
        {
            RemovePropertySet(node);
            return null;
        }
        return node;
    }

    static bool PropertySetChanged(
        OdfStyleProperty[] properties,
        OdfNode[] children,
        OdfAttribute[] originalAttributes,
        OdfNode[] originalChildren)
    {
        if (properties.Length != originalAttributes.Length
            || !NodesMatch(children, originalChildren))
        {
            return true;
        }
        int index = 0;
        while (index < properties.Length)
        {
            OdfStyleProperty property = properties[index];
            if (property == null
                || property.OriginalAttribute != originalAttributes[index]
                || property.Name == null
                || property.Name.NamespaceName
                    != property.OriginalNamespaceName
                || property.Name.LocalName != property.OriginalLocalName
                || property.Name.Prefix != property.OriginalPrefix
                || property.LexicalValue
                    != property.OriginalLexicalValue)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void SynchronizePropertyAttributes(
        OdfNode node,
        OdfStyleProperty[] properties,
        OdfAttribute[] originalAttributes)
    {
        int index = 0;
        while (index < originalAttributes.Length)
        {
            OdfAttribute attribute = originalAttributes[index];
            if (!RetainsAttribute(properties, attribute))
            {
                RemovePropertyAttribute(node, attribute);
            }
            index = index + 1;
        }
        index = 0;
        while (index < properties.Length)
        {
            OdfStyleProperty property = properties[index];
            ValidateProperty(properties, property, index);
            string prefix = SelectPropertyPrefix(property);
            property.Name.Prefix = prefix;
            OdfAttribute? attribute = property.OriginalAttribute;
            bool typedChanged = PropertyChanged(property);
            bool moved = attribute != null
                && !AttributeWasOriginal(originalAttributes, attribute);
            if (attribute == null)
            {
                attribute = new OdfAttribute();
                AppendPropertyAttribute(node, attribute);
                property.OriginalAttribute = attribute;
                typedChanged = true;
            }
            else if (!ContainsPropertyAttribute(node, attribute))
            {
                if (typedChanged || moved)
                {
                    AppendPropertyAttribute(node, attribute);
                }
            }
            if (typedChanged)
            {
                attribute.Name.NamespaceName = property.Name.NamespaceName;
                attribute.Name.LocalName = property.Name.LocalName;
                attribute.Name.Prefix = prefix;
                attribute.Value = property.LexicalValue;
            }
            if (ContainsPropertyAttribute(node, attribute))
            {
                attribute.Location.PartPath = node.Location.PartPath;
                EnsurePropertyPrefix(
                    node,
                    attribute.Name.Prefix,
                    attribute.Name.NamespaceName);
            }
            property.Attribute = attribute;
            SnapshotProperty(property);
            index = index + 1;
        }
    }

    static bool PropertyChanged(OdfStyleProperty property)
    {
        return property.Name.NamespaceName != property.OriginalNamespaceName
            || property.Name.LocalName != property.OriginalLocalName
            || property.Name.Prefix != property.OriginalPrefix
            || property.LexicalValue != property.OriginalLexicalValue;
    }

    static bool AttributeWasOriginal(
        OdfAttribute[] originalAttributes,
        OdfAttribute attribute)
    {
        int index = 0;
        while (index < originalAttributes.Length)
        {
            if (originalAttributes[index] == attribute)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void ValidateProperty(
        OdfStyleProperty[] properties,
        OdfStyleProperty property,
        int propertyIndex)
    {
        if (property == null || property.Name == null)
        {
            throw new OdfDocumentException(
                "A style property and qualified name are required for writing.");
        }
        if (property.Name.LocalName.Length == 0)
        {
            throw new OdfDocumentException(
                "A style property local name is required for writing.");
        }
        int index = 0;
        while (index < propertyIndex)
        {
            OdfStyleProperty earlier = properties[index];
            if (earlier != null
                && earlier.Name != null
                && earlier.Name.NamespaceName == property.Name.NamespaceName
                && earlier.Name.LocalName == property.Name.LocalName)
            {
                throw new OdfDocumentException(
                    "A style property set cannot contain duplicate qualified names.");
            }
            index = index + 1;
        }
    }

    static string SelectPropertyPrefix(OdfStyleProperty property)
    {
        string namespaceName = property.Name.NamespaceName;
        string prefix = property.Name.Prefix;
        if (namespaceName.Length == 0)
        {
            if (prefix.Length > 0)
            {
                throw new OdfDocumentException(
                    "An unqualified style property cannot use a namespace prefix.");
            }
            return "";
        }
        if (namespaceName == OdfNamespaces.Xml)
        {
            return OdfNamespaces.XmlPrefix;
        }
        if (prefix.Length == 0)
        {
            prefix = PreferredPropertyPrefix(namespaceName);
        }
        if (prefix.Length == 0)
        {
            throw new OdfDocumentException(
                "A qualified style property requires a namespace prefix.");
        }
        return prefix;
    }

    static string PreferredPropertyPrefix(string namespaceName)
    {
        if (namespaceName == OdfNamespaces.Fo)
        {
            return OdfNamespaces.FoPrefix;
        }
        if (namespaceName == OdfNamespaces.Style)
        {
            return OdfNamespaces.StylePrefix;
        }
        if (namespaceName == OdfNamespaces.Text)
        {
            return OdfNamespaces.TextPrefix;
        }
        if (namespaceName == OdfNamespaces.Svg)
        {
            return OdfNamespaces.SvgPrefix;
        }
        if (namespaceName == OdfNamespaces.Drawing)
        {
            return OdfNamespaces.DrawingPrefix;
        }
        if (namespaceName == OdfNamespaces.Table)
        {
            return OdfNamespaces.TablePrefix;
        }
        if (namespaceName == OdfNamespaces.Chart)
        {
            return OdfNamespaces.ChartPrefix;
        }
        if (namespaceName == OdfNamespaces.Presentation)
        {
            return OdfNamespaces.PresentationPrefix;
        }
        if (namespaceName == OdfNamespaces.Dr3d)
        {
            return OdfNamespaces.Dr3dPrefix;
        }
        if (namespaceName == OdfNamespaces.Number)
        {
            return OdfNamespaces.NumberPrefix;
        }
        if (namespaceName == OdfNamespaces.XLink)
        {
            return OdfNamespaces.XLinkPrefix;
        }
        if (namespaceName == OdfNamespaces.Office)
        {
            return OdfNamespaces.OfficePrefix;
        }
        if (namespaceName == OdfNamespaces.Smil)
        {
            return OdfNamespaces.SmilPrefix;
        }
        return "";
    }

    static void EnsurePropertyPrefix(
        OdfNode node,
        string prefix,
        string namespaceName)
    {
        if (prefix.Length == 0 || prefix == OdfNamespaces.XmlPrefix)
        {
            return;
        }
        OdfNode? current = node;
        bool searching = true;
        while (current != null && searching)
        {
            int index = 0;
            while (index < current.NamespaceDeclarations.Length)
            {
                OdfNamespaceDeclaration declaration =
                    current.NamespaceDeclarations[index];
                if (declaration.Prefix == prefix)
                {
                    if (declaration.NamespaceName == namespaceName)
                    {
                        return;
                    }
                    searching = false;
                    index = current.NamespaceDeclarations.Length;
                }
                else
                {
                    index = index + 1;
                }
            }
            if (searching)
            {
                current = current.Parent;
            }
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

    static bool RetainsAttribute(
        OdfStyleProperty[] properties,
        OdfAttribute attribute)
    {
        int index = 0;
        while (index < properties.Length)
        {
            OdfStyleProperty property = properties[index];
            if (property != null && property.OriginalAttribute == attribute)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsPropertyAttribute(
        OdfNode node,
        OdfAttribute attribute)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index] == attribute)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void AppendPropertyAttribute(
        OdfNode node,
        OdfAttribute attribute)
    {
        OdfAttribute[] attributes = new OdfAttribute[
            node.Attributes.Length + 1];
        Array.Copy(node.Attributes, attributes, node.Attributes.Length);
        attributes[attributes.Length - 1] = attribute;
        node.Attributes = attributes;
    }

    static void RemovePropertyAttribute(
        OdfNode node,
        OdfAttribute attribute)
    {
        int match = -1;
        int index = 0;
        while (index < node.Attributes.Length && match < 0)
        {
            if (node.Attributes[index] == attribute)
            {
                match = index;
            }
            index = index + 1;
        }
        if (match < 0)
        {
            return;
        }
        OdfAttribute[] attributes = new OdfAttribute[
            node.Attributes.Length - 1];
        int source = 0;
        int destination = 0;
        while (source < node.Attributes.Length)
        {
            if (source != match)
            {
                attributes[destination] = node.Attributes[source];
                destination = destination + 1;
            }
            source = source + 1;
        }
        node.Attributes = attributes;
    }

    static void SynchronizePropertyChildren(
        OdfNode node,
        OdfNode[] children)
    {
        int index = 0;
        while (index < children.Length)
        {
            OdfNode child = children[index];
            if (child == null)
            {
                throw new OdfDocumentException(
                    "A style property child collection cannot contain null.");
            }
            int earlier = 0;
            while (earlier < index)
            {
                if (children[earlier] == child)
                {
                    throw new OdfDocumentException(
                        "A style property child cannot occur more than once.");
                }
                earlier = earlier + 1;
            }
            if (child.Parent != null && child.Parent != node)
            {
                RemovePropertySetChild(child);
            }
            child.Parent = node;
            if (child.Location.PartPath.Length == 0)
            {
                child.Location.PartPath = node.Location.PartPath;
            }
            index = index + 1;
        }
        index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (!RetainsChild(children, child))
            {
                child.Parent = null;
            }
            index = index + 1;
        }
        node.Children = CopyNodes(children);
        node.WasEmptyElement = children.Length == 0;
    }

    static bool RetainsChild(OdfNode[] children, OdfNode child)
    {
        int index = 0;
        while (index < children.Length)
        {
            if (children[index] == child)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RemovePropertySetChild(OdfNode child)
    {
        OdfNode? parent = child.Parent;
        if (parent == null)
        {
            return;
        }
        List<OdfNode> children = new List<OdfNode>();
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (parent.Children[index] != child)
            {
                children.Add(parent.Children[index]);
            }
            index = index + 1;
        }
        parent.Children = children.ToArray();
        child.Parent = null;
    }

    static OdfNode CreatePropertySet(string localName, string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = new OdfQualifiedName();
        result.Name.NamespaceName = OdfNamespaces.Style;
        result.Name.LocalName = localName;
        result.Name.Prefix = OdfNamespaces.StylePrefix;
        result.Location.PartPath = partPath;
        return result;
    }

    static void InsertPropertySet(
        OdfNode styleNode,
        OdfNode propertyNode,
        string localName)
    {
        int insertion = styleNode.Children.Length;
        int index = 0;
        while (index < styleNode.Children.Length)
        {
            OdfNode child = styleNode.Children[index];
            if (PropertySetRank(child) > RequestedPropertySetRank(localName))
            {
                insertion = index;
                index = styleNode.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        OdfNode[] children = new OdfNode[styleNode.Children.Length + 1];
        Array.Copy(styleNode.Children, 0, children, 0, insertion);
        children[insertion] = propertyNode;
        Array.Copy(
            styleNode.Children,
            insertion,
            children,
            insertion + 1,
            styleNode.Children.Length - insertion);
        styleNode.Children = children;
        propertyNode.Parent = styleNode;
        propertyNode.Location.PartPath = styleNode.Location.PartPath;
        styleNode.WasEmptyElement = false;
    }

    static int PropertySetRank(OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Style)
        {
            if (node.Name.LocalName == "map")
            {
                return 200;
            }
            if (node.Name.LocalName.EndsWith(
                "-properties",
                StringComparison.Ordinal))
            {
                return RequestedPropertySetRank(node.Name.LocalName);
            }
        }
        return 50;
    }

    static int RequestedPropertySetRank(string localName)
    {
        if (localName == "text-properties")
        {
            return 40;
        }
        if (localName == "paragraph-properties")
        {
            return 30;
        }
        if (localName == "graphic-properties")
        {
            return 20;
        }
        return 10;
    }

    static void RemovePropertySet(OdfNode node)
    {
        OdfNode? parent = node.Parent;
        if (parent == null)
        {
            return;
        }
        List<OdfNode> children = new List<OdfNode>();
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (parent.Children[index] != node)
            {
                children.Add(parent.Children[index]);
            }
            index = index + 1;
        }
        parent.Children = children.ToArray();
        node.Parent = null;
    }

    static bool IsPropertySet(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Style
            && node.Name.LocalName == localName;
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

    internal static OdfAttribute[] AttributeNodes(OdfStyleProperty[] properties)
    {
        List<OdfAttribute> result = new List<OdfAttribute>();
        int index = 0;
        while (index < properties.Length)
        {
            OdfStyleProperty property = properties[index];
            if (property != null && property.OriginalAttribute != null)
            {
                result.Add(property.OriginalAttribute);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal static OdfNode[] CopyNodes(OdfNode[] source)
    {
        OdfNode[] result = new OdfNode[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static OdfQualifiedName CopyQualifiedName(OdfQualifiedName source)
    {
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = source.NamespaceName;
        result.LocalName = source.LocalName;
        result.Prefix = source.Prefix;
        return result;
    }

    static void SnapshotProperty(OdfStyleProperty property)
    {
        property.OriginalNamespaceName = property.Name.NamespaceName;
        property.OriginalLocalName = property.Name.LocalName;
        property.OriginalPrefix = property.Name.Prefix;
        property.OriginalLexicalValue = property.LexicalValue;
    }
}
