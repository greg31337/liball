namespace docx;

/// <summary>
/// Represents custom xml part data used by a DOCX document.
/// </summary>
public class DocxCustomXmlPart
{
    /// <summary>
    /// The absolute package part name for the source.
    /// </summary>
    public string SourcePartName;
    /// <summary>
    /// The normalized absolute name of the custom XML package part.
    /// </summary>
    public string PartName;
    /// <summary>
    /// The relationship identifier stored in the document.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// The content type value stored in the document model.
    /// </summary>
    public string ContentType;
    internal string Xml;

    internal DocxCustomXmlPart(string sourcePartName, string partName, string relationshipId, string contentType, string xml)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.SourcePartName = DocxCustomXmlParts.NormalizeSourcePartName(sourcePartName);
        this.PartName = DocxPartName.Normalize(partName);
        this.RelationshipId = relationshipId;
        this.ContentType = contentType;
        this.Xml = xml;
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool HasXml()
    {
        return this.Xml != "";
    }
}
