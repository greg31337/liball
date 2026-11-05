namespace geo;

/// <summary>Identifies how offset segments are joined by a buffer.</summary>
public enum BufferJoinStyle
{
    /// <summary>A circular fillet join.</summary>
    Round = 1,

    /// <summary>A mitred join limited by the configured mitre ratio.</summary>
    Mitre = 2,

    /// <summary>A bevelled join.</summary>
    Bevel = 3
}
