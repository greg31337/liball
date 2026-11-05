using System.Xml;

namespace docx;

/// <summary>
/// Reads and edits custom XML parts, item properties, bindings, and relationships.
/// </summary>
public class DocxCustomXmlManagement : IDocxCustomXmlManagement
{
    internal const string NamespaceCustomXmlDataStore = "http://schemas.openxmlformats.org/officeDocument/2006/customXml";

    /// <summary>Creates a custom XML management service.</summary>
    public DocxCustomXmlManagement()
    {
    }

    /// <summary>Returns custom XML parts with item-property metadata.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The parts in relationship discovery order.</returns>
    public List<DocxCustomXmlPartInfo> GetCustomXmlParts(DocxDocument document)
    {
        ValidateDocument(document);
        List<DocxCustomXmlPartInfo> parts = new List<DocxCustomXmlPartInfo>();

        foreach (DocxCustomXmlPart part in document.CustomXmlParts.Parts)
        {
            parts.Add(GetCustomXmlPartInfo(document, part));
        }

        return parts;
    }

    /// <summary>Returns detailed information for one custom XML part.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="part">The part to inspect.</param>
    /// <returns>The part descriptor.</returns>
    public DocxCustomXmlPartInfo GetCustomXmlPartInfo(DocxDocument document, DocxCustomXmlPart part)
    {
        ValidateOwnedPart(document, part);
        List<DocxRelationship> relationships = document.Package.GetRelationships(part.PartName);
        string propertiesPartName = "";
        string propertiesRelationshipId = "";
        string propertiesXml = "";
        string itemId = "";
        List<string> schemaReferences = new List<string>();

        foreach (DocxRelationship relationship in relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeCustomXmlProperties && !relationship.IsExternal)
            {
                propertiesPartName = document.Package.ResolveRelationshipTarget(part.PartName, relationship.Id);
                propertiesRelationshipId = relationship.Id;
                DocxPart? propertiesPart = document.Package.FindPart(propertiesPartName);

                if (propertiesPart != null)
                {
                    propertiesXml = propertiesPart.GetText();
                    itemId = ReadItemProperties(propertiesXml, schemaReferences);
                }

                break;
            }
        }

        return new DocxCustomXmlPartInfo(part, part.GetXml(), propertiesPartName, propertiesRelationshipId, itemId, propertiesXml, schemaReferences, relationships);
    }

    /// <summary>Finds a custom XML part by package part name.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="partName">The package part name.</param>
    /// <returns>The matching part, or null.</returns>
    public DocxCustomXmlPart? FindCustomXmlPartByName(DocxDocument document, string partName)
    {
        ValidateDocument(document);
        return document.CustomXmlParts.FindByPartName(partName);
    }

    /// <summary>Finds a custom XML part through a source relationship.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="sourcePartName">The relationship source.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <returns>The matching part, or null.</returns>
    public DocxCustomXmlPart? FindCustomXmlPartByRelationship(DocxDocument document, string sourcePartName, string relationshipId)
    {
        ValidateDocument(document);
        return document.CustomXmlParts.FindByRelationshipId(sourcePartName, relationshipId);
    }

    /// <summary>Finds a custom XML part by item identifier.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="itemId">The custom XML store item identifier.</param>
    /// <returns>The matching part, or null.</returns>
    public DocxCustomXmlPart? FindCustomXmlPartByItemId(DocxDocument document, string itemId)
    {
        ValidateDocument(document);
        ValidateString(itemId, "itemId", true);

        foreach (DocxCustomXmlPartInfo info in GetCustomXmlParts(document))
        {
            if (string.Equals(info.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
            {
                return info.Part;
            }
        }

        return null;
    }

    /// <summary>Finds the custom XML part referenced by a bound content control.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="contentControl">The bound content control.</param>
    /// <returns>The matching part, or null.</returns>
    public DocxCustomXmlPart? FindCustomXmlPartForContentControl(DocxDocument document, DocxContentControl contentControl)
    {
        ValidateDocument(document);

        if (contentControl == null)
        {
            throw new ArgumentNullException("contentControl");
        }

        if (contentControl.DataBindingStoreItemId == "")
        {
            return null;
        }

        return FindCustomXmlPartByItemId(document, contentControl.DataBindingStoreItemId);
    }

    /// <summary>Returns content controls bound to a custom XML part.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="part">The custom XML part.</param>
    /// <returns>The bound controls in document story order.</returns>
    public List<DocxContentControl> GetBoundContentControls(DocxDocument document, DocxCustomXmlPart part)
    {
        DocxCustomXmlPartInfo info = GetCustomXmlPartInfo(document, part);
        List<DocxContentControl> controls = new List<DocxContentControl>();

        if (info.ItemId == "")
        {
            return controls;
        }

        foreach (DocxContentControl contentControl in document.GetContentControls())
        {
            if (string.Equals(contentControl.DataBindingStoreItemId, info.ItemId, StringComparison.OrdinalIgnoreCase))
            {
                controls.Add(contentControl);
            }
        }

        return controls;
    }

    /// <summary>Returns a custom XML payload.</summary>
    /// <param name="part">The custom XML part.</param>
    /// <returns>The XML payload.</returns>
    public string GetCustomXmlContent(DocxCustomXmlPart part)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        return part.GetXml();
    }

    /// <summary>Inserts a custom XML part and source relationship.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="sourcePartName">The package root or source part name.</param>
    /// <param name="xml">The XML payload.</param>
    /// <param name="itemId">The item identifier, or an empty string.</param>
    /// <returns>The inserted part.</returns>
    public DocxCustomXmlPart AddCustomXmlPart(DocxDocument document, string sourcePartName, string xml, string itemId)
    {
        ValidateDocument(document);
        string normalizedSourcePartName = DocxCustomXmlParts.NormalizeSourcePartName(sourcePartName);
        ValidateXml(xml, "xml");
        ValidateString(itemId, "itemId", false);

        if (normalizedSourcePartName != "/" && !document.Package.ContainsPart(normalizedSourcePartName))
        {
            throw new KeyNotFoundException("Custom XML relationship source part is missing.");
        }

        string partName = AllocateCustomXmlPartName(document.Package, "item");
        document.Package.AddXmlPartWithContent(partName, DocxConstants.ContentTypeXml, xml);
        string target = GetRelativeTarget(normalizedSourcePartName, partName);
        DocxRelationship relationship = document.Package.CreateRelationship(normalizedSourcePartName, DocxConstants.RelationshipTypeCustomXml, target, false);
        document.ParseCustomXmlParts();
        DocxCustomXmlPart? part = document.CustomXmlParts.FindByRelationshipId(normalizedSourcePartName, relationship.Id);

        if (part == null)
        {
            throw new InvalidOperationException("Inserted custom XML part could not be discovered.");
        }

        if (itemId != "")
        {
            SetCustomXmlItemProperties(document, part, itemId, new List<string>());
        }

        return part;
    }

    /// <summary>Replaces a custom XML payload while retaining its relationships.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The part to replace.</param>
    /// <param name="xml">The replacement XML.</param>
    public void ReplaceCustomXmlContent(DocxDocument document, DocxCustomXmlPart part, string xml)
    {
        ValidateOwnedPart(document, part);
        ValidateXml(xml, "xml");
        document.Package.ReplaceXmlPart(part.PartName, part.ContentType, xml);
        part.Xml = xml;
    }

    /// <summary>Removes a custom XML part, its source relationship, and its item properties.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The part to remove.</param>
    /// <returns>True when the part was removed.</returns>
    public bool RemoveCustomXmlPart(DocxDocument document, DocxCustomXmlPart part)
    {
        ValidateOwnedPart(document, part);
        RemoveCustomXmlItemProperties(document, part);
        List<DocxRelationship> relationships = document.Package.GetRelationships(part.PartName);

        foreach (DocxRelationship relationship in relationships)
        {
            document.Package.RemoveRelationship(part.PartName, relationship.Id);
        }

        document.Package.RemovePart(DocxRelationshipSet.GetRelationshipPartName(part.PartName));
        document.Package.RemoveRelationship(part.SourcePartName, part.RelationshipId);
        bool removed = document.Package.RemovePart(part.PartName);
        document.ParseCustomXmlParts();
        return removed;
    }

    /// <summary>Returns relationships originating from a custom XML part.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="part">The relationship source.</param>
    /// <returns>The relationships in declaration order.</returns>
    public List<DocxRelationship> GetCustomXmlRelationships(DocxDocument document, DocxCustomXmlPart part)
    {
        ValidateOwnedPart(document, part);
        return document.Package.GetRelationships(part.PartName);
    }

    /// <summary>Adds a relationship originating from a custom XML part.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The relationship source.</param>
    /// <param name="type">The relationship type URI.</param>
    /// <param name="target">The relationship target.</param>
    /// <param name="external">Whether the target is external.</param>
    /// <returns>The created relationship.</returns>
    public DocxRelationship AddCustomXmlRelationship(DocxDocument document, DocxCustomXmlPart part, string type, string target, bool external)
    {
        ValidateOwnedPart(document, part);
        return document.Package.CreateRelationship(part.PartName, type, target, external);
    }

    /// <summary>Removes a relationship originating from a custom XML part.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The relationship source.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <returns>True when the relationship was removed.</returns>
    public bool RemoveCustomXmlRelationship(DocxDocument document, DocxCustomXmlPart part, string relationshipId)
    {
        ValidateOwnedPart(document, part);
        return document.Package.RemoveRelationship(part.PartName, relationshipId);
    }

    /// <summary>Creates or replaces item properties with an item identifier and schema references.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The custom XML part.</param>
    /// <param name="itemId">The store item identifier.</param>
    /// <param name="schemaReferences">The schema reference URIs.</param>
    public void SetCustomXmlItemProperties(DocxDocument document, DocxCustomXmlPart part, string itemId, List<string> schemaReferences)
    {
        DocxCustomXmlPartInfo info = GetCustomXmlPartInfo(document, part);
        ValidateString(itemId, "itemId", true);

        if (schemaReferences == null)
        {
            throw new ArgumentNullException("schemaReferences");
        }

        string xml = BuildItemPropertiesXml(info.ItemPropertiesXml, itemId, schemaReferences);

        if (info.ItemPropertiesPartName == "")
        {
            string propertiesPartName = AllocateCustomXmlPartName(document.Package, "itemProps");
            document.Package.AddXmlPartWithContent(propertiesPartName, DocxConstants.ContentTypeCustomXmlProperties, xml);
            string target = GetRelativeTarget(part.PartName, propertiesPartName);
            document.Package.CreateRelationship(part.PartName, DocxConstants.RelationshipTypeCustomXmlProperties, target, false);
        }
        else
        {
            DocxPart? propertiesPart = document.Package.FindPart(info.ItemPropertiesPartName);
            string contentType = DocxConstants.ContentTypeCustomXmlProperties;

            if (propertiesPart != null)
            {
                contentType = propertiesPart.ContentType;
            }

            document.Package.ReplaceXmlPart(info.ItemPropertiesPartName, contentType, xml);
        }
    }

    /// <summary>Replaces item-properties XML without interpreting unsupported content.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The custom XML part.</param>
    /// <param name="xml">The replacement item-properties XML.</param>
    public void ReplaceCustomXmlItemPropertiesXml(DocxDocument document, DocxCustomXmlPart part, string xml)
    {
        DocxCustomXmlPartInfo info = GetCustomXmlPartInfo(document, part);
        ValidateItemPropertiesXml(xml);

        if (info.ItemPropertiesPartName == "")
        {
            throw new InvalidOperationException("Custom XML item properties are missing.");
        }

        DocxPart? propertiesPart = document.Package.FindPart(info.ItemPropertiesPartName);
        string contentType = DocxConstants.ContentTypeCustomXmlProperties;

        if (propertiesPart != null)
        {
            contentType = propertiesPart.ContentType;
        }

        document.Package.ReplaceXmlPart(info.ItemPropertiesPartName, contentType, xml);
    }

    /// <summary>Removes a custom XML part's item-properties relationship and part.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="part">The custom XML part.</param>
    /// <returns>True when item properties were removed.</returns>
    public bool RemoveCustomXmlItemProperties(DocxDocument document, DocxCustomXmlPart part)
    {
        DocxCustomXmlPartInfo info = GetCustomXmlPartInfo(document, part);

        if (info.ItemPropertiesPartName == "")
        {
            return false;
        }

        document.Package.RemoveRelationship(part.PartName, info.ItemPropertiesRelationshipId);
        return document.Package.RemovePart(info.ItemPropertiesPartName);
    }

    internal static void ValidateDocument(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }

    internal static void ValidateOwnedPart(DocxDocument document, DocxCustomXmlPart part)
    {
        ValidateDocument(document);

        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        foreach (DocxCustomXmlPart candidate in document.CustomXmlParts.Parts)
        {
            if (candidate == part)
            {
                return;
            }
        }

        throw new ArgumentException("Custom XML part does not belong to the document.", "part");
    }

    internal static void ValidateString(string value, string name, bool requireValue)
    {
        if (value == null)
        {
            throw new ArgumentNullException(name);
        }

        if (requireValue && value == "")
        {
            throw new ArgumentException("A value is required.", name);
        }
    }

    internal static void ValidateXml(string xml, string name)
    {
        if (xml == null)
        {
            throw new ArgumentNullException(name);
        }

        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(xml);
    }

    internal static void ValidateItemPropertiesXml(string xml)
    {
        ValidateXml(xml, "xml");
        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(xml);
        XmlElement? root = document.DocumentElement;

        if (root == null || root.LocalName != "datastoreItem" || root.NamespaceURI != NamespaceCustomXmlDataStore)
        {
            throw new ArgumentException("Custom XML item-properties root is invalid.", "xml");
        }
    }

    internal static string ReadItemProperties(string xml, List<string> schemaReferences)
    {
        string itemId = "";

        if (xml == "")
        {
            return itemId;
        }

        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(xml);
        XmlElement? root = document.DocumentElement;

        if (root == null)
        {
            return itemId;
        }

        itemId = GetAttributeByLocalName(root, "itemID");
        XmlNodeList references = root.GetElementsByTagName("schemaRef", NamespaceCustomXmlDataStore);

        foreach (XmlNode node in references)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                schemaReferences.Add(GetAttributeByLocalName((XmlElement)node, "uri"));
            }
        }

        return itemId;
    }

    internal static string GetAttributeByLocalName(XmlElement element, string localName)
    {
        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (attribute.LocalName == localName)
            {
                return attribute.Value;
            }
        }

        return "";
    }

    internal static string AllocateCustomXmlPartName(DocxPackage package, string baseName)
    {
        int index = 1;

        while (true)
        {
            string partName = "/customXml/" + baseName + index.ToString() + ".xml";

            if (!package.ContainsPart(partName))
            {
                return partName;
            }

            index = index + 1;
        }
    }

    internal static string GetRelativeTarget(string sourcePartName, string targetPartName)
    {
        string normalizedSource = DocxCustomXmlParts.NormalizeSourcePartName(sourcePartName);
        string normalizedTarget = DocxPartName.Normalize(targetPartName);

        if (normalizedSource == "/")
        {
            return normalizedTarget.Substring(1);
        }

        string sourceDirectory = DocxRelationshipSet.GetSourceBaseDirectory(normalizedSource);
        string[] sourceSegments = sourceDirectory.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string[] targetSegments = normalizedTarget.Split('/', StringSplitOptions.RemoveEmptyEntries);
        int commonCount = 0;

        while (commonCount < sourceSegments.Length && commonCount < targetSegments.Length && sourceSegments[commonCount] == targetSegments[commonCount])
        {
            commonCount = commonCount + 1;
        }

        string target = "";
        int index = commonCount;

        while (index < sourceSegments.Length)
        {
            target = target + "../";
            index = index + 1;
        }

        index = commonCount;

        while (index < targetSegments.Length)
        {
            if (target != "" && !target.EndsWith("/"))
            {
                target = target + "/";
            }

            target = target + targetSegments[index];
            index = index + 1;
        }

        return target;
    }

    internal static string BuildItemPropertiesXml(string existingXml, string itemId, List<string> schemaReferences)
    {
        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;

        if (existingXml == "")
        {
            document.LoadXml("<ds:datastoreItem xmlns:ds=\"" + NamespaceCustomXmlDataStore + "\"><ds:schemaRefs /></ds:datastoreItem>");
        }
        else
        {
            document.LoadXml(existingXml);
        }

        XmlElement? root = document.DocumentElement;

        if (root == null || root.LocalName != "datastoreItem" || root.NamespaceURI != NamespaceCustomXmlDataStore)
        {
            throw new InvalidOperationException("Custom XML item-properties root is invalid.");
        }

        SetDataStoreAttribute(document, root, "itemID", itemId);
        XmlElement? schemaRefs = FindDirectChild(root, "schemaRefs");

        if (schemaRefs == null)
        {
            schemaRefs = document.CreateElement("ds", "schemaRefs", NamespaceCustomXmlDataStore);
            root.AppendChild(schemaRefs);
        }

        List<XmlNode> removedReferences = new List<XmlNode>();

        foreach (XmlNode child in schemaRefs.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element && child.LocalName == "schemaRef" && child.NamespaceURI == NamespaceCustomXmlDataStore)
            {
                removedReferences.Add(child);
            }
        }

        foreach (XmlNode removedReference in removedReferences)
        {
            schemaRefs.RemoveChild(removedReference);
        }

        foreach (string schemaReference in schemaReferences)
        {
            ValidateString(schemaReference, "schemaReferences", true);
            XmlElement schemaRef = document.CreateElement("ds", "schemaRef", NamespaceCustomXmlDataStore);
            SetDataStoreAttribute(document, schemaRef, "uri", schemaReference);
            schemaRefs.AppendChild(schemaRef);
        }

        return document.OuterXml;
    }

    internal static XmlElement? FindDirectChild(XmlElement parent, string localName)
    {
        foreach (XmlNode child in parent.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element && child.LocalName == localName && child.NamespaceURI == NamespaceCustomXmlDataStore)
            {
                return (XmlElement)child;
            }
        }

        return null;
    }

    internal static void SetDataStoreAttribute(XmlDocument document, XmlElement element, string localName, string value)
    {
        XmlAttribute attribute = document.CreateAttribute("ds", localName, NamespaceCustomXmlDataStore);
        attribute.Value = value;
        element.Attributes.SetNamedItem(attribute);
    }
}
