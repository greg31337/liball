namespace pptx;

/// <summary>
/// Describes effective drawing and text formatting after inheritance and theme resolution.
/// </summary>
public class PptxEffectiveStyle
{
    /// <summary>
    /// Stores the effective fill.
    /// </summary>
    public PptxFill Fill;
    /// <summary>
    /// Stores the effective line.
    /// </summary>
    public PptxLine Line;
    /// <summary>
    /// Stores the effective effects.
    /// </summary>
    public PptxEffect Effect;
    /// <summary>
    /// Stores the effective slide background.
    /// </summary>
    public PptxFill Background;
    /// <summary>
    /// Stores the effective text color.
    /// </summary>
    public PptxColor TextColor;
    /// <summary>
    /// Stores the effective font.
    /// </summary>
    public PptxFont Font;
    /// <summary>
    /// Stores the level supplying the fill.
    /// </summary>
    public string FillSource;
    /// <summary>
    /// Stores the level supplying the line.
    /// </summary>
    public string LineSource;
    /// <summary>
    /// Stores the level supplying the effects.
    /// </summary>
    public string EffectSource;
    /// <summary>
    /// Stores the level supplying the background.
    /// </summary>
    public string BackgroundSource;
    /// <summary>
    /// Stores the level supplying text formatting.
    /// </summary>
    public string TextSource;

    /// <summary>
    /// Initializes an empty effective style.
    /// </summary>
    public PptxEffectiveStyle()
    {
        Fill = new PptxFill();
        Line = new PptxLine();
        Effect = new PptxEffect();
        Background = new PptxFill();
        TextColor = new PptxColor();
        Font = new PptxFont();
        FillSource = "";
        LineSource = "";
        EffectSource = "";
        BackgroundSource = "";
        TextSource = "";
    }
}
