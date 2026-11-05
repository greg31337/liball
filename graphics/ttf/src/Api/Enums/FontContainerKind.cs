namespace ttf;

/// <summary>Identifies the container that supplied a loaded font.</summary>
public enum FontContainerKind
{
    /// <summary>The container has not been identified.</summary>
    Unknown = 0,

    /// <summary>A scalar SFNT TrueType or OpenType font.</summary>
    Sfnt = 1,

    /// <summary>An SFNT TrueType or OpenType font collection.</summary>
    Collection = 2,

    /// <summary>A WOFF 1 container.</summary>
    Woff1 = 3,

    /// <summary>A WOFF 2 container.</summary>
    Woff2 = 4
}
