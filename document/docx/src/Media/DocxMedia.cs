using System.IO;

namespace docx;

internal class DocxMediaImportMap
{
    internal List<DocxMediaRelationshipMap> Relationships;

    internal DocxMediaImportMap()
    {
        this.Relationships = new List<DocxMediaRelationshipMap>();
    }

    internal void Add(string sourcePartName, string sourceRelationshipId, string targetSourcePartName, string targetRelationshipId, string sourceImagePartName, string targetImagePartName)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (sourceRelationshipId == null)
        {
            throw new ArgumentNullException("sourceRelationshipId");
        }

        if (targetSourcePartName == null)
        {
            throw new ArgumentNullException("targetSourcePartName");
        }

        if (targetRelationshipId == null)
        {
            throw new ArgumentNullException("targetRelationshipId");
        }

        if (sourceImagePartName == null)
        {
            throw new ArgumentNullException("sourceImagePartName");
        }

        if (targetImagePartName == null)
        {
            throw new ArgumentNullException("targetImagePartName");
        }

        DocxMediaRelationshipMap relationship = new DocxMediaRelationshipMap(
            sourcePartName,
            sourceRelationshipId,
            targetSourcePartName,
            targetRelationshipId,
            sourceImagePartName,
            targetImagePartName);
        this.Relationships.Add(relationship);
    }

    internal int Count()
    {
        return this.Relationships.Count;
    }

    internal DocxMediaRelationshipMap Get(int index)
    {
        return this.Relationships[index];
    }

    internal string GetRelationshipId(string sourcePartName, string sourceRelationshipId)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (sourceRelationshipId == null)
        {
            throw new ArgumentNullException("sourceRelationshipId");
        }

        string normalizedSourcePartName = DocxImage.NormalizeSourcePartName(sourcePartName);

        foreach (DocxMediaRelationshipMap relationship in this.Relationships)
        {
            if (relationship.SourcePartName == normalizedSourcePartName && relationship.SourceRelationshipId == sourceRelationshipId)
            {
                return relationship.TargetRelationshipId;
            }
        }

        return sourceRelationshipId;
    }

    internal string GetImagePartName(string sourceImagePartName)
    {
        if (sourceImagePartName == null)
        {
            throw new ArgumentNullException("sourceImagePartName");
        }

        string normalizedSourceImagePartName = DocxPartName.Normalize(sourceImagePartName);

        foreach (DocxMediaRelationshipMap relationship in this.Relationships)
        {
            if (relationship.SourceImagePartName == normalizedSourceImagePartName)
            {
                return relationship.TargetImagePartName;
            }
        }

        return normalizedSourceImagePartName;
    }
}

internal class DocxMediaRelationshipMap
{
    internal string SourcePartName;
    internal string SourceRelationshipId;
    internal string TargetSourcePartName;
    internal string TargetRelationshipId;
    internal string SourceImagePartName;
    internal string TargetImagePartName;

    internal DocxMediaRelationshipMap(string sourcePartName, string sourceRelationshipId, string targetSourcePartName, string targetRelationshipId, string sourceImagePartName, string targetImagePartName)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (sourceRelationshipId == null)
        {
            throw new ArgumentNullException("sourceRelationshipId");
        }

        if (targetSourcePartName == null)
        {
            throw new ArgumentNullException("targetSourcePartName");
        }

        if (targetRelationshipId == null)
        {
            throw new ArgumentNullException("targetRelationshipId");
        }

        if (sourceImagePartName == null)
        {
            throw new ArgumentNullException("sourceImagePartName");
        }

        if (targetImagePartName == null)
        {
            throw new ArgumentNullException("targetImagePartName");
        }

        this.SourcePartName = DocxImage.NormalizeSourcePartName(sourcePartName);
        this.SourceRelationshipId = sourceRelationshipId;
        this.TargetSourcePartName = DocxImage.NormalizeSourcePartName(targetSourcePartName);
        this.TargetRelationshipId = targetRelationshipId;
        this.SourceImagePartName = DocxPartName.Normalize(sourceImagePartName);
        this.TargetImagePartName = DocxPartName.Normalize(targetImagePartName);
    }
}
