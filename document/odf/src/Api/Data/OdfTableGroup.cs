namespace odf;

/// <summary>Describes one compact row-group or column-group logical range.</summary>
public class OdfTableGroup
{
    /// <summary>Whether this is a row group rather than a column group.</summary>
    public bool IsRowGroup;
    /// <summary>The zero-based first logical row or column in the group.</summary>
    public long StartIndex;
    /// <summary>The logical row or column count in the group.</summary>
    public long Count;
    /// <summary>The one-based nesting depth of the group.</summary>
    public int Depth;
    /// <summary>The underlying group element.</summary>
    public OdfNode? Node;
}
