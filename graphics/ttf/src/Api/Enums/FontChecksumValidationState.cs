namespace ttf;

/// <summary>Describes the result of SFNT checksum validation.</summary>
public enum FontChecksumValidationState
{
    /// <summary>Checksum validation was disabled by policy.</summary>
    NotChecked = 0,

    /// <summary>The stored checksum or checksum adjustment is valid.</summary>
    Valid = 1,

    /// <summary>The stored checksum or checksum adjustment is invalid.</summary>
    Invalid = 2,

    /// <summary>The checksum does not apply or cannot be evaluated for this item.</summary>
    NotApplicable = 3
}
