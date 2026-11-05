namespace iso20022;

/// <summary>Provides version-neutral business values for a cash-management report or notification.</summary>
public class CashManagementTemplate
{
    /// <summary>The report message identifier.</summary>
    public string MessageIdentifier = string.Empty;
    /// <summary>The report, statement, or notification identifier.</summary>
    public string ReportIdentifier = string.Empty;
    /// <summary>The reported cash account identifier.</summary>
    public string AccountIdentifier = string.Empty;
    /// <summary>The account owner's display name.</summary>
    public string Owner = string.Empty;
    /// <summary>The principal reported balance and exact currency.</summary>
    public IsoAmountValue BalanceAmount = new IsoAmountValue();
    /// <summary>The balance or value date.</summary>
    public IsoDateValue BalanceDate = new IsoDateValue();
}
