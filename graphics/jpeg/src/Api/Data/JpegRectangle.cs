namespace jpeg;

/// <summary>Describes an integer image rectangle using a top-left origin.</summary>
public class JpegRectangle
{
    /// <summary>Creates an empty rectangle.</summary>
    public JpegRectangle()
    {
    }

    /// <summary>The horizontal coordinate of the left edge.</summary>
    public int X;
    /// <summary>The vertical coordinate of the top edge.</summary>
    public int Y;
    /// <summary>The rectangle width.</summary>
    public int Width;
    /// <summary>The rectangle height.</summary>
    public int Height;
}
