namespace icc;

/// <summary>Represents one indexed triangle in a gamut-boundary mesh.</summary>
public class IccGamutTriangle
{
    /// <summary>Gets or sets the first vertex index.</summary>
    public int First;
    /// <summary>Gets or sets the second vertex index.</summary>
    public int Second;
    /// <summary>Gets or sets the third vertex index.</summary>
    public int Third;

    /// <summary>Initializes a triangle whose indices are zero.</summary>
    public IccGamutTriangle()
    {
        First = 0;
        Second = 0;
        Third = 0;
    }
}
