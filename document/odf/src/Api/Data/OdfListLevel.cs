namespace odf;

/// <summary>
/// Describes one numbering, bullet, image, or outline level.
/// </summary>
public class OdfListLevel
{
    /// <summary>
    /// The level label mechanism.
    /// </summary>
    public OdfListLevelKind Kind;
    /// <summary>
    /// The preserved one-based level number.
    /// </summary>
    public string Level = "";
    /// <summary>
    /// The optional text style used for the label.
    /// </summary>
    public string StyleName = "";
    /// <summary>
    /// The numbering format.
    /// </summary>
    public string NumberFormat = "";
    /// <summary>
    /// The numbering prefix.
    /// </summary>
    public string NumberPrefix = "";
    /// <summary>
    /// The numbering suffix.
    /// </summary>
    public string NumberSuffix = "";
    /// <summary>
    /// The preserved letter-synchronization flag.
    /// </summary>
    public string NumberLetterSync = "";
    /// <summary>
    /// The optional named number-list format.
    /// </summary>
    public string NumberListFormatName = "";
    /// <summary>
    /// The number of ancestor levels displayed in a label.
    /// </summary>
    public string DisplayLevels = "";
    /// <summary>
    /// The starting number for this level.
    /// </summary>
    public string StartValue = "";
    /// <summary>
    /// The bullet character.
    /// </summary>
    public string BulletCharacter = "";
    /// <summary>
    /// The bullet size relative to the surrounding text.
    /// </summary>
    public string BulletRelativeSize = "";
    /// <summary>
    /// The linked image resource reference.
    /// </summary>
    public string ImageHref = "";
    /// <summary>
    /// The list-level positioning mode.
    /// </summary>
    public string PositionAndSpaceMode = "";
    /// <summary>
    /// The legacy space before the label.
    /// </summary>
    public string SpaceBefore = "";
    /// <summary>
    /// The legacy minimum label width.
    /// </summary>
    public string MinimumLabelWidth = "";
    /// <summary>
    /// The legacy minimum distance after the label.
    /// </summary>
    public string MinimumLabelDistance = "";
    /// <summary>
    /// The content inserted after a label.
    /// </summary>
    public string LabelFollowedBy = "";
    /// <summary>
    /// The tab-stop position following a label.
    /// </summary>
    public string ListTabStopPosition = "";
    /// <summary>
    /// The label-alignment text indent.
    /// </summary>
    public string LabelTextIndent = "";
    /// <summary>
    /// The label-alignment left margin.
    /// </summary>
    public string LabelMarginLeft = "";
    /// <summary>
    /// All qualified level attributes and preserved lexical values.
    /// </summary>
    public OdfStyleProperty[] Attributes = [];
    /// <summary>
    /// The list-level positioning and spacing properties.
    /// </summary>
    public OdfStyleProperty[] LevelProperties = [];
    /// <summary>
    /// Child content of the list-level-properties element.
    /// </summary>
    public OdfNode[] LevelPropertyChildren = [];
    /// <summary>
    /// The underlying list-level-properties element.
    /// </summary>
    public OdfNode? LevelPropertiesNode;
    /// <summary>
    /// The label-alignment rule, when present.
    /// </summary>
    public OdfNode? LabelAlignmentNode;
    /// <summary>
    /// Text formatting applied to the level label.
    /// </summary>
    public OdfStyleProperty[] TextProperties = [];
    /// <summary>
    /// Child content of the text-properties element.
    /// </summary>
    public OdfNode[] TextPropertyChildren = [];
    /// <summary>
    /// The underlying text-properties element.
    /// </summary>
    public OdfNode? TextPropertiesNode;
    /// <summary>
    /// The embedded image-data element, when present.
    /// </summary>
    public OdfNode? ImageDataNode;
    /// <summary>
    /// Other level content retained in source order.
    /// </summary>
    public OdfNode[] Content = [];
    /// <summary>
    /// The underlying level-style element.
    /// </summary>
    public OdfNode? Node;
    internal OdfNode? OriginalNode;
    internal OdfAttribute[] OriginalAttributes = [];
    internal OdfNode? OriginalLevelPropertiesNode;
    internal OdfAttribute[] OriginalLevelPropertyAttributes = [];
    internal OdfNode[] OriginalLevelPropertyChildren = [];
    internal OdfNode? OriginalTextPropertiesNode;
    internal OdfAttribute[] OriginalTextPropertyAttributes = [];
    internal OdfNode[] OriginalTextPropertyChildren = [];
    internal OdfNode? OriginalLabelAlignmentNode;
    internal OdfNode? OriginalImageDataNode;
    internal OdfNode[] OriginalContent = [];
    internal OdfListLevelKind OriginalKind;
    internal string OriginalLevel = "";
    internal string OriginalStyleName = "";
    internal string OriginalNumberFormat = "";
    internal string OriginalNumberPrefix = "";
    internal string OriginalNumberSuffix = "";
    internal string OriginalNumberLetterSync = "";
    internal string OriginalNumberListFormatName = "";
    internal string OriginalDisplayLevels = "";
    internal string OriginalStartValue = "";
    internal string OriginalBulletCharacter = "";
    internal string OriginalBulletRelativeSize = "";
    internal string OriginalImageHref = "";
    internal string OriginalPositionAndSpaceMode = "";
    internal string OriginalSpaceBefore = "";
    internal string OriginalMinimumLabelWidth = "";
    internal string OriginalMinimumLabelDistance = "";
    internal string OriginalLabelFollowedBy = "";
    internal string OriginalListTabStopPosition = "";
    internal string OriginalLabelTextIndent = "";
    internal string OriginalLabelMarginLeft = "";
}
