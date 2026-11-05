using System;
namespace token;

/// <summary>Identifies a COSE protection applied to a CWT Claims Set.</summary>
public enum CwtProtectionKind
{
    /// <summary>A single-signature COSE_Sign1 layer.</summary>
    Sign1 = 0,

    /// <summary>A multiple-signature COSE_Sign layer.</summary>
    Sign = 1,

    /// <summary>A recipient-less COSE_Mac0 layer.</summary>
    Mac0 = 2,

    /// <summary>A recipient-bearing COSE_Mac layer.</summary>
    Mac = 3,

    /// <summary>A single-recipient COSE_Encrypt0 layer.</summary>
    Encrypt0 = 4,

    /// <summary>A recipient-bearing COSE_Encrypt layer.</summary>
    Encrypt = 5
}
