using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Represents slide master data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxSlideMaster
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
    /// Stores the name associated with this slide master.
    /// </summary>
    public string Name;
    /// <summary>
    /// Contains the layouts in package or presentation order.
    /// </summary>
    public List<PptxSlideLayout> Layouts;
    /// <summary>
    /// Contains the themes in package or presentation order.
    /// </summary>
    public List<PptxTheme> Themes;
    /// <summary>
    /// Contains the placeholders in package or presentation order.
    /// </summary>
    public List<PptxPlaceholder> Placeholders;

    internal PptxSlideMaster(PptxPart part, string relationshipId)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        Part = part;
        PartName = part.PartName;
        RelationshipId = PptxDiagnostic.CleanText(relationshipId);
        Name = "";
        Layouts = new List<PptxSlideLayout>();
        Themes = new List<PptxTheme>();
        Placeholders = new List<PptxPlaceholder>();
    }
    internal PptxPartShapeTree GetShapeTree()
    {
        return PptxPartInspectorCore.GetShapeTree(Part);
    }
}
