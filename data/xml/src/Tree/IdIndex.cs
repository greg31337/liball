using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal class IdIndex
{
    internal XmlDocument? Document;
    internal long MutationVersion;
    internal Dictionary<string, XmlNode> Identifiers;

    internal IdIndex()
    {
        Document = null;
        MutationVersion = -1;
        Identifiers = new Dictionary<string, XmlNode>(StringComparer.Ordinal);
    }

    internal void BuildXmlIdentifierIndex(XmlDocument document)
    {
        Guard.NotNull(document, "document");
        Identifiers.Clear();
        XmlNode? container = document.DocumentNode;
        if (container != null)
        {
            AddXmlIdentifierSubtree(document, container);
        }

        Document = document;
        MutationVersion = document.MutationVersion;
    }

    internal XmlNode? FindIndexedXmlNode(string identifier)
    {
        Guard.NotNull(identifier, "identifier");
        if (Identifiers.ContainsKey(identifier))
        {
            return Identifiers[identifier];
        }

        return null;
    }

    internal void AddXmlIdentifierSubtree(XmlDocument document, XmlNode node)
    {
        if (node.Kind == XmlNodeKind.Element)
        {
            AddXmlElementIdentifiers(document, node);
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            AddXmlIdentifierSubtree(document, children[index]);
            index = index + 1;
        }
    }

    internal void AddXmlElementIdentifiers(XmlDocument document, XmlNode element)
    {
        XmlNode[] children = element.Children == null ? [] : element.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            XmlNode child = children[childIndex];
            XmlSchemaTypeInfo? childTypeInfo = child.TypeInfo;
            if (child.Kind == XmlNodeKind.Element && childTypeInfo != null && childTypeInfo.IsId)
            {
                string? schemaValue = childTypeInfo.NormalizedValue == null ? XmlIdentifierElementStringValue(child) : childTypeInfo.NormalizedValue;
                string normalizedSchemaId = CollapseXmlIdentifierWhitespace(schemaValue);
                if (IsValidXmlIdentifierNcName(normalizedSchemaId, document.Version) && !Identifiers.ContainsKey(normalizedSchemaId))
                {
                    Identifiers.Add(normalizedSchemaId, element);
                }
            }

            childIndex = childIndex + 1;
        }

        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            string? identifier = SelectXmlIdentifierValue(document, element, attribute);
            if (identifier != null && !Identifiers.ContainsKey(identifier))
            {
                Identifiers.Add(identifier, element);
            }

            index = index + 1;
        }
    }

    internal string XmlIdentifierElementStringValue(XmlNode element)
    {
        StringBuilder result = new StringBuilder();
        AppendXmlIdentifierElementValue(result, element);
        return result.ToString();
    }

    internal void AppendXmlIdentifierElementValue(StringBuilder result, XmlNode node)
    {
        if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
        {
            if (node.Value != null)
            {
                result.Append(node.Value);
            }

            return;
        }

        if (node.Kind == XmlNodeKind.Comment || node.Kind == XmlNodeKind.ProcessingInstruction || node.Kind == XmlNodeKind.DocumentType)
        {
            return;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            AppendXmlIdentifierElementValue(result, children[index]);
            index = index + 1;
        }
    }

    internal string? SelectXmlIdentifierValue(XmlDocument document, XmlNode element, XmlNode attribute)
    {
        if (IsXmlIdAttribute(attribute))
        {
            string normalizedXmlId = CollapseXmlIdentifierWhitespace(attribute.Value);
            return IsValidXmlIdentifierNcName(normalizedXmlId, document.Version) ? normalizedXmlId : null;
        }

        XmlSchemaTypeInfo? typeInfo = attribute.TypeInfo;
        if (typeInfo != null && typeInfo.IsId)
        {
            string? schemaValue = typeInfo.NormalizedValue == null ? attribute.Value : typeInfo.NormalizedValue;
            string normalizedSchemaId = CollapseXmlIdentifierWhitespace(schemaValue);
            return IsValidXmlIdentifierNcName(normalizedSchemaId, document.Version) ? normalizedSchemaId : null;
        }

        if (IsDtdIdAttribute(document, element, attribute))
        {
            string normalizedDtdId = CollapseXmlIdentifierWhitespace(attribute.Value);
            return normalizedDtdId.Length == 0 ? null : normalizedDtdId;
        }

        return null;
    }

    internal bool IsXmlIdAttribute(XmlNode attribute)
    {
        return IsXmlIdName(attribute.Name);
    }

    internal bool IsXmlIdName(XmlName? name)
    {
        if (name == null)
        {
            return false;
        }

        string? namespaceUri = name.NamespaceUri;
        bool localName = string.Equals(name.LocalName, "id", StringComparison.Ordinal);
        if (namespaceUri != null && namespaceUri.Length != 0)
        {
            return localName && string.Equals(namespaceUri, XmlNamespaces.Xml, StringComparison.Ordinal);
        }

        bool qualifiedName = string.Equals(name.QualifiedName, "xml:id", StringComparison.Ordinal);
        bool prefixedName = localName && string.Equals(name.Prefix, "xml", StringComparison.Ordinal);
        return qualifiedName || prefixedName;
    }

    internal bool IsDtdIdAttribute(XmlDocument document, XmlNode element, XmlNode attribute)
    {
        XmlDocumentType? documentType = document.DocumentType;
        if (documentType == null || documentType.AttributeDeclarations == null)
        {
            return false;
        }

        XmlAttributeDeclaration[] declarations = documentType.AttributeDeclarations;
        int index = 0;
        while (index < declarations.Length)
        {
            XmlAttributeDeclaration declaration = declarations[index];
            if (declaration.IsId && SameXmlIdentifierLexicalName(declaration.ElementName, element.Name) && SameXmlIdentifierLexicalName(declaration.Name, attribute.Name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool SameXmlIdentifierLexicalName(XmlName? left, XmlName? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        string? leftQualified = left.QualifiedName;
        string? rightQualified = right.QualifiedName;
        if (leftQualified != null && rightQualified != null)
        {
            return string.Equals(leftQualified, rightQualified, StringComparison.Ordinal);
        }

        return string.Equals(left.LocalName, right.LocalName, StringComparison.Ordinal);
    }

    internal string CollapseXmlIdentifierWhitespace(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return "";
        }

        StringBuilder result = new StringBuilder(value.Length);
        bool pendingSpace = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (XmlLexicalCharacterRules.IsXmlSpaceCharacter(character))
            {
                if (result.Length > 0)
                {
                    pendingSpace = true;
                }
            }
            else
            {
                if (pendingSpace)
                {
                    result.Append(' ');
                    pendingSpace = false;
                }

                result.Append(character);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal bool IsValidXmlIdentifierNcName(string value, XmlVersion version)
    {
        XmlNamespaceHandling handling = version == XmlVersion.Xml11 ? XmlNamespaceHandling.Namespaces11 : XmlNamespaceHandling.Namespaces10;
        return new QualifiedNameParser(handling, null).IsValidXmlNcName(value);
    }
}
