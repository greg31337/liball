namespace pptx;

/// <summary>
/// Provides conventional package part names used by PowerPoint presentations.
/// </summary>
public static class PptxPartNames
{
    /// <summary>
    /// Identifies the content types entry value accepted by the API.
    /// </summary>
    public const string ContentTypesEntry = "[Content_Types].xml";
    /// <summary>
    /// Identifies the root relationships value accepted by the API.
    /// </summary>
    public const string RootRelationships = "/_rels/.rels";
    /// <summary>
    /// Identifies the core properties value accepted by the API.
    /// </summary>
    public const string CoreProperties = "/docProps/core.xml";
    /// <summary>
    /// Identifies the extended properties value accepted by the API.
    /// </summary>
    public const string ExtendedProperties = "/docProps/app.xml";
    /// <summary>
    /// Identifies the custom properties value accepted by the API.
    /// </summary>
    public const string CustomProperties = "/docProps/custom.xml";
    /// <summary>
    /// Identifies the conventional JPEG thumbnail part name.
    /// </summary>
    public const string Thumbnail = "/docProps/thumbnail.jpeg";
    internal const string Presentation = "/ppt/presentation.xml";
    /// <summary>
    /// Identifies the presentation properties value accepted by the API.
    /// </summary>
    public const string PresentationProperties = "/ppt/presProps.xml";
    /// <summary>
    /// Identifies the view properties value accepted by the API.
    /// </summary>
    public const string ViewProperties = "/ppt/viewProps.xml";
    /// <summary>
    /// Identifies the table styles value accepted by the API.
    /// </summary>
    public const string TableStyles = "/ppt/tableStyles.xml";
    /// <summary>
    /// Identifies the first slide value accepted by the API.
    /// </summary>
    public const string FirstSlide = "/ppt/slides/slide1.xml";
    /// <summary>
    /// Identifies the first slide layout value accepted by the API.
    /// </summary>
    public const string FirstSlideLayout = "/ppt/slideLayouts/slideLayout1.xml";
    /// <summary>
    /// Identifies the first slide master value accepted by the API.
    /// </summary>
    public const string FirstSlideMaster = "/ppt/slideMasters/slideMaster1.xml";
    /// <summary>
    /// Identifies the first theme value accepted by the API.
    /// </summary>
    public const string FirstTheme = "/ppt/theme/theme1.xml";
    /// <summary>
    /// Identifies the first notes slide value accepted by the API.
    /// </summary>
    public const string FirstNotesSlide = "/ppt/notesSlides/notesSlide1.xml";
    /// <summary>
    /// Identifies the first notes master value accepted by the API.
    /// </summary>
    public const string FirstNotesMaster = "/ppt/notesMasters/notesMaster1.xml";
    /// <summary>
    /// Identifies the first handout master value accepted by the API.
    /// </summary>
    public const string FirstHandoutMaster = "/ppt/handoutMasters/handoutMaster1.xml";
}
