namespace barcode;

/// <summary>
/// Selects the Code 39 character repertoire.
/// </summary>
public enum Code39CharacterMode
{
    /// <summary>
    /// Uses the standard Code 39 alphabet.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Uses Full ASCII Code 39 shift sequences.
    /// </summary>
    FullAscii = 1
}
