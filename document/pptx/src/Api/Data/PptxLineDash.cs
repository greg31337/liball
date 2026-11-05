namespace pptx;

/// <summary>
/// Represents one dash and space pair in a custom line dash pattern.
/// </summary>
public class PptxLineDash
{
    /// <summary>
    /// Stores dash length in thousandths of one percent of the line width.
    /// </summary>
    public int DashLength;
    /// <summary>
    /// Stores following space length in thousandths of one percent of the line width.
    /// </summary>
    public int SpaceLength;

    /// <summary>
    /// Initializes an empty custom dash pair.
    /// </summary>
    public PptxLineDash()
    {
        DashLength = 100000;
        SpaceLength = 100000;
    }

    internal static PptxLineDash Create(int dashLength, int spaceLength)
    {
        if (dashLength <= 0 || spaceLength <= 0)
        {
            throw new System.ArgumentOutOfRangeException("dashLength", "Dash and space lengths must be greater than zero.");
        }

        PptxLineDash dash = new PptxLineDash();
        dash.DashLength = dashLength;
        dash.SpaceLength = spaceLength;
        return dash;
    }
}
