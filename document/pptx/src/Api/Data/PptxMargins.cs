namespace pptx;

/// <summary>
/// Stores rectangular margins in English Metric Units (EMUs).
/// </summary>
public class PptxMargins
{
    /// <summary>
    /// Stores the left margin in English Metric Units (EMUs).
    /// </summary>
    public long Left;
    /// <summary>
    /// Stores the top margin in English Metric Units (EMUs).
    /// </summary>
    public long Top;
    /// <summary>
    /// Stores the right margin in English Metric Units (EMUs).
    /// </summary>
    public long Right;
    /// <summary>
    /// Stores the bottom margin in English Metric Units (EMUs).
    /// </summary>
    public long Bottom;

    /// <summary>
    /// Initializes margins using the supplied edge distances.
    /// </summary>
    /// <param name="left">The left margin in English Metric Units (EMUs).</param>
    /// <param name="top">The top margin in English Metric Units (EMUs).</param>
    /// <param name="right">The right margin in English Metric Units (EMUs).</param>
    /// <param name="bottom">The bottom margin in English Metric Units (EMUs).</param>
    public PptxMargins(long left, long top, long right, long bottom)
    {
        if (left < 0L || top < 0L || right < 0L || bottom < 0L)
        {
            throw new System.ArgumentOutOfRangeException("left", "Margins cannot be negative.");
        }

        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}
