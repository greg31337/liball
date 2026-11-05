using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace iso20022;

internal static class RawXmlFragmentMaterializer
{
    internal static DynamicXmlNode MaterializeDocument(RawXmlFragment fragment, MessageWriteOptions writeOptions)
    {
        string fragmentXml = fragment.Xml;
        if (fragmentXml.Length == 0 && fragment.Utf8Bytes.Length > 0)
        {
            fragmentXml = Encoding.UTF8.GetString(fragment.Utf8Bytes);
        }

        if (fragmentXml.Length == 0)
        {
            throw new XmlException("A preserved raw XML fragment does not contain XML text or UTF-8 bytes.");
        }

        string wrapperXml = CreateWrapperXml(fragmentXml, fragment.NamespaceDeclarations);
        MessageReadOptions readOptions = new MessageReadOptions();
        readOptions.UnknownContentMode = UnknownContentMode.Preserve;
        readOptions.Limits = CreateMaterializerLimits(writeOptions.Limits, fragmentXml, wrapperXml, fragment.NamespaceDeclarations.Count);
        readOptions.PreserveComments = true;
        readOptions.PreserveProcessingInstructions = true;
        readOptions.PreserveWhitespace = true;
        readOptions.PreserveLexicalValues = true;
        DynamicXmlNode wrapperDocument = new GenericXmlNodeReader(readOptions).ReadDocumentFromText(wrapperXml, null);
        List<DynamicXmlNode> roots = MessageDetector.GetElementChildren(wrapperDocument);
        if (roots.Count != 1)
        {
            throw new XmlException("The raw XML fragment wrapper did not produce one internal root.");
        }

        DynamicXmlNode wrapper = roots[0];
        DynamicXmlNode document = new DynamicXmlNode();
        document.Kind = XmlNodeKind.Document;
        int index = 0;
        while (index < wrapper.Children.Count)
        {
            DynamicXmlNode child = wrapper.Children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                AddClosureDeclarations(child, fragment.NamespaceDeclarations);
            }

            document.Children.Add(child);
            index++;
        }

        ValidateRootName(fragment, document);
        return document;
    }

    internal static ResourceLimits CreateMaterializerLimits(ResourceLimits source, string fragmentXml, string wrapperXml, int declarationCount)
    {
        ResourceLimits limits = new ResourceLimits();
        limits.MaxDepth = AddAllowance(source.MaxDepth, 1);
        limits.MaxAttributesPerElement = source.MaxAttributesPerElement;
        int wrapperAttributeCount = declarationCount + 1;
        if (wrapperAttributeCount > limits.MaxAttributesPerElement)
        {
            limits.MaxAttributesPerElement = wrapperAttributeCount;
        }

        limits.MaxTextLength = source.MaxTextLength;
        limits.MaxElementCount = AddAllowance(source.MaxElementCount, 1);
        limits.MaxCollectionCount = source.MaxCollectionCount;
        int fragmentByteCount = Encoding.UTF8.GetByteCount(fragmentXml);
        int wrapperByteCount = Encoding.UTF8.GetByteCount(wrapperXml);
        limits.MaxTotalBytes = AddAllowance(source.MaxTotalBytes, wrapperByteCount - fragmentByteCount);
        limits.MaxSchemaCount = source.MaxSchemaCount;
        limits.MaxDiagnosticCount = source.MaxDiagnosticCount;
        return limits;
    }

    internal static int AddAllowance(int value, int allowance)
    {
        if (allowance <= 0)
        {
            return value;
        }

        if (value > int.MaxValue - allowance)
        {
            return int.MaxValue;
        }

        return value + allowance;
    }

    internal static string CreateWrapperXml(string fragmentXml, List<NamespaceDeclarationData> declarations)
    {
        string wrapperPrefix = FindWrapperPrefix(declarations);
        StringBuilder text = new StringBuilder();
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        settings.ConformanceLevel = ConformanceLevel.Document;
        settings.CheckCharacters = true;
        using (StringWriter output = new StringWriter(text, CultureInfo.InvariantCulture))
        {
            using (XmlWriter writer = XmlWriter.Create(output, settings))
            {
                writer.WriteStartElement(wrapperPrefix, "Fragment", "urn:iso20022:internal:raw-fragment");
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

                writer.WriteRaw(fragmentXml);
                writer.WriteEndElement();
            }
        }

        return text.ToString();
    }

    internal static string FindWrapperPrefix(List<NamespaceDeclarationData> declarations)
    {
        string prefix = "iso20022Raw";
        int suffix = 0;
        bool found = HasPrefix(declarations, prefix);
        while (found)
        {
            suffix++;
            prefix = "iso20022Raw" + suffix.ToString(CultureInfo.InvariantCulture);
            found = HasPrefix(declarations, prefix);
        }

        return prefix;
    }

    internal static bool HasPrefix(List<NamespaceDeclarationData> declarations, string prefix)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            if (declarations[index].Prefix == prefix)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static void AddClosureDeclarations(DynamicXmlNode root, List<NamespaceDeclarationData> declarations)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            NamespaceDeclarationData declaration = declarations[index];
            if (!HasPrefix(root.NamespaceDeclarations, declaration.Prefix))
            {
                root.NamespaceDeclarations.Add(NamespaceScopeTracker.CopyDeclaration(declaration));
            }

            index++;
        }
    }

    internal static void ValidateRootName(RawXmlFragment fragment, DynamicXmlNode document)
    {
        if (fragment.RootName == null)
        {
            return;
        }

        List<DynamicXmlNode> roots = MessageDetector.GetElementChildren(document);
        if (roots.Count != 1 || roots[0].Name.NamespaceUri != fragment.RootName.NamespaceUri || roots[0].Name.LocalName != fragment.RootName.LocalName)
        {
            throw new XmlException("The preserved raw XML fragment does not match its declared root name.");
        }
    }
}
