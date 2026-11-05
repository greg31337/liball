using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Represents custom xml part data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxCustomXmlPart
{
    internal PptxPresentation Presentation;
    internal PptxPart Part;
    internal PptxRelationship Relationship;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the content type associated with this object.
    /// </summary>
    public string ContentType;
    /// <summary>
    /// Stores the UTF-8 decoded XML payload.
    /// </summary>
    public string Xml;

    internal PptxCustomXmlPart(PptxPresentation presentation, PptxPart part, PptxRelationship relationship)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        if (relationship == null)
        {
            throw new System.ArgumentNullException("relationship");
        }

        Presentation = presentation;
        Part = part;
        Relationship = relationship;
        RelationshipId = relationship.Id;
        PartName = part.PartName;
        ContentType = part.ContentType;
        Xml = System.Text.Encoding.UTF8.GetString(part.GetBytes());
    }

    internal void SetXml(string xml)
    {
        EnsurePartExists();
        Xml = PptxDiagnostic.CleanText(xml);
        Part.SetBytes(System.Text.Encoding.UTF8.GetBytes(Xml));
        Part.ClearXmlDocument();
        Part.SetContentType(PptxContentTypes.Xml);
        Presentation.Package.EnsureContentTypeOverride(Part.PartName, Part.ContentType);
        ContentType = Part.ContentType;
    }
    internal byte[] GetBytes()
    {
        if (!Presentation.Package.HasPart(Part.PartName))
        {
            return new byte[0];
        }

        return Part.GetBytes();
    }
    internal void ReplaceBytes(byte[] bytes, string contentType)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        EnsurePartExists();
        Part.SetBytes(bytes);
        Part.ClearXmlDocument();
        Part.SetContentType(contentType);
        Presentation.Package.EnsureContentTypeOverride(Part.PartName, Part.ContentType);
        ContentType = Part.ContentType;
        Xml = System.Text.Encoding.UTF8.GetString(Part.GetBytes());
    }
    internal List<PptxRelationship> GetRelationships()
    {
        List<PptxRelationship> relationships = new List<PptxRelationship>();
        if (!Presentation.Package.HasPart(Part.PartName))
        {
            return relationships;
        }

        for (int index = 0; index < Part.Relationships.Relationships.Count; index++)
        {
            relationships.Add(Part.Relationships.Relationships[index]);
        }

        return relationships;
    }
    internal PptxRelationship AddRelationship(string type, string target, PptxRelationshipTargetMode targetMode)
    {
        EnsurePartExists();
        return Presentation.Package.AddRelationship(Part.PartName, type, target, targetMode);
    }
    internal PptxRelationship SetRelationshipTarget(string relationshipId, string target, PptxRelationshipTargetMode targetMode)
    {
        EnsurePartExists();
        return Presentation.Package.SetRelationshipTarget(Part.PartName, relationshipId, target, targetMode);
    }
    internal void RemoveRelationship(string relationshipId)
    {
        EnsurePartExists();
        Presentation.Package.RemoveRelationship(Part.PartName, relationshipId);
    }
    internal bool Remove()
    {
        if (!Presentation.Package.HasPart(Part.PartName))
        {
            return false;
        }

        bool removed = Presentation.RemoveCustomXmlPart(this);
        if (removed)
        {
            RelationshipId = "";
            PartName = "";
            ContentType = "";
            Xml = "";
        }

        return removed;
    }
    internal void EnsurePartExists()
    {
        if (!Presentation.Package.HasPart(Part.PartName))
        {
            throw new System.ArgumentException("Custom XML part was not found.");
        }
    }
}
