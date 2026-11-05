namespace fake;

/// <summary>
/// Represents a generated synthetic IBAN and its correlated registry fields.
/// </summary>
/// <remarks>
/// Checksum validity describes syntax only. Generated values do not represent active accounts.
/// </remarks>
public class IbanRecord
{
    /// <summary>The compact electronic IBAN without spaces.</summary>
    public string Electronic = string.Empty;

    /// <summary>The print IBAN grouped in blocks of four characters with spaces.</summary>
    public string Print = string.Empty;

    /// <summary>The generated basic bank account number portion.</summary>
    public string Bban = string.Empty;

    /// <summary>The country or territory code used to select the registry entry.</summary>
    public string CountryCode = string.Empty;

    /// <summary>The two-letter prefix placed at the start of the IBAN.</summary>
    public string IbanPrefix = string.Empty;

    /// <summary>The electronic IBAN length.</summary>
    public int Length;

    /// <summary>The registry pattern used to generate the BBAN.</summary>
    public string BbanPattern = string.Empty;

    /// <summary>The bank-code portion extracted from the BBAN.</summary>
    public string BankCode = string.Empty;

    /// <summary>The branch-code portion, or an empty string when no separate range is defined.</summary>
    public string BranchCode = string.Empty;

    /// <summary>The account-code portion extracted from the BBAN.</summary>
    public string AccountCode = string.Empty;

    /// <summary>Whether the electronic value passes the ISO 13616 MOD 97 check.</summary>
    public bool IsMod97Valid;

    /// <summary>Whether this record was generated as synthetic test data.</summary>
    public bool IsSynthetic;

    /// <summary>A stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the value claims to represent an active account; always <c>false</c>.</summary>
    public bool RepresentsActiveAccount;

    /// <summary>The version of the bundled registry transcription.</summary>
    public string RegistryVersion = string.Empty;

    internal IbanRecord()
    {
    }
}
