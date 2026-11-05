namespace geo;

/// <summary>Identifies how open line ends are capped by a buffer.</summary>
public enum BufferCapStyle
{
    /// <summary>A semicircular cap.</summary>
    Round = 1,

    /// <summary>A flat cap ending at the source endpoint.</summary>
    Flat = 2,

    /// <summary>A square cap extended by the buffer distance.</summary>
    Square = 3
}
