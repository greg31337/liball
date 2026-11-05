namespace cbor;

/// <summary>
/// Controls parsing and formatting of CBOR diagnostic notation.
/// </summary>
public class CborDiagnosticOptions
{
    /// <summary>The diagnostic notation layout.</summary>
    public CborDiagnosticStyle Style = CborDiagnosticStyle.Compact;
    /// <summary>The number of spaces used for each indentation level.</summary>
    public int IndentSize = 2;
    /// <summary>Indicates whether extended diagnostic notation is enabled.</summary>
    public bool UseExtendedNotation;
    /// <summary>Indicates whether retained representation indicators are included.</summary>
    public bool IncludeRepresentation;
    /// <summary>Selects preserved or deterministic encoded-key ordering for maps.</summary>
    public CborMapKeyOrder MapKeyOrder = CborMapKeyOrder.Preserve;
    /// <summary>The maximum nested value depth accepted or formatted.</summary>
    public int MaxNestingDepth = 64;
    /// <summary>The maximum number of input or output characters.</summary>
    public int MaxCharacters = 16777216;
    /// <summary>The maximum nested depth of embedded CBOR byte-string notation.</summary>
    public int MaxEmbeddedDepth = 8;
    /// <summary>The maximum total number of items encoded by embedded CBOR notation.</summary>
    public int MaxEmbeddedItems = 1000000;
    /// <summary>The maximum total encoded bytes produced by embedded CBOR notation.</summary>
    public long MaxEmbeddedBytes = 67108864;
}
