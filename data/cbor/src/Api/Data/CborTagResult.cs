namespace cbor;

/// <summary>
/// Holds the outcome of semantic tag handling.
/// </summary>
public class CborTagResult
{
    /// <summary>Indicates whether the handler recognized the operation.</summary>
    public bool Handled;
    /// <summary>Indicates whether the tag and its content are valid.</summary>
    public bool IsValid;
    /// <summary>The generic CBOR value produced by the handler.</summary>
    public CborValue? Value;
    /// <summary>The native value produced by the handler.</summary>
    public object? NativeValue;
    /// <summary>The stable failure code, or null when no failure occurred.</summary>
    public string? ErrorCode;
    /// <summary>The failure message, or null when no failure occurred.</summary>
    public string? ErrorMessage;
}
