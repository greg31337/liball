using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Represents editable presentation-wide view, numbering, text, timing, orientation, and slide-show settings.
/// </summary>
public class PptxPresentationSettings
{
    /// <summary>Stores editing view, grid, and guide settings.</summary>
    public PptxPresentationViewSettings View;
    /// <summary>Stores the complete presentation default text style.</summary>
    public PptxDefaultTextStyle DefaultTextStyle;
    /// <summary>Stores the number assigned to the first slide.</summary>
    public int FirstSlideNumber;
    /// <summary>Indicates whether slide-number placeholders are enabled on masters and layouts.</summary>
    public bool ShowSlideNumbers;
    /// <summary>Indicates whether special placeholders are shown on title slides.</summary>
    public bool ShowSpecialPlaceholdersOnTitleSlides;
    /// <summary>Stores <c>landscape</c> or <c>portrait</c>.</summary>
    public string Orientation;
    /// <summary>Indicates whether the slide show loops after its final slide.</summary>
    public bool Loop;
    /// <summary>Indicates whether the slide show uses recorded slide timings.</summary>
    public bool UseTimings;
    /// <summary>Contains slide advance settings keyed by slide identifier.</summary>
    public List<PptxSlideTimingSettings> SlideTimings;

    /// <summary>
    /// Initializes presentation settings with common defaults.
    /// </summary>
    public PptxPresentationSettings()
    {
        View = new PptxPresentationViewSettings();
        DefaultTextStyle = new PptxDefaultTextStyle();
        FirstSlideNumber = 1;
        ShowSlideNumbers = true;
        ShowSpecialPlaceholdersOnTitleSlides = true;
        Orientation = "landscape";
        Loop = false;
        UseTimings = true;
        SlideTimings = new List<PptxSlideTimingSettings>();
    }
}
