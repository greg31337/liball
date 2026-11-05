using System;

namespace fake;

/// <summary>Represents an explicitly synthetic check fixture.</summary>
/// <remarks>The record is non-negotiable and does not identify an account, institution, or payee.</remarks>
public class CheckRecord
{
    /// <summary>The synthetic check-number shape.</summary>
    public string Number = string.Empty;

    /// <summary>The checksum-valid ABA routing-number shape.</summary>
    public string RoutingNumber = string.Empty;

    /// <summary>The synthetic domestic account-number shape.</summary>
    public string AccountNumber = string.Empty;

    /// <summary>The explicitly fictional payee label.</summary>
    public string Payee = string.Empty;

    /// <summary>The project-authored check memo.</summary>
    public string Memo = string.Empty;

    /// <summary>The numeric check amount.</summary>
    public decimal Amount;

    /// <summary>The ISO 4217 currency code associated with the amount.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>The captured reference time used as the written date.</summary>
    public DateTimeOffset WrittenOn;

    /// <summary>Whether the routing-number shape passes the ABA checksum.</summary>
    public bool IsRoutingChecksumValid;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the fixture is a negotiable instrument, always <c>false</c>.</summary>
    public bool IsNegotiable;

    internal CheckRecord()
    {
    }
}
