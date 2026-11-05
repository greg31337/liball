using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Represents slide layout data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxSlideLayout
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
    /// Stores the slide master when one is related; otherwise, <c>null</c>.
    /// </summary>
    public PptxSlideMaster? Master;
    /// <summary>
    /// Stores the name associated with this object.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the type associated with this object.
    /// </summary>
    public string Type;
    /// <summary>
    /// Contains the placeholders in package or presentation order.
    /// </summary>
    public List<PptxPlaceholder> Placeholders;
    /// <summary>
    /// Contains the effective placeholders in package or presentation order.
    /// </summary>
    public List<PptxPlaceholder> EffectivePlaceholders;

    internal PptxSlideLayout(PptxPart part, string relationshipId)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        Part = part;
        PartName = part.PartName;
        RelationshipId = PptxDiagnostic.CleanText(relationshipId);
        Master = null;
        Name = "";
        Type = "";
        Placeholders = new List<PptxPlaceholder>();
        EffectivePlaceholders = new List<PptxPlaceholder>();
    }
    internal PptxPartShapeTree GetShapeTree()
    {
        return PptxPartInspectorCore.GetShapeTree(Part);
    }
}
