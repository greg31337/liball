namespace fake;

/// <summary>Represents a locale-shaped synthetic domestic bank account.</summary>
/// <remarks>The record is fixture data and does not represent an opened, active, or reachable account.</remarks>
public class BankAccountRecord
{
    /// <summary>The project-authored account display name.</summary>
    public string Name = string.Empty;

    /// <summary>The account category.</summary>
    public BankAccountType Type;

    /// <summary>The locale-selected display name for the account category.</summary>
    public string TypeName = string.Empty;

    /// <summary>The synthetic domestic account-shaped value.</summary>
    public string Number = string.Empty;

    /// <summary>The account-shaped value with all but its final four alphanumeric characters masked.</summary>
    public string MaskedNumber = string.Empty;

    /// <summary>The locale-selected generation mask.</summary>
    public string Mask = string.Empty;

    /// <summary>The selected two-letter country code.</summary>
    public string CountryCode = string.Empty;

    /// <summary>The normalized requested locale.</summary>
    public string Locale = string.Empty;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the record represents an active account, always <c>false</c>.</summary>
    public bool RepresentsActiveAccount;

    internal BankAccountRecord()
    {
    }
}
