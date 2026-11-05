using System.Xml;

namespace pptx;

/// <summary>
/// Represents core properties data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxCoreProperties
{
    internal PptxPart Part;
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the title associated with this object.
    /// </summary>
    public string Title;
    /// <summary>
    /// Stores the subject associated with this object.
    /// </summary>
    public string Subject;
    /// <summary>
    /// Stores the creator associated with this object.
    /// </summary>
    public string Creator;
    /// <summary>
    /// Stores the description associated with this object.
    /// </summary>
    public string Description;
    /// <summary>
    /// Stores the keywords associated with this object.
    /// </summary>
    public string Keywords;
    /// <summary>
    /// Stores the last modified by associated with this object.
    /// </summary>
    public string LastModifiedBy;
    /// <summary>
    /// Stores the revision associated with this object.
    /// </summary>
    public string Revision;
    /// <summary>
    /// Stores the category associated with this object.
    /// </summary>
    public string Category;
    /// <summary>
    /// Stores the content status associated with this object.
    /// </summary>
    public string ContentStatus;
    /// <summary>
    /// Stores the created associated with this object.
    /// </summary>
    public string Created;
    /// <summary>
    /// Stores the modified associated with this object.
    /// </summary>
    public string Modified;
    /// <summary>
    /// Stores the last printed timestamp in W3CDTF form.
    /// </summary>
    public string LastPrinted;
    /// <summary>
    /// Stores the document identifier.
    /// </summary>
    public string Identifier;
    /// <summary>
    /// Stores the document language tag.
    /// </summary>
    public string Language;
    /// <summary>
    /// Stores the document version text.
    /// </summary>
    public string Version;

    internal PptxCoreProperties(PptxPart part, string relationshipId)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        Part = part;
        PartName = part.PartName;
        RelationshipId = PptxDiagnostic.CleanText(relationshipId);
        Title = "";
        Subject = "";
        Creator = "";
        Description = "";
        Keywords = "";
        LastModifiedBy = "";
        Revision = "";
        Category = "";
        ContentStatus = "";
        Created = "";
        Modified = "";
        LastPrinted = "";
        Identifier = "";
        Language = "";
        Version = "";
        Load();
    }

    internal void Load()
    {
        XmlDocument document = PptxXml.LoadBytes(GetCurrentBytes());
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        Title = GetChildText(root, PptxNamespaces.DublinCore, "title");
        Subject = GetChildText(root, PptxNamespaces.DublinCore, "subject");
        Creator = GetChildText(root, PptxNamespaces.DublinCore, "creator");
        Description = GetChildText(root, PptxNamespaces.DublinCore, "description");
        Keywords = GetChildText(root, PptxNamespaces.CoreProperties, "keywords");
        LastModifiedBy = GetChildText(root, PptxNamespaces.CoreProperties, "lastModifiedBy");
        Revision = GetChildText(root, PptxNamespaces.CoreProperties, "revision");
        Category = GetChildText(root, PptxNamespaces.CoreProperties, "category");
        ContentStatus = GetChildText(root, PptxNamespaces.CoreProperties, "contentStatus");
        Created = GetChildText(root, PptxNamespaces.DublinTerms, "created");
        Modified = GetChildText(root, PptxNamespaces.DublinTerms, "modified");
        LastPrinted = GetChildText(root, PptxNamespaces.CoreProperties, "lastPrinted");
        Identifier = GetChildText(root, PptxNamespaces.DublinCore, "identifier");
        Language = GetChildText(root, PptxNamespaces.DublinCore, "language");
        Version = GetChildText(root, PptxNamespaces.CoreProperties, "version");
    }
    internal void Save()
    {
        XmlDocument document = PptxXml.LoadBytes(GetCurrentBytes());
        XmlElement? rootValue = document.DocumentElement;
        if (rootValue == null)
        {
            throw new System.ArgumentException("Core properties XML root is required.");
        }

        XmlElement root = rootValue;
        SetTextChild(document, root, "dc", PptxNamespaces.DublinCore, "title", Title);
        SetTextChild(document, root, "dc", PptxNamespaces.DublinCore, "subject", Subject);
        SetTextChild(document, root, "dc", PptxNamespaces.DublinCore, "creator", Creator);
        SetTextChild(document, root, "dc", PptxNamespaces.DublinCore, "description", Description);
        SetTextChild(document, root, "cp", PptxNamespaces.CoreProperties, "keywords", Keywords);
        SetTextChild(document, root, "cp", PptxNamespaces.CoreProperties, "lastModifiedBy", LastModifiedBy);
        SetTextChild(document, root, "cp", PptxNamespaces.CoreProperties, "revision", Revision);
        SetTextChild(document, root, "cp", PptxNamespaces.CoreProperties, "category", Category);
        SetTextChild(document, root, "cp", PptxNamespaces.CoreProperties, "contentStatus", ContentStatus);
        SetDateChild(document, root, "created", Created);
        SetDateChild(document, root, "modified", Modified);
        SetTextChild(document, root, "cp", PptxNamespaces.CoreProperties, "lastPrinted", LastPrinted);
        SetTextChild(document, root, "dc", PptxNamespaces.DublinCore, "identifier", Identifier);
        SetTextChild(document, root, "dc", PptxNamespaces.DublinCore, "language", Language);
        SetTextChild(document, root, "cp", PptxNamespaces.CoreProperties, "version", Version);
        Part.SetXmlDocument(document);
    }

    internal byte[] GetCurrentBytes()
    {
        if (Part.HasXmlDocument)
        {
            return PptxXml.WriteBytes(Part.XmlDocument);
        }

        return Part.GetBytes();
    }

    internal static string GetChildText(XmlElement root, string namespaceUri, string localName)
    {
        XmlElement? child = PptxXml.FindFirstChild(root, namespaceUri, localName);
        if (child == null)
        {
            return "";
        }

        return child.InnerText;
    }

    internal static void AppendTextChild(XmlDocument document, XmlElement root, string prefix, string namespaceUri, string localName, string value)
    {
        XmlElement child = PptxXml.CreateElement(document, prefix, localName, namespaceUri);
        child.InnerText = PptxDiagnostic.CleanText(value);
        root.AppendChild(child);
    }

    internal static void AppendDateChild(XmlDocument document, XmlElement root, string localName, string value)
    {
        XmlElement child = PptxXml.CreateElement(document, "dcterms", localName, PptxNamespaces.DublinTerms);
        PptxXml.SetAttributeValue(child, "xsi", PptxNamespaces.Xsi, "type", "dcterms:W3CDTF");
        child.InnerText = PptxDiagnostic.CleanText(value);
        root.AppendChild(child);
    }

    internal static void SetTextChild(XmlDocument document, XmlElement root, string prefix, string namespaceUri, string localName, string value)
    {
        XmlElement? child = PptxXml.FindFirstChild(root, namespaceUri, localName);
        if (child == null)
        {
            child = PptxXml.CreateElement(document, prefix, localName, namespaceUri);
            root.AppendChild(child);
        }

        child.InnerText = PptxDiagnostic.CleanText(value);
    }

    internal static void SetDateChild(XmlDocument document, XmlElement root, string localName, string value)
    {
        XmlElement? child = PptxXml.FindFirstChild(root, PptxNamespaces.DublinTerms, localName);
        if (child == null)
        {
            child = PptxXml.CreateElement(document, "dcterms", localName, PptxNamespaces.DublinTerms);
            root.AppendChild(child);
        }

        PptxXml.SetAttributeValue(child, "xsi", PptxNamespaces.Xsi, "type", "dcterms:W3CDTF");
        child.InnerText = PptxDiagnostic.CleanText(value);
    }
}
