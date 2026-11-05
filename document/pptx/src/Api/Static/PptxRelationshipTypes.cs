namespace pptx;

/// <summary>
/// Provides the relationship type identifiers used by PowerPoint package parts.
/// </summary>
public static class PptxRelationshipTypes
{
    /// <summary>
    /// Identifies the office document value accepted by the API.
    /// </summary>
    public const string OfficeDocument = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
    /// <summary>
    /// Identifies the core properties value accepted by the API.
    /// </summary>
    public const string CoreProperties = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties";
    /// <summary>
    /// Identifies the extended properties value accepted by the API.
    /// </summary>
    public const string ExtendedProperties = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties";
    /// <summary>
    /// Identifies the custom properties value accepted by the API.
    /// </summary>
    public const string CustomProperties = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/custom-properties";
    /// <summary>
    /// Identifies the thumbnail value accepted by the API.
    /// </summary>
    public const string Thumbnail = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail";
    internal const string Presentation = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
    /// <summary>
    /// Identifies the presentation properties value accepted by the API.
    /// </summary>
    public const string PresentationProperties = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/presProps";
    /// <summary>
    /// Identifies the view properties value accepted by the API.
    /// </summary>
    public const string ViewProperties = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/viewProps";
    /// <summary>
    /// Identifies the table styles value accepted by the API.
    /// </summary>
    public const string TableStyles = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/tableStyles";
    internal const string Slide = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide";
    /// <summary>
    /// Identifies the slide master value accepted by the API.
    /// </summary>
    public const string SlideMaster = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideMaster";
    /// <summary>
    /// Identifies the slide layout value accepted by the API.
    /// </summary>
    public const string SlideLayout = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout";
    /// <summary>
    /// Identifies the theme value accepted by the API.
    /// </summary>
    public const string Theme = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme";
    /// <summary>
    /// Identifies the notes slide value accepted by the API.
    /// </summary>
    public const string NotesSlide = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/notesSlide";
    /// <summary>
    /// Identifies the notes master value accepted by the API.
    /// </summary>
    public const string NotesMaster = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/notesMaster";
    /// <summary>
    /// Identifies the handout master value accepted by the API.
    /// </summary>
    public const string HandoutMaster = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/handoutMaster";
    /// <summary>
    /// Identifies the tags value accepted by the API.
    /// </summary>
    public const string Tags = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/tags";
    /// <summary>
    /// Identifies the custom xml value accepted by the API.
    /// </summary>
    public const string CustomXml = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/customXml";
    /// <summary>
    /// Identifies the image value accepted by the API.
    /// </summary>
    public const string Image = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";
    /// <summary>
    /// Identifies the hyperlink value accepted by the API.
    /// </summary>
    public const string Hyperlink = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink";
    /// <summary>
    /// Identifies the chart value accepted by the API.
    /// </summary>
    public const string Chart = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart";
    /// <summary>
    /// Identifies the chart style value accepted by the API.
    /// </summary>
    public const string ChartStyle = "http://schemas.microsoft.com/office/2011/relationships/chartStyle";
    /// <summary>
    /// Identifies the chart color style value accepted by the API.
    /// </summary>
    public const string ChartColorStyle = "http://schemas.microsoft.com/office/2011/relationships/chartColorStyle";
    /// <summary>
    /// Identifies the diagram data value accepted by the API.
    /// </summary>
    public const string DiagramData = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/diagramData";
    /// <summary>
    /// Identifies the diagram layout value accepted by the API.
    /// </summary>
    public const string DiagramLayout = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/diagramLayout";
    /// <summary>
    /// Identifies the diagram quick style value accepted by the API.
    /// </summary>
    public const string DiagramQuickStyle = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/diagramQuickStyle";
    /// <summary>
    /// Identifies the diagram colors value accepted by the API.
    /// </summary>
    public const string DiagramColors = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/diagramColors";
    /// <summary>
    /// Identifies the comments value accepted by the API.
    /// </summary>
    public const string Comments = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/comments";
    /// <summary>
    /// Identifies the modern comments value accepted by the API.
    /// </summary>
    public const string ModernComments = "http://schemas.microsoft.com/office/2018/10/relationships/comments";
    /// <summary>
    /// Identifies the comment authors value accepted by the API.
    /// </summary>
    public const string CommentAuthors = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/commentAuthors";
    /// <summary>
    /// Identifies the vba project value accepted by the API.
    /// </summary>
    public const string VbaProject = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/vbaProject";
    /// <summary>
    /// Identifies the audio value accepted by the API.
    /// </summary>
    public const string Audio = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/audio";
    /// <summary>
    /// Identifies the video value accepted by the API.
    /// </summary>
    public const string Video = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/video";
    /// <summary>
    /// Identifies the ole object value accepted by the API.
    /// </summary>
    public const string OleObject = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/oleObject";
    /// <summary>
    /// Identifies an embedded package relationship.
    /// </summary>
    public const string Package = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/package";
    /// <summary>
    /// Identifies an ActiveX control persistence relationship.
    /// </summary>
    public const string ActiveXControl = "http://schemas.microsoft.com/office/2006/relationships/activeXControl";
    /// <summary>
    /// Identifies an ActiveX control binary-property relationship.
    /// </summary>
    public const string ActiveXControlBinary = "http://schemas.microsoft.com/office/2006/relationships/activeXControlBinary";
}
