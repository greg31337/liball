using System.Xml;

namespace docx;

internal class DocxXmlReader
{
    internal XmlReader Reader;

    internal DocxXmlReader(XmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        this.Reader = reader;
    }

    internal static DocxXmlReader Create(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        StringReader stringReader = new StringReader(xml);
        XmlReader reader = XmlReader.Create(stringReader);
        return new DocxXmlReader(reader);
    }

    internal bool Read()
    {
        return this.Reader.Read();
    }

    internal bool MoveToContent()
    {
        XmlNodeType nodeType = this.Reader.MoveToContent();
        return nodeType != XmlNodeType.None;
    }

    internal bool IsElement(string localName, string namespaceUri)
    {
        ValidateName(localName, namespaceUri);

        return this.Reader.NodeType == XmlNodeType.Element
            && this.Reader.LocalName == localName
            && this.Reader.NamespaceURI == namespaceUri;
    }

    internal bool IsEndElement(string localName, string namespaceUri)
    {
        ValidateName(localName, namespaceUri);

        return this.Reader.NodeType == XmlNodeType.EndElement
            && this.Reader.LocalName == localName
            && this.Reader.NamespaceURI == namespaceUri;
    }

    internal bool ReadToElement(string localName, string namespaceUri)
    {
        ValidateName(localName, namespaceUri);

        while (this.Reader.Read())
        {
            if (IsElement(localName, namespaceUri))
            {
                return true;
            }
        }

        return false;
    }

    internal bool ReadToChildElement(string localName, string namespaceUri)
    {
        ValidateName(localName, namespaceUri);

        if (this.Reader.NodeType != XmlNodeType.Element)
        {
            return false;
        }

        if (this.Reader.IsEmptyElement)
        {
            return false;
        }

        int parentDepth = this.Reader.Depth;

        while (this.Reader.Read())
        {
            if (this.Reader.NodeType == XmlNodeType.Element
                && this.Reader.Depth == parentDepth + 1
                && this.Reader.LocalName == localName
                && this.Reader.NamespaceURI == namespaceUri)
            {
                return true;
            }

            if (this.Reader.NodeType == XmlNodeType.EndElement
                && this.Reader.Depth == parentDepth)
            {
                return false;
            }
        }

        return false;
    }

    internal bool ReadToNextSiblingElement(string localName, string namespaceUri)
    {
        ValidateName(localName, namespaceUri);

        int siblingDepth = this.Reader.Depth;

        if (this.Reader.NodeType == XmlNodeType.Element)
        {
            ReadPastCurrentElement();
        }
        else if (!this.Reader.Read())
        {
            return false;
        }

        while (!this.Reader.EOF)
        {
            if (this.Reader.NodeType == XmlNodeType.Element)
            {
                if (this.Reader.Depth < siblingDepth)
                {
                    return false;
                }

                if (this.Reader.Depth == siblingDepth)
                {
                    if (this.Reader.LocalName == localName && this.Reader.NamespaceURI == namespaceUri)
                    {
                        return true;
                    }

                    ReadPastCurrentElement();
                    continue;
                }
            }

            if (this.Reader.NodeType == XmlNodeType.EndElement && this.Reader.Depth < siblingDepth)
            {
                return false;
            }

            if (!this.Reader.Read())
            {
                return false;
            }
        }

        return false;
    }

    internal string ReadElementText(string localName, string namespaceUri)
    {
        if (!IsElement(localName, namespaceUri))
        {
            throw new InvalidOperationException("Reader is not positioned on the expected element.");
        }

        return this.Reader.ReadElementContentAsString();
    }

    internal string GetAttribute(string localName, string namespaceUri)
    {
        ValidateName(localName, namespaceUri);

        string? value = this.Reader.GetAttribute(localName, namespaceUri);

        if (value == null)
        {
            return "";
        }

        return value;
    }

    internal void ReadPastCurrentElement()
    {
        if (this.Reader.NodeType != XmlNodeType.Element)
        {
            this.Reader.Read();
            return;
        }

        if (this.Reader.IsEmptyElement)
        {
            this.Reader.Read();
            return;
        }

        int startDepth = this.Reader.Depth;

        while (this.Reader.Read())
        {
            if (this.Reader.NodeType == XmlNodeType.EndElement && this.Reader.Depth == startDepth)
            {
                this.Reader.Read();
                return;
            }
        }
    }

    internal int Depth()
    {
        return this.Reader.Depth;
    }

    internal string LocalName()
    {
        return this.Reader.LocalName;
    }

    internal string NamespaceUri()
    {
        return this.Reader.NamespaceURI;
    }

    internal static void ValidateName(string localName, string namespaceUri)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (localName == "")
        {
            throw new ArgumentException("Local name is required.", "localName");
        }
    }
}
