using System;

namespace pdf;

/// <summary>Stores one contract signature line.</summary>
public class PdfContractSignature
{
    /// <summary>Stores the signing party.</summary>
    public string Party = string.Empty;
    /// <summary>Stores the signer name.</summary>
    public string Signer = string.Empty;

    /// <summary>Initializes a contract signature line.</summary>
    /// <param name="party">The signing party.</param>
    /// <param name="signer">The signer name.</param>
    public PdfContractSignature(string party, string signer)
    {
        if (party == null) { throw new ArgumentNullException("party"); }
        if (signer == null) { throw new ArgumentNullException("signer"); }
        Party = party;
        Signer = signer;
    }
}
