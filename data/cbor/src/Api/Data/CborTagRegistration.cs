namespace cbor;

/// <summary>
/// Associates a CBOR tag number with a semantic handler.
/// </summary>
public class CborTagRegistration
{
    /// <summary>The unsigned CBOR tag number.</summary>
    public ulong TagNumber;
    /// <summary>An optional descriptive registration name.</summary>
    public string? Name;
    /// <summary>The semantic handler.</summary>
    public ICborTagHandler? Handler;
    /// <summary>Indicates whether the handler validates tag content while decoding.</summary>
    public bool ValidateContent = true;
}
