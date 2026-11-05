namespace docx;

/// <summary>
/// Represents custom xml parts data used by a DOCX document.
/// </summary>
public class DocxCustomXmlParts
{
    internal DocxPackage? Package;
    /// <summary>
    /// The discovered custom XML parts.
    /// </summary>
    public List<DocxCustomXmlPart> Parts;

    internal DocxCustomXmlParts()
    {
        this.Package = null;
        this.Parts = new List<DocxCustomXmlPart>();
    }

    internal static DocxCustomXmlParts Discover(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxCustomXmlParts customXmlParts = new DocxCustomXmlParts();
        customXmlParts.Package = package;
        customXmlParts.DiscoverParts(package);
        return customXmlParts;
    }

    internal int DiscoverParts(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        this.Parts = new List<DocxCustomXmlPart>();

        foreach (DocxRelationshipSet relationshipSet in package.RelationshipSets)
        {
            DiscoverPartsFromRelationshipSet(package, relationshipSet);
        }

        return this.Parts.Count;
    }

    internal void DiscoverPartsFromRelationshipSet(DocxPackage package, DocxRelationshipSet relationshipSet)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        foreach (DocxRelationship relationship in relationshipSet.Relationships)
        {
            if (relationship.Type == DocxConstants.RelationshipTypeCustomXml && !relationship.IsExternal)
            {
                AddPartFromRelationship(package, relationshipSet, relationship);
            }
        }
    }

    internal bool AddPartFromRelationship(DocxPackage package, DocxRelationshipSet relationshipSet, DocxRelationship relationship)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        if (relationship.Type != DocxConstants.RelationshipTypeCustomXml || relationship.IsExternal)
        {
            return false;
        }

        string partName = relationshipSet.ResolveTargetPartName(relationship);
        DocxPart? part = package.FindPart(partName);

        if (part == null)
        {
            return false;
        }

        DocxCustomXmlPart customXmlPart = new DocxCustomXmlPart(
            relationshipSet.SourcePartName,
            part.PartName,
            relationship.Id,
            part.ContentType,
            part.GetText());

        this.Parts.Add(customXmlPart);
        return true;
    }

    internal int Count()
    {
        return this.Parts.Count;
    }

    internal DocxCustomXmlPart GetPart(int index)
    {
        return this.Parts[index];
    }

    internal DocxCustomXmlPart? FindByPartName(string partName)
    {
        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        string normalizedPartName = DocxPartName.Normalize(partName);

        foreach (DocxCustomXmlPart part in this.Parts)
        {
            if (part.PartName == normalizedPartName)
            {
                return part;
            }
        }

        return null;
    }

    internal DocxCustomXmlPart? FindByRelationshipId(string sourcePartName, string relationshipId)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        string normalizedSourcePartName = NormalizeSourcePartName(sourcePartName);

        foreach (DocxCustomXmlPart part in this.Parts)
        {
            if (part.SourcePartName == normalizedSourcePartName && part.RelationshipId == relationshipId)
            {
                return part;
            }
        }

        return null;
    }

    internal static string NormalizeSourcePartName(string sourcePartName)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (sourcePartName == "/")
        {
            return "/";
        }

        return DocxPartName.Normalize(sourcePartName);
    }
}
