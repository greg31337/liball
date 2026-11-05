namespace ttf;

/// <summary>Identifies inline alignment within an available line width.</summary>
public enum TextAlignment
{
    /// <summary>Align content with the inline start edge.</summary>
    Start = 0,

    /// <summary>Align content with the inline end edge.</summary>
    End = 1,

    /// <summary>Center content between the inline edges.</summary>
    Center = 2,

    /// <summary>Expand eligible opportunities to fill the available inline width.</summary>
    Justify = 3
}
