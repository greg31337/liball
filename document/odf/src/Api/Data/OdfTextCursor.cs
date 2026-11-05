namespace odf;

/// <summary>Describes a tracked insertion point and optional text selection.</summary>
public class OdfTextCursor
{
    /// <summary>The block containing the active insertion point.</summary>
    public OdfTextBlock? Block;
    /// <summary>The UTF-16 offset of the active insertion point.</summary>
    public int Offset;
    /// <summary>The block containing the selection anchor.</summary>
    public OdfTextBlock? AnchorBlock;
    /// <summary>The UTF-16 offset of the selection anchor.</summary>
    public int AnchorOffset;
    /// <summary>Whether the cursor has a non-empty selection.</summary>
    public bool HasSelection;
}
