using System;
using System.Collections.Generic;
using System.Globalization;

namespace iso20022;

internal static class CanonicalXmlNodeNormalizer
{
    internal static DynamicXmlNode NormalizeDocument(DynamicXmlNode source)
    {
        DynamicXmlNode document = new DynamicXmlNode();
        document.Kind = source.Kind;
        document.Name = XmlPathTracker.CopyName(source.Name);
        document.Value = source.Value;
        document.Location = XmlPathTracker.CopyLocation(source.Location);
        int index = 0;
        while (index < source.Children.Count)
        {
            DynamicXmlNode child = source.Children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                document.Children.Add(NormalizeTopElement(child));
            }
            else
            {
                document.Children.Add(CloneNode(child));
            }

            index++;
        }

        return document;
    }

    internal static DynamicXmlNode NormalizeTopElement(DynamicXmlNode source)
    {
        List<string> namespaceUris = new List<string>();
        CollectNamespaces(source, namespaceUris);
        SortNamespaceUris(namespaceUris);
        return NormalizeNode(source, namespaceUris, true);
    }

    internal static void CollectNamespaces(DynamicXmlNode node, List<string> namespaceUris)
    {
        if (node.Kind == XmlNodeKind.Element)
        {
            AddNamespaceUri(namespaceUris, node.Name.NamespaceUri);
            int attributeIndex = 0;
            bool hasNilAttribute = false;
            while (attributeIndex < node.Attributes.Count)
            {
                XmlAttributeData attribute = node.Attributes[attributeIndex];
                AddNamespaceUri(namespaceUris, attribute.Name.NamespaceUri);
                if (attribute.Name.NamespaceUri == Iso20022Namespaces.XmlSchemaInstance && attribute.Name.LocalName == "nil")
                {
                    hasNilAttribute = true;
                }

                attributeIndex++;
            }

            if (node.IsNil && !hasNilAttribute)
            {
                AddNamespaceUri(namespaceUris, Iso20022Namespaces.XmlSchemaInstance);
            }
        }

        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            CollectNamespaces(node.Children[childIndex], namespaceUris);
            childIndex++;
        }
    }

    internal static void AddNamespaceUri(List<string> namespaceUris, string namespaceUri)
    {
        if (namespaceUri.Length == 0 || namespaceUri == Iso20022Namespaces.Xml)
        {
            return;
        }

        int index = 0;
        while (index < namespaceUris.Count)
        {
            if (namespaceUris[index] == namespaceUri)
            {
                return;
            }

            index++;
        }

        namespaceUris.Add(namespaceUri);
    }

    internal static void SortNamespaceUris(List<string> namespaceUris)
    {
        int outer = 1;
        while (outer < namespaceUris.Count)
        {
            string value = namespaceUris[outer];
            int inner = outer - 1;
            while (inner >= 0 && string.CompareOrdinal(namespaceUris[inner], value) > 0)
            {
                namespaceUris[inner + 1] = namespaceUris[inner];
                inner--;
            }

            namespaceUris[inner + 1] = value;
            outer++;
        }
    }

    internal static DynamicXmlNode NormalizeNode(DynamicXmlNode source, List<string> namespaceUris, bool isRoot)
    {
        DynamicXmlNode result = new DynamicXmlNode();
        result.Kind = source.Kind == XmlNodeKind.CData ? XmlNodeKind.Text : source.Kind;
        result.Name = NormalizeName(source.Name, namespaceUris);
        result.Value = source.Value;
        result.IsNil = source.IsNil;
        result.IsEmptyElement = false;
        result.Location = XmlPathTracker.CopyLocation(source.Location);
        if (source.Kind == XmlNodeKind.Element)
        {
            List<XmlAttributeData> attributes = new List<XmlAttributeData>();
            int attributeIndex = 0;
            bool hasNilAttribute = false;
            while (attributeIndex < source.Attributes.Count)
            {
                XmlAttributeData copy = CopyCanonicalAttribute(source.Attributes[attributeIndex], namespaceUris);
                attributes.Add(copy);
                if (copy.Name.NamespaceUri == Iso20022Namespaces.XmlSchemaInstance && copy.Name.LocalName == "nil")
                {
                    hasNilAttribute = true;
                }

                attributeIndex++;
            }

            if (source.IsNil && !hasNilAttribute)
            {
                XmlAttributeData nilAttribute = new XmlAttributeData();
                nilAttribute.Name.NamespaceUri = Iso20022Namespaces.XmlSchemaInstance;
                nilAttribute.Name.LocalName = "nil";
                nilAttribute.Name.Prefix = GetPrefix(namespaceUris, Iso20022Namespaces.XmlSchemaInstance);
                nilAttribute.Value = "true";
                nilAttribute.IsSpecified = true;
                attributes.Add(nilAttribute);
            }

            SortAttributes(attributes);
            result.Attributes = attributes;
            if (isRoot)
            {
                int namespaceIndex = 0;
                while (namespaceIndex < namespaceUris.Count)
                {
                    NamespaceDeclarationData declaration = new NamespaceDeclarationData();
                    declaration.Prefix = "n" + namespaceIndex.ToString(CultureInfo.InvariantCulture);
                    declaration.NamespaceUri = namespaceUris[namespaceIndex];
                    result.NamespaceDeclarations.Add(declaration);
                    namespaceIndex++;
                }
            }
        }

        int childIndex = 0;
        while (childIndex < source.Children.Count)
        {
            result.Children.Add(NormalizeNode(source.Children[childIndex], namespaceUris, false));
            childIndex++;
        }

        return result;
    }

    internal static QualifiedNameData NormalizeName(QualifiedNameData source, List<string> namespaceUris)
    {
        QualifiedNameData name = new QualifiedNameData();
        name.NamespaceUri = source.NamespaceUri;
        name.LocalName = source.LocalName;
        name.Prefix = GetPrefix(namespaceUris, source.NamespaceUri);
        return name;
    }

    internal static string GetPrefix(List<string> namespaceUris, string namespaceUri)
    {
        if (namespaceUri.Length == 0)
        {
            return string.Empty;
        }

        if (namespaceUri == Iso20022Namespaces.Xml)
        {
            return "xml";
        }

        int index = 0;
        while (index < namespaceUris.Count)
        {
            if (namespaceUris[index] == namespaceUri)
            {
                return "n" + index.ToString(CultureInfo.InvariantCulture);
            }

            index++;
        }

        throw new InvalidOperationException("A canonical namespace prefix was not assigned.");
    }

    internal static XmlAttributeData CopyCanonicalAttribute(XmlAttributeData source, List<string> namespaceUris)
    {
        XmlAttributeData copy = new XmlAttributeData();
        copy.Name = NormalizeName(source.Name, namespaceUris);
        copy.Value = source.Value;
        copy.IsSpecified = source.IsSpecified;
        copy.Location = XmlPathTracker.CopyLocation(source.Location);
        return copy;
    }

    internal static void SortAttributes(List<XmlAttributeData> attributes)
    {
        int outer = 1;
        while (outer < attributes.Count)
        {
            XmlAttributeData value = attributes[outer];
            int inner = outer - 1;
            while (inner >= 0 && CompareAttributes(attributes[inner], value) > 0)
            {
                attributes[inner + 1] = attributes[inner];
                inner--;
            }

            attributes[inner + 1] = value;
            outer++;
        }
    }

    internal static int CompareAttributes(XmlAttributeData first, XmlAttributeData second)
    {
        int namespaceComparison = string.CompareOrdinal(first.Name.NamespaceUri, second.Name.NamespaceUri);
        if (namespaceComparison != 0)
        {
            return namespaceComparison;
        }

        return string.CompareOrdinal(first.Name.LocalName, second.Name.LocalName);
    }

    internal static DynamicXmlNode CloneNode(DynamicXmlNode source)
    {
        DynamicXmlNode copy = new DynamicXmlNode();
        copy.Kind = source.Kind;
        copy.Name = XmlPathTracker.CopyName(source.Name);
        copy.Value = source.Value;
        copy.IsNil = source.IsNil;
        copy.IsEmptyElement = source.IsEmptyElement;
        copy.Location = XmlPathTracker.CopyLocation(source.Location);
        copy.NamespaceDeclarations = GenericXmlNodeWriter.CopyDeclarations(source.NamespaceDeclarations);
        copy.Attributes = GenericXmlNodeWriter.CopyAttributes(source.Attributes);
        int index = 0;
        while (index < source.Children.Count)
        {
            copy.Children.Add(CloneNode(source.Children[index]));
            index++;
        }

        return copy;
    }
}
