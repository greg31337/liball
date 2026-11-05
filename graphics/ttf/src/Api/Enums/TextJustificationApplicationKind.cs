namespace ttf;

/// <summary>Identifies one mechanism applied while expanding a text line.</summary>
public enum TextJustificationApplicationKind
{
    /// <summary>Expansion assigned after a visible whitespace cluster.</summary>
    Space = 0,

    /// <summary>Tracking expansion assigned between non-whitespace clusters.</summary>
    Tracking = 1,

    /// <summary>Expansion assigned to a caller-supplied JSTF opportunity.</summary>
    Jstf = 2,

    /// <summary>Expansion assigned to a caller-supplied kashida opportunity.</summary>
    Kashida = 3
}
