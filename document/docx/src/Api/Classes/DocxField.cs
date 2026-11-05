namespace docx;

/// <summary>
/// Represents field data used by a DOCX document.
/// </summary>
public class DocxField : IDocxTextContent
{
    internal string Xml;
    internal string Instruction;
    internal string FieldLock;
    internal string Dirty;
    internal List<DocxXmlBuffer> Children;
    internal List<DocxRun> Runs;

    /// <summary>
    /// Creates a field instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxField(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.Instruction = "";
        this.FieldLock = "";
        this.Dirty = "";
        this.Children = new List<DocxXmlBuffer>();
        this.Runs = new List<DocxRun>();
        LoadFromXml();
    }

    internal static DocxField Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!IsSupported(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on w:fldSimple.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxField(buffer.GetXml());
    }

    internal static DocxField Create(string instruction, string resultText)
    {
        if (instruction == null)
        {
            throw new ArgumentNullException("instruction");
        }

        if (resultText == null)
        {
            throw new ArgumentNullException("resultText");
        }

        if (instruction == "")
        {
            throw new ArgumentException("Field instruction is required.", "instruction");
        }

        DocxField field = new DocxField("");
        field.SetInstruction(instruction);
        field.AddRun(DocxHyperlink.CreateTextRun(resultText));
        return field;
    }

    internal static bool IsSupported(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.IsElement("fldSimple", DocxConstants.NamespaceWordprocessingMl);
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
        this.Instruction = "";
        this.FieldLock = "";
        this.Dirty = "";
        this.Children = new List<DocxXmlBuffer>();
        this.Runs = new List<DocxRun>();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("fldSimple", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        this.Instruction = ReadRequiredAttribute(reader, "instr");
        this.FieldLock = reader.GetAttribute("fldLock", DocxConstants.NamespaceWordprocessingMl);
        this.Dirty = reader.GetAttribute("dirty", DocxConstants.NamespaceWordprocessingMl);

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int fieldDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == fieldDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == fieldDepth + 1)
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

    internal static string ReadRequiredAttribute(DocxXmlReader reader, string localName)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (localName == "")
        {
            throw new ArgumentException("Attribute name is required.", "localName");
        }

        string value = reader.GetAttribute(localName, DocxConstants.NamespaceWordprocessingMl);

        if (value == "")
        {
            throw new InvalidDataException("Simple field is missing required attribute.");
        }

        return value;
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

    internal bool IsLocked()
    {
        if (this.FieldLock == "")
        {
            return false;
        }

        return DocxXmlAttributes.ParseBool(this.FieldLock);
    }

    internal bool IsDirty()
    {
        if (this.Dirty == "")
        {
            return false;
        }

        return DocxXmlAttributes.ParseBool(this.Dirty);
    }

    internal void SetInstruction(string instruction)
    {
        if (instruction == null)
        {
            throw new ArgumentNullException("instruction");
        }

        this.Instruction = instruction;
    }

    internal void SetResultText(string resultText)
    {
        if (resultText == null)
        {
            throw new ArgumentNullException("resultText");
        }

        this.Xml = "";
        this.Children = new List<DocxXmlBuffer>();
        this.Runs = new List<DocxRun>();
        AddRun(DocxHyperlink.CreateTextRun(resultText));
    }

    internal void SetFieldLock(bool locked)
    {
        if (locked)
        {
            this.FieldLock = "1";
        }
        else
        {
            this.FieldLock = "0";
        }
    }

    internal void SetDirty(bool dirty)
    {
        if (dirty)
        {
            this.Dirty = "1";
        }
        else
        {
            this.Dirty = "0";
        }
    }

    internal void ClearFieldLock()
    {
        this.FieldLock = "";
    }

    internal void ClearDirty()
    {
        this.Dirty = "";
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

        if (this.Instruction == "")
        {
            throw new InvalidOperationException("Field instruction is required.");
        }

        writer.StartElement("w", "fldSimple", DocxConstants.NamespaceWordprocessingMl);
        writer.WriteAttribute("w", "instr", DocxConstants.NamespaceWordprocessingMl, this.Instruction);

        if (this.FieldLock != "")
        {
            writer.WriteAttribute("w", "fldLock", DocxConstants.NamespaceWordprocessingMl, this.FieldLock);
        }

        if (this.Dirty != "")
        {
            writer.WriteAttribute("w", "dirty", DocxConstants.NamespaceWordprocessingMl, this.Dirty);
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
