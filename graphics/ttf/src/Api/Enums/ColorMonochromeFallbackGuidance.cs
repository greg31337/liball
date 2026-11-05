namespace ttf;

/// <summary>Describes use of an ordinary outline with a resolved color glyph.</summary>
public enum ColorMonochromeFallbackGuidance
{
    /// <summary>No supported ordinary outline is available.</summary>
    Unavailable = 0,
    /// <summary>An ordinary outline is available as an optional fallback.</summary>
    Available = 1,
    /// <summary>The ordinary outline was selected as the representation.</summary>
    Selected = 2,
    /// <summary>The selected bitmap requests drawing the ordinary outline too.</summary>
    Required = 3,
    /// <summary>The selected bitmap requests an outline, but none is available.</summary>
    RequiredButUnavailable = 4
}
