namespace barcode;

/// <summary>
/// Holds public definition and association metadata for a GS1 Application Identifier.
/// </summary>
public class Gs1ApplicationIdentifierInfo
{
    /// <summary>
    /// The exact or wildcard Application Identifier pattern.
    /// </summary>
    public string? Identifier;

    /// <summary>
    /// The human-readable title.
    /// </summary>
    public string? Title;

    /// <summary>
    /// The minimum permitted data length.
    /// </summary>
    public int MinimumLength;

    /// <summary>
    /// The maximum permitted data length.
    /// </summary>
    public int MaximumLength;

    /// <summary>
    /// The GS1 character-set designation.
    /// </summary>
    public string? CharacterSet;

    /// <summary>
    /// Indicates whether the data has fixed length.
    /// </summary>
    public bool FixedLength;

    /// <summary>
    /// Indicates whether the data includes a GS1 check character.
    /// </summary>
    public bool HasCheckCharacter;

    /// <summary>
    /// Indicates whether the identifier is a GS1 Digital Link primary key.
    /// </summary>
    public bool IsPrimaryKey;

    /// <summary>
    /// The Application Identifiers required when this identifier is present.
    /// </summary>
    public string[]? RequiredIdentifiers;

    /// <summary>
    /// The Application Identifiers excluded when this identifier is present.
    /// </summary>
    public string[]? ExcludedIdentifiers;

    /// <summary>
    /// The permitted Digital Link key qualifiers.
    /// </summary>
    public string[]? Qualifiers;
}
