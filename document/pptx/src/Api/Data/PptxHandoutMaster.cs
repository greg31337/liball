using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Represents handout master data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxHandoutMaster
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
    /// Contains the themes in package or presentation order.
    /// </summary>
    public List<PptxTheme> Themes;

    internal PptxHandoutMaster(PptxPart part, string relationshipId)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        Part = part;
        PartName = part.PartName;
        RelationshipId = PptxDiagnostic.CleanText(relationshipId);
        Themes = new List<PptxTheme>();
    }
    internal PptxPartMetadata GetMetadata()
    {
        return PptxPartInspectorCore.GetMetadata(Part, RelationshipId, Themes.Count);
    }
    internal PptxPartShapeTree GetShapeTree()
    {
        return PptxPartInspectorCore.GetShapeTree(Part);
    }
    internal PptxTheme? GetTheme()
    {
        if (Themes.Count == 0)
        {
            return null;
        }

        return Themes[0];
    }
}
