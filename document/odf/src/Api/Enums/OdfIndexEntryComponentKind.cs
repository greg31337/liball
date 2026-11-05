namespace odf;

/// <summary>Identifies a generated-index entry component.</summary>
public enum OdfIndexEntryComponentKind
{
    /// <summary>An unrecognized component.</summary>
    Unknown = 0,
    /// <summary>A chapter number or name.</summary>
    Chapter = 1,
    /// <summary>The indexed entry text.</summary>
    Text = 2,
    /// <summary>A page number.</summary>
    PageNumber = 3,
    /// <summary>Literal span text.</summary>
    Span = 4,
    /// <summary>A tab stop.</summary>
    TabStop = 5,
    /// <summary>A bibliography data field.</summary>
    Bibliography = 6,
    /// <summary>The start of a hyperlink.</summary>
    LinkStart = 7,
    /// <summary>The end of a hyperlink.</summary>
    LinkEnd = 8
}
