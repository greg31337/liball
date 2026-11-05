namespace fake;

/// <summary>Represents a correlated synthetic account ledger.</summary>
/// <remarks>The record is fixture data, not an account statement or balance assertion.</remarks>
public class AccountLedgerRecord
{
    /// <summary>The synthetic domestic account associated with every entry.</summary>
    public BankAccountRecord Account = new BankAccountRecord();

    /// <summary>The ordered interval containing every entry time.</summary>
    public LedgerIntervalRecord Interval = new LedgerIntervalRecord();

    /// <summary>The ISO 4217 currency code used for every monetary field.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>The numeric balance before the first entry.</summary>
    public decimal OpeningBalance;

    /// <summary>The numeric balance after the final entry.</summary>
    public decimal ClosingBalance;

    /// <summary>The locale-formatted display of <c>OpeningBalance</c>.</summary>
    public string OpeningBalanceDisplay = string.Empty;

    /// <summary>The locale-formatted display of <c>ClosingBalance</c>.</summary>
    public string ClosingBalanceDisplay = string.Empty;

    /// <summary>The chronologically ordered debit and credit entries.</summary>
    public LedgerEntryRecord[] Entries = new LedgerEntryRecord[0];

    /// <summary>Whether the ledger is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>The stable marker identifying the synthetic-data contract.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the record represents a posted account ledger, always <c>false</c>.</summary>
    public bool RepresentsPostedLedger;

    internal AccountLedgerRecord()
    {
    }
}
