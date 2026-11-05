namespace pptx;

/// <summary>
/// Represents one color stop in a gradient fill.
/// </summary>
public class PptxGradientStop
{
    /// <summary>
    /// Stores the stop position from zero through 100,000.
    /// </summary>
    public int Position;
    /// <summary>
    /// Stores the color at the stop position.
    /// </summary>
    public PptxColor Color;

    /// <summary>
    /// Initializes an empty gradient stop.
    /// </summary>
    public PptxGradientStop()
    {
        Position = 0;
        Color = new PptxColor();
    }

    internal static PptxGradientStop Create(int position, PptxColor color)
    {
        if (position < 0 || position > 100000)
        {
            throw new System.ArgumentOutOfRangeException("position", "Gradient stop positions must be between zero and 100,000.");
        }

        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        PptxGradientStop stop = new PptxGradientStop();
        stop.Position = position;
        stop.Color = color;
        return stop;
    }
}
