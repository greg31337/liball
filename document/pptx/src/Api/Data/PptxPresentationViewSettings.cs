using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Represents presentation-wide editing view settings.
/// </summary>
public class PptxPresentationViewSettings
{
    /// <summary>Stores the last active presentation view name.</summary>
    public string LastView;
    /// <summary>Indicates whether comments are shown in the presentation view.</summary>
    public bool ShowComments;
    /// <summary>Stores the horizontal grid spacing.</summary>
    public long GridSpacingX;
    /// <summary>Stores the vertical grid spacing.</summary>
    public long GridSpacingY;
    /// <summary>Indicates whether editing guides are shown.</summary>
    public bool ShowGuides;
    /// <summary>Indicates whether objects snap to the editing grid.</summary>
    public bool SnapToGrid;
    /// <summary>Indicates whether objects snap to other objects.</summary>
    public bool SnapToObjects;
    /// <summary>Contains the slide-view editing guides.</summary>
    public List<PptxPresentationGuide> Guides;

    /// <summary>
    /// Initializes presentation view settings with safe defaults.
    /// </summary>
    public PptxPresentationViewSettings()
    {
        LastView = "";
        ShowComments = false;
        GridSpacingX = 0L;
        GridSpacingY = 0L;
        ShowGuides = false;
        SnapToGrid = false;
        SnapToObjects = false;
        Guides = new List<PptxPresentationGuide>();
    }
}
