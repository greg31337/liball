namespace pptx;

/// <summary>
/// Summarizes editable presentation-model content without exposing parser implementation types.
/// </summary>
public class PptxEditableModelSummary
{
    /// <summary>Stores the slide count.</summary>
    public int SlideCount;
    /// <summary>Stores the slide width in English Metric Units.</summary>
    public long SlideWidth;
    /// <summary>Stores the slide height in English Metric Units.</summary>
    public long SlideHeight;
    /// <summary>Stores the declared slide-size type.</summary>
    public string SlideSizeType;
    /// <summary>Stores the slide master count.</summary>
    public int SlideMasterCount;
    /// <summary>Stores the slide layout count.</summary>
    public int SlideLayoutCount;
    /// <summary>Stores the theme count.</summary>
    public int ThemeCount;
    /// <summary>Stores the section count.</summary>
    public int SectionCount;
    /// <summary>Stores the custom-show count.</summary>
    public int CustomShowCount;
    /// <summary>Stores the total shape count across slides.</summary>
    public int ShapeCount;
    /// <summary>Stores the total placeholder count across slides.</summary>
    public int PlaceholderCount;
    /// <summary>Stores the total embedded image count across slides.</summary>
    public int ImageCount;
    /// <summary>Stores the total table count across slides.</summary>
    public int TableCount;
    /// <summary>Stores the total chart count across slides.</summary>
    public int ChartCount;
    /// <summary>Stores the total diagram count across slides.</summary>
    public int DiagramCount;
    /// <summary>Stores the number of slides with notes.</summary>
    public int NotesSlideCount;
    /// <summary>Stores the legacy comment count across slides.</summary>
    public int CommentCount;
    /// <summary>Stores the modern comment count across slides.</summary>
    public int ModernCommentCount;
    /// <summary>Stores the media count.</summary>
    public int MediaCount;
    /// <summary>Stores the embedded object count.</summary>
    public int EmbeddedObjectCount;
    /// <summary>Stores the animation count.</summary>
    public int AnimationCount;
    /// <summary>Stores the transition count.</summary>
    public int TransitionCount;
    /// <summary>Indicates whether the editable model contains a notes master.</summary>
    public bool HasNotesMaster;
    /// <summary>Indicates whether the editable model contains a handout master.</summary>
    public bool HasHandoutMaster;
    /// <summary>Indicates whether the package contains macros.</summary>
    public bool HasMacros;

    /// <summary>
    /// Initializes an empty editable-model summary.
    /// </summary>
    public PptxEditableModelSummary()
    {
        SlideCount = 0;
        SlideWidth = 0L;
        SlideHeight = 0L;
        SlideSizeType = "";
        SlideMasterCount = 0;
        SlideLayoutCount = 0;
        ThemeCount = 0;
        SectionCount = 0;
        CustomShowCount = 0;
        ShapeCount = 0;
        PlaceholderCount = 0;
        ImageCount = 0;
        TableCount = 0;
        ChartCount = 0;
        DiagramCount = 0;
        NotesSlideCount = 0;
        CommentCount = 0;
        ModernCommentCount = 0;
        MediaCount = 0;
        EmbeddedObjectCount = 0;
        AnimationCount = 0;
        TransitionCount = 0;
        HasNotesMaster = false;
        HasHandoutMaster = false;
        HasMacros = false;
    }
}
