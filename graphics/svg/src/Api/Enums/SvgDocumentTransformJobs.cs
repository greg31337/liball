namespace svg;

/// <summary>Selects the theme-controlled replacement used for matching literal colors.</summary>
public enum SvgThemeColorMode
{
    /// <summary>Replace matching colors with the inherited <c>currentColor</c> value.</summary>
    CurrentColor = 0,
    /// <summary>Replace matching colors with a CSS custom-property reference.</summary>
    CustomProperty = 1
}

/// <summary>Selects how a root viewport or view box is changed.</summary>
public enum SvgViewBoxResizeMode
{
    /// <summary>Change the root width and height while retaining its view box.</summary>
    ResizeViewport = 0,
    /// <summary>Set the root view box to an explicit rectangle.</summary>
    SetViewBox = 1,
    /// <summary>Expand the current view box to a requested aspect ratio.</summary>
    FitViewBox = 2,
    /// <summary>Crop the current view box to a requested aspect ratio.</summary>
    CropViewBox = 3
}
