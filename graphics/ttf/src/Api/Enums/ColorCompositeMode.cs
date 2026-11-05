namespace ttf;

/// <summary>Identifies a COLR Porter-Duff or blend composite operation.</summary>
public enum ColorCompositeMode
{
    /// <summary>Clears the destination.</summary>
    Clear = 0,
    /// <summary>Uses the source.</summary>
    Source = 1,
    /// <summary>Uses the destination.</summary>
    Destination = 2,
    /// <summary>Draws the source over the destination.</summary>
    SourceOver = 3,
    /// <summary>Draws the destination over the source.</summary>
    DestinationOver = 4,
    /// <summary>Keeps the source where it overlaps the destination.</summary>
    SourceIn = 5,
    /// <summary>Keeps the destination where it overlaps the source.</summary>
    DestinationIn = 6,
    /// <summary>Keeps the source outside the destination.</summary>
    SourceOut = 7,
    /// <summary>Keeps the destination outside the source.</summary>
    DestinationOut = 8,
    /// <summary>Places the source atop the destination.</summary>
    SourceAtop = 9,
    /// <summary>Places the destination atop the source.</summary>
    DestinationAtop = 10,
    /// <summary>Keeps non-overlapping source and destination regions.</summary>
    Xor = 11,
    /// <summary>Adds source and destination components.</summary>
    Plus = 12,
    /// <summary>Applies the screen blend mode.</summary>
    Screen = 13,
    /// <summary>Applies the overlay blend mode.</summary>
    Overlay = 14,
    /// <summary>Selects the darker components.</summary>
    Darken = 15,
    /// <summary>Selects the lighter components.</summary>
    Lighten = 16,
    /// <summary>Applies the color-dodge blend mode.</summary>
    ColorDodge = 17,
    /// <summary>Applies the color-burn blend mode.</summary>
    ColorBurn = 18,
    /// <summary>Applies the hard-light blend mode.</summary>
    HardLight = 19,
    /// <summary>Applies the soft-light blend mode.</summary>
    SoftLight = 20,
    /// <summary>Applies the difference blend mode.</summary>
    Difference = 21,
    /// <summary>Applies the exclusion blend mode.</summary>
    Exclusion = 22,
    /// <summary>Multiplies source and destination components.</summary>
    Multiply = 23,
    /// <summary>Uses source hue with destination saturation and luminosity.</summary>
    Hue = 24,
    /// <summary>Uses source saturation with destination hue and luminosity.</summary>
    Saturation = 25,
    /// <summary>Uses source hue and saturation with destination luminosity.</summary>
    Color = 26,
    /// <summary>Uses source luminosity with destination hue and saturation.</summary>
    Luminosity = 27
}
