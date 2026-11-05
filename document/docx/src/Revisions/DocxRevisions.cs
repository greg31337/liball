using System.Xml;

namespace docx;

internal class DocxRevisions
{
    internal List<DocxRevision> Revisions;

    internal DocxRevisions()
    {
        this.Revisions = new List<DocxRevision>();
    }

    internal void AddRevision(DocxRevision revision)
    {
        if (revision == null)
        {
            throw new ArgumentNullException("revision");
        }

        this.Revisions.Add(revision);
    }

    internal int RevisionCount()
    {
        return this.Revisions.Count;
    }

    internal DocxRevision GetRevision(int index)
    {
        return this.Revisions[index];
    }

    internal int CountRevisions(string kind)
    {
        if (kind == null)
        {
            throw new ArgumentNullException("kind");
        }

        if (kind == "")
        {
            throw new ArgumentException("Revision kind is required.", "kind");
        }

        int count = 0;

        foreach (DocxRevision revision in this.Revisions)
        {
            if (revision.IsKind(kind))
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal DocxRevision? FindRevisionById(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        foreach (DocxRevision revision in this.Revisions)
        {
            if (revision.Id == id)
            {
                return revision;
            }
        }

        return null;
    }

    internal static string AcceptRevisionXml(string xml)
    {
        return DocxRevision.AcceptRevisionXml(xml);
    }

    internal static string RejectRevisionXml(string xml)
    {
        return DocxRevision.RejectRevisionXml(xml);
    }
}

internal class DocxRevision
{
    internal const string KindInserted = "inserted";
    internal const string KindDeleted = "deleted";
    internal const string KindMoveFrom = "moveFrom";
    internal const string KindMoveTo = "moveTo";
    internal const string KindFormatting = "formatting";

    internal string Xml;
    internal string Prefix;
    internal string LocalName;
    internal string NamespaceUri;
    internal string Kind;
    internal string Id;
    internal string Author;
    internal string Date;
    internal List<DocxXmlBuffer> Children;
    internal List<DocxBlock> Blocks;
    internal List<DocxRun> Runs;
    internal List<DocxXmlBuffer> UnknownChildren;

    internal DocxRevision(string xml, string prefix, string localName, string namespaceUri)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

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
            throw new ArgumentException("Revision local name is required.", "localName");
        }

        this.Xml = xml;
        this.Prefix = prefix;
        this.LocalName = localName;
        this.NamespaceUri = namespaceUri;
        this.Kind = KindFromLocalName(localName);
        this.Id = "";
        this.Author = "";
        this.Date = "";
        this.Children = new List<DocxXmlBuffer>();
        this.Blocks = new List<DocxBlock>();
        this.Runs = new List<DocxRun>();
        this.UnknownChildren = new List<DocxXmlBuffer>();
        LoadFromXml();
    }

    internal static bool IsSupported(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.NamespaceUri() != DocxConstants.NamespaceWordprocessingMl)
        {
            return false;
        }

        return IsRevisionLocalName(reader.LocalName());
    }

    internal static bool IsRevisionLocalName(string localName)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        return localName == "ins"
            || localName == "del"
            || localName == "moveFrom"
            || localName == "moveTo"
            || localName == "rPrChange"
            || localName == "pPrChange"
            || localName == "tblPrChange"
            || localName == "trPrChange"
            || localName == "tcPrChange"
            || localName == "sectPrChange";
    }

    internal static string KindFromLocalName(string localName)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (localName == "ins")
        {
            return KindInserted;
        }

        if (localName == "del")
        {
            return KindDeleted;
        }

        if (localName == "moveFrom")
        {
            return KindMoveFrom;
        }

        if (localName == "moveTo")
        {
            return KindMoveTo;
        }

        return KindFormatting;
    }

    internal static DocxRevision Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!IsSupported(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on revision markup.");
        }

        string prefix = reader.Reader.Prefix;
        string localName = reader.LocalName();
        string namespaceUri = reader.NamespaceUri();
        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxRevision(buffer.GetXml(), prefix, localName, namespaceUri);
    }

    internal static DocxRevision ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);
        reader.MoveToContent();
        return Parse(reader);
    }

    internal static string AcceptRevisionXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (xml == "")
        {
            return "";
        }

        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(xml);

        if (document.DocumentElement == null)
        {
            return "";
        }

        StringWriter stringWriter = new StringWriter();
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        settings.ConformanceLevel = ConformanceLevel.Fragment;
        XmlWriter writer = XmlWriter.Create(stringWriter, settings);

        WriteAcceptedNode(document.DocumentElement, writer);
        writer.Flush();
        return stringWriter.ToString();
    }

    internal static string RejectRevisionXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (xml == "")
        {
            return "";
        }

        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(xml);

        if (document.DocumentElement == null)
        {
            return "";
        }

        StringWriter stringWriter = new StringWriter();
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        settings.ConformanceLevel = ConformanceLevel.Fragment;
        XmlWriter writer = XmlWriter.Create(stringWriter, settings);

        WriteRejectedNode(document.DocumentElement, writer);
        writer.Flush();
        return stringWriter.ToString();
    }

    internal static int CountRevisionElementsXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (xml == "")
        {
            return 0;
        }

        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(xml);

        if (document.DocumentElement == null)
        {
            return 0;
        }

        return CountRevisionElementsNode(document.DocumentElement);
    }

    internal static int CountRevisionElementsNode(XmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        int count = 0;

        if (node.NodeType == XmlNodeType.Element)
        {
            XmlElement element = (XmlElement)node;

            if (IsRevisionElement(element))
            {
                count = count + 1;
            }
        }

        foreach (XmlNode child in node.ChildNodes)
        {
            count = count + CountRevisionElementsNode(child);
        }

        return count;
    }

    internal static void WriteAcceptedNode(XmlNode node, XmlWriter writer)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (node.NodeType == XmlNodeType.Element)
        {
            WriteAcceptedElement((XmlElement)node, writer);
            return;
        }

        if (node.NodeType == XmlNodeType.Text)
        {
            writer.WriteString(node.Value);
            return;
        }

        if (node.NodeType == XmlNodeType.CDATA)
        {
            writer.WriteCData(node.Value);
            return;
        }

        if (node.NodeType == XmlNodeType.Whitespace || node.NodeType == XmlNodeType.SignificantWhitespace)
        {
            writer.WriteWhitespace(node.Value);
            return;
        }

        if (node.NodeType == XmlNodeType.Comment)
        {
            writer.WriteComment(node.Value);
            return;
        }

        if (node.NodeType == XmlNodeType.ProcessingInstruction)
        {
            writer.WriteProcessingInstruction(node.Name, node.Value);
            return;
        }
    }

    internal static void WriteAcceptedElement(XmlElement element, XmlWriter writer)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (IsRevisionElement(element))
        {
            if (ShouldKeepAcceptedRevisionChildren(element))
            {
                foreach (XmlNode child in element.ChildNodes)
                {
                    WriteAcceptedNode(child, writer);
                }
            }

            return;
        }

        writer.WriteStartElement(element.Prefix, element.LocalName, element.NamespaceURI);

        foreach (XmlAttribute attribute in element.Attributes)
        {
            writer.WriteAttributeString(attribute.Prefix, attribute.LocalName, attribute.NamespaceURI, attribute.Value);
        }

        foreach (XmlNode child in element.ChildNodes)
        {
            WriteAcceptedNode(child, writer);
        }

        writer.WriteEndElement();
    }

    internal static void WriteRejectedNode(XmlNode node, XmlWriter writer)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (node.NodeType == XmlNodeType.Element)
        {
            WriteRejectedElement((XmlElement)node, writer);
            return;
        }

        WriteAcceptedNode(node, writer);
    }

    internal static void WriteRejectedElement(XmlElement element, XmlWriter writer)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (IsDeletedTextElement(element))
        {
            WriteRejectedDeletedTextElement(element, writer);
            return;
        }

        if (HasRejectedPropertyChange(element))
        {
            WriteRejectedPropertyElement(element, writer);
            return;
        }

        if (IsRevisionElement(element))
        {
            if (ShouldKeepRejectedRevisionChildren(element))
            {
                foreach (XmlNode child in element.ChildNodes)
                {
                    WriteRejectedNode(child, writer);
                }
            }

            return;
        }

        writer.WriteStartElement(element.Prefix, element.LocalName, element.NamespaceURI);

        foreach (XmlAttribute attribute in element.Attributes)
        {
            writer.WriteAttributeString(attribute.Prefix, attribute.LocalName, attribute.NamespaceURI, attribute.Value);
        }

        foreach (XmlNode child in element.ChildNodes)
        {
            WriteRejectedNode(child, writer);
        }

        writer.WriteEndElement();
    }

    internal static bool IsDeletedTextElement(XmlElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        return element.LocalName == "delText" && element.NamespaceURI == DocxConstants.NamespaceWordprocessingMl;
    }

    internal static void WriteRejectedDeletedTextElement(XmlElement element, XmlWriter writer)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.WriteStartElement(element.Prefix, "t", element.NamespaceURI);

        foreach (XmlAttribute attribute in element.Attributes)
        {
            writer.WriteAttributeString(attribute.Prefix, attribute.LocalName, attribute.NamespaceURI, attribute.Value);
        }

        foreach (XmlNode child in element.ChildNodes)
        {
            WriteRejectedNode(child, writer);
        }

        writer.WriteEndElement();
    }

    internal static bool HasRejectedPropertyChange(XmlElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        return FindRejectedPropertyElement(element) != null;
    }

    internal static XmlElement? FindRejectedPropertyElement(XmlElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        string changeName = element.LocalName + "Change";

        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                XmlElement changeElement = (XmlElement)child;

                if (changeElement.LocalName == changeName && IsRevisionElement(changeElement))
                {
                    foreach (XmlNode changeChild in changeElement.ChildNodes)
                    {
                        if (changeChild.NodeType == XmlNodeType.Element)
                        {
                            XmlElement replacement = (XmlElement)changeChild;

                            if (replacement.LocalName == element.LocalName && replacement.NamespaceURI == element.NamespaceURI)
                            {
                                return replacement;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    internal static void WriteRejectedPropertyElement(XmlElement element, XmlWriter writer)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        XmlElement? replacement = FindRejectedPropertyElement(element);

        writer.WriteStartElement(element.Prefix, element.LocalName, element.NamespaceURI);

        if (replacement != null)
        {
            foreach (XmlAttribute attribute in replacement.Attributes)
            {
                writer.WriteAttributeString(attribute.Prefix, attribute.LocalName, attribute.NamespaceURI, attribute.Value);
            }

            foreach (XmlNode child in replacement.ChildNodes)
            {
                WriteRejectedNode(child, writer);
            }
        }

        writer.WriteEndElement();
    }

    internal static bool IsRevisionElement(XmlElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (element.NamespaceURI != DocxConstants.NamespaceWordprocessingMl)
        {
            return false;
        }

        return IsRevisionLocalName(element.LocalName);
    }

    internal static bool ShouldKeepAcceptedRevisionChildren(XmlElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        return element.LocalName == "ins" || element.LocalName == "moveTo";
    }

    internal static bool ShouldKeepRejectedRevisionChildren(XmlElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (element.LocalName == "del" || element.LocalName == "moveFrom")
        {
            return true;
        }

        if (IsFormattingChangeLocalName(element.LocalName))
        {
            return true;
        }

        return IsRevisionLocalName(element.LocalName) && element.LocalName != "ins" && element.LocalName != "moveTo";
    }

    internal static bool IsFormattingChangeLocalName(string localName)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        return localName == "rPrChange"
            || localName == "pPrChange"
            || localName == "tblPrChange"
            || localName == "trPrChange"
            || localName == "tcPrChange"
            || localName == "sectPrChange";
    }

    internal string AcceptXml()
    {
        return AcceptRevisionXml(this.Xml);
    }

    internal string RejectXml()
    {
        return RejectRevisionXml(this.Xml);
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool HasXml()
    {
        return this.Xml != "";
    }

    internal void LoadFromXml()
    {
        this.Id = "";
        this.Author = "";
        this.Date = "";
        this.Children = new List<DocxXmlBuffer>();
        this.Blocks = new List<DocxBlock>();
        this.Runs = new List<DocxRun>();
        this.UnknownChildren = new List<DocxXmlBuffer>();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.MoveToContent())
        {
            return;
        }

        if (!reader.IsElement(this.LocalName, this.NamespaceUri))
        {
            return;
        }

        this.Id = reader.GetAttribute("id", DocxConstants.NamespaceWordprocessingMl);
        this.Author = reader.GetAttribute("author", DocxConstants.NamespaceWordprocessingMl);
        this.Date = reader.GetAttribute("date", DocxConstants.NamespaceWordprocessingMl);

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int revisionDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == revisionDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == revisionDepth + 1)
            {
                bool isRun = reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl);
                bool isParagraph = reader.IsElement("p", DocxConstants.NamespaceWordprocessingMl);
                bool isTable = reader.IsElement("tbl", DocxConstants.NamespaceWordprocessingMl);
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                this.Children.Add(child);

                if (isRun)
                {
                    this.Runs.Add(DocxRunFromBuffer(child));
                }
                else if (isParagraph)
                {
                    this.Blocks.Add(DocxBlock.FromParagraph(DocxParagraphFromBuffer(child)));
                }
                else if (isTable)
                {
                    this.Blocks.Add(DocxBlock.FromTable(DocxTableFromBuffer(child)));
                }
                else
                {
                    this.UnknownChildren.Add(child);
                }

                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal static DocxRun DocxRunFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxRun.Parse(reader);
    }

    internal static DocxParagraph DocxParagraphFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxParagraph.Parse(reader);
    }

    internal static DocxTable DocxTableFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxTable.Parse(reader);
    }

    internal bool IsKind(string kind)
    {
        if (kind == null)
        {
            throw new ArgumentNullException("kind");
        }

        if (kind == "")
        {
            throw new ArgumentException("Revision kind is required.", "kind");
        }

        return this.Kind == kind;
    }

    internal bool IsInserted()
    {
        return IsKind(KindInserted);
    }

    internal bool IsDeleted()
    {
        return IsKind(KindDeleted);
    }

    internal bool IsMoveFrom()
    {
        return IsKind(KindMoveFrom);
    }

    internal bool IsMoveTo()
    {
        return IsKind(KindMoveTo);
    }

    internal bool IsFormattingChange()
    {
        return IsKind(KindFormatting);
    }

    internal int ChildCount()
    {
        return this.Children.Count;
    }

    internal string GetChildXml(int index)
    {
        return this.Children[index].GetXml();
    }

    internal int BlockCount()
    {
        return this.Blocks.Count;
    }

    internal DocxBlock GetBlock(int index)
    {
        return this.Blocks[index];
    }

    internal int RunCount()
    {
        return this.Runs.Count;
    }

    internal DocxRun GetRun(int index)
    {
        return this.Runs[index];
    }

    internal int UnknownChildCount()
    {
        return this.UnknownChildren.Count;
    }

    internal string GetUnknownChildXml(int index)
    {
        return this.UnknownChildren[index].GetXml();
    }

    internal string GetText()
    {
        DocxTextOptions options = new DocxTextOptions();
        return GetTextWithOptions(options);
    }

    internal string GetTextWithOptions(DocxTextOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (IsInserted() && !options.IncludeInsertedText)
        {
            return "";
        }

        if (IsMoveTo() && !options.IncludeInsertedText)
        {
            return "";
        }

        if (IsDeleted() && !options.IncludeDeletedText)
        {
            return "";
        }

        if (IsMoveFrom() && !options.IncludeDeletedText)
        {
            return "";
        }

        if (IsFormattingChange())
        {
            return "";
        }

        string text = "";
        bool hasText = false;

        foreach (DocxXmlBuffer child in this.Children)
        {
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (reader.MoveToContent() && reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl))
            {
                text = text + DocxRunFromBuffer(child).GetTextWithOptions(options);
                hasText = true;
            }
            else if (reader.IsElement("p", DocxConstants.NamespaceWordprocessingMl))
            {
                if (hasText)
                {
                    text = text + options.ParagraphSeparator;
                }

                text = text + DocxParagraphFromBuffer(child).GetTextWithOptions(options);
                hasText = true;
            }
            else if (reader.IsElement("tbl", DocxConstants.NamespaceWordprocessingMl))
            {
                if (hasText)
                {
                    text = text + options.ParagraphSeparator;
                }

                text = text + DocxTableFromBuffer(child).GetTextWithOptions(options);
                hasText = true;
            }
        }

        return text;
    }

    internal string WriteXml()
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        WriteTo(writer);
        return writer.GetXml();
    }

    internal void WriteTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        DocxXmlBuffer buffer = new DocxXmlBuffer(this.Xml);
        buffer.WriteTo(writer);
    }
}
