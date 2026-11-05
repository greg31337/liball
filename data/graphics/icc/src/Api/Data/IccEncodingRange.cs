namespace icc;

/// <summary>Represents the numeric domain or range of one channel.</summary>
public class IccEncodingRange
{
    /// <summary>Gets or sets the minimum value.</summary>
    public double Minimum;
    /// <summary>Gets or sets the maximum value.</summary>
    public double Maximum;

    /// <summary>Initializes a normalized zero-to-one range.</summary>
    public IccEncodingRange()
    {
        Minimum = 0.0;
        Maximum = 1.0;
    }
}
