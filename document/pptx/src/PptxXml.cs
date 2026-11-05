using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace pptx;

internal static class PptxXml
{
    internal static XmlReaderSettings CreateReaderSettings()
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.MaxCharactersFromEntities = 0;
        return settings;
    }

    internal static XmlWriterSettings CreateWriterSettings()
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Encoding = new UTF8Encoding(false);
        settings.OmitXmlDeclaration = false;
        settings.Indent = false;
        settings.CloseOutput = false;
        settings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
        return settings;
    }

    internal static XmlDocument CreateDocument()
    {
        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.XmlResolver = null;
        return document;
    }

    internal static XmlDocument LoadString(string xml)
    {
        if (xml == null)
        {
            throw new System.ArgumentNullException("xml");
        }

        using (StringReader stringReader = new StringReader(xml))
        {
            XmlDocument document = CreateDocument();
            using (XmlReader reader = XmlReader.Create(stringReader, CreateReaderSettings()))
            {
                document.Load(reader);
            }

            return document;
        }
    }

    internal static XmlDocument LoadBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        using (MemoryStream stream = new MemoryStream(bytes))
        {
            return LoadStream(stream);
        }
    }

    internal static XmlDocument LoadStream(Stream stream)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        XmlDocument document = CreateDocument();
        using (XmlReader reader = XmlReader.Create(stream, CreateReaderSettings()))
        {
            document.Load(reader);
        }

        return document;
    }

    internal static XmlDocument LoadUtf8String(string xml)
    {
        return LoadBytes(Encoding.UTF8.GetBytes(xml));
    }

    internal static byte[] WriteBytes(XmlDocument document)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        using (MemoryStream stream = new MemoryStream())
        {
            using (XmlWriter writer = XmlWriter.Create(stream, CreateWriterSettings()))
            {
                WriteDocument(writer, document);
            }

            return stream.ToArray();
        }
    }

    internal static string WriteString(XmlDocument document)
    {
        return Encoding.UTF8.GetString(WriteBytes(document));
    }

    internal static void WriteDocument(XmlWriter writer, XmlDocument document)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        writer.WriteStartDocument();

        for (int index = 0; index < document.ChildNodes.Count; index++)
        {
            XmlNode? child = document.ChildNodes[index];
            if (child == null)
            {
                continue;
            }

            if (child.NodeType == XmlNodeType.XmlDeclaration)
            {
                continue;
            }

            WriteNode(writer, child);
        }

        writer.WriteEndDocument();
    }

    internal static void WriteNode(XmlWriter writer, XmlNode node)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (node == null)
        {
            throw new System.ArgumentNullException("node");
        }

        if (node.NodeType == XmlNodeType.Element)
        {
            WriteElement(writer, (XmlElement)node);
            return;
        }

        if (node.NodeType == XmlNodeType.Text)
        {
            writer.WriteString(GetNodeValue(node));
            return;
        }

        if (node.NodeType == XmlNodeType.CDATA)
        {
            writer.WriteCData(GetNodeValue(node));
            return;
        }

        if (node.NodeType == XmlNodeType.Comment)
        {
            writer.WriteComment(GetNodeValue(node));
            return;
        }

        if (node.NodeType == XmlNodeType.Whitespace || node.NodeType == XmlNodeType.SignificantWhitespace)
        {
            writer.WriteWhitespace(GetNodeValue(node));
            return;
        }

        if (node.NodeType == XmlNodeType.ProcessingInstruction)
        {
            XmlProcessingInstruction instruction = (XmlProcessingInstruction)node;
            writer.WriteProcessingInstruction(instruction.Target, instruction.Data);
            return;
        }

        if (node.NodeType == XmlNodeType.DocumentType)
        {
            XmlDocumentType documentType = (XmlDocumentType)node;
            writer.WriteDocType(documentType.Name, documentType.PublicId, documentType.SystemId, documentType.InternalSubset);
            return;
        }

        if (node.NodeType == XmlNodeType.EntityReference)
        {
            writer.WriteEntityRef(node.Name);
        }
    }

    internal static void WriteElement(XmlWriter writer, XmlElement element)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        string prefix = GetPreferredPrefix(element.NamespaceURI, element.Prefix);
        writer.WriteStartElement(prefix, element.LocalName, element.NamespaceURI);

        WriteNamespaceDeclarations(writer, element);
        WriteAttributes(writer, element);

        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlNode? child = element.ChildNodes[index];
            if (child == null)
            {
                continue;
            }

            WriteNode(writer, child);
        }

        writer.WriteEndElement();
    }

    internal static void WriteNamespaceDeclarations(XmlWriter writer, XmlElement element)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        for (int index = 0; index < element.Attributes.Count; index++)
        {
            XmlAttribute attribute = element.Attributes[index];
            if (attribute == null)
            {
                continue;
            }

            if (!IsNamespaceDeclaration(attribute))
            {
                continue;
            }

            if (IsKnownNamespace(attribute.Value))
            {
                continue;
            }

            WriteNamespaceDeclaration(writer, attribute);
        }
    }

    internal static void WriteNamespaceDeclaration(XmlWriter writer, XmlAttribute attribute)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (attribute == null)
        {
            throw new System.ArgumentNullException("attribute");
        }

        if (attribute.Prefix == "xmlns")
        {
            writer.WriteAttributeString("xmlns", attribute.LocalName, PptxNamespaces.Xmlns, attribute.Value);
            return;
        }

        writer.WriteAttributeString("", "xmlns", PptxNamespaces.Xmlns, attribute.Value);
    }

    internal static void WriteAttributes(XmlWriter writer, XmlElement element)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        for (int index = 0; index < element.Attributes.Count; index++)
        {
            XmlAttribute attribute = element.Attributes[index];
            if (attribute == null)
            {
                continue;
            }

            if (IsNamespaceDeclaration(attribute))
            {
                continue;
            }

            string prefix = GetPreferredPrefix(attribute.NamespaceURI, attribute.Prefix);
            writer.WriteAttributeString(prefix, attribute.LocalName, attribute.NamespaceURI, attribute.Value);
        }
    }

    internal static XmlNamespaceManager CreateNamespaceManager(XmlDocument document)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        XmlNamespaceManager manager = new XmlNamespaceManager(document.NameTable);
        manager.AddNamespace("ct", PptxNamespaces.ContentTypes);
        manager.AddNamespace("pkgrel", PptxNamespaces.PackageRelationships);
        manager.AddNamespace("mc", PptxNamespaces.MarkupCompatibility);
        manager.AddNamespace("p", PptxNamespaces.Presentation);
        manager.AddNamespace("a", PptxNamespaces.Drawing);
        manager.AddNamespace("c", PptxNamespaces.Chart);
        manager.AddNamespace("r", PptxNamespaces.Relationships);
        manager.AddNamespace("cp", PptxNamespaces.CoreProperties);
        manager.AddNamespace("dc", PptxNamespaces.DublinCore);
        manager.AddNamespace("dcterms", PptxNamespaces.DublinTerms);
        manager.AddNamespace("dcmitype", PptxNamespaces.DublinCoreType);
        manager.AddNamespace("xsi", PptxNamespaces.Xsi);
        manager.AddNamespace("ep", PptxNamespaces.ExtendedProperties);
        manager.AddNamespace("cust", PptxNamespaces.CustomProperties);
        manager.AddNamespace("vt", PptxNamespaces.VariantTypes);
        return manager;
    }

    internal static string GetPreferredPrefix(string namespaceUri, string existingPrefix)
    {
        if (namespaceUri == null)
        {
            throw new System.ArgumentNullException("namespaceUri");
        }

        if (existingPrefix == null)
        {
            throw new System.ArgumentNullException("existingPrefix");
        }

        if (namespaceUri == PptxNamespaces.ContentTypes)
        {
            return "ct";
        }

        if (namespaceUri == PptxNamespaces.PackageRelationships)
        {
            return "pkgrel";
        }

        if (namespaceUri == PptxNamespaces.MarkupCompatibility)
        {
            return "mc";
        }

        if (namespaceUri == PptxNamespaces.Presentation)
        {
            return "p";
        }

        if (namespaceUri == PptxNamespaces.Drawing)
        {
            return "a";
        }

        if (namespaceUri == PptxNamespaces.Chart)
        {
            return "c";
        }

        if (namespaceUri == PptxNamespaces.Relationships)
        {
            return "r";
        }

        if (namespaceUri == PptxNamespaces.CoreProperties)
        {
            return "cp";
        }

        if (namespaceUri == PptxNamespaces.DublinCore)
        {
            return "dc";
        }

        if (namespaceUri == PptxNamespaces.DublinTerms)
        {
            return "dcterms";
        }

        if (namespaceUri == PptxNamespaces.DublinCoreType)
        {
            return "dcmitype";
        }

        if (namespaceUri == PptxNamespaces.Xsi)
        {
            return "xsi";
        }

        if (namespaceUri == PptxNamespaces.ExtendedProperties)
        {
            return "ep";
        }

        if (namespaceUri == PptxNamespaces.CustomProperties)
        {
            return "cust";
        }

        if (namespaceUri == PptxNamespaces.VariantTypes)
        {
            return "vt";
        }

        if (namespaceUri == PptxNamespaces.Xml)
        {
            return "xml";
        }

        if (namespaceUri == PptxNamespaces.Xmlns)
        {
            return "xmlns";
        }

        return existingPrefix;
    }

    internal static bool IsKnownNamespace(string namespaceUri)
    {
        if (namespaceUri == null)
        {
            throw new System.ArgumentNullException("namespaceUri");
        }

        return GetPreferredPrefix(namespaceUri, "") != "";
    }

    internal static bool IsNamespaceDeclaration(XmlAttribute attribute)
    {
        if (attribute == null)
        {
            throw new System.ArgumentNullException("attribute");
        }

        if (attribute.Prefix == "xmlns")
        {
            return true;
        }

        if (attribute.Prefix.Length == 0 && attribute.LocalName == "xmlns")
        {
            return true;
        }

        return false;
    }

    internal static string GetNodeValue(XmlNode node)
    {
        if (node == null)
        {
            throw new System.ArgumentNullException("node");
        }

        if (node.Value == null)
        {
            return "";
        }

        return node.Value;
    }

    internal static XmlElement? FindFirstChild(XmlElement parent, string namespaceUri, string localName)
    {
        if (parent == null)
        {
            throw new System.ArgumentNullException("parent");
        }

        if (namespaceUri == null)
        {
            throw new System.ArgumentNullException("namespaceUri");
        }

        if (localName == null)
        {
            throw new System.ArgumentNullException("localName");
        }

        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlNode? child = parent.ChildNodes[index];
            if (child == null)
            {
                continue;
            }

            if (child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == namespaceUri && element.LocalName == localName)
            {
                return element;
            }
        }

        return null;
    }

    internal static XmlElement? FindFirstDescendant(XmlNode root, string namespaceUri, string localName)
    {
        if (root == null)
        {
            throw new System.ArgumentNullException("root");
        }

        if (namespaceUri == null)
        {
            throw new System.ArgumentNullException("namespaceUri");
        }

        if (localName == null)
        {
            throw new System.ArgumentNullException("localName");
        }

        if (root.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)root;
            if (element.NamespaceURI == namespaceUri && element.LocalName == localName)
            {
                return element;
            }
        }

        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null)
            {
                continue;
            }

            XmlElement? descendant = FindFirstDescendant(child, namespaceUri, localName);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }

    internal static XmlElement? FindFirstChildByAttribute(XmlElement parent, string namespaceUri, string localName, string attributeNamespaceUri, string attributeLocalName, string attributeValue)
    {
        if (parent == null)
        {
            throw new System.ArgumentNullException("parent");
        }

        if (namespaceUri == null)
        {
            throw new System.ArgumentNullException("namespaceUri");
        }

        if (localName == null)
        {
            throw new System.ArgumentNullException("localName");
        }

        if (attributeNamespaceUri == null)
        {
            throw new System.ArgumentNullException("attributeNamespaceUri");
        }

        if (attributeLocalName == null)
        {
            throw new System.ArgumentNullException("attributeLocalName");
        }

        if (attributeValue == null)
        {
            throw new System.ArgumentNullException("attributeValue");
        }

        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlNode? child = parent.ChildNodes[index];
            if (child == null)
            {
                continue;
            }

            if (child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI != namespaceUri || element.LocalName != localName)
            {
                continue;
            }

            if (GetAttributeValue(element, attributeNamespaceUri, attributeLocalName) == attributeValue)
            {
                return element;
            }
        }

        return null;
    }

    internal static XmlElement CreateElement(XmlDocument document, string prefix, string localName, string namespaceUri)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        if (prefix == null)
        {
            throw new System.ArgumentNullException("prefix");
        }

        if (localName == null)
        {
            throw new System.ArgumentNullException("localName");
        }

        if (namespaceUri == null)
        {
            throw new System.ArgumentNullException("namespaceUri");
        }

        return document.CreateElement(prefix, localName, namespaceUri);
    }

    internal static XmlElement AppendChildElement(XmlElement parent, string prefix, string localName, string namespaceUri)
    {
        if (parent == null)
        {
            throw new System.ArgumentNullException("parent");
        }

        XmlDocument document = parent.OwnerDocument;
        if (document == null)
        {
            throw new System.ArgumentException("Parent element must belong to a document.", "parent");
        }

        XmlElement child = CreateElement(document, prefix, localName, namespaceUri);
        parent.AppendChild(child);
        return child;
    }

    internal static XmlElement GetOrCreateChild(XmlElement parent, string prefix, string localName, string namespaceUri)
    {
        XmlElement? existing = FindFirstChild(parent, namespaceUri, localName);
        if (existing != null)
        {
            return existing;
        }

        return AppendChildElement(parent, prefix, localName, namespaceUri);
    }

    internal static string GetAttributeValue(XmlElement element, string namespaceUri, string localName)
    {
        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        if (namespaceUri == null)
        {
            throw new System.ArgumentNullException("namespaceUri");
        }

        if (localName == null)
        {
            throw new System.ArgumentNullException("localName");
        }

        XmlAttribute? attribute = null;
        if (namespaceUri.Length == 0)
        {
            attribute = element.GetAttributeNode(localName);
        }
        else
        {
            attribute = element.GetAttributeNode(localName, namespaceUri);
        }

        if (attribute == null)
        {
            return "";
        }

        return attribute.Value;
    }

    internal static bool HasAttributeValue(XmlElement element, string namespaceUri, string localName)
    {
        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        if (namespaceUri == null)
        {
            throw new System.ArgumentNullException("namespaceUri");
        }

        if (localName == null)
        {
            throw new System.ArgumentNullException("localName");
        }

        if (namespaceUri.Length == 0)
        {
            return element.GetAttributeNode(localName) != null;
        }

        return element.GetAttributeNode(localName, namespaceUri) != null;
    }

    internal static void SetAttributeValue(XmlElement element, string prefix, string namespaceUri, string localName, string value)
    {
        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        if (prefix == null)
        {
            throw new System.ArgumentNullException("prefix");
        }

        if (namespaceUri == null)
        {
            throw new System.ArgumentNullException("namespaceUri");
        }

        if (localName == null)
        {
            throw new System.ArgumentNullException("localName");
        }

        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        XmlAttribute? attribute = null;
        if (namespaceUri.Length == 0)
        {
            attribute = element.GetAttributeNode(localName);
        }
        else
        {
            attribute = element.GetAttributeNode(localName, namespaceUri);
        }

        if (attribute == null)
        {
            XmlDocument document = element.OwnerDocument;
            if (document == null)
            {
                throw new System.ArgumentException("Element must belong to a document.", "element");
            }

            attribute = document.CreateAttribute(prefix, localName, namespaceUri);
            element.Attributes.Append(attribute);
        }

        attribute.Value = value;
    }

    internal static int GetIntAttribute(XmlElement element, string namespaceUri, string localName, int fallbackValue)
    {
        string value = GetAttributeValue(element, namespaceUri, localName);
        if (value.Length == 0)
        {
            return fallbackValue;
        }

        int parsed = 0;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        return fallbackValue;
    }

    internal static double GetDoubleAttribute(XmlElement element, string namespaceUri, string localName, double fallbackValue)
    {
        string value = GetAttributeValue(element, namespaceUri, localName);
        if (value.Length == 0)
        {
            return fallbackValue;
        }

        double parsed = 0;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        return fallbackValue;
    }

    internal static bool GetBoolAttribute(XmlElement element, string namespaceUri, string localName, bool fallbackValue)
    {
        string value = GetAttributeValue(element, namespaceUri, localName);
        if (value.Length == 0)
        {
            return fallbackValue;
        }

        if (value == "1")
        {
            return true;
        }

        if (value == "0")
        {
            return false;
        }

        bool parsed = false;
        if (bool.TryParse(value, out parsed))
        {
            return parsed;
        }

        return fallbackValue;
    }

    internal static void InsertChildInOrder(XmlElement parent, XmlElement child, string namespaceUri, string[] orderedLocalNames)
    {
        if (parent == null)
        {
            throw new System.ArgumentNullException("parent");
        }

        if (child == null)
        {
            throw new System.ArgumentNullException("child");
        }

        if (namespaceUri == null)
        {
            throw new System.ArgumentNullException("namespaceUri");
        }

        if (orderedLocalNames == null)
        {
            throw new System.ArgumentNullException("orderedLocalNames");
        }

        int childOrder = IndexOfOrderedName(orderedLocalNames, child.LocalName);
        if (child.NamespaceURI != namespaceUri || childOrder < 0)
        {
            parent.AppendChild(child);
            return;
        }

        for (int index = 0; index < parent.ChildNodes.Count; index++)
        {
            XmlNode? existingNode = parent.ChildNodes[index];
            if (existingNode == null)
            {
                continue;
            }

            if (existingNode.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement existing = (XmlElement)existingNode;
            if (existing.NamespaceURI != namespaceUri)
            {
                continue;
            }

            int existingOrder = IndexOfOrderedName(orderedLocalNames, existing.LocalName);
            if (existingOrder > childOrder)
            {
                parent.InsertBefore(child, existing);
                return;
            }
        }

        parent.AppendChild(child);
    }

    internal static int IndexOfOrderedName(string[] orderedLocalNames, string localName)
    {
        if (orderedLocalNames == null)
        {
            throw new System.ArgumentNullException("orderedLocalNames");
        }

        if (localName == null)
        {
            throw new System.ArgumentNullException("localName");
        }

        for (int index = 0; index < orderedLocalNames.Length; index++)
        {
            if (orderedLocalNames[index] == localName)
            {
                return index;
            }
        }

        return -1;
    }
}
