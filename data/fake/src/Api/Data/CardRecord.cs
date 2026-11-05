namespace fake;

/// <summary>Represents a correlated, explicitly synthetic payment-card fixture.</summary>
/// <remarks>The record does not represent an active account, authorization, stored credential, or usable payment instrument.</remarks>
public class CardRecord
{
    /// <summary>The selected issuer shape.</summary>
    public CardIssuer Issuer;

    /// <summary>The issuer display name.</summary>
    public string IssuerName = string.Empty;

    /// <summary>The Luhn-valid test PAN.</summary>
    public string Number = string.Empty;

    /// <summary>The masked PAN retaining only its final four digits.</summary>
    public string MaskedNumber = string.Empty;

    /// <summary>The synthetic three- or four-digit verification code.</summary>
    public string Cvv = string.Empty;

    /// <summary>The synthetic four-digit PIN.</summary>
    public string Pin = string.Empty;

    /// <summary>The expiry month and two-digit year in <c>MM/yy</c> form.</summary>
    public string Expiry = string.Empty;

    /// <summary>The expiry month from one through twelve.</summary>
    public int ExpiryMonth;

    /// <summary>The four-digit expiry year.</summary>
    public int ExpiryYear;

    /// <summary>The explicitly synthetic cardholder label.</summary>
    public string Cardholder = string.Empty;

    /// <summary>Whether <c>Number</c> has a valid Luhn check digit.</summary>
    public bool IsLuhnValid;

    /// <summary>Whether this record is synthetic, always true.</summary>
    public bool IsSynthetic;

    /// <summary>Whether this record uses a test PAN, always true.</summary>
    public bool IsTestCard;

    /// <summary>The project marker identifying the record as synthetic test data.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the fixture represents an authorization, always false.</summary>
    public bool IsAuthorized;

    /// <summary>Whether the fixture represents an active account, always false.</summary>
    public bool RepresentsActiveAccount;

    internal CardRecord()
    {
    }
}
