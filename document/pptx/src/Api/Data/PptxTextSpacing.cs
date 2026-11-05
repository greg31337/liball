namespace pptx;

/// <summary>
/// Describes paragraph spacing as points, a percentage, or an inherited value.
/// </summary>
public class PptxTextSpacing
{
    /// <summary>
    /// Identifies spacing measured in hundredths of one point.
    /// </summary>
    public const string Points = "points";
    /// <summary>
    /// Identifies spacing measured in thousandths of one percent.
    /// </summary>
    public const string Percent = "percent";
    /// <summary>
    /// Stores <c>points</c>, <c>percent</c>, or an empty inherited value.
    /// </summary>
    public string Unit;
    /// <summary>
    /// Stores the spacing value in the units selected by <see cref="Unit"/>.
    /// </summary>
    public int Value;

    /// <summary>
    /// Initializes inherited paragraph spacing.
    /// </summary>
    public PptxTextSpacing()
    {
        Unit = "";
        Value = 0;
    }
}
