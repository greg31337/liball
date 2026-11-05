using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace iso20022;

internal class GenericXmlNodeWriter
{
    MessageWriteOptions options;

    internal GenericXmlNodeWriter(MessageWriteOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        this.options = options;
    }

    internal string WriteText(DynamicXmlNode? node)
    {
        byte[] bytes = WriteOutput(node, false);
        return Encoding.UTF8.GetString(bytes);
    }

    internal byte[] WriteBytes(DynamicXmlNode? node)
    {
        return WriteOutput(node, options.WriteByteOrderMark);
    }

    internal byte[] WriteOutput(DynamicXmlNode? node, bool writeByteOrderMark)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        XmlWriterSettings settings = CreateSettings(writeByteOrderMark);
        using (MemoryStream output = new MemoryStream())
        {
            using (XmlWriter writer = XmlWriter.Create(output, settings))
            {
                NamespaceScopeTracker namespaces = new NamespaceScopeTracker();
                if (options.WriteXmlDeclaration)
                {
                    writer.WriteStartDocument();
                }

                WriteNode(writer, node, namespaces);
                if (options.WriteXmlDeclaration)
                {
                    writer.WriteEndDocument();
                }
            }

            return output.ToArray();
        }
    }

    internal XmlWriterSettings CreateSettings(bool writeByteOrderMark)
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Encoding = new UTF8Encoding(writeByteOrderMark);
        settings.OmitXmlDeclaration = !options.WriteXmlDeclaration;
        settings.CheckCharacters = true;
        settings.CloseOutput = false;
        settings.ConformanceLevel = ConformanceLevel.Auto;
        settings.Indent = options.FormattingMode == XmlFormattingMode.Indented;
        settings.IndentChars = options.IndentText;
        settings.NewLineChars = options.NewLineText;
        settings.NewLineHandling = NewLineHandling.None;
        settings.NamespaceHandling = NamespaceHandling.Default;
        return settings;
    }

    internal void WriteNode(XmlWriter writer, DynamicXmlNode node, NamespaceScopeTracker namespaces)
    {
        if (node.Kind == XmlNodeKind.Document)
        {
            int childIndex = 0;
            while (childIndex < node.Children.Count)
            {
                WriteNode(writer, node.Children[childIndex], namespaces);
                childIndex++;
            }

            return;
        }

        if (node.Kind == XmlNodeKind.Element)
        {
            WriteElementNode(writer, node, namespaces);
            return;
        }

        if (node.Kind == XmlNodeKind.Text)
        {
            writer.WriteString(node.Value);
            return;
        }

        if (node.Kind == XmlNodeKind.CData)
        {
            writer.WriteCData(node.Value);
            return;
        }

        if (node.Kind == XmlNodeKind.Comment)
        {
            writer.WriteComment(node.Value);
            return;
        }

        if (node.Kind == XmlNodeKind.ProcessingInstruction)
        {
            if (!XmlNameLexical.IsXmlName(node.Name.LocalName.AsSpan()))
            {
                throw new XmlException("A processing instruction must have a valid XML name.");
            }

            writer.WriteProcessingInstruction(node.Name.LocalName, node.Value);
            return;
        }

        if (options.UnknownContentMode != UnknownContentMode.Ignore)
        {
            throw new XmlException("The generic model contains an unsupported node kind.");
        }
    }

    internal void WriteElementNode(XmlWriter writer, DynamicXmlNode element, NamespaceScopeTracker namespaces)
    {
        XmlPathTracker.ValidateName(element.Name);
        namespaces.EnterScope();
        List<NamespaceDeclarationData> declarations = CopyDeclarations(element.NamespaceDeclarations);
        int declarationIndex = 0;
        while (declarationIndex < declarations.Count)
        {
            NamespaceDeclarationData declaration = declarations[declarationIndex];
            namespaces.DeclareNamespace(declaration.Prefix, declaration.NamespaceUri);
            declarationIndex++;
        }

        EnsureNameBinding(element.Name, false, namespaces, declarations);
        List<XmlAttributeData> attributes = CopyAttributes(element.Attributes);
        bool hasNilAttribute = false;
        bool nilAttributeValue = false;
        int attributeIndex = 0;
        while (attributeIndex < attributes.Count)
        {
            XmlAttributeData attribute = attributes[attributeIndex];
            EnsureNameBinding(attribute.Name, true, namespaces, declarations);
            if (attribute.Name.NamespaceUri == Iso20022Namespaces.XmlSchemaInstance && attribute.Name.LocalName == "nil")
            {
                hasNilAttribute = true;
                nilAttributeValue = GenericXmlNodeReader.ParseNilValue(attribute.Value);
            }

            attributeIndex++;
        }

        if (hasNilAttribute && nilAttributeValue != element.IsNil)
        {
            throw new XmlException("The xsi:nil attribute conflicts with the generic element state.");
        }

        if (element.IsNil && !hasNilAttribute)
        {
            XmlAttributeData nilAttribute = new XmlAttributeData();
            nilAttribute.Name.Prefix = "xsi";
            nilAttribute.Name.LocalName = "nil";
            nilAttribute.Name.NamespaceUri = Iso20022Namespaces.XmlSchemaInstance;
            nilAttribute.Value = "true";
            nilAttribute.IsSpecified = true;
            EnsureNameBinding(nilAttribute.Name, true, namespaces, declarations);
            attributes.Add(nilAttribute);
        }

        writer.WriteStartElement(element.Name.Prefix, element.Name.LocalName, element.Name.NamespaceUri);
        WriteNamespaceDeclarations(writer, declarations);
        attributeIndex = 0;
        while (attributeIndex < attributes.Count)
        {
            XmlAttributeData attribute = attributes[attributeIndex];
            writer.WriteAttributeString(attribute.Name.Prefix, attribute.Name.LocalName, attribute.Name.NamespaceUri, attribute.Value);
            attributeIndex++;
        }

        int childIndex = 0;
        while (childIndex < element.Children.Count)
        {
            WriteNode(writer, element.Children[childIndex], namespaces);
            childIndex++;
        }

        if (element.IsEmptyElement && element.Children.Count == 0)
        {
            writer.WriteEndElement();
        }
        else
        {
            writer.WriteFullEndElement();
        }

        namespaces.ExitScope();
    }

    internal static void EnsureNameBinding(QualifiedNameData name, bool isAttribute, NamespaceScopeTracker namespaces, List<NamespaceDeclarationData> declarations)
    {
        XmlPathTracker.ValidateName(name);
        if (name.Prefix == "xmlns" || name.NamespaceUri == Iso20022Namespaces.Xmlns)
        {
            throw new XmlException("Namespace declarations must be represented separately from ordinary names.");
        }

        if (isAttribute && name.Prefix.Length == 0)
        {
            if (name.NamespaceUri.Length > 0)
            {
                throw new XmlException("A namespaced attribute must have an explicit prefix.");
            }

            return;
        }

        string? activeNamespace = namespaces.ResolvePrefix(name.Prefix);
        if (activeNamespace == name.NamespaceUri)
        {
            return;
        }

        NamespaceDeclarationData declaration = new NamespaceDeclarationData();
        declaration.Prefix = name.Prefix;
        declaration.NamespaceUri = name.NamespaceUri;
        namespaces.DeclareNamespace(declaration.Prefix, declaration.NamespaceUri);
        declarations.Add(declaration);
    }

    internal static void WriteNamespaceDeclarations(XmlWriter writer, List<NamespaceDeclarationData> declarations)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            NamespaceDeclarationData declaration = declarations[index];
            if (declaration.Prefix.Length == 0)
            {
                writer.WriteAttributeString(string.Empty, "xmlns", Iso20022Namespaces.Xmlns, declaration.NamespaceUri);
            }
            else
            {
                writer.WriteAttributeString("xmlns", declaration.Prefix, Iso20022Namespaces.Xmlns, declaration.NamespaceUri);
            }

            index++;
        }
    }

    internal static List<NamespaceDeclarationData> CopyDeclarations(List<NamespaceDeclarationData> source)
    {
        List<NamespaceDeclarationData> copies = new List<NamespaceDeclarationData>();
        int index = 0;
        while (index < source.Count)
        {
            copies.Add(NamespaceScopeTracker.CopyDeclaration(source[index]));
            index++;
        }

        return copies;
    }

    internal static List<XmlAttributeData> CopyAttributes(List<XmlAttributeData> source)
    {
        List<XmlAttributeData> copies = new List<XmlAttributeData>();
        int index = 0;
        while (index < source.Count)
        {
            XmlAttributeData sourceAttribute = source[index];
            XmlAttributeData copy = new XmlAttributeData();
            copy.Name = XmlPathTracker.CopyName(sourceAttribute.Name);
            copy.Value = sourceAttribute.Value;
            copy.IsSpecified = sourceAttribute.IsSpecified;
            copy.Location = XmlPathTracker.CopyLocation(sourceAttribute.Location);
            copies.Add(copy);
            index++;
        }

        return copies;
    }
}
