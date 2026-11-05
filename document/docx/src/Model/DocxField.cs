namespace docx;

internal class DocxComplexFieldElement
{
    internal const string KindCharacter = "character";
    internal const string KindInstructionText = "instructionText";
    internal const string KindDeletedInstructionText = "deletedInstructionText";

    internal string Xml;
    internal string Kind;
    internal string FieldCharType;
    internal string InstructionText;
    internal bool PreserveSpace;
    internal string FieldLock;
    internal string Dirty;

    internal DocxComplexFieldElement(string xml, string kind, string fieldCharType, string instructionText, bool preserveSpace, string fieldLock, string dirty)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (kind == null)
        {
            throw new ArgumentNullException("kind");
        }

        if (fieldCharType == null)
        {
            throw new ArgumentNullException("fieldCharType");
        }

        if (instructionText == null)
        {
            throw new ArgumentNullException("instructionText");
        }

        if (fieldLock == null)
        {
            throw new ArgumentNullException("fieldLock");
        }

        if (dirty == null)
        {
            throw new ArgumentNullException("dirty");
        }

        if (kind == "")
        {
            throw new ArgumentException("Kind is required.", "kind");
        }

        this.Xml = xml;
        this.Kind = kind;
        this.FieldCharType = fieldCharType;
        this.InstructionText = instructionText;
        this.PreserveSpace = preserveSpace;
        this.FieldLock = fieldLock;
        this.Dirty = dirty;
    }

    internal static DocxComplexFieldElement Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!IsSupported(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on a complex field element.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return ParseXml(buffer.GetXml());
    }

    internal static DocxComplexFieldElement ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.MoveToContent())
        {
            throw new InvalidOperationException("XML does not contain a complex field element.");
        }

        if (reader.IsElement("fldChar", DocxConstants.NamespaceWordprocessingMl))
        {
            string fieldCharType = DocxField.ReadRequiredAttribute(reader, "fldCharType");
            string fieldLock = reader.GetAttribute("fldLock", DocxConstants.NamespaceWordprocessingMl);
            string dirty = reader.GetAttribute("dirty", DocxConstants.NamespaceWordprocessingMl);
            return new DocxComplexFieldElement(xml, KindCharacter, fieldCharType, "", false, fieldLock, dirty);
        }

        if (reader.IsElement("instrText", DocxConstants.NamespaceWordprocessingMl))
        {
            return ReadInstructionElement(reader, xml, KindInstructionText);
        }

        if (reader.IsElement("delInstrText", DocxConstants.NamespaceWordprocessingMl))
        {
            return ReadInstructionElement(reader, xml, KindDeletedInstructionText);
        }

        throw new InvalidOperationException("XML does not contain a complex field element.");
    }

    internal static DocxComplexFieldElement ReadInstructionElement(DocxXmlReader reader, string xml, string kind)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (kind == null)
        {
            throw new ArgumentNullException("kind");
        }

        string space = reader.GetAttribute("space", DocxConstants.NamespaceXml);
        bool preserveSpace = space == "preserve";
        string text = reader.Reader.ReadElementContentAsString();
        return new DocxComplexFieldElement(xml, kind, "", text, preserveSpace, "", "");
    }

    internal static bool IsSupported(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.IsElement("fldChar", DocxConstants.NamespaceWordprocessingMl))
        {
            return true;
        }

        if (reader.IsElement("instrText", DocxConstants.NamespaceWordprocessingMl))
        {
            return true;
        }

        if (reader.IsElement("delInstrText", DocxConstants.NamespaceWordprocessingMl))
        {
            return true;
        }

        return false;
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool IsFieldCharacter()
    {
        return this.Kind == KindCharacter;
    }

    internal bool IsInstructionText()
    {
        return this.Kind == KindInstructionText;
    }

    internal bool IsDeletedInstructionText()
    {
        return this.Kind == KindDeletedInstructionText;
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

        if (this.Kind == KindCharacter)
        {
            WriteFieldCharacterTo(writer);
            return;
        }

        if (this.Kind == KindInstructionText)
        {
            WriteInstructionTextTo(writer, "instrText");
            return;
        }

        if (this.Kind == KindDeletedInstructionText)
        {
            WriteInstructionTextTo(writer, "delInstrText");
            return;
        }

        throw new InvalidOperationException("Unsupported complex field element kind.");
    }

    internal void WriteFieldCharacterTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.FieldCharType == "")
        {
            throw new InvalidOperationException("Field character type is required.");
        }

        writer.StartElement("w", "fldChar", DocxConstants.NamespaceWordprocessingMl);
        writer.WriteAttribute("w", "fldCharType", DocxConstants.NamespaceWordprocessingMl, this.FieldCharType);

        if (this.FieldLock != "")
        {
            writer.WriteAttribute("w", "fldLock", DocxConstants.NamespaceWordprocessingMl, this.FieldLock);
        }

        if (this.Dirty != "")
        {
            writer.WriteAttribute("w", "dirty", DocxConstants.NamespaceWordprocessingMl, this.Dirty);
        }

        writer.EndElement();
    }

    internal void WriteInstructionTextTo(DocxXmlWriter writer, string localName)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        writer.StartElement("w", localName, DocxConstants.NamespaceWordprocessingMl);

        if (this.PreserveSpace)
        {
            writer.WriteAttribute("xml", "space", DocxConstants.NamespaceXml, "preserve");
        }

        writer.WriteText(this.InstructionText);
        writer.EndElement();
    }
}
