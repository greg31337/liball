namespace cbor;

/// <summary>Selects how preserved CBOR tag wrappers are interpreted in JSON input.</summary>
public enum CborJsonTagConvention
{
    /// <summary>Treats an ordinary JSON object as a CBOR map.</summary>
    Map = 0,
    /// <summary>Recognizes objects using the preserved <c>$cbor</c> tag wrapper.</summary>
    PreserveWrapper = 1
}
