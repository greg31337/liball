namespace odf;

/// <summary>Describes a trace or trace-removal operation attached to a spreadsheet cell.</summary>
public class OdfDetectiveOperation
{
    /// <summary>The operation name.</summary>
    public string Name = "trace-precedents";
    /// <summary>The zero-based operation sequence index.</summary>
    public long Index;
    /// <summary>The cell that owns the detective operation.</summary>
    public OdfCell? Cell;
    /// <summary>The underlying operation element.</summary>
    public OdfNode? Node;
}
