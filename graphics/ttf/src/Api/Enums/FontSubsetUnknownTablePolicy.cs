namespace ttf;

/// <summary>Controls retention of bounded tables with unknown semantics.</summary>
public enum FontSubsetUnknownTablePolicy
{
    /// <summary>Remove unknown tables from output.</summary>
    Drop = 0,

    /// <summary>Copy only tables explicitly classified as safe to retain.</summary>
    RetainSafe = 1
}
