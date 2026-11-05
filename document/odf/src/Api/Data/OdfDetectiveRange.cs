namespace odf;

/// <summary>Describes a highlighted precedent, dependent, or error range attached to a cell.</summary>
public class OdfDetectiveRange
{
    /// <summary>The highlighted cell range address.</summary>
    public string RangeAddress = "";
    /// <summary>The trace direction.</summary>
    public string Direction = "from-same-table";
    /// <summary>Whether the traced range contains an error.</summary>
    public bool ContainsError;
    /// <summary>Whether the trace is marked invalid instead of identifying a range.</summary>
    public bool MarkedInvalid;
    /// <summary>The cell that owns the detective trace.</summary>
    public OdfCell? Cell;
    /// <summary>The underlying highlighted-range element.</summary>
    public OdfNode? Node;
}
