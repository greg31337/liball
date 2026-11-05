namespace fake;

/// <summary>Represents a correlated synthetic bank branch and domestic routing shape.</summary>
/// <remarks>Identifiers are structurally generated fixtures and are not directory or allocation claims.</remarks>
public class BankBranchRecord
{
    /// <summary>The explicitly fictional institution name.</summary>
    public string BankName = string.Empty;

    /// <summary>The explicitly fictional branch name.</summary>
    public string BranchName = string.Empty;

    /// <summary>An eleven-character BIC-shaped value including a branch identifier.</summary>
    public string Bic = string.Empty;

    /// <summary>The eight-character business-party portion of <c>Bic</c>.</summary>
    public string Bic8 = string.Empty;

    /// <summary>The three-character branch identifier from <c>Bic</c>.</summary>
    public string BranchCode = string.Empty;

    /// <summary>A locale-selected domestic routing-shaped value.</summary>
    public string DomesticRoutingNumber = string.Empty;

    /// <summary>The locale-selected mask used for the domestic routing-shaped value.</summary>
    public string DomesticRoutingMask = string.Empty;

    /// <summary>Whether the routing value passes the ABA checksum when the selected country is the United States.</summary>
    public bool IsAbaChecksumValid;

    /// <summary>The selected two-letter country code.</summary>
    public string CountryCode = string.Empty;

    /// <summary>The normalized requested locale.</summary>
    public string Locale = string.Empty;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the record represents a registered institution or branch, always <c>false</c>.</summary>
    public bool RepresentsRegisteredInstitution;

    internal BankBranchRecord()
    {
    }
}
