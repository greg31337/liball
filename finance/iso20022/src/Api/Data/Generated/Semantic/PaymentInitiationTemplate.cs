namespace iso20022;

/// <summary>Provides version-neutral business values for a customer payment initiation.</summary>
public class PaymentInitiationTemplate
{
    /// <summary>The initiating party's message identifier.</summary>
    public string MessageIdentifier = string.Empty;
    /// <summary>The payment-information block identifier.</summary>
    public string PaymentInformationIdentifier = string.Empty;
    /// <summary>The end-to-end transaction identifier.</summary>
    public string EndToEndIdentifier = string.Empty;
    /// <summary>The debtor display name.</summary>
    public string Debtor = string.Empty;
    /// <summary>The debtor account identifier.</summary>
    public string DebtorAccount = string.Empty;
    /// <summary>The creditor display name.</summary>
    public string Creditor = string.Empty;
    /// <summary>The creditor account identifier.</summary>
    public string CreditorAccount = string.Empty;
    /// <summary>The instructed amount and exact currency.</summary>
    public IsoAmountValue Amount = new IsoAmountValue();
    /// <summary>The requested execution date.</summary>
    public IsoDateValue RequestedExecutionDate = new IsoDateValue();
    /// <summary>The unstructured remittance information.</summary>
    public string RemittanceInformation = string.Empty;
}
