namespace svg;

/// <summary>Configures document-wide literal color replacement.</summary>
public class SvgRecolorProcessOptions
{
    /// <summary>Maps one parsed literal color to its replacement, or returns <c>null</c> to retain it.</summary>
    public System.Func<SvgColor, SvgColor>? ColorProvider;
    /// <summary>Whether presentation attributes are processed.</summary>
    public bool IncludePresentationAttributes;
    /// <summary>Whether inline style declarations are processed.</summary>
    public bool IncludeStyleAttributes;
    /// <summary>Whether embedded stylesheet declarations are processed.</summary>
    public bool IncludeStyleSheets;
    /// <summary>The maximum number of document nodes inspected.</summary>
    public int MaximumNodes;
    /// <summary>The maximum color or paint token length.</summary>
    public int MaximumTokenCharacters;

    /// <summary>Creates bounded opt-in recoloring defaults.</summary>
    public SvgRecolorProcessOptions()
    {
        ColorProvider = null;
        IncludePresentationAttributes = true;
        IncludeStyleAttributes = true;
        IncludeStyleSheets = true;
        MaximumNodes = 1000000;
        MaximumTokenCharacters = 4096;
    }
}

/// <summary>Configures conversion of literal colors to theme-controlled values.</summary>
public class SvgThemeColorProcessOptions
{
    /// <summary>The replacement theme mechanism.</summary>
    public SvgThemeColorMode Mode;
    /// <summary>The optional exact source color; a missing value selects every literal color.</summary>
    public SvgColor? SourceColor;
    /// <summary>The custom-property name, including its leading two hyphens.</summary>
    public string CustomPropertyName;
    /// <summary>Whether custom-property references retain each authored color as a fallback.</summary>
    public bool PreserveFallback;
    /// <summary>Whether presentation attributes are processed.</summary>
    public bool IncludePresentationAttributes;
    /// <summary>Whether inline style declarations are processed.</summary>
    public bool IncludeStyleAttributes;
    /// <summary>Whether embedded stylesheet declarations are processed.</summary>
    public bool IncludeStyleSheets;
    /// <summary>The maximum number of document nodes inspected.</summary>
    public int MaximumNodes;
    /// <summary>The maximum color or paint token length.</summary>
    public int MaximumTokenCharacters;

    /// <summary>Creates bounded current-color conversion defaults.</summary>
    public SvgThemeColorProcessOptions()
    {
        Mode = SvgThemeColorMode.CurrentColor;
        SourceColor = null;
        CustomPropertyName = "--svg-color";
        PreserveFallback = true;
        IncludePresentationAttributes = true;
        IncludeStyleAttributes = true;
        IncludeStyleSheets = true;
        MaximumNodes = 1000000;
        MaximumTokenCharacters = 4096;
    }
}

/// <summary>Configures root viewport resizing and view-box fitting or cropping.</summary>
public class SvgViewBoxResizeOptions
{
    /// <summary>The requested resize operation.</summary>
    public SvgViewBoxResizeMode Mode;
    /// <summary>The explicit view box used by <see cref="SvgViewBoxResizeMode.SetViewBox"/>.</summary>
    public SvgViewBox? ViewBox;
    /// <summary>The positive viewport width used by viewport resizing.</summary>
    public double ViewportWidth;
    /// <summary>The positive viewport height used by viewport resizing.</summary>
    public double ViewportHeight;
    /// <summary>The positive width-to-height ratio used by fit and crop operations.</summary>
    public double TargetAspectRatio;
    /// <summary>The horizontal alignment from zero through one used when the view box changes.</summary>
    public double AlignX;
    /// <summary>The vertical alignment from zero through one used when the view box changes.</summary>
    public double AlignY;
    /// <summary>The maximum numeric token length accepted while parsing the current view box.</summary>
    public int MaximumTokenCharacters;

    /// <summary>Creates an explicit-view-box configuration that is inert until a rectangle is supplied.</summary>
    public SvgViewBoxResizeOptions()
    {
        Mode = SvgViewBoxResizeMode.SetViewBox;
        ViewBox = null;
        ViewportWidth = 300;
        ViewportHeight = 150;
        TargetAspectRatio = 2;
        AlignX = 0.5;
        AlignY = 0.5;
        MaximumTokenCharacters = 4096;
    }
}

/// <summary>Configures affine conversion of root content coordinates.</summary>
public class SvgCoordinateSystemProcessOptions
{
    /// <summary>The finite affine transform applied around root graphical content.</summary>
    public SvgMatrix Transform;
    /// <summary>An optional replacement view box; otherwise the current view box is transformed.</summary>
    public SvgViewBox? TargetViewBox;
    /// <summary>Whether a current or explicit target view box is rewritten.</summary>
    public bool TransformViewBox;
    /// <summary>The maximum number of root children moved into the coordinate wrapper.</summary>
    public int MaximumChildren;

    /// <summary>Creates identity coordinate-conversion defaults.</summary>
    public SvgCoordinateSystemProcessOptions()
    {
        Transform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        TargetViewBox = null;
        TransformViewBox = true;
        MaximumChildren = 100000;
    }
}

/// <summary>Configures cropping of the root view box to resolved drawing bounds.</summary>
public class SvgDrawingBoundsCropOptions
{
    /// <summary>The document resolution environment.</summary>
    public SvgResolveOptions ResolveOptions;
    /// <summary>The aggregate drawing-bounds settings.</summary>
    public SvgDocumentBoundsOptions BoundsOptions;
    /// <summary>The nonnegative horizontal padding in root user units.</summary>
    public double HorizontalPadding;
    /// <summary>The nonnegative vertical padding in root user units.</summary>
    public double VerticalPadding;
    /// <summary>Whether root width and height are set to the cropped dimensions.</summary>
    public bool ResizeViewport;

    /// <summary>Creates bounded drawing-bounds crop defaults.</summary>
    public SvgDrawingBoundsCropOptions()
    {
        ResolveOptions = new SvgResolveOptions();
        BoundsOptions = new SvgDocumentBoundsOptions();
        BoundsOptions.ClipToViewports = false;
        HorizontalPadding = 0;
        VerticalPadding = 0;
        ResizeViewport = false;
    }
}
