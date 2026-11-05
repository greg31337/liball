using System.Xml;

namespace pptx;

/// <summary>
/// Represents modern comment data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxModernComment
{
    internal PptxSlide Slide;
    internal PptxPart Part;
    internal PptxRelationship Relationship;
    internal XmlElement Element;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the id used to identify or connect related presentation data.
    /// </summary>
    public string Id;
    /// <summary>
    /// Stores the author id used to identify or connect related presentation data.
    /// </summary>
    public string AuthorId;
    /// <summary>
    /// Stores the created associated with this object.
    /// </summary>
    public string Created;
    /// <summary>
    /// Stores the status associated with this object.
    /// </summary>
    public string Status;
    /// <summary>
    /// Stores the text associated with this object.
    /// </summary>
    public string Text;
    internal string RawXml;
    internal PptxModernComment(PptxSlide slide, PptxPart part, PptxRelationship relationship, XmlElement element)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        if (relationship == null)
        {
            throw new System.ArgumentNullException("relationship");
        }

        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        Slide = slide;
        Part = part;
        Relationship = relationship;
        Element = element;
        RelationshipId = "";
        PartName = "";
        Id = "";
        AuthorId = "";
        Created = "";
        Status = "";
        Text = "";
        RawXml = "";
        Load();
    }

    internal void Load()
    {
        RelationshipId = Relationship.Id;
        PartName = Part.PartName;
        Id = PptxXml.GetAttributeValue(Element, "", "id");
        AuthorId = PptxXml.GetAttributeValue(Element, "", "authorId");
        Created = PptxXml.GetAttributeValue(Element, "", "created");
        Status = PptxXml.GetAttributeValue(Element, "", "status");
        Text = ExtractCommentText();
        RawXml = Element.OuterXml;
    }
    internal string ExtractCommentText()
    {
        XmlElement? textBody = PptxXml.FindFirstDescendant(Element, PptxNamespaces.ModernComments, "txBody");
        if (textBody == null)
        {
            return "";
        }

        return PptxTextFrame.GetTextBodyText(textBody);
    }
}

