namespace fake;

/// <summary>
/// Describes one country or territory format from the bundled IBAN registry transcription.
/// </summary>
/// <remarks>
/// A format describes syntax only. It is not evidence that an account exists or can receive funds.
/// </remarks>
public class IbanFormat
{
    /// <summary>The two-letter country or territory code used to select the registry entry.</summary>
    public string CountryCode = string.Empty;

    /// <summary>The two-letter prefix placed at the start of an IBAN.</summary>
    public string IbanPrefix = string.Empty;

    /// <summary>The required electronic IBAN length.</summary>
    public int IbanLength;

    /// <summary>The compact registry pattern used for the BBAN portion.</summary>
    public string BbanPattern = string.Empty;

    /// <summary>The required BBAN length.</summary>
    public int BbanLength;

    /// <summary>Whether the registry entry identifies the country or territory as part of SEPA.</summary>
    public bool IsSepa;

    /// <summary>The zero-based start of the bank code within the BBAN.</summary>
    public int BankCodeStart;

    /// <summary>The bank-code length within the BBAN.</summary>
    public int BankCodeLength;

    /// <summary>The zero-based start of the branch code within the BBAN.</summary>
    public int BranchCodeStart;

    /// <summary>The branch-code length within the BBAN, or zero when no separate branch range is defined.</summary>
    public int BranchCodeLength;

    /// <summary>The zero-based start of the account code within the BBAN.</summary>
    public int AccountCodeStart;

    /// <summary>The account-code length within the BBAN.</summary>
    public int AccountCodeLength;

    /// <summary>The version of the bundled registry transcription.</summary>
    public string RegistryVersion = string.Empty;

    internal IbanFormat()
    {
    }
}
