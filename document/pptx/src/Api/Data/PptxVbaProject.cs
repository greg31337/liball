namespace pptx;

/// <summary>
/// Represents vba project data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxVbaProject
{
    internal PptxPresentation Presentation;
    internal PptxRelationship Relationship;
    internal PptxPart? Part;
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
    /// Contains the binary payload bytes. Callers should treat the array as mutable data.
    /// </summary>
    public byte[] Bytes;
    /// <summary>
    /// Indicates whether the present setting is enabled.
    /// </summary>
    public bool Present;

    internal PptxVbaProject(PptxPresentation presentation, PptxRelationship relationship)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        if (relationship == null)
        {
            throw new System.ArgumentNullException("relationship");
        }

        Presentation = presentation;
        Relationship = relationship;
        Part = null;
        RelationshipId = "";
        PartName = "";
        ContentType = "";
        Bytes = new byte[0];
        Present = false;
        Load();
    }

    internal void Load()
    {
        RelationshipId = Relationship.Id;
        Part = null;
        PartName = "";
        ContentType = "";
        Bytes = new byte[0];
        Present = false;

        if (Relationship.TargetMode != PptxRelationshipTargetMode.Internal)
        {
            return;
        }

        PartName = Relationship.TargetPartName;
        if (!Presentation.Package.HasPart(Relationship.TargetPartName))
        {
            return;
        }

        Part = Presentation.Package.GetPart(Relationship.TargetPartName);
        ContentType = Part.ContentType;
        Bytes = Part.GetBytes();
        Present = true;
    }
    internal byte[] GetBytes()
    {
        if (Part == null)
        {
            return new byte[0];
        }

        return Part.GetBytes();
    }
    internal void ReplaceBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        if (Part == null)
        {
            throw new System.ArgumentException("VBA project part was not found.");
        }

        Part.SetBytes(bytes);
        Part.SetContentType(PptxContentTypes.VbaProject);
        Presentation.Package.EnsureContentTypeOverride(Part.PartName, Part.ContentType);
        Presentation.EnsureMacroEnabledContentType();
        Load();
    }
    internal PptxResult ScanSecurity()
    {
        PptxResult result = new PptxResult();
        PptxSecurityScannerCore.ScanRelationship(Relationship, result);
        if (Part != null)
        {
            PptxSecurityScannerCore.ScanPart(Part, result);
        }

        return result;
    }
}
