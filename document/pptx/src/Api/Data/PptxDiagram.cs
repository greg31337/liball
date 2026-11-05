using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Represents diagram data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxDiagram
{
    internal PptxShape Shape;
    /// <summary>
    /// Stores the data relationship id used to identify or connect related presentation data.
    /// </summary>
    public string DataRelationshipId;
    /// <summary>
    /// Stores the layout relationship id used to identify or connect related presentation data.
    /// </summary>
    public string LayoutRelationshipId;
    /// <summary>
    /// Stores the quick style relationship id used to identify or connect related presentation data.
    /// </summary>
    public string QuickStyleRelationshipId;
    /// <summary>
    /// Stores the colors relationship id used to identify or connect related presentation data.
    /// </summary>
    public string ColorsRelationshipId;
    internal PptxPart? DataPart;
    internal PptxPart? LayoutPart;
    internal PptxPart? QuickStylePart;
    internal PptxPart? ColorsPart;
    internal PptxDiagram(PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        if (!shape.IsDiagram())
        {
            throw new System.ArgumentException("Shape is not a diagram.");
        }

        Shape = shape;
        DataRelationshipId = "";
        LayoutRelationshipId = "";
        QuickStyleRelationshipId = "";
        ColorsRelationshipId = "";
        DataPart = null;
        LayoutPart = null;
        QuickStylePart = null;
        ColorsPart = null;
        Load();
    }

    internal void Load()
    {
        DataRelationshipId = Shape.GetDiagramRelationshipId("dm");
        LayoutRelationshipId = Shape.GetDiagramRelationshipId("lo");
        QuickStyleRelationshipId = Shape.GetDiagramRelationshipId("qs");
        ColorsRelationshipId = Shape.GetDiagramRelationshipId("cs");
        DataPart = ResolvePart(DataRelationshipId);
        LayoutPart = ResolvePart(LayoutRelationshipId);
        QuickStylePart = ResolvePart(QuickStyleRelationshipId);
        ColorsPart = ResolvePart(ColorsRelationshipId);
    }
    internal string ExtractText()
    {
        if (DataPart == null)
        {
            return "";
        }

        if (Shape.Tree.Slide.Presentation == null)
        {
            return "";
        }

        return PptxTextExtractorCore.ExtractPartText(Shape.Tree.Slide.Presentation, DataPart);
    }
    internal List<PptxPart> GetParts()
    {
        List<PptxPart> parts = new List<PptxPart>();
        AddPart(parts, DataPart);
        AddPart(parts, LayoutPart);
        AddPart(parts, QuickStylePart);
        AddPart(parts, ColorsPart);
        return parts;
    }
    internal PptxPart? ResolvePart(string relationshipId)
    {
        string clean = PptxDiagnostic.CleanText(relationshipId);
        if (clean.Length == 0)
        {
            return null;
        }

        if (Shape.Tree.Slide.Presentation == null)
        {
            return null;
        }

        PptxRelationship? relationship = Shape.Tree.FindRelationshipById(clean);
        if (relationship == null)
        {
            return null;
        }

        if (!relationship.IsInternal())
        {
            return null;
        }

        if (!Shape.Tree.Slide.Presentation.Package.HasPart(relationship.TargetPartName))
        {
            return null;
        }

        return Shape.Tree.Slide.Presentation.Package.GetPart(relationship.TargetPartName);
    }

    internal static void AddPart(List<PptxPart> parts, PptxPart? part)
    {
        if (part != null)
        {
            parts.Add(part);
        }
    }
}
