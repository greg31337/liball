namespace pptx;

/// <summary>
/// Represents embedded object data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxEmbeddedObject
{
    internal PptxShape Shape;
    internal PptxRelationship? Relationship;
    internal PptxPart? Part;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the relationship type associated with this object.
    /// </summary>
    public string RelationshipType;
    /// <summary>
    /// Stores the target associated with this object.
    /// </summary>
    public string Target;
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
    /// Stores the program id used to identify or connect related presentation data.
    /// </summary>
    public string ProgramId;
    /// <summary>
    /// Stores the name associated with this object.
    /// </summary>
    public string Name;
    /// <summary>
    /// Indicates whether the show as icon setting is enabled.
    /// </summary>
    public bool ShowAsIcon;
    /// <summary>
    /// Indicates whether the embedded setting is enabled.
    /// </summary>
    public bool Embedded;
    /// <summary>
    /// Indicates whether the linked setting is enabled.
    /// </summary>
    public bool Linked;

    internal PptxEmbeddedObject(PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        if (!shape.IsEmbeddedObject())
        {
            throw new System.ArgumentException("Shape is not an embedded object.");
        }

        Shape = shape;
        Relationship = null;
        Part = null;
        RelationshipId = "";
        RelationshipType = "";
        Target = "";
        PartName = "";
        ContentType = "";
        Bytes = new byte[0];
        ProgramId = "";
        Name = "";
        ShowAsIcon = false;
        Embedded = false;
        Linked = false;
        Load();
    }

    internal void Load()
    {
        RelationshipId = Shape.GetEmbeddedObjectRelationshipId();
        ProgramId = Shape.GetEmbeddedObjectProgramId();
        Name = Shape.GetEmbeddedObjectName();
        ShowAsIcon = Shape.GetEmbeddedObjectShowAsIcon();
        Relationship = ResolveRelationship(RelationshipId);
        RelationshipType = "";
        Target = "";
        PartName = "";
        ContentType = "";
        Bytes = new byte[0];
        Embedded = false;
        Linked = false;
        Part = null;

        if (Relationship == null)
        {
            return;
        }

        RelationshipType = Relationship.Type;
        Target = Relationship.Target;
        Embedded = Relationship.TargetMode == PptxRelationshipTargetMode.Internal;
        Linked = Relationship.TargetMode == PptxRelationshipTargetMode.External;
        if (!Embedded)
        {
            return;
        }

        PartName = Relationship.TargetPartName;
        if (Shape.Tree.Slide.Presentation == null)
        {
            return;
        }

        if (!Shape.Tree.Slide.Presentation.Package.HasPart(Relationship.TargetPartName))
        {
            return;
        }

        Part = Shape.Tree.Slide.Presentation.Package.GetPart(Relationship.TargetPartName);
        ContentType = Part.ContentType;
        Bytes = Part.GetBytes();
    }
    internal bool IsOleObject()
    {
        if (RelationshipType == PptxRelationshipTypes.OleObject)
        {
            return true;
        }

        if (Part != null && PptxSecurityScannerCore.IsOleObjectPart(Part))
        {
            return true;
        }

        return false;
    }
    internal bool IsEmbeddedPackage()
    {
        if (RelationshipType == PptxRelationshipTypes.Package)
        {
            return true;
        }

        if (Part != null && PptxSecurityScannerCore.IsEmbeddedPackagePart(Part))
        {
            return true;
        }

        return false;
    }
    internal byte[] GetBytes()
    {
        if (Part == null)
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

        if (!Embedded || Part == null)
        {
            throw new System.ArgumentException("Embedded object part was not found.");
        }

        Part.SetBytes(bytes);
        Part.SetContentType(contentType);
        if (Shape.Tree.Slide.Presentation != null)
        {
            Shape.Tree.Slide.Presentation.Package.EnsureContentTypeOverride(Part.PartName, Part.ContentType);
        }

        Load();
    }
    internal void Remove()
    {
        if (Shape.Tree.Slide.Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        if (Relationship == null)
        {
            throw new System.ArgumentException("Embedded object relationship was not found.");
        }

        PptxPresentation presentation = Shape.Tree.Slide.Presentation;
        string partName = Relationship.TargetPartName;
        presentation.Package.RemoveRelationship(Shape.Tree.Slide.PartName, Relationship.Id);
        Shape.Tree.Remove(Shape);
        presentation.RemovePartWhenUnreferenced(partName);
        Load();
    }
    internal PptxResult ScanSecurity()
    {
        PptxResult result = new PptxResult();
        if (Relationship != null)
        {
            PptxSecurityScannerCore.ScanRelationship(Relationship, result);
        }

        if (Part != null)
        {
            PptxSecurityScannerCore.ScanPart(Part, result);
        }

        return result;
    }
    internal PptxRelationship? ResolveRelationship(string relationshipId)
    {
        string clean = PptxDiagnostic.CleanText(relationshipId);
        if (clean.Length == 0)
        {
            return null;
        }

        return Shape.Tree.FindRelationshipById(clean);
    }
}
