namespace ttf;

/// <summary>Selects the smallest source unit retained as a shaping cluster.</summary>
public enum TextShapingClusterPolicy
{
    /// <summary>Keep each extended grapheme cluster indivisible.</summary>
    GraphemeClusters = 0,

    /// <summary>Allow distinct Unicode scalars to begin distinct clusters.</summary>
    UnicodeScalars = 1
}
