namespace docx;

/// <summary>
/// Describes a custom XML part, its item properties, and outgoing relationships.
/// </summary>
public class DocxCustomXmlPartInfo
{
    /// <summary>The discovered custom XML part.</summary>
    public DocxCustomXmlPart Part;
    /// <summary>The custom XML payload.</summary>
    public string Xml;
    /// <summary>The custom XML properties part name, or an empty string.</summary>
    public string ItemPropertiesPartName;
    /// <summary>The properties relationship identifier, or an empty string.</summary>
    public string ItemPropertiesRelationshipId;
    /// <summary>The custom XML store item identifier, or an empty string.</summary>
    public string ItemId;
    /// <summary>The preserved item-properties XML, or an empty string.</summary>
    public string ItemPropertiesXml;
    /// <summary>The schema reference URIs.</summary>
    public List<string> SchemaReferences;
    /// <summary>The relationships originating from the custom XML part.</summary>
    public List<DocxRelationship> Relationships;

    /// <summary>Creates a custom XML part descriptor.</summary>
    /// <param name="part">The custom XML part.</param>
    /// <param name="xml">The custom XML payload.</param>
    /// <param name="itemPropertiesPartName">The item-properties part name.</param>
    /// <param name="itemPropertiesRelationshipId">The properties relationship identifier.</param>
    /// <param name="itemId">The store item identifier.</param>
    /// <param name="itemPropertiesXml">The item-properties XML.</param>
    /// <param name="schemaReferences">The schema references.</param>
    /// <param name="relationships">The outgoing relationships.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxCustomXmlPartInfo(DocxCustomXmlPart part, string xml, string itemPropertiesPartName, string itemPropertiesRelationshipId, string itemId, string itemPropertiesXml, List<string> schemaReferences, List<DocxRelationship> relationships)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        if (xml == null || itemPropertiesPartName == null || itemPropertiesRelationshipId == null || itemId == null || itemPropertiesXml == null || schemaReferences == null || relationships == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Part = part;
        this.Xml = xml;
        this.ItemPropertiesPartName = itemPropertiesPartName;
        this.ItemPropertiesRelationshipId = itemPropertiesRelationshipId;
        this.ItemId = itemId;
        this.ItemPropertiesXml = itemPropertiesXml;
        this.SchemaReferences = schemaReferences;
        this.Relationships = relationships;
    }
}
