namespace docx;

/// <summary>
/// Represents break data used by a DOCX document.
/// </summary>
public class DocxBreak : IDocxTextContent
{
    internal const string KindTab = "tab";
    internal const string KindCarriageReturn = "carriageReturn";
    internal const string KindLineBreak = "lineBreak";
    internal const string KindPageBreak = "pageBreak";
    internal const string KindColumnBreak = "columnBreak";
    internal const string KindSymbol = "symbol";
    internal const string KindSoftHyphen = "softHyphen";
    internal const string KindNoBreakHyphen = "noBreakHyphen";

    internal string Xml;
    internal string Kind;
    internal string Type;
    internal string Font;
    internal string Character;

    /// <summary>
    /// Creates a break instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <param name="kind">The kind used by the operation.</param>
    /// <param name="type">The type used by the operation.</param>
    /// <param name="font">The font used by the operation.</param>
    /// <param name="character">The character used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty, malformed, or not supported.</exception>
    public DocxBreak(string xml, string kind, string type, string font, string character)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (kind == null)
        {
            throw new ArgumentNullException("kind");
        }

        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        if (font == null)
        {
            throw new ArgumentNullException("font");
        }

        if (character == null)
        {
            throw new ArgumentNullException("character");
        }

        if (kind == "")
        {
            throw new ArgumentException("Kind is required.", "kind");
        }

        this.Xml = xml;
        this.Kind = kind;
        this.Type = type;
        this.Font = font;
        this.Character = character;
    }

    internal static DocxBreak Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!IsSupported(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on a supported run control element.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return ParseXml(buffer.GetXml());
    }

    internal static DocxBreak ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.MoveToContent())
        {
            throw new InvalidOperationException("XML does not contain a supported run control element.");
        }

        if (reader.IsElement("tab", DocxConstants.NamespaceWordprocessingMl))
        {
            return new DocxBreak(xml, KindTab, "", "", "");
        }

        if (reader.IsElement("cr", DocxConstants.NamespaceWordprocessingMl))
        {
            return new DocxBreak(xml, KindCarriageReturn, "", "", "");
        }

        if (reader.IsElement("br", DocxConstants.NamespaceWordprocessingMl))
        {
            string type = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
            string kind = KindLineBreak;

            if (type == "page")
            {
                kind = KindPageBreak;
            }

            if (type == "column")
            {
                kind = KindColumnBreak;
            }

            return new DocxBreak(xml, kind, type, "", "");
        }

        if (reader.IsElement("sym", DocxConstants.NamespaceWordprocessingMl))
        {
            string font = reader.GetAttribute("font", DocxConstants.NamespaceWordprocessingMl);
            string character = reader.GetAttribute("char", DocxConstants.NamespaceWordprocessingMl);
            return new DocxBreak(xml, KindSymbol, "", font, character);
        }

        if (reader.IsElement("softHyphen", DocxConstants.NamespaceWordprocessingMl))
        {
            return new DocxBreak(xml, KindSoftHyphen, "", "", "");
        }

        if (reader.IsElement("noBreakHyphen", DocxConstants.NamespaceWordprocessingMl))
        {
            return new DocxBreak(xml, KindNoBreakHyphen, "", "", "");
        }

        throw new InvalidOperationException("XML does not contain a supported run control element.");
    }

    internal static bool IsSupported(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.IsElement("tab", DocxConstants.NamespaceWordprocessingMl))
        {
            return true;
        }

        if (reader.IsElement("cr", DocxConstants.NamespaceWordprocessingMl))
        {
            return true;
        }

        if (reader.IsElement("br", DocxConstants.NamespaceWordprocessingMl))
        {
            return true;
        }

        if (reader.IsElement("sym", DocxConstants.NamespaceWordprocessingMl))
        {
            return true;
        }

        if (reader.IsElement("softHyphen", DocxConstants.NamespaceWordprocessingMl))
        {
            return true;
        }

        if (reader.IsElement("noBreakHyphen", DocxConstants.NamespaceWordprocessingMl))
        {
            return true;
        }

        return false;
    }

    internal static DocxBreak CreateTab()
    {
        return new DocxBreak("", KindTab, "", "", "");
    }

    internal static DocxBreak CreateCarriageReturn()
    {
        return new DocxBreak("", KindCarriageReturn, "", "", "");
    }

    internal static DocxBreak CreateLineBreak()
    {
        return new DocxBreak("", KindLineBreak, "", "", "");
    }

    internal static DocxBreak CreatePageBreak()
    {
        return new DocxBreak("", KindPageBreak, "page", "", "");
    }

    internal static DocxBreak CreateColumnBreak()
    {
        return new DocxBreak("", KindColumnBreak, "column", "", "");
    }

    internal static DocxBreak CreateSymbol(string font, string character)
    {
        if (font == null)
        {
            throw new ArgumentNullException("font");
        }

        if (character == null)
        {
            throw new ArgumentNullException("character");
        }

        return new DocxBreak("", KindSymbol, "", font, character);
    }

    internal static DocxBreak CreateSoftHyphen()
    {
        return new DocxBreak("", KindSoftHyphen, "", "", "");
    }

    internal static DocxBreak CreateNoBreakHyphen()
    {
        return new DocxBreak("", KindNoBreakHyphen, "", "", "");
    }

    internal string GetXml()
    {
        return this.Xml;
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

        if (this.Kind == KindTab)
        {
            return TextOrEmpty(options.TabSeparator);
        }

        if (this.Kind == KindCarriageReturn)
        {
            return TextOrEmpty(options.LineBreakSeparator);
        }

        if (this.Kind == KindLineBreak)
        {
            return TextOrEmpty(options.LineBreakSeparator);
        }

        if (this.Kind == KindPageBreak)
        {
            return TextOrEmpty(options.PageBreakSeparator);
        }

        if (this.Kind == KindColumnBreak)
        {
            return TextOrEmpty(options.ColumnBreakSeparator);
        }

        if (this.Kind == KindSymbol)
        {
            return TextOrEmpty(options.SymbolText);
        }

        if (this.Kind == KindSoftHyphen)
        {
            return TextOrEmpty(options.SoftHyphenText);
        }

        if (this.Kind == KindNoBreakHyphen)
        {
            return TextOrEmpty(options.NoBreakHyphenText);
        }

        return "";
    }

    internal static string TextOrEmpty(string text)
    {
        if (text == null)
        {
            return "";
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

        if (this.Kind == KindTab)
        {
            writer.StartElement("w", "tab", DocxConstants.NamespaceWordprocessingMl);
            writer.EndElement();
            return;
        }

        if (this.Kind == KindCarriageReturn)
        {
            writer.StartElement("w", "cr", DocxConstants.NamespaceWordprocessingMl);
            writer.EndElement();
            return;
        }

        if (this.Kind == KindLineBreak || this.Kind == KindPageBreak || this.Kind == KindColumnBreak)
        {
            WriteBreakTo(writer);
            return;
        }

        if (this.Kind == KindSymbol)
        {
            WriteSymbolTo(writer);
            return;
        }

        if (this.Kind == KindSoftHyphen)
        {
            writer.StartElement("w", "softHyphen", DocxConstants.NamespaceWordprocessingMl);
            writer.EndElement();
            return;
        }

        if (this.Kind == KindNoBreakHyphen)
        {
            writer.StartElement("w", "noBreakHyphen", DocxConstants.NamespaceWordprocessingMl);
            writer.EndElement();
            return;
        }

        throw new InvalidOperationException("Unsupported run control kind.");
    }

    internal void WriteBreakTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("w", "br", DocxConstants.NamespaceWordprocessingMl);

        if (this.Kind == KindPageBreak)
        {
            writer.WriteAttribute("w", "type", DocxConstants.NamespaceWordprocessingMl, "page");
        }
        else if (this.Kind == KindColumnBreak)
        {
            writer.WriteAttribute("w", "type", DocxConstants.NamespaceWordprocessingMl, "column");
        }
        else if (this.Type != "")
        {
            writer.WriteAttribute("w", "type", DocxConstants.NamespaceWordprocessingMl, this.Type);
        }

        writer.EndElement();
    }

    internal void WriteSymbolTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("w", "sym", DocxConstants.NamespaceWordprocessingMl);

        if (this.Font != "")
        {
            writer.WriteAttribute("w", "font", DocxConstants.NamespaceWordprocessingMl, this.Font);
        }

        if (this.Character != "")
        {
            writer.WriteAttribute("w", "char", DocxConstants.NamespaceWordprocessingMl, this.Character);
        }

        writer.EndElement();
    }
}
