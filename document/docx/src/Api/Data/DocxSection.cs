namespace docx;

/// <summary>
/// Represents one document section and its page, header, and footer settings.
/// </summary>
public class DocxSection
{
    internal string Xml;
    /// <summary>
    /// The format value stored in the document model.
    /// </summary>
    public DocxSectionFormat Format;

    /// <summary>
    /// Creates a section instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxSection(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.Format = new DocxSectionFormat();
        LoadFromXml();
    }

    internal static DocxSection Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("sectPr", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:sectPr.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxSection(buffer.GetXml());
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
        this.Format = new DocxSectionFormat();

        if (this.Xml == "")
        {
            return;
        }

        this.Format = DocxSectionFormat.ParseXml(this.Xml);
    }

    internal void SetXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        LoadFromXml();
    }

    internal void SetFormat(DocxSectionFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        this.Format = format;
        this.Xml = "";
    }

    internal bool HasFormat()
    {
        return this.Format.HasAnyFormatting();
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

        if (this.Xml != "" && this.Format.Xml == this.Xml)
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.Xml);
            buffer.WriteTo(writer);
            return;
        }

        this.Format.WriteTo(writer);
    }
}
