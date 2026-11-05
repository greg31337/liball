using System;

namespace fake;

/// <summary>Represents one arithmetically correlated entry in a synthetic account ledger.</summary>
/// <remarks>The entry does not represent posted, authorized, or settled funds.</remarks>
public class LedgerEntryRecord
{
    /// <summary>The synthetic transaction reference.</summary>
    public string Reference = string.Empty;

    /// <summary>The domestic account-shaped value shared with the containing ledger.</summary>
    public string AccountNumber = string.Empty;

    /// <summary>The transaction category.</summary>
    public FinancialTransactionType Type;

    /// <summary>The project-authored description correlated with <c>Type</c>.</summary>
    public string Description = string.Empty;

    /// <summary>The instant within the containing ledger interval.</summary>
    public DateTimeOffset OccurredOn;

    /// <summary>The ISO 4217 currency code shared by the containing ledger.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>The numeric amount subtracted from the preceding balance.</summary>
    public decimal Debit;

    /// <summary>The numeric amount added to the preceding balance.</summary>
    public decimal Credit;

    /// <summary>The numeric balance before applying debit and credit.</summary>
    public decimal BalanceBefore;

    /// <summary>The numeric balance after applying debit and credit.</summary>
    public decimal BalanceAfter;

    /// <summary>The locale-formatted display of <c>Debit</c>.</summary>
    public string DebitDisplay = string.Empty;

    /// <summary>The locale-formatted display of <c>Credit</c>.</summary>
    public string CreditDisplay = string.Empty;

    /// <summary>The locale-formatted display of <c>BalanceAfter</c>.</summary>
    public string BalanceAfterDisplay = string.Empty;

    /// <summary>Whether the entry is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the entry represents a posted transaction, always <c>false</c>.</summary>
    public bool RepresentsPostedTransaction;

    internal LedgerEntryRecord()
    {
    }
}
