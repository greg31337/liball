namespace cbor;

/// <summary>
/// Holds policies for converting between CBOR and JSON.
/// </summary>
public class CborJsonOptions
{
    /// <summary>The policy for CBOR values outside the JSON data model.</summary>
    public CborJsonPolicy Policy = CborJsonPolicy.Strict;
    /// <summary>The policy for duplicate JSON object property names.</summary>
    public CborDuplicateKeyPolicy DuplicatePropertyPolicy = CborDuplicateKeyPolicy.Preserve;
    /// <summary>The convention for converting JSON strings and preserved byte wrappers.</summary>
    public CborJsonByteStringConvention ByteStringConvention = CborJsonByteStringConvention.TextString;
    /// <summary>The convention for recognizing preserved CBOR tag wrappers.</summary>
    public CborJsonTagConvention TagConvention = CborJsonTagConvention.Map;
    /// <summary>Indicates whether generated JSON uses indentation.</summary>
    public bool Indented;
    /// <summary>The maximum nested JSON or CBOR value depth.</summary>
    public int MaxNestingDepth = 64;
    /// <summary>The maximum number of input characters.</summary>
    public int MaxInputCharacters = 67108864;
    /// <summary>The maximum number of output characters.</summary>
    public int MaxOutputCharacters = 67108864;
}
