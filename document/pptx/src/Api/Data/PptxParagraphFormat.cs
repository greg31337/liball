using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Describes typed paragraph properties after reading their direct DrawingML values.
/// </summary>
public class PptxParagraphFormat
{
    /// <summary>
    /// Stores the horizontal paragraph alignment.
    /// </summary>
    public string Alignment;
    /// <summary>
    /// Stores the paragraph outline level.
    /// </summary>
    public int Level;
    /// <summary>
    /// Stores the left paragraph margin in English Metric Units.
    /// </summary>
    public long MarginLeft;
    /// <summary>
    /// Stores the right paragraph margin in English Metric Units.
    /// </summary>
    public long MarginRight;
    /// <summary>
    /// Stores the first-line indentation in English Metric Units.
    /// </summary>
    public long Indent;
    /// <summary>
    /// Stores the default tab size in English Metric Units.
    /// </summary>
    public long DefaultTabSize;
    /// <summary>
    /// Stores line spacing.
    /// </summary>
    public PptxTextSpacing LineSpacing;
    /// <summary>
    /// Stores spacing before the paragraph.
    /// </summary>
    public PptxTextSpacing SpaceBefore;
    /// <summary>
    /// Stores spacing after the paragraph.
    /// </summary>
    public PptxTextSpacing SpaceAfter;
    /// <summary>
    /// Contains direct paragraph tab stops.
    /// </summary>
    public List<PptxTextTab> Tabs;
    /// <summary>
    /// Stores direct default run properties.
    /// </summary>
    public PptxFont DefaultFont;
    /// <summary>
    /// Stores the direct bullet type.
    /// </summary>
    public string BulletType;
    /// <summary>
    /// Stores the direct character bullet.
    /// </summary>
    public string BulletCharacter;
    /// <summary>
    /// Stores the automatic numbering scheme.
    /// </summary>
    public string NumberingType;
    /// <summary>
    /// Stores the starting value for automatic numbering.
    /// </summary>
    public int NumberingStartAt;
    /// <summary>
    /// Stores the relationship identifier of a preserved picture bullet.
    /// </summary>
    public string PictureBulletRelationshipId;

    /// <summary>
    /// Initializes empty direct paragraph formatting.
    /// </summary>
    public PptxParagraphFormat()
    {
        Alignment = "";
        Level = 0;
        MarginLeft = 0L;
        MarginRight = 0L;
        Indent = 0L;
        DefaultTabSize = 0L;
        LineSpacing = new PptxTextSpacing();
        SpaceBefore = new PptxTextSpacing();
        SpaceAfter = new PptxTextSpacing();
        Tabs = new List<PptxTextTab>();
        DefaultFont = new PptxFont();
        BulletType = PptxBullet.None;
        BulletCharacter = "";
        NumberingType = "";
        NumberingStartAt = 0;
        PictureBulletRelationshipId = "";
    }
}
