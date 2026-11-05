namespace pptx;

/// <summary>
/// Represents click and timed slide-advance settings for one slide.
/// </summary>
public class PptxSlideTimingSettings
{
    /// <summary>Stores the presentation slide identifier.</summary>
    public int SlideId;
    /// <summary>Indicates whether the slide advances on click.</summary>
    public bool AdvanceOnClick;
    /// <summary>Indicates whether the slide advances after a duration.</summary>
    public bool AdvanceAfter;
    /// <summary>Stores the timed-advance duration in milliseconds.</summary>
    public long AdvanceAfterMilliseconds;

    /// <summary>
    /// Initializes slide timing settings with click advance enabled.
    /// </summary>
    public PptxSlideTimingSettings()
    {
        SlideId = 0;
        AdvanceOnClick = true;
        AdvanceAfter = false;
        AdvanceAfterMilliseconds = 0L;
    }
}
