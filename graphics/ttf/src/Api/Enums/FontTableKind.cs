namespace ttf;

/// <summary>Classifies a bounded SFNT table.</summary>
public enum FontTableKind
{
    /// <summary>The table has supported semantic parsing.</summary>
    Known = 0,

    /// <summary>The table is recognized but not semantically supported.</summary>
    Unsupported = 1,

    /// <summary>The table tag is not recognized.</summary>
    Unknown = 2
}
