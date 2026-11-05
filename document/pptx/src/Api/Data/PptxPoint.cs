namespace pptx;

/// <summary>
/// Stores a two-dimensional point in English Metric Units (EMUs).
/// </summary>
public class PptxPoint
{
    /// <summary>
    /// Stores the x in English Metric Units (EMUs).
    /// </summary>
    public long X;
    /// <summary>
    /// Stores the y in English Metric Units (EMUs).
    /// </summary>
    public long Y;

    /// <summary>
    /// Initializes a point using the supplied coordinates.
    /// </summary>
    /// <param name="x">The x value to use.</param>
    /// <param name="y">The y value to use.</param>
    public PptxPoint(long x, long y)
    {
        X = x;
        Y = y;
    }
    internal PptxBounds BoundsTo(PptxPoint end)
    {
        if (end == null)
        {
            throw new System.ArgumentNullException("end");
        }

        return new PptxBounds(X, Y, end.X - X, end.Y - Y);
    }
}

