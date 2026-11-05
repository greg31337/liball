namespace ttf;

/// <summary>Identifies the backgrounds for which a CPAL palette is suitable.</summary>
public enum ColorPaletteType
{
    /// <summary>No preferred background is specified.</summary>
    Unspecified = 0,

    /// <summary>The palette is suitable for a light background.</summary>
    LightBackground = 1,

    /// <summary>The palette is suitable for a dark background.</summary>
    DarkBackground = 2,

    /// <summary>The palette is suitable for both light and dark backgrounds.</summary>
    LightAndDarkBackgrounds = 3
}
