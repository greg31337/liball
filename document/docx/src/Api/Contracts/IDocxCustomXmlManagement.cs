namespace docx;

/// <summary>
/// Reads and edits custom XML parts, item properties, bindings, and relationships.
/// </summary>
public interface IDocxCustomXmlManagement
{
    /// <summary>Returns custom XML parts with item-property metadata.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The parts in relationship discovery order.</returns>
    List<DocxCustomXmlPartInfo> GetCustomXmlParts(DocxDocument document);

    /// <summary>Returns detailed information for one custom XML part.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="part">The part to inspect.</param>
    /// <returns>The part descriptor.</returns>
    DocxCustomXmlPartInfo GetCustomXmlPartInfo(DocxDocument document, DocxCustomXmlPart part);

    /// <summary>Finds a custom XML part by package part name.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="partName">The package part name.</param>
    /// <returns>The matching part, or null.</returns>
    DocxCustomXmlPart? FindCustomXmlPartByName(DocxDocument document, string partName);

    /// <summary>Finds a custom XML part through a source relationship.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="sourcePartName">The relationship source.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <returns>The matching part, or null.</returns>
    DocxCustomXmlPart? FindCustomXmlPartByRelationship(DocxDocument document, string sourcePartName, string relationshipId);

    /// <summary>Finds a custom XML part by item identifier.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="itemId">The custom XML store item identifier.</param>
    /// <returns>The matching part, or null.</returns>
    DocxCustomXmlPart? FindCustomXmlPartByItemId(DocxDocument document, string itemId);

    /// <summary>Finds the custom XML part referenced by a bound content control.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="contentControl">The bound content control.</param>
    /// <returns>The matching part, or null.</returns>
    DocxCustomXmlPart? FindCustomXmlPartForContentControl(DocxDocument document, DocxContentControl contentControl);

    /// <summary>Returns content controls bound to a custom XML part.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="part">The custom XML part.</param>
    /// <returns>The bound controls in document story order.</returns>
    List<DocxContentControl> GetBoundContentControls(DocxDocument document, DocxCustomXmlPart part);

    /// <summary>Returns a custom XML payload.</summary>
    /// <param name="part">The custom XML part.</param>
    /// <returns>The XML payload.</returns>
    string GetCustomXmlContent(DocxCustomXmlPart part);

    /// <summary>Inserts a custom XML part and source relationship.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="sourcePartName">The package root or source part name.</param>
    /// <param name="xml">The XML payload.</param>
    /// <param name="itemId">The item identifier, or an empty string.</param>
    /// <returns>The inserted part.</returns>
    DocxCustomXmlPart AddCustomXmlPart(DocxDocument document, string sourcePartName, string xml, string itemId);

    /// <summary>Replaces a custom XML payload while retaining its relationships.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The part to replace.</param>
    /// <param name="xml">The replacement XML.</param>
    void ReplaceCustomXmlContent(DocxDocument document, DocxCustomXmlPart part, string xml);

    /// <summary>Removes a custom XML part, its source relationship, and its item properties.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The part to remove.</param>
    /// <returns>True when the part was removed.</returns>
    bool RemoveCustomXmlPart(DocxDocument document, DocxCustomXmlPart part);

    /// <summary>Returns relationships originating from a custom XML part.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="part">The relationship source.</param>
    /// <returns>The relationships in declaration order.</returns>
    List<DocxRelationship> GetCustomXmlRelationships(DocxDocument document, DocxCustomXmlPart part);

    /// <summary>Adds a relationship originating from a custom XML part.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The relationship source.</param>
    /// <param name="type">The relationship type URI.</param>
    /// <param name="target">The relationship target.</param>
    /// <param name="external">Whether the target is external.</param>
    /// <returns>The created relationship.</returns>
    DocxRelationship AddCustomXmlRelationship(DocxDocument document, DocxCustomXmlPart part, string type, string target, bool external);

    /// <summary>Removes a relationship originating from a custom XML part.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The relationship source.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <returns>True when the relationship was removed.</returns>
    bool RemoveCustomXmlRelationship(DocxDocument document, DocxCustomXmlPart part, string relationshipId);

    /// <summary>Creates or replaces item properties with an item identifier and schema references.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The custom XML part.</param>
    /// <param name="itemId">The store item identifier.</param>
    /// <param name="schemaReferences">The schema reference URIs.</param>
    void SetCustomXmlItemProperties(DocxDocument document, DocxCustomXmlPart part, string itemId, List<string> schemaReferences);

    /// <summary>Replaces item-properties XML without interpreting unsupported content.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The custom XML part.</param>
    /// <param name="xml">The replacement item-properties XML.</param>
    void ReplaceCustomXmlItemPropertiesXml(DocxDocument document, DocxCustomXmlPart part, string xml);

    /// <summary>Removes a custom XML part's item-properties relationship and part.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The custom XML part.</param>
    /// <returns>True when item properties were removed.</returns>
    bool RemoveCustomXmlItemProperties(DocxDocument document, DocxCustomXmlPart part);
}
