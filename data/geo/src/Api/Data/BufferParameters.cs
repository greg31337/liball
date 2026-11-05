namespace geo;

/// <summary>Specifies line cap and join policies for buffer construction.</summary>
public class BufferParameters
{
    /// <summary>The cap style applied to open line ends.</summary>
    public BufferCapStyle CapStyle = BufferCapStyle.Round;

    /// <summary>The join style applied between offset segments.</summary>
    public BufferJoinStyle JoinStyle = BufferJoinStyle.Round;

    /// <summary>The number of line segments used per circular quadrant.</summary>
    public int QuadrantSegments = 8;

    /// <summary>The maximum mitre ratio before a mitre join is bevelled.</summary>
    public double MitreLimit = 5.0;

    /// <summary>Whether a line buffer is generated on only one side.</summary>
    public bool SingleSided;

    /// <summary>The precision model applied while constructing the buffer.</summary>
    public PrecisionModel Precision = new PrecisionModel();
}
