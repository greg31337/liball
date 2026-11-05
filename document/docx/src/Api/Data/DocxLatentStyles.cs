namespace docx;

/// <summary>
/// Represents latent styles data used by a DOCX document.
/// </summary>
public class DocxLatentStyles
{
    internal string Xml;
    internal List<DocxXmlBuffer> Children;
    internal List<DocxLatentStyleException> Exceptions = new List<DocxLatentStyleException>();
    /// <summary>
    /// Whether default locked state was explicitly supplied.
    /// </summary>
    public bool HasDefaultLockedState;
    /// <summary>
    /// Whether the default locked state behavior is enabled.
    /// </summary>
    public bool DefaultLockedState;
    /// <summary>
    /// The default ui priority value stored in the document model.
    /// </summary>
    public string DefaultUiPriority = "";
    /// <summary>
    /// Whether default semi hidden was explicitly supplied.
    /// </summary>
    public bool HasDefaultSemiHidden;
    /// <summary>
    /// Whether the default semi hidden behavior is enabled.
    /// </summary>
    public bool DefaultSemiHidden;
    /// <summary>
    /// Whether default unhide when used was explicitly supplied.
    /// </summary>
    public bool HasDefaultUnhideWhenUsed;
    /// <summary>
    /// Whether the default unhide when used behavior is enabled.
    /// </summary>
    public bool DefaultUnhideWhenUsed;
    /// <summary>
    /// Whether default quick format was explicitly supplied.
    /// </summary>
    public bool HasDefaultQuickFormat;
    /// <summary>
    /// Whether the default quick format behavior is enabled.
    /// </summary>
    public bool DefaultQuickFormat;
    /// <summary>
    /// The count value stored in the document model.
    /// </summary>
    public string Count = "";

    /// <summary>
    /// Creates a latent styles instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxLatentStyles(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.Children = new List<DocxXmlBuffer>();
        InitializeParsedState();
        LoadFromXml();
    }

    internal static DocxLatentStyles Create()
    {
        return new DocxLatentStyles("");
    }

    internal static DocxLatentStyles Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("latentStyles", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:latentStyles.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return ParseXml(buffer.GetXml());
    }

    internal static DocxLatentStyles ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        return new DocxLatentStyles(xml);
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
        this.Children = new List<DocxXmlBuffer>();
        InitializeParsedState();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("latentStyles", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        ReadAttributes(reader);

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int latentDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == latentDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == latentDepth + 1)
            {
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddChild(child);

                if (IsLatentStyleException(child))
                {
                    AddException(DocxLatentStyleExceptionFromBuffer(child));
                }

                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void InitializeParsedState()
    {
        this.Exceptions = new List<DocxLatentStyleException>();
        this.HasDefaultLockedState = false;
        this.DefaultLockedState = false;
        this.DefaultUiPriority = "";
        this.HasDefaultSemiHidden = false;
        this.DefaultSemiHidden = false;
        this.HasDefaultUnhideWhenUsed = false;
        this.DefaultUnhideWhenUsed = false;
        this.HasDefaultQuickFormat = false;
        this.DefaultQuickFormat = false;
        this.Count = "";
    }

    internal void ReadAttributes(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("latentStyles", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:latentStyles.");
        }

        this.DefaultUiPriority = reader.GetAttribute("defUIPriority", DocxConstants.NamespaceWordprocessingMl);
        this.Count = reader.GetAttribute("count", DocxConstants.NamespaceWordprocessingMl);
        ReadDefaultLockedStateAttribute(reader);
        ReadDefaultSemiHiddenAttribute(reader);
        ReadDefaultUnhideWhenUsedAttribute(reader);
        ReadDefaultQuickFormatAttribute(reader);
    }

    internal void ReadDefaultLockedStateAttribute(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("defLockedState", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasDefaultLockedState = true;
            this.DefaultLockedState = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadDefaultSemiHiddenAttribute(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("defSemiHidden", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasDefaultSemiHidden = true;
            this.DefaultSemiHidden = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadDefaultUnhideWhenUsedAttribute(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("defUnhideWhenUsed", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasDefaultUnhideWhenUsed = true;
            this.DefaultUnhideWhenUsed = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadDefaultQuickFormatAttribute(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("defQFormat", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasDefaultQuickFormat = true;
            this.DefaultQuickFormat = DocxXmlAttributes.ParseBool(value);
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

    internal void AddException(DocxLatentStyleException exception)
    {
        if (exception == null)
        {
            throw new ArgumentNullException("exception");
        }

        this.Exceptions.Add(exception);
    }

    internal void ReplaceException(DocxLatentStyleException exception)
    {
        if (exception == null)
        {
            throw new ArgumentNullException("exception");
        }

        for (int index = 0; index < this.Exceptions.Count; index = index + 1)
        {
            if (this.Exceptions[index].Name == exception.Name)
            {
                this.Exceptions[index] = exception;
                ClearRawXml();
                return;
            }
        }

        this.Exceptions.Add(exception);
        ClearRawXml();
    }

    internal bool RemoveException(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        for (int index = 0; index < this.Exceptions.Count; index = index + 1)
        {
            if (this.Exceptions[index].Name == name)
            {
                this.Exceptions.RemoveAt(index);
                ClearRawXml();
                return true;
            }
        }

        return false;
    }

    internal int ChildCount()
    {
        return this.Children.Count;
    }

    internal string GetChildXml(int index)
    {
        return this.Children[index].GetXml();
    }

    internal int ExceptionCount()
    {
        return this.Exceptions.Count;
    }

    internal DocxLatentStyleException GetException(int index)
    {
        return this.Exceptions[index];
    }

    internal bool HasAnySettings()
    {
        return this.HasDefaultLockedState
            || this.DefaultUiPriority != ""
            || this.HasDefaultSemiHidden
            || this.HasDefaultUnhideWhenUsed
            || this.HasDefaultQuickFormat
            || this.Count != ""
            || this.Exceptions.Count > 0;
    }

    internal static bool IsLatentStyleException(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

        if (!reader.MoveToContent())
        {
            return false;
        }

        return reader.IsElement("lsdException", DocxConstants.NamespaceWordprocessingMl);
    }

    internal static DocxLatentStyleException DocxLatentStyleExceptionFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxLatentStyleException.Parse(reader);
    }

    internal void ClearRawXml()
    {
        this.Xml = "";
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

        writer.StartElement("w", "latentStyles", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalBoolAttribute(writer, "defLockedState", this.HasDefaultLockedState, this.DefaultLockedState);
        WriteOptionalAttribute(writer, "defUIPriority", this.DefaultUiPriority);
        WriteOptionalBoolAttribute(writer, "defSemiHidden", this.HasDefaultSemiHidden, this.DefaultSemiHidden);
        WriteOptionalBoolAttribute(writer, "defUnhideWhenUsed", this.HasDefaultUnhideWhenUsed, this.DefaultUnhideWhenUsed);
        WriteOptionalBoolAttribute(writer, "defQFormat", this.HasDefaultQuickFormat, this.DefaultQuickFormat);
        WriteOptionalAttribute(writer, "count", this.Count);
        WriteChildrenTo(writer);

        writer.EndElement();
    }

    internal void WriteChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        List<string> writtenNames = new List<string>();

        foreach (DocxXmlBuffer child in this.Children)
        {
            if (IsLatentStyleException(child))
            {
                DocxLatentStyleException parsed = DocxLatentStyleExceptionFromBuffer(child);
                DocxLatentStyleException? current = FindExceptionByName(parsed.Name);

                if (current != null && !writtenNames.Contains(current.Name))
                {
                    current.WriteTo(writer);
                    writtenNames.Add(current.Name);
                }
            }
            else
            {
                child.WriteTo(writer);
            }
        }

        foreach (DocxLatentStyleException exception in this.Exceptions)
        {
            if (!writtenNames.Contains(exception.Name))
            {
                exception.WriteTo(writer);
                writtenNames.Add(exception.Name);
            }
        }
    }

    internal DocxLatentStyleException? FindExceptionByName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        foreach (DocxLatentStyleException exception in this.Exceptions)
        {
            if (exception.Name == name)
            {
                return exception;
            }
        }

        return null;
    }

    internal void WriteOptionalAttribute(DocxXmlWriter writer, string localName, string value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value != "")
        {
            writer.WriteAttribute("w", localName, DocxConstants.NamespaceWordprocessingMl, value);
        }
    }

    internal void WriteOptionalBoolAttribute(DocxXmlWriter writer, string localName, bool hasValue, bool value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (hasValue)
        {
            DocxXmlAttributes.WriteBool(writer, "w", localName, DocxConstants.NamespaceWordprocessingMl, value);
        }
    }
}
