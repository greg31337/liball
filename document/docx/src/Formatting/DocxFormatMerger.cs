namespace docx;

internal static class DocxFormatMerger
{
    internal static string Merge(string rootName, List<string> formats)
    {
        if (rootName == null)
        {
            throw new ArgumentNullException("rootName");
        }

        if (formats == null)
        {
            throw new ArgumentNullException("formats");
        }

        System.Xml.XmlDocument targetDocument = new System.Xml.XmlDocument();
        targetDocument.PreserveWhitespace = true;
        targetDocument.LoadXml("<w:" + rootName + " xmlns:w=\"" + DocxConstants.NamespaceWordprocessingMl + "\" />");
        System.Xml.XmlElement? targetRootValue = targetDocument.DocumentElement;

        if (targetRootValue == null)
        {
            throw new InvalidOperationException("Formatting root element is missing.");
        }

        System.Xml.XmlElement targetRoot = targetRootValue;

        foreach (string formatXml in formats)
        {
            if (formatXml != "")
            {
                System.Xml.XmlDocument sourceDocument = new System.Xml.XmlDocument();
                sourceDocument.PreserveWhitespace = true;
                sourceDocument.LoadXml(formatXml);

                if (sourceDocument.DocumentElement != null)
                {
                    MergeChildren(targetDocument, targetRoot, sourceDocument.DocumentElement);
                }
            }
        }

        return targetRoot.OuterXml;
    }

    internal static string GetDirectChildXml(string xml, string localName)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (xml == "")
        {
            return "";
        }

        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(xml);

        if (document.DocumentElement == null)
        {
            return "";
        }

        foreach (System.Xml.XmlNode node in document.DocumentElement.ChildNodes)
        {
            if (node.NodeType == System.Xml.XmlNodeType.Element
                && node.LocalName == localName
                && node.NamespaceURI == DocxConstants.NamespaceWordprocessingMl)
            {
                return node.OuterXml;
            }
        }

        return "";
    }

    internal static void MergeChildren(System.Xml.XmlDocument targetDocument, System.Xml.XmlElement target, System.Xml.XmlElement source)
    {
        if (targetDocument == null)
        {
            throw new ArgumentNullException("targetDocument");
        }

        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        foreach (System.Xml.XmlNode sourceNode in source.ChildNodes)
        {
            if (sourceNode.NodeType == System.Xml.XmlNodeType.Element)
            {
                System.Xml.XmlElement sourceElement = (System.Xml.XmlElement)sourceNode;
                System.Xml.XmlElement? targetElement = FindChild(target, sourceElement.LocalName, sourceElement.NamespaceURI);

                if (targetElement == null)
                {
                    target.AppendChild(targetDocument.ImportNode(sourceElement, true));
                }
                else if (sourceElement.LocalName == "tabs")
                {
                    target.ReplaceChild(targetDocument.ImportNode(sourceElement, true), targetElement);
                }
                else if (HasElementChildren(sourceElement))
                {
                    MergeAttributes(targetElement, sourceElement);
                    MergeChildren(targetDocument, targetElement, sourceElement);
                }
                else if (ShouldMergeAttributes(sourceElement.LocalName))
                {
                    MergeAttributes(targetElement, sourceElement);
                }
                else
                {
                    target.ReplaceChild(targetDocument.ImportNode(sourceElement, true), targetElement);
                }
            }
        }
    }

    internal static System.Xml.XmlElement? FindChild(System.Xml.XmlElement parent, string localName, string namespaceUri)
    {
        if (parent == null)
        {
            throw new ArgumentNullException("parent");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        foreach (System.Xml.XmlNode node in parent.ChildNodes)
        {
            if (node.NodeType == System.Xml.XmlNodeType.Element
                && node.LocalName == localName
                && node.NamespaceURI == namespaceUri)
            {
                return (System.Xml.XmlElement)node;
            }
        }

        return null;
    }

    internal static bool HasElementChildren(System.Xml.XmlElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        foreach (System.Xml.XmlNode node in element.ChildNodes)
        {
            if (node.NodeType == System.Xml.XmlNodeType.Element)
            {
                return true;
            }
        }

        return false;
    }

    internal static void MergeAttributes(System.Xml.XmlElement target, System.Xml.XmlElement source)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        ClearConflictingAttributes(target, source);

        foreach (System.Xml.XmlAttribute attribute in source.Attributes)
        {
            if (attribute.NamespaceURI != "http://www.w3.org/2000/xmlns/")
            {
                target.SetAttribute(attribute.LocalName, attribute.NamespaceURI, attribute.Value);
            }
        }
    }

    internal static void ClearConflictingAttributes(System.Xml.XmlElement target, System.Xml.XmlElement source)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        string wordNamespace = DocxConstants.NamespaceWordprocessingMl;

        if (source.LocalName == "rFonts")
        {
            ClearFontPair(target, source, "ascii", "asciiTheme");
            ClearFontPair(target, source, "hAnsi", "hAnsiTheme");
            ClearFontPair(target, source, "eastAsia", "eastAsiaTheme");
            ClearFontPair(target, source, "cs", "cstheme");
        }
        else if (source.LocalName == "color")
        {
            if (source.HasAttribute("val", wordNamespace))
            {
                target.RemoveAttribute("themeColor", wordNamespace);
                target.RemoveAttribute("themeTint", wordNamespace);
                target.RemoveAttribute("themeShade", wordNamespace);
            }

            if (source.HasAttribute("themeColor", wordNamespace))
            {
                target.RemoveAttribute("val", wordNamespace);
                target.RemoveAttribute("themeTint", wordNamespace);
                target.RemoveAttribute("themeShade", wordNamespace);
            }
        }
        else if (source.LocalName == "u")
        {
            if (source.HasAttribute("color", wordNamespace))
            {
                target.RemoveAttribute("themeColor", wordNamespace);
                target.RemoveAttribute("themeTint", wordNamespace);
                target.RemoveAttribute("themeShade", wordNamespace);
            }

            if (source.HasAttribute("themeColor", wordNamespace))
            {
                target.RemoveAttribute("color", wordNamespace);
                target.RemoveAttribute("themeTint", wordNamespace);
                target.RemoveAttribute("themeShade", wordNamespace);
            }
        }
        else if (source.LocalName == "shd")
        {
            if (source.HasAttribute("color", wordNamespace))
            {
                target.RemoveAttribute("themeColor", wordNamespace);
                target.RemoveAttribute("themeTint", wordNamespace);
                target.RemoveAttribute("themeShade", wordNamespace);
            }

            if (source.HasAttribute("themeColor", wordNamespace))
            {
                target.RemoveAttribute("color", wordNamespace);
                target.RemoveAttribute("themeTint", wordNamespace);
                target.RemoveAttribute("themeShade", wordNamespace);
            }

            if (source.HasAttribute("fill", wordNamespace))
            {
                target.RemoveAttribute("themeFill", wordNamespace);
                target.RemoveAttribute("themeFillTint", wordNamespace);
                target.RemoveAttribute("themeFillShade", wordNamespace);
            }

            if (source.HasAttribute("themeFill", wordNamespace))
            {
                target.RemoveAttribute("fill", wordNamespace);
                target.RemoveAttribute("themeFillTint", wordNamespace);
                target.RemoveAttribute("themeFillShade", wordNamespace);
            }
        }
    }

    internal static void ClearFontPair(System.Xml.XmlElement target, System.Xml.XmlElement source, string directName, string themeName)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (directName == null)
        {
            throw new ArgumentNullException("directName");
        }

        if (themeName == null)
        {
            throw new ArgumentNullException("themeName");
        }

        string wordNamespace = DocxConstants.NamespaceWordprocessingMl;

        if (source.HasAttribute(directName, wordNamespace))
        {
            target.RemoveAttribute(themeName, wordNamespace);
        }

        if (source.HasAttribute(themeName, wordNamespace))
        {
            target.RemoveAttribute(directName, wordNamespace);
        }
    }

    internal static bool ShouldMergeAttributes(string localName)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        return localName == "rFonts"
            || localName == "u"
            || localName == "color"
            || localName == "lang"
            || localName == "spacing"
            || localName == "ind"
            || localName == "shd"
            || localName == "tblW"
            || localName == "tblInd"
            || localName == "tblCellSpacing"
            || localName == "tblLook"
            || localName == "tcW"
            || localName == "trHeight"
            || localName == "top"
            || localName == "left"
            || localName == "start"
            || localName == "bottom"
            || localName == "right"
            || localName == "end"
            || localName == "insideH"
            || localName == "insideV"
            || localName == "between"
            || localName == "bar";
    }
}
