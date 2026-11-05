namespace iso20022;

/// <summary>Provides version-neutral business values for an interbank credit transfer.</summary>
public class InterbankTransferTemplate
{
    /// <summary>The transfer message identifier.</summary>
    public string MessageIdentifier = string.Empty;
    /// <summary>The instructing agent's instruction identifier.</summary>
    public string InstructionIdentifier = string.Empty;
    /// <summary>The end-to-end transaction identifier.</summary>
    public string EndToEndIdentifier = string.Empty;
    /// <summary>The interbank transaction identifier.</summary>
    public string TransactionIdentifier = string.Empty;
    /// <summary>The unique end-to-end transaction reference.</summary>
    public string Uetr = string.Empty;
    /// <summary>The debtor display name.</summary>
    public string Debtor = string.Empty;
    /// <summary>The debtor account identifier.</summary>
    public string DebtorAccount = string.Empty;
    /// <summary>The creditor display name.</summary>
    public string Creditor = string.Empty;
    /// <summary>The creditor account identifier.</summary>
    public string CreditorAccount = string.Empty;
    /// <summary>The interbank settlement amount and exact currency.</summary>
    public IsoAmountValue SettlementAmount = new IsoAmountValue();
    /// <summary>The interbank settlement date.</summary>
    public IsoDateValue SettlementDate = new IsoDateValue();
    /// <summary>The unstructured remittance information.</summary>
    public string RemittanceInformation = string.Empty;
}
