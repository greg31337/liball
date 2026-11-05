namespace odf;

/// <summary>Describes an insertion or movement cut-off in a spreadsheet deletion.</summary>
public class OdfSpreadsheetChangeCutOff
{
    /// <summary>The cut-off kind: <c>insertion</c> or <c>movement</c>.</summary>
    public string Kind = "";
    /// <summary>The referenced insertion identifier.</summary>
    public string Id = "";
    /// <summary>Whether a single position is stored.</summary>
    public bool HasPosition;
    /// <summary>The single cut-off position.</summary>
    public long Position;
    /// <summary>Whether a start and end position range is stored.</summary>
    public bool HasRange;
    /// <summary>The movement cut-off start position.</summary>
    public long StartPosition;
    /// <summary>The movement cut-off end position.</summary>
    public long EndPosition;
    /// <summary>The underlying cut-off element.</summary>
    public OdfNode? Node;
}
