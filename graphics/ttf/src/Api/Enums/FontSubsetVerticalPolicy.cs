namespace ttf;

/// <summary>Controls retention of vertical metrics and layout data.</summary>
public enum FontSubsetVerticalPolicy
{
    /// <summary>Remove optional vertical tables from horizontal-only output.</summary>
    Drop = 0,

    /// <summary>Retain and rewrite supported vertical tables.</summary>
    Retain = 1
}
