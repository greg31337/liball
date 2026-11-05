namespace cbor;

/// <summary>
/// Selects the layout used for CBOR diagnostic notation.
/// </summary>
public enum CborDiagnosticStyle
{
    /// <summary>Formats a value on the smallest practical number of lines.</summary>
    Compact = 0,
    /// <summary>Formats nested values with indentation and line breaks.</summary>
    Indented = 1
}
