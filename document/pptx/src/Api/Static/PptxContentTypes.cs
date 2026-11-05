namespace pptx;

/// <summary>
/// Provides the MIME content-type strings used by common PowerPoint package parts.
/// </summary>
public static class PptxContentTypes
{
    /// <summary>
    /// Identifies the relationships value accepted by the API.
    /// </summary>
    public const string Relationships = "application/vnd.openxmlformats-package.relationships+xml";
    internal const string Xml = "application/xml";
    /// <summary>
    /// Identifies the binary value accepted by the API.
    /// </summary>
    public const string Binary = "application/octet-stream";
    /// <summary>
    /// Identifies the core properties value accepted by the API.
    /// </summary>
    public const string CoreProperties = "application/vnd.openxmlformats-package.core-properties+xml";
    /// <summary>
    /// Identifies the extended properties value accepted by the API.
    /// </summary>
    public const string ExtendedProperties = "application/vnd.openxmlformats-officedocument.extended-properties+xml";
    /// <summary>
    /// Identifies the custom properties value accepted by the API.
    /// </summary>
    public const string CustomProperties = "application/vnd.openxmlformats-officedocument.custom-properties+xml";
    internal const string Presentation = "application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml";
    /// <summary>
    /// Identifies the macro enabled presentation value accepted by the API.
    /// </summary>
    public const string MacroEnabledPresentation = "application/vnd.ms-powerpoint.presentation.macroEnabled.main+xml";
    /// <summary>
    /// Identifies the vba project value accepted by the API.
    /// </summary>
    public const string VbaProject = "application/vnd.ms-office.vbaProject";
    /// <summary>
    /// Identifies the presentation properties value accepted by the API.
    /// </summary>
    public const string PresentationProperties = "application/vnd.openxmlformats-officedocument.presentationml.presProps+xml";
    /// <summary>
    /// Identifies the view properties value accepted by the API.
    /// </summary>
    public const string ViewProperties = "application/vnd.openxmlformats-officedocument.presentationml.viewProps+xml";
    /// <summary>
    /// Identifies the table styles value accepted by the API.
    /// </summary>
    public const string TableStyles = "application/vnd.openxmlformats-officedocument.presentationml.tableStyles+xml";
    internal const string Slide = "application/vnd.openxmlformats-officedocument.presentationml.slide+xml";
    /// <summary>
    /// Identifies the slide master value accepted by the API.
    /// </summary>
    public const string SlideMaster = "application/vnd.openxmlformats-officedocument.presentationml.slideMaster+xml";
    /// <summary>
    /// Identifies the slide layout value accepted by the API.
    /// </summary>
    public const string SlideLayout = "application/vnd.openxmlformats-officedocument.presentationml.slideLayout+xml";
    /// <summary>
    /// Identifies the theme value accepted by the API.
    /// </summary>
    public const string Theme = "application/vnd.openxmlformats-officedocument.theme+xml";
    /// <summary>
    /// Identifies the notes slide value accepted by the API.
    /// </summary>
    public const string NotesSlide = "application/vnd.openxmlformats-officedocument.presentationml.notesSlide+xml";
    /// <summary>
    /// Identifies the notes master value accepted by the API.
    /// </summary>
    public const string NotesMaster = "application/vnd.openxmlformats-officedocument.presentationml.notesMaster+xml";
    /// <summary>
    /// Identifies the handout master value accepted by the API.
    /// </summary>
    public const string HandoutMaster = "application/vnd.openxmlformats-officedocument.presentationml.handoutMaster+xml";
    /// <summary>
    /// Identifies the tags value accepted by the API.
    /// </summary>
    public const string Tags = "application/vnd.openxmlformats-officedocument.presentationml.tags+xml";
    /// <summary>
    /// Identifies the chart value accepted by the API.
    /// </summary>
    public const string Chart = "application/vnd.openxmlformats-officedocument.drawingml.chart+xml";
    /// <summary>
    /// Identifies the chart style value accepted by the API.
    /// </summary>
    public const string ChartStyle = "application/vnd.ms-office.chartstyle+xml";
    /// <summary>
    /// Identifies the chart color style value accepted by the API.
    /// </summary>
    public const string ChartColorStyle = "application/vnd.ms-office.chartcolorstyle+xml";
    /// <summary>
    /// Identifies the diagram data value accepted by the API.
    /// </summary>
    public const string DiagramData = "application/vnd.openxmlformats-officedocument.drawingml.diagramData+xml";
    /// <summary>
    /// Identifies the diagram layout value accepted by the API.
    /// </summary>
    public const string DiagramLayout = "application/vnd.openxmlformats-officedocument.drawingml.diagramLayout+xml";
    /// <summary>
    /// Identifies the diagram style value accepted by the API.
    /// </summary>
    public const string DiagramStyle = "application/vnd.openxmlformats-officedocument.drawingml.diagramStyle+xml";
    /// <summary>
    /// Identifies the diagram colors value accepted by the API.
    /// </summary>
    public const string DiagramColors = "application/vnd.openxmlformats-officedocument.drawingml.diagramColors+xml";
    /// <summary>
    /// Identifies the ole object value accepted by the API.
    /// </summary>
    public const string OleObject = "application/vnd.openxmlformats-officedocument.oleObject";
    /// <summary>
    /// Identifies an XML ActiveX control persistence part.
    /// </summary>
    public const string ActiveX = "application/vnd.ms-office.activeX+xml";
    /// <summary>
    /// Identifies a binary ActiveX control property part.
    /// </summary>
    public const string ActiveXBinary = "application/vnd.ms-office.activeX";
    /// <summary>
    /// Identifies the comments value accepted by the API.
    /// </summary>
    public const string Comments = "application/vnd.openxmlformats-officedocument.presentationml.comments+xml";
    /// <summary>
    /// Identifies the modern comments value accepted by the API.
    /// </summary>
    public const string ModernComments = "application/vnd.ms-powerpoint.comments+xml";
    /// <summary>
    /// Identifies the comment authors value accepted by the API.
    /// </summary>
    public const string CommentAuthors = "application/vnd.openxmlformats-officedocument.presentationml.commentAuthors+xml";
    /// <summary>
    /// Identifies the png value accepted by the API.
    /// </summary>
    public const string Png = "image/png";
    /// <summary>
    /// Identifies the jpeg value accepted by the API.
    /// </summary>
    public const string Jpeg = "image/jpeg";
    /// <summary>
    /// Identifies the gif value accepted by the API.
    /// </summary>
    public const string Gif = "image/gif";
    /// <summary>
    /// Identifies the bmp value accepted by the API.
    /// </summary>
    public const string Bmp = "image/bmp";
    /// <summary>
    /// Identifies the tiff value accepted by the API.
    /// </summary>
    public const string Tiff = "image/tiff";
    /// <summary>
    /// Identifies the svg value accepted by the API.
    /// </summary>
    public const string Svg = "image/svg+xml";
    /// <summary>
    /// Identifies the emf value accepted by the API.
    /// </summary>
    public const string Emf = "image/x-emf";
    /// <summary>
    /// Identifies the wmf value accepted by the API.
    /// </summary>
    public const string Wmf = "image/x-wmf";
    /// <summary>
    /// Identifies the mp3 value accepted by the API.
    /// </summary>
    public const string Mp3 = "audio/mpeg";
    /// <summary>
    /// Identifies the mp4 value accepted by the API.
    /// </summary>
    public const string Mp4 = "video/mp4";
}
