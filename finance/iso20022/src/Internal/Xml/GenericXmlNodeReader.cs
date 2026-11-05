using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace iso20022;

internal class GenericXmlNodeReader
{
    MessageReadOptions options;
    internal List<MessageRepair> Repairs = new List<MessageRepair>();

    internal GenericXmlNodeReader(MessageReadOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        this.options = options;
    }

    internal DynamicXmlNode ReadDocumentFromText(string text, string? sourceName)
    {
        new ResourceLimitGuard(options.Limits).EnsureInputByteCount(Encoding.UTF8.GetByteCount(text));
        XmlInputRepairResult preparation = XmlInputRepairProcessor.PrepareText(text, options);
        using (BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromText(preparation.Text, options))
        {
            DynamicXmlNode document = ReadDocumentNodes(reader, sourceName);
            ApplyPreparedRepairs(preparation);
            return document;
        }
    }

    internal DynamicXmlNode ReadDocumentFromBytes(byte[] bytes, string? sourceName)
    {
        new ResourceLimitGuard(options.Limits).EnsureInputByteCount(bytes.LongLength);
        XmlInputRepairResult preparation = XmlInputRepairProcessor.PrepareBytes(bytes, options);
        using (BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromBytes(preparation.Bytes, options))
        {
            DynamicXmlNode document = ReadDocumentNodes(reader, sourceName);
            ApplyPreparedRepairs(preparation);
            return document;
        }
    }

    internal void ApplyPreparedRepairs(XmlInputRepairResult preparation)
    {
        MessageRepairFactory.AddCopies(Repairs, preparation.Repairs, options.Limits);
    }

    internal DynamicXmlNode ReadDocumentNodes(BoundedXmlReader reader, string? sourceName)
    {
        DynamicXmlNode document = new DynamicXmlNode();
        document.Kind = XmlNodeKind.Document;
        NamespaceScopeTracker namespaces = new NamespaceScopeTracker();
        XmlPathTracker paths = new XmlPathTracker();
        while (reader.ReadNext())
        {
            XmlNodeType nodeType = reader.GetNodeType();
            if (nodeType == XmlNodeType.Element)
            {
                document.Children.Add(ReadElementNode(reader, sourceName, namespaces, paths));
            }
            else if (nodeType != XmlNodeType.XmlDeclaration)
            {
                DynamicXmlNode? node = CreateValueNode(reader, sourceName);
                if (node != null)
                {
                    document.Children.Add(node);
                }
            }
        }

        if (paths.GetPathDepth() != 0 || namespaces.GetScopeDepth() != 0)
        {
            throw new XmlException("The XML reader ended with an incomplete element scope.");
        }

        return document;
    }

    internal DynamicXmlNode ReadElementNode(BoundedXmlReader reader, string? sourceName, NamespaceScopeTracker namespaces, XmlPathTracker paths)
    {
        DynamicXmlNode element = new DynamicXmlNode();
        element.Kind = XmlNodeKind.Element;
        element.Name = CreateCurrentName(reader);
        element.Location = reader.CaptureLocation(sourceName);
        element.IsEmptyElement = reader.GetIsEmptyElement();
        namespaces.EnterScope();
        ReadElementAttributes(reader, element, namespaces, sourceName);
        paths.EnterElement(element.Name, element.Location);

        if (!element.IsEmptyElement)
        {
            bool foundEndElement = false;
            while (!foundEndElement && reader.ReadNext())
            {
                XmlNodeType nodeType = reader.GetNodeType();
                if (nodeType == XmlNodeType.EndElement)
                {
                    foundEndElement = true;
                }
                else if (nodeType == XmlNodeType.Element)
                {
                    element.Children.Add(ReadElementNode(reader, sourceName, namespaces, paths));
                }
                else
                {
                    DynamicXmlNode? child = CreateValueNode(reader, sourceName);
                    if (child != null)
                    {
                        element.Children.Add(child);
                    }
                }
            }

            if (!foundEndElement)
            {
                throw new XmlException("An XML element ended without a matching end tag.");
            }
        }

        paths.ExitElement();
        namespaces.ExitScope();
        return element;
    }

    internal void ReadElementAttributes(BoundedXmlReader reader, DynamicXmlNode element, NamespaceScopeTracker namespaces, string? sourceName)
    {
        if (!reader.MoveToFirstAttribute())
        {
            ValidateElementBinding(element.Name, namespaces);
            return;
        }

        bool hasAttribute = true;
        while (hasAttribute)
        {
            string namespaceUri = reader.GetNamespaceUri();
            string localName = reader.GetLocalName();
            string prefix = reader.GetPrefix();
            string value = reader.GetValue();
            if (namespaceUri == Iso20022Namespaces.Xmlns)
            {
                NamespaceDeclarationData declaration = new NamespaceDeclarationData();
                declaration.Prefix = prefix == "xmlns" ? localName : string.Empty;
                declaration.NamespaceUri = value;
                namespaces.DeclareNamespace(declaration.Prefix, declaration.NamespaceUri);
                element.NamespaceDeclarations.Add(declaration);
            }
            else
            {
                XmlAttributeData attribute = new XmlAttributeData();
                attribute.Name.NamespaceUri = namespaceUri;
                attribute.Name.LocalName = localName;
                attribute.Name.Prefix = prefix;
                attribute.Value = value;
                attribute.IsSpecified = true;
                attribute.Location = reader.CaptureLocation(sourceName);
                element.Attributes.Add(attribute);
                if (namespaceUri == Iso20022Namespaces.XmlSchemaInstance && localName == "nil")
                {
                    element.IsNil = ParseNilValue(value);
                }
            }

            hasAttribute = reader.MoveToNextAttribute();
        }

        reader.MoveBackToElement();
        ValidateElementBinding(element.Name, namespaces);
    }

    internal DynamicXmlNode? CreateValueNode(BoundedXmlReader reader, string? sourceName)
    {
        XmlNodeType nodeType = reader.GetNodeType();
        DynamicXmlNode? node = null;
        if (nodeType == XmlNodeType.Text || nodeType == XmlNodeType.Whitespace || nodeType == XmlNodeType.SignificantWhitespace)
        {
            node = new DynamicXmlNode();
            node.Kind = XmlNodeKind.Text;
        }
        else if (nodeType == XmlNodeType.CDATA)
        {
            node = new DynamicXmlNode();
            node.Kind = XmlNodeKind.CData;
        }
        else if (nodeType == XmlNodeType.Comment)
        {
            node = new DynamicXmlNode();
            node.Kind = XmlNodeKind.Comment;
        }
        else if (nodeType == XmlNodeType.ProcessingInstruction)
        {
            node = new DynamicXmlNode();
            node.Kind = XmlNodeKind.ProcessingInstruction;
            node.Name = CreateCurrentName(reader);
        }
        else if (options.UnknownContentMode == UnknownContentMode.Ignore)
        {
            return null;
        }
        else
        {
            throw new XmlException("The XML input contains a node kind that cannot be represented by the generic model.");
        }

        node.Value = reader.GetValue();
        node.Location = reader.CaptureLocation(sourceName);
        return node;
    }

    internal static QualifiedNameData CreateCurrentName(BoundedXmlReader reader)
    {
        QualifiedNameData name = new QualifiedNameData();
        name.NamespaceUri = reader.GetNamespaceUri();
        name.LocalName = reader.GetLocalName();
        name.Prefix = reader.GetPrefix();
        return name;
    }

    internal static bool ParseNilValue(string value)
    {
        if (value == "true" || value == "1")
        {
            return true;
        }

        if (value == "false" || value == "0")
        {
            return false;
        }

        throw new XmlException("The xsi:nil attribute does not contain a valid XML Schema boolean.");
    }

    internal static void ValidateElementBinding(QualifiedNameData name, NamespaceScopeTracker namespaces)
    {
        string? namespaceUri = namespaces.ResolvePrefix(name.Prefix);
        if (namespaceUri == null || namespaceUri != name.NamespaceUri)
        {
            throw new XmlException("An XML element name does not match its active namespace binding.");
        }
    }
}
