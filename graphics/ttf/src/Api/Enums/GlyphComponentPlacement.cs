namespace ttf;

/// <summary>Identifies how a composite component is positioned.</summary>
public enum GlyphComponentPlacement
{
    /// <summary>The component uses an x/y translation.</summary>
    Offset = 0,

    /// <summary>The component aligns one child point to one accumulated parent point.</summary>
    PointAttachment = 1
}
