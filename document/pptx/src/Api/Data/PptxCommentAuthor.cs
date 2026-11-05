using System.Xml;

namespace pptx;

/// <summary>
/// Represents comment author data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxCommentAuthor
{
    internal PptxPresentation Presentation;
    internal PptxPart Part;
    internal XmlElement Element;
    /// <summary>
    /// Stores the id used to identify or connect related presentation data.
    /// </summary>
    public int Id;
    /// <summary>
    /// Stores the name associated with this object.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the initials associated with this object.
    /// </summary>
    public string Initials;
    /// <summary>
    /// Stores the last index value associated with this object.
    /// </summary>
    public int LastIndex;
    /// <summary>
    /// Stores the color index value associated with this object.
    /// </summary>
    public int ColorIndex;

    internal PptxCommentAuthor(PptxPresentation presentation, PptxPart part, XmlElement element)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        Presentation = presentation;
        Part = part;
        Element = element;
        Id = 0;
        Name = "";
        Initials = "";
        LastIndex = 0;
        ColorIndex = 0;
        Load();
    }

    internal void Load()
    {
        Id = PptxXml.GetIntAttribute(Element, "", "id", 0);
        Name = PptxXml.GetAttributeValue(Element, "", "name");
        Initials = PptxXml.GetAttributeValue(Element, "", "initials");
        LastIndex = PptxXml.GetIntAttribute(Element, "", "lastIdx", 0);
        ColorIndex = PptxXml.GetIntAttribute(Element, "", "clrIdx", 0);
    }
    internal void SetName(string name)
    {
        Name = PptxDiagnostic.CleanText(name);
        PptxXml.SetAttributeValue(Element, "", "", "name", Name);
        MarkDirty();
    }
    internal void SetInitials(string initials)
    {
        Initials = PptxDiagnostic.CleanText(initials);
        PptxXml.SetAttributeValue(Element, "", "", "initials", Initials);
        MarkDirty();
    }
    internal void SetLastIndex(int lastIndex)
    {
        if (lastIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("lastIndex");
        }

        LastIndex = lastIndex;
        PptxXml.SetAttributeValue(Element, "", "", "lastIdx", LastIndex.ToString());
        MarkDirty();
    }
    internal void SetColorIndex(int colorIndex)
    {
        if (colorIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("colorIndex");
        }

        ColorIndex = colorIndex;
        PptxXml.SetAttributeValue(Element, "", "", "clrIdx", ColorIndex.ToString());
        MarkDirty();
    }
    internal bool Remove()
    {
        return Presentation.RemoveCommentAuthor(this);
    }

    internal void MarkDirty()
    {
        Part.SetXmlDocument(PptxTextFrame.GetOwnerDocument(Element));
    }
}
