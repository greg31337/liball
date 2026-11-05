namespace svg;

/// <summary>Identifies the staged source representation produced by a load entry point.</summary>
public enum SvgLoadSourceKind
{
    /// <summary>No source was staged.</summary>
    None = 0,
    /// <summary>Decoded UTF-16 characters were staged.</summary>
    Characters = 1,
    /// <summary>Encoded bytes were staged.</summary>
    Bytes = 2,
    /// <summary>A caller-owned XML reader was retained for the XML construction stage.</summary>
    XmlReader = 3
}
