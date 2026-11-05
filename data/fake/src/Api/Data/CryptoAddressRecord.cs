namespace fake;

/// <summary>Represents an explicitly synthetic cryptocurrency address fixture.</summary>
/// <remarks>
/// The record contains no private key and does not establish ownership, spendability,
/// chain activity, balance, or safe use as a transaction destination.
/// </remarks>
public class CryptoAddressRecord
{
    /// <summary>The selected cryptocurrency address family.</summary>
    public Cryptocurrency Cryptocurrency;

    /// <summary>The conventional uppercase currency symbol.</summary>
    public string Symbol = string.Empty;

    /// <summary>The generated address-shaped text.</summary>
    public string Address = string.Empty;

    /// <summary>The generated address encoding.</summary>
    public string Format = string.Empty;

    /// <summary>The network presentation encoded by the address shape.</summary>
    public string Network = string.Empty;

    /// <summary>The checksum convention applied to the address.</summary>
    public string ChecksumKind = string.Empty;

    /// <summary>Whether the generated address passes its structural checksum validation.</summary>
    public bool IsChecksumValid;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the record contains private-key material, always <c>false</c>.</summary>
    public bool ContainsPrivateKey;

    /// <summary>Whether the record claims that the address is spendable, always <c>false</c>.</summary>
    public bool IsSpendable;

    /// <summary>Whether the record claims that the address is active on a network, always <c>false</c>.</summary>
    public bool RepresentsActiveWallet;

    internal CryptoAddressRecord()
    {
    }
}
