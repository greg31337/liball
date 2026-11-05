using System.Xml;

namespace docx;

internal class DocxXmlPreservation
{
    internal static string PreserveRootMetadata(string originalXml, string updatedXml)
    {
        if (originalXml == null)
        {
            throw new ArgumentNullException("originalXml");
        }

        if (updatedXml == null)
        {
            throw new ArgumentNullException("updatedXml");
        }

        XmlDocument originalDocument = LoadDocument(originalXml);
        XmlDocument updatedDocument = LoadDocument(updatedXml);
        XmlElement? originalRoot = originalDocument.DocumentElement;
        XmlElement? updatedRoot = updatedDocument.DocumentElement;

        if (originalRoot == null || updatedRoot == null)
        {
            return updatedXml;
        }

        if (originalRoot.LocalName != updatedRoot.LocalName || originalRoot.NamespaceURI != updatedRoot.NamespaceURI)
        {
            return updatedXml;
        }

        CopyRootMetadata(originalRoot, updatedRoot, updatedDocument);
        return updatedDocument.OuterXml;
    }

    internal static string PreserveContainerContent(string originalXml, string updatedXml, List<string> modeledChildNames)
    {
        if (originalXml == null)
        {
            throw new ArgumentNullException("originalXml");
        }

        if (updatedXml == null)
        {
            throw new ArgumentNullException("updatedXml");
        }

        if (modeledChildNames == null)
        {
            throw new ArgumentNullException("modeledChildNames");
        }

        XmlDocument originalDocument = LoadDocument(originalXml);
        XmlDocument updatedDocument = LoadDocument(updatedXml);
        XmlElement? originalRoot = originalDocument.DocumentElement;
        XmlElement? updatedRoot = updatedDocument.DocumentElement;

        if (originalRoot == null || updatedRoot == null)
        {
            return updatedXml;
        }

        if (originalRoot.LocalName != updatedRoot.LocalName || originalRoot.NamespaceURI != updatedRoot.NamespaceURI)
        {
            return updatedXml;
        }

        CopyRootMetadata(originalRoot, updatedRoot, updatedDocument);
        CopyModeledChildMetadata(originalRoot, updatedRoot, updatedDocument, modeledChildNames);
        CopyUnmodeledChildren(originalRoot, updatedRoot, updatedDocument, modeledChildNames);
        return updatedDocument.OuterXml;
    }

    internal static string PreserveRelationshipContent(string originalXml, string updatedXml)
    {
        List<string> modeledChildNames = new List<string>();
        modeledChildNames.Add("Relationship");
        return PreserveContainerContent(originalXml, updatedXml, modeledChildNames);
    }

    internal static XmlDocument LoadDocument(string xml)
    {
        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(xml);
        return document;
    }

    internal static void CopyRootMetadata(XmlElement originalRoot, XmlElement updatedRoot, XmlDocument updatedDocument)
    {
        updatedRoot.Prefix = originalRoot.Prefix;

        foreach (XmlAttribute attribute in originalRoot.Attributes)
        {
            XmlAttribute? existing = updatedRoot.GetAttributeNode(attribute.LocalName, attribute.NamespaceURI);

            if (existing == null)
            {
                XmlAttribute imported = (XmlAttribute)updatedDocument.ImportNode(attribute, true);
                updatedRoot.Attributes.Append(imported);
            }
        }
    }

    internal static void CopyUnmodeledChildren(XmlElement originalRoot, XmlElement updatedRoot, XmlDocument updatedDocument, List<string> modeledChildNames)
    {
        int modeledIndex = 0;
        Dictionary<string, int> unmodeledIndexes = new Dictionary<string, int>();

        foreach (XmlNode child in originalRoot.ChildNodes)
        {
            if (IsModeledChild(child, originalRoot.NamespaceURI, modeledChildNames))
            {
                modeledIndex = modeledIndex + 1;
                continue;
            }

            if (child.NodeType == XmlNodeType.Element)
            {
                string key = child.NamespaceURI + "\n" + child.LocalName;
                int unmodeledIndex = 0;

                if (unmodeledIndexes.ContainsKey(key))
                {
                    unmodeledIndex = unmodeledIndexes[key];
                }

                XmlElement? updatedChild = FindUnmodeledElementByNameIndex(
                    updatedRoot,
                    child.LocalName,
                    child.NamespaceURI,
                    modeledChildNames,
                    unmodeledIndex);
                unmodeledIndexes[key] = unmodeledIndex + 1;

                if (updatedChild != null)
                {
                    CopyRootMetadata((XmlElement)child, updatedChild, updatedDocument);
                    continue;
                }
            }

            XmlNode imported = updatedDocument.ImportNode(child, true);
            XmlNode? before = FindModeledChild(updatedRoot, originalRoot.NamespaceURI, modeledChildNames, modeledIndex);

            if (before == null)
            {
                updatedRoot.AppendChild(imported);
            }
            else
            {
                updatedRoot.InsertBefore(imported, before);
            }
        }
    }

    internal static void CopyModeledChildMetadata(XmlElement originalRoot, XmlElement updatedRoot, XmlDocument updatedDocument, List<string> modeledChildNames)
    {
        Dictionary<string, int> childIndexes = new Dictionary<string, int>();

        foreach (XmlNode child in originalRoot.ChildNodes)
        {
            if (!IsModeledChild(child, originalRoot.NamespaceURI, modeledChildNames))
            {
                continue;
            }

            int childIndex = 0;

            if (childIndexes.ContainsKey(child.LocalName))
            {
                childIndex = childIndexes[child.LocalName];
            }

            XmlElement? updatedChild = FindElementByNameIndex(updatedRoot, child.LocalName, child.NamespaceURI, childIndex);

            if (updatedChild != null)
            {
                CopyRootMetadata((XmlElement)child, updatedChild, updatedDocument);
            }

            childIndexes[child.LocalName] = childIndex + 1;
        }
    }

    internal static bool IsModeledChild(XmlNode child, string namespaceUri, List<string> modeledChildNames)
    {
        if (child.NodeType != XmlNodeType.Element || child.NamespaceURI != namespaceUri)
        {
            return false;
        }

        foreach (string localName in modeledChildNames)
        {
            if (child.LocalName == localName)
            {
                return true;
            }
        }

        return false;
    }

    internal static XmlNode? FindModeledChild(XmlElement root, string namespaceUri, List<string> modeledChildNames, int modeledIndex)
    {
        int currentIndex = 0;

        foreach (XmlNode child in root.ChildNodes)
        {
            if (IsModeledChild(child, namespaceUri, modeledChildNames))
            {
                if (currentIndex == modeledIndex)
                {
                    return child;
                }

                currentIndex = currentIndex + 1;
            }
        }

        return null;
    }

    internal static XmlElement? FindElementByNameIndex(XmlElement root, string localName, string namespaceUri, int elementIndex)
    {
        int currentIndex = 0;

        foreach (XmlNode child in root.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element && child.LocalName == localName && child.NamespaceURI == namespaceUri)
            {
                if (currentIndex == elementIndex)
                {
                    return (XmlElement)child;
                }

                currentIndex = currentIndex + 1;
            }
        }

        return null;
    }

    internal static XmlElement? FindUnmodeledElementByNameIndex(
        XmlElement root,
        string localName,
        string namespaceUri,
        List<string> modeledChildNames,
        int elementIndex)
    {
        int currentIndex = 0;

        foreach (XmlNode child in root.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element
                && child.LocalName == localName
                && child.NamespaceURI == namespaceUri
                && !IsModeledChild(child, root.NamespaceURI, modeledChildNames))
            {
                if (currentIndex == elementIndex)
                {
                    return (XmlElement)child;
                }

                currentIndex = currentIndex + 1;
            }
        }

        return null;
    }
}
