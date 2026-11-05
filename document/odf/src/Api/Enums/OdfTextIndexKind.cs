namespace odf;

/// <summary>Identifies a generated text-index family.</summary>
public enum OdfTextIndexKind
{
    /// <summary>An unrecognized index.</summary>
    Unknown = 0,
    /// <summary>A table of contents.</summary>
    TableOfContent = 1,
    /// <summary>An alphabetical index.</summary>
    Alphabetical = 2,
    /// <summary>A bibliography.</summary>
    Bibliography = 3,
    /// <summary>A user-defined index.</summary>
    User = 4,
    /// <summary>An index of tables.</summary>
    Table = 5,
    /// <summary>An index of illustrations.</summary>
    Illustration = 6,
    /// <summary>An index of objects.</summary>
    Object = 7
}
