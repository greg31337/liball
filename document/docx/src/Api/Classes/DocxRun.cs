using System.Xml;

namespace docx;

/// <summary>
/// Represents a run of text or other inline content that shares formatting.
/// </summary>
public class DocxRun : IDocxTextContent, IDocxRunModel
{
    internal string Xml;
    internal string PropertiesXml;
    internal List<DocxXmlBuffer> Children;
    internal List<DocxText> Texts;
    internal List<DocxBreak> Breaks;
    internal List<DocxComplexFieldElement> ComplexFieldElements;
    internal List<DocxUnknownElement> UnknownElements;
    internal List<DocxRevision> Revisions;
    internal List<DocxDrawing> Drawings;
    internal DocxRunFormat Format;
    internal DocxParagraph? ParentParagraph;
    internal int ParentRunIndex;
    internal string ParentXml;

    /// <summary>
    /// Creates a run instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxRun(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.PropertiesXml = "";
        this.Children = new List<DocxXmlBuffer>();
        this.Texts = new List<DocxText>();
        this.Breaks = new List<DocxBreak>();
        this.ComplexFieldElements = new List<DocxComplexFieldElement>();
        this.UnknownElements = new List<DocxUnknownElement>();
        this.Revisions = new List<DocxRevision>();
        this.Drawings = new List<DocxDrawing>();
        this.Format = new DocxRunFormat();
        this.ParentParagraph = null;
        this.ParentRunIndex = -1;
        this.ParentXml = "";
        LoadFromXml();
    }

    internal static DocxRun Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:r.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxRun(buffer.GetXml());
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
        this.PropertiesXml = "";
        this.Children = new List<DocxXmlBuffer>();
        this.Texts = new List<DocxText>();
        this.Breaks = new List<DocxBreak>();
        this.ComplexFieldElements = new List<DocxComplexFieldElement>();
        this.UnknownElements = new List<DocxUnknownElement>();
        this.Revisions = new List<DocxRevision>();
        this.Drawings = new List<DocxDrawing>();
        this.Format = new DocxRunFormat();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("r", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int runDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == runDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == runDepth + 1)
            {
                bool isText = reader.IsElement("t", DocxConstants.NamespaceWordprocessingMl);
                bool isBreak = DocxBreak.IsSupported(reader);
                bool isComplexFieldElement = DocxComplexFieldElement.IsSupported(reader);
                bool isDrawing = DocxDrawing.IsSupported(reader);
                bool isRevision = DocxRevision.IsSupported(reader);
                bool isUnknown = !isText && !isBreak && !isComplexFieldElement;

                if (reader.IsElement("rPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    DocxXmlBuffer properties = DocxXmlBuffer.Capture(reader);
                    this.PropertiesXml = properties.GetXml();
                    this.Format = DocxRunFormat.ParseXml(this.PropertiesXml);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddChild(child);

                if (isText)
                {
                    this.Texts.Add(DocxTextFromBuffer(child));
                }

                if (isBreak)
                {
                    this.Breaks.Add(DocxBreakFromBuffer(child));
                }

                if (isComplexFieldElement)
                {
                    this.ComplexFieldElements.Add(DocxComplexFieldElementFromBuffer(child));
                }

                if (isDrawing)
                {
                    this.Drawings.Add(DocxDrawingFromBuffer(child));
                }

                if (isRevision)
                {
                    this.Revisions.Add(DocxRevisionFromBuffer(child));
                }

                if (isUnknown)
                {
                    this.UnknownElements.Add(DocxUnknownElementFromBuffer(child));
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

    internal void AddText(DocxText text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        this.Texts.Add(text);
        this.Children.Add(new DocxXmlBuffer(text.WriteXml()));
    }

    internal void AddBreak(DocxBreak value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Breaks.Add(value);
        this.Children.Add(new DocxXmlBuffer(value.WriteXml()));
    }

    internal void AddComplexFieldElement(DocxComplexFieldElement value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.ComplexFieldElements.Add(value);
        this.Children.Add(new DocxXmlBuffer(value.WriteXml()));
    }

    internal void AddRevision(DocxRevision revision)
    {
        if (revision == null)
        {
            throw new ArgumentNullException("revision");
        }

        string xml = revision.WriteXml();
        this.Revisions.Add(revision);
        this.UnknownElements.Add(DocxUnknownElement.ParseXml(xml));
        this.Children.Add(new DocxXmlBuffer(xml));
    }

    internal void AddUnknownElement(DocxUnknownElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        this.UnknownElements.Add(element);
        this.Children.Add(new DocxXmlBuffer(element.WriteXml()));
    }

    internal void AddDrawing(DocxDrawing drawing)
    {
        if (drawing == null)
        {
            throw new ArgumentNullException("drawing");
        }

        string xml = drawing.WriteXml();
        this.Drawings.Add(drawing);
        this.UnknownElements.Add(DocxUnknownElement.ParseXml(xml));
        this.Children.Add(new DocxXmlBuffer(xml));
    }

    internal static DocxText DocxTextFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        return DocxText.ParseXml(buffer.GetXml());
    }

    internal static DocxBreak DocxBreakFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        return DocxBreak.ParseXml(buffer.GetXml());
    }

    internal static DocxComplexFieldElement DocxComplexFieldElementFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        return DocxComplexFieldElement.ParseXml(buffer.GetXml());
    }

    internal static DocxUnknownElement DocxUnknownElementFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        return DocxUnknownElement.ParseXml(buffer.GetXml());
    }

    internal static DocxRevision DocxRevisionFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        return DocxRevision.ParseXml(buffer.GetXml());
    }

    internal static DocxDrawing DocxDrawingFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        return DocxDrawing.ParseXml(buffer.GetXml());
    }

    internal void SetPropertiesXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.PropertiesXml = xml;
        this.Format = DocxRunFormat.ParseXml(xml);
    }

    /// <summary>
    /// Replaces the direct run format model.
    /// </summary>
    /// <param name="format">The replacement run format.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>format</c> is null.</exception>
    public void SetFormat(DocxRunFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        this.Format = format;
        this.PropertiesXml = "";
        NotifyParentParagraph();
    }

    internal bool HasProperties()
    {
        return this.PropertiesXml != "";
    }

    internal string GetPropertiesXml()
    {
        return this.PropertiesXml;
    }

    /// <summary>
    /// Returns whether direct run formatting is present.
    /// </summary>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    public bool HasFormat()
    {
        return this.Format.HasAnyFormatting();
    }

    /// <summary>
    /// Returns the direct run format model.
    /// </summary>
    /// <returns>The run format model.</returns>
    public DocxRunFormat GetFormat()
    {
        return this.Format;
    }

    /// <summary>
    /// Returns all direct run children in document order.
    /// </summary>
    /// <returns>A new list containing the run children.</returns>
    public List<DocxRunChild> GetChildren()
    {
        List<DocxRunChild> children = new List<DocxRunChild>();

        foreach (DocxXmlBuffer child in this.Children)
        {
            children.Add(new DocxRunChild(child.GetXml()));
        }

        return children;
    }

    /// <summary>
    /// Returns the number of direct run children.
    /// </summary>
    /// <returns>The child count.</returns>
    public int ChildCount()
    {
        return this.Children.Count;
    }

    /// <summary>
    /// Returns a direct run child by position.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <returns>The requested child.</returns>
    public DocxRunChild GetChild(int index)
    {
        return new DocxRunChild(this.Children[index].GetXml());
    }

    /// <summary>
    /// Appends a direct run child.
    /// </summary>
    /// <param name="child">The child to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>child</c> is null.</exception>
    public void AddRunChild(DocxRunChild child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        List<DocxRunChild> children = GetChildren();
        children.Add(child);
        ApplyRunChildren(children);
    }

    /// <summary>
    /// Inserts a direct run child.
    /// </summary>
    /// <param name="index">The zero-based insertion position.</param>
    /// <param name="child">The child to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>child</c> is null.</exception>
    public void InsertRunChild(int index, DocxRunChild child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        List<DocxRunChild> children = GetChildren();
        children.Insert(index, child);
        ApplyRunChildren(children);
    }

    /// <summary>
    /// Inserts a text node at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="text">The text node to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    public void InsertTextNode(int index, DocxText text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        InsertRunChild(index, DocxRunChild.FromText(text));
    }

    /// <summary>
    /// Inserts a run control at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="value">The tab, carriage return, break, hyphen, or symbol to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    public void InsertBreak(int index, DocxBreak value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        InsertRunChild(index, DocxRunChild.FromBreak(value));
    }

    /// <summary>
    /// Inserts a drawing at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="drawing">The drawing to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>drawing</c> is null.</exception>
    public void InsertDrawing(int index, DocxDrawing drawing)
    {
        if (drawing == null)
        {
            throw new ArgumentNullException("drawing");
        }

        InsertRunChild(index, DocxRunChild.FromDrawing(drawing));
    }

    /// <summary>
    /// Replaces a direct run child.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="child">The replacement child.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>child</c> is null.</exception>
    public DocxRunChild ReplaceRunChild(int index, DocxRunChild child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        List<DocxRunChild> children = GetChildren();
        DocxRunChild replaced = children[index];
        children[index] = child;
        ApplyRunChildren(children);
        return replaced;
    }

    /// <summary>
    /// Replaces a direct run child with a text node.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="text">The replacement text node.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    public DocxRunChild ReplaceChildWithText(int index, DocxText text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return ReplaceRunChild(index, DocxRunChild.FromText(text));
    }

    /// <summary>
    /// Replaces a direct run child with a run control.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="value">The replacement tab, carriage return, break, hyphen, or symbol.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    public DocxRunChild ReplaceChildWithBreak(int index, DocxBreak value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return ReplaceRunChild(index, DocxRunChild.FromBreak(value));
    }

    /// <summary>
    /// Replaces a direct run child with a drawing.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="drawing">The replacement drawing.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>drawing</c> is null.</exception>
    public DocxRunChild ReplaceChildWithDrawing(int index, DocxDrawing drawing)
    {
        if (drawing == null)
        {
            throw new ArgumentNullException("drawing");
        }

        return ReplaceRunChild(index, DocxRunChild.FromDrawing(drawing));
    }

    /// <summary>
    /// Moves a direct run child to another position.
    /// </summary>
    /// <param name="sourceIndex">The current zero-based child position.</param>
    /// <param name="destinationIndex">The resulting zero-based child position.</param>
    public void MoveRunChild(int sourceIndex, int destinationIndex)
    {
        List<DocxRunChild> children = GetChildren();
        DocxRunChild child = children[sourceIndex];

        if (destinationIndex < 0 || destinationIndex >= children.Count)
        {
            throw new ArgumentOutOfRangeException("destinationIndex");
        }

        if (sourceIndex == destinationIndex)
        {
            return;
        }

        children.RemoveAt(sourceIndex);
        children.Insert(destinationIndex, child);
        ApplyRunChildren(children);
    }

    /// <summary>
    /// Removes a direct run child.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <returns>The removed child.</returns>
    public DocxRunChild RemoveRunChildAt(int index)
    {
        List<DocxRunChild> children = GetChildren();
        DocxRunChild removed = children[index];
        children.RemoveAt(index);
        ApplyRunChildren(children);
        return removed;
    }

    internal void ApplyRunChildren(List<DocxRunChild> children)
    {
        if (children == null)
        {
            throw new ArgumentNullException("children");
        }

        this.Children = new List<DocxXmlBuffer>();

        foreach (DocxRunChild child in children)
        {
            this.Children.Add(new DocxXmlBuffer(child.WriteXml()));
        }

        this.Xml = WriteXml();
        LoadFromXml();
        NotifyParentParagraph();
    }

    internal void NotifyParentParagraph()
    {
        if (this.ParentParagraph != null)
        {
            this.ParentParagraph.SynchronizeRun(this);
        }
    }

    internal List<DocxRunChild> GetChildrenByKind(DocxRunChildKind kind)
    {
        List<DocxRunChild> matching = new List<DocxRunChild>();

        foreach (DocxRunChild child in GetChildren())
        {
            if (child.Kind == kind)
            {
                matching.Add(child);
            }
        }

        return matching;
    }

    internal string GetChildXml(int index)
    {
        return this.Children[index].GetXml();
    }

    /// <summary>
    /// Returns direct tab children in document order.
    /// </summary>
    /// <returns>A new list containing the tabs.</returns>
    public List<DocxRunChild> GetTabs()
    {
        return GetChildrenByKind(DocxRunChildKind.Tab);
    }

    /// <summary>
    /// Returns the number of direct tab children.
    /// </summary>
    /// <returns>The tab count.</returns>
    public int TabCount()
    {
        return GetTabs().Count;
    }

    /// <summary>
    /// Returns a direct tab child by position.
    /// </summary>
    /// <param name="index">The zero-based tab position.</param>
    /// <returns>The requested tab.</returns>
    public DocxRunChild GetTab(int index)
    {
        return GetTabs()[index];
    }

    /// <summary>
    /// Returns direct carriage-return children in document order.
    /// </summary>
    /// <returns>A new list containing the carriage returns.</returns>
    public List<DocxRunChild> GetCarriageReturns()
    {
        return GetChildrenByKind(DocxRunChildKind.CarriageReturn);
    }

    /// <summary>
    /// Returns the number of direct carriage-return children.
    /// </summary>
    /// <returns>The carriage-return count.</returns>
    public int CarriageReturnCount()
    {
        return GetCarriageReturns().Count;
    }

    /// <summary>
    /// Returns a direct carriage-return child by position.
    /// </summary>
    /// <param name="index">The zero-based carriage-return position.</param>
    /// <returns>The requested carriage return.</returns>
    public DocxRunChild GetCarriageReturn(int index)
    {
        return GetCarriageReturns()[index];
    }

    /// <summary>
    /// Returns direct line, page, and column break children in document order.
    /// </summary>
    /// <returns>A new list containing the break elements.</returns>
    public List<DocxRunChild> GetBreakElements()
    {
        return GetChildrenByKind(DocxRunChildKind.Break);
    }

    /// <summary>
    /// Returns the number of direct line, page, and column break children.
    /// </summary>
    /// <returns>The break-element count.</returns>
    public int BreakElementCount()
    {
        return GetBreakElements().Count;
    }

    /// <summary>
    /// Returns a direct line, page, or column break child by position.
    /// </summary>
    /// <param name="index">The zero-based break-element position.</param>
    /// <returns>The requested break element.</returns>
    public DocxRunChild GetBreakElement(int index)
    {
        return GetBreakElements()[index];
    }

    /// <summary>
    /// Returns direct soft and non-breaking hyphen children in document order.
    /// </summary>
    /// <returns>A new list containing the hyphen controls.</returns>
    public List<DocxRunChild> GetHyphens()
    {
        return GetChildrenByKind(DocxRunChildKind.Hyphen);
    }

    /// <summary>
    /// Returns the number of direct hyphen controls.
    /// </summary>
    /// <returns>The hyphen-control count.</returns>
    public int HyphenCount()
    {
        return GetHyphens().Count;
    }

    /// <summary>
    /// Returns a direct hyphen control by position.
    /// </summary>
    /// <param name="index">The zero-based hyphen-control position.</param>
    /// <returns>The requested hyphen control.</returns>
    public DocxRunChild GetHyphen(int index)
    {
        return GetHyphens()[index];
    }

    /// <summary>
    /// Returns direct symbol children in document order.
    /// </summary>
    /// <returns>A new list containing the symbols.</returns>
    public List<DocxRunChild> GetSymbols()
    {
        return GetChildrenByKind(DocxRunChildKind.Symbol);
    }

    /// <summary>
    /// Returns the number of direct symbol children.
    /// </summary>
    /// <returns>The symbol count.</returns>
    public int SymbolCount()
    {
        return GetSymbols().Count;
    }

    /// <summary>
    /// Returns a direct symbol child by position.
    /// </summary>
    /// <param name="index">The zero-based symbol position.</param>
    /// <returns>The requested symbol.</returns>
    public DocxRunChild GetSymbol(int index)
    {
        return GetSymbols()[index];
    }

    /// <summary>
    /// Returns direct complex-field character children in document order.
    /// </summary>
    /// <returns>A new list containing the field characters.</returns>
    public List<DocxRunChild> GetFieldCharacters()
    {
        return GetChildrenByKind(DocxRunChildKind.FieldCharacter);
    }

    /// <summary>
    /// Returns the number of direct complex-field character children.
    /// </summary>
    /// <returns>The field-character count.</returns>
    public int FieldCharacterCount()
    {
        return GetFieldCharacters().Count;
    }

    /// <summary>
    /// Returns a direct complex-field character by position.
    /// </summary>
    /// <param name="index">The zero-based field-character position.</param>
    /// <returns>The requested field character.</returns>
    public DocxRunChild GetFieldCharacter(int index)
    {
        return GetFieldCharacters()[index];
    }

    /// <summary>
    /// Returns direct complex-field instruction children in document order.
    /// </summary>
    /// <returns>A new list containing the field instructions.</returns>
    public List<DocxRunChild> GetFieldInstructions()
    {
        return GetChildrenByKind(DocxRunChildKind.FieldInstruction);
    }

    /// <summary>
    /// Returns the number of direct complex-field instruction children.
    /// </summary>
    /// <returns>The field-instruction count.</returns>
    public int FieldInstructionCount()
    {
        return GetFieldInstructions().Count;
    }

    /// <summary>
    /// Returns a direct complex-field instruction by position.
    /// </summary>
    /// <param name="index">The zero-based field-instruction position.</param>
    /// <returns>The requested field instruction.</returns>
    public DocxRunChild GetFieldInstruction(int index)
    {
        return GetFieldInstructions()[index];
    }

    /// <summary>
    /// Returns preserved direct children not represented by another child kind.
    /// </summary>
    /// <returns>A new list containing the preserved unknown children.</returns>
    public List<DocxRunChild> GetPreservedUnknownChildren()
    {
        return GetChildrenByKind(DocxRunChildKind.PreservedUnknown);
    }

    /// <summary>
    /// Returns the number of preserved direct children not represented by another kind.
    /// </summary>
    /// <returns>The preserved unknown child count.</returns>
    public int PreservedUnknownChildCount()
    {
        return GetPreservedUnknownChildren().Count;
    }

    /// <summary>
    /// Returns a preserved unknown child by position.
    /// </summary>
    /// <param name="index">The zero-based preserved unknown child position.</param>
    /// <returns>The requested preserved unknown child.</returns>
    public DocxRunChild GetPreservedUnknownChild(int index)
    {
        return GetPreservedUnknownChildren()[index];
    }

    internal int CountChildElements(string localName, string namespaceUri)
    {
        DocxXmlReader.ValidateName(localName, namespaceUri);

        int count = 0;

        foreach (DocxXmlBuffer child in this.Children)
        {
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (reader.MoveToContent() && reader.IsElement(localName, namespaceUri))
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal int TextElementCount()
    {
        return CountChildElements("t", DocxConstants.NamespaceWordprocessingMl);
    }

    /// <summary>
    /// Returns all direct text nodes in document order.
    /// </summary>
    /// <returns>A new list containing the text nodes.</returns>
    public List<DocxText> GetTextNodes()
    {
        return new List<DocxText>(this.Texts);
    }

    /// <summary>
    /// Returns the number of direct text nodes.
    /// </summary>
    /// <returns>The text-node count.</returns>
    public int TextCount()
    {
        return this.Texts.Count;
    }

    /// <summary>
    /// Returns a text node by position.
    /// </summary>
    /// <param name="index">The zero-based text-node position.</param>
    /// <returns>The requested text node.</returns>
    public DocxText GetTextNode(int index)
    {
        return this.Texts[index];
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

        foreach (DocxXmlBuffer child in this.Children)
        {
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (reader.MoveToContent() && IsTextExtractionElement(reader))
            {
                text = text + ReadTextExtractionElement(child);
            }
            else if (DocxBreak.IsSupported(reader))
            {
                text = text + DocxBreakFromBuffer(child).GetTextWithOptions(options);
            }
            else if (DocxRevision.IsSupported(reader))
            {
                text = text + DocxRevisionFromBuffer(child).GetTextWithOptions(options);
            }
        }

        return text;
    }

    internal int ReplaceText(string searchText, string replacementText)
    {
        return DocxTextReplacer.Replace(this, searchText, replacementText);
    }

    internal static bool IsTextExtractionElement(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.NamespaceUri() == DocxConstants.NamespaceWordprocessingMl
            && (reader.LocalName() == "t" || reader.LocalName() == "delText");
    }

    internal static string ReadTextExtractionElement(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());

        if (!reader.MoveToContent() || !IsTextExtractionElement(reader))
        {
            throw new InvalidOperationException("XML does not contain an extractable text element.");
        }

        return reader.Reader.ReadElementContentAsString();
    }

    /// <summary>
    /// Returns all parsed run controls in document order.
    /// </summary>
    /// <returns>A new list containing tabs, carriage returns, breaks, hyphens, and symbols.</returns>
    public List<DocxBreak> GetBreaks()
    {
        return new List<DocxBreak>(this.Breaks);
    }

    /// <summary>
    /// Returns the number of parsed run controls.
    /// </summary>
    /// <returns>The run-control count.</returns>
    public int BreakCount()
    {
        return this.Breaks.Count;
    }

    /// <summary>
    /// Returns a parsed run control by position.
    /// </summary>
    /// <param name="index">The zero-based run-control position.</param>
    /// <returns>The requested run control.</returns>
    public DocxBreak GetBreak(int index)
    {
        return this.Breaks[index];
    }

    internal int ComplexFieldElementCount()
    {
        return this.ComplexFieldElements.Count;
    }

    internal DocxComplexFieldElement GetComplexFieldElement(int index)
    {
        return this.ComplexFieldElements[index];
    }

    internal string GetComplexFieldInstructionText()
    {
        string text = "";

        foreach (DocxComplexFieldElement value in this.ComplexFieldElements)
        {
            if (value.IsInstructionText())
            {
                text = text + value.InstructionText;
            }
        }

        return text;
    }

    internal int UnknownElementCount()
    {
        return this.UnknownElements.Count;
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

    internal int AcceptRevisions()
    {
        string xml = WriteXml();
        int count = DocxRevision.CountRevisionElementsXml(xml);

        if (count == 0)
        {
            return 0;
        }

        this.Xml = FlattenNestedRunXml(DocxRevision.AcceptRevisionXml(xml));
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

        this.Xml = FlattenNestedRunXml(DocxRevision.RejectRevisionXml(xml));
        LoadFromXml();
        return count;
    }

    internal static string FlattenNestedRunXml(string xml)
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
        XmlWriter writer = XmlWriter.Create(stringWriter, settings);
        WriteFlattenedRunElement(document.DocumentElement, writer);
        writer.Flush();
        return stringWriter.ToString();
    }

    internal static void WriteFlattenedRunElement(XmlElement element, XmlWriter writer)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.WriteStartElement(element.Prefix, element.LocalName, element.NamespaceURI);

        foreach (XmlAttribute attribute in element.Attributes)
        {
            writer.WriteAttributeString(attribute.Prefix, attribute.LocalName, attribute.NamespaceURI, attribute.Value);
        }

        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element && IsNestedRunElement((XmlElement)child))
            {
                foreach (XmlNode nestedChild in child.ChildNodes)
                {
                    DocxRevision.WriteAcceptedNode(nestedChild, writer);
                }
            }
            else
            {
                DocxRevision.WriteAcceptedNode(child, writer);
            }
        }

        writer.WriteEndElement();
    }

    internal static bool IsNestedRunElement(XmlElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        return element.LocalName == "r" && element.NamespaceURI == DocxConstants.NamespaceWordprocessingMl;
    }

    internal DocxUnknownElement GetUnknownElement(int index)
    {
        return this.UnknownElements[index];
    }

    internal int CountUnknownElements(string localName, string namespaceUri)
    {
        DocxXmlReader.ValidateName(localName, namespaceUri);

        int count = 0;

        foreach (DocxUnknownElement element in this.UnknownElements)
        {
            if (element.IsElement(localName, namespaceUri))
            {
                count = count + 1;
            }
        }

        return count;
    }

    /// <summary>
    /// Returns all direct drawings in document order.
    /// </summary>
    /// <returns>A new list containing the drawings.</returns>
    public List<DocxDrawing> GetDrawings()
    {
        return new List<DocxDrawing>(this.Drawings);
    }

    /// <summary>
    /// Returns the number of direct drawings.
    /// </summary>
    /// <returns>The drawing count.</returns>
    public int DrawingCount()
    {
        return this.Drawings.Count;
    }

    /// <summary>
    /// Returns a drawing by position.
    /// </summary>
    /// <param name="index">The zero-based drawing position.</param>
    /// <returns>The requested drawing.</returns>
    public DocxDrawing GetDrawing(int index)
    {
        return this.Drawings[index];
    }

    internal int CountDrawings(string kind)
    {
        if (kind == null)
        {
            throw new ArgumentNullException("kind");
        }

        if (kind == "")
        {
            throw new ArgumentException("Drawing kind is required.", "kind");
        }

        int count = 0;

        foreach (DocxDrawing drawing in this.Drawings)
        {
            if (drawing.IsKind(kind))
            {
                count = count + 1;
            }
        }

        return count;
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

        writer.StartElement("w", "r", DocxConstants.NamespaceWordprocessingMl);
        WritePropertiesTo(writer);
        WriteChildrenTo(writer);
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
            return;
        }

        if (this.Format.HasAnyFormatting())
        {
            this.Format.WriteTo(writer);
        }
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
