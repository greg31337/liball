namespace ttf;

/// <summary>Controls handling of malformed UTF-16 during text shaping.</summary>
public enum Utf16DecodingMode
{
    /// <summary>Reject unpaired surrogate code units.</summary>
    Strict = 0,

    /// <summary>Replace each unpaired surrogate with U+FFFD.</summary>
    Replacement = 1
}
