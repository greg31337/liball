using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace odf;

internal class OdfTreeWritePlan
{
    internal OdfNamespaceScope Scope;
    internal OdfQualifiedName ElementName;
    internal OdfNamespaceDeclaration[] NamespaceDeclarations;
    internal OdfAttribute[] Attributes;

    internal OdfTreeWritePlan(
        OdfNamespaceScope scope,
        OdfQualifiedName elementName,
        OdfNamespaceDeclaration[] namespaceDeclarations,
        OdfAttribute[] attributes)
    {
        Scope = scope;
        ElementName = elementName;
        NamespaceDeclarations = namespaceDeclarations;
        Attributes = attributes;
    }
}

internal static class OdfTreeWriter
{
    const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    internal static string WriteText(
        OdfNode[] nodes,
        OdfSaveOptions options)
    {
        return Encoding.UTF8.GetString(WriteBytes(nodes, options));
    }

    internal static byte[] WriteBytes(
        OdfNode[] nodes,
        OdfSaveOptions options)
    {
        ValidateInputs(nodes, options);
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.CheckCharacters = true;
        settings.CloseOutput = false;
        settings.ConformanceLevel = ConformanceLevel.Document;
        settings.Encoding = new UTF8Encoding(false);
        settings.Indent = options.IndentXml;
        settings.IndentChars = "  ";
        settings.NewLineChars = "\n";
        settings.NewLineHandling = NewLineHandling.None;
        settings.NewLineOnAttributes = false;
        settings.OmitXmlDeclaration = false;

        using (MemoryStream output = new MemoryStream())
        {
            using (XmlWriter writer = XmlWriter.Create(output, settings))
            {
                writer.WriteStartDocument();
                foreach (OdfNode node in nodes)
                {
                    WriteNode(writer, node, options, null);
                }
                writer.WriteEndDocument();
                writer.Flush();
            }
            return output.ToArray();
        }
    }

    static void WriteNode(
        XmlWriter writer,
        OdfNode node,
        OdfSaveOptions options,
        OdfNamespaceScope? parentScope)
    {
        CheckCancellation(options);
        if (node == null)
        {
            throw new OdfXmlException("The XML tree contains a null node.");
        }
        if (node.Kind == OdfNodeKind.Element)
        {
            if (node.Name == null)
            {
                throw new OdfXmlException("An XML element requires a qualified name.");
            }
            if (node.Value.Length > 0)
            {
                throw new OdfXmlException("Element text must be represented by ordered child nodes.");
            }
            OdfTreeWritePlan plan = PrepareElement(node, options, parentScope);
            writer.WriteStartElement(
                plan.ElementName.Prefix,
                plan.ElementName.LocalName,
                plan.ElementName.NamespaceName);
            foreach (OdfNamespaceDeclaration declaration in plan.NamespaceDeclarations)
            {
                WriteDeclaration(writer, declaration);
            }
            foreach (OdfAttribute attribute in plan.Attributes)
            {
                WriteAttribute(writer, attribute);
            }
            foreach (OdfNode child in node.Children)
            {
                WriteNode(writer, child, options, plan.Scope);
            }
            if (options.Deterministic
                || options.PreserveLexicalValues && !node.WasEmptyElement)
            {
                writer.WriteFullEndElement();
            }
            else
            {
                writer.WriteEndElement();
            }
            return;
        }
        if (node.Children.Length > 0
            || node.NamespaceDeclarations.Length > 0
            || node.Attributes.Length > 0)
        {
            throw new OdfXmlException("Only XML elements may own attributes, declarations, or child nodes.");
        }
        if (node.Kind == OdfNodeKind.Text)
        {
            if (IsWhitespaceValue(node.Value))
            {
                writer.WriteWhitespace(node.Value);
            }
            else
            {
                writer.WriteString(node.Value);
            }
            return;
        }
        if (node.Kind == OdfNodeKind.Comment)
        {
            writer.WriteComment(node.Value);
            return;
        }
        if (node.Kind == OdfNodeKind.ProcessingInstruction)
        {
            if (node.Name == null
                || node.Name.NamespaceName.Length > 0
                || node.Name.Prefix.Length > 0
                || !OdfNameRules.IsName(node.Name.LocalName))
            {
                throw new OdfXmlException("An XML processing instruction requires a valid target name.");
            }
            writer.WriteProcessingInstruction(node.Name.LocalName, node.Value);
            return;
        }
        throw new OdfXmlException("The XML tree contains an unknown node kind.");
    }

    static OdfTreeWritePlan PrepareElement(
        OdfNode node,
        OdfSaveOptions options,
        OdfNamespaceScope? parentScope)
    {
        bool preservePrefixes = options.PreserveLexicalValues
            && !options.Deterministic;
        OdfNamespaceScope scope = new OdfNamespaceScope(
            parentScope,
            preservePrefixes);
        List<OdfNamespaceDeclaration> declarations = new List<OdfNamespaceDeclaration>();
        foreach (OdfNamespaceDeclaration source in node.NamespaceDeclarations)
        {
            OdfNamespaceDeclaration declaration = new OdfNamespaceDeclaration
            {
                Prefix = source.Prefix,
                NamespaceName = source.NamespaceName
            };
            scope.AddDeclaration(declaration);
            declarations.Add(declaration);
        }

        OdfQualifiedName elementName = SelectName(scope, node.Name, true);
        EnsureBinding(scope, elementName, declarations);
        List<OdfAttribute> attributes = new List<OdfAttribute>();
        foreach (OdfAttribute source in node.Attributes)
        {
            OdfQualifiedName attributeName = SelectName(scope, source.Name, false);
            EnsureBinding(scope, attributeName, declarations);
            attributes.Add(new OdfAttribute
            {
                Name = attributeName,
                Value = source.Value
            });
        }

        OdfNamespaceDeclaration[] declarationArray = declarations.ToArray();
        OdfAttribute[] attributeArray = attributes.ToArray();
        if (options.Deterministic)
        {
            declarationArray = SortDeclarations(declarationArray);
            attributeArray = SortAttributes(attributeArray);
        }
        return new OdfTreeWritePlan(
            scope,
            elementName,
            declarationArray,
            attributeArray);
    }

    static OdfQualifiedName SelectName(
        OdfNamespaceScope scope,
        OdfQualifiedName? source,
        bool allowDefault)
    {
        if (source == null || !OdfNameRules.IsQualifiedNameValue(source))
        {
            throw new OdfXmlException("The XML tree contains an invalid qualified name.");
        }
        return new OdfQualifiedName
        {
            NamespaceName = source.NamespaceName,
            LocalName = source.LocalName,
            Prefix = scope.SelectPrefix(
                source.NamespaceName,
                source.Prefix,
                allowDefault)
        };
    }

    static void EnsureBinding(
        OdfNamespaceScope scope,
        OdfQualifiedName name,
        List<OdfNamespaceDeclaration> declarations)
    {
        if (name.NamespaceName == OdfNamespaces.Xml)
        {
            return;
        }
        string? currentNamespace = scope.LookupNamespace(name.Prefix);
        if (currentNamespace == name.NamespaceName
            || currentNamespace == null && name.NamespaceName.Length == 0)
        {
            return;
        }
        OdfNamespaceDeclaration declaration = new OdfNamespaceDeclaration
        {
            Prefix = name.Prefix,
            NamespaceName = name.NamespaceName
        };
        scope.AddDeclaration(declaration);
        declarations.Add(declaration);
    }

    static void WriteDeclaration(
        XmlWriter writer,
        OdfNamespaceDeclaration declaration)
    {
        if (declaration.Prefix.Length == 0)
        {
            writer.WriteAttributeString(
                "",
                "xmlns",
                XmlnsNamespace,
                declaration.NamespaceName);
        }
        else
        {
            writer.WriteAttributeString(
                "xmlns",
                declaration.Prefix,
                XmlnsNamespace,
                declaration.NamespaceName);
        }
    }

    static void WriteAttribute(
        XmlWriter writer,
        OdfAttribute attribute)
    {
        writer.WriteAttributeString(
            attribute.Name.Prefix,
            attribute.Name.LocalName,
            attribute.Name.NamespaceName,
            attribute.Value);
    }

    static OdfNamespaceDeclaration[] SortDeclarations(
        OdfNamespaceDeclaration[] source)
    {
        OdfNamespaceDeclaration[] result = new OdfNamespaceDeclaration[source.Length];
        Array.Copy(source, result, source.Length);
        int index = 1;
        while (index < result.Length)
        {
            OdfNamespaceDeclaration current = result[index];
            int destination = index;
            while (destination > 0
                && CompareDeclarations(current, result[destination - 1]) < 0)
            {
                result[destination] = result[destination - 1];
                destination--;
            }
            result[destination] = current;
            index++;
        }
        return result;
    }

    static int CompareDeclarations(
        OdfNamespaceDeclaration first,
        OdfNamespaceDeclaration second)
    {
        int comparison = string.CompareOrdinal(first.Prefix, second.Prefix);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(first.NamespaceName, second.NamespaceName);
    }

    static OdfAttribute[] SortAttributes(OdfAttribute[] source)
    {
        OdfAttribute[] result = new OdfAttribute[source.Length];
        Array.Copy(source, result, source.Length);
        int index = 1;
        while (index < result.Length)
        {
            OdfAttribute current = result[index];
            int destination = index;
            while (destination > 0
                && CompareAttributes(current, result[destination - 1]) < 0)
            {
                result[destination] = result[destination - 1];
                destination--;
            }
            result[destination] = current;
            index++;
        }
        return result;
    }

    static int CompareAttributes(
        OdfAttribute first,
        OdfAttribute second)
    {
        int comparison = string.CompareOrdinal(
            first.Name.NamespaceName,
            second.Name.NamespaceName);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(
            first.Name.LocalName,
            second.Name.LocalName);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(first.Name.Prefix, second.Name.Prefix);
    }

    static bool IsWhitespaceValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != ' '
                && character != '\t'
                && character != '\r'
                && character != '\n')
            {
                return false;
            }
            index++;
        }
        return value.Length > 0;
    }

    static void CheckCancellation(OdfSaveOptions options)
    {
        if (options.Cancellation.IsCancellationRequested)
        {
            throw new OdfXmlException("XML tree writing was cancelled.");
        }
    }

    static void ValidateInputs(
        OdfNode[] nodes,
        OdfSaveOptions options)
    {
        if (nodes == null || options == null)
        {
            throw new OdfXmlException("XML nodes and save options are required.");
        }
        if (options.Cancellation == null)
        {
            throw new OdfXmlException("An XML writing cancellation state is required.");
        }
        CheckCancellation(options);
    }
}
