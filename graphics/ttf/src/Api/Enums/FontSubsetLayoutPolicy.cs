namespace ttf;

/// <summary>Controls retention of OpenType shaping and justification tables.</summary>
public enum FontSubsetLayoutPolicy
{
    /// <summary>Remove optional shaping and justification tables.</summary>
    Drop = 0,

    /// <summary>Retain supported layout behavior after glyph remapping and pruning.</summary>
    Retain = 1
}
