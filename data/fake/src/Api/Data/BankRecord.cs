namespace fake;

/// <summary>Represents a project-authored synthetic bank identity.</summary>
/// <remarks>The BIC is shape-only and is not checked against the BIC Directory.</remarks>
public class BankRecord
{
    /// <summary>The explicitly fictional institution name.</summary>
    public string Name = string.Empty;

    /// <summary>An eight-character BIC-shaped value.</summary>
    public string Bic = string.Empty;

    /// <summary>The selected two-letter country code.</summary>
    public string CountryCode = string.Empty;

    /// <summary>The normalized requested locale.</summary>
    public string Locale = string.Empty;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the record represents a registered institution, always <c>false</c>.</summary>
    public bool RepresentsRegisteredInstitution;

    internal BankRecord()
    {
    }
}
