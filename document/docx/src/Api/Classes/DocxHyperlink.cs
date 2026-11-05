namespace docx;

/// <summary>
/// Represents hyperlink data used by a DOCX document.
/// </summary>
public class DocxHyperlink : IDocxTextContent
{
    internal string Xml;
    internal string RelationshipId;
    internal string Anchor;
    internal string History;
    internal string Tooltip;
    internal string Target;
    internal string TargetPartName;
    internal bool IsExternal;
    internal DocxRelationship? Relationship;
    internal List<DocxXmlBuffer> Children;
    internal List<DocxRun> Runs;

    /// <summary>
    /// Creates a hyperlink instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxHyperlink(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.RelationshipId = "";
        this.Anchor = "";
        this.History = "";
        this.Tooltip = "";
        this.Target = "";
        this.TargetPartName = "";
        this.IsExternal = false;
        this.Relationship = null;
        this.Children = new List<DocxXmlBuffer>();
        this.Runs = new List<DocxRun>();
        LoadFromXml();
    }

    internal static DocxHyperlink Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("hyperlink", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:hyperlink.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxHyperlink(buffer.GetXml());
    }

    internal static DocxHyperlink CreateAnchor(string anchor, string displayText)
    {
        if (anchor == null)
        {
            throw new ArgumentNullException("anchor");
        }

        if (displayText == null)
        {
            throw new ArgumentNullException("displayText");
        }

        if (anchor == "")
        {
            throw new ArgumentException("Anchor is required.", "anchor");
        }

        DocxHyperlink hyperlink = new DocxHyperlink("");
        hyperlink.SetAnchor(anchor);
        hyperlink.AddRun(CreateTextRun(displayText));
        return hyperlink;
    }

    internal static DocxHyperlink CreateExternal(DocxRelationshipSet relationships, string url, string displayText)
    {
        if (relationships == null)
        {
            throw new ArgumentNullException("relationships");
        }

        if (url == null)
        {
            throw new ArgumentNullException("url");
        }

        if (displayText == null)
        {
            throw new ArgumentNullException("displayText");
        }

        if (url == "")
        {
            throw new ArgumentException("URL is required.", "url");
        }

        DocxRelationship relationship = relationships.CreateRelationship(
            DocxConstants.RelationshipTypeHyperlink,
            url,
            true);
        DocxHyperlink hyperlink = CreateRelationshipLink(relationship.Id, displayText);
        hyperlink.ResolveRelationship(relationships);
        return hyperlink;
    }

    internal static DocxHyperlink CreateRelationshipLink(string relationshipId, string displayText)
    {
        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        if (displayText == null)
        {
            throw new ArgumentNullException("displayText");
        }

        if (relationshipId == "")
        {
            throw new ArgumentException("Relationship id is required.", "relationshipId");
        }

        DocxHyperlink hyperlink = new DocxHyperlink("");
        hyperlink.SetRelationshipId(relationshipId);
        hyperlink.AddRun(CreateTextRun(displayText));
        return hyperlink;
    }

    internal static DocxRun CreateTextRun(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        DocxRun run = new DocxRun("");
        run.AddText(DocxText.Create(text));
        return run;
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
        this.RelationshipId = "";
        this.Anchor = "";
        this.History = "";
        this.Tooltip = "";
        this.Target = "";
        this.TargetPartName = "";
        this.IsExternal = false;
        this.Relationship = null;
        this.Children = new List<DocxXmlBuffer>();
        this.Runs = new List<DocxRun>();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("hyperlink", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        this.RelationshipId = reader.GetAttribute("id", DocxConstants.NamespaceRelationships);
        this.Anchor = reader.GetAttribute("anchor", DocxConstants.NamespaceWordprocessingMl);
        this.History = reader.GetAttribute("history", DocxConstants.NamespaceWordprocessingMl);
        this.Tooltip = reader.GetAttribute("tooltip", DocxConstants.NamespaceWordprocessingMl);

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int hyperlinkDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == hyperlinkDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == hyperlinkDepth + 1)
            {
                bool isRun = reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl);
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddChild(child);

                if (isRun)
                {
                    this.Runs.Add(DocxRunFromBuffer(child));
                }

                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void AddChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        this.Children.Add(child);
    }

    internal void AddRun(DocxRun run)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        this.Runs.Add(run);
        this.Children.Add(new DocxXmlBuffer(run.WriteXml()));
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

    internal int ChildCount()
    {
        return this.Children.Count;
    }

    internal string GetChildXml(int index)
    {
        return this.Children[index].GetXml();
    }

    internal int RunCount()
    {
        return this.Runs.Count;
    }

    internal DocxRun GetRun(int index)
    {
        return this.Runs[index];
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

        foreach (DocxRun run in this.Runs)
        {
            text = text + run.GetTextWithOptions(options);
        }

        return text;
    }

    internal int AcceptRevisions()
    {
        string xml = WriteXml();
        int count = DocxRevision.CountRevisionElementsXml(xml);

        if (count == 0)
        {
            return 0;
        }

        this.Xml = DocxRevision.AcceptRevisionXml(xml);
        LoadFromXml();
        return count;
    }

    internal int RejectRevisions()
    {
        string xml = WriteXml();
        int count = DocxRevision.CountRevisionElementsXml(xml);

        if (count == 0)
        {
            return 0;
        }

        this.Xml = DocxRevision.RejectRevisionXml(xml);
        LoadFromXml();
        return count;
    }

    internal void SetRelationshipId(string relationshipId)
    {
        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        this.RelationshipId = relationshipId;
    }

    internal void SetAnchor(string anchor)
    {
        if (anchor == null)
        {
            throw new ArgumentNullException("anchor");
        }

        this.Anchor = anchor;
    }

    internal void SetHistory(string history)
    {
        if (history == null)
        {
            throw new ArgumentNullException("history");
        }

        this.History = history;
    }

    internal void SetTooltip(string tooltip)
    {
        if (tooltip == null)
        {
            throw new ArgumentNullException("tooltip");
        }

        this.Tooltip = tooltip;
    }

    internal bool ResolveRelationship(DocxRelationshipSet relationships)
    {
        if (relationships == null)
        {
            throw new ArgumentNullException("relationships");
        }

        this.Target = "";
        this.TargetPartName = "";
        this.IsExternal = false;
        this.Relationship = null;

        if (this.RelationshipId == "")
        {
            return false;
        }

        DocxRelationship? relationship = relationships.FindById(this.RelationshipId);

        if (relationship == null)
        {
            return false;
        }

        if (relationship.Type != DocxConstants.RelationshipTypeHyperlink)
        {
            return false;
        }

        this.Relationship = relationship;
        this.Target = relationship.Target;
        this.IsExternal = relationship.IsExternal;

        if (!relationship.IsExternal)
        {
            this.TargetPartName = relationships.ResolveTargetPartName(relationship);
        }

        return true;
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

        writer.StartElement("w", "hyperlink", DocxConstants.NamespaceWordprocessingMl);

        if (this.RelationshipId != "")
        {
            writer.WriteAttribute("r", "id", DocxConstants.NamespaceRelationships, this.RelationshipId);
        }

        if (this.Anchor != "")
        {
            writer.WriteAttribute("w", "anchor", DocxConstants.NamespaceWordprocessingMl, this.Anchor);
        }

        if (this.History != "")
        {
            writer.WriteAttribute("w", "history", DocxConstants.NamespaceWordprocessingMl, this.History);
        }

        if (this.Tooltip != "")
        {
            writer.WriteAttribute("w", "tooltip", DocxConstants.NamespaceWordprocessingMl, this.Tooltip);
        }

        WriteChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxXmlBuffer child in this.Children)
        {
            child.WriteTo(writer);
        }
    }
}
