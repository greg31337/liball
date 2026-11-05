namespace docx;

/// <summary>
/// Represents content control data used by a DOCX document.
/// </summary>
public class DocxContentControl : IDocxTextContent
{
    internal const string NamespaceWordprocessingMl2010 = "http://schemas.microsoft.com/office/word/2010/wordml";
    internal string Xml;
    internal string PropertiesXml;
    internal string ContentXml;
    internal string Alias;
    internal string Tag;
    internal string Type;
    internal string DataBindingStoreItemId;
    internal string DataBindingXPath;
    internal string DataBindingPrefixMappings;
    internal List<DocxXmlBuffer> ContentChildren;
    internal List<DocxBlock> Blocks;
    internal List<DocxRun> Runs;
    internal List<DocxContentControl> ContentControls;

    /// <summary>
    /// Creates a content control instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxContentControl(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.PropertiesXml = "";
        this.ContentXml = "";
        this.Alias = "";
        this.Tag = "";
        this.Type = "";
        this.DataBindingStoreItemId = "";
        this.DataBindingXPath = "";
        this.DataBindingPrefixMappings = "";
        this.ContentChildren = new List<DocxXmlBuffer>();
        this.Blocks = new List<DocxBlock>();
        this.Runs = new List<DocxRun>();
        this.ContentControls = new List<DocxContentControl>();
        LoadFromXml();
    }

    internal static bool IsSupported(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.IsElement("sdt", DocxConstants.NamespaceWordprocessingMl);
    }

    internal static DocxContentControl Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!IsSupported(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on w:sdt.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxContentControl(buffer.GetXml());
    }

    internal static DocxContentControl ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.MoveToContent() || !IsSupported(reader))
        {
            throw new InvalidOperationException("XML does not contain a w:sdt root element.");
        }

        return Parse(reader);
    }

    internal void LoadFromXml()
    {
        this.PropertiesXml = "";
        this.ContentXml = "";
        this.Alias = "";
        this.Tag = "";
        this.Type = "";
        this.DataBindingStoreItemId = "";
        this.DataBindingXPath = "";
        this.DataBindingPrefixMappings = "";
        this.ContentChildren = new List<DocxXmlBuffer>();
        this.Blocks = new List<DocxBlock>();
        this.Runs = new List<DocxRun>();
        this.ContentControls = new List<DocxContentControl>();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("sdt", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int sdtDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == sdtDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == sdtDepth + 1)
            {
                if (reader.IsElement("sdtPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    DocxXmlBuffer properties = DocxXmlBuffer.Capture(reader);
                    this.PropertiesXml = properties.GetXml();
                    ReadPropertiesXml(this.PropertiesXml);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("sdtContent", DocxConstants.NamespaceWordprocessingMl))
                {
                    DocxXmlBuffer content = DocxXmlBuffer.Capture(reader);
                    this.ContentXml = content.GetXml();
                    LoadContentFromXml(this.ContentXml);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void ReadPropertiesXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.ReadToElement("sdtPr", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int propertiesDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == propertiesDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == propertiesDepth + 1)
            {
                if (reader.IsElement("alias", DocxConstants.NamespaceWordprocessingMl))
                {
                    this.Alias = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
                    reader.ReadPastCurrentElement();
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("tag", DocxConstants.NamespaceWordprocessingMl))
                {
                    this.Tag = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
                    reader.ReadPastCurrentElement();
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("dataBinding", DocxConstants.NamespaceWordprocessingMl))
                {
                    this.DataBindingStoreItemId = reader.GetAttribute("storeItemID", DocxConstants.NamespaceWordprocessingMl);
                    this.DataBindingXPath = reader.GetAttribute("xpath", DocxConstants.NamespaceWordprocessingMl);
                    this.DataBindingPrefixMappings = reader.GetAttribute("prefixMappings", DocxConstants.NamespaceWordprocessingMl);
                    reader.ReadPastCurrentElement();
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (IsTypeElement(reader))
                {
                    if (this.Type == "")
                    {
                        this.Type = reader.LocalName();
                    }

                    reader.ReadPastCurrentElement();
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal static bool IsTypeElement(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.LocalName() == "checkbox" && reader.NamespaceUri() == NamespaceWordprocessingMl2010)
        {
            return true;
        }

        if (reader.NamespaceUri() != DocxConstants.NamespaceWordprocessingMl)
        {
            return false;
        }

        string localName = reader.LocalName();

        return localName == "text"
            || localName == "richText"
            || localName == "picture"
            || localName == "checkbox"
            || localName == "dropDownList"
            || localName == "comboBox"
            || localName == "date"
            || localName == "docPartObj"
            || localName == "docPartList"
            || localName == "citation"
            || localName == "group"
            || localName == "bibliography"
            || localName == "equation"
            || localName == "repeatingSection"
            || localName == "repeatingSectionItem";
    }

    internal void LoadContentFromXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.ContentChildren = new List<DocxXmlBuffer>();
        this.Blocks = new List<DocxBlock>();
        this.Runs = new List<DocxRun>();
        this.ContentControls = new List<DocxContentControl>();

        if (xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.ReadToElement("sdtContent", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int contentDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == contentDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == contentDepth + 1)
            {
                bool isParagraph = reader.IsElement("p", DocxConstants.NamespaceWordprocessingMl);
                bool isTable = reader.IsElement("tbl", DocxConstants.NamespaceWordprocessingMl);
                bool isRun = reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl);
                bool isContentControl = IsSupported(reader);
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);

                this.ContentChildren.Add(child);

                if (isParagraph)
                {
                    this.Blocks.Add(DocxBlock.FromParagraph(DocxParagraphFromBuffer(child)));
                }
                else if (isTable)
                {
                    this.Blocks.Add(DocxBlock.FromTable(DocxTableFromBuffer(child)));
                }
                else if (isRun)
                {
                    this.Runs.Add(DocxRunFromBuffer(child));
                }
                else if (isContentControl)
                {
                    DocxContentControl contentControl = DocxContentControlFromBuffer(child);
                    this.ContentControls.Add(contentControl);
                    this.Blocks.Add(DocxBlock.FromContentControl(contentControl));
                }

                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
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

    internal static DocxContentControl DocxContentControlFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return Parse(reader);
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool HasXml()
    {
        return this.Xml != "";
    }

    internal bool HasProperties()
    {
        return this.PropertiesXml != "";
    }

    internal string GetPropertiesXml()
    {
        return this.PropertiesXml;
    }

    internal bool HasAlias()
    {
        return this.Alias != "";
    }

    internal bool HasTag()
    {
        return this.Tag != "";
    }

    internal bool HasType()
    {
        return this.Type != "";
    }

    internal bool HasDataBinding()
    {
        return this.DataBindingStoreItemId != ""
            || this.DataBindingXPath != ""
            || this.DataBindingPrefixMappings != "";
    }

    internal bool HasContent()
    {
        return this.ContentXml != "";
    }

    internal string GetContentXml()
    {
        return this.ContentXml;
    }

    internal int ContentChildCount()
    {
        return this.ContentChildren.Count;
    }

    internal string GetContentChildXml(int index)
    {
        return this.ContentChildren[index].GetXml();
    }

    internal int BlockCount()
    {
        return this.Blocks.Count;
    }

    internal DocxBlock GetBlock(int index)
    {
        return this.Blocks[index];
    }

    internal int ParagraphCount()
    {
        int count = 0;

        foreach (DocxBlock block in this.Blocks)
        {
            if (block.IsParagraph())
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal int TableCount()
    {
        int count = 0;

        foreach (DocxBlock block in this.Blocks)
        {
            if (block.IsTable())
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal int RunCount()
    {
        return this.Runs.Count;
    }

    internal DocxRun GetRun(int index)
    {
        return this.Runs[index];
    }

    internal int ContentControlCount()
    {
        return this.ContentControls.Count;
    }

    internal DocxContentControl GetContentControl(int index)
    {
        return this.ContentControls[index];
    }

    /// <summary>
    /// Builds plain text from the current content in document order.
    /// </summary>
    /// <returns>The resulting text.</returns>
    public string GetText()
    {
        DocxTextOptions options = new DocxTextOptions();
        return GetTextWithOptions(options);
    }

    /// <summary>
    /// Builds plain text from the current content in document order.
    /// </summary>
    /// <param name="options">The options that control this operation.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public string GetTextWithOptions(DocxTextOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string text = "";

        foreach (DocxXmlBuffer child in this.ContentChildren)
        {
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (reader.MoveToContent() && reader.IsElement("p", DocxConstants.NamespaceWordprocessingMl))
            {
                text = text + DocxParagraphFromBuffer(child).GetTextWithOptions(options);
            }
            else if (reader.IsElement("tbl", DocxConstants.NamespaceWordprocessingMl))
            {
                text = text + DocxTableFromBuffer(child).GetTextWithOptions(options);
            }
            else if (reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl))
            {
                text = text + DocxRunFromBuffer(child).GetTextWithOptions(options);
            }
            else if (IsSupported(reader))
            {
                text = text + DocxContentControlFromBuffer(child).GetTextWithOptions(options);
            }
        }

        return text;
    }

    internal string GetValue()
    {
        DocxTextOptions options = new DocxTextOptions();
        return GetValueWithOptions(options);
    }

    internal string GetValueWithOptions(DocxTextOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return GetTextWithOptions(options);
    }

    internal bool HasValue()
    {
        return GetValue() != "";
    }

    internal string GetValueKey()
    {
        if (this.Tag != "")
        {
            return this.Tag;
        }

        return this.Alias;
    }

    internal bool HasValueKey()
    {
        return GetValueKey() != "";
    }

    internal void SetValue(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (this.RunCount() > 0 && this.BlockCount() == 0)
        {
            this.ContentXml = CreateRunValueContentXml(value);
        }
        else
        {
            this.ContentXml = CreateParagraphValueContentXml(value);
        }

        this.Xml = "";
        LoadContentFromXml(this.ContentXml);
    }

    internal int FillContentControls(Dictionary<string, string> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        string key = GetValueKey();

        if (key != "" && values.ContainsKey(key))
        {
            SetValue(values[key]);
            return 1;
        }

        int count = 0;
        List<DocxXmlBuffer> filledChildren = new List<DocxXmlBuffer>();

        foreach (DocxXmlBuffer child in this.ContentChildren)
        {
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (reader.MoveToContent() && IsSupported(reader))
            {
                DocxContentControl contentControl = DocxContentControlFromBuffer(child);
                int filledCount = contentControl.FillContentControls(values);
                count = count + filledCount;

                if (filledCount > 0)
                {
                    filledChildren.Add(new DocxXmlBuffer(contentControl.WriteXml()));
                }
                else
                {
                    filledChildren.Add(child);
                }
            }
            else if (reader.IsElement("p", DocxConstants.NamespaceWordprocessingMl))
            {
                DocxParagraph paragraph = DocxParagraphFromBuffer(child);
                int filledCount = paragraph.FillContentControls(values);
                count = count + filledCount;

                if (filledCount > 0)
                {
                    filledChildren.Add(new DocxXmlBuffer(paragraph.WriteXml()));
                }
                else
                {
                    filledChildren.Add(child);
                }
            }
            else if (reader.IsElement("tbl", DocxConstants.NamespaceWordprocessingMl))
            {
                DocxTable table = DocxTableFromBuffer(child);
                int filledCount = table.FillContentControls(values);
                count = count + filledCount;

                if (filledCount > 0)
                {
                    filledChildren.Add(new DocxXmlBuffer(table.WriteXml()));
                }
                else
                {
                    filledChildren.Add(child);
                }
            }
            else
            {
                filledChildren.Add(child);
            }
        }

        if (count > 0)
        {
            this.ContentXml = CreateContentXmlFromChildren(filledChildren);
            this.Xml = "";
            LoadContentFromXml(this.ContentXml);
        }

        return count;
    }

    internal static string CreateRunValueContentXml(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        DocxRun run = CreateValueRun(value);
        DocxXmlWriter writer = DocxXmlWriter.Create();
        writer.StartElement("w", "sdtContent", DocxConstants.NamespaceWordprocessingMl);
        run.WriteTo(writer);
        writer.EndElement();
        return writer.GetXml();
    }

    internal static string CreateParagraphValueContentXml(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        DocxParagraph paragraph = CreateValueParagraph(value);
        DocxXmlWriter writer = DocxXmlWriter.Create();
        writer.StartElement("w", "sdtContent", DocxConstants.NamespaceWordprocessingMl);
        paragraph.WriteTo(writer);
        writer.EndElement();
        return writer.GetXml();
    }

    internal static string CreateContentXmlFromChildren(List<DocxXmlBuffer> children)
    {
        if (children == null)
        {
            throw new ArgumentNullException("children");
        }

        DocxXmlWriter writer = DocxXmlWriter.Create();
        writer.StartElement("w", "sdtContent", DocxConstants.NamespaceWordprocessingMl);

        foreach (DocxXmlBuffer child in children)
        {
            child.WriteTo(writer);
        }

        writer.EndElement();
        return writer.GetXml();
    }

    internal static DocxRun CreateValueRun(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        DocxRun run = new DocxRun("");
        run.AddText(DocxText.Create(value));
        return run;
    }

    internal static DocxParagraph CreateValueParagraph(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        DocxParagraph paragraph = new DocxParagraph("");
        DocxRun run = CreateValueRun(value);
        paragraph.AddRun(run);
        paragraph.Children.Add(new DocxXmlBuffer(run.WriteXml()));
        return paragraph;
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

        if (this.Xml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.Xml);
            buffer.WriteTo(writer);
            return;
        }

        writer.StartElement("w", "sdt", DocxConstants.NamespaceWordprocessingMl);
        WritePropertiesTo(writer);
        WriteContentTo(writer);
        writer.EndElement();
    }

    internal void WritePropertiesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.PropertiesXml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.PropertiesXml);
            buffer.WriteTo(writer);
        }
    }

    internal void WriteContentTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.ContentXml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.ContentXml);
            buffer.WriteTo(writer);
            return;
        }

        writer.StartElement("w", "sdtContent", DocxConstants.NamespaceWordprocessingMl);

        foreach (DocxXmlBuffer child in this.ContentChildren)
        {
            child.WriteTo(writer);
        }

        writer.EndElement();
    }
}
