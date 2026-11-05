namespace fake;

/// <summary>Represents a synthetic ISO 6346-shaped freight-container identifier.</summary>
/// <remarks>Checksum validity does not establish registration, ownership, or the existence of a container.</remarks>
public class ContainerIdentifierRecord
{
    /// <summary>The complete eleven-character identifier.</summary>
    public string Identifier = string.Empty;

    /// <summary>The three-letter synthetic owner-code shape.</summary>
    public string OwnerCode = string.Empty;

    /// <summary>The equipment-category letter, such as <c>U</c>.</summary>
    public string EquipmentCategory = string.Empty;

    /// <summary>The six-digit serial-number shape.</summary>
    public string SerialNumber = string.Empty;

    /// <summary>The ISO 6346 check digit.</summary>
    public int CheckDigit;

    /// <summary>Whether <c>Identifier</c> has a valid structural check digit, always <c>true</c>.</summary>
    public bool IsCheckDigitValid;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the identifier is claimed to be registered, always <c>false</c>.</summary>
    public bool RepresentsRegisteredContainer;

    internal ContainerIdentifierRecord()
    {
    }
}
